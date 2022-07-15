using System;
using System.Threading.Tasks;
using MewtocolNet;
using MewtocolNet.Logging;
using MewtocolNet.Registers;

namespace Examples {

    class Program {

        static void Main(string[] args) { 

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

                await interf.ConnectAsync(
                    (plcinf) => {

                        //reading a value from the register collection
                        Console.WriteLine($"BitValue is: {registers.BitValue}");
                        Console.WriteLine($"TestEnum is: {registers.TestEnum}");

                        //writing a value to the registers
                        Task.Factory.StartNew(async () => {

                            //set plc to run mode if not already
                            await interf.SetOperationMode(OPMode.Run);

                            await Task.Delay(2000);

                            await interf.SetRegisterAsync(nameof(registers.TestInt32), 100);

                            _ = Task.Factory.StartNew(async () => {
                                while(true) {
                                    for (int i = 0; i < 5; i++) {
                                        var bytes = await interf.ReadByteRange(1020, 20);
                                        await interf.SetRegisterAsync(nameof(registers.TestBool1), !registers.TestBool1);
                                        await interf.SetRegisterAsync(nameof(registers.TestInt32), registers.TestInt32 + 100);
                                        await Task.Delay(1333);
                                    }
                                    await Task.Delay(10000);
                                }
                            });

                            //adds 10 each time the plc connects to the PLCs INT regíster
                            //interf.SetRegister(nameof(registers.TestInt16), (short)(registers.TestInt16 + 10));
                            //adds 1 each time the plc connects to the PLCs DINT regíster
                            //interf.SetRegister(nameof(registers.TestInt32), (registers.TestInt32 + 1));
                            //adds 11.11 each time the plc connects to the PLCs REAL regíster
                            //interf.SetRegister(nameof(registers.TestFloat32), (float)(registers.TestFloat32 + 11.11));
                            //writes 'Hello' to the PLCs string register
                            //interf.SetRegister(nameof(registers.TestString2), "Hello");
                            //set the current second to the PLCs TIME register
                            //interf.SetRegister(nameof(registers.TestTime), TimeSpan.FromSeconds(DateTime.Now.Second));

                        });

                    }
                );

            });
            
            Console.ReadLine();
        }
    }
}
