using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Program;

public class DefineFunction() : TypedTypeFunction("DefineFunction", Visitor.VoidType, [(Visitor.CompilerStringType, "name"), (Visitor.CompilerStringType, "returnType"), (Visitor.CompilerStringType[], "parameterTypes"), (Visitor.CompilerStringType[], "parameterNames"), (Visitor.CompilerStringType, "varArgParameterType"), (Visitor.CompilerStringType, "varArgParameterType"), (new TypedTypeCompilerClosure(Visitor.VoidType)?, "body")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		
	}
}