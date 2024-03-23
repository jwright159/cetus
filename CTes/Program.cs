using Antlr4.Runtime;
using CTes;
using CTes.Antlr;

AntlrFileStream stream = new("sample.ctes");
CTesLexer lexer = new(stream);
CTesParser parser = new(new CommonTokenStream(lexer));

CTesParser.ProgramContext program = parser.program();
CodeGenerator visitor = new();
visitor.Generate(program);
visitor.Dump();
// visitor.Optimize();
// visitor.Dump();
Console.WriteLine();
visitor.CompileAndRun();
visitor.Dispose();