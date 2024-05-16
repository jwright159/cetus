using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

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

public partial class Parser(Lexer lexer)
{
	public ProgramContext Parse()
	{
		ProgramContext program = new();
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
			{ new CompilerFunctionContext(Visitor.WhileFunctionType, [new LiteralToken("While"), new LiteralToken("("), new ParameterIndexToken(0), new LiteralToken(")"), new ParameterIndexToken(1)]), new TypedValueType(Visitor.WhileFunctionType) },
			{ new CompilerFunctionContext(Visitor.ReturnFunctionType, [new LiteralToken("Return"), new ParameterIndexToken(0)]), new TypedValueType(Visitor.ReturnFunctionType) },
			{ new CompilerFunctionContext(Visitor.ReturnVoidFunctionType, [new LiteralToken("Return")]), new TypedValueType(Visitor.ReturnVoidFunctionType) },
			{ new CompilerFunctionContext(Visitor.LessThanFunctionType, [new ParameterIndexToken(0), new LiteralToken("<"), new ParameterIndexToken(1)]), new TypedValueType(Visitor.LessThanFunctionType) },
			{ new CompilerFunctionContext(Visitor.AddFunctionType, [new ParameterIndexToken(0), new LiteralToken("+"), new ParameterIndexToken(1)]), new TypedValueType(Visitor.AddFunctionType) },
		};
		
		Console.WriteLine("Parsing...");
		Result result = ParseProgram(program);
		if (result is not Result.Ok)
			throw new Exception(result.ToString());
		return program;
	}
}