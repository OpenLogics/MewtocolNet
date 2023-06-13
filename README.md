[![Build pipeline](https://github.com/WOmed/MewtocolNet/actions/workflows/build-pipeline.yml/badge.svg)](https://github.com/WOmed/MewtocolNet/actions/workflows/build-pipeline.yml)
[![Nuget](https://img.shields.io/nuget/v/Mewtocol.NET)](https://www.nuget.org/packages/Mewtocol.NET)
![Lines of code](https://img.shields.io/tokei/lines/github/WOmed/MewtocolNet)
![GitHub](https://img.shields.io/github/license/WOmed/MewtocolNet)
![Status](https://img.shields.io/badge/Status-In%20dev-orange)

# MewtocolNet
An easy to use Mewtocol protocol library to interface with Panasonic PLCs over TCP/Serial.

## Disclaimer 
This library is not an official panasonic product nor does panasonic provide financial support or limitations in any form. 
This software was written by WOLF Medizintechnik GmbH (@WOmed/dev).

# Features

> Features that are not checked still need implementation

- [x] Read out stats from your PLC
- [x] Read and write registers in real time
- [x] Dynamic register type casting from properties
- [x] Change run / prog modes
- [x] Write / read byte blocks in a whole chain
- [ ] Upload / Download programs to the PLC
- [ ] Reading / writing PLC system registers

# Support

## .NET Support

This library was written in **netstandard2.0** and should be compatible with a lot of .NET environments.

For a full list of supported .NET clrs see [this page](https://docs.microsoft.com/de-de/dotnet/standard/net-standard?tabs=net-standard-2-0#select-net-standard-version)

## PLC Support

> This library was only tested with a few PLCs, other types that support the Panasonic Mewtocol protocol might work. 
> Use at your own risk, others might follow with community feedback

|PLC Type|Supported|Tested|
|--------|---------|------|
FPX C14  |✔        |✔   |
FPX C30  |✔        |✔   |
FPX-H C14|✔        |✔   |
FPX-H C30|✔        |✔   |
FP Sigma |✔        |❌  |

Where is the RS232/Serial support?

> Support for the serial protocol will be added soon, feel free to contribute

# Installing

Install this package by using [Nuget](https://www.nuget.org/packages/Mewtocol.NET) or reference 
```XML
<PackageReference Include="Mewtocol.NET" Version="0.6.2"/>
```
in your dependencies.
Alternatively use the dotnet CLI and run 
```Shell
dotnet add package Mewtocol.NET
```

# Protocol description

Panasonic has published a [protocol definition](https://mediap.industry.panasonic.eu/assets/custom-upload/Factory%20&%20Automation/PLC/Manuals/mn_all_plcs_mewtocol_user_pidsx_en.pdf) on their site.
Refer to this site if you want to see the general functionality or add / report missing features.

> This library is at the time not feature complete, but all essential features are provided

# Usage

See [More examples](/Examples) here

## Connecting to a PLC 

Connecting to a PLC is as simple as 

```C#
 //attaching a logger
Logger.LogLevel = LogLevel.Verbose;
Logger.OnNewLogMessage((date, msg) => {
    Console.WriteLine($"{date.ToString("HH:mm:ss")} {msg}");
});

//setting up a new PLC interface
MewtocolInterface plc = new MewtocolInterface("192.168.115.5");

await plc.ConnectAsync();
```

## Reading data registers / contacts

[Detailed instructions](https://github.com/WOmed/MewtocolNet/wiki/Attribute-handled-reading)

- Create a new class that inherits from `RegisterCollectionBase`

```C#
public class TestRegisters : RegisterCollectionBase {

    //corresponds to a R100 boolean register in the PLC
    [Register(100, RegisterType.R)]
    public bool TestBool1 { get; private set; }

    //corresponds to a XD input of the PLC
    [Register(RegisterType.X, SpecialAddress.D)]
    public bool TestBoolInputXD { get; private set; } 

    //corresponds to a DT7012 - DT7013 as a 32bit time value that gets parsed as a timespan (TIME)
    //the smallest value to communicate to the PLC is 10ms
    [Register(7012)]
    public TimeSpan TestTime { get; private set; }  
    
    //corresponds to a DT1101 - DT1104 string register in the PLC with (STRING[4])
    [Register(1101, 4)]
    public string TestString1 { get; private set; }

}
```

- Connect to the PLC and attach the register collection and logger
- attach an automatic poller by chaining `.WithPoller()` after the register attachment

```C#
//setting up a new PLC interface and register collection
MewtocolInterface plc = new MewtocolInterface("192.168.115.5");
TestRegisters registers = new TestRegisters();

//attaching the register collection and an automatic poller
plc.WithRegisterCollection(registers).WithPoller();

await plc.ConnectAsync(
    (plcinf) => {
        //reading a value from the register collection
        Console.WriteLine($"Time Value is: {registers.TestTime}");
    }
);
```
- Your properties are getting automatically updated after the initial connection

> Note! this is not your only option to read registers, see here

## Writing data registers / contacts

⚠ **Never set a register by setting the property, always use one of the provided methods**

### Synchronous

Sets the register without feedback if it was set

```C#
//inverts the boolean register
plc.SetRegister(nameof(registers.TestBool1), !registers.TestBool1);

//set the current second to the PLCs TIME register
plc.SetRegister(nameof(registers.TestTime), TimeSpan.FromSeconds(DateTime.Now.Second));

 //writes 'Test' to the PLCs string register
plc.SetRegister(nameof(registers.TestString1), "Test");
```

You can also set a register by calling its name directly (Must be either in an attached register collection or added to the list manually)

Adding registers to a manual list
```C#
plc.AddRegister<bool>(105, _name: "ManualBoolRegister");
```

Reading the value of the manually added register
```C#
//get the value as a string
string value = plc.GetRegister("ManualBoolRegister").GetValueString();
//get the value by casting
bool value2 = plc.GetRegister<BRegister>("ManualBoolRegister").Value;
//for double casted ones like numbers
var value2 = plc.GetRegister<NRegister<short>>("NumberRegister").Value;
```
