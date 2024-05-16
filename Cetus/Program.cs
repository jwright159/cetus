using Cetus.Parser;

Lexer lexer = new(File.ReadAllText("recursive.cetus"));
Parser parser = new(lexer);
ProgramContext program = parser.Parse();
Visitor visitor = new();
visitor.Visit(program);
Console.WriteLine();

visitor.Optimize();
visitor.Dump();
Console.WriteLine();

visitor.Compile(program);

visitor.Dispose();