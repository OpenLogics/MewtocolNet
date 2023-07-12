using System;

namespace MewtocolNet.RegisterBuilding {
    public static class RegBuilderExtensions {

        public static IPlc AddTrackedRegisters(this IPlc plc, Action<RBuild> builder) {

            if (plc.IsConnected)
                throw new Exception("Can't add registers if the PLC is connected");

            var regBuilder = new RBuild();
            builder.Invoke(regBuilder);

            var assembler = new RegisterAssembler((MewtocolInterface)plc);
            var registers = assembler.Assemble(regBuilder);

            var interf = (MewtocolInterface)plc;

            interf.AddRegisters(registers.ToArray());

            return plc;

        }


    }

}
