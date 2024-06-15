using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public abstract class TypedTypeFunction(string name, TypedType returnType, (TypedType Type, string Name)[] parameters, (TypedType Type, string Name)? varArg) : TypedType, IFunctionContext
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(returnType.LLVMType, parameters.Select(param => param.Type.LLVMType).ToArray(), IsVarArg);
	public string Name => name;
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public IToken? Pattern { get; }
	public TypeIdentifier ReturnType => new(returnType);
	public FunctionParametersContext Parameters { get; } = new(parameters, varArg);
	public float Priority { get; }
	public bool IsVarArg => varArg is not null;
	public override string ToString() => name;
	
	public abstract TypedValue Call(IHasIdentifiers context, FunctionArgs args);
}

public abstract class TypedTypeFunctionSimple(string name, TypedType returnType, (TypedType Type, string Name)[] parameters, (TypedType Type, string Name)? varArg) : TypedTypeFunction(name, returnType, parameters, varArg)
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new ParseOnlyCall(returnType, (visitContext, visitTypeHint, visitBuilder) => Visit(visitContext, visitBuilder, visitTypeHint, args));
	}
	
	public abstract LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args);
	
	private class ParseOnlyCall(TypedType returnType, Func<IHasIdentifiers, TypedType?, LLVMBuilderRef, LLVMValueRef> visit) : TypedValue
	{
		public TypedType Type => returnType;
		public LLVMValueRef LLVMValue { get; private set; }
		
		public void Parse(IHasIdentifiers context)
		{
			
		}
		
		public void Transform(IHasIdentifiers context, TypedType? typeHint)
		{
			
		}
		
		public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
		{
			LLVMValue = visit(context, typeHint, builder);
		}
	}
}

public class FunctionArgs
{
	private Dictionary<string, (TypeIdentifier Type, TypedValue? Value)> args = new();
	
	public FunctionArgs(FunctionParametersContext parameters)
	{
		foreach (FunctionParameterContext parameter in parameters.Parameters)
			args[parameter.Name] = (parameter.Type, null);
	}
	
	public TypedValue this[string key]
	{
		get
		{
			if (!args.TryGetValue(key, out (TypeIdentifier Type, TypedValue? Value) arg))
				throw new KeyNotFoundException($"Argument {key} does not exist");
			return arg.Value;
		}
		set
		{
			if (!args.TryGetValue(key, out (TypeIdentifier Type, TypedValue? Value) arg))
				throw new KeyNotFoundException($"Argument {key} does not exist");
			args[key] = (arg.Type, value);
		}
	}
	
	public void Parse(IHasIdentifiers context)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			value.Parse(context);
	}
	
	public void Transform(IHasIdentifiers context)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			value.Transform(context, type.Type);
	}
	
	public void Visit(IHasIdentifiers context, LLVMBuilderRef builder)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			value.Visit(context, type.Type, builder);
	}
}