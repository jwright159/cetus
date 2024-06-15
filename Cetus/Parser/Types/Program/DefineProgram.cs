using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineProgram : TypedTypeFunctionBase
{
	public override string Name => "DefineProgram";
	public override IToken Pattern => new TokenSplit(new PassToken(), new LiteralToken(";"), new EOFToken(), new ParameterExpressionToken("statements"));
	public override TypeIdentifier ReturnType => new(Visitor.VoidType);
	public override FunctionParameters Parameters => new([(Visitor.AnyFunctionCall.List(), "statements")], null);
	public override float Priority => 100;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineProgramCall(
			((TypedValueCompiler<List<FunctionCall>>)args["statements"]).CompilerValue);
	}
}

public class DefineProgramCall(List<FunctionCall> statements) : TypedValue, IHasIdentifiers
{
	public TypedType Type => Visitor.VoidType;
	public LLVMValueRef LLVMValue => Visitor.Void.LLVMValue;
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<TypedTypeFunction> Functions { get; set; }
	public ICollection<TypedType> Types { get; set; }
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		
	}
}