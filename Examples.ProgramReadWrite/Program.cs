namespace Examples.ProgramReadWrite;

internal class Program {
    
    static void Main(string[] args) {

        MewtocolNet.ProgramParsing.PlcBinaryProgram.ParseFromFile(@"C:\Users\feli1\Documents\Test\prog4.fp").AnalyzeProgram();

    }

}
