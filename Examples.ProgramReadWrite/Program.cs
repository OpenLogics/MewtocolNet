using MewtocolNet;
using MewtocolNet.Logging;

namespace Examples.ProgramReadWrite;

internal class Program {

    static void Main(string[] args) => Task.Run(AsyncMain).Wait();

    //MewtocolNet.ProgramParsing.PlcBinaryProgram.ParseFromFile(@"C:\Users\fwe\Documents\sps\FPXH_C30_Test1.fp").AnalyzeProgram();

    static async Task AsyncMain () {

        Logger.LogLevel = LogLevel.Error;

        using (var plc = Mewtocol.Ethernet("192.168.115.210").Build()) {

            await plc.ConnectAsync();
            var prog = await plc.ReadProgramAsync();

            if (prog != null) {

                prog.AnalyzeProgram();

            }

        }

    }

}
