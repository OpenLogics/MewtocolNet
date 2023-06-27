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

namespace Examples;

public class ExampleScenarios {

    public void SetupLogger () {

        //attaching the logger
        Logger.LogLevel = LogLevel.Info;
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

        _ = Task.Factory.StartNew(async () => {
            
            while (interf.IsConnected) {

                //flip the bool register each tick and wait for it to be registered
                //await interf.SetRegisterAsync(nameof(registers.TestBool1), !registers.TestBool1);

                Console.Title = $"Polling Paused: {interf.PollingPaused}, " +
                $"Poller active: {interf.PollerActive}, " +
                $"Speed UP: {interf.BytesPerSecondUpstream} B/s, " +
                $"Speed DOWN: {interf.BytesPerSecondDownstream} B/s, " +
                $"Poll delay: {interf.PollerDelayMs} ms, " +
                $"Queued MSGs: {interf.QueuedMessages}";

                Console.Clear();
                Console.WriteLine("Underlying registers on tick: \n");

                foreach (var register in interf.Registers) {

                    Console.WriteLine($"{register.ToString(true)}");

                }

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
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210");

        //auto add all built registers to the interface 
        var builder = RegBuilder.ForInterface(interf);
        var r0reg = builder.FromPlcRegName("R0").Build();
        builder.FromPlcRegName("R1").Build();
        builder.FromPlcRegName("R1F").Build();
        builder.FromPlcRegName("R101A").Build();

        var shortReg = builder.FromPlcRegName("DT35").AsPlcType(PlcVarType.INT).Build();
        builder.FromPlcRegName("DDT36").AsPlcType(PlcVarType.DINT).Build();
        builder.FromPlcRegName("DT200").AsBytes(30).Build();

        builder.FromPlcRegName("DDT38").AsPlcType(PlcVarType.TIME).Build();
        //builder.FromPlcRegName("DT40").AsPlcType(PlcVarType.STRING).Build();

        //connect
        await interf.ConnectAsync();

        //var res = await interf.SendCommandAsync("%01#RCSR000F");

        while(true) {

            await interf.SetRegisterAsync(r0reg, !(bool)r0reg.Value);
            await interf.SetRegisterAsync(shortReg, (short)new Random().Next(0, 100));

            var sw = Stopwatch.StartNew();

            foreach (var reg in interf.Registers) {

                await reg.ReadAsync();

                Console.WriteLine($"Register {reg.ToString()}");

            }

            sw.Stop();

            Console.WriteLine($"Total read time for registers: {sw.Elapsed.TotalMilliseconds:N0}ms");

            Console.WriteLine();

            await Task.Delay(1000);

        }

    }

    [Scenario("Test multi frame")]
    public async Task MultiFrameTest() {

        var preLogLevel = Logger.LogLevel;
        Logger.LogLevel = LogLevel.Error;

        //setting up a new PLC interface and register collection
        MewtocolInterface interf = new MewtocolInterface("192.168.115.210");

        //auto add all built registers to the interface 
        var builder = RegBuilder.ForInterface(interf);
        var r0reg = builder.FromPlcRegName("R0").Build();
        builder.FromPlcRegName("R1").Build();
        builder.FromPlcRegName("R1F").Build();
        builder.FromPlcRegName("R60A").Build();
        builder.FromPlcRegName("R61A").Build();
        builder.FromPlcRegName("R62A").Build();
        builder.FromPlcRegName("R63A").Build();
        builder.FromPlcRegName("R64A").Build();
        builder.FromPlcRegName("R65A").Build();
        builder.FromPlcRegName("R66A").Build();
        builder.FromPlcRegName("R67A").Build();
        builder.FromPlcRegName("R68A").Build();
        builder.FromPlcRegName("R69A").Build();
        builder.FromPlcRegName("R70A").Build();
        builder.FromPlcRegName("R71A").Build();

        //connect
        await interf.ConnectAsync();

        Console.WriteLine("Poller cycle started");
        var sw = Stopwatch.StartNew();

        await interf.RunPollerCylceManual(false);

        sw.Stop();

        Console.WriteLine("Poller cycle finished");

        Console.WriteLine($"Single frame excec time: {sw.ElapsedMilliseconds:N0}ms");

        interf.Disconnect();

        await Task.Delay(1000);

        await interf.ConnectAsync();

        sw = Stopwatch.StartNew();

        Console.WriteLine("Poller cycle started");

        await interf.RunPollerCylceManual(true);

        sw.Stop();

        Console.WriteLine("Poller cycle finished");

        Console.WriteLine($"Multi frame excec time: {sw.ElapsedMilliseconds:N0}ms");

        Logger.LogLevel = preLogLevel;

        await Task.Delay(10000);

    }

}
