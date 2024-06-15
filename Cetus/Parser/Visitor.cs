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
	
	public static readonly TypedTypeVoid VoidType = new();
	public static readonly TypedTypeInt IntType = new();
	public static readonly TypedTypeBool BoolType = new();
	public static readonly TypedTypeChar CharType = new();
	public static readonly TypedTypeFloat FloatType = new();
	public static readonly TypedTypeDouble DoubleType = new();
	public static readonly TypedTypePointer StringType = new(CharType);
	public static readonly TypedTypeCompilerString CompilerStringType = new();
	public static readonly TypedTypeType TypeType = new();
	public static readonly TypedTypeCompilerAnyFunctionCall AnyFunctionCall = new();
	public static readonly TypedTypeCompilerTypeIdentifier TypeIdentifierType = new();
	public static readonly TypedTypeCompilerAnyFunction AnyFunctionType = new();
	public static readonly TypedTypeCompilerAnyValue AnyValueType = new();
	
	public static readonly TypedValue Void = new TypedValueType(VoidType);
	
	public static readonly TypedValue TrueValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false));
	public static readonly TypedValue FalseValue = new TypedValueValue(BoolType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0, false));
	
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
	
	public void Visit(ProgramContext program)
	{
		Console.WriteLine("Visiting...");
		VisitProgram(program);
		Dump();
		module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	private void VisitProgram(ProgramContext program)
	{
		program.Call.Visit(program.Call, null, builder);
	}
	
	[UsedImplicitly]
	private void Printf(string message, ProgramContext program, params TypedValue[] args)
	{
		TypedValue function = program.Call.Identifiers["printf"];
		TypedTypeFunction functionType = (TypedTypeFunction)function.Type;
		TypedValueValue messageValue = new(StringType, builder.BuildGlobalStringPtr(message, "message"));
		FunctionArgs functionArgs = new(functionType.Parameters);
		functionArgs["args"] = args.Prepend(messageValue).ToArray();
		FunctionCallContext functionCall = new(new TypedValueType(functionType), functionArgs);
		functionCall.Call(program.Call);
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