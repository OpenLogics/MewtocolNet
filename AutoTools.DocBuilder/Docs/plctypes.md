# PLC Type Table
Auto Generated @ **2023-07-27 17:05:59Z**

All supported PLC types for auto recognition are listed in this table. Other ones might also be supported but are shown as unknown in the library. Some models are never uniquely identifiable by their typecode and need extra hints like Prog Capacity in EXRT or RT. 

Typecode explained:
```
From left to right
0x
07 <= extended code (00 for non Mewtocol 7 devices)
0120 <= for 120k (Prog capacity), with RT/EXRT/MEW7 override order
A5 <= Is the actual typecode, with RT/EXRT/MEW7 override order
```
> <b>Discontinued PLCs</b><br>
> These are PLCs that are no longer sold by Panasonic. Marked with ⚠️

> <b>EXRT PLCs</b><br>
> These are PLCs that utilize the basic `%EE#RT` and `%EE#EX00RT` command. All newer models do this. Old models only use the `%EE#RT` command.

<table>
<tr>
<th>Type</th>
<th>Capacity</th>
<th>Code</th>
<th>Enum</th>
<th>DCNT</th>
<th>EXRT</th>
<th>Tested</th>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>ECOLOGIX</b> </td>
</tr>
<tr>
<td> ELC500 </td>
<td> 0k </td>
<td><code>0x7000010</code></td>
<td><i>ECOLOGIX_0k__ELC500</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP0</b> </td>
</tr>
<tr>
<td> C10, C14, C16 </td>
<td> 2.7k </td>
<td><code>0x000340</code></td>
<td><i>FP0_2c7k__C10_C14_C16</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> C32, SL1 </td>
<td> 5k </td>
<td><code>0x000541</code></td>
<td><i>FP0_5k__C32_SL1</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> T32 </td>
<td> 10k </td>
<td><code>0x001042</code></td>
<td><i>FP0_10c0k__T32</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP0H</b> </td>
</tr>
<tr>
<td> C32ET/EP </td>
<td> 32k </td>
<td><code>0x0032B1</code></td>
<td colspan="2"><i>FP0H_32k__C32ETsEP</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C32T/P </td>
<td> 32k </td>
<td><code>0x0032B0</code></td>
<td colspan="2"><i>FP0H_32k__C32TsP</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP0R</b> </td>
</tr>
<tr>
<td> C10, C14, C16 </td>
<td> 16k </td>
<td><code>0x001646</code></td>
<td colspan="2"><i>FP0R_16k__C10_C14_C16</i></td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> C32 </td>
<td> 32k </td>
<td><code>0x003247</code></td>
<td colspan="2"><i>FP0R_32k__C32</i></td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> F32 </td>
<td> 32k </td>
<td><code>0x003249</code></td>
<td colspan="2"><i>FP0R_32k__F32</i></td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> T32 </td>
<td> 32k </td>
<td><code>0x003248</code></td>
<td colspan="2"><i>FP0R_32k__T32</i></td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP1</b> </td>
</tr>
<tr>
<td> C14, C16 </td>
<td> 0.9k </td>
<td><code>0x000004</code></td>
<td><i>FP1_0c9k__C14_C16</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C24, C40 </td>
<td> 2.7k </td>
<td><code>0x000305</code></td>
<td><i>FP1_2c7k__C24_C40</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C56, C72 </td>
<td> 5k </td>
<td><code>0x000506</code></td>
<td><i>FP1_5k__C56_C72</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP10</b> </td>
</tr>
<tr>
<td> - </td>
<td> 30k </td>
<td><code>0x003020</code></td>
<td><i>FP10_30k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 60k </td>
<td><code>0x006020</code></td>
<td><i>FP10_60k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP10S</b> </td>
</tr>
<tr>
<td> - </td>
<td> 30k </td>
<td><code>0x003020</code></td>
<td><i>FP10S_30k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP10SH</b> </td>
</tr>
<tr>
<td> - </td>
<td> 30k </td>
<td><code>0x003030</code></td>
<td><i>FP10SH_30k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 60k </td>
<td><code>0x006030</code></td>
<td><i>FP10SH_60k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 120k </td>
<td><code>0x012030</code></td>
<td><i>FP10SH_120k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP2</b> </td>
</tr>
<tr>
<td> - </td>
<td> 16k </td>
<td><code>0x001650</code></td>
<td><i>FP2_16k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> - </td>
<td> 32k </td>
<td><code>0x003250</code></td>
<td><i>FP2_32k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP2SH</b> </td>
</tr>
<tr>
<td> - </td>
<td> 32k </td>
<td><code>0x003262</code></td>
<td><i>FP2SH_32k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> - </td>
<td> 60k </td>
<td><code>0x006060</code></td>
<td><i>FP2SH_60k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> - </td>
<td> 120k </td>
<td><code>0x012060</code></td>
<td><i>FP2SH_120k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP3</b> </td>
</tr>
<tr>
<td> - </td>
<td> 10k </td>
<td><code>0x001003</code></td>
<td><i>FP3_10k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 16k </td>
<td><code>0x001613</code></td>
<td><i>FP3_16k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP5</b> </td>
</tr>
<tr>
<td> - </td>
<td> 16k </td>
<td><code>0x001602</code></td>
<td><i>FP5_16k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 24k </td>
<td><code>0x002412</code></td>
<td><i>FP5_24k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP7</b> </td>
</tr>
<tr>
<td> CPS21 </td>
<td> 64k </td>
<td><code>0x7006409</code></td>
<td colspan="2"><i>FP7_64k__CPS21</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> CPS31 </td>
<td> 120k </td>
<td><code>0x7012005</code></td>
<td colspan="2"><i>FP7_120k__CPS31</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> CPS31E </td>
<td> 120k </td>
<td><code>0x7012004</code></td>
<td colspan="2"><i>FP7_120k__CPS31E</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> CPS31ES </td>
<td> 120k </td>
<td><code>0x7012007</code></td>
<td colspan="2"><i>FP7_120k__CPS31ES</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> CPS31S </td>
<td> 120k </td>
<td><code>0x7012008</code></td>
<td colspan="2"><i>FP7_120k__CPS31S</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> CPS41E </td>
<td> 196k </td>
<td><code>0x7019603</code></td>
<td colspan="2"><i>FP7_196k__CPS41E</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> CPS41ES </td>
<td> 196k </td>
<td><code>0x7019606</code></td>
<td colspan="2"><i>FP7_196k__CPS41ES</i></td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-C</b> </td>
</tr>
<tr>
<td> - </td>
<td> 16k </td>
<td><code>0x001613</code></td>
<td><i>FPdC_16k</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-e</b> </td>
</tr>
<tr>
<td> - </td>
<td> 2.7k </td>
<td><code>0x000345</code></td>
<td><i>FPde_2c7k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-M</b> </td>
</tr>
<tr>
<td> C16T </td>
<td> 0.9k </td>
<td><code>0x000004</code></td>
<td><i>FPdM_0c9k__C16T</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C20R, C20T, C32T </td>
<td> 2.7k </td>
<td><code>0x000305</code></td>
<td><i>FPdM_2c7k__C20R_C20T_C32T</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C20RC, C20TC, C32TC </td>
<td> 5k </td>
<td><code>0x000506</code></td>
<td><i>FPdM_5k__C20RC_C20TC_C32TC</i></td>
<td align=center>⚠️</td>
<td align=center> ❌ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-SIGMA</b> </td>
</tr>
<tr>
<td> - </td>
<td> 12k </td>
<td><code>0x001243</code></td>
<td><i>FPdSIGMA_12k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 16k </td>
<td><code>0x0016E1</code></td>
<td><i>FPdSIGMA_16k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> - </td>
<td> 32k </td>
<td><code>0x003244</code></td>
<td><i>FPdSIGMA_32k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> - </td>
<td> 40k </td>
<td><code>0x0040E1</code></td>
<td><i>FPdSIGMA_40k</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-X</b> </td>
</tr>
<tr>
<td> C40RT0A </td>
<td> 2.5k </td>
<td><code>0x00027A</code></td>
<td><i>FPdX_2c5k__C40RT0A</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C14R </td>
<td> 16k </td>
<td><code>0x001670</code></td>
<td><i>FPdX_16k__C14R</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> C14T/P </td>
<td> 16k </td>
<td><code>0x001676</code></td>
<td><i>FPdX_16k__C14TsP</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> L14 </td>
<td> 16k </td>
<td><code>0x001673</code></td>
<td><i>FPdX_16k__L14</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C30R, C60R </td>
<td> 32k </td>
<td><code>0x003271</code></td>
<td><i>FPdX_32k__C30R_C60R</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C30T/P, C60T/P, C38AT, C40T </td>
<td> 32k </td>
<td><code>0x003277</code></td>
<td><i>FPdX_32k__C30TsP_C60TsP_C38AT_C40T</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> L30, L60 </td>
<td> 32k </td>
<td><code>0x003274</code></td>
<td><i>FPdX_32k__L30_L60</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-X0</b> </td>
</tr>
<tr>
<td> L14, L30 </td>
<td> 2.5k </td>
<td><code>0x000272</code></td>
<td><i>FPdX0_2c5k__L14_L30</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> L40, L60 </td>
<td> 8k </td>
<td><code>0x000875</code></td>
<td><i>FPdX0_8k__L40_L60</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> L40, L60 </td>
<td> 16k </td>
<td><code>0x00167F</code></td>
<td><i>FPdX0_16k__L40_L60</i></td>
<td align=center>⚠️</td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td colspan="7" height=50>📟 <b>FP-XH</b> </td>
</tr>
<tr>
<td> C14R </td>
<td> 16k </td>
<td><code>0x0016A0</code></td>
<td colspan="2"><i>FPdXH_16k__C14R</i></td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> C14T/P </td>
<td> 16k </td>
<td><code>0x0016A4</code></td>
<td colspan="2"><i>FPdXH_16k__C14TsP</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C30R, C40R, C60R </td>
<td> 32k </td>
<td><code>0x0032A1</code></td>
<td colspan="2"><i>FPdXH_32k__C30R_C40R_C60R</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C30T/P, C40T, C60T/P </td>
<td> 32k </td>
<td><code>0x0032A5</code></td>
<td colspan="2"><i>FPdXH_32k__C30TsP_C40T_C60TsP</i></td>
<td align=center> ✅ </td>
<td align=center> ✅ </td>
</tr>
<tr>
<td> C38AT </td>
<td> 32k </td>
<td><code>0x0032A7</code></td>
<td colspan="2"><i>FPdXH_32k__C38AT</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C40ET, C60ET </td>
<td> 32k </td>
<td><code>0x0032AE</code></td>
<td colspan="2"><i>FPdXH_32k__C40ET_C60ET</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> C60ETF </td>
<td> 32k </td>
<td><code>0x0032AF</code></td>
<td colspan="2"><i>FPdXH_32k__C60ETF</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> M4T/L </td>
<td> 32k </td>
<td><code>0x0032A8</code></td>
<td colspan="2"><i>FPdXH_32k__M4TsL</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> M8N16T/P </td>
<td> 32k </td>
<td><code>0x0032AC</code></td>
<td colspan="2"><i>FPdXH_32k__M8N16TsP</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
<tr>
<td> M8N30T </td>
<td> 32k </td>
<td><code>0x0032AD</code></td>
<td colspan="2"><i>FPdXH_32k__M8N30T</i></td>
<td align=center> ✅ </td>
<td align=center> ❌ </td>
</tr>
</table>


