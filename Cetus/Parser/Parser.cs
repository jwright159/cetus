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
	
	public Program Parse()
	{
		Program program = new();
		program.Contexts.Add(CompilationPhase.Program, new IdentifiersContext(program));
		program.Contexts.Add(CompilationPhase.Struct, new IdentifiersContext(program));
		program.Contexts.Add(CompilationPhase.Function, new IdentifiersContext(program));
		program.IHasIdentifiers = new IdentifiersBase(program.Contexts[CompilationPhase.Program]);
		
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
			(program as IHasIdentifiers).Types.Add(type);
			(program as IHasIdentifiers).Identifiers.Add(name ?? type.Name, new TypedValueType(type));
		}
		
		void AddFunction(CompilationPhase context, TypedTypeFunction function)
		{
			program.Contexts[context].Functions.Add(function);
			program.Contexts[context].Identifiers.Add(function.Name, new TypedValueType(function));
		}
		
		void AddValue(string name, TypedValue value)
		{
			(program as IHasIdentifiers).Identifiers.Add(name, value);
		}
	}
	
	private void ParseProgram(Program program)
	{
		FunctionCall programCall = new(program, 0, float.MaxValue);
		Result result = lexer.Eat(programCall);
		if (result is not Result.Ok)
			throw new Exception("Parsing failed\n" + result);
		if (programCall.FunctionType is not DefineProgram)
			throw new Exception($"Parsed program is a {programCall.FunctionType}, not a program definition");
		program.Call = (DefineProgramCall)programCall.Call(program);
		program.Call.Parse(program);
	}
	
	private void TransformProgram(Program program)
	{
		program.Call.Transform(program, null);
	}
}

public enum CompilationPhase
{
	Program,
	Struct,
	Function,
}