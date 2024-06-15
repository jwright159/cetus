using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Function;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineFunction() : TypedTypeFunction("DefineFunction", new TypedTypeCompilerValue(), [(Visitor.CompilerStringType, "name"), (Visitor.TypeIdentifierType, "returnType"), (Visitor.TypeIdentifierType.List(), "parameterTypes"), (Visitor.CompilerStringType.List(), "parameterNames"), (Visitor.CompilerStringType, "varArgParameterType"), (Visitor.CompilerStringType, "varArgParameterType"), (new TypedTypeCompilerClosure(Visitor.VoidType), "body")], null)
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineFunctionCall(
			context,
			((Tokens.String)args["name"]).Value,
			((TypedValueCompiler<TypeIdentifier>)args["returnType"]).CompilerValue,
			new FunctionParametersContext
			{
				Parameters = ((TypedValueCompiler<List<TypedValueCompiler<TypeIdentifier>>>)args["parameterTypes"]).CompilerValue.Zip(((TypedValueCompiler<List<Tokens.String>>)args["parameterNames"]).CompilerValue, (type, name) => new FunctionParameterContext(type.CompilerValue, name.Value)).ToList(),
				VarArg = new FunctionParameterContext(((TypedValueCompiler<TypeIdentifier>)args["varArgParameterType"]).CompilerValue, ((Tokens.String)args["varArgParameterName"]).Value),
			},
			((TypedValueCompiler<Closure>)args["body"]).CompilerValue);
	}
}

public class DefineFunctionCall(IHasIdentifiers parent, string name, TypeIdentifier returnType, FunctionParametersContext parameters, Closure body) : TypedValue, IFunctionContext, IHasIdentifiers
{
	public string Name => name;
	public TypeIdentifier ReturnType => returnType;
	public FunctionParametersContext Parameters => parameters;
	public float Priority { get; }
	public TypedType? Type { get; set; }
	public LLVMValueRef LLVMValue { get; }
	public TypedValue? Value { get; set; }
	public IToken? Pattern { get; set; }
	public Closure Body => body;
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new NestedDictionary<string, TypedValue>(parent.Identifiers);
	public ICollection<IFunctionContext> Functions { get; set; } = new NestedCollection<IFunctionContext>(parent.Functions);
	public ICollection<ITypeContext> Types { get; set; } = new NestedCollection<ITypeContext>(parent.Types);
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
	
	public void Parse(IHasIdentifiers context)
	{
		context.Functions.Add(this);
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		Type = new DefinedFunctionCall(name, returnType.Type, parameters.Parameters.Select(param => (param.Type.Type, param.Name)).ToArray(), parameters.VarArg?.Type.Type);
		LLVMValueRef functionValue = module.AddFunction(name, Type.LLVMType);
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
		
		body.Transform(this, null);
		body.Visit(this, null, builder);
	}
	
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
}