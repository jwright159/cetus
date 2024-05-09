using System.Diagnostics;
using LLVMSharp.Interop;

LLVM.LinkInMCJIT();
LLVM.InitializeX86TargetInfo();
LLVM.InitializeX86Target();
LLVM.InitializeX86TargetMC();
LLVM.InitializeX86AsmPrinter();


LLVMModuleRef module = LLVMModuleRef.CreateWithName("mainModule");
LLVMBuilderRef builder = LLVMBuilderRef.Create(module.Context);

LLVMTypeRef printfFunctionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], true);
LLVMValueRef printfFunction = module.AddFunction("printf", printfFunctionType);

LLVMValueRef mainFunction = module.AddFunction("main", LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [], false));
builder.PositionAtEnd(mainFunction.AppendBasicBlock("entry"));

Printf("Hello, World!\n");

LLVMValueRef intVal = builder.BuildAlloca(LLVMTypeRef.Int32, "intVal");
builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 42, false), intVal);
Printf("intVal: %p\n", intVal);
LLVMValueRef loadedIntVal = builder.BuildLoad2(LLVMTypeRef.Int32, intVal, "loadedIntVal");
Printf("loadedIntVal: %d\n", loadedIntVal);

LLVMTypeRef structType = LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0)], false);
LLVMValueRef structValue = builder.BuildAlloca(structType, "structValue");
Printf("structValue: %p\n", structValue);
LLVMValueRef structField = builder.BuildStructGEP2(structType, structValue, 0, "structField");
Printf("structField: %p\n", structField);
builder.BuildStore(intVal, structField);
LLVMValueRef loadedStructField = builder.BuildLoad2(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int32, 0), structField, "loadedStructField");
Printf("loadedStructField: %p\n", loadedStructField);
LLVMValueRef loadedStructFieldVal = builder.BuildLoad2(LLVMTypeRef.Int32, loadedStructField, "loadedStructFieldVal");
Printf("loadedStructFieldVal: %d\n", loadedStructFieldVal);

builder.BuildRetVoid();


module.Dump();

module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);

Compile();
return;

void Printf(string format, params LLVMValueRef[] args)
{
	LLVMValueRef formatString = builder.BuildGlobalStringPtr(format, format.Length == 0 ? "emptyString" : format);
	builder.BuildCall2(printfFunctionType, printfFunction, args.Prepend(formatString).ToArray(), "printfCall");
}

void Compile(string filename = "main")
{
	const string targetTriple = "x86_64-pc-windows-msvc";
	LLVMTargetRef target = LLVMTargetRef.GetTargetFromTriple(targetTriple);
	LLVMTargetMachineRef targetMachine = target.CreateTargetMachine(targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
	
	targetMachine.EmitToFile(module, filename + ".s", LLVMCodeGenFileType.LLVMAssemblyFile);
	
	CompileSFileToExe(filename + ".s", filename + ".exe");
}

void CompileSFileToExe(string sFilePath, string exeFilePath)
{
	ProcessStartInfo startInfo = new()
	{
		FileName = "clang",
		Arguments = $"{sFilePath} -o {exeFilePath} -L lib",
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