using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MewtocolNet.RegisterBuilding.BuilderPatterns {

    public class RBuildMulti : RBuild {
    
        internal RBuildMulti(MewtocolInterface plc) : base(plc) {}

        //bool constructor
        public new MultStructStp<bool> Bool(string fpAddr, string name = null) => new MultStructStp<bool>().Map(base.Bool(fpAddr, name));

        //struct constructor 
        public new MultStructStp<T> Struct<T>(string fpAddr, string name = null) where T : struct => new MultStructStp<T>().Map(base.Struct<T>(fpAddr, name));

        //string constructor
        public new MultStringStp<string> String(string fpAddr, int sizeHint, string name = null) => new MultStringStp<string>().Map(base.String(fpAddr, sizeHint, name));

        public class MultStructStp<T> : MultArrayStp<T> where T : struct {

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

        public class MultStringStp<T> : MultArrayStp<T> where T : class {

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

        public class MultArrayStp<T> : ArrayStp<T> {

            public new MultTypedArr1D<T> AsArray(int i) => new MultTypedArr1D<T>().Map(base.AsArray(i));

            public new MultTypedArr2D<T> AsArray(int i1, int i2) => new MultTypedArr2D<T>().Map(base.AsArray(i1, i2));

            public new MultTypedArr3D<T> AsArray(int i1, int i2, int i3) => new MultTypedArr3D<T>().Map(base.AsArray(i1, i2, i3));

        }

        //1D array

        public class MultTypedArr1D<T> : TypedArr1D<T> {

            public void Build() => builder.Assemble(this);

            public void Build(out IArrayRegister<T> reference) => reference = (IArrayRegister<T>)builder.Assemble(this);

            public MultTypedArr1DOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new MultTypedArr1DOut<T>().Map(this);

            }

        }

        public class MultTypedArr1DOut<T> : TypedArr1DOut<T> {

            public void Build() => builder.Assemble(this);

            public void Build(out IArrayRegister<T> reference) => reference = (IArrayRegister<T>)builder.Assemble(this);

        }

        //2D array

        public class MultTypedArr2D<T> : TypedArr2D<T> {

            public void Build() => builder.Assemble(this);

            public void Build(out IArrayRegister2D<T> reference) => reference = (IArrayRegister2D<T>)builder.Assemble(this);

            public MultTypedArr2DOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new MultTypedArr2DOut<T>().Map(this);

            }

        }

        public class MultTypedArr2DOut<T> : TypedArr2DOut<T> {

            public void Build() => builder.Assemble(this);

            public void Build(out IArrayRegister2D<T> reference) => reference = (IArrayRegister2D<T>)builder.Assemble(this);

        }

        //3D array

        public class MultTypedArr3D<T> : TypedArr3D<T> {

            public void Build() => builder.Assemble(this);

            public void Build(out IArrayRegister3D<T> reference) => reference = (IArrayRegister3D<T>)builder.Assemble(this);

            public MultTypedArr3DOut<T> PollLevel(int level) {

                Data.pollLevel = level;
                return new MultTypedArr3DOut<T>().Map(this);

            }

        }

        public class MultTypedArr3DOut<T> : TypedArr3DOut<T> {

            public void Build() => builder.Assemble(this);

            public void Build(out IArrayRegister3D<T> reference) => reference = (IArrayRegister3D<T>)builder.Assemble(this);

        }

    }

}
