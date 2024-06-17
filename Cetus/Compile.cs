using Cetus.Parser;
using Cetus.Parser.Types.Program;

namespace Cetus;

public static class Compile
{
	public static void Main(string[] args)
	{
		Lexer lexer = new(File.ReadAllText("recursive.cetus"));
		Parser.Parser parser = new(lexer);
		Program program = parser.Parse();
		Visitor visitor = new();
		visitor.Visit(program);
		Console.WriteLine();

		visitor.Optimize();
		visitor.Dump();
		Console.WriteLine();

		visitor.Compile(program);

		visitor.Dispose();
	}
}