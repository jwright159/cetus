namespace Cetus.Parser;

public partial class Parser(Lexer lexer)
{
	public ProgramContext Parse()
	{
		Console.WriteLine("Parsing...");
		Result result = ParseProgram(out ProgramContext context);
		if (result is not Result.Ok)
			throw new Exception(result.ToString());
		return context;
	}
}