using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Program;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class Getter(TypedType @struct, StructField field) : TypedTypeFunctionSimple
{
	public override string Name => $"{@struct.Name}.Get_{field.Name}";
	public override IToken Pattern { get; } = new TokenString([new ParameterExpressionToken(field.Name), new LiteralToken("."), new LiteralToken(field.Name)]);
	public override TypeIdentifier ReturnType => field.TypeIdentifier;
	public override FunctionParameters Parameters { get; } = new([(field.TypeIdentifier.Type.Pointer(), field.Name)], null);
	public override float Priority => 0;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildStructGEP2(@struct.LLVMType, args["value"].LLVMValue, (uint)field.Index, field.Name + "Ptr");
	}
}