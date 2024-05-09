using Cetus;

Lexer lexer = new(File.ReadAllText("recursive.cetus"));
Parser parser = new(lexer);

Parser.ProgramContext program = parser.Parse();
Visitor visitor = new();
visitor.Generate(program);
Console.WriteLine();

// visitor.Optimize();
// visitor.Dump();
// Console.WriteLine();

visitor.Compile();

visitor.Dispose();