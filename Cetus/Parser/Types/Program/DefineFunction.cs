using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineFunction : TypedTypeFunctionBase
{
	public override string Name => "DefineFunction";
	public override IToken Pattern => new TokenString([new ParameterValueToken("returnType"), new ParameterValueToken("name"), new TokenSplit(new LiteralToken("("), new LiteralToken(","), new LiteralToken(")"), new TokenOptions([
		new TokenString([new ParameterValueToken("parameterTypes"), new ParameterValueToken("parameterNames")]),
		new TokenString([new ParameterValueToken("varArgParameterType"), new LiteralToken("..."), new ParameterValueToken("varArgParameterName")]),
	])), new TokenOptional(new ParameterExpressionToken("body"))]);
	public override TypeIdentifier ReturnType => new(new TypedTypeCompilerValue());
	public override FunctionParameters Parameters => new([
		(Visitor.CompilerStringType, "name"),
		(Visitor.TypeIdentifierType, "returnType"),
		(Visitor.TypeIdentifierType.List(), "parameterTypes"),
		(Visitor.CompilerStringType.List(), "parameterNames"),
		(Visitor.TypeIdentifierType, "varArgParameterType"),
		(Visitor.CompilerStringType, "varArgParameterName"),
		(new TypedTypeCompilerClosure(Visitor.VoidType), "body"),
	], null);
	public override float Priority => 800;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineFunctionCall(
			context.Program.Phases[CompilationPhase.Function],
			((ValueIdentifier)args["name"]).Name,
			new TypeIdentifier(((ValueIdentifier)args["returnType"]).Name),
			new FunctionParameters(
				args["parameterTypes"] is not null ? ((TypedValueCompiler<List<ValueIdentifier>>)args["parameterTypes"]).CompilerValue.Zip(((TypedValueCompiler<List<ValueIdentifier>>)args["parameterNames"]).CompilerValue, (type, name) => new FunctionParameter(new TypeIdentifier(type.Name), name.Name)) : [],
				args["varArgParameterType"] is not null ? new FunctionParameter(new TypeIdentifier(((ValueIdentifier)args["varArgParameterType"]).Name), ((ValueIdentifier)args["varArgParameterName"]).Name) : null),
			args["body"] is not null ? (Closure)((Expression)args["body"]).ReturnValue : null);
	}
}

public class DefineFunctionCall(IHasIdentifiers parent, string name, TypeIdentifier returnType, FunctionParameters parameters, Closure? body) : TypedValue, TypedTypeFunction, IHasIdentifiers
{
	public LLVMTypeRef LLVMType { get; }
	public string Name => name;
	public TypeIdentifier ReturnType => returnType;
	public FunctionParameters Parameters => parameters;
	public float Priority { get; }
	public TypedType? Type { get; set; }
	public LLVMValueRef LLVMValue { get; }
	public TypedValue? Value { get; set; }
	public IToken? Pattern { get; set; }
	public Closure? Body => body;
	public IDictionary<string, TypedValue> Identifiers { get; } = new NestedDictionary<string, TypedValue>(parent.Identifiers);
	public ICollection<TypedTypeFunction> Functions { get; } = new NestedCollection<TypedTypeFunction>(parent.Functions);
	public ICollection<TypedType> Types { get; } = new NestedCollection<TypedType>(parent.Types);
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public ProgramContext Program => parent.Program;
	
	public void Parse(IHasIdentifiers context)
	{
		context.Functions.Add(this);
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		parameters.Transform(this);
		returnType.Transform(this, Visitor.TypeType);
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		Type = new DefinedFunctionCall(name, this, returnType, new FunctionParameters(parameters.Parameters.Select(param => (param.Type.Type, param.Name)).ToArray(), parameters.VarArg is not null ? (parameters.VarArg.Type.Type, parameters.VarArg.Name) : null));
		LLVMValueRef functionValue = visitor.Module.AddFunction(name, Type.LLVMType);
		Value = new TypedValueValue(Type, functionValue);
		Identifiers.Add(name, Value);
		
		functionValue.Linkage = LLVMLinkage.LLVMExternalLinkage;
		
		for (int i = 0; i < parameters.Parameters.Count; ++i)
		{
			string parameterName = parameters.Parameters[i].Name;
			TypedType parameterType = parameters.Parameters[i].Type.Type;
			LLVMValueRef param = functionValue.GetParam((uint)i);
			param.Name = parameterName;
			Identifiers.Add(parameterName, new TypedValueValue(parameterType, param));
		}
		
		if (body is not null)
		{
			TypedTypeCompilerClosure bodyType = new(returnType.Type);
			body.Parse(this);
			body.Transform(this, bodyType);
			body.Visit(this, bodyType, visitor);
		}
	}
	
	public TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		throw new NotImplementedException();
	}
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}