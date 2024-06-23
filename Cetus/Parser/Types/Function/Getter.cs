using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Struct;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class Getter(TypedType @struct, StructField field) : TypedTypeFunctionSimple
{
	public override string Name => $"{@struct.Name}.Get_{field.Name}";
	public override IToken Pattern { get; } = new TokenString([new ParameterExpressionToken("this"), new LiteralToken("."), new LiteralToken(field.Name)]);
	public override TypeIdentifier ReturnType => field.TypeIdentifier.Pointer();
	public override FunctionParameters Parameters { get; } = new([(@struct.Pointer(), "this")], null);
	public override float Priority => 0;
	
	public override LLVMValueRef? VisitResult(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildStructGEP2(@struct.LLVMType, args["this"].LLVMValue, field.Index, field.Name + "Ptr");
	}
}