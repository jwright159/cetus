using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IHasIdentifiers
{
	public ProgramContext Program { get; }
	public Dictionary<string, TypedValue> Identifiers { get; }
}

public class ProgramContext : IHasIdentifiers
{
	public ProgramContext Program => this;
	public Dictionary<IFunctionContext, TypedValue?> Functions;
	public List<ITypeContext> Types;
	public Dictionary<string, TypedValue> Identifiers { get; set; }
	public List<string> Libraries = [];
}

public partial class Parser
{
	public Result ParseProgram(ProgramContext program)
	{
		List<Result> results = [];
		
		while (true)
		{
			Result result = ParseProgramStatementFirstPass(program);
			if (result is Result.ComplexRuleFailed)
				results.Add(result);
			if (result is Result.TokenRuleFailed)
				break;
		}
		
		if (!lexer.IsAtEnd)
			return new Result.TokenRuleFailed("Expected program statement", lexer.Line, lexer.Column);
		
		Console.WriteLine("Parsing type declarations...");
		foreach (ITypeContext type in program.Types)
			if (ParseTypeStatementDeclaration(program, type) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing type definitions...");
		foreach (ITypeContext type in program.Types)
			if (ParseTypeStatementDefinition(program, type) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing function declarations...");
		foreach (IFunctionContext function in program.Functions.Keys)
			if (ParseFunctionStatementDeclaration(program, function) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing function definitions...");
		foreach (IFunctionContext function in program.Functions.Keys)
			if (ParseFunctionStatementDefinition(program, function) is Result.Failure result)
				results.Add(result);
		
		return Result.WrapPassable("Invalid program", results.ToArray());
	}
}

public partial class Visitor
{
	public void VisitProgram(ProgramContext program)
	{
		foreach (ITypeContext type in program.Types)
			VisitTypeStatement(program, type);
		
		foreach (IFunctionContext function in program.Functions.Keys)
			VisitFunctionStatement(program, function);
	}
}