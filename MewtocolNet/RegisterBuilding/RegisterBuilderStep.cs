using System;

namespace MewtocolNet.RegisterBuilding {
    public class RegisterBuilderStep {

        internal bool wasCasted = false;

        internal string OriginalInput;

        internal string Name;
        internal RegisterType RegType;
        internal int MemAddress;
        internal byte? SpecialAddress;

        internal PlcVarType? plcVarType;
        internal Type dotnetVarType;

        public RegisterBuilderStep () => throw new NotSupportedException("Cant make a new instance of RegisterBuilderStep, use the builder pattern");
        
        internal RegisterBuilderStep (RegisterType regType, int memAddr) { 
        
            RegType = regType;  
            MemAddress = memAddr;        

        }

        internal RegisterBuilderStep(RegisterType regType, int memAddr, byte specialAddr) {

            RegType = regType;
            MemAddress = memAddr;
            SpecialAddress = specialAddr;   

        }

        public RegisterBuilderStep AsPlcType (PlcVarType varType) {

            dotnetVarType = null;
            plcVarType = varType;

            wasCasted = true;

            return this;

        }

        public RegisterBuilderStep AsType<T> () {

            if(!typeof(T).IsAllowedPlcCastingType()) {

                throw new NotSupportedException($"The dotnet type {typeof(T)}, is not supported for PLC type casting");

            }

            dotnetVarType = typeof(T);
            plcVarType = null;

            wasCasted = true;

            return this;

        }

        internal RegisterBuilderStep AutoType () {

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
                case RegisterType.DT_START:
                dotnetVarType = typeof(string);
                break;
            }

            plcVarType = null;

            wasCasted = true;

            return this;

        }

    }

}
