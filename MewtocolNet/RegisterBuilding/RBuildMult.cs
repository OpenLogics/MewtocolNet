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
        internal SAddress AddressFromAttribute(string plcAddrName, string typeDef, RegisterCollection regCol, PropertyInfo prop, uint? bytesizeHint = null) {

            var built = Address(plcAddrName);
            
            built.Data.typeDef = typeDef;
            built.Data.buildSource = RegisterBuildSource.Attribute;
            built.Data.regCollection = regCol;
            built.Data.boundProperty = prop;
            built.Data.byteSizeHint = bytesizeHint; 

            return built;

        }

        #endregion

        #region Typing stage

        public new class SAddress : RBuildBase.SAddress {

            public new TypedRegister AsType<T>() => new TypedRegister().Map(base.AsType<T>());

            public new TypedRegister AsType(Type type) => new TypedRegister().Map(base.AsType(type));

            public new TypedRegister AsType(PlcVarType type) => new TypedRegister().Map(base.AsType(type));

            public new TypedRegister AsType(string type) => new TypedRegister().Map(base.AsType(type));

            public new TypedRegister AsTypeArray<T>(params int[] indicies) => new TypedRegister().Map(base.AsTypeArray<T>(indicies));


        }

        #endregion

        #region Typing size hint

        public new class TypedRegister : RBuildBase.TypedRegister {

            public new OptionsRegister SizeHint(int hint) => new OptionsRegister().Map(base.SizeHint(hint));

            ///<inheritdoc cref="RBuildBase.OptionsRegister.PollLevel(int)"/>
            public new OutRegister PollLevel(int level) => new OutRegister().Map(base.PollLevel(level));

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public void Out(Action<IRegister> registerOut) {

                Data.registerOut = registerOut;

            }

        }

        #endregion

        #region Options stage

        public new class OptionsRegister : RBuildBase.OptionsRegister {

            ///<inheritdoc cref="RBuildBase.OptionsRegister.PollLevel(int)"/>
            public new OutRegister PollLevel(int level) => new OutRegister().Map(base.PollLevel(level));

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public void Out(Action<IRegister> registerOut) {

                Data.registerOut = registerOut;

            }

        }

        #endregion

        public class OutRegister : SBase {

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public void Out(Action<IRegister> registerOut) {

                Data.registerOut = registerOut;

            }

        }

    }

}
