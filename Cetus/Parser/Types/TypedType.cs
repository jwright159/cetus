using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
	public string Name { get; }
	public TypedType? InnerType => null;
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
			LLVMValueRef valueValue = visitor.Builder.BuildLoad2(resultTypePointer.InnerType.LLVMType, value.LLVMValue, "loadtmp");
			value = new TypedValueValue(resultTypePointer.InnerType, valueValue);
		}
		
		if (!value.IsOfType(typeHint))
			throw new Exception($"Type mismatch in value of '{name}', expected {typeHint} but got {value.Type}");
		
		return value;
	}
	
	public static TypedTypeCompilerList<T> List<T>(this T type) where T : TypedType => new(type);
}