using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Reflection;

namespace MewtocolNet.RegisterBuilding {

    /// <summary>
    /// Contains useful tools for bunch register creation
    /// </summary>
    public class RBuildMult : RBuildBase {

        public RBuildMult(MewtocolInterface plc) : base(plc) { }

        #region String parse stage

        /// <summary>
        /// Starts the register builder for a new mewtocol address <br/>
        /// Examples:
        /// <code>Address("DT100") | Address("R10A") | Address("DDT50", "MyRegisterName")</code>
        /// </summary>
        /// <param name="plcAddrName">Address name formatted as FP-Address like in FP-Winpro</param>
        /// <param name="name">Custom name for the register to referr to it later</param>
        public SAddress Address(string plcAddrName, string name = null) {

            var data = ParseAddress(plcAddrName, name);

            return new SAddress {
                Data = data,
                builder = this,
            };

        }

        //internal use only, adds a type definition (for use when building from attibute)
        internal SAddress AddressFromAttribute(string plcAddrName, string typeDef) {

            var built = Address(plcAddrName);
            built.Data.typeDef = typeDef;
            built.Data.buildSource = RegisterBuildSource.Attribute;
            return built;

        }

        #endregion

        #region Typing stage

        public new class SAddress : RBuildBase.SAddress {

            public new TempRegister<T> AsType<T>(int? sizeHint = null) => new TempRegister<T>().Map(base.AsType<T>(sizeHint));

            public new TempRegister AsType(Type type) => new TempRegister().Map(base.AsType(type));

            public new TempRegister AsType(PlcVarType type) => new TempRegister().Map(base.AsType(type));

            public new TempRegister AsType(string type) => new TempRegister().Map(base.AsType(type));

            public new TempRegister AsTypeArray<T>(params int[] indicies) => new TempRegister().Map(base.AsTypeArray<T>(indicies));

        }

        #endregion

        #region Options stage

        public new class TempRegister<T> : RBuildBase.TempRegister<T> {

            internal TempRegister() { }

            internal TempRegister(StepData data, RBuildBase bldr) : base(data, bldr) { }

            ///<inheritdoc cref="RBuildBase.TempRegister.PollLevel(int)"/>
            public new TempRegister<T> PollLevel(int level) => new TempRegister<T>().Map(base.PollLevel(level));

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public TempRegister<T> Out(Action<IRegister> registerOut) {

                Data.registerOut = registerOut;
                return this;

            }

        }

        public new class TempRegister : RBuildBase.TempRegister {

            internal TempRegister() { }

            internal TempRegister(StepData data, RBuildBase bldr) : base(data, bldr) { }

            ///<inheritdoc cref="RBuildBase.TempRegister.PollLevel(int)"/>
            public new TempRegister PollLevel(int level) => new TempRegister().Map(base.PollLevel(level));

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public TempRegister Out(Action<IRegister> registerOut) {

                Data.registerOut = registerOut;
                return this;

            }

            //internal use only
            internal TempRegister RegCollection(RegisterCollection col) {

                Data.regCollection = col;
                return this;

            }

            internal TempRegister BoundProp(PropertyInfo prop) {

                Data.boundProperty = prop;
                return this;

            }

        }

        #endregion

    }

}
