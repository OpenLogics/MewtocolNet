using MewtocolNet.RegisterBuilding;
using MewtocolNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Examples;

class Program {

    static ExampleScenarios ExampleSzenarios = new ExampleScenarios();

    static void Main(string[] args) {

        AppDomain.CurrentDomain.UnhandledException += (s,e) => {
            Console.WriteLine(e.ExceptionObject.ToString());
        };

        TaskScheduler.UnobservedTaskException += (s,e) => {
            Console.WriteLine(e.Exception.ToString());
        };

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

        Console.WriteLine("\nOther possible commands: \n\n" +
                          "'toggle logger' - toggle the built in mewtocol logger on/off\n" +
                          "'exit' - to close this program \n" +
                          "'clear' - to clear the output \n");

        Console.Write("> ");

        var line = Console.ReadLine();

        if (line == "toggle logger") {

            ExampleScenarios.MewtocolLoggerEnabled = !ExampleScenarios.MewtocolLoggerEnabled;

            Console.WriteLine(ExampleScenarios.MewtocolLoggerEnabled ? "Logger enabled" : "Logger disabled");

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
