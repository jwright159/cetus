﻿using Cetus.Parser.Tokens;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Add : TypedTypeFunctionSimple
{
	public override string Name => "Add";
	public override IToken Pattern => new TokenString([new ParameterExpressionToken("a"), new LiteralToken("+"), new ParameterExpressionToken("b")]);
	public override TypeIdentifier ReturnType => Visitor.IntType.Id();
	public override FunctionParameters Parameters => new([(Visitor.IntType, "a"), (Visitor.IntType, "b")], null);
	public override float Priority => 30;
	
	public override LLVMValueRef? VisitResult(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildAdd(args["a"].LLVMValue, args["b"].LLVMValue, "addtmp");
	}
}