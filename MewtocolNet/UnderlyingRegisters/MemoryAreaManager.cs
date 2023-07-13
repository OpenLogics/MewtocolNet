using MewtocolNet.Helpers;
using MewtocolNet.Registers;
using MewtocolNet.SetupClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    internal class MemoryAreaManager {

        internal int maxOptimizationDistance = 8;
        internal int maxRegistersPerGroup = -1;
        internal bool allowByteRegDupes;

        private int wrAreaSize;
        private int dtAreaSize;

        internal MewtocolInterface mewInterface;
        internal List<PollLevel> pollLevels;
        internal Dictionary<int, PollLevelConfig> pollLevelConfigs = new Dictionary<int, PollLevelConfig>();

        private uint pollIteration = 0;

        internal MemoryAreaManager (MewtocolInterface mewIf, int wrSize = 512, int dtSize = 32_765) {

            mewInterface = mewIf;   
            Setup(wrSize, dtSize);      

        }

        // Later on pass memory area sizes here
        internal void Setup (int wrSize, int dtSize) {

            wrAreaSize = wrSize;
            dtAreaSize = dtSize;
            pollLevels = new List<PollLevel> {
                new PollLevel(wrSize, dtSize) {
                    level = 1,
                }
            };

        }

        internal void LinkRegisters (List<BaseRegister> registers = null) {

            //for self calling
            if (registers == null) registers = GetAllRegisters().ToList();

            //pre combine
            var groupedByAdd = registers
            .GroupBy(x => new {
                x.MemoryAddress,
                len = x.GetRegisterAddressLen(),
                spadd = x.GetSpecialAddress(),
            });

            var filteredRegisters = new List<BaseRegister>();    
            var propertyLookupTable = new Dictionary<PropertyInfo, BaseRegister>(); 

            foreach (var addressGroup in groupedByAdd) {

                var ordered = addressGroup.OrderBy(x => x.pollLevel);
                var highestPollLevel = ordered.Max(x => x.pollLevel);

                var distinctByUnderlyingType = 
                ordered.GroupBy(x => x.underlyingSystemType).ToList();

                foreach (var underlyingTypeGroup in distinctByUnderlyingType) {

                    foreach (var register in underlyingTypeGroup) {

                        register.pollLevel = highestPollLevel;

                        var alreadyAdded = filteredRegisters
                        .FirstOrDefault(x => x.underlyingSystemType == register.underlyingSystemType);

                        if(alreadyAdded == null) {
                            filteredRegisters.Add(register);
                        } else {
                            alreadyAdded.WithBoundProperties(register.boundProperties);
                        }

                    }
                    
                }

            }

            foreach (var reg in filteredRegisters) {

                TestPollLevelExistence(reg);

                switch (reg.RegisterType) {
                    case RegisterType.X:
                    case RegisterType.Y:
                    case RegisterType.R:
                    AddToWRArea(reg);
                    break;
                    case RegisterType.DT:
                    case RegisterType.DDT:
                    case RegisterType.DT_BYTE_RANGE:
                    AddToDTArea(reg);
                    break;
                }

            }

        }

        private void TestPollLevelExistence (BaseRegister reg) {

            if(!pollLevelConfigs.ContainsKey(1)) {
                pollLevelConfigs.Add(1, new PollLevelConfig {
                    skipNth = 1,
                });
            }

            if(!pollLevels.Any(x => x.level == reg.pollLevel)) {

                pollLevels.Add(new PollLevel(wrAreaSize, dtAreaSize) {
                    level = reg.pollLevel,
                });
            
                //add config if it was not made at setup
                if(!pollLevelConfigs.ContainsKey(reg.pollLevel)) {
                    pollLevelConfigs.Add(reg.pollLevel, new PollLevelConfig {
                        skipNth = reg.pollLevel,
                    });
                }

            }
            
        }

        private bool AddToWRArea (BaseRegister insertReg) {

            var pollLevelFound = pollLevels.FirstOrDefault(x => x.level == insertReg.pollLevel);

            List<WRArea> collection = null;

            switch (insertReg.RegisterType) {
                case RegisterType.X:
                collection = pollLevelFound.externalRelayInAreas;
                break;
                case RegisterType.Y:
                collection = pollLevelFound.externalRelayOutAreas;
                break;
                case RegisterType.R:
                collection = pollLevelFound.internalRelayAreas;
                break;
            }

            WRArea area = collection.FirstOrDefault(x => x.AddressStart == insertReg.MemoryAddress);

            if(area != null) {

                var existingLinkedRegister = area.linkedRegisters
                .FirstOrDefault(x => x.CompareIsDuplicate(insertReg));

                if(existingLinkedRegister != null) {

                    return false;

                } else {

                    insertReg.underlyingMemory = area;
                    area.linkedRegisters.Add(insertReg);
                    return true;

                }

            } else {

                area = new WRArea(mewInterface) {
                    registerType = insertReg.RegisterType,
                    addressStart = insertReg.MemoryAddress,
                };

                insertReg.underlyingMemory = area;
                area.linkedRegisters.Add(insertReg);

                collection.Add(area);
                collection = collection.OrderBy(x => x.AddressStart).ToList();

                return true;

            }

        }

        private void AddToDTArea (BaseRegister insertReg) {

            uint regInsAddStart = insertReg.MemoryAddress;
            uint regInsAddEnd = insertReg.MemoryAddress + insertReg.GetRegisterAddressLen() - 1;

            DTArea targetArea = null;

            var pollLevelFound = pollLevels.FirstOrDefault(x => x.level == insertReg.pollLevel);
            var dataAreas = pollLevelFound.dataAreas;

            foreach (var dtArea in dataAreas) {

                bool addressInsideArea = regInsAddStart >= dtArea.AddressStart &&
                                         regInsAddEnd <= dtArea.AddressEnd;

                if (addressInsideArea) {

                    //found an area that is already existing where the register can fit into 
                    targetArea = dtArea;
                    break;

                }

                //found adjacent before
                if(dtArea.AddressEnd <= regInsAddStart) {

                    ulong distance = regInsAddStart - dtArea.AddressEnd;

                    if (distance <= (uint)maxOptimizationDistance) {

                        //expand the boundaries for the area to include the new adjacent area
                        dtArea.BoundaryUdpdate(addrTo: regInsAddEnd);
                        targetArea = dtArea;
                        break;

                    }

                }

                //found adjacent after
                if (dtArea.AddressStart >= regInsAddEnd) {

                    ulong distance = dtArea.AddressStart - regInsAddEnd;

                    if (distance <= (uint)maxOptimizationDistance) {

                        //expand the boundaries for the area to include the new adjacent area
                        dtArea.BoundaryUdpdate(addrFrom: regInsAddStart);
                        targetArea = dtArea;
                        break;

                    }

                }

            }

            //create a new area
            if (targetArea == null) {

                targetArea = new DTArea(mewInterface) {
                    addressStart = regInsAddStart,
                    addressEnd = regInsAddEnd,
                    registerType = insertReg.RegisterType,
                };

                targetArea.BoundaryUdpdate();
                dataAreas.Add(targetArea);
            
            }

            insertReg.underlyingMemory = targetArea;

            if (insertReg.name == null) {
                insertReg.name = $"auto_{Guid.NewGuid().ToString("N")}";
            }

            Console.WriteLine($"Adding linked register: {insertReg}");
            targetArea.linkedRegisters.Add(insertReg);
            return;

        }

        internal void MergeAndSizeDataAreas () {

            //merge gaps that the algorithm didn't catch be rerunning the register attachment

            LinkRegisters();

        }

        internal async Task PollAllAreasAsync () {

            foreach (var pollLevel in pollLevels) {

                var sw = Stopwatch.StartNew();

                //determine to skip poll levels, first iteration is always polled
                if(pollIteration > 0 && pollLevel.level > 1) {

                    var lvlConfig = pollLevelConfigs[pollLevel.level];
                    var skipIterations = lvlConfig.skipNth;
                    var skipDelay = lvlConfig.delay;

                    if (skipIterations != null && pollIteration % skipIterations.Value != 0) {

                        //count delayed poll skips
                        continue;

                    } else if(skipDelay != null) {

                        //time delayed poll skips

                        if(lvlConfig.timeFromLastRead.Elapsed < skipDelay.Value) {

                            continue;

                        }

                    }

                }

                //set stopwatch for levels
                if(pollLevelConfigs.ContainsKey(pollLevel.level)) {

                    pollLevelConfigs[pollLevel.level].timeFromLastRead = Stopwatch.StartNew();   
                
                }

                //update registers in poll level
                foreach (var dtArea in pollLevel.dataAreas.ToArray()) {

                    //set the whole memory area at once
                    await dtArea.RequestByteReadAsync(dtArea.AddressStart, dtArea.AddressEnd);

                }

                pollLevel.lastReadTimeMs = (int)sw.Elapsed.TotalMilliseconds;

            }

            if(pollIteration == uint.MaxValue) {
                pollIteration = uint.MinValue;
            } else {
                pollIteration++;
            }

        }

        internal string ExplainLayout () {

            var sb = new StringBuilder();

            foreach (var pollLevel in pollLevels) {

                sb.AppendLine($"==== Poll lvl {pollLevel.level} ====");

                sb.AppendLine();
                if (pollLevelConfigs[pollLevel.level].delay != null) {
                    sb.AppendLine($"Poll each {pollLevelConfigs[pollLevel.level].delay?.TotalMilliseconds}ms");
                } else {
                    sb.AppendLine($"Poll every {pollLevelConfigs[pollLevel.level].skipNth} iterations");
                }
                sb.AppendLine($"Level read time: {pollLevel.lastReadTimeMs}ms");
                sb.AppendLine($"Optimization distance: {maxOptimizationDistance}");
                sb.AppendLine();

                sb.AppendLine($"---- DT Areas: ----");

                foreach (var area in pollLevel.dataAreas) {

                    sb.AppendLine();
                    sb.AppendLine($"=> {area} = {area.underlyingBytes.Length} bytes");
                    sb.AppendLine();
                    sb.AppendLine(string.Join("\n", area.underlyingBytes.ToHexString(" ").SplitInParts(3 * 8)));
                    sb.AppendLine();

                    foreach (var reg in area.linkedRegisters) {

                        sb.AppendLine($"{reg.Explain()}");

                    }

                }

                sb.AppendLine($"---- WR X Area ----");

                foreach (var area in pollLevel.externalRelayInAreas) {

                    sb.AppendLine(area.ToString());

                    foreach (var reg in area.linkedRegisters) {

                        sb.AppendLine($"{reg.Explain()}");

                    }

                }

                sb.AppendLine($"---- WR Y Area ---");

                foreach (var area in pollLevel.externalRelayOutAreas) {

                    sb.AppendLine(area.ToString());

                    foreach (var reg in area.linkedRegisters) {

                        sb.AppendLine($"{reg.Explain()}");

                    }

                }

                sb.AppendLine($"---- WR R Area ----");

                foreach (var area in pollLevel.internalRelayAreas) {

                    sb.AppendLine(area.ToString());

                    foreach (var reg in area.linkedRegisters) {

                        sb.AppendLine($"{reg.Explain()}");

                    }

                }

            }

            return sb.ToString();

        } 

        internal IEnumerable<BaseRegister> GetAllRegisters () {

            List<BaseRegister> registers = new List<BaseRegister>();    

            foreach (var lvl in pollLevels) {

                registers.AddRange(lvl.dataAreas.SelectMany(x => x.linkedRegisters));
                registers.AddRange(lvl.internalRelayAreas.SelectMany(x => x.linkedRegisters));
                registers.AddRange(lvl.externalRelayInAreas.SelectMany(x => x.linkedRegisters));
                registers.AddRange(lvl.externalRelayOutAreas.SelectMany(x => x.linkedRegisters));

            }

            return registers;

        }

    } 

}
