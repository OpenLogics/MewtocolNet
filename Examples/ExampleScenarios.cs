using MewtocolNet.Logging;
using MewtocolNet;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections;
using MewtocolNet.RegisterBuilding;
using System.Collections.Generic;
using MewtocolNet.Registers;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32;
using MewtocolNet.ComCassette;
using System.Linq;
using System.Net;

namespace Examples;

public class ExampleScenarios {

    public void SetupLogger () {

        //attaching the logger
        Logger.LogLevel = LogLevel.Error;
        Logger.OnNewLogMessage((date, level, msg) => {

            if (level == LogLevel.Error) Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine($"{date.ToString("HH:mm:ss")} {msg}");

            Console.ResetColor();

        });

    }

    [Scenario("Dispose and disconnect connection")]
    public async Task RunDisposalAndDisconnectAsync () {

        //automatic disposal
        using (var interf = new MewtocolInterface("192.168.115.210")) {

            await interf.ConnectAsync();

            if (interf.IsConnected) {

                Console.WriteLine("Opened connection");

                await Task.Delay(5000);

            }

        }

        Console.WriteLine("Disposed, closed connection");

        //manual close
        var interf2 = new MewtocolInterface("192.168.115.210");

        await interf2.ConnectAsync();

        if (interf2.IsConnected) {

            Console.WriteLine("Opened connection");

            await Task.Delay(5000);

        }

        interf2.Disconnect();

        Console.WriteLine("Disconnected, closed connection");

    }

    [Scenario("Read all kinds of example registers")]
    public async Task RunReadTest () {

        //setting up a new PLC interface and register collection
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210").WithPoller();

        //auto add all built registers to the interface 
        var builder = RegBuilder.ForInterface(interf);
        var r0reg = builder.FromPlcRegName("R0").Build();
        builder.FromPlcRegName("R1", "Testname").Build();
        builder.FromPlcRegName("R1F").Build();
        builder.FromPlcRegName("R101A").Build();

        var shortReg = builder.FromPlcRegName("DT35").AsPlcType(PlcVarType.INT).Build();
        builder.FromPlcRegName("DDT36").AsPlcType(PlcVarType.DINT).Build();
        builder.FromPlcRegName("DT200").AsBytes(30).Build();

        var timeReg = builder.FromPlcRegName("DDT38").AsPlcType(PlcVarType.TIME).Build();
        var stringReg = builder.FromPlcRegName("DT40").AsPlcType(PlcVarType.STRING).Build();

        //connect
        await interf.ConnectAsync();

        //await first register data
        await interf.AwaitFirstDataAsync();

        _ = Task.Factory.StartNew(async () => {

            void setTitle () {

                Console.Title =
                $"Speed UP: {interf.BytesPerSecondUpstream} B/s, " +
                $"Speed DOWN: {interf.BytesPerSecondDownstream} B/s, " +
                $"Poll cycle: {interf.PollerCycleDurationMs} ms, " +
                $"Queued MSGs: {interf.QueuedMessages}";

            }

            while (interf.IsConnected) { 
                setTitle();
                await Task.Delay(1000);
            }

            setTitle();

        });

        while (interf.IsConnected) {

            var sw = Stopwatch.StartNew();

            //set bool
            await r0reg.WriteAsync(!(bool)r0reg.Value);
            
            //set random num
            await shortReg.WriteAsync((short)new Random().Next(0, 100));
            await stringReg.WriteAsync($"_{DateTime.Now.Second}s_");

            sw.Stop();

            foreach (var reg in interf.Registers)
                Console.WriteLine(reg.ToString());     

            Console.WriteLine($"Total write time for registers: {sw.Elapsed.TotalMilliseconds:N0}ms");

            Console.WriteLine();

            //await Task.Delay(new Random().Next(0, 10000));
            await Task.Delay(1000);

        }

    }

    [Scenario("Test read speed 100 R registers")]
    public async Task ReadRSpeedTest() {

        var preLogLevel = Logger.LogLevel;
        Logger.LogLevel = LogLevel.Critical;

        //setting up a new PLC interface and register collection
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210") {
            ConnectTimeout = 3000,
        };

        //auto add all built registers to the interface 
        var builder = RegBuilder.ForInterface(interf);
        for (int i = 0; i < 100; i++) {

            builder.FromPlcRegName($"R{i}A").Build();

        }

        //connect
        await interf.ConnectAsync();

        Console.WriteLine("Poller cycle started");
        var sw = Stopwatch.StartNew();

        int cmdCount = await interf.RunPollerCylceManual();

        sw.Stop();

        Console.WriteLine("Poller cycle finished");

        Console.WriteLine($"Single frame excec time: {sw.ElapsedMilliseconds:N0}ms for {cmdCount} commands and {interf.Registers.Count()} R registers");

        interf.Disconnect();

        await Task.Delay(1000);

    }

    [Scenario("Find all COM5 cassettes in the network")]
    public async Task FindCassettes () {

        Console.Clear();

        var casettes =  await CassetteFinder.FindClientsAsync();

        foreach (var cassette in casettes) {

            Console.WriteLine($"{cassette.Name}");
            Console.WriteLine($"IP: {cassette.IPAddress}");
            Console.WriteLine($"Port: {cassette.Port}");
            Console.WriteLine($"DHCP: {cassette.UsesDHCP}");
            Console.WriteLine($"Subnet Mask: {cassette.SubnetMask}");
            Console.WriteLine($"Gateway: {cassette.GatewayAddress}");
            Console.WriteLine($"Mac: {cassette.MacAddress.ToHexString(":")}");
            Console.WriteLine($"Firmware: {cassette.FirmwareVersion}");
            Console.WriteLine($"Status: {cassette.Status}");
            Console.WriteLine($"Endpoint: {cassette.EndpointName} - {cassette.Endpoint.Address}");
            Console.WriteLine();

        }

        await Task.Delay(5000);

        var found = casettes.FirstOrDefault(x => x.Endpoint.Address.ToString() == "10.237.191.75");

        found.IPAddress = IPAddress.Parse($"192.168.1.{new Random().Next(20, 120)}");
        found.Name = $"Rand{new Random().Next(5, 15)}";

        await found.SendNewConfigAsync();

    }

}
