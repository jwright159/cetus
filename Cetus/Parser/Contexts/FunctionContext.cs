using Cetus.Parser.Values;

namespace Cetus.Parser.Contexts;

public class FunctionContext : IHasIdentifiers
{
	public ProgramContext Program { get; }
	public Dictionary<string, TypedValue> Identifiers { get; }
	
	public FunctionContext(ProgramContext program)
	{
		Program = program;
		Identifiers = new Dictionary<string, TypedValue>(program.Identifiers);
	}
	
	public FunctionContext(FunctionContext function)
	{
		Program = function.Program;
		Identifiers = new Dictionary<string, TypedValue>(function.Identifiers);
	}
}