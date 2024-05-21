using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class CompilerTypeContext(string name, TypedType type) : ITypeContext
{
	public string Name => name;
	public TypedType Type => type;
}

public class CompilerFunctionContext(TypedTypeFunction function, IToken[]? pattern) : IFunctionContext
{
	public TypedType Type => function;
	public TypedValue Value => new TypedValueType(function);
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
		
		program.Types.Add(new CompilerTypeContext("Void", Visitor.VoidType));
		program.Types.Add(new CompilerTypeContext("Float", Visitor.FloatType));
		program.Types.Add(new CompilerTypeContext("Double", Visitor.DoubleType));
		program.Types.Add(new CompilerTypeContext("Char", Visitor.CharType));
		program.Types.Add(new CompilerTypeContext("Int", Visitor.IntType));
		program.Types.Add(new CompilerTypeContext("String", Visitor.StringType));
		program.Types.Add(new CompilerTypeContext("CompilerString", Visitor.CompilerStringType));
		program.Types.Add(new CompilerTypeContext("Bool", Visitor.BoolType));
		program.Types.Add(new CompilerTypeContext("Type", Visitor.TypeType));
		
		program.Functions.Add(new CompilerFunctionContext(Visitor.AssignFunctionType, [new ParameterExpressionToken(0), new LiteralToken("="), new ParameterExpressionToken(1)]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.DeclareFunctionType, [new LiteralToken("Declare"), new ParameterValueToken(0), new ParameterValueToken(1)]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.DefineFunctionType, [new ParameterValueToken(0), new ParameterValueToken(1), new LiteralToken("="), new ParameterExpressionToken(2)]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.WhileFunctionType, [new LiteralToken("While"), new LiteralToken("("), new ParameterExpressionToken(0), new LiteralToken(")"), new ParameterExpressionToken(1)]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.ReturnFunctionType, [new LiteralToken("Return"), new ParameterExpressionToken(0)]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.ReturnVoidFunctionType, [new LiteralToken("Return")]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.LessThanFunctionType, [new ParameterExpressionToken(0), new LiteralToken("<"), new ParameterExpressionToken(1)]));
		program.Functions.Add(new CompilerFunctionContext(Visitor.AddFunctionType, [new ParameterExpressionToken(0), new LiteralToken("+"), new ParameterExpressionToken(1)]));
		
		program.Identifiers.Add("Void", new TypedValueType(Visitor.VoidType));
		program.Identifiers.Add("Float", new TypedValueType(Visitor.FloatType));
		program.Identifiers.Add("Double", new TypedValueType(Visitor.DoubleType));
		program.Identifiers.Add("Char", new TypedValueType(Visitor.CharType));
		program.Identifiers.Add("Int", new TypedValueType(Visitor.IntType));
		program.Identifiers.Add("String", new TypedValueType(Visitor.StringType));
		program.Identifiers.Add("CompilerString", new TypedValueType(Visitor.CompilerStringType));
		program.Identifiers.Add("Bool", new TypedValueType(Visitor.BoolType));
		program.Identifiers.Add("Type", new TypedValueType(Visitor.TypeType));
		
		program.Identifiers.Add("True", Visitor.TrueValue);
		program.Identifiers.Add("False", Visitor.FalseValue);
		
		program.Identifiers.Add("Declare", new TypedValueType(Visitor.DeclareFunctionType));
		program.Identifiers.Add("Define", new TypedValueType(Visitor.DefineFunctionType));
		program.Identifiers.Add("Assign", new TypedValueType(Visitor.AssignFunctionType));
		program.Identifiers.Add("Return", new TypedValueType(Visitor.ReturnFunctionType));
		program.Identifiers.Add("ReturnVoid", new TypedValueType(Visitor.ReturnVoidFunctionType));
		program.Identifiers.Add("Add", new TypedValueType(Visitor.AddFunctionType));
		program.Identifiers.Add("LessThan", new TypedValueType(Visitor.LessThanFunctionType));
		program.Identifiers.Add("While", new TypedValueType(Visitor.WhileFunctionType));
		
		Console.WriteLine("Parsing...");
		Result result = ParseProgram(program);
		if (result is not Result.Ok)
			throw new Exception(result.ToString());
		return program;
	}
}