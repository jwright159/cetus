﻿using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public static class TypedTypeExtensions
{
	public static bool IsOfType(this TypedValue value, TypedType type)
	{
		if (type is TypedTypeType)
		{
			return value is TypedValueType;
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
}