using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineProgram : TypedTypeFunctionBase
{
	public override string Name => "DefineProgram";
	public override IToken Pattern => new TokenSplit(new PassToken(), new LiteralToken(";"), new EOFToken(), new ParameterStatementToken("statements"));
	public override TypeIdentifier ReturnType => new(Visitor.VoidType);
	public override FunctionParameters Parameters => new([(Visitor.AnyFunctionCall.List(), "statements")], null);
	public override float Priority => 100;
	
	public override TypedValue Call(IHasIdentifiers context, FunctionArgs args)
	{
		List<FunctionCall> statements = ((TypedValueCompiler<List<FunctionCall>>)args["statements"]).CompilerValue;
		Console.WriteLine("STATEMENTS: [\n\t" + string.Join(",\n", statements).Replace("\n", "\n\t") + "\n]");
		return new DefineProgramCall(
			context,
			statements.Select(statement => statement.Call(context)).ToList());
	}
}

public class DefineProgramCall(IHasIdentifiers parent, List<TypedValue> statements) : TypedValue, IHasIdentifiers
{
	public TypedType Type => Visitor.VoidType;
	public LLVMValueRef LLVMValue => Visitor.Void.LLVMValue;
	public IDictionary<string, TypedValue> Identifiers { get; set; } = new NestedDictionary<string, TypedValue>(parent.Identifiers);
	public ICollection<TypedTypeFunction> Functions { get; set; } = new NestedCollection<TypedTypeFunction>(parent.Functions);
	public ICollection<TypedType> Types { get; set; } = new NestedCollection<TypedType>(parent.Types);
	public List<TypedTypeFunction>? FinalizedFunctions { get; set; }
	
	public void Parse(IHasIdentifiers context)
	{
		statements.ForEach(statement => statement.Parse(this));
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		statements.ForEach(statement => statement.Transform(this, null));
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		statements.ForEach(statement => statement.Visit(this, null, visitor));
	}
}