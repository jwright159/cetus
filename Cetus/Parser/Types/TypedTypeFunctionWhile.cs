using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionWhile() : TypedTypeFunction("While", Visitor.VoidType, [new TypedTypeCompilerExpression(Visitor.BoolType), new TypedTypeCompilerClosure(Visitor.VoidType)], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		TypedValueCompilerExpression condition = (TypedValueCompilerExpression)args[0];
		TypedValueCompilerClosure loop = (TypedValueCompilerClosure)args[1];
		LLVMBasicBlockRef merge = builder.InsertBlock.Parent.AppendBasicBlock("whileMerge");
		
		builder.BuildBr(condition.Block);
		
		builder.PositionAtEnd(condition.Block);
		builder.BuildCondBr(condition.ReturnValue.Value, loop.Block, merge);
		
		builder.PositionAtEnd(loop.Block);
		builder.BuildBr(condition.Block);
		
		builder.PositionAtEnd(merge);
		return Visitor.Void;
	}
}