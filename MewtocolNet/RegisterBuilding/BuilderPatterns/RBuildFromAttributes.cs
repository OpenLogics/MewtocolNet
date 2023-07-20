using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MewtocolNet.RegisterBuilding.BuilderPatterns {

    internal class RBuildFromAttributes {

        internal RegisterAssembler assembler;

        public RBuildFromAttributes(MewtocolInterface plc) {

            assembler = new RegisterAssembler(plc);

        }

        public class SBaseRBDyn : StepBase {

            internal RBuildFromAttributes builder;

        }

        //internal use only, adds a type definition (for use when building from attibute)
        internal DynamicStp AddressFromAttribute(string dtAddr, string typeDef, RegisterCollection regCol, PropertyInfo prop, uint? bytesizeHint = null) {

            var stpData = AddressTools.ParseAddress(dtAddr);

            stpData.typeDef = typeDef;
            stpData.buildSource = RegisterBuildSource.Attribute;
            stpData.regCollection = regCol;
            stpData.boundProperty = prop;
            stpData.byteSizeHint = bytesizeHint;

            return new DynamicStp {
                builder = this,
                Data = stpData, 
            };

        }

        //non generic
        internal class DynamicStp : SBaseRBDyn {

            public DynamicRegister AsType<T>() => new DynamicRegister().Map(StepBaseTyper.AsType<T>(this));

            public DynamicRegister AsType(Type type) => new DynamicRegister().Map(StepBaseTyper.AsType(this, type));

            public DynamicRegister AsType(PlcVarType type) => new DynamicRegister().Map(StepBaseTyper.AsType(this, type));

            public DynamicRegister AsType(string type) => new DynamicRegister().Map(StepBaseTyper.AsType(this, type));

            public DynamicRegister AsTypeArray<T>(params int[] indicies) => new DynamicRegister().Map(StepBaseTyper.AsTypeArray<T>(this, indicies));

        }

        internal class DynamicRegister : SBaseRBDyn { 
            
            public void PollLevel (int level) {

                Data.pollLevel = level;

            }
        
        }

    }
}
