using CommandLine;
using MewtocolNet;
using MewtocolNet.RegisterBuilding;
using MewtocolNet.Registers;
using Spectre.Console;

namespace MewTerminal.Commands.OnlineCommands;

[Verb("rset", HelpText = "Sets the values of the given PLC registers")]
internal class SetRegisterCommand : OnlineCommand {

    [Value(0, MetaName = "registers", Default = "DT0", HelpText = "The registers to write formatted as <mewtocol_name:plc_type> (DT0:INT:VALUE)")]
    public IEnumerable<string> Registers { get; set; }

    internal override async Task AfterSetup(IPlc plc) {

        var builder = RegBuilder.ForInterface(plc);

        var toWriteVals = new List<object>();   

        foreach (var reg in Registers) {

            var split = reg.Split(":");

            if (split.Length <= 2) {
                throw new FormatException($"Register name was not formatted correctly: {reg}, missing :PlcVarType:Value");
            }

            var mewtocolName = split[0];
            var mewtocolType = split[1];
            var value = split[2];

            if (Enum.TryParse<PlcVarType>(mewtocolType, out var parsedT)) {

                var built = builder.FromPlcRegName(mewtocolName).AsPlcType(parsedT).Build();

                if(built is BoolRegister) toWriteVals.Add(bool.Parse(value));
                else if(built is NumberRegister<short>) toWriteVals.Add(short.Parse(value));
                else if(built is NumberRegister<ushort>) toWriteVals.Add(ushort.Parse(value));
                else if(built is NumberRegister<int>) toWriteVals.Add(int.Parse(value));
                else if(built is NumberRegister<uint>) toWriteVals.Add(uint.Parse(value));
                else if(built is NumberRegister<float>) toWriteVals.Add(float.Parse(value));
                else if(built is NumberRegister<TimeSpan>) toWriteVals.Add(TimeSpan.Parse(value));

            }

        }

        await plc.ConnectAsync();

        int i = 0;
        foreach (var reg in plc.GetAllRegisters()) {

            await reg.WriteAsync(toWriteVals[i]);

            i++;    

        }

        AnsiConsole.WriteLine("All registers written");

    }

}