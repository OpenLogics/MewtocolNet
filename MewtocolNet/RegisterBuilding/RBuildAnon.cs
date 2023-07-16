using MewtocolNet.Registers;
using System.Threading.Tasks;

namespace MewtocolNet.RegisterBuilding {

    /// <summary>
    /// Anonymous register builder
    /// </summary>
    public class RBuildAnon : RBuildBase {

        public RBuildAnon(MewtocolInterface plc) : base(plc) { }

        /// <inheritdoc cref="RBuildMult.Address(string, string)"/>
        public SAddress Address(string plcAddrName) {

            return new SAddress {
                attachedPlc = this.attachedPLC,
                addrString = plcAddrName
            };

        }

        public new class SAddress {

            protected internal MewtocolInterface attachedPlc;
            protected internal string addrString;
            protected internal string name;

            /// <summary>
            /// Writes data to the register and bypasses the memory manager <br/>
            /// </summary>
            /// <param name="value">The value to write</param>
            /// <returns>True if success</returns>
            public async Task<bool> WriteToAsync<T>(T value) {

                try {

                    var tempRegister = AssembleTemporaryRegister<T>();
                    return await tempRegister.WriteAsync(value);

                } catch {

                    throw;

                }

            }

            /// <summary>
            /// Reads data from the register and bypasses the memory manager <br/>
            /// </summary>
            /// <returns>The value read or null if failed</returns>
            public async Task<T> ReadFromAsync<T>() {

                try {

                    var tempRegister = AssembleTemporaryRegister<T>();
                    return (T)await tempRegister.ReadAsync();

                } catch {

                    throw;

                }

            }

            private Register AssembleTemporaryRegister<T>() {

                var temp = new RBuildMult(attachedPlc).Address(addrString).AsType<T>();

                var assembler = new RegisterAssembler(attachedPlc);
                return assembler.Assemble(temp.Data);

            }

        }

    }

    public class RBuildSingle : RBuildBase {

        public RBuildSingle(MewtocolInterface plc) : base(plc) { }

        /// <inheritdoc cref="RBuildMult.Address(string, string)"/>
        public SAddress Address(string plcAddrName, string name = null) {

            var data = ParseAddress(plcAddrName, name);

            return new SAddress {
                Data = data,
                builder = this,
            };

        }

    }

}
