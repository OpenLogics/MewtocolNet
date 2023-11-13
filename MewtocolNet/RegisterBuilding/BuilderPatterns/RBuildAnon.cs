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

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                await ((IRegister<T>)reg).WriteAsync(value);

            }

            public async Task<T> ReadAsync() {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                return await ((IRegister<T>)reg).ReadAsync();

            }

        }

        public class MultStringStp<T> : MultArrayStp<T> where T : class {

            public async Task WriteAsync(string value) {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                await ((IStringRegister)reg).WriteAsync(value);

            }

            public async Task<string> ReadAsync() {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                return await ((IStringRegister)reg).ReadAsync();

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

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                var tReg = (IArrayRegister<T>)reg;
                await tReg.WriteAsync(value);

            }

            public async Task<T[]> ReadAsync() {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                var tReg = (IArrayRegister<T>)reg;
                return await tReg.ReadAsync();

            }

        }

        //2D array

        public class MultTypedArr2D<T> : TypedArr2D<T> {

            public async Task WriteAsync(T[,] value) {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                await ((IArrayRegister2D<T>)reg).WriteAsync(value);

            }

            public async Task<T[,]> ReadAsync() {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                return await ((IArrayRegister2D<T>)reg).ReadAsync();

            }

        }

        //3D array

        public class MultTypedArr3D<T> : TypedArr3D<T> {

            public async Task WriteAsync(T[,,] value) {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                await ((IArrayRegister3D<T>)reg).WriteAsync(value);

            }

            public async Task<T[,,]> ReadAsync() {

                var reg = builder.Assemble(this);
                reg.isAnonymous = true;
                return await ((IArrayRegister3D<T>)reg).ReadAsync();

            }

        }

    }

}
