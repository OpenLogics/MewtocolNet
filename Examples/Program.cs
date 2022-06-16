using System;
using System.Threading.Tasks;
using System.Text.Json;
using MewtocolNet;
using MewtocolNet.Responses;

namespace Examples {
    class Program {
        static void Main(string[] args) { 

            Console.WriteLine("Starting test");

            Task.Factory.StartNew(async () => {

                MewtocolInterface interf = new MewtocolInterface("10.237.191.3");

                interf.AddRegister<short>("Cooler Status",1204);
                interf.AddRegister<string>(1101, 4);

                interf.WithPoller();

                interf.RegisterChanged += (o) => {
                    Console.WriteLine($"DT{o.MemoryAdress} {(o.Name != null ? $"({o.Name}) " : "")}changed to {o.GetValueString()}");
                };

                await interf.ConnectAsync(
                    (plcinf) => {

                        Console.WriteLine("Connected to PLC:\n" + plcinf.ToString());

                        //read back a register value
                        var statusNum = (NRegister<short>)interf.Registers[1204];
                        Console.WriteLine($"Status num is: {statusNum.Value}");

                    },
                    () => {
                        Console.WriteLine("Failed connection");
                    }
                );


            });
            
            Console.ReadLine();
        }
    }
}
