using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Types.Struct;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineStruct : TypedTypeFunctionBase
{
	public override string Name => "DefineStruct";
	public override IToken Pattern => new TokenString([new ParameterValueToken("name"), new ParameterExpressionToken("body")]);
	public override TypeIdentifier ReturnType => Visitor.VoidType.Id();
	public override FunctionParameters Parameters => new([
		(Visitor.CompilerStringType, "name"),
		(new TypedTypeCompilerClosure(Visitor.VoidType), "body"),
	], null);
	public override float Priority => 80;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineStructCall(
			context,
			((ValueIdentifier)args["name"]).Name,
			(Closure)((Expression)args["body"]).ReturnValue);
	}
}

public class DefineStructCall(IHasIdentifiers parent, string name, Closure body) : TypedValue, TypedType, IHazIdentifiers
{
	public string Name => name;
	public TypedType Type { get; } = new TypedTypeStruct(LLVMContextRef.Global.CreateNamedStruct(name));
	public LLVMValueRef LLVMValue => Visitor.Void.LLVMValue;
	public LLVMTypeRef LLVMType => Type.LLVMType;
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public List<TypedTypeWithPattern>? FinalizedTypes { get; set; }
	public IHasIdentifiers IHasIdentifiers { get; } = new IdentifiersNest(parent, CompilationPhase.Struct);
	public Dictionary<DefineFunctionCall, LateCompilerFunctionContext> FunctionGetters = new();
	public Dictionary<DefineFunctionCall, LateCompilerFunctionContext> FunctionCallers = new();
	public List<StructField> Fields { get; private set; }
	public List<DefineFunctionCall> Functions { get; private set; }
	
	public void Parse(IHasIdentifiers context)
	{
		context.Types.Add(this);
		context.Identifiers.Add(Name, new TypedValueType(Type));
		
		body.Parse(this);
		
		uint fieldIndex = 0;
		Fields = body.Statements
			.OfType<StructField>()
			.Select(field =>
			{
				field.Index = fieldIndex++;
				return field;
			})
			.ToList();
		
		Functions = body.Statements
			.OfType<DefineFunctionCall>()
			.ToList();
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		body.Transform(this, null);
		
		foreach (StructField field in Fields)
		{
			field.Getter = new Getter(this, field);
			(this as IHasIdentifiers).Functions.Add(field.Getter);
		}
		
		foreach (DefineFunctionCall function in Functions)
		{
			{
				LateCompilerFunctionContext getterFunction = new(
					function.Id(),
					$"{Name}.Get_{function.Name}",
					0,
					new TokenString([new ParameterValueToken("type"), new LiteralToken("."), new LiteralToken(function.Name)]),
					new FunctionParameters([new FunctionParameter(Visitor.TypeType.Id(), "type")], null));
				getterFunction.Type = new MethodGetter(this, function);
				FunctionGetters.Add(function, getterFunction);
				context.Functions.Add(getterFunction);
			}
			
			if (TypedTypeExtensions.TypesEqual(function.Parameters.Parameters[0].Type.Type, this))
			{
				LateCompilerFunctionContext callerFunction = new(
					function.Id(),
					$"{Name}.Call_{function.Name}",
					0,
					new TokenString([new ParameterValueToken("this"), new LiteralToken("."), new LiteralToken(function.Name)]),
					new FunctionParameters([new FunctionParameter(new TypeIdentifierName(Name), "this")], null));
				callerFunction.Type = new MethodCaller(this, function);
				FunctionCallers.Add(function, callerFunction);
				context.Functions.Add(callerFunction);
			}
		}
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		foreach (StructField field in Fields)
			field.Visit(this, null, visitor);
		
		Type.LLVMType.StructSetBody(Fields.Select(field => field.Type.LLVMType).ToArray(), false);
		
		foreach (DefineFunctionCall function in Functions)
			function.Visit(this, null, visitor);
	}
	
	public override string ToString() => Name;
}