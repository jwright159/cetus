using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunction(string name, TypedType returnType, IReadOnlyCollection<TypedType> paramTypes, TypedType? varArgType, string? pattern) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreateFunction(returnType.LLVMType, paramTypes.Select(paramType => paramType.LLVMType).ToArray(), IsVarArg);
	public string FunctionName => name;
	public TypedType ReturnType => returnType;
	public IEnumerable<TypedType> ParamTypes => paramTypes;
	public int NumParams => paramTypes.Count;
	public TypedType? VarArgType => varArgType;
	public bool IsVarArg => varArgType is not null;
	public string? Pattern => pattern;
	public override string ToString() => LLVMType.ToString();
	
	public virtual TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		return new TypedValueValue(ReturnType, builder.BuildCall2(LLVMType, function.Value, args.Select(arg => arg.Value).ToArray(), ReturnType is TypedTypeVoid ? "" : name + "Call"));
	}
}