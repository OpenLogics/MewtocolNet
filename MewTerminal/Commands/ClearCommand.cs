using System;
using System.Collections.Generic;
using CommandLine;
using MewtocolNet;
using MewtocolNet.ComCassette;
using Spectre.Console;

namespace MewTerminal.Commands;

[Verb("clear", HelpText = "Clears console", Hidden = true)]
internal class ClearCommand : CommandLineExcecuteable {

    public override void Run() {

        Console.Clear();

    }

}