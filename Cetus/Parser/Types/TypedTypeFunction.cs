using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public abstract class TypedTypeFunction(string name, TypedType returnType, TypedType[] paramTypes, TypedType? varArgType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(returnType.LLVMType, paramTypes.Select(paramType => paramType.LLVMType).ToArray(), IsVarArg);
	public string Name => name;
	public TypedType ReturnType => returnType;
	public TypedType[] ParamTypes => paramTypes;
	public int NumParams => paramTypes.Length;
	public TypedType? VarArgType => varArgType;
	public bool IsVarArg => varArgType is not null;
	public override string ToString() => LLVMType.ToString();
	
	public abstract TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args);
}