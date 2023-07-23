using System;
using System.Collections.Generic;
using System.Xml.Linq;
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

            if (decomp == null) continue;

            lst.Add(decomp);

        }

        AnsiConsole.Write(lst.ToTable());

    }

}