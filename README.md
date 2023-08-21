[![Publish pipeline](https://github.com/WOmed/MewtocolNet/actions/workflows/publish-pipeline.yml/badge.svg)](https://github.com/WOmed/MewtocolNet/actions/workflows/publish-pipeline.yml)
[![Test pipeline](https://github.com/WOmed/MewtocolNet/actions/workflows/test-pipeline.yml/badge.svg)](https://github.com/WOmed/MewtocolNet/actions/workflows/test-pipeline.yml)
[![GitHub tag](https://img.shields.io/github/v/tag/WOmed/MewtocolNet?label=Package%20Version)](https://github.com/WOmed/MewtocolNet/pkgs/nuget/Mewtocol.NET)
[![gencov](../badges/Builds/TestResults/badge_combined_master.svg)](https://htmlpreview.github.io/?https://github.com/WOmed/MewtocolNet/blob/badges/Builds/TestResults/summary_master.html)
![GitHub](https://img.shields.io/github/license/WOmed/MewtocolNet?label=License)
![Status](https://img.shields.io/badge/Status-In%20dev-orange)

# MewtocolNet
An easy to use Mewtocol protocol library to interface with Panasonic PLCs over TCP/Serial.

> ⚠️ This library is not an official panasonic product nor does panasonic provide financial support or limitations in any form. 
> This software was written by WOLF Medizintechnik GmbH (@WOmed/dev).

# PLC Support

## For a full list check [this table](../master_auto_docs/plctypes.md)

> This library was only tested with a few PLCs, other types that support the Panasonic Mewtocol protocol might work. 
> Use at your own risk, others might follow with community feedback

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

# Installation

Use the dotnet CLI and run 
```Shell
dotnet add package Mewtocol.NET
```

# Protocol description

Panasonic has published a [protocol definition](https://mediap.industry.panasonic.eu/assets/custom-upload/Factory%20&%20Automation/PLC/Manuals/mn_all_plcs_mewtocol_user_pidsx_en.pdf) on their site.
Refer to this site if you want to see the general functionality or add / report missing features.

> This library is at the time not feature complete, but all essential features are provided

# Examples

To see a full list of examples [click here](/Examples).

## Connecting to a PLC 

Connecting to a PLC is as simple as 

```C#
using (var plc = Mewtocol.Ethernet("192.168.178.55").Build()) {

    await plc.ConnectAsync();
    if (!plc.IsConnected) {
        Console.WriteLine("Failed to connect to the plc...");
    } else {
        Console.WriteLine(plc.PlcInfo);
    }

}
```

## Reading data registers / contacts

[Detailed instructions](https://github.com/WOmed/MewtocolNet/wiki/Attribute-handled-reading)

- Create a new class that inherits from `RegisterCollection`

```C#
public class TestRegisters : RegisterCollection {

    //corresponds to a R100 boolean register in the PLC
    [Register("R100")]
    public bool TestBool1 { get; private set; }

    //corresponds to a XD input of the PLC
    [Register("XD")]
    public bool TestBoolInputXD { get; private set; } 

    //corresponds to a DDT7012 - DDT7013 as a 32bit time value that gets parsed as a timespan (TIME)
    //the smallest value to communicate to the PLC is 10ms
    [Register("DDT7012")]
    public TimeSpan TestTime { get; private set; }  
    
    //corresponds to a DT1101 - DT1104 string register in the PLC with (STRING[4])
    [Register("DT1101", "STRING[4]")]
    public string TestString1 { get; private set; }

}
```

- Connect to the PLC and attach the register collection and logger
- attach an automatic poller by chaining `.WithPoller()` after the register attachment

```C#

TestRegisters registers = null;

//setting up a new PLC serial interface and tell it to use the register collection
var plc = Mewtocol.Serial("COM4", BaudRate._19200)
.WithPoller()
.WithRegisterCollections(c => {
    registers = c.AddCollection<TestRegisters>();
    // or use
    // c.AddCollection(new TestRegisters());
    // if you want to pass data to a constructor
})
.Build();

//connect to it
await plc.ConnectAsync(async () => {

    //restart the plc program during the connection process
    await plc.RestartProgramAsync();

});

//wait for the first data cycle of the poller module
//otherwise the property value might still be unset or null
await App.ViewModel.Plc.AwaitFirstDataCycleAsync();

if (App.ViewModel.Plc.IsConnected) {

    Console.WriteLine(registers.TestBool1);

}

```
- Your properties are getting automatically updated after the initial connection

> Note! this is not your only option to read registers, see here

## Reading & Writing

In addition to the automatic property binding you can use these patterns:

### Reading & Writing by using the anonymous builder pattern

```C#
await plc.Register.Struct<short>("DT100").WriteAsync(100);

var value = await plc.Register.Struct<short>("DT100").ReadAsync();
```
### Reading & Writing by using the direct reference from the builder pattern

```C#

IRegister<bool> outputContactReference;

var plc = Mewtocol.Ethernet("127.0.0.1")
.WithRegisters(b => {

    b.Bool("Y4").Build(out outputContactReference);

})
.Build();

await plc.ConnectAsync();

await outputContactReference.WriteAsync(true);
```
