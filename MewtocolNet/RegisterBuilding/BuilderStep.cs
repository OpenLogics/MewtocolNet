using MewtocolNet.Registers;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace MewtocolNet.RegisterBuilding {

    public static class BuilderStepExtensions {

        public static BuilderStep<T> AsType<T> (this BuilderStepBase baseStep) {

            if (!typeof(T).IsAllowedPlcCastingType()) {

                throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

            }
           
            var castStp = new BuilderStep<T>();

            if (baseStep.SpecialAddress != null) castStp.SpecialAddress = baseStep.SpecialAddress;

            castStp.Name = baseStep.Name;
            castStp.RegType = baseStep.RegType;
            castStp.MemAddress = baseStep.MemAddress;
            castStp.MemByteSize = baseStep.MemByteSize;
            castStp.dotnetVarType = typeof(T);
            castStp.plcVarType = null;
            castStp.wasCasted = true;

            return castStp;

        }

        public static BuilderStep AsType (this BuilderStepBase baseStep, Type type) {

            if (!type.IsAllowedPlcCastingType()) {

                throw new NotSupportedException($"The dotnet type {type}, is not supported for PLC type casting");

            }

            var castStp = new BuilderStep();

            if (baseStep.SpecialAddress != null) castStp.SpecialAddress = baseStep.SpecialAddress;

            castStp.Name = baseStep.Name;
            castStp.RegType = baseStep.RegType;
            castStp.MemAddress = baseStep.MemAddress;
            castStp.MemByteSize = baseStep.MemByteSize;
            castStp.dotnetVarType = type;
            castStp.plcVarType = null;
            castStp.wasCasted = true;

            return castStp;

        }

        public static IRegister Build (this BuilderStepBase step) {

            //if no casting method in builder was called => autocast the type from the RegisterType
            if (!step.wasCasted && step.MemByteSize == null) step.AutoType();

            //fallbacks if no casting builder was given
            BuilderStepBase.GetFallbackDotnetType(step);

            BaseRegister builtReg;

            var bInfo = new RegisterBuildInfo {

                name = step.Name,
                specialAddress = step.SpecialAddress,
                memoryAddress = step.MemAddress,
                memorySizeBytes = step.MemByteSize,
                memorySizeBits = step.MemBitSize,
                registerType = step.RegType,
                dotnetCastType = step.dotnetVarType,

            };

            builtReg = bInfo.Build();

            BuilderStepBase.AddToRegisterList(step, builtReg);

            return builtReg;

        }

        public static IRegister Build<T>(this BuilderStep<T> step) {

            //fallbacks if no casting builder was given
            BuilderStepBase.GetFallbackDotnetType(step);

            BaseRegister builtReg;

            var bInfo = new RegisterBuildInfo {

                name = step.Name,
                specialAddress = step.SpecialAddress,
                memoryAddress = step.MemAddress,
                memorySizeBytes = step.MemByteSize,
                memorySizeBits = step.MemBitSize,
                registerType = step.RegType,
                dotnetCastType = step.dotnetVarType,

            };

            if (step.dotnetVarType.IsEnum) {

                builtReg = bInfo.Build();

            } else {

                builtReg = bInfo.Build();

            }

            BuilderStepBase.AddToRegisterList(step, builtReg);

            return builtReg;

        }

    }

    public abstract class BuilderStepBase {

        internal MewtocolInterface forInterface;

        internal bool wasCasted = false;

        internal string OriginalInput;

        internal string Name;
        internal RegisterType RegType;

        internal uint MemAddress;

        internal uint? MemByteSize;
        internal ushort? MemBitSize;
        internal byte? SpecialAddress;

        internal PlcVarType? plcVarType;
        internal Type dotnetVarType;

        internal BuilderStepBase AutoType() {

            switch (RegType) {
                case RegisterType.X:
                case RegisterType.Y:
                case RegisterType.R:
                dotnetVarType = typeof(bool);
                break;
                case RegisterType.DT:
                dotnetVarType = typeof(short);
                break;
                case RegisterType.DDT:
                dotnetVarType = typeof(int);
                break;
                case RegisterType.DT_BYTE_RANGE:
                dotnetVarType = typeof(string);
                break;
            }

            plcVarType = null;

            wasCasted = true;

            return this;

        }

        internal static void GetFallbackDotnetType (BuilderStepBase step) {

            bool isBoolean = step.RegType.IsBoolean();
            bool isTypeNotDefined = step.plcVarType == null && step.dotnetVarType == null;

            if (isTypeNotDefined && step.RegType == RegisterType.DT) {

                step.dotnetVarType = typeof(short);

            }
            if (isTypeNotDefined && step.RegType == RegisterType.DDT) {

                step.dotnetVarType = typeof(int);

            } else if (isTypeNotDefined && isBoolean) {

                step.dotnetVarType = typeof(bool);

            } else if (isTypeNotDefined && step.RegType == RegisterType.DT_BYTE_RANGE) {

                step.dotnetVarType = typeof(string);

            }

            if (step.plcVarType != null) {

                step.dotnetVarType = step.plcVarType.Value.GetDefaultDotnetType();

            }

        }

        internal static void AddToRegisterList(BuilderStepBase step, BaseRegister instance) {

            if (step.forInterface == null) return;

            step.forInterface.AddRegister(instance);

        }

    }

    public class BuilderStep<T> : BuilderStepBase { }

    public class BuilderStep : BuilderStepBase {

        public BuilderStep AsPlcType (PlcVarType varType) {

            dotnetVarType = null;
            plcVarType = varType;

            wasCasted = true;

            return this;

        }

        public BuilderStep AsBytes (uint byteLength) {

            if (RegType != RegisterType.DT) {

                throw new NotSupportedException($"Cant use the {nameof(AsBytes)} converter on a non {nameof(RegisterType.DT)} register");

            }

            MemByteSize = byteLength;
            dotnetVarType = typeof(byte[]);
            plcVarType = null;

            wasCasted = true;

            return this;

        }

        public BuilderStep AsBits(ushort bitCount = 16) {

            if (RegType != RegisterType.DT) {

                throw new NotSupportedException($"Cant use the {nameof(AsBits)} converter on a non {nameof(RegisterType.DT)} register");

            }

            MemBitSize = bitCount;
            dotnetVarType = typeof(BitArray);
            plcVarType = null;

            wasCasted = true;

            return this;

        }

    }

}
