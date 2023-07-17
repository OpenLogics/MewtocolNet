using MewtocolNet.Registers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MewtocolNet.RegisterBuilding {

    public static class RegBuilderExtensions {

        /// <summary>
        /// Adds a single register to the plc stack and returns the generated <see cref="IRegister"/><br/>
        /// This waits for the memory manager to size all dynamic registers correctly
        /// </summary>
        /// <returns>The generated <see cref="IRegister"/></returns>
        public static IRegister AddRegister(this IPlc plc, Action<RBuildSingle> builder) {

            var assembler = new RegisterAssembler((MewtocolInterface)plc);
            var regBuilder = new RBuildSingle((MewtocolInterface)plc);

            builder.Invoke(regBuilder);

            var registers = assembler.AssembleAll(regBuilder);

            var interf = (MewtocolInterface)plc;

            interf.AddRegisters(registers.ToArray());

            return registers.First();

        }

        /// <summary>
        /// Adds multiple registers to the plc stack at once <br/>
        /// Using this over adding each register individually will result in better generation time performance
        /// of the <see cref="UnderlyingRegisters.MemoryAreaManager"/> <br/><br/>
        /// <b>WARNING!</b> This will not wait for the memory manager to account for dynamically sized registers 
        /// like ones with the <see cref="string"/> type.. <br/>
        /// use <see cref="AddRegistersAsync"/>
        /// for this case
        /// </summary>
        public static IPlc AddRegisters (this IPlc plc, Action<RBuildMult> builder) {

            var assembler = new RegisterAssembler((MewtocolInterface)plc);
            var regBuilder = new RBuildMult((MewtocolInterface)plc);

            builder.Invoke(regBuilder);

            var registers = assembler.AssembleAll(regBuilder);

            var interf = (MewtocolInterface)plc;

            interf.AddRegisters(registers.ToArray());

            return plc;

        }

    }

}
