//This program builds Markdown and docs for all kinds of data

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using MewtocolNet;
using MewtocolNet.DocAttributes;

Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("Building docs for PLC types...");

var entryLoc = Assembly.GetEntryAssembly();
ArgumentNullException.ThrowIfNull(entryLoc);

string filePath = Path.Combine(entryLoc.Location, @"..\..\..\..\Docs\plctypes.md");
Console.WriteLine($"{filePath}");   

StringBuilder markdownBuilder = new StringBuilder(); 

var plcs = Enum.GetValues(typeof(PlcType)).Cast<PlcType>().OrderBy(x => x.ToString());

void WritePlcTypeTable(IEnumerable<PlcType> vals) {

    var groups = vals.GroupBy(x => x.ToNameDecompose()[0].Group)
                     .SelectMany(grouping => grouping.OrderBy(b => (int)b))
                     .GroupBy(
                        x => string.Join(", ", 
                        x.ToNameDecompose()
                         .DistinctBy(y => y.Group)
                         .Select(y => y.Group))
                      );

    markdownBuilder.AppendLine("<table>");

    bool isFirstIt = true;

    foreach (var group in groups) {

        group.OrderBy(x => (int)x);

        bool isFirstGroup = true;

        foreach (var enu in group) {

            ParsedPlcName[] decomposed = null!;
            string cpuOrMachCode = null!;

            decomposed = enu.ToNameDecompose();

            cpuOrMachCode = ((int)enu).ToString("X2");
            ArgumentNullException.ThrowIfNull(decomposed);

            //first iteration
            if (isFirstIt) {

                markdownBuilder.AppendLine("<tr>");

                markdownBuilder.AppendLine($"<th>Type</th>");
                markdownBuilder.AppendLine($"<th>Capacity</th>");
                markdownBuilder.AppendLine($"<th>Code</th>");
                markdownBuilder.AppendLine($"<th>Enum</th>");
                markdownBuilder.AppendLine($"<th>DCNT</th>");
                markdownBuilder.AppendLine($"<th>EXRT</th>");
                markdownBuilder.AppendLine($"<th>Tested</th>");

                markdownBuilder.AppendLine("</tr>");

                isFirstIt = false;

            }

            if(isFirstGroup) {

                markdownBuilder.AppendLine("<tr>");

                markdownBuilder.AppendLine($"<td colspan=\"7\" height=50>📟 <b>{group.Key}</b> </td>");

                markdownBuilder.AppendLine("</tr>");

                isFirstGroup = false;

            }

            foreach (var decomp in decomposed) {

                markdownBuilder.AppendLine("<tr>");

                markdownBuilder.AppendLine($"<td> {(decomp.SubTypes.Length == 0 ? "-" : string.Join(", ", decomp.SubTypes))} </td>");
                markdownBuilder.AppendLine($"<td> {decomp.Size}k </td>");
                markdownBuilder.AppendLine($"<td><code>0x{cpuOrMachCode}</code></td>");
                
                if(enu.IsDiscontinued()) {

                    markdownBuilder.AppendLine($"<td><i>{enu.ToString()}</i></td>");
                    markdownBuilder.AppendLine($"<td align=center>⚠️</td>");

                } else {

                    markdownBuilder.AppendLine($"<td colspan=\"2\"><i>{enu.ToString()}</i></td>");
                
                }

                markdownBuilder.AppendLine($"<td align=center> {(enu.IsEXRTPLC() ? "✅" : "❌")} </td>");
                markdownBuilder.AppendLine($"<td align=center> {(enu.WasTestedLive() ? "✅" : "❌")} </td>");

                markdownBuilder.AppendLine("</tr>");

            }


        }

        isFirstIt = false;

    }

    markdownBuilder.AppendLine("</table>");
    markdownBuilder.AppendLine("\n");

}

markdownBuilder.AppendLine($"# PLC Type Table");
markdownBuilder.AppendLine($"All supported PLC types for auto recognition are listed in this table. " +
                           $"Other ones might also be supported but are shown as unknown in the library");

markdownBuilder.AppendLine($"> <b>Discontinued PLCs</b><br>");
markdownBuilder.AppendLine($"> These are PLCs that are no longer sold by Panasonic. Marked with ⚠️\n");

markdownBuilder.AppendLine($"> <b>EXRT PLCs</b><br>");
markdownBuilder.AppendLine($"> These are PLCs that utilize the basic `%EE#RT` and `%EE#EX00RT` command. All newer models do this. Old models only use the `%EE#RT` command.\n");

WritePlcTypeTable(plcs);


File.WriteAllText(filePath, markdownBuilder.ToString());