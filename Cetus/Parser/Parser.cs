using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class CompilerTypeContext(string name, TypedType type) : ITypeContext
{
	public string Name => name;
	public TypedType Type => type;
}

public class CompilerFunctionContext(TypedTypeFunction function, float priority, IToken? pattern) : IFunctionContext
{
	public string Name => function.Name;
	public TypedType Type => function;
	public TypedValue Value => new TypedValueType(function);
	public TypeIdentifierContext ReturnType { get; } = new()
	{
		Name = function.ReturnType.BaseType.ToString(),
		PointerDepth = function.ReturnType.PointerDepth,
	};
	public FunctionParametersContext ParameterContexts { get; } = new()
	{
		Parameters = function.Parameters
			.Select(param => new FunctionParameterContext(new TypeIdentifierContext
			{
				Name = param.Type.BaseType.ToString(),
				PointerDepth = param.Type.PointerDepth,
			}, param.Name))
			.ToList(),
	};
	public float Priority => priority;
	public IToken? Pattern => pattern;
	
	public override string ToString() => $"{ReturnType} {Name}{ParameterContexts}";
}

public class LateCompilerFunctionContext(TypeIdentifierContext returnType, string name, float priority, IToken? pattern, FunctionParametersContext parameters) : IFunctionContext
{
	public string Name => name;
	public TypedType? Type { get; set; }
	public TypedValue? Value => Type is null ? null : new TypedValueType(Type);
	public TypeIdentifierContext ReturnType => returnType;
	public FunctionParametersContext ParameterContexts => parameters;
	public float Priority => priority;
	public IToken? Pattern => pattern;
	
	public override string ToString() => $"{ReturnType} {Name}{ParameterContexts}";
}

public partial class Parser(Lexer lexer)
{
	public ProgramContext Parse()
	{
		ProgramContext program = new();
		
		AddType("Void", Visitor.VoidType);
		AddType("Float", Visitor.FloatType);
		AddType("Double", Visitor.DoubleType);
		AddType("Char", Visitor.CharType);
		AddType("Int", Visitor.IntType);
		AddType("String", Visitor.StringType);
		AddType("CompilerString", Visitor.CompilerStringType);
		AddType("Bool", Visitor.BoolType);
		AddType("Type", Visitor.TypeType);
		
		IToken fieldToken = new TokenString([new ParameterValueToken("fieldTypes"), new ParameterValueToken("fieldNames")]);
		IToken parameterToken = new TokenString([new ParameterValueToken("parameterTypes"), new ParameterValueToken("parameterNames")]);
		IToken varArgParameterToken = new TokenString([new ParameterValueToken("varArgParameterType"), new LiteralToken("..."), new ParameterValueToken("varArgParameterName")]);
		AddFunction(ContextType.Program, Functions.DefineProgram, 100, new TokenSplit(new PassToken(), new LiteralToken(";"), new EOFToken(), new ParameterExpressionToken("statements")));
		AddFunction(ContextType.Program, Functions.DefineStruct, 90, new TokenString([new ParameterValueToken("name"), new TokenSplit(new LiteralToken("{"), new LiteralToken(";"), new  LiteralToken("}"), new TokenOptions([fieldToken, new ParameterExpressionToken("functions")]))]));
		AddFunction(ContextType.Program, Functions.DefineFunction, 80, new TokenString([new ParameterValueToken("returnType"), new ParameterValueToken("name"), new TokenSplit(new LiteralToken("("), new LiteralToken(","), new LiteralToken(")"), new TokenOptions([parameterToken, varArgParameterToken])), new TokenOptional(new ParameterExpressionToken("body"))]));
		
		AddFunction(ContextType.Function, Functions.Declare, 100, new TokenString([new LiteralToken("Declare"), new ParameterValueToken("type"), new ParameterValueToken("name")]));
		AddFunction(ContextType.Function, Functions.Define, 100, new TokenString([new ParameterValueToken("type"), new ParameterValueToken("name"), new LiteralToken("="), new ParameterExpressionToken("value")]));
		AddFunction(ContextType.Function, Functions.Assign, 100, new TokenString([new ParameterExpressionToken("target"), new LiteralToken("="), new ParameterExpressionToken("value")]));
		AddFunction(ContextType.Function, Functions.Return, 100, new TokenString([new LiteralToken("Return"), new TokenOptional(new ParameterExpressionToken("value"))]));
		AddFunction(ContextType.Function, Functions.Add, 30, new TokenString([new ParameterExpressionToken("a"), new LiteralToken("+"), new ParameterExpressionToken("b")]));
		AddFunction(ContextType.Function, Functions.LessThan, 40, new TokenString([new ParameterExpressionToken("a"), new LiteralToken("<"), new ParameterExpressionToken("b")]));
		AddFunction(ContextType.Function, Functions.While, 100, new TokenString([new LiteralToken("While"), new LiteralToken("("), new ParameterExpressionToken("condition"), new LiteralToken(")"), new ParameterExpressionToken("body")]));
		
		AddValue("True", Visitor.TrueValue);
		AddValue("False", Visitor.FalseValue);
		
		Parse(program, ContextType.Program);
		Transform(program, ContextType.Program);
		Parse(program, ContextType.Function);
		
		Console.WriteLine("Parsing...");
		Result result = ParseProgram(program);
		if (result is not Result.Ok)
			throw new Exception("\n" + result);
		return program;
		
		
		void AddType(string name, TypedType type)
		{
			program.Types.Add(new CompilerTypeContext(name, type));
			program.Identifiers.Add(name, new TypedValueType(type));
		}
		
		void AddFunction(ContextType context, TypedTypeFunction function, float priority, IToken? pattern = null)
		{
			program.Functions.Add(new CompilerFunctionContext(function, priority, pattern));
			program.Identifiers.Add(function.Name, new TypedValueType(function));
		}
		
		void AddValue(string name, TypedValue value)
		{
			program.Identifiers.Add(name, value);
		}
	}
}

public enum ContextType
{
	Program,
	Function,
}

public static class Functions
{
	public static readonly TypedTypeFunction DefineProgram = new DefineProgram();
	public static readonly TypedTypeFunction DefineStruct = new DefineStruct();
	public static readonly TypedTypeFunction DefineFunction = new DefineFunction();
	
	public static readonly TypedTypeFunction Declare = new Declare();
	public static readonly TypedTypeFunction Define = new Define();
	public static readonly TypedTypeFunction Assign = new Assign();
	public static readonly TypedTypeFunction Return = new Types.Function.Return();
	public static readonly TypedTypeFunction Add = new Add();
	public static readonly TypedTypeFunction LessThan = new LessThan();
	public static readonly TypedTypeFunction While = new While();
}