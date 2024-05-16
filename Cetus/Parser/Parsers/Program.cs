using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
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
	public Dictionary<ITypeContext, TypedType?> Types;
	public Dictionary<string, TypedValue> Identifiers { get; set; }
	public List<string> Libraries = [];
}

public class CompilerTypeContext(string name) : ITypeContext
{
	public string Name => name;
}

public class CompilerFunctionContext(TypedTypeFunction function, IToken[]? pattern) : IFunctionContext
{
	public FunctionParametersContext ParameterContexts { get; } = new()
	{
		Parameters = function.ParamTypes
			.Select(paramType => new FunctionParameterContext(new TypeIdentifierContext
			{
				Name = paramType.ToString(),
				PointerDepth = paramType.PointerDepth,
			}, null))
			.ToList(),
	};
	public IToken[]? Pattern => pattern;
}

public partial class Parser
{
	public Result ParseProgram(out ProgramContext program)
	{
		program = new ProgramContext();
		program.Types = new Dictionary<ITypeContext, TypedType?>
		{
			{ new CompilerTypeContext("Void"), Visitor.VoidType },
			{ new CompilerTypeContext("Float"), Visitor.FloatType },
			{ new CompilerTypeContext("Double"), Visitor.DoubleType },
			{ new CompilerTypeContext("Char"), Visitor.CharType },
			{ new CompilerTypeContext("Int"), Visitor.IntType },
			{ new CompilerTypeContext("String"), Visitor.StringType },
			{ new CompilerTypeContext("CompilerString"), Visitor.CompilerStringType },
			{ new CompilerTypeContext("Bool"), Visitor.BoolType },
			{ new CompilerTypeContext("Type"), Visitor.TypeType },
		};
		program.Functions = new Dictionary<IFunctionContext, TypedValue?>
		{
			{ new CompilerFunctionContext(Visitor.AssignFunctionType, [new ParameterIndexToken(0), new LiteralToken("="), new ParameterIndexToken(1)]), new TypedValueType(Visitor.AssignFunctionType) },
			{ new CompilerFunctionContext(Visitor.DeclareFunctionType, [new ParameterIndexToken(0), new ParameterIndexToken(1), new LiteralToken("="), new ParameterIndexToken(2)]), new TypedValueType(Visitor.DeclareFunctionType) },
			{ new CompilerFunctionContext(Visitor.ReturnFunctionType, null), new TypedValueType(Visitor.ReturnFunctionType) },
			{ new CompilerFunctionContext(Visitor.ReturnVoidFunctionType, null), new TypedValueType(Visitor.ReturnVoidFunctionType) },
			{ new CompilerFunctionContext(Visitor.AddFunctionType, [new ParameterIndexToken(0), new LiteralToken("+"), new ParameterIndexToken(1)]), new TypedValueType(Visitor.AddFunctionType) },
			{ new CompilerFunctionContext(Visitor.LessThanFunctionType, [new ParameterIndexToken(0), new LiteralToken("<"), new ParameterIndexToken(1)]), new TypedValueType(Visitor.LessThanFunctionType) },
			{ new CompilerFunctionContext(Visitor.WhileFunctionType, null), new TypedValueType(Visitor.WhileFunctionType) },
		};
		
		List<Result> results = [];
		
		while (ParseProgramStatementFirstPass(program)) { }
		
		if (!lexer.IsAtEnd)
			return new Result.TokenRuleFailed("Expected program statement", lexer.Line, lexer.Column);
		
		Console.WriteLine("Parsing type declarations...");
		foreach (ITypeContext type in program.Types.Keys)
			if (ParseTypeStatementDeclaration(program, type) is Result.Failure result)
				results.Add(result);
		
		Console.WriteLine("Parsing type definitions...");
		foreach (ITypeContext type in program.Types.Keys)
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
		foreach (ITypeContext type in program.Types.Keys)
			VisitTypeStatement(program, type);
		
		foreach (IFunctionContext function in program.Functions.Keys)
			VisitFunctionStatement(program, function);
	}
}