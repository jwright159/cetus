using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public abstract class TypedTypeFunction(string name, TypedType returnType, (TypedType Type, string Name)[] parameters, TypedType? varArgType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(returnType.LLVMType, Parameters.Select(param => param.Type.LLVMType).ToArray(), IsVarArg);
	public string Name => name;
	public TypedType ReturnType => returnType;
	public (TypedType Type, string Name)[] Parameters => parameters;
	public int NumParams => parameters.Length;
	public TypedType? VarArgType => varArgType;
	public bool IsVarArg => varArgType is not null;
	public override string ToString() => LLVMType.ToString();
	
	public abstract TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args);
}