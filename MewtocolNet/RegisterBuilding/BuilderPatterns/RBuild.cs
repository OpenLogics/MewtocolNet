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

        //bool constructor

        internal StructStp<bool> Bool(string fpAddr, string name = null) {

            var data = AddressTools.ParseAddress(fpAddr, name);

            if (!data.regType.IsBoolean())
                throw new NotSupportedException($"The address '{fpAddr}' was no boolean FP address");

            data.dotnetVarType = typeof(bool);

            return new StructStp<bool>(data) {
                builder = this,
            };

        }

        //struct constructor 

        internal StructStp<T> Struct<T>(string fpAddr, string name = null) where T : struct {

            var data = AddressTools.ParseAddress(fpAddr, name);

            if (data.regType.IsBoolean())
                throw new NotSupportedException($"The address '{fpAddr}' was no DT address");

            data.dotnetVarType = typeof(T);

            return new StructStp<T>(data) {
                builder = this,
            };

        }

        //string constructor

        internal StringStp<string> String(string fpAddr, int sizeHint, string name = null) {

            var data = AddressTools.ParseAddress(fpAddr, name);

            if (data.regType.IsBoolean())
                throw new NotSupportedException($"The address '{fpAddr}' was no string address");

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

            internal StructStp() {}

            internal StructStp(StepData data) {

                this.Data = data;
                this.Map(StepBaseTyper.AsType(this, typeof(T)));

            }

        }

        //strings can lead to arrays
        public class StringStp<T> : ArrayStp<T> where T : class {

            internal StringStp() { }

            internal StringStp(StepData data) {

                this.Data = data;
                this.Map(StepBaseTyper.AsType(this, typeof(T)));

            }

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

        public class TypedArr1D<T> : TypedArr1DOut<T> { }

        public class TypedArr1DOut<T> : SBaseRB { }

        //2D array

        public class TypedArr2D<T> : TypedArr2DOut<T> { }

        public class TypedArr2DOut<T> : SBaseRB { }

        //3D array

        public class TypedArr3D<T> : SBaseRB { }

        public class TypedArr3DOut<T> : SBaseRB { }

        #endregion

    }

}
