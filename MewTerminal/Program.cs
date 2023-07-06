using CommandLine;
using CommandLine.Text;
using MewTerminal.Commands;
using MewtocolNet.Logging;
using Spectre.Console;
using System.Reflection;

namespace MewTerminal;

internal class Program {
    
    static void Main(string[] args) {

        Logger.OnNewLogMessage((dt, lv, msg) => {

            AnsiConsole.WriteLine($"{msg}");

        });

        #if DEBUG

        Console.Clear();

        var firstArg = new string[] { "help" };

        start:

        if(firstArg == null) {
            Console.WriteLine("Enter arguments [DEBUG MODE]");
            args = Console.ReadLine().SplitArgs();
        }

        //print help first time
        InitParser(firstArg ?? args);
        firstArg = null;
        goto start;

        #else

        InitParser(args);

        #endif

    }

    private static Type[] LoadVerbs() {

        var lst = Assembly.GetExecutingAssembly()
                          .GetTypes()
                          .Where(t => t.GetCustomAttribute<VerbAttribute>() != null)
                          .ToArray();
        return lst;

    }

    static void InitParser (string[] args) {

        var types = LoadVerbs();

        var parseRes = Parser.Default.ParseArguments(args, types);

        var helpText = HelpText.AutoBuild(parseRes, h => {

            h.AddEnumValuesToHelpText = true;
                                                           
            return h;
        
        }, e => e);

        parseRes.WithNotParsed(err => {

        });

        if(parseRes?.Value != null && parseRes.Value is CommandLineExcecuteable exc) {

            Logger.LogLevel = exc.LogLevel;

            exc.Run();
            var task = Task.Run(exc.RunAsync);
            task.Wait();
            
        }

    }

}
