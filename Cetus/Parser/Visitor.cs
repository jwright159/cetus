using System.Diagnostics;
using Cetus.Parser.Types;
using Cetus.Parser.Types.Program;
using Cetus.Parser.Values;
using JetBrains.Annotations;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class Visitor
{
	public LLVMModuleRef Module { get; }
	public LLVMBuilderRef Builder { get; }
	
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
		
		Module = LLVMModuleRef.CreateWithName("mainModule");
		Builder = LLVMBuilderRef.Create(Module.Context);
	}
	
	public void Visit(Program program)
	{
		Console.WriteLine("Visiting...");
		VisitProgram(program);
		Dump();
		Module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	private void VisitProgram(Program program)
	{
		program.Call.Visit(program, null, this);
	}
	
	[UsedImplicitly]
	private void Printf(string message, Program program, params TypedValue[] args)
	{
		TypedValue function = (program as IHasIdentifiers).Identifiers["printf"];
		TypedTypeFunction functionType = (TypedTypeFunction)function.Type;
		TypedValueValue messageValue = new(StringType, Builder.BuildGlobalStringPtr(message, "message"));
		FunctionArgs functionArgs = new(functionType.Parameters);
		functionArgs["args"] = new TypedValueCompiler<List<TypedValue>>(new TypedTypeCompilerValue().List(), args.Prepend(messageValue).ToList());
		functionType.Call(program, functionArgs);
	}
	
	public void Optimize()
	{
		LLVMPassManagerRef passManager = LLVMPassManagerRef.Create();
		passManager.AddConstantMergePass();
		passManager.AddInstructionCombiningPass();
		passManager.AddPromoteMemoryToRegisterPass();
		passManager.AddGVNPass();
		passManager.AddCFGSimplificationPass();
		passManager.Run(Module);
		passManager.Dispose();
	}
	
	public void Dispose()
	{
		Module.Dispose();
		Builder.Dispose();
	}
	
	public void Dump()
	{
		Module.Dump();
	}
	
	public void Compile(Program program, string filename = "main")
	{
		const string targetTriple = "x86_64-pc-windows-msvc";
		LLVMTargetRef target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
		LLVMTargetMachineRef targetMachine = target.CreateTargetMachine(targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		
		targetMachine.EmitToFile(Module, filename + ".s", LLVMCodeGenFileType.LLVMAssemblyFile);
		
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