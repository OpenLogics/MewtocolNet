using System;
using System.Collections.Generic;
using CommandLine;
using MewtocolNet;
using MewtocolNet.ComCassette;
using MewtocolNet.Logging;
using Spectre.Console;

namespace MewTerminal.Commands;

[Verb("scan", HelpText = "Scans all network PLCs")]
internal class ScanCommand : CommandLineExcecuteable {

    [Option("ip", HelpText = "IP of the source adapter" )]
    public string? IPSource { get; set; }

    [Option("timeout", Default = 100)]
    public int? TimeoutMS { get; set; }

    [Option("plc", Required = false, HelpText = "Gets the PLC types")]
    public bool GetPLCTypes { get; set; }

    private class PLCCassetteTypeInfo {

        public CassetteInformation Cassette { get; set; }

        public PLCInfo PLCInf { get; set; }

    }

    public override async Task RunAsync () {

        await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Scanning...", async ctx => {

            var query = await CassetteFinder.FindClientsAsync(IPSource, TimeoutMS ?? 100);

            var found = query.Select(x => new PLCCassetteTypeInfo { Cassette = x }).ToList();

            if (found.Count > 0 && GetPLCTypes) {

                foreach (var item in found) {

                    ctx.Status($"Getting cassette PLC {item.Cassette.IPAddress}:{item.Cassette.Port}")
                       .Spinner(Spinner.Known.Dots);

                    var dev = Mewtocol.Ethernet(item.Cassette.IPAddress, item.Cassette.Port);
                    dev.ConnectTimeout = 1000;
                    await dev.ConnectAsync();
                    item.PLCInf = dev.PlcInfo;

                    dev.Disconnect();

                }

            }

            if (found.Count() > 0) {

                AnsiConsole.MarkupLineInterpolated($"✅ Found {found.Count()} devices...");

            } else {

                AnsiConsole.MarkupLineInterpolated($"❌ Found no devices");
                return;

            }

            if (found.Any(x => x.PLCInf != PLCInfo.None)) {

                AnsiConsole.Write(found.Select(x => new {
                    x.Cassette.Name,
                    PLC = x.PLCInf.TypeCode.ToName(),
                    IsRun = x.PLCInf.OperationMode.HasFlag(OPMode.RunMode),
                    IP = x.Cassette.IPAddress,
                    x.Cassette.Port,
                    DHCP = x.Cassette.UsesDHCP,
                    MAC = x.Cassette.MacAddress,
                    Ver = x.Cassette.FirmwareVersion,
                    x.Cassette.Status,
                }).ToTable());

            } else {

                AnsiConsole.Write(found.Select(x => new {
                    x.Cassette.Name,
                    IP = x.Cassette.IPAddress,
                    x.Cassette.Port,
                    DHCP = x.Cassette.UsesDHCP,
                    MAC = x.Cassette.MacAddress,
                    Ver = x.Cassette.FirmwareVersion,
                    x.Cassette.Status,
                }).ToTable());

            }

        });

    }

}