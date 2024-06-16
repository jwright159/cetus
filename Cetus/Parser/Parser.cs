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
		return ((TypedTypeFunction)Type).Call(context, args);
	}
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}

public partial class Parser(Lexer lexer)
{
	public const float ExpressionPriorityThreshold = 100;
	
	public ProgramContext Parse()
	{
		ProgramContext program = new();
		program.Phases.Add(CompilationPhase.Program, new IdentifiersBase(program));
		program.Phases.Add(CompilationPhase.Function, new IdentifiersNest(program.Phases[CompilationPhase.Program]));
		
		AddType(Visitor.VoidType);
		AddType(Visitor.FloatType);
		AddType(Visitor.DoubleType);
		AddType(Visitor.CharType);
		AddType(Visitor.IntType);
		AddType(Visitor.StringType, "String");
		AddType(Visitor.CompilerStringType);
		AddType(Visitor.BoolType);
		AddType(Visitor.TypeType);
		AddType(new TypedTypePointer());
		
		AddFunction(CompilationPhase.Program, new DefineProgram());
		AddFunction(CompilationPhase.Program, new DefineStruct());
		AddFunction(CompilationPhase.Program, new DefineFunction());
		
		AddFunction(CompilationPhase.Function, new Declare());
		AddFunction(CompilationPhase.Function, new Define());
		AddFunction(CompilationPhase.Function, new Assign());
		AddFunction(CompilationPhase.Function, new Types.Function.Return());
		AddFunction(CompilationPhase.Function, new Add());
		AddFunction(CompilationPhase.Function, new LessThan());
		AddFunction(CompilationPhase.Function, new While());
		AddFunction(CompilationPhase.Function, new CallFunction());
		
		AddValue("True", Visitor.TrueValue);
		AddValue("False", Visitor.FalseValue);
		
		Console.WriteLine("Parsing...");
		ParseProgram(program);
		Console.WriteLine("Transforming...");
		TransformProgram(program);
		return program;
		
		
		void AddType(TypedType type, string? name = null)
		{
			program.Phases[CompilationPhase.Program].Types.Add(type);
			program.Phases[CompilationPhase.Program].Identifiers.Add(name ?? type.Name, new TypedValueType(type));
		}
		
		void AddFunction(CompilationPhase context, TypedTypeFunction function)
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
		FunctionCall programCall = new(program.Phases[CompilationPhase.Program], 0, float.MaxValue);
		Result result = lexer.Eat(programCall);
		if (result is not Result.Ok)
			throw new Exception("Parsing failed\n" + result);
		if (programCall.FunctionType is not DefineProgram)
			throw new Exception($"Parsed program is a {programCall.FunctionType}, not a program definition");
		program.Call = (DefineProgramCall)programCall.Call(program.Phases[CompilationPhase.Program]);
		program.Call.Parse(program.Phases[CompilationPhase.Program]);
	}
	
	private void TransformProgram(ProgramContext program)
	{
		program.Call.Transform(program.Phases[CompilationPhase.Program], null);
	}
}

public enum CompilationPhase
{
	Program,
	Function,
}