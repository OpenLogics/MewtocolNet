using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using MewtocolNet;
using MewtocolNet.RegisterAttributes;
using System.Collections;
using MewtocolNet.Logging;

namespace Examples {


    public class TestRegisters : RegisterCollectionBase {

        //corresponds to a R100 boolean register in the PLC
        [Register(100, RegisterType.R)]
        public bool TestBool1 { get; private set; }

        //corresponds to a R100 boolean register in the PLC
        [Register(RegisterType.X, SpecialAddress.D)]
        public bool TestBoolInputXD { get; private set; } 

        //corresponds to a DT1101 - DT1104 string register in the PLC with (STRING[4])
        [Register(1101, 4)]
        public string TestString1 { get; private set; }

        //corresponds to a DT7000 16 bit int register in the PLC
        [Register(7000)]
        public short TestInt16 { get; private set; }

        //corresponds to a DTD7001 - DTD7002 32 bit int register in the PLC
        [Register(7001)]
        public int TestInt32 { get; private set; }

        //corresponds to a DTD7001 - DTD7002 32 bit float register in the PLC (REAL)
        [Register(7003)]
        public float TestFloat32 { get; private set; }

        //corresponds to a DT7005 - DT7009 string register in the PLC with (STRING[5])
        [Register(7005, 5)]
        public string TestString2 { get; private set; }

        //corresponds to a DT7010 as a 16bit word/int and parses the word as single bits
        [Register(7010)]
        public BitArray TestBitRegister { get; private set; }

        //corresponds to a DT1204 as a 16bit word/int takes the bit at index 9 and writes it back as a boolean
        [Register(1204, 9, BitCount.B16)]
        public bool BitValue { get; private set; }


    }

    class Program {

        static void Main(string[] args) { 

            Task.Factory.StartNew(async () => {

                //attaching the logger
                Logger.LogLevel = LogLevel.Critical;
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

                        //writing a value to the registers
                        Task.Factory.StartNew(async () => {

                            await Task.Delay(2000);
                            //inverts the boolean register
                            interf.SetRegister(nameof(registers.TestBool1), !registers.TestBool1);
                            //adds 10 each time the plc connects to the PLCs INT regíster
                            interf.SetRegister(nameof(registers.TestInt16), (short)(registers.TestInt16 + 10));
                            //adds 1 each time the plc connects to the PLCs DINT regíster
                            interf.SetRegister(nameof(registers.TestInt32), (registers.TestInt32 + 1));
                            //adds 11.11 each time the plc connects to the PLCs REAL regíster
                            interf.SetRegister(nameof(registers.TestFloat32), (float)(registers.TestFloat32 + 11.11));

                            interf.SetRegister(nameof(registers.TestString2), new Random().Next(0, 99999).ToString());

                        });

                    }
                );

            });
            
            Console.ReadLine();
        }
    }
}
