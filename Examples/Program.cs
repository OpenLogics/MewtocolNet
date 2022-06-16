using System;
using System.Threading.Tasks;
using System.Text.Json;
using MewtocolNet;

namespace Examples {
    class Program {
        static void Main(string[] args) { 

            Console.WriteLine("Starting test");

            Task.Factory.StartNew(async () => {

                MewtocolInterface interf = new MewtocolInterface("10.237.191.3");

                interf.AddRegister<short>("Cooler Status",1204);
                interf.AddRegister<string>(1101, 4);

                interf.RegisterChanged += (o) => {
                    Console.WriteLine($"DT{o.MemoryAdress} {(o.Name != null ? $"({o.Name}) " : "")}changed to {o.GetValueString()}");
                };

                await interf.ConnectAsync(
                    (plcinf) => {

                        Console.WriteLine("Connected to PLC:\n" + plcinf.ToString());
                    },
                    () => {
                        Console.WriteLine("Failed connection");
                    }
                ).AttachContinousReader(50);


            });
            
            Console.ReadLine();
        }
    }
}
