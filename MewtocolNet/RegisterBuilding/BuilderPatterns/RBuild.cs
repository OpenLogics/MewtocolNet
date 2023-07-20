using MewtocolNet;
using MewtocolNet.PublicEnums;
using MewtocolNet.RegisterAttributes;
using MewtocolNet.Registers;
using System;
using System.Linq;
using System.Reflection;
using static MewtocolNet.RegisterBuilding.BuilderPatterns.RBuild;

namespace MewtocolNet.RegisterBuilding.BuilderPatterns {

    /// <summary>
    /// Contains useful tools for bunch register creation
    /// </summary>
    public class RBuild {

        internal RegisterAssembler assembler;

        public RBuild(MewtocolInterface plc) {

            assembler = new RegisterAssembler(plc);

        }

        public class SBaseRB : StepBase {

            internal RBuild builder;

        }

        #region String parse stage

        internal Register Assemble(StepBase stp) => assembler.Assemble(stp.Data);

        //struct constructor

        public StructStp<T> Struct<T>(string fpAddr, string name = null) where T : struct {

            var data = AddressTools.ParseAddress(fpAddr, name);

            data.dotnetVarType = typeof(T);

            return new StructStp<T>(data) {
                builder = this,
            };

        }

        //string constructor

        public StringStp<string> String(string fpAddr, int sizeHint, string name = null) {

            var data = AddressTools.ParseAddress(fpAddr, name);

            data.dotnetVarType = typeof(string);
            data.byteSizeHint = (uint)sizeHint;

            return new StringStp<string>(data) {
                builder = this,
            };

        }

        #endregion

        #region Typing stage

        //structs can lead to arrays
        public class StructStp<T> : ArrayStp<T> where T : struct {

            internal StructStp(StepData data) {

                this.Data = data;
                this.Map(StepBaseTyper.AsType(this, typeof(T)));

            }

            public void Build() => builder.Assemble(this);

            public void Build(out IRegister<T> reference) => reference = (IRegister<T>)builder.Assemble(this);

            public StructStpOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new StructStpOut<T>().Map(this);

            }

        }

        public class StructStpOut<T> : SBaseRB where T : struct {

            public void Build() => builder.Assemble(this);

            public void Build(out IRegister<T> reference) => reference = (IRegister<T>)builder.Assemble(this);

        }

        //strings can lead to arrays
        public class StringStp<T> : ArrayStp<T> where T : class {

            internal StringStp(StepData data) {

                this.Data = data;
                this.Map(StepBaseTyper.AsType(this, typeof(T)));

            }

            public void Build() => builder.Assemble(this);

            public void Build(out IStringRegister reference) => reference = (IStringRegister)builder.Assemble(this);

            public StringOutStp PollLevel(int level) {

                Data.pollLevel = level;
                return new StringOutStp().Map(this);

            }

        }

        public class StringOutStp : SBaseRB {

            public void Build() => builder.Assemble(this);

            public void Build(out IStringRegister reference) => reference = (IStringRegister)builder.Assemble(this);

        }

        //arrays
        public class ArrayStp<T> : SBaseRB {

            public TypedArr1D<T> AsArray(int i) {

                Data.arrayIndicies = new int[] { i };
                SetSizing();
                return new TypedArr1D<T>().Map(this);

            }

            public TypedArr2D<T> AsArray(int i1, int i2) {

                Data.arrayIndicies = new int[] { i1, i2 };
                SetSizing();
                return new TypedArr2D<T>().Map(this);

            }

            public TypedArr3D<T> AsArray(int i1, int i2, int i3) {

                Data.arrayIndicies = new int[] { i1, i2, i3 };
                SetSizing();
                return new TypedArr3D<T>().Map(this);

            }

            private void SetSizing() {

                var arr = Array.CreateInstance(Data.dotnetVarType, Data.arrayIndicies.ToArray());

                Data.dotnetVarType = arr.GetType();

                var itemCount = (uint)Data.arrayIndicies.Aggregate((a, x) => a * x);

                if (typeof(T) == typeof(string)) {

                    var byteSize = Data.byteSizeHint.Value;
                    if (byteSize % 2 != 0) byteSize++;
                    Data.byteSizeHint = itemCount * (byteSize + 4);

                } else {

                    var byteSize = (uint)typeof(T).DetermineTypeByteIntialSize();
                    Data.byteSizeHint = itemCount * byteSize;

                }

            }

        }

        #endregion

        #region Typing size hint

        //1D array

        public class TypedArr1D<T> : TypedArr1DOut<T> {

            public TypedArr1DOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new TypedArr1DOut<T>().Map(this);

            }

        }

        public class TypedArr1DOut<T> : SBaseRB {

            public IArrayRegister<T> Build() => (IArrayRegister<T>)builder.Assemble(this);

            public void Build(out IArrayRegister<T> reference) => reference = (IArrayRegister<T>)builder.Assemble(this);

        }

        //2D array

        public class TypedArr2D<T> : TypedArr2DOut<T> {

            public TypedArr2DOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new TypedArr2DOut<T>().Map(this);

            }

        }

        public class TypedArr2DOut<T> : SBaseRB {

            public IArrayRegister2D<T> Build() => (IArrayRegister2D<T>)builder.Assemble(this);

            public void Build(out IArrayRegister2D<T> reference) => reference = (IArrayRegister2D<T>)builder.Assemble(this);

        }

        //3D array

        public class TypedArr3D<T> : SBaseRB {

            public TypedArr3DOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new TypedArr3DOut<T>().Map(this);

            }

        }

        public class TypedArr3DOut<T> : SBaseRB {

            public IArrayRegister3D<T> Build() => (IArrayRegister3D<T>)builder.Assemble(this);

            public void Build(out IArrayRegister3D<T> reference) => reference = (IArrayRegister3D<T>)builder.Assemble(this);

        }

        #endregion

    }

}
