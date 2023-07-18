using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Reflection;
using static MewtocolNet.RegisterBuilding.RBuildMult;

namespace MewtocolNet.RegisterBuilding {

    /// <summary>
    /// Contains useful tools for bunch register creation
    /// </summary>
    public class RBuildMult : RBuildBase {

        public RBuildMult(MewtocolInterface plc) : base(plc) { }

        #region String parse stage

        //at runtime constructor

        /// <summary>
        /// Starts the register builder for a new mewtocol address <br/>
        /// Examples:
        /// <code>Address("DT100") | Address("R10A") | Address("DDT50", "MyRegisterName")</code>
        /// </summary>
        /// <param name="dtAddr">Address name formatted as FP-Address like in FP-Winpro</param>
        /// <param name="name">Custom name for the register to referr to it later</param>
        public AddressStp Address(string dtAddr, string name = null) {

            var data = ParseAddress(dtAddr, name);

            unfinishedList.Add(data);

            return new AddressStp {
                Data = data,
                builder = this,
            };

        }

        //struct constructor

        public StructStp<T> Struct<T>(string dtAddr, string name = null) where T : struct {

            var data = ParseAddress(dtAddr, name);

            data.dotnetVarType = typeof(T);

            unfinishedList.Add(data);

            return new StructStp<T>(data) {
                builder = this,
            };

        }

        public StringStp<T> String<T>(string dtAddr, int sizeHint, string name = null) where T : class {

            var data = ParseAddress(dtAddr, name);

            data.dotnetVarType = typeof(T);
            data.byteSizeHint = (uint)sizeHint;

            unfinishedList.Add(data);

            if (typeof(T).IsArray) {

                return new StringStp<T>(data, true) {
                    Data = data,
                    builder = this,
                };

            }

            return new StringStp<T>(data) {
                builder = this,
            };

        }

        public ArrayStp<T> Array<T>(string dtAddr, string name = null) where T : class {

            var data = ParseAddress(dtAddr, name);

            data.dotnetVarType = typeof(T);

            unfinishedList.Add(data);

            if (typeof(T).IsArray) {

                return new ArrayStp<T>(data, true) {
                    Data = data,
                    builder = this,
                };

            }

            return new ArrayStp<T>(data) {
                builder = this,
            };

        }

        //internal use only, adds a type definition (for use when building from attibute)
        internal AddressStp AddressFromAttribute(string dtAddr, string typeDef, RegisterCollection regCol, PropertyInfo prop, uint? bytesizeHint = null) {

            var built = Address(dtAddr);
            
            built.Data.typeDef = typeDef;
            built.Data.buildSource = RegisterBuildSource.Attribute;
            built.Data.regCollection = regCol;
            built.Data.boundProperty = prop;
            built.Data.byteSizeHint = bytesizeHint; 

            return built;

        }

        #endregion

        #region Typing stage

        //non generic
        public new class AddressStp : RBuildBase.SAddress {

            public new TypedRegister AsType<T>() => new TypedRegister().Map(base.AsType<T>());

            public new TypedRegister AsType(Type type) => new TypedRegister().Map(base.AsType(type));

            public new TypedRegister AsType(PlcVarType type) => new TypedRegister().Map(base.AsType(type));

            public new TypedRegister AsType(string type) => new TypedRegister().Map(base.AsType(type));

            public new TypedRegister AsTypeArray<T>(params int[] indicies) => new TypedRegister().Map(base.AsTypeArray<T>(indicies));

        }

        //structs
        public class StructStp<T> : RBuildBase.SAddress where T : struct {

            internal StructStp(StepData data) {

                this.Data = data;    

                this.Map(AsType(typeof(T)));

            }

            internal StructStp(StepData data, bool arrayed) {

                this.Data = data;

            }

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public void Out(Action<IRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IRegister<T>)o));

            }

            public OutStruct<T> PollLevel(int level) {

                Data.pollLevel = level;

                return new OutStruct<T>().Map(this);

            }

        }

        //arrays
        public class ArrayStp<T> : RBuildBase.SAddress {

            internal ArrayStp(StepData data) {

                Data = data;

                this.Map(AsType(typeof(T)));

            }

            internal ArrayStp(StepData data, bool arrayed) {

                Data = data;

            }

            public TypedRegisterArray<T> Indices(params int[] indices) {

                if (typeof(T).GetElementType() == typeof(string) && Data.byteSizeHint == null) {

                    throw new NotSupportedException($"For string arrays use {nameof(ArrayStp<T>.StrHint)} before setting the indices");

                }

                Data.arrayIndicies = indices;

                return new TypedRegisterArray<T>().Map(this);

            } 

            public TypedRegisterStringArray<T> StrHint(int hint) {

                Data.byteSizeHint = (uint)hint;
                return new TypedRegisterStringArray<T>().Map(this);

            }

        }

        //strings
        public class StringStp<T> : RBuildBase.SAddress where T : class {

            internal StringStp(StepData data) {

                this.Data = data;

                this.Map(AsType(typeof(T)));

            }

            internal StringStp(StepData data, bool arrayed) {

                this.Data = data;

            }

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public void Out(Action<IStringRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IStringRegister<T>)o));

            }

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

                Data.registerOut = new Action<object>(o => registerOut((IRegister)o));

            }

        }

        public class TypedRegisterString<T> : RBuildBase.TypedRegister where T : class {

            public new OptionsRegister SizeHint(int hint) => new OptionsRegister().Map(base.SizeHint(hint));

            ///<inheritdoc cref="RBuildBase.OptionsRegister.PollLevel(int)"/>
            public new OutRegister PollLevel(int level) => new OutRegister().Map(base.PollLevel(level));

            /// <summary>
            /// Outputs the generated <see cref="IRegister"/>
            /// </summary>
            public void Out(Action<IStringRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IStringRegister<T>)o));

            }

        }

        public class TypedRegisterArray<T> : RBuildBase.TypedRegister {

            public new OutArray<T> PollLevel(int level) => new OutArray<T>().Map(base.PollLevel(level));

            public void Out(Action<IArrayRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IArrayRegister<T>)o));

            }

        }

        public class TypedRegisterStringArray<T> : RBuildBase.TypedRegister {

            public OptionsRegisterArray<T> Indices(params int[] indices) {

                Data.arrayIndicies = indices;
                return new OptionsRegisterArray<T>().Map(this);

            }

            public new OutArray<T> PollLevel(int level) => new OutArray<T>().Map(base.PollLevel(level));

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

                Data.registerOut = new Action<object>(o => registerOut((IRegister)o));

            }

        }

        public class OptionsRegisterArray<T> : RBuildBase.OptionsRegister {

            ///<inheritdoc cref="RBuildBase.OptionsRegister.PollLevel(int)"/>
            public new OutArray<T> PollLevel(int level) => new OutArray<T>().Map(base.PollLevel(level));

            public void Out(Action<IArrayRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IArrayRegister<T>)o));

            }

        }

        #endregion

        public class OutRegister : SBase {

            public void Out(Action<IRegister> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IRegister)o));

            }

        }

        public class OutStruct<T> : SBase where T : struct {

            public void Out(Action<IRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IRegister<T>)o));

            }

        }

        public class OutArray<T> : SBase {

            public void Out(Action<IArrayRegister<T>> registerOut) {

                Data.registerOut = new Action<object>(o => registerOut((IArrayRegister<T>)o));

            }

        }


    }

}
