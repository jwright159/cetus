using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineProgram() : TypedTypeFunction("DefineProgram", Visitor.VoidType, [(Visitor.AnyFunctionCall.List(), "statements")], null)
{
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		return new DefineProgramCall(
			((TypedValueCompiler<List<FunctionCallContext>>)args["statements"]).CompilerValue);
	}
}

public class DefineProgramCall(List<FunctionCallContext> statements) : TypedValue, IHasIdentifiers
{
	public TypedType Type => Visitor.VoidType;
	public LLVMValueRef LLVMValue => Visitor.Void.LLVMValue;
	public IDictionary<string, TypedValue> Identifiers { get; set; }
	public ICollection<IFunctionContext> Functions { get; set; }
	public ICollection<TypedType> Types { get; set; }
	public List<IFunctionContext>? FinalizedFunctions { get; set; }
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		
	}
}