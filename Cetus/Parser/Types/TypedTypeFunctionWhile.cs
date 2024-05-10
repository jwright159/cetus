using Cetus.Parser.Contexts;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionWhile() : TypedTypeFunction("While", Parser.VoidType, [new TypedTypeCompilerClosure(Parser.IntType), new TypedTypeCompilerClosure(Parser.VoidType)], null, "while ( $0 ) $1")
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, FunctionContext context, params TypedValue[] args)
	{
		TypedValueCompilerClosure conditionClosure = (TypedValueCompilerClosure)args[0];
		TypedValueCompilerClosure loopClosure = (TypedValueCompilerClosure)args[1];
		LLVMBasicBlockRef merge = builder.InsertBlock.Parent.AppendBasicBlock("whileMerge");
		
		builder.BuildBr(conditionClosure.Block);
		
		builder.PositionAtEnd(conditionClosure.Block);
		TypedValue conditionValue = conditionClosure.ReturnValue ?? throw new Exception("While condition is missing a return value");
		LLVMValueRef condition = builder.BuildTrunc(conditionValue.Value, LLVMTypeRef.Int1, "whileCondition");
		builder.BuildCondBr(condition, loopClosure.Block, merge);
		
		builder.PositionAtEnd(loopClosure.Block);
		builder.BuildBr(conditionClosure.Block);
		
		builder.PositionAtEnd(merge);
		return Parser.Void;
	}
}