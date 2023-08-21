using MewtocolNet.Logging;
using MewtocolNet.Registers;
using MewtocolNet;

namespace Examples.Polling;

internal class Program {

    const string PLC_IP = "192.168.178.55";

    static void Main(string[] args) => Task.Run(AsyncMain).Wait();

    static async Task AsyncMain() {

        Console.Clear();

        //the library provides a logging tool, comment this out if needed
        Logger.LogLevel = LogLevel.Change;

        //create a new interface to the plc using ethernet / tcp ip
        using var plc = Mewtocol.Ethernet(PLC_IP)
        .WithPoller() // <= use this in the builder pattern to automatically poll registers
        .WithInterfaceSettings(s => {

            //this means registers at the same address
            //or that are contained by overlapping arrays
            //always get assigned the same poll level
            s.PollLevelOverwriteMode = PollLevelOverwriteMode.Highest;

            //this means combine all registers that are not farther
            //than 8 words apart from another into one tcp message,
            //this safes message frames but a to large number can block read write traffic
            s.MaxOptimizationDistance = 8;

        })
        .WithCustomPollLevels(l => {

            //this makes registers at polllevel 2 only run all 3 iterations
            l.SetLevel(2, 3);

            //this makes registers at polllevel 3 only run all 5 seconds
            l.SetLevel(3, TimeSpan.FromSeconds(5));

        })
        .WithRegisters(b => {

            b.Struct<short>("DT1000").Build();
            b.Struct<Word>("DT1000").Build();
            b.Struct<ushort>("DT1001").Build();
            b.Struct<Word>("DT999").AsArray(3).Build();

            //we dont want to poll the string register as fast as we can
            //so we assign it to level 2 to run only all 3 iterations
            b.String("DT1024", 32).PollLevel(2).Build();

            //we want to poll this array only at the first iteration, 
            //and after this only if we need the data
            b.Struct<Word>("DT2000").AsArray(3).PollLevel(PollLevel.FirstIteration).Build();

            //we want to poll this string array all 5 seconds
            b.String("DT2030", 5).AsArray(3).PollLevel(3).Build();

            //this is a fairly large array, so we never poll it automatically 
            //only if we need the data
            b.Struct<short>("DT2003").AsArray(3, 3).PollLevel(PollLevel.Never).Build();

        })
        .Build();

        //this explains the underlying data structure for the poller
        Console.WriteLine(plc.Explain());

        await plc.ConnectAsync(async () => {

            //we want to restart the program before the poller starts
            await plc.RestartProgramAsync();

        });

        if (!plc.IsConnected) {
            Console.WriteLine("Failed to connect to the plc...");
            Environment.Exit(1);
        }

        Console.ReadLine();
        
    }


}
