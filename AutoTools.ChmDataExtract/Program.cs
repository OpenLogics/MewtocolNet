using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MewtocolNet;
using MewtocolNet.Helpers;

namespace AutoTools.ChmDataExtract;

internal class Program {

    const string sysVarsLoc = @"Panasonic-ID SUNX Control\Control FPWIN Pro 7\Mak\Res_Eng\SysVars.chm";
    const string funcNamesLoc = @"Panasonic-ID SUNX Control\Control FPWIN Pro 7\Mak\Res_Eng\FPWINPro.chm";

    static Dictionary<string, List<PlcType>> plcGroups = new() { 
        { "FP7 CPS41/31 E/ES", new List<PlcType> {
            PlcType.FP7_120k__CPS31E,
            PlcType.FP7_196k__CPS41E,
            PlcType.FP7_120k__CPS31ES,
            PlcType.FP7_196k__CPS41ES,
        }},
        { "FP7 CPS31/31S", new List<PlcType> {
            PlcType.FP7_120k__CPS31,
            PlcType.FP7_120k__CPS31S,
        }},
        { "FP7 CPS21", new List<PlcType> {
            PlcType.FP7_64k__CPS21,
        }},
        { "ELC500", new List<PlcType> {
            PlcType.ECOLOGIX_0k__ELC500,
        }},
        { "FP-SIGMA 12k", new List<PlcType> {
            PlcType.FPdSIGMA_12k,
            PlcType.FPdSIGMA_16k,
        }},
        { "FP-SIGMA 32k", new List<PlcType> {
            PlcType.FPdSIGMA_32k,
            PlcType.FPdSIGMA_40k,
        }},
        { "FP0R 16k/32k C types", new List<PlcType> {
            PlcType.FP0R_16k__C10_C14_C16,
            PlcType.FP0R_32k__C32,
        }},
        { "FP0R 32k T32", new List<PlcType> {
            PlcType.FP0R_32k__T32,
            PlcType.FP0R_32k__F32,
        }},
        { "FP2 16k", new List<PlcType> {
            PlcType.FP2_16k,
        }},
        { "FP2 32k", new List<PlcType> {
            PlcType.FP2_32k,
        }},
        { "FP2SH 32k/60k/120k", new List<PlcType> {
            PlcType.FP2SH_60k,
            PlcType.FP2SH_60k,
            PlcType.FP2SH_120k,
        }},
        { "FP-X 16k/32k R-types", new List<PlcType> {
            PlcType.FPdX_16k__C14R,
            PlcType.FPdX_32k__C30R_C60R,
        }},
        { "FP-X 16k/32k T-types", new List<PlcType> {
            PlcType.FPdX_16k__C14TsP,
            PlcType.FPdX_32k__C30TsP_C60TsP_C38AT_C40T,
        }},
        {"FP0H C32T/P ET/EP", new List<PlcType> {
            PlcType.FP0H_32k__C32TsP,
            PlcType.FP0H_32k__C32ETsEP,
        }},
        { "FP-X 16k/32k L-types", new List<PlcType> {
            PlcType.FPdX_16k__L14,
            PlcType.FPdX_32k__L30_L60,
        }},
        { "FP-X 2.5k C40RT0A", new List<PlcType> {
            PlcType.FPdX_2c5k__C40RT0A,
        }},
        { "FP-X0 2.5k L14,L30", new List<PlcType> {
            PlcType.FPdX0_2c5k__L14_L30,
        }},
        { "FP-X0 8k L40,L60", new List<PlcType> {
            PlcType.FPdX0_8k__L40_L60,
        }},
        { "FP-e 2.7k", new List<PlcType> {
            PlcType.FPde_2c7k,
        }},
        { "FP-XH 16k/32k R-types", new List<PlcType> {
            PlcType.FPdXH_16k__C14R,
            PlcType.FPdXH_32k__C30R_C40R_C60R,
        }},
        { "FP-XH 16k/32k T-types", new List<PlcType> {
            PlcType.FPdXH_16k__C14TsP,
            PlcType.FPdXH_32k__C30TsP_C40T_C60TsP,
            PlcType.FPdXH_32k__C30TsP_C40T_C60TsP,
            PlcType.FPdXH_32k__C38AT,
            PlcType.FPdXH_32k__C40ET_C60ET,
            PlcType.FPdXH_32k__C60ETF,
        }},
        { "FP-XH M4/M8 types", new List<PlcType> {
            PlcType.FPdXH_32k__M4TsL,
            PlcType.FPdXH_32k__M8N16TsP,
            PlcType.FPdXH_32k__M8N30T,
        }},
    };

    class AddressException {

        public string ExceptionTitle;

        public string ForSysRegister;

    }

    internal class FPFunction {

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RedundantName { get; set; } = null!;

        public string Description { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? ParametersIn { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string[]>? ParametersOut { get; set; }

    }

    static void Main(string[] args) => Task.Run(AsyncMain).Wait();
    
    static async Task AsyncMain () {

        GetFunctionNames();
        //await GetSystemRegisters();

    }

    static void GetFunctionNames () {

        var functions = new Dictionary<string, FPFunction>();

        var progLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var progFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var sysVarsPath = Path.Combine(progFilesPath, funcNamesLoc);

        Directory.SetCurrentDirectory(progLoc);
        File.Copy(sysVarsPath, "./FPWINPro.chm", true);

        var startInfo = new ProcessStartInfo {
            WorkingDirectory = progLoc,
            FileName = "hh.exe",
            Arguments = $"-decompile ./DecompFuncs ./FPWINPro.chm",
        };

        //call the hh.exe decompiler for chm
        if (!File.Exists("./DecompFuncs/topics/availability.html")) {
            var proc = Process.Start(startInfo)!;
            proc.WaitForExit();
        }

        var doc = new HtmlDocument();
        doc.Load("./DecompFuncs/topics/availability.html");

        //[contains(@class, 'table mainbody')]
        foreach (HtmlNode table in doc.DocumentNode.SelectNodes("//table[1]")) {

            var rows = table?.SelectSingleNode("tbody")?.SelectNodes("tr");
            if (rows == null) continue;

            foreach (var row in rows) {

                var columns = row.SelectNodes("td");
                if (columns == null) continue;

                var itemRow = columns?.FirstOrDefault()?.SelectSingleNode("p/a[contains(@class,'xref')]");

                string rowName = itemRow?.InnerText ?? "Unnamed";

                if (!Regex.IsMatch(rowName, @"^F[0-9]{1,3}_.*$")) continue;

                FPFunction functionIns = new FPFunction();
                
                Console.Write($"Var: {rowName, -20}");
                
                var href = itemRow?.GetAttributeValue("href", null);

                if (href != null) {
                    
                    //Console.Write($" {href}");

                    var docSub = new HtmlDocument();
                    docSub.Load($"./DecompFuncs{href}");

                    var noteSection = docSub.DocumentNode.SelectSingleNode("//section/div[contains(@class,'note note')]");
                    var xrefRedundant = noteSection?.SelectSingleNode("p/a[contains(@class,'xref')]");
                    var xrefNodeContent = noteSection?.SelectSingleNode("p/span");
                    
                    //get params in / out
                    var inOutDefinitionNodes = docSub.DocumentNode.SelectNodes("//p[contains(@class,'p inoutput')]");

                    if (inOutDefinitionNodes != null) {

                        foreach (var ioTypeNode in inOutDefinitionNodes) {

                            var nodeInOutType = ioTypeNode.InnerText.SanitizeLinebreakFormatting();

                            Console.Write($"{nodeInOutType}: ");

                            var currentSibling = ioTypeNode;

                            while (true) {

                                if (currentSibling.NextSibling == null) break;
                                currentSibling = currentSibling.NextSibling;

                                if (currentSibling.HasClass("inoutput")) {
                                    break;
                                }

                                var paramNodes = currentSibling.SelectNodes("dt");

                                if (paramNodes == null) continue;

                                foreach (var paramNode in paramNodes) {

                                    var paramName = paramNode.SelectSingleNode("span[1]")?.InnerText?.SanitizeBracketFormatting();
                                    var paramTypes = paramNode.SelectSingleNode("span[2]")?.InnerText?.SanitizeBracketFormatting();

                                    if (paramName != null && paramTypes != null) {

                                        if (functionIns.ParametersIn == null)
                                            functionIns.ParametersIn = new Dictionary<string, string[]>();

                                        if (functionIns.ParametersOut == null)
                                            functionIns.ParametersOut = new Dictionary<string, string[]>();

                                        Console.Write($"{paramName} {paramTypes}");

                                        var splitParamNames = paramName.Split(", ");

                                        foreach (var splitName in splitParamNames) {

                                            if (nodeInOutType == "Input") {

                                                if (functionIns.ParametersIn.ContainsKey(splitName)) break;
                                                functionIns.ParametersIn.Add(splitName, paramTypes.SanitizeBracketFormatting().Split(", "));

                                            } else {

                                                if (functionIns.ParametersOut.ContainsKey(splitName)) break;
                                                functionIns.ParametersOut.Add(splitName, paramTypes.SanitizeBracketFormatting().Split(", "));

                                            }

                                        }

                                    }

                                }

                            }

                        }

                    }

                    HtmlNode? descrSection = null;

                    if (xrefRedundant != null && xrefNodeContent != null && xrefNodeContent.InnerText.StartsWith("This is a redundant F instruction")) {

                        descrSection = docSub.DocumentNode.SelectSingleNode("//section[2]");

                        functionIns.RedundantName = xrefRedundant.InnerText;

                        //Console.Write($"{xrefRedundant.InnerText}");

                    } else {

                        descrSection = docSub.DocumentNode.SelectSingleNode("//section[1]");

                    }

                    if (descrSection != null) {

                        var descrText = descrSection?.InnerText;

                        if(descrText != null) {

                            descrText = descrText.SanitizeLinebreakFormatting();

                            functionIns.Description = descrText;

                            //Console.Write($" {descrText}");

                        }

                    }

                }

                functions.Add(rowName, functionIns);
                Console.WriteLine();

                //compatibility matrix
                //for (int i = 1; i < columns?.Count - 1; i++) {

                //    bool isChecked = columns?.ElementAtOrDefault(i)?.SelectSingleNode("p")?.InnerHtml != "";

                //    Console.Write($"{(isChecked ? "1" : "0")}, ");

                //}

            }

        }

        var funcsJson = JsonSerializer.Serialize(functions, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText("./function_names.json", funcsJson);


    }

    static async Task GetSystemRegisters () {

        var addressExceptions = new List<AddressException>();

        var progLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var progFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var sysVarsPath = Path.Combine(progFilesPath, sysVarsLoc);

        Directory.SetCurrentDirectory(progLoc);
        File.Copy(sysVarsPath, "./SysVars.chm", true);

        var startInfo = new ProcessStartInfo {
            WorkingDirectory = progLoc,
            FileName = "hh.exe",
            Arguments = $"-decompile ./Decomp ./SysVars.chm",
        };

        //call the hh.exe decompiler for chm
        if (!File.Exists("./Decomp/topics/availability.html")) {
            var proc = Process.Start(startInfo)!;
            proc.WaitForExit();
        }

        var doc = new HtmlDocument();
        doc.Load("./Decomp/topics/availability.html");

        //[contains(@class, 'table mainbody')]
        foreach (HtmlNode table in doc.DocumentNode.SelectNodes("//table[1]")) {

            var rows = table?.SelectSingleNode("tbody")?.SelectNodes("tr");
            if (rows == null) continue;

            string lastRegisterName = "Name";

            int iSystemRegister = 0;

            foreach (var row in rows) {

                var columns = row.SelectNodes("td");
                if (columns == null) continue;

                //get var name
                var varNameNode = columns?.FirstOrDefault()?.SelectSingleNode("p/a[contains(@class,'xref')]");

                string registerAddress;
                int iterateStart;

                if (varNameNode != null) {

                    lastRegisterName = varNameNode.InnerText;

                    //get second col
                    var regAddressNode = columns?.ElementAtOrDefault(1)?.SelectSingleNode("p");
                    registerAddress = regAddressNode?.InnerText ?? "Null";
                    iterateStart = 2;

                } else {

                    //get first col
                    var regAddressNode = columns?.ElementAtOrDefault(0)?.SelectSingleNode("p");
                    registerAddress = regAddressNode?.InnerText ?? "Null";
                    iterateStart = 1;
                }

                //filter the address for annotations
                var regexAnnotation = new Regex(@"\(.*\)");
                var matchAnnotation = regexAnnotation.Match(registerAddress);
                if (matchAnnotation.Success) {

                    registerAddress = regexAnnotation.Replace(registerAddress, "");

                    addressExceptions.Add(new AddressException {
                        ForSysRegister = lastRegisterName,
                        ExceptionTitle = matchAnnotation.Value,
                    });

                }

                Console.Write($"Var: {lastRegisterName} | {registerAddress} ".PadRight(100, ' '));

                for (int i = iterateStart, j = 0; i < columns?.Count + 1; i++) {

                    if (j >= plcGroups.Count - 1) continue;

                    var group = plcGroups.Keys.ToList()[j];

                    bool isChecked = columns?.ElementAtOrDefault(i)?.SelectSingleNode("p")?.InnerHtml != "";

                    Console.Write($"{(isChecked ? "1" : "0")}, ");

                    j++;

                }

                Console.WriteLine();

            }

        }

    }

}
