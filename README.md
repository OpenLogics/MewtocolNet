# MewtocolNet
A Mewtocol protocol library to interface with Panasonic PLCs over TCP/Serial.

# Usage

1. Connecting to a PLC 

```C#
//Create a new interface class with your PLCs IP address and port
MewtocolInterface interf = new MewtocolInterface("127.0.0.1", 9094);

//Setup the dataregisters of the PLC you want to read
interf.AddRegister<short>("Test Integer",1204);
interf.AddRegister<string>(1101, 4);

//attaches an auto reader that polls the registers
interf.WithPoller();

//triggers when a dataregister changes its value
interf.RegisterChanged += (o) => {
    Console.WriteLine($"DT{o.MemoryAdress} {(o.Name != null ? $"({o.Name}) " : "")}changed to {o.GetValueString()}");
};

//Connects to the PLC asynchronous and invokes connected or failed
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
```
