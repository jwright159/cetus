using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class While() : TypedTypeFunctionSimple("While", Visitor.VoidType, [(new TypedTypeCompilerExpression(Visitor.BoolType), "condition"), (new TypedTypeCompilerClosure(Visitor.VoidType), "body")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		Expression condition = ((TypedValueCompiler<Expression>)args["condition"]).CompilerValue;
		Closure body = ((TypedValueCompiler<Closure>)args["body"]).CompilerValue;
		LLVMBasicBlockRef merge = builder.InsertBlock.Parent.AppendBasicBlock("whileMerge");
		
		builder.BuildBr(condition.Block);
		
		builder.PositionAtEnd(condition.Block);
		builder.BuildCondBr(condition.ReturnValue.LLVMValue, body.Block, merge);
		
		builder.PositionAtEnd(body.Block);
		builder.BuildBr(condition.Block);
		
		builder.PositionAtEnd(merge);
		return Visitor.Void.LLVMValue;
	}
}