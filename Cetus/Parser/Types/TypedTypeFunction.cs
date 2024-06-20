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
	protected virtual bool AutoVisit => true;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new SimpleCall(ReturnType.Type, args, Visit, AutoVisit);
	}
	
	public abstract LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args);
	
	private class SimpleCall(TypedType returnType, FunctionArgs args, Func<IHasIdentifiers, TypedType?, Visitor, FunctionArgs, LLVMValueRef?> visit, bool autoVisit) : TypedValue
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
			if (autoVisit)
				args.Visit(context, visitor);
			LLVMValue = visit(context, typeHint, visitor, args) ?? default;
		}
	}
}

public class FunctionParameters
{
	public FunctionParameters(IEnumerable<FunctionParameter> parameters, FunctionParameter? varArg)
	{
		Parameters = parameters.ToList();
		VarArg = varArg;
	}
	
	public FunctionParameters(IEnumerable<(TypedType Type, string Name)> parameters, (TypedType Type, string Name)? varArg = null)
	{
		Parameters = parameters.Select(param => new FunctionParameter(param.Type.Id(), param.Name)).ToList();
		VarArg = varArg is null ? null : new FunctionParameter(varArg.Value.Type.Id(), varArg.Value.Name);
	}
	
	public List<FunctionParameter> Parameters;
	public FunctionParameter? VarArg;
	
	public int Count => Parameters.Count;
	
	public IEnumerable<FunctionParameter> ParamsOfCount(int count)
	{
		if (VarArg is null)
		{
			if (count != Parameters.Count)
				throw new ArgumentOutOfRangeException(nameof(count), $"Count must equal the number of parameters ({Parameters.Count})");
			return Parameters;
		}
		else
		{
			if (count < Parameters.Count)
				throw new ArgumentOutOfRangeException(nameof(count), $"Count must be greater than or equal to the number of parameters ({Parameters.Count})");
			return Parameters.Concat(Enumerable.Repeat(VarArg, count - Parameters.Count));
		}
	}
	
	public IEnumerable<TReturn> ZipArgs<TReturn>(ICollection<TypedValue> arguments, Func<FunctionParameter, TypedValue, TReturn> zip)
	{
		return ParamsOfCount(arguments.Count).Zip(arguments, zip);
	}
	
	public IEnumerable<(TypedType Type, string Name)> TupleParams => Parameters.Select(param => (param.Type.Type, param.Name));
	
	public void Transform(IHasIdentifiers context)
	{
		foreach (FunctionParameter parameter in Parameters)
			parameter.Type.Transform(context, Visitor.TypeType);
		VarArg?.Type.Transform(context, Visitor.TypeType);
	}
	
	public TypeIdentifier this[string name]
	{
		get
		{
			FunctionParameter? parameter = Parameters.FirstOrDefault(param => param.Name == name);
			if (parameter is not null)
				return parameter.Type;
			
			if (VarArg is not null && VarArg.Name == name)
				return VarArg.Type;
			
			throw new KeyNotFoundException($"Parameter {name} does not exist");
		}
	}
	
	public override string ToString() => $"({string.Join(", ", Parameters)}{(VarArg is not null ? $", {VarArg.Type}... {VarArg.Name}" : "")})";
}

public class FunctionParameter(TypeIdentifier type, string name)
{
	public TypeIdentifier Type => type;
	public string Name => name;
	
	public override string ToString() => $"{Type} {Name}";
}

public interface Args
{
	public TypedValue this[string key] { get; set; }
}

public class FunctionArgs : Args
{
	public List<string> Keys = [];
	private Dictionary<string, (TypeIdentifier Type, TypedValue? Value)> args = new();
	
	public FunctionArgs(FunctionParameters parameters)
	{
		foreach (FunctionParameter param in parameters.Parameters)
		{
			Keys.Add(param.Name);
			args.Add(param.Name, (param.Type, null));
		}
		if (parameters.VarArg is not null)
		{
			FunctionParameter param = parameters.VarArg;
			TypedType varArgType = new TypedTypeCompilerList<TypedTypeCompilerAnyValue>(Visitor.AnyValueType);
			Keys.Add(param.Name);
			args.Add(param.Name, (varArgType.Id(), null));
		}
	}
	
	public FunctionArgs(FunctionParameters parameters, IList<TypedValue> arguments)
	{
		int i = 0;
		for (; i < parameters.Parameters.Count; i++)
		{
			FunctionParameter param = parameters.Parameters[i];
			TypedValue arg = arguments[i];
			Keys.Add(param.Name);
			args.Add(param.Name, (param.Type, arg));
		}
		if (parameters.VarArg is not null)
		{
			FunctionParameter param = parameters.VarArg;
			TypedType varArgType = new TypedTypeCompilerList<TypedTypeCompilerAnyValue>(Visitor.AnyValueType);
			Keys.Add(param.Name);
			args.Add(param.Name, (varArgType.Id(), new TypedValueCompiler<List<TypedValue>>(varArgType, arguments.Skip(i).ToList())));
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