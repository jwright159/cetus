using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IHasIdentifiers
{
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<ITypeContext> Types { get; set; }
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
}

public static class IHasIdentifiersExtensions
{
	public static void NestFrom(this IHasIdentifiers child, IHasIdentifiers parent)
	{
		child.Identifiers = new NestedDictionary<string, TypedValue>(parent.Identifiers);
		child.Functions = new NestedCollection<IFunctionContext>(parent.Functions);
		child.Types = new NestedCollection<ITypeContext>(parent.Types);
	}
	
	public static List<IFunctionContext> GetFinalizedFunctions(this IHasIdentifiers program)
	{
		if (program.FinalizedFunctions is null)
		{
			program.FinalizedFunctions = program.Functions
				.Where(value => value is { Pattern.Length: > 0 })
				.ToList();
			program.FinalizedFunctions.Sort((a, b) => -a.Priority.CompareTo(b.Priority)); // Sort in descending order
		}
		return program.FinalizedFunctions;
	}
}

public class ProgramContext
{
	public List<string> Libraries = [];
}

public partial class Parser
{
	public Result ParseProgram(ProgramContext program)
	{
		List<Result> results = [];
		
		while (!lexer.IsAtEnd)
		{
			Result result = ParseProgramStatementFirstPass(program);
			if (result is Result.Failure)
				results.Add(result);
			if (result is Result.TokenRuleFailed)
				return Result.WrapPassable("Invalid program", results.ToArray());
		}
		
		Console.WriteLine("Parsing type definitions...");
		foreach (ITypeContext type in program.Types)
			if (ParseTypeStatementDefinition(type) is Result.Failure result)
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