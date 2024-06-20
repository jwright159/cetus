using Cetus.Parser.Tokens;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class While : TypedTypeFunctionSimple
{
	public override string Name => "While";
	public override IToken Pattern => new TokenString([new LiteralToken("While"), new LiteralToken("("), new ParameterExpressionToken("condition"), new LiteralToken(")"), new ParameterExpressionToken("body")]);
	public override TypeIdentifier ReturnType => Visitor.VoidType.Id();
	public override FunctionParameters Parameters => new([(new TypedTypeCompilerExpression(Visitor.BoolType), "condition"), (new TypedTypeCompilerClosure(Visitor.VoidType), "body")], null);
	public override float Priority => 100;

	protected override bool AutoVisit => false;
	
	public override LLVMValueRef? Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		Expression condition = (Expression)args["condition"];
		Closure body = (Closure)((Expression)args["body"]).ReturnValue;
		body.BlockName = "whileBody";
		
		LLVMBasicBlockRef entryBlock = visitor.Builder.InsertBlock;
		LLVMBasicBlockRef conditionBlock = entryBlock.Parent.AppendBasicBlock("whileCondition");
		
		visitor.Builder.BuildBr(conditionBlock);
		visitor.Builder.PositionAtEnd(conditionBlock);
		condition.Visit(context, Parameters["condition"].Type, visitor);
		
		body.Visit(context, Parameters["body"].Type, visitor);
		LLVMBasicBlockRef mergeBlock = entryBlock.Parent.AppendBasicBlock("whileMerge");
		visitor.Builder.BuildCondBr(condition.ReturnValue.LLVMValue, body.Block, mergeBlock);
		visitor.Builder.PositionAtEnd(body.Block);
		visitor.Builder.BuildBr(conditionBlock);
		
		visitor.Builder.PositionAtEnd(mergeBlock);
		return null;
	}
}