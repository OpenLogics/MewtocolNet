3 byte system registers, read with RR

|RR Adress|Interpreting type|Description|
|-|-|-|
|RR000|uint16|Program size steps capacity|
|RR005|uint16|Start address counter|
|RR006|uint16|Start address timer/counter|
|RR007|uint16|Start WR area (self reliant)|
|RR008 - RR009|uint32|Start DT area (self reliant)|

4 byte / 1 word system registers read with R

|WR Adress|Interpreting type|Description|
|-|-|-|
|R900|uint16|Self diag error|
|R902|uint16|Mode info|