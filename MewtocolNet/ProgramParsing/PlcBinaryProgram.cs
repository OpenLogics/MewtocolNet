using System.Text;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MewtocolNet.ProgramParsing {

    public class PlcBinaryProgram {

        static readonly Dictionary<ushort, string> stepCommands = new Dictionary<ushort, string> {
            { 0x00FF, "SUB 0" },

            { 0xB0FF, "DF" },
            { 0xFAF8, "ED" },
            { 0xFDF8, "RET" }, //return
            { 0xF6F8, "CALL" },
            
            { 0x21CC, "OT R501" },
            { 0x21AC, "ST R501" },
            { 0xF7FF, "ST R9010" }, 

        };

        // ST R50_1 21 AC => WR system area start was set to 50
        // ST R57_1 91 AC => WR system area start was set to 57
        // ST R901_3 F7 FF 53 A9
        // ST R901_C F7 FF 5C A9
        // AN/ R900_B F7 FF 4B 49

        static readonly Dictionary<string, string> stepFunctions = new Dictionary<string, string> {
            { "F0", "MV" },
            { "F1", "DMV" },
            { "F2", "MVN" },
            { "F3", "DMVN" },
            { "F5", "BTM" },
            { "F8", "DMV2" },
            { "F11", "COPY" },
            { "F61", "DCMP" },
            { "F246", "CALL" },
        };

        const int STEP_BYTE_LEN = 2;

        const int PROG_AUTHOR_INDEX = 57;
        const int PROG_AUTHOR_MAX_CHARS = 12;

        const int PROG_DESCRIPTION_INDEX = 17;
        const int PROG_DESCRIPTION_MAX_CHARS = 40;

        const int PROG_SIZE_START = 1139;
        const int PROG_DATA_START = 1179;

        public List<byte[]> rawSteps;

        public string Author { get; internal set; }  

        public string Description { get; internal set; }

        public static PlcBinaryProgram ParseFromFile (string path) {

            var retInstance = new PlcBinaryProgram();

            var rawBytes = File.ReadAllBytes(path);

            if (rawBytes.Length < 2 || (rawBytes[0] != 0x46 && rawBytes[1] != 0x50))
                throw new NotSupportedException("The loaded file was no FP file");

            var rawString = Encoding.ASCII.GetString(rawBytes);

            //get author and description
            var progAuthor = Encoding.ASCII.GetString(rawBytes, PROG_AUTHOR_INDEX, PROG_AUTHOR_MAX_CHARS).Replace("\0", "");
            var progDescription = Encoding.ASCII.GetString(rawBytes, PROG_DESCRIPTION_INDEX, PROG_DESCRIPTION_MAX_CHARS).Replace("\0", "");
            var progSizeSteps = BitConverter.ToInt32(rawBytes, PROG_SIZE_START);

            //improve performance later
            var steps = new List<byte[]>();

            for (int i = 0; i < progSizeSteps; i++) {

                //00-FF F8 => 0-255 default function
                //FF F8 => extended function, look for next step

                var step = rawBytes.Skip(PROG_DATA_START + (i * STEP_BYTE_LEN)).Take(STEP_BYTE_LEN).ToArray();

                steps.Add(step);

            }

            retInstance.rawSteps = steps;

            return retInstance;

        }

        public void AnalyzeProgram () {

            for (int i = 0; i < rawSteps.Count; i++) {

                var step = rawSteps[i];

                var stepAscii = Encoding.ASCII.GetString(step);
                var stepBytesString = string.Join(" ", step.Select(x => x.ToString("X2")));

                Console.Write($"{i,3} => {stepBytesString} ");

                var stepKey = BitConverter.ToUInt16(step.Reverse().ToArray(), 0);

                if (stepCommands.ContainsKey(stepKey)) {

                    Console.Write($"{stepCommands[stepKey]}");

                } else if (stepKey == 0xFFF8) {

                    //custom function that goes over FF, the F instruction number is calced by
                    //the next step first byte plus 190

                    var nextStep = rawSteps[i + 1];
                    var stepID = nextStep[0] + 190;

                    string funcName = GetFunctionName($"F{stepID}");
                    Console.Write(funcName);

                } else if (step[1] == 0xF8) {

                    string funcName = GetFunctionName($"F{step[0]}");
                    Console.Write(funcName);

                }

                //if (stepBytesString.StartsWith("F")) {
                //    Console.Write(" STEP COMMAND");
                //}

                Console.WriteLine();

            }


            Console.WriteLine();

        }

        private string GetFunctionName (string funcName) {

            return stepFunctions.ContainsKey(funcName) ? $"{funcName} ({stepFunctions[funcName]})" : funcName; 

        } 

    }

}
