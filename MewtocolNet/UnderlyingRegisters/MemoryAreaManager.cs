using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.UnderlyingRegisters {

    internal class MemoryAreaManager {

        internal int maxOptimizationDistance = 8;
        internal int maxRegistersPerGroup = -1;

        internal MewtocolInterface mewInterface;

        // WR areas are n of words, each word has 2 bytes representing the "special address component"

        //X WR
        internal List<WRArea> externalRelayInAreas;

        //Y WR
        internal List<WRArea> externalRelayOutAreas;

        //R WR
        internal List<WRArea> internalRelayAreas;

        //DT
        internal List<DTArea> dataAreas;

        internal MemoryAreaManager (MewtocolInterface mewIf, int wrSize = 512, int dtSize = 32_765) {

            mewInterface = mewIf;   
            Setup(wrSize, dtSize);      

        }

        // Later on pass memory area sizes here
        internal void Setup (int wrSize, int dtSize) {

            externalRelayInAreas = new List<WRArea>(wrSize * 16);
            externalRelayOutAreas = new List<WRArea>(wrSize * 16);
            internalRelayAreas = new List<WRArea>(wrSize * 16);
            dataAreas = new List<DTArea>(dtSize);

        }

        internal bool LinkRegister (BaseRegister reg) {

            switch (reg.RegisterType) {
                case RegisterType.X:
                return AddWRArea(reg, externalRelayInAreas);
                case RegisterType.Y:
                return AddWRArea(reg, externalRelayOutAreas);
                case RegisterType.R:
                return AddWRArea(reg, internalRelayAreas);
                case RegisterType.DT:
                case RegisterType.DDT:
                case RegisterType.DT_BYTE_RANGE:
                return AddDTArea(reg);
            }

            return false;

        }

        private bool AddWRArea (BaseRegister insertReg, List<WRArea> collection) {

            WRArea area = collection.FirstOrDefault(x => x.AddressStart == insertReg.MemoryAddress);

            if(area != null) {

                var existingLinkedRegister = area.linkedRegisters
                .FirstOrDefault(x => x.CompareIsDuplicate(insertReg));

                if(existingLinkedRegister != null) {

                    foreach (var prop in insertReg.boundToProps)
                        existingLinkedRegister.WithBoundProperty(prop);

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

        private bool AddDTArea (BaseRegister insertReg) {

            uint regInsAddStart = insertReg.MemoryAddress;
            uint regInsAddEnd = insertReg.MemoryAddress + insertReg.GetRegisterAddressLen() - 1;

            DTArea targetArea = null;

            foreach (var dtArea in dataAreas) {

                bool matchingAddress = regInsAddStart >= dtArea.AddressStart &&
                                       regInsAddEnd <= dtArea.addressEnd;

                //found matching
                if (matchingAddress) {

                    //check if the area has registers linked that are overlapping (not matching)
                    var foundDupe = dtArea.linkedRegisters.FirstOrDefault(x => x.CompareIsDuplicateNonCast(insertReg));

                    if(foundDupe != null) {
                        throw new NotSupportedException(
                        message: $"Can't have registers of different types at the same referenced plc address: " +
                                 $"{insertReg.PLCAddressName} ({insertReg.GetType()}) <=> " +
                                 $"{foundDupe.PLCAddressName} ({foundDupe.GetType()})"
                        );
                    }

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

            var existingLinkedRegister = targetArea.linkedRegisters
                .FirstOrDefault(x => x.CompareIsDuplicate(insertReg));

            if (existingLinkedRegister != null) {

                foreach (var prop in insertReg.boundToProps)
                    existingLinkedRegister.WithBoundProperty(prop);

                return false;

            } else {

                targetArea.linkedRegisters.Add(insertReg);
                return true;

            }
            
        }

        internal async Task PollAllAreasAsync () {

            foreach (var dtArea in dataAreas) {

                //set the whole memory area at once
                var res = await dtArea.RequestByteReadAsync(dtArea.AddressStart, dtArea.AddressEnd);

                foreach (var register in dtArea.linkedRegisters) {

                    var regStart = register.MemoryAddress;
                    var addLen = (int)register.GetRegisterAddressLen();

                    var bytes = dtArea.GetUnderlyingBytes(regStart, addLen);
                    register.SetValueFromBytes(bytes);

                }

            }

        }

        internal void Merge () {

            //merge gaps that the algorithm didn't catch be rerunning the register attachment

            var allDataAreaRegisters = dataAreas.SelectMany(x => x.linkedRegisters).ToList();
            dataAreas = new List<DTArea>(allDataAreaRegisters.Capacity);

            foreach (var reg in allDataAreaRegisters)
                AddDTArea(reg);

        }

        internal string ExplainLayout () {

            var sb = new StringBuilder();

            sb.AppendLine("---- DT Area ----");

            sb.AppendLine($"Optimization distance: {maxOptimizationDistance}");

            foreach (var area in dataAreas) {

                sb.AppendLine();
                sb.AppendLine($"=> {area} = {area.underlyingBytes.Length} bytes");
                sb.AppendLine();
                sb.AppendLine(string.Join("\n", area.underlyingBytes.ToHexString(" ").SplitInParts(3 * 8)));
                sb.AppendLine();

                foreach (var reg in area.linkedRegisters) {

                    sb.AppendLine($"{reg.ToString(true)}");

                }

            }

            sb.AppendLine("---- WR X Area ----");

            foreach (var area in externalRelayInAreas) {

                sb.AppendLine(area.ToString());

                foreach (var reg in area.linkedRegisters) {

                    sb.AppendLine($"{reg.ToString(true)}");

                }

            }

            sb.AppendLine("---- WR Y Area ----");

            foreach (var area in externalRelayOutAreas) {

                sb.AppendLine(area.ToString());

                foreach (var reg in area.linkedRegisters) {

                    sb.AppendLine($"{reg.ToString(true)}");

                }

            }

            sb.AppendLine("---- WR R Area ----");

            foreach (var area in internalRelayAreas) {

                sb.AppendLine(area.ToString());

                foreach (var reg in area.linkedRegisters) {

                    sb.AppendLine($"{reg.ToString(true)}");

                }

            }

            return sb.ToString();

        } 


    } 

}
