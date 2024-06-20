using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineFunction : TypedTypeFunctionBase
{
	public override string Name => "DefineFunction";
	public override IToken Pattern => new TokenString([new ParameterTypeToken("returnType"), new ParameterValueToken("name"), new TokenSplit(new LiteralToken("("), new LiteralToken(","), new LiteralToken(")"), new TokenOptions([
		new TokenString([new ParameterTypeToken("parameterTypes"), new ParameterValueToken("parameterNames")]),
		new TokenString([new ParameterTypeToken("varArgParameterType"), new LiteralToken("..."), new ParameterValueToken("varArgParameterName")]),
	])), new TokenOptional(new ParameterExpressionToken("body"))]);
	public override TypeIdentifier ReturnType => new TypedTypeCompilerValue().Id();
	public override FunctionParameters Parameters => new([
		(Visitor.CompilerStringType, "name"),
		(Visitor.TypeIdentifierType, "returnType"),
		(Visitor.TypeIdentifierType.List(), "parameterTypes"),
		(Visitor.CompilerStringType.List(), "parameterNames"),
		(Visitor.TypeIdentifierType, "varArgParameterType"),
		(Visitor.CompilerStringType, "varArgParameterName"),
		(new TypedTypeCompilerClosure(Visitor.VoidType), "body"),
	], null);
	public override float Priority => 90;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineFunctionCall(
			context,
			((ValueIdentifier)args["name"]).Name,
			(TypeIdentifier)args["returnType"],
			new FunctionParameters(
				args["parameterTypes"] is not null ? ((TypedValueCompiler<List<TypeIdentifier>>)args["parameterTypes"]).CompilerValue.Zip(((TypedValueCompiler<List<ValueIdentifier>>)args["parameterNames"]).CompilerValue, (type, name) => new FunctionParameter(type, name.Name)) : [],
				args["varArgParameterType"] is not null ? new FunctionParameter((TypeIdentifier)args["varArgParameterType"], ((ValueIdentifier)args["varArgParameterName"]).Name) : null),
			args["body"] is not null ? (Closure)((Expression)args["body"]).ReturnValue : null);
	}
}

public class DefineFunctionCall(IHasIdentifiers parent, string name, TypeIdentifier returnType, FunctionParameters parameters, Closure? body) : TypedValue, TypedTypeFunction, IHazIdentifiers
{
	public LLVMTypeRef LLVMType { get; }
	public string Name => name;
	public TypeIdentifier ReturnType => returnType;
	public FunctionParameters Parameters => parameters;
	public float Priority { get; }
	public TypedType? Type { get; set; }
	public LLVMValueRef LLVMValue => llvmValue;
	private LLVMValueRef llvmValue;
	public TypedValue? Value { get; set; }
	public IToken? Pattern { get; set; }
	public Closure? Body => body;
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	public List<TypedTypeWithPattern>? FinalizedTypes { get; set; }
	public IHasIdentifiers IHasIdentifiers { get; } = new IdentifiersNest(parent, CompilationPhase.Function);
	
	public void Parse(IHasIdentifiers context)
	{
		context.Functions.Add(this);
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		parameters.Transform(this);
		returnType.Transform(this, Visitor.TypeType);
		Type = new DefinedFunctionCall(name, this, returnType, new FunctionParameters(parameters.Parameters.Select(param => (param.Type.Type, param.Name)).ToArray(), parameters.VarArg is not null ? (parameters.VarArg.Type.Type, parameters.VarArg.Name) : null));
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		llvmValue = visitor.Module.AddFunction(name, Type.LLVMType);
		Value = new TypedValueValue(Type, llvmValue);
		
		llvmValue.Linkage = LLVMLinkage.LLVMExternalLinkage;
		
		for (int i = 0; i < parameters.Parameters.Count; ++i)
		{
			string parameterName = parameters.Parameters[i].Name;
			TypedType parameterType = parameters.Parameters[i].Type.Type;
			LLVMValueRef param = llvmValue.GetParam((uint)i);
			param.Name = parameterName;
			(this as IHasIdentifiers).Identifiers.Add(parameterName, new TypedValueValue(parameterType, param));
		}
		
		if (body is not null)
		{
			visitor.Builder.PositionAtEnd(llvmValue.AppendBasicBlock("entry"));
			body.Parse(this);
			body.Transform(this, null);
			body.Visit(this, null, visitor);
		}
	}
	
	public TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return ((DefinedFunctionCall)Type).Call(context, args);
	}
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}