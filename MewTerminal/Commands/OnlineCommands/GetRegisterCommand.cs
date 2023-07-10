using System;
using System.Collections.Generic;
using CommandLine;
using MewtocolNet;
using MewtocolNet.ComCassette;
using MewtocolNet.Logging;
using MewtocolNet.RegisterBuilding;
using Spectre.Console;

namespace MewTerminal.Commands.OnlineCommands;

[Verb("rget", HelpText = "Gets the values of the given PLC registers")]
internal class GetRegisterCommand : OnlineCommand {

    [Value(0, MetaName = "registers", Default = "DT0", HelpText = "The registers to read formatted as <mewtocol_name:plc_type> (DT0:INT)")]
    public IEnumerable<string> Registers { get; set; }

    internal override async Task AfterSetup(IPlc plc) {

        var builder = RegBuilder.ForInterface(plc);

        foreach (var reg in Registers) {

            var split = reg.Split(":");

            if (split.Length <= 1) {
                throw new FormatException($"Register name was not formatted correctly: {reg}, missing :PlcVarType");
            }

            var mewtocolName = split[0];
            var mewtocolType = split[1];

            if (Enum.TryParse<PlcVarType>(mewtocolType, out var parsedT)) {

                builder.FromPlcRegName(mewtocolName).AsPlcType(parsedT).Build();

            }

        }

        await plc.ConnectAsync();

        foreach (var reg in plc.GetAllRegisters()) {

            await reg.ReadAsync();

        }

        AnsiConsole.Write(plc.GetAllRegisters().ToTable());

    }

}
