﻿using MewtocolNet.Helpers;
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

        internal void LinkAndMergeRegisters (List<BaseRegister> registers = null) {

            //for self calling
            if (registers == null) registers = GetAllRegisters().ToList();

            //pre combine per address
            var groupedByAdd = registers
            .GroupBy(x => new {
                x.MemoryAddress,
                len = x.GetRegisterAddressLen(),
                spadd = x.GetSpecialAddress(),
            });

            //poll level merging
            foreach (var addressGroup in groupedByAdd) {

                //determine highest poll level for same addresses
                var highestPollLevel = addressGroup.Max(x => x.pollLevel);

                //apply poll level to all registers in same group
                foreach (var reg in addressGroup)
                    reg.pollLevel = highestPollLevel;  

            }

            //insert into area
            foreach (var reg in registers) {

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

            //order 

            foreach (var lvl in pollLevels) {

                foreach (var area in lvl.dataAreas) {

                    area.managedRegisters = area.managedRegisters.OrderBy(x => x.AddressStart).ToList();

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

            var existinglinkedGroup = targetArea.managedRegisters
            .FirstOrDefault(x => x.AddressStart == insertReg.MemoryAddress &&
                                 x.AddressEnd == insertReg.GetRegisterAddressEnd());

            if (existinglinkedGroup == null) {
                // make a new linked group
                existinglinkedGroup = new LinkedRegisterGroup {
                    AddressStart = insertReg.MemoryAddress,
                    AddressEnd = insertReg.GetRegisterAddressEnd(),
                };
                targetArea.managedRegisters.Add(existinglinkedGroup);   
            }

            //check if the linked group has duplicate type registers

            var dupedTypeReg = existinglinkedGroup.Linked.FirstOrDefault(x => x.IsSameAddressAndType(insertReg));
            if (dupedTypeReg != null) {
                dupedTypeReg.WithBoundProperties(insertReg.boundProperties);
            } else {
                existinglinkedGroup.Linked.Add(insertReg);
            }

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

                sb.AppendLine($"\n> ==== Poll lvl {pollLevel.level} ====");

                sb.AppendLine();
                if (pollLevelConfigs[pollLevel.level].delay != null) {
                    sb.AppendLine($"> Poll each {pollLevelConfigs[pollLevel.level].delay?.TotalMilliseconds}ms");
                } else {
                    sb.AppendLine($"> Poll every {pollLevelConfigs[pollLevel.level].skipNth} iterations");
                }
                sb.AppendLine($"> Level read time: {pollLevel.lastReadTimeMs}ms");
                sb.AppendLine($"> Optimization distance: {maxOptimizationDistance}");
                sb.AppendLine();

                foreach (var area in pollLevel.dataAreas) {

                    var areaHeader = $"AREA => {area} = {area.underlyingBytes.Length} bytes";
                    sb.AppendLine($"* {new string('-', areaHeader.Length)}*");
                    sb.AppendLine($"* {areaHeader}");
                    sb.AppendLine($"* {new string('-', areaHeader.Length)}*");
                    sb.AppendLine("*");
                    sb.AppendLine($"* {(string.Join("\n* ", area.underlyingBytes.ToHexString(" ").SplitInParts(3 * 8)))}");
                    sb.AppendLine("*");

                    int seperatorLen = 50;

                    LinkedRegisterGroup prevGroup = null;

                    foreach (var linkedG in area.managedRegisters) {

                        if (prevGroup != null && (linkedG.AddressStart - prevGroup.AddressEnd - 1 > 0)) {

                            var dist = linkedG.AddressStart - prevGroup.AddressEnd - 1;

                            sb.AppendLine($"* {new string('=', seperatorLen + 3)}");
                            sb.AppendLine($"* Byte spacer: {dist} Words");
                            sb.AppendLine($"* {new string('=', seperatorLen + 3)}");

                        }

                        sb.AppendLine($"* {new string('_', seperatorLen + 3)}");
                        sb.AppendLine($"* || Linked group {linkedG.AddressStart} - {linkedG.AddressEnd}");
                        sb.AppendLine($"* || {new string('=', seperatorLen)}");

                        foreach (var reg in linkedG.Linked) {

                            string explained = reg.Explain();

                            sb.AppendLine($"* || {explained.Replace("\n", "\n* || ")}");

                            if (linkedG.Linked.Count > 1) {
                                sb.AppendLine($"* || {new string('-', seperatorLen)}");
                            }

                        }

                        sb.AppendLine($"* {new string('=', seperatorLen + 3)}");

                        prevGroup = linkedG;    

                    }

                    sb.AppendLine();

                }

            }

            return sb.ToString();

        } 

        internal IEnumerable<BaseRegister> GetAllRegisters () {

            List<BaseRegister> registers = new List<BaseRegister>();    

            foreach (var lvl in pollLevels) {

                registers.AddRange(lvl.dataAreas.SelectMany(x => x.managedRegisters).SelectMany(x => x.Linked));

                registers.AddRange(lvl.internalRelayAreas.SelectMany(x => x.linkedRegisters));
                registers.AddRange(lvl.externalRelayInAreas.SelectMany(x => x.linkedRegisters));
                registers.AddRange(lvl.externalRelayOutAreas.SelectMany(x => x.linkedRegisters));

            }

            return registers;

        }

    } 

}
