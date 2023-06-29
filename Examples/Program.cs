using MewtocolNet.RegisterBuilding;
using MewtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MewtocolNet.Logging;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

namespace Examples;

class Program {

    static ExampleScenarios ExampleSzenarios = new ExampleScenarios();

    static void Main(string[] args) {

        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

        Console.Clear();

        AppDomain.CurrentDomain.UnhandledException += (s,e) => {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Uncatched exception: {e.ExceptionObject.ToString()}");
            Console.ResetColor();
        };

        //TaskScheduler.UnobservedTaskException += (s,e) => {
        //    Console.ForegroundColor = ConsoleColor.Magenta;
        //    Console.WriteLine($"Unobserved Task Uncatched exception: {e.Exception.ToString()}");
        //    Console.ResetColor();
        //};

        ExampleSzenarios.SetupLogger();

        LoopInput();

    }

    private static void LoopInput () {

        Console.WriteLine("All available scenarios\n");

        var methods = ExampleSzenarios.GetType().GetMethods();
        var invokeableMethods = new List<MethodInfo>(); 

        for (int i = 0, j = 0; i < methods.Length; i++) {

            MethodInfo method = methods[i];
            var foundAtt = method.GetCustomAttribute(typeof(ScenarioAttribute));

            if(foundAtt != null && foundAtt is ScenarioAttribute att) {

                Console.WriteLine($"[{j + 1}] {method.Name}() - {att.Description}");
                invokeableMethods.Add(method);

                j++;

            }

        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nEnter a number to excecute a example");
        Console.ResetColor();

        Console.WriteLine("\nOther possible commands: \n");
        Console.WriteLine($"'logger <level>' - set loglevel to one of: {string.Join(", ", Enum.GetNames(typeof(LogLevel)).ToList())}");
        Console.WriteLine("'exit' - to close this program");
        Console.WriteLine("'clear' - to clear the output");


        Console.Write("> ");

        var line = Console.ReadLine();

        var loggerMatch = Regex.Match(line, @"logger (?<level>[a-zA-Z]{0,})");

        if (loggerMatch.Success && Enum.TryParse<LogLevel>(loggerMatch.Groups["level"].Value, out var loglevel)) {

            Logger.LogLevel = loglevel;

            Console.WriteLine($"Loglevel changed to: {loglevel}");

        } else if (line == "exit") {

            Environment.Exit(0);

        } else if (line == "clear") {

            Console.Clear();

        } else if (int.TryParse(line, out var lineNum)) {

            var index = Math.Clamp(lineNum - 1, 0, invokeableMethods.Count - 1);

            var task = (Task)invokeableMethods.ElementAt(index).Invoke(ExampleSzenarios, null);

            task.Wait();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The program ran to completition");
            Console.ResetColor();

        } else {

            Console.WriteLine("Wrong input");

        }

        LoopInput();

    }

}
