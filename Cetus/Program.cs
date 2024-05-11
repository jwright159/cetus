using Cetus.Parser;

Lexer lexer = new(File.ReadAllText("recursive.cetus"));
Parser parser = new(lexer);

parser.Generate();
Console.WriteLine();

parser.Optimize();
parser.Dump();
Console.WriteLine();

parser.Compile();

parser.Dispose();