using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MewtocolNet.RegisterBuilding.BuilderPatterns {

    /// <summary>
    /// An anonymous register build interface
    /// </summary>
    public class RBuildAnon : RBuild {

        internal RBuildAnon(MewtocolInterface plc) : base(plc) { }

        public new MultStructStp<bool> Bool(string fpAddr) => new MultStructStp<bool>().Map(base.Bool(fpAddr));

        public new MultStructStp<T> Struct<T>(string fpAddr) where T : struct => new MultStructStp<T>().Map(base.Struct<T>(fpAddr));

        public new MultStringStp<string> String(string fpAddr, int sizeHint) => new MultStringStp<string>().Map(base.String(fpAddr, sizeHint));

        public class MultStructStp<T> : MultArrayStp<T> where T : struct {

            public async Task WriteAsync(T value) {

                var reg = (IRegister<T>)builder.Assemble(this);
                await reg.WriteAsync(value);

            }

            public async Task<T> ReadAsync() {

                var reg = (IRegister<T>)builder.Assemble(this);
                return await reg.ReadAsync();

            }

        }

        public class MultStringStp<T> : MultArrayStp<T> where T : class {

            public async Task WriteAsync(string value) {

                var reg = (IStringRegister)builder.Assemble(this);
                await reg.WriteAsync(value);

            }

            public async Task<string> ReadAsync() {

                var reg = (IStringRegister)builder.Assemble(this);
                return await reg.ReadAsync();

            }

        }

        public class MultArrayStp<T> : ArrayStp<T> {

            public new MultTypedArr1D<T> AsArray(int i) => new MultTypedArr1D<T>().Map(base.AsArray(i));

            public new MultTypedArr2D<T> AsArray(int i1, int i2) => new MultTypedArr2D<T>().Map(base.AsArray(i1, i2));

            public new MultTypedArr3D<T> AsArray(int i1, int i2, int i3) => new MultTypedArr3D<T>().Map(base.AsArray(i1, i2, i3));

        }

        //1D array

        public class MultTypedArr1D<T> : TypedArr1D<T> {

            public async Task WriteAsync(T[] value) {

                var reg = (IArrayRegister<T>)builder.Assemble(this);
                await reg.WriteAsync(value);

            }

            public async Task<T[]> ReadAsync() {

                var reg = (IArrayRegister<T>)builder.Assemble(this);
                return await reg.ReadAsync();

            }

        }

        //2D array

        public class MultTypedArr2D<T> : TypedArr2D<T> {

            public async Task WriteAsync(T[,] value) {

                var reg = (IArrayRegister2D<T>)builder.Assemble(this);
                await reg.WriteAsync(value);

            }

            public async Task<T[,]> ReadAsync() {

                var reg = (IArrayRegister2D<T>)builder.Assemble(this);
                return await reg.ReadAsync();

            }

        }

        //3D array

        public class MultTypedArr3D<T> : TypedArr3D<T> {

            public async Task WriteAsync(T[,,] value) {

                var reg = (IArrayRegister3D<T>)builder.Assemble(this);
                await reg.WriteAsync(value);

            }

            public async Task<T[,,]> ReadAsync() {

                var reg = (IArrayRegister3D<T>)builder.Assemble(this);
                return await reg.ReadAsync();

            }

        }

    }

}
