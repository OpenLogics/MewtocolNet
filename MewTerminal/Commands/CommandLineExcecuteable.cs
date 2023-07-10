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

    public virtual void Run() { }

    public virtual Task RunAsync () => Task.CompletedTask;      

}
