using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class While() : TypedTypeFunctionSimple("While", Visitor.VoidType, [(new TypedTypeCompilerExpression(Visitor.BoolType), "condition"), (new TypedTypeCompilerClosure(Visitor.VoidType), "body")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		Expression condition = ((TypedValueCompiler<Expression>)args["condition"]).CompilerValue;
		Closure body = ((TypedValueCompiler<Closure>)args["body"]).CompilerValue;
		LLVMBasicBlockRef merge = visitor.Builder.InsertBlock.Parent.AppendBasicBlock("whileMerge");
		
		visitor.Builder.BuildBr(condition.Block);
		
		visitor.Builder.PositionAtEnd(condition.Block);
		visitor.Builder.BuildCondBr(condition.ReturnValue.LLVMValue, body.Block, merge);
		
		visitor.Builder.PositionAtEnd(body.Block);
		visitor.Builder.BuildBr(condition.Block);
		
		visitor.Builder.PositionAtEnd(merge);
		return Visitor.Void.LLVMValue;
	}
}