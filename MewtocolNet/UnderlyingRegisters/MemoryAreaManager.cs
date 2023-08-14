using MewtocolNet.Logging;
using MewtocolNet.Registers;
using MewtocolNet.SetupClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    internal class MemoryAreaManager {

        internal event Action MemoryLayoutChanged;

        internal int maxOptimizationDistance = 8;
        internal int maxRegistersPerGroup = -1;
        internal PollLevelOverwriteMode pollLevelOrMode = PollLevelOverwriteMode.Highest;

        private int wrAreaSize;
        private int dtAreaSize;

        internal MewtocolInterface mewInterface;
        internal List<PollLevel> pollLevels;

        internal Dictionary<int, PollLevelConfig> pollLevelConfigs = new Dictionary<int, PollLevelConfig>() {
            { 
                MewtocolNet.PollLevel.Always, 
                new PollLevelConfig {
                    skipNth = 1,
                }
            },
            {
                MewtocolNet.PollLevel.FirstIteration, 
                new PollLevelConfig {
                    skipAllButFirst = true
                }
            },
            {
                MewtocolNet.PollLevel.Never, 
                new PollLevelConfig {
                    skipsAll = true,
                }
            }
        };

        private uint pollIteration = 0;

        internal MemoryAreaManager(MewtocolInterface mewIf, int wrSize = 512, int dtSize = 32_765) {

            mewInterface = mewIf;
            Setup(wrSize, dtSize);

        }

        // Later on pass memory area sizes here
        internal void Setup(int wrSize, int dtSize) {

            wrAreaSize = wrSize;
            dtAreaSize = dtSize;
            pollLevels = new List<PollLevel>();

        }

        internal async Task OnPlcConnected () {

            await Task.CompletedTask;

        }

        internal void LinkAndMergeRegisters(List<Register> registers = null) {

            //for self calling
            if (registers == null) {
           
                //get a copy of the current ones
                registers = GetAllRegisters().ToList();
                //clear old ones
                ClearAllRegisters();
           
            }

            //maxes the highest poll level for all registers that contain each other
            var ordered = registers
            .OrderByDescending(x => x.GetRegisterAddressLen())
            .ToList();

            ordered.ForEach(x => x.AveragePollLevel(registers, pollLevelOrMode));

            //insert into area
            foreach (var reg in ordered) {

                TestPollLevelExistence(reg);

                AddToArea(reg, reg.RegisterType);

                //reset overlap fitted for all
                reg.wasOverlapFitted = false;

            }

            //order 
            for (int i = 0; i < pollLevels.Count; i++) {

                PollLevel lvl = pollLevels[i];

                //poll level has no areas
                if(lvl.GetAllAreas().Count() == 0) {

                    pollLevels.Remove(lvl);
                    continue;

                }

                foreach (var area in lvl.GetAllAreas()) {

                    area.managedRegisters = area.managedRegisters.OrderBy(x => x.AddressStart).ToList();

                }

                //lvl.dataAreas = lvl.dataAreas.OrderBy(x => x.AddressStart).ToList();

            }

            MemoryLayoutChanged?.Invoke();

        }

        private void TestPollLevelExistence(Register reg) {

            if (!pollLevels.Any(x => x.level == reg.pollLevel)) {

                pollLevels.Add(new PollLevel(wrAreaSize, dtAreaSize) {
                    level = reg.pollLevel,
                });

                //add config if it was not made at setup
                if (!pollLevelConfigs.ContainsKey(reg.pollLevel)) {
                    pollLevelConfigs.Add(reg.pollLevel, new PollLevelConfig {
                        skipNth = reg.pollLevel,
                    });
                }

            }

        }

        private void AddToArea(Register insertReg, RegisterPrefix prefix) {

            uint regInsAddStart = insertReg.MemoryAddress;
            uint regInsAddEnd = insertReg.MemoryAddress + insertReg.GetRegisterAddressLen() - 1;

            AreaBase targetArea = null;

            var pollLevelFound = pollLevels.FirstOrDefault(x => x.level == insertReg.pollLevel);
            List<AreaBase> pollLevelAreas = null;

            switch (prefix) {
                case RegisterPrefix.X:
                pollLevelAreas = pollLevelFound.externalRelayInAreas;
                break;
                case RegisterPrefix.Y:
                pollLevelAreas = pollLevelFound.externalRelayOutAreas;
                break;
                case RegisterPrefix.R:
                pollLevelAreas = pollLevelFound.internalRelayAreas;
                break;
                case RegisterPrefix.DT:
                case RegisterPrefix.DDT:
                pollLevelAreas = pollLevelFound.dataAreas;
                break;
            }

            foreach (var dtArea in pollLevelAreas) {

                bool addressInsideArea = regInsAddStart >= dtArea.AddressStart &&
                                         regInsAddEnd <= dtArea.AddressEnd;

                if (addressInsideArea) {

                    //found an area that is already existing where the register can fit into 
                    targetArea = dtArea;
                    break;

                }

                //found adjacent before
                if (dtArea.AddressEnd <= regInsAddStart) {

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

                targetArea = new AreaBase(mewInterface, pollLevelFound.level) {
                    addressStart = regInsAddStart,
                    addressEnd = regInsAddEnd,
                    registerType = insertReg.RegisterType,
                };

                targetArea.BoundaryUdpdate();
                pollLevelAreas.Add(targetArea);

            }

            insertReg.underlyingMemory = targetArea;

            if (insertReg.autoGenerated && insertReg.name == null) {
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

            var dupedTypeReg = existinglinkedGroup.Linked
            .FirstOrDefault(x => x.IsSameAddressAndType(insertReg) && x.PollLevel == insertReg.PollLevel);

            if (dupedTypeReg != null) {
                dupedTypeReg.WithBoundProperties(insertReg.boundProperties);
                dupedTypeReg.autoGenerated = insertReg.autoGenerated;
            } else {
                existinglinkedGroup.Linked.Add(insertReg);
                existinglinkedGroup.Linked = existinglinkedGroup.Linked.OrderBy(x => x.MemoryAddress).ToList();
            }

        }

        internal async Task PollAllAreasAsync(Func<Task> inbetweenCallback) {

            foreach (var pollLevel in pollLevels) {

                var sw = Stopwatch.StartNew();

                var lvlConfig = pollLevelConfigs[pollLevel.level];

                if (lvlConfig.skipsAll) continue;
                if (lvlConfig.skipAllButFirst && pollIteration > 0) continue;

                //determine to skip poll levels, first iteration is always polled
                if (pollIteration > 0 && pollLevel.level > 1) {

                    var skipIterations = lvlConfig.skipNth;
                    var skipDelay = lvlConfig.delay;

                    if (skipIterations != null && pollIteration % skipIterations.Value != 0) {

                        //count delayed poll skips
                        continue;

                    } else if (skipDelay != null) {

                        //time delayed poll skips

                        if (lvlConfig.timeFromLastRead.Elapsed < skipDelay.Value) {

                            continue;

                        }

                    }

                }

                //set stopwatch for levels
                if (pollLevelConfigs.ContainsKey(pollLevel.level)) {

                    pollLevelConfigs[pollLevel.level].timeFromLastRead = Stopwatch.StartNew();

                }

                //update registers in poll level
                foreach (var area in pollLevel.GetAllAreas().ToArray()) {

                    //set the whole memory area at once
                    await area.RequestByteReadAsync(area.AddressStart, area.AddressEnd);

                    await inbetweenCallback();

                }

                pollLevel.lastReadTimeMs = (int)sw.Elapsed.TotalMilliseconds;

            }

            if (pollIteration == uint.MaxValue) {
                pollIteration = uint.MinValue;
            } else {
                pollIteration++;
            }

        }

        internal string ExplainLayout() {

            var sb = new StringBuilder();

            foreach (var pollLevel in pollLevels) {

                if (pollLevel.level == MewtocolNet.PollLevel.Always) {

                    sb.AppendLine($"\n> ==== Poll lvl ALWAYS ====\n");
                    sb.AppendLine($"> Poll each iteration");

                }else if (pollLevel.level == MewtocolNet.PollLevel.FirstIteration) {

                    sb.AppendLine($"\n> ==== Poll lvl FIRST ITERATION ====\n");
                    sb.AppendLine($"> Poll only on the first iteration");

                } else if (pollLevel.level == MewtocolNet.PollLevel.Never) {

                    sb.AppendLine($"\n> ==== Poll lvl NEVER ====\n");
                    sb.AppendLine($"> Poll never");

                } else {

                    sb.AppendLine($"\n> ==== Poll lvl {pollLevel.level} ====\n");
                    if (pollLevelConfigs.ContainsKey(pollLevel.level) && pollLevelConfigs[pollLevel.level].delay != null) {
                        sb.AppendLine($"> Poll each {pollLevelConfigs[pollLevel.level].delay?.TotalMilliseconds}ms");
                    } else if (pollLevelConfigs.ContainsKey(pollLevel.level)) {
                        sb.AppendLine($"> Poll every {pollLevelConfigs[pollLevel.level].skipNth} iterations");
                    }

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

                        if (prevGroup != null &&
                            linkedG.AddressStart != prevGroup.AddressStart &&
                            linkedG.AddressEnd > prevGroup.AddressEnd &&
                            linkedG.AddressStart - prevGroup.AddressEnd > 1) {

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

        internal void ClearAllRegisters () {

            foreach (var lvl in pollLevels) {

                lvl.dataAreas.Clear();
            
            }
            
        }

        internal IEnumerable<Register> GetAllRegisters() {

            List<Register> registers = new List<Register>();

            foreach (var lvl in pollLevels) {

                registers.AddRange(lvl.dataAreas.SelectMany(x => x.managedRegisters).SelectMany(x => x.Linked));
                registers.AddRange(lvl.internalRelayAreas.SelectMany(x => x.managedRegisters).SelectMany(x => x.Linked));
                registers.AddRange(lvl.externalRelayInAreas.SelectMany(x => x.managedRegisters).SelectMany(x => x.Linked));
                registers.AddRange(lvl.externalRelayOutAreas.SelectMany(x => x.managedRegisters).SelectMany(x => x.Linked));

            }

            return registers.OrderBy(x => x.MemoryAddress).ThenByDescending(x => x.GetRegisterString());

        }

        internal IReadOnlyList<IMemoryArea> GetAllMemoryAreas() {

            List<IMemoryArea> areas = new List<IMemoryArea>();

            foreach (var lvl in pollLevels) {

                areas.AddRange(lvl.GetAllAreas());

            }

            return areas;

        }

        internal bool HasSingleCyclePollableRegisters() {

            bool hasCyclicPollableLevels = pollLevels.Any(x => x.level != MewtocolNet.PollLevel.FirstIteration);

            return hasCyclicPollableLevels;

        }

        internal bool HasCyclicPollableRegisters () {

            bool hasCyclicPollableLevels = pollLevels
            .Any(x => x.level != MewtocolNet.PollLevel.Never && x.level != MewtocolNet.PollLevel.FirstIteration &&  x.level != 0);

            return hasCyclicPollableLevels;

        }

    }

}
