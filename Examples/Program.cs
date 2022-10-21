using System;
using System.Threading.Tasks;
using MewtocolNet;
using MewtocolNet.Logging;
using MewtocolNet.Registers;

namespace Examples;

class Program {

    static void Main(string[] args) {

        Console.WriteLine("Enter your scenario number:\n" +
                          "1 = Permanent connection\n" +
                          "2 = Dispose connection");

        var line = Console.ReadLine();

        if(line == "1") {
            Scenario1();
        }

        if (line == "2") {
            Scenario2();
        }

        Console.ReadLine();
    }

    private static bool isProgressReadout = false;

    static void Scenario1 () {

        Task.Factory.StartNew(async () => {

            //attaching the logger
            Logger.LogLevel = LogLevel.Verbose;
            Logger.OnNewLogMessage((date, msg) => {
                Console.WriteLine($"{date.ToString("HH:mm:ss")} {msg}");
            });

            //setting up a new PLC interface and register collection
            MewtocolInterface interf = new MewtocolInterface("10.237.191.3");
            TestRegisters registers = new TestRegisters();

            //attaching the register collection and an automatic poller
            interf.WithRegisterCollection(registers).WithPoller();

            _ = Task.Factory.StartNew(async () => {
                while (true) {
                    if (isProgressReadout) continue;
                    Console.Title = $"Polling Paused: {interf.PollingPaused}, " +
                    $"Poller active: {interf.PollerActive}, " +
                    $"Speed UP: {interf.BytesPerSecondUpstream} B/s, " +
                    $"Speed DOWN: {interf.BytesPerSecondDownstream} B/s, " +
                    $"Poll delay: {interf.PollerDelayMs} ms, " +
                    $"Queued MSGs: {interf.QueuedMessages}";
                    await Task.Delay(1000);
                }
            });

            //await interf.ConnectAsync((plcinf) => AfterConnect(interf, registers));

            bool flip = false;
            while(true) {

                if(!flip) {
                    await interf.ConnectAsync();
                } else {
                    interf.Disconnect();
                }

                flip = !flip;
                await Task.Delay(5000);

            }

        });

    }

    static void AfterConnect (MewtocolInterface interf, TestRegisters registers) {

        //reading a value from the register collection
        Console.WriteLine($"BitValue is: {registers.BitValue}");
        Console.WriteLine($"TestEnum is: {registers.TestEnum}");

        _ = Task.Factory.StartNew(async () => {

            while(true) {

                isProgressReadout = true;

                await interf.ReadByteRange(1000, 2000, (p) => {

                    var totSteps = 10;
                    var cSteps = totSteps * p;

                    string progBar = "";
                    for (int i = 0; i < totSteps; i++) {

                        if(i < (int)cSteps) {
                            progBar += "⬛";
                        } else {
                            progBar += "⬜";
                        }
                        
                    }

                    Console.Title = $"Prog read range: {(p * 100).ToString("N1")}% {progBar} Queued MSGs: {interf.QueuedMessages}";

                });

                isProgressReadout = false;

                await Task.Delay(3000);

            }

        });

        //writing a value to the registers
        _ = Task.Factory.StartNew(async () => {

            //set plc to run mode if not already
            await interf.SetOperationMode(OPMode.Run);

            int startAdress = 10000;
            int entryByteSize = 20 * 20;

            var bytes = await interf.ReadByteRange(startAdress, entryByteSize);
            Console.WriteLine($"Bytes: {string.Join('-', bytes)}");

            await Task.Delay(2000);

            await interf.SetRegisterAsync(nameof(registers.TestInt32), 100);

            //adds 10 each time the plc connects to the PLCs INT regíster
            interf.SetRegister(nameof(registers.TestInt16), (short)(registers.TestInt16 + 10));
            //adds 1 each time the plc connects to the PLCs DINT regíster
            interf.SetRegister(nameof(registers.TestInt32), (registers.TestInt32 + 1));
            //adds 11.11 each time the plc connects to the PLCs REAL regíster
            interf.SetRegister(nameof(registers.TestFloat32), (float)(registers.TestFloat32 + 11.11));
            //writes 'Hello' to the PLCs string register
            interf.SetRegister(nameof(registers.TestString2), "Hello");
            //set the current second to the PLCs TIME register
            interf.SetRegister(nameof(registers.TestTime), TimeSpan.FromSeconds(DateTime.Now.Second));

            //test pausing poller

            bool pollerPaused = false;

            while (true) {

                await Task.Delay(5000);

                pollerPaused = !pollerPaused;

                if (pollerPaused) {
                    Console.WriteLine("Pausing poller");
                    await interf.PausePollingAsync();
                    //interf.PollerDelayMs += 10;
                    Console.WriteLine("Paused poller");
                } else {
                    interf.ResumePolling();
                    Console.WriteLine("Resumed poller");
                }

            }


        });

    } 

    static void Scenario2 () {

        Logger.LogLevel = LogLevel.Critical;
        Logger.OnNewLogMessage((date, msg) => {
            Console.WriteLine($"{date.ToString("HH:mm:ss")} {msg}");
        });

        Task.Factory.StartNew(async () => {

            //automatic endpoint
            using (var interf = new MewtocolInterface("10.237.191.3")) {

                await interf.ConnectAsync();

                if (interf.IsConnected) {

                    await Task.Delay(5000);

                }

                interf.Disconnect();

            }

            //manual endpoint
            using (var interf = new MewtocolInterface("10.237.191.3")) {

                interf.HostEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("10.237.191.77"), 0);

                await interf.ConnectAsync();

                if(interf.IsConnected) {

                    await Task.Delay(5000);

                }

                interf.Disconnect();

            }


        });

    }

}
