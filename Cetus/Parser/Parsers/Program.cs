using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<ITypeContext> Types { get; set; }
}

public static class IHasIdentifiersExtensions
{
	public static void NestFrom(this IHasIdentifiers child, IHasIdentifiers parent)
	{
		child.Identifiers = new NestedDictionary<string, TypedValue>(parent.Identifiers);
		child.Functions = new NestedCollection<IFunctionContext>(parent.Functions);
		child.Types = new NestedCollection<ITypeContext>(parent.Types);
	}
}

public class ProgramContext : IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new Dictionary<string, TypedValue>();
	public ICollection<IFunctionContext> Functions { get; set; } = new List<IFunctionContext>();
	public ICollection<ITypeContext> Types { get; set; } = new List<ITypeContext>();
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
			if (ParseTypeStatementDeclaration(type) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing type definitions...");
		foreach (ITypeContext type in program.Types)
			if (ParseTypeStatementDefinition(type) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing function declarations...");
		foreach (IFunctionContext function in program.Functions)
			if (ParseFunctionStatementDeclaration(function) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing function definitions...");
		foreach (IFunctionContext function in program.Functions)
			if (ParseFunctionStatementDefinition(function) is Result.Failure result)
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
		
		foreach (IFunctionContext function in program.Functions)
			VisitFunctionStatement(program, function);
	}
}