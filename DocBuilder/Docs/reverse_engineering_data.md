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

### %EE$EX00RT with error
 
|Reponse Byte|Description|
|------------|-----------|
| 00 | Extended mode
| 32 | Data item count
| 70 | Machine type
| 00 | Version (Fixed to 00)
| 16 | Prog capacity in K
| 81 | Operation mode / status
| 00 | Link unit
| 60 | Error flag
| 0000 | Self diag error
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