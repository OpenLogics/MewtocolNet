using System;
using System.Collections.Generic;
using CommandLine;
using MewtocolNet;
using MewtocolNet.ComCassette;
using MewtocolNet.Logging;
using Spectre.Console;

namespace MewTerminal.Commands;

[Verb("support-list", HelpText = "Lists all supported PLC types")]
internal class ListSupportCommand : CommandLineExcecuteable {

    public override void Run () {

        var plcs = Enum.GetValues<PlcType>().Cast<PlcType>();

        var lst = new List<ParsedPlcName>();    

        foreach (var plcT in plcs) {

            var decomp = plcT.ToNameDecompose();

            foreach (var name in decomp)
                lst.Add(name);

        }

        AnsiConsole.Write(lst.ToTable());

    }

}