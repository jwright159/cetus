using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public interface TypedTypeFunction : TypedType
{
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public IToken? Pattern { get; }
	public TypeIdentifier ReturnType { get; }
	public FunctionParameters Parameters { get; }
	public float Priority { get; }
	
	public TypedValue Call(IHasIdentifiers context, FunctionArgs args);
}

public abstract class TypedTypeFunctionBase(string name, TypedType returnType, FunctionParameters parameters) : TypedTypeFunction
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(returnType.LLVMType, parameters.Parameters.Select(param => param.Type.Type.LLVMType).ToArray(), Parameters.VarArg is not null);
	public string Name => name;
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public IToken? Pattern { get; }
	public TypeIdentifier ReturnType => new(returnType);
	public FunctionParameters Parameters => parameters;
	public float Priority { get; }
	public override string ToString() => name;
	
	public abstract TypedValue Call(IHasIdentifiers context, FunctionArgs args);
}

public abstract class TypedTypeFunctionSimple(string name, TypedType returnType, (TypedType Type, string Name)[] parameters, (TypedType Type, string Name)? varArg) : TypedTypeFunctionBase(name, returnType, new FunctionParameters(parameters, varArg))
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new ParseOnlyCall(returnType, (visitContext, visitTypeHint, visitVisitor) => Visit(visitContext, visitTypeHint, visitVisitor, args));
	}
	
	public abstract LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args);
	
	private class ParseOnlyCall(TypedType returnType, Func<IHasIdentifiers, TypedType?, Visitor, LLVMValueRef> visit) : TypedValue
	{
		public TypedType Type => returnType;
		public LLVMValueRef LLVMValue { get; private set; }
		
		public void Parse(IHasIdentifiers context)
		{
			
		}
		
		public void Transform(IHasIdentifiers context, TypedType? typeHint)
		{
			
		}
		
		public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
		{
			LLVMValue = visit(context, typeHint, visitor);
		}
	}
}

public class FunctionArgs
{
	public List<string> Keys = [];
	private Dictionary<string, (TypeIdentifier Type, TypedValue? Value)> args = new();
	
	public FunctionArgs(FunctionParameters parameters)
	{
		foreach (FunctionParameter param in parameters.Parameters)
		{
			Keys.Add(param.Name);
			args[param.Name] = (param.Type, null);
		}
	}
	
	public FunctionArgs(FunctionParameters parameters, ICollection<TypedValue> arguments)
	{
		foreach ((FunctionParameter param, TypedValue arg) in parameters.ZipArgs(arguments, (param, arg) => (param, arg)))
		{
			Keys.Add(param.Name);
			args[param.Name] = (param.Type, arg);
		}
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
	
	public void Visit(IHasIdentifiers context, Visitor visitor)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			value.Visit(context, type.Type, visitor);
	}
}