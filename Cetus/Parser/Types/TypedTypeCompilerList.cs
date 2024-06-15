using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerList<T>(T elementType) : TypedType where T : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("Compiler list does not have an LLVM type");
	public TypedType ElementType => elementType;
	public string Name => $"List<{elementType}>";
	public override string ToString() => Name;
}

public static class TypedTypeCompilerList
{
	public static TypedTypeCompilerList<T> Of<T>() where T : TypedType, new() => new(new T());
	public static TypedTypeCompilerList<T> Of<T>(T elementType) where T : TypedType, new() => new(elementType);
}