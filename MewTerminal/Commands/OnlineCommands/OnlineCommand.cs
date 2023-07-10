using CommandLine;
using MewtocolNet;
using Spectre.Console;

namespace MewTerminal.Commands.OnlineCommands;

internal class OnlineCommand : CommandLineExcecuteable {

    [Option('e', "ethernet", Default = "127.0.0.1:9094", HelpText = "Ethernet config")]
    public string? EthernetStr { get; set; }

    [Option('s', "serial", Default = null, HelpText = "Serial port config")]
    public string? SerialStr { get; set; }

    public override async Task RunAsync() {

        try {

            if (!string.IsNullOrEmpty(SerialStr)) {

            } else {

                var split = EthernetStr.Split(":");

                string ip = split[0];
                int port = int.Parse(split[1]);

                using (var plc = Mewtocol.Ethernet(ip, port)) {

                    await AfterSetup(plc);

                }

            }

        } catch (Exception ex) {

            AnsiConsole.WriteLine($"[red]{ex.Message.ToString()}[/]");

        }

    }

    internal virtual async Task AfterSetup(IPlc plc) => throw new NotImplementedException();

}
