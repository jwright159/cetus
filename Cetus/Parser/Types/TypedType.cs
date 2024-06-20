using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
	public string Name { get; }
}

public interface TypedTypeWithPattern : TypedType
{
	public IToken Pattern { get; }
	public float Priority { get; }
	public TypeParameters TypeParameters { get; }
	
	public TypedType Call(IHasIdentifiers context, TypeArgs args);
}

public static class TypedTypeExtensions
{
	public static bool IsOfType(this TypedValue value, TypedType type)
	{
		if (type is TypedTypeType)
		{
			return value is TypedValueType;
		}
		
		if (type is TypedTypeCompilerAnyFunction)
		{
			return value.Type is TypedTypeFunction;
		}
		
		return TypesEqual(value.Type, type);
	}
	
	public static bool TypesEqual(TypedType lhs, TypedType rhs) => TypesEqual(lhs.LLVMType, rhs.LLVMType);
	public static bool TypesEqual(LLVMTypeRef lhs, LLVMTypeRef rhs)
	{
		while (lhs.Kind == LLVMTypeKind.LLVMPointerTypeKind && rhs.Kind == LLVMTypeKind.LLVMPointerTypeKind)
		{
			lhs = lhs.ElementType;
			rhs = rhs.ElementType;
		}
		
		if (lhs.Kind != rhs.Kind)
			return false;
		
		if (lhs.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
			return lhs.IntWidth == rhs.IntWidth;
		
		if (lhs.Kind == LLVMTypeKind.LLVMFunctionTypeKind)
			return TypesEqual(lhs.ReturnType, rhs.ReturnType) && lhs.ParamTypesCount == rhs.ParamTypesCount && lhs.ParamTypes.Zip(rhs.ParamTypes).All(pair => TypesEqual(pair.First, pair.Second));
		
		return true;
	}
	
	public static TypedTypePointer Pointer(this TypedType type) => new(type);
	
	public static TypedValue CoersePointer(this TypedValue value, TypedType typeHint, Visitor visitor, string name)
	{
		if (typeHint is not TypedTypePointer && value.Type is TypedTypePointer resultTypePointer)
		{
			LLVMValueRef valueValue = visitor.Builder.BuildLoad2(resultTypePointer.InnerType.LLVMType, value.LLVMValue, name + "loadtmp");
			value = new TypedValueValue(resultTypePointer.InnerType, valueValue);
		}
		
		if (typeHint is TypedTypePointer typeHintPointer && value.Type is not TypedTypePointer)
		{
			LLVMValueRef valuePtr = visitor.Builder.BuildAlloca(typeHintPointer.InnerType.LLVMType, name + "storetmp");
			visitor.Builder.BuildStore(value.LLVMValue, valuePtr);
			value = new TypedValueValue(value.Type.Pointer(), valuePtr);
		}
		
		if (!value.IsOfType(typeHint))
			throw new Exception($"Type mismatch in value of '{name}', expected {typeHint} but got {value.Type}");
		
		return value;
	}
	
	public static TypedTypeCompilerList<T> List<T>(this T type) where T : TypedType => new(type);
	
	public static TypeIdentifierBase Id(this TypedType type) => new(type);
}

public class TypeParameters
{
	public TypeParameters(IEnumerable<TypeParameter> parameters)
	{
		Parameters = parameters.ToList();
	}
	
	public TypeParameters(IEnumerable<string> parameters)
	{
		Parameters = parameters.Select(param => new TypeParameter(param)).ToList();
	}
	
	public List<TypeParameter> Parameters;
	
	public int Count => Parameters.Count;
	
	public override string ToString() => $"({string.Join(", ", Parameters)})";
}

public class TypeParameter(string name)
{
	public string Name => name;
	
	public override string ToString() => $"{Name}";
}

public class TypeArgs : Args
{
	public List<string> Keys = [];
	private Dictionary<string, TypeIdentifier?> args = new();
	
	public TypeArgs(TypeParameters parameters)
	{
		foreach (TypeParameter param in parameters.Parameters)
		{
			Keys.Add(param.Name);
			args.Add(param.Name, null);
		}
	}
	
	public TypeArgs(TypeParameters parameters, IList<TypeIdentifier> arguments)
	{
		int i = 0;
		for (; i < parameters.Parameters.Count; i++)
		{
			TypeParameter param = parameters.Parameters[i];
			TypeIdentifier arg = arguments[i];
			Keys.Add(param.Name);
			args.Add(param.Name, arg);
		}
	}
	
	public TypedValue this[string key]
	{
		get
		{
			if (!args.TryGetValue(key, out TypeIdentifier? arg))
				throw new KeyNotFoundException($"Argument {key} does not exist");
			return arg;
		}
		set
		{
			if (!args.TryGetValue(key, out TypeIdentifier? arg))
				throw new KeyNotFoundException($"Argument {key} does not exist");
			args[key] = (TypeIdentifier)value;
		}
	}
	
	public void Parse(IHasIdentifiers context)
	{
		foreach (TypeIdentifier arg in args.Values)
			arg?.Parse(context);
	}
	
	public void Transform(IHasIdentifiers context)
	{
		foreach (TypeIdentifier arg in args.Values)
			arg?.Transform(context, null);
	}
	
	public void Visit(IHasIdentifiers context, Visitor visitor)
	{
		foreach (TypeIdentifier arg in args.Values)
			arg?.Visit(context, null, visitor);
	}
	
	public override string ToString() => "(\n\t" + string.Join(",\n", args.Select(arg => $"{arg.Key}: {arg.Value}")).Replace("\n", "\n\t") + "\n)";
}