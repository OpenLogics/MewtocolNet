# Open Questions

- What is the HW information byte for?
- What are the last bytes of the EXRT message for?

# Mewtocol Init

| PLC TYPE     | CPU Code | Machine Code | HW Information|
|--------------|--------------|--------------|---------------|
| FPX C14 R | 20 | 70 | 02 |
| FPX C30 T | 20 | 77 | 02 |
| FPX-H C14 R | 20 | A0 | 01 |
| FPX-H C30 T | 20 | A5 | 01 |

## FPX 16k C14R Actual Response Examples

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 20 | Model code |
| 25 | Version |
| 16 | Prog capacity |
| 80 | Op mode |
| 00 | Link unit |
| E1 | Error flag |
| 2D00 | Self diag error |

### %EE$EX00RT Normal Operation

|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 70 | Machine type
| 00 | Version (Fixed to 00)
| 16 | Prog capacity in K
| 80 | Operation mode / status
| 00 | Link unit
| E1 | Error flag
| 2D00 | Self diag error
| 50 | Version
| 02 | Hardware information
| 0 | Number of programs
| 4100 | Program size BCD
| 1600 | Header size (no. of words) bcd
| 1604 | System register size
| 96230000001480004 | ?

What are the last bytes?

FP-X 16k C14R
96 23 00 00 00 14 80 00 4

FP-X 32k C30T/P
96 23 00 00 00 14 80 00 4

FP-XH 32k C30T/P
96 23 00 00 00 40 00 00 4

FP-XH 16k C14R
96 23 00 00 00 40 00 00 4

## FP-XH 16k C14R Actual Response Examples

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 20 | Model code |
| 16 | Version |
| 16 | Prog capacity |
| 81 | Op mode |
| 00 | Link unit |
| 60 | Error flag |
| 0000 | Self diag error |

## FP-X0 2.5k L14,L30

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 72 | Model code |
| 12 | Version |
| 02 | Prog capacity |
| 82 | Op mode |
| 00 | Link unit |
| 00 | Error flag |
| 0000 | Self diag error |

### %EE$EX00RT

|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 72 | Machine type
| 00 | Version (Fixed to 00)
| 02 | Prog capacity in K
| 82 | Operation mode / status
| 00 | Link unit
| 00 | Error flag
| 0000 | Self diag error
| 23 | Version
| 01 | Hardware information
| 0 | Number of programs
| 4100 | Program size BCD
| 0301 | Header size (no. of words) bcd
| 2819 | System register size
| 0000001480004 | ?

## FP0 2.7k C10,C14

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 05 | Model code |
| 12 | Version |
| 03 | Prog capacity |
| 82 | Op mode |
| 00 | Link unit |
| 00 | Error flag |
| 0000 | Self diag error |

### %EE$EX00RT

|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 40 | Machine type
| 00 | Version (Fixed to 00)
| 03 | Prog capacity in K
| 82 | Operation mode / status
| 00 | Link unit
| 00 | Error flag
| 0000 | Self diag error
| 23 | Version
| 01 | Hardware information
| 0 | Number of programs
| 4100 | Program size BCD
| 0301 | Header size (no. of words) bcd
| 2819 | System register size
| 20130000080070004 | ?

## FP0 5k C32,SL1

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 06 | Model code |
| 12 | Version |
| 05 | Prog capacity |
| 82 | Op mode |
| 00 | Link unit |
| 00 | Error flag |
| 0000 | Self diag error |

### %EE$EX00RT

|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 41 | Machine type
| 00 | Version (Fixed to 00)
| 03 | Prog capacity in K
| 82 | Operation mode / status
| 00 | Link unit
| 00 | Error flag
| 0000 | Self diag error
| 23 | Version
| 01 | Hardware information
| 0 | Number of programs
| 4100 | Program size BCD
| 0501 | Header size (no. of words) bcd
| 2819 | System register size
| 20130000080070004 | ?

## FP0 10k

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 42 | Model code |
| 12 | Version |
| 10 | Prog capacity |
| 82 | Op mode |
| 00 | Link unit |
| 00 | Error flag |
| 0000 | Self diag error |

### %EE$EX00RT

|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 42 | Machine type
| 00 | Version (Fixed to 00)
| 10 | Prog capacity in K
| 82 | Operation mode / status
| 00 | Link unit
| 00 | Error flag
| 0000 | Self diag error
| 23 | Version
| 01 | Hardware information
| 0 | Number of programs
| 4100 | Program size BCD
| 1001 | Header size (no. of words) bcd
| 2819 | System register size
| 20130000080070004 | ?

## FP2SH 60k

### %EE$RT

|Reponse Byte|Description|
|------------|-----------|
| 60 | Model code |
| 12 | Version |
| 00 | Prog capacity |
| 82 | Op mode |
| 00 | Link unit |
| 00 | Error flag |
| 0000 | Self diag error |

### %EE$EX00RT

|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 60 | Machine type
| 00 | Version (Fixed to 00)
| 00 | Prog capacity in K
| 82 | Operation mode / status
| 00 | Link unit
| 00 | Error flag
| 0000 | Self diag error
| 23 | Version
| 01 | Hardware information
| 0 | Number of programs
| 4100 | Program size BCD
| 6001 | Header size (no. of words) bcd
| 2819 | System register size
| 20130000000080004 | ?

# Mewtocol-7 Com

## Getting the status of the plc

`%EE#RT`

Normal plc would return the default, see above.

The FP7 series returns:

`%EE$RT0000000100000000`

> This indicates that the device uses the Mewtocol 7 format

When trying to use EXRT the FP7 returns not supported

[R] `%EE#EX00RT00`

[S] `%EE!42`

Then request the status from the PLC using Mewtocol 7

[R] `>@EEE00#30STRD00`

[O] `>@EEE00$30STRD`

|Reponse Byte|Description|
|-------------|-----------|
| 00 | Read status (always 0) |
| 07 | series code: 7 = FP7 |
| 10 | model code: 3 = CPS41E, 4 = CPS31E, 5 = CPS31, 6 = CPS41ES, 7 = CPS31ES, 8 = CPS31S, 9 = CPS21, 10 = ElC500 |
| 0000 | user special order code (Not important) |
| 0453 | latest cpu version = 4.53 |
| 0453 | communication cpu version = 4.53 |
| 0453 | operation cpu version = 4.53 |
| 01 | op mode = 4.53 |
| 00 |  error flag |
| 0000 | self diag error |
| 00 | sd card information |