//This program builds Markdown and docs for all kinds of data

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using MewtocolNet;

Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("Building docs for PLC types...");

var entryLoc = Assembly.GetEntryAssembly();
ArgumentNullException.ThrowIfNull(entryLoc);

string filePath = null!;

if (args.Length == 0) {

    filePath = Path.Combine(entryLoc.Location, @"..\..\..\..\Docs\plctypes.md");

} else {

    filePath = args[0];

}

Console.WriteLine($"{filePath}");

StringBuilder markdownBuilder = new StringBuilder();

var plcNames = Enum.GetNames<PlcType>().OrderBy(x => x).ToArray();

void WritePlcTypeTable(string[] names) {

    var groups = names.Select(x => x.ToNameDecompose())
                      .GroupBy(x => x.Group)
                      .SelectMany(g => g.OrderBy(x => x.Size))
                      .GroupBy(x => x.Group);

    markdownBuilder.AppendLine("<table>");

    bool isFirstIt = true;

    foreach (var group in groups) {

        group.OrderBy(x => x.TypeCode);

        bool isFirstGroup = true;

        foreach (var enu in group) {

            string cpuOrMachCode = null!;

            cpuOrMachCode = enu.TypeCode.ToString("X6");
            ArgumentNullException.ThrowIfNull(enu);

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

            if (isFirstGroup) {

                markdownBuilder.AppendLine("<tr>");

                markdownBuilder.AppendLine($"<td colspan=\"7\" height=50>📟 <b>{group.Key}</b> </td>");

                markdownBuilder.AppendLine("</tr>");

                isFirstGroup = false;

            }

            markdownBuilder.AppendLine("<tr>");

            markdownBuilder.AppendLine($"<td> {(enu.SubTypes.Length == 0 ? "-" : string.Join(", ", enu.SubTypes))} </td>");
            markdownBuilder.AppendLine($"<td> {enu.Size}k </td>");
            markdownBuilder.AppendLine($"<td><code>0x{cpuOrMachCode}</code></td>");

            if (enu.IsDiscontinuedModel) {

                markdownBuilder.AppendLine($"<td><i>{enu.EncodedName}</i></td>");
                markdownBuilder.AppendLine($"<td align=center>⚠️</td>");

            } else {

                markdownBuilder.AppendLine($"<td colspan=\"2\"><i>{enu.EncodedName}</i></td>");

            }

            markdownBuilder.AppendLine($"<td align=center> {(enu.UsesEXRT ? "✅" : "❌")} </td>");
            markdownBuilder.AppendLine($"<td align=center> {(enu.WasTestedLive ? "✅" : "❌")} </td>");

            markdownBuilder.AppendLine("</tr>");


        }

        isFirstIt = false;

    }

    markdownBuilder.AppendLine("</table>");
    markdownBuilder.AppendLine("\n");

}

markdownBuilder.AppendLine($"# PLC Type Table");
markdownBuilder.AppendLine($"Auto Generated @ **{DateTime.Now.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)}**\n");
markdownBuilder.AppendLine(
$"All supported PLC types for auto recognition are listed in this table. " +
$"Other ones might also be supported but are shown as unknown in the library. " +
$"Some models are never uniquely identifiable by their typecode and need extra hints like Prog Capacity in EXRT or RT. \n\n" +
$"Typecode explained:\n" +
$"```\n" +
$"From left to right\n" +
$"0x\n" +
$"07 <= extended code (00 non mewtocol 7 devices)\n" +
$"20 <= Is hex for 32 (Prog capacity)\n" +
$"A5 <= Is the actual typecode, can overlap with others\n" +
$"```"
);

markdownBuilder.AppendLine($"> <b>Discontinued PLCs</b><br>");
markdownBuilder.AppendLine($"> These are PLCs that are no longer sold by Panasonic. Marked with ⚠️\n");

markdownBuilder.AppendLine($"> <b>EXRT PLCs</b><br>");
markdownBuilder.AppendLine($"> These are PLCs that utilize the basic `%EE#RT` and `%EE#EX00RT` command. All newer models do this. Old models only use the `%EE#RT` command.\n");

WritePlcTypeTable(plcNames);

File.WriteAllText(filePath, markdownBuilder.ToString());