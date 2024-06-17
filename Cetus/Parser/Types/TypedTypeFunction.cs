using Cetus.Parser.Tokens;
using Cetus.Parser.Types.Program;
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

public abstract class TypedTypeFunctionBase : TypedTypeFunction
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(ReturnType.Type.LLVMType, Parameters.Parameters.Select(param => param.Type.Type.LLVMType).ToArray(), Parameters.VarArg is not null);
	public abstract string Name { get; }
	public TypedType? Type { get; }
	public TypedValue? Value { get; }
	public abstract IToken? Pattern { get; }
	public abstract TypeIdentifier ReturnType { get; }
	public abstract FunctionParameters Parameters { get; }
	public abstract float Priority { get; }
	public override string ToString() => $"{ReturnType} {Name}{Parameters}";
	
	public abstract TypedValue Call(IHasIdentifiers context, FunctionArgs args);
}

public abstract class TypedTypeFunctionSimple : TypedTypeFunctionBase
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new SimpleCall(ReturnType.Type, args, Visit);
	}
	
	public abstract LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args);
	
	private class SimpleCall(TypedType returnType, FunctionArgs args, Func<IHasIdentifiers, TypedType?, Visitor, FunctionArgs, LLVMValueRef?> visit) : TypedValue
	{
		public TypedType Type => returnType;
		public LLVMValueRef LLVMValue { get; private set; }
		
		public void Parse(IHasIdentifiers context)
		{
			args.Parse(context);
		}
		
		public void Transform(IHasIdentifiers context, TypedType? typeHint)
		{
			args.Transform(context);
		}
		
		public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
		{
			args.Visit(context, visitor);
			LLVMValue = visit(context, typeHint, visitor, args) ?? default;
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
			switch (arg.Type.Type)
			{
				case TypedTypeCompilerList<TypedTypeCompilerAnyFunctionCall>:
					if (arg.Value is null)
					{
						arg.Value = new TypedValueCompiler<List<FunctionCall>>(arg.Type.Type, []);
						args[key] = arg;
					}
					((TypedValueCompiler<List<FunctionCall>>)arg.Value).CompilerValue.Add((FunctionCall)value);
					break;
				
				case TypedTypeCompilerList<TypedTypeCompilerAnyFunction>:
					if (arg.Value is null)
					{
						arg.Value = new TypedValueCompiler<List<FunctionCall>>(arg.Type.Type, []);
						args[key] = arg;
					}
					((TypedValueCompiler<List<FunctionCall>>)arg.Value).CompilerValue.Add((FunctionCall)value);
					break;
				
				case TypedTypeCompilerList<TypedTypeCompilerAnyValue>:
					if (arg.Value is null)
					{
						arg.Value = new TypedValueCompiler<List<TypedValue>>(arg.Type.Type, []);
						args[key] = arg;
					}
					((TypedValueCompiler<List<TypedValue>>)arg.Value).CompilerValue.Add(value);
					break;
				
				case TypedTypeCompilerList<TypedTypeCompilerString>:
					if (arg.Value is null)
					{
						arg.Value = new TypedValueCompiler<List<ValueIdentifier>>(arg.Type.Type, []);
						args[key] = arg;
					}
					((TypedValueCompiler<List<ValueIdentifier>>)arg.Value).CompilerValue.Add((ValueIdentifier)value);
					break;
				
				case TypedTypeCompilerList<TypedTypeCompilerTypeIdentifier>:
					if (arg.Value is null)
					{
						arg.Value = new TypedValueCompiler<List<TypeIdentifier>>(arg.Type.Type, []);
						args[key] = arg;
					}
					((TypedValueCompiler<List<TypeIdentifier>>)arg.Value).CompilerValue.Add((TypeIdentifier)value);
					break;
				
				default:
					args[key] = (arg.Type, value);
					break;
			}
		}
	}
	
	public void Parse(IHasIdentifiers context)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			value?.Parse(context);
	}
	
	public void Transform(IHasIdentifiers context)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			value?.Transform(context, type.Type);
	}
	
	public void Visit(IHasIdentifiers context, Visitor visitor)
	{
		foreach ((TypeIdentifier type, TypedValue? value) in args.Values)
			if (type.Type is not TypedTypeCompilerAnyValue) // Gotta visit these manually later
				value?.Visit(context, type.Type, visitor);
	}
	
	public override string ToString() => "(\n\t" + string.Join(",\n", args.Select(arg => $"{arg.Key}: {arg.Value.Value}")).Replace("\n", "\n\t") + "\n)";
}