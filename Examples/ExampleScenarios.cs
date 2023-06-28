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

    [Scenario("Permament connection with poller")]
    public async Task RunCyclicPollerAsync () {

        Console.WriteLine("Starting poller scenario");

        int runTime = 10000;
        int remainingTime = runTime;

        //setting up a new PLC interface and register collection
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210");
        TestRegisters registers = new TestRegisters();

        //attaching the register collection and an automatic poller
        interf.WithRegisterCollection(registers).WithPoller();

        await interf.ConnectAsync();
        await interf.AwaitFirstDataAsync();

        _ = Task.Factory.StartNew(async () => {
            
            while (interf.IsConnected) {

                //flip the bool register each tick and wait for it to be registered
                //await interf.SetRegisterAsync(nameof(registers.TestBool1), !registers.TestBool1);

                Console.Title =
                $"Speed UP: {interf.BytesPerSecondUpstream} B/s, " +
                $"Speed DOWN: {interf.BytesPerSecondDownstream} B/s, " +
                $"Poll cycle: {interf.PollerCycleDurationMs} ms, " +
                $"Queued MSGs: {interf.QueuedMessages}";

                Console.Clear();
                Console.WriteLine("Underlying registers on tick: \n");

                foreach (var register in interf.Registers)
                    Console.WriteLine($"{register.ToString(true)}");

                Console.WriteLine($"{registers.TestBool1}");
                Console.WriteLine($"{registers.TestDuplicate}");

                remainingTime -= 1000;

                Console.WriteLine($"\nStopping in: {remainingTime}ms");

                await Task.Delay(1000);

            }

        });

        await Task.Delay(runTime);
        interf.Disconnect();

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

    [Scenario("Test auto enums and bitwise, needs the example program from MewtocolNet/PLC_Test")]
    public async Task RunEnumsBitwiseAsync () {

        Console.WriteLine("Starting auto enums and bitwise");

        //setting up a new PLC interface and register collection
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210");
        TestRegistersEnumBitwise registers = new TestRegistersEnumBitwise();

        //attaching the register collection and an automatic poller
        interf.WithRegisterCollection(registers).WithPoller();

        registers.PropertyChanged += (s, e) => {

            Console.Clear();

            var props = registers.GetType().GetProperties();

            foreach (var prop in props) {

                var val = prop.GetValue(registers);
                string printVal = val?.ToString() ?? "null";

                if (val is BitArray bitarr) {
                    printVal = bitarr.ToBitString();
                }

                Console.Write($"{prop.Name} - ");

                if(printVal == "True") {
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                Console.Write($"{printVal}");

                Console.ResetColor();

                Console.WriteLine();

            }

        };

        await interf.ConnectAsync();

        //use the async method to make sure the cycling is stopped
        //await interf.SetRegisterAsync(nameof(registers.StartCyclePLC), false);

        await Task.Delay(5000);

        //set the register without waiting for it async
        registers.StartCyclePLC = true;

        await Task.Delay(5000);

        registers.StartCyclePLC = false;

        await Task.Delay(2000);

    }

    [Scenario("Read register test")]
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

    [Scenario("Test multi frame")]
    public async Task MultiFrameTest() {

        var preLogLevel = Logger.LogLevel;
        Logger.LogLevel = LogLevel.Critical;

        //setting up a new PLC interface and register collection
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210") {
            ConnectTimeout = 3000,
        };

        //auto add all built registers to the interface 
        var builder = RegBuilder.ForInterface(interf);
        var r0reg = builder.FromPlcRegName("R0").Build();
        builder.FromPlcRegName("R1").Build();
        builder.FromPlcRegName("DT0").AsBytes(100).Build();

        for (int i = 1; i < 100; i++) {

            builder.FromPlcRegName($"R{i}A").Build();

        }

        //connect
        await interf.ConnectAsync();

        Console.WriteLine("Poller cycle started");
        var sw = Stopwatch.StartNew();

        int cmdCount = await interf.RunPollerCylceManual();

        sw.Stop();

        Console.WriteLine("Poller cycle finished");

        Console.WriteLine($"Single frame excec time: {sw.ElapsedMilliseconds:N0}ms for {cmdCount} commands");

        interf.Disconnect();

        await Task.Delay(1000);

    }

}
