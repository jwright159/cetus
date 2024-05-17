using System.Diagnostics;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using JetBrains.Annotations;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Visitor
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	
	public static readonly TypedType VoidType = new TypedTypeVoid();
	public static readonly TypedType IntType = new TypedTypeInt();
	public static readonly TypedType BoolType = new TypedTypeBool();
	public static readonly TypedType CharType = new TypedTypeChar();
	public static readonly TypedType FloatType = new TypedTypeFloat();
	public static readonly TypedType DoubleType = new TypedTypeDouble();
	public static readonly TypedType StringType = new TypedTypePointer(CharType);
	public static readonly TypedType CompilerStringType = new TypedTypeCompilerString();
	public static readonly TypedType TypeType = new TypedTypeType();
	
	public static readonly TypedValue Void = new TypedValueType(VoidType);
	
	public static readonly TypedValue TrueValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false));
	public static readonly TypedValue FalseValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0, false));
	
	public static readonly TypedTypeFunction DeclareFunctionType = new TypedTypeFunctionDeclare();
	public static readonly TypedTypeFunction DefineFunctionType = new TypedTypeFunctionDefine();
	public static readonly TypedTypeFunction AssignFunctionType = new TypedTypeFunctionAssign();
	public static readonly TypedTypeFunction ReturnFunctionType = new TypedTypeFunctionReturn();
	public static readonly TypedTypeFunction ReturnVoidFunctionType = new TypedTypeFunctionReturnVoid();
	public static readonly TypedTypeFunction AddFunctionType = new TypedTypeFunctionAdd();
	public static readonly TypedTypeFunction LessThanFunctionType = new TypedTypeFunctionLessThan();
	public static readonly TypedTypeFunction WhileFunctionType = new TypedTypeFunctionWhile();
	
	public Visitor()
	{
		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();
		
		module = LLVMModuleRef.CreateWithName("mainModule");
		builder = LLVMBuilderRef.Create(module.Context);
	}
	
	public void Visit(ProgramContext context)
	{
		Console.WriteLine("Visiting...");
		
		context.Identifiers = new Dictionary<string, TypedValue>
		{
			{ "Void", new TypedValueType(VoidType) },
			{ "Float", new TypedValueType(FloatType) },
			{ "Double", new TypedValueType(DoubleType) },
			{ "Char", new TypedValueType(CharType) },
			{ "Int", new TypedValueType(IntType) },
			{ "String", new TypedValueType(StringType) },
			{ "CompilerString", new TypedValueType(CompilerStringType) },
			{ "Bool", new TypedValueType(BoolType) },
			{ "Type", new TypedValueType(TypeType) },
			
			{ "True", TrueValue },
			{ "False", FalseValue },
			
			{ "Declare", new TypedValueType(DeclareFunctionType) },
			{ "Define", new TypedValueType(DefineFunctionType) },
			{ "Assign", new TypedValueType(AssignFunctionType) },
			{ "Return", new TypedValueType(ReturnFunctionType) },
			{ "ReturnVoid", new TypedValueType(ReturnVoidFunctionType) },
			{ "Add", new TypedValueType(AddFunctionType) },
			{ "LessThan", new TypedValueType(LessThanFunctionType) },
			{ "While", new TypedValueType(WhileFunctionType) },
		};
		VisitProgram(context);
		Dump();
		module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	[UsedImplicitly]
	private void Printf(string message, ProgramContext context, params TypedValue[] args)
	{
		TypedValue function = context.Identifiers["printf"];
		TypedTypeFunction functionType = (TypedTypeFunction)function.Type;
		TypedValueValue messageValue = new(StringType, builder.BuildGlobalStringPtr(message, "message"));
		args = args.Prepend(messageValue).ToArray();
		functionType.Call(builder, function, context, args);
	}
	
	public void Optimize()
	{
		LLVMPassManagerRef passManager = LLVMPassManagerRef.Create();
		passManager.AddConstantMergePass();
		passManager.AddInstructionCombiningPass();
		passManager.AddPromoteMemoryToRegisterPass();
		passManager.AddGVNPass();
		passManager.AddCFGSimplificationPass();
		passManager.Run(module);
		passManager.Dispose();
	}
	
	public void Dispose()
	{
		module.Dispose();
		builder.Dispose();
	}
	
	public void Dump()
	{
		module.Dump();
	}
	
	public void Compile(ProgramContext program, string filename = "main")
	{
		const string targetTriple = "x86_64-pc-windows-msvc";
		LLVMTargetRef target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
		LLVMTargetMachineRef targetMachine = target.CreateTargetMachine(targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		
		targetMachine.EmitToFile(module, filename + ".s", LLVMCodeGenFileType.LLVMAssemblyFile);
		
		CompileSFileToExe(filename + ".s", filename + ".exe", program.Libraries);
	}
	
	private void CompileSFileToExe(string sFilePath, string exeFilePath, IEnumerable<string> libraries)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "clang",
			Arguments = $"{sFilePath} -o {exeFilePath} -L lib {string.Join(" ", libraries.Select(lib => "-l" + lib))}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		
		using Process? process = Process.Start(startInfo);
		if (process == null)
			throw new Exception("Failed to start the compilation process");
		
		process.WaitForExit();
		
		string output = process.StandardOutput.ReadToEnd();
		Console.Write(output);
		string error = process.StandardError.ReadToEnd();
		Console.Write(error);
		
		if (process.ExitCode != 0)
			throw new Exception($"Compilation failed with exit code {process.ExitCode}");
	}
}