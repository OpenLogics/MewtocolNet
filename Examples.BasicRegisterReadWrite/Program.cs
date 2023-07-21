using MewtocolNet;
using MewtocolNet.Logging;
using MewtocolNet.Registers;

namespace Examples.BasicRegisterReadWrite;

internal class Program {

    //const string PLC_IP = "192.168.178.55";
    const string PLC_IP = "192.168.115.210";

    static void Main(string[] args) => Task.Run(AsyncMain).Wait();

    static async Task AsyncMain() {

        Console.Clear();

        //the library provides a logging tool, comment this out if needed
        Logger.LogLevel = LogLevel.Critical;

        //here we collect our built registers
        IRegister<short> simpleNumberRegister = null!;
        IRegister<ushort> simpleNumberRegister2 = null!;

        IRegister<Word> simpleWordRegister = null!;
        IArrayRegister<Word> overlapWordRegister = null!;

        IStringRegister stringRegister = null!;
        IArrayRegister<string> stringArrayRegister = null!;

        IArrayRegister2D<short> simpleNumberRegister2Dim = null!;

        //create a new interface to the plc using ethernet / tcp ip
        using var plc = Mewtocol.Ethernet(PLC_IP)
        .WithRegisters(b => {

            //a simple INT at register address DT1000
            b.Struct<short>("DT1000").Build(out simpleNumberRegister);
            b.Struct<ushort>("DT1001").Build(out simpleNumberRegister2);

            //you can also read the same array as an other data type
            //not how they are at the same address, that means their values are linked
            b.Struct<Word>("DT1000").Build(out simpleWordRegister);

            //same link feature is also true for arrays that overlap their addresses
            //this will go from DT999 to DT1001 because its a 3 Word array
            b.Struct<Word>("DT999").AsArray(3).Build(out overlapWordRegister);

            //strings area not stacially sized, they use a different constructor
            b.String("DT1024", 32).Build(out stringRegister);

            //string can also be arrayed
            b.String("DT2030", 5).AsArray(3).Build(out stringArrayRegister);

            //a simple 2 dimensional ARRAY [0..2, 0..2] OF INT at DT2003
            b.Struct<short>("DT2003").AsArray(3, 3).Build(out simpleNumberRegister2Dim);

            b.Struct<bool>("R19A").Build();

        })
        .Build();

        //you can explain the internal register layout and which ones are linked by
        Console.WriteLine(plc.Explain());

        //connect async to the plc
        await plc.ConnectAsync();

        //check if the connection was established
        if (!plc.IsConnected) {
            Console.WriteLine("Failed to connect to the plc...");
            Environment.Exit(1);
        }

        //restarts the program, this will make sure the global registers of the plc get reset each run
        await plc.RestartProgramAsync();

        //from this point on we can modify our registers

        //read the value of the the register
        short readValue = await simpleNumberRegister.ReadAsync();
        ushort readValue2 = await simpleNumberRegister2.ReadAsync();

        //show the value
        Console.WriteLine($"Read value for {nameof(simpleNumberRegister)}: {readValue}");
        Console.WriteLine($"Read value for {nameof(simpleNumberRegister2)}: {readValue2}");

        //write the value
        await simpleNumberRegister.WriteAsync(30);

        //show the value
        Console.WriteLine($"Value of {nameof(simpleNumberRegister)}: {simpleNumberRegister.Value}");

        //because the two registers at DT1000 are linked by their memory address in the plc,
        //both of their values got updated
        Console.WriteLine($"Value of {nameof(simpleWordRegister)}: {simpleWordRegister.Value}");

        //also the overlapping word array register value got updated, but only at the DT positions that were read
        int i = 0;
        overlapWordRegister.Value.ToList().ForEach(x => {
            Console.WriteLine($"Value of {nameof(overlapWordRegister)}[{i}]: {x}");
            i++;
        });

        //you can even read out a word bitwise
        Console.WriteLine($"Bits of {nameof(simpleWordRegister)}: {simpleWordRegister.Value?.ToStringBits()}");
        //or as a single bit
        Console.WriteLine($"Bit at index 3 of {nameof(simpleWordRegister)}: {simpleWordRegister.Value?[3]}");

        //reading / writing the string register
        //await stringRegister.WriteAsync("Lorem ipsum dolor sit amet, cons");
        await stringRegister.ReadAsync();
        Console.WriteLine($"Value of {nameof(stringRegister)}: {stringRegister.Value}");

        //reading writing a string array register
        await stringArrayRegister.ReadAsync();

        i = 0;
        stringArrayRegister.Value.ToList().ForEach(x => {
            Console.WriteLine($"Value of {nameof(stringArrayRegister)}[{i}]: {x}");
            i++;
        });

        //you can either set the whole array at once,
        //this will be slow if you only want to update one item
        await stringArrayRegister.WriteAsync(new string[] {
            "Test1",
            "Test2",
            "Test3",
        });

        //or update just one

        //COMING LATER

        //same thing also works for 2 dim arrays
        await simpleNumberRegister2Dim.ReadAsync();

        //the array is multi dimensional but can also be iterated per element
        foreach (var item in simpleNumberRegister2Dim)
            Console.WriteLine($"Element of {nameof(simpleNumberRegister2Dim)}: {item}");

        //you can also use the array indexer accessors
        Console.WriteLine($"Element [1, 2] of {nameof(simpleNumberRegister2Dim)}: {simpleNumberRegister2Dim[1, 2]}");

    }

}
