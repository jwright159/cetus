using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerExpression(TypedType returnType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreateFunction(returnType.LLVMType, [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false), 0);
	public TypedType ReturnType => returnType;
	public override string ToString() => LLVMType.ToString();
}