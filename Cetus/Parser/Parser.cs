using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class LateCompilerFunctionContext(TypeIdentifier returnType, string name, float priority, IToken? pattern, FunctionParameters parameters) : TypedTypeFunction
{
	public LLVMTypeRef LLVMType { get; }
	public string Name => name;
	public TypedType? Type { get; set; }
	public TypedValue? Value => Type is null ? null : new TypedValueType(Type);
	public TypeIdentifier ReturnType => returnType;
	public FunctionParameters Parameters => parameters;
	public float Priority => priority;
	public IToken? Pattern => pattern;
	
	public TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		throw new NotImplementedException();
	}
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}

public partial class Parser(Lexer lexer)
{
	public ProgramContext Parse()
	{
		ProgramContext program = new();
		program.Phases.Add(CompilationPhase.Program, new IdentifiersBase());
		program.Phases.Add(CompilationPhase.Function, new IdentifiersNest(program.Phases[CompilationPhase.Program]));
		
		AddType(Visitor.VoidType);
		AddType(Visitor.FloatType);
		AddType(Visitor.DoubleType);
		AddType(Visitor.CharType);
		AddType(Visitor.IntType);
		AddType(Visitor.StringType);
		AddType(Visitor.CompilerStringType);
		AddType(Visitor.BoolType);
		AddType(Visitor.TypeType);
		
		IToken fieldToken = new TokenString([new ParameterValueToken("fieldTypes"), new ParameterValueToken("fieldNames")]);
		IToken parameterToken = new TokenString([new ParameterValueToken("parameterTypes"), new ParameterValueToken("parameterNames")]);
		IToken varArgParameterToken = new TokenString([new ParameterValueToken("varArgParameterType"), new LiteralToken("..."), new ParameterValueToken("varArgParameterName")]);
		AddFunction(CompilationPhase.Program, Functions.DefineProgram, 100, new TokenSplit(new PassToken(), new LiteralToken(";"), new EOFToken(), new ParameterExpressionToken("statements")));
		AddFunction(CompilationPhase.Program, Functions.DefineStruct, 90, new TokenString([new ParameterValueToken("name"), new TokenSplit(new LiteralToken("{"), new LiteralToken(";"), new  LiteralToken("}"), new TokenOptions([fieldToken, new ParameterExpressionToken("functions")]))]));
		AddFunction(CompilationPhase.Program, Functions.DefineFunction, 80, new TokenString([new ParameterValueToken("returnType"), new ParameterValueToken("name"), new TokenSplit(new LiteralToken("("), new LiteralToken(","), new LiteralToken(")"), new TokenOptions([parameterToken, varArgParameterToken])), new TokenOptional(new ParameterExpressionToken("body"))]));
		
		AddFunction(CompilationPhase.Function, Functions.Declare, 100, new TokenString([new LiteralToken("Declare"), new ParameterValueToken("type"), new ParameterValueToken("name")]));
		AddFunction(CompilationPhase.Function, Functions.Define, 100, new TokenString([new ParameterValueToken("type"), new ParameterValueToken("name"), new LiteralToken("="), new ParameterExpressionToken("value")]));
		AddFunction(CompilationPhase.Function, Functions.Assign, 100, new TokenString([new ParameterExpressionToken("target"), new LiteralToken("="), new ParameterExpressionToken("value")]));
		AddFunction(CompilationPhase.Function, Functions.Return, 100, new TokenString([new LiteralToken("Return"), new TokenOptional(new ParameterExpressionToken("value"))]));
		AddFunction(CompilationPhase.Function, Functions.Add, 30, new TokenString([new ParameterExpressionToken("a"), new LiteralToken("+"), new ParameterExpressionToken("b")]));
		AddFunction(CompilationPhase.Function, Functions.LessThan, 40, new TokenString([new ParameterExpressionToken("a"), new LiteralToken("<"), new ParameterExpressionToken("b")]));
		AddFunction(CompilationPhase.Function, Functions.While, 100, new TokenString([new LiteralToken("While"), new LiteralToken("("), new ParameterExpressionToken("condition"), new LiteralToken(")"), new ParameterExpressionToken("body")]));
		AddFunction(CompilationPhase.Function, Functions.Call, 10, new TokenString([new ParameterExpressionToken("function"), new TokenSplit(new LiteralToken("("), new LiteralToken(","), new LiteralToken(")"), new ParameterExpressionToken("arguments"))]));
		
		AddValue("True", Visitor.TrueValue);
		AddValue("False", Visitor.FalseValue);
		
		Console.WriteLine("Parsing...");
		ParseProgram(program);
		TransformProgram(program);
		return program;
		
		
		void AddType(TypedType type)
		{
			program.Phases[CompilationPhase.Program].Types.Add(type);
			program.Phases[CompilationPhase.Program].Identifiers.Add(type.Name, new TypedValueType(type));
		}
		
		void AddFunction(CompilationPhase context, TypedTypeFunction function, float priority, IToken? pattern = null)
		{
			program.Phases[context].Functions.Add(function);
			program.Phases[context].Identifiers.Add(function.Name, new TypedValueType(function));
		}
		
		void AddValue(string name, TypedValue value)
		{
			program.Phases[CompilationPhase.Program].Identifiers.Add(name, value);
		}
	}
	
	private void ParseProgram(ProgramContext program)
	{
		Result result = ParseFunctionCall(program.Phases[CompilationPhase.Program], out FunctionCallContext programCall);
		if (result is not Result.Ok)
			throw new Exception(result.ToString());
		if (programCall.FunctionType is not DefineProgram)
			throw new Exception($"Parsed program is a {programCall.FunctionType}, not a program definition");
		program.Call = (DefineProgramCall)programCall.Call(program.Phases[CompilationPhase.Program]);
		program.Call.Parse(program.Call);
	}
	
	private void TransformProgram(ProgramContext program)
	{
		program.Call.Transform(program.Call, null);
	}
}

public enum CompilationPhase
{
	Program,
	Function,
}

public static class Functions
{
	public static readonly DefineProgram DefineProgram = new();
	public static readonly DefineStruct DefineStruct = new();
	public static readonly DefineFunction DefineFunction = new();
	
	public static readonly TypedTypeFunction Declare = new Declare();
	public static readonly TypedTypeFunction Define = new Define();
	public static readonly TypedTypeFunction Assign = new Assign();
	public static readonly TypedTypeFunction Return = new Types.Function.Return();
	public static readonly TypedTypeFunction Add = new Add();
	public static readonly TypedTypeFunction LessThan = new LessThan();
	public static readonly TypedTypeFunction While = new While();
	public static readonly TypedTypeFunction Call = new Call();
}