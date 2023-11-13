using MewtocolNet;
using MewtocolNet.Logging;

namespace Examples.BasicUsage;

internal class Program {

    const string IP = "192.168.115.210";
    const int PORT = 9094;

    static void Main(string[] args) => Task.Run(AsyncMain).Wait();

    static async Task AsyncMain () {

        //the library provides a logging tool, comment this out if needed
        Logger.LogLevel = LogLevel.Critical;
        Logger.OnNewLogMessage((t, l, m) => { Console.WriteLine(m); });

        //create a new interface to the plc using ethernet / tcp ip
        //the using keyword is optional, if you want to use your PLC instance
        //globally leave it 

        //you can also specify the source ip with:
        //Mewtocol.Ethernet(IP, PORT).FromSource("192.168.113.2", 46040).Build()

        using (var plc = Mewtocol.Ethernet(IP, PORT).Build()) {

            //connect async to the plc
            await plc.ConnectAsync();

            //check if the connection was established
            if (!plc.IsConnected) {
                Console.WriteLine("Failed to connect to the plc...");
                Environment.Exit(1);
            }

            //print basic plc info
            Console.WriteLine(plc.PlcInfo);

            //check if the plc is not in RUN mode, change to run
            if(!plc.PlcInfo.IsRunMode) await plc.SetOperationModeAsync(true);

            //get information about the plc
            Console.WriteLine($"PLC type: {plc.PlcInfo.TypeName}");
            Console.WriteLine($"Capacity: {plc.PlcInfo.ProgramCapacity}k");
            Console.WriteLine($"Error: {plc.PlcInfo.SelfDiagnosticError}k");

            //set the plc to prog mode
            //await plc.SetOperationModeAsync(false);

            //do disconnect use
            plc.Disconnect();

            //or
            //await plc.DisconnectAsync();
            
            //you can then change the connection settings for example to another PLC
            plc.ConfigureConnection("192.168.115.212", 9094);
            await plc.ConnectAsync();

            plc.Disconnect();

            plc.ConfigureConnection("192.168.115.214", 9094);
            await plc.ConnectAsync();

            plc.Disconnect();

            plc.ConfigureConnection("192.168.178.55", 9094);
            await plc.ConnectAsync();

        }

        //you can also find any applicable source endpoints by using:
        foreach (var endpoint in Mewtocol.GetSourceEndpoints()) {

            Console.WriteLine($"Usable endpoint: {endpoint}");
        
        }

    }

}
