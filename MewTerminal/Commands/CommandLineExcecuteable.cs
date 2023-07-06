using CommandLine.Text;
using CommandLine;
using MewtocolNet.Logging;

namespace MewTerminal.Commands;

public abstract class CommandLineExcecuteable {

    static UnParserSettings UnparserSet = new UnParserSettings {
        PreferShortName = true,
    };

    [Option('v', "verbosity", HelpText = "Sets the Loglevel verbosity", Default = LogLevel.None)]
    public LogLevel LogLevel { get; set; } = LogLevel.None;

    [Usage]
    public static IEnumerable<Example> Examples {
        get {
            return new List<Example>() {
                new Example(
                helpText: "Sanning from adapter with ip 127.0.0.1 and logging all critical messages", 
                formatStyle: UnparserSet,
                sample: new ScanCommand { 
                    IPSource = "127.0.0.1",
                    LogLevel = LogLevel.Critical,   
                }),
                new Example(
                helpText: "Scanning from all adapters and logging only errors",
                formatStyle: UnparserSet,
                sample: new ScanCommand {
                    LogLevel = LogLevel.Error,
                }),
            };
        }
    }

    public virtual void Run() { }

    public virtual Task RunAsync () => Task.CompletedTask;      

}
