using System.Diagnostics;
using System.Runtime.InteropServices;
using LLVMSharp;

namespace CTes;

public class CodeGenerator
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	private LLVMValueRef mainFunction;
	
	private LLVMValueRef printfFunction;
	private LLVMValueRef format;
	
	public CodeGenerator()
	{
		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();
		
		module = LLVM.ModuleCreateWithName("mainModule");
		builder = LLVM.CreateBuilder();
	}
	
	public void Generate(IEnumerable<Expression> expressions)
	{
		LLVMTypeRef[] paramTypes = [];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.VoidType(), paramTypes, false);
		mainFunction = LLVM.AddFunction(module, "main", functionType);
		
		LLVMBasicBlockRef entry = LLVM.AppendBasicBlock(mainFunction, "entry");
		LLVM.PositionBuilderAtEnd(builder, entry);
		
		GeneratePrintfBinding();
		
		foreach (Expression expression in expressions)
		{
			LLVMValueRef result = GenerateExpression(expression);
			LLVM.BuildCall(builder, printfFunction, [format, result], "printfCall");
		}
		
		LLVM.BuildRetVoid(builder);
	}
	
	private void GeneratePrintfBinding()
	{
		format = LLVM.BuildGlobalStringPtr(builder, "%f\n", "format");
		
		LLVMTypeRef[] paramTypes = [LLVM.PointerType(LLVM.Int8Type(), 0)];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.Int32Type(), paramTypes, true);
		printfFunction = LLVM.AddFunction(module, "printf", functionType);
	}
	
	private LLVMValueRef GenerateExpression(Expression expression)
	{
		if (expression is NumberExpression numberExpression)
		{
			return LLVM.ConstReal(LLVM.DoubleType(), numberExpression.Value);
		}
		
		if (expression is IdentifierExpression identifierExpression)
		{
			throw new NotImplementedException("Variable declarations are not supported yet");
		}
		
		if (expression is BinaryExpression binaryExpression)
		{
			LLVMValueRef left = GenerateExpression(binaryExpression.Left);
			LLVMValueRef right = GenerateExpression(binaryExpression.Right);
			
			switch (binaryExpression.Operator.Value)
			{
				case "+":
					return LLVM.BuildFAdd(builder, left, right, "addtmp");
				case "-":
					return LLVM.BuildFSub(builder, left, right, "subtmp");
				case "*":
					return LLVM.BuildFMul(builder, left, right, "multmp");
				case "/":
					return LLVM.BuildFDiv(builder, left, right, "divtmp");
				default:
					throw new Exception($"Unsupported operator: {binaryExpression.Operator.Value}");
			}
		}
		
		throw new Exception($"Unsupported expression type: {expression.GetType()}");
	}
	
	public void Optimize()
	{
		LLVMPassManagerRef passManager = LLVM.CreatePassManager();
		LLVM.AddConstantPropagationPass(passManager);
		LLVM.AddInstructionCombiningPass(passManager);
		LLVM.AddPromoteMemoryToRegisterPass(passManager);
		LLVM.AddGVNPass(passManager);
		LLVM.AddCFGSimplificationPass(passManager);
		LLVM.RunPassManager(passManager, module);
		LLVM.DisposePassManager(passManager);
	}
	
	public void Dispose()
	{
		LLVM.DisposeModule(module);
		LLVM.DisposeBuilder(builder);
	}
	
	public static void GenerateAndDump(IEnumerable<Expression> expressions)
	{
		CodeGenerator generator = new();
		generator.Generate(expressions);
		generator.Optimize();
		LLVM.DumpModule(generator.module);
		generator.Dispose();
	}
	
	public static void GenerateAndCompile(IEnumerable<Expression> expressions, string filename = "main")
	{
		CodeGenerator generator = new();
		generator.Generate(expressions);
		generator.Optimize();
		LLVM.DumpModule(generator.module);
		Console.WriteLine();
		
		const string targetTriple = "x86_64-pc-windows-msvc";
		if (LLVM.GetTargetFromTriple(targetTriple, out LLVMTargetRef target, out string error))
			throw new Exception(error);
		LLVMTargetMachineRef targetMachine = LLVM.CreateTargetMachine(target, targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		
		LLVM.SetModuleDataLayout(generator.module, LLVM.CreateTargetDataLayout(targetMachine));
		LLVM.SetTarget(generator.module, targetTriple);
		
		IntPtr asmFilename = Marshal.StringToHGlobalAnsi(filename + ".s");
		if (LLVM.TargetMachineEmitToFile(targetMachine, generator.module, asmFilename, LLVMCodeGenFileType.LLVMAssemblyFile, out error))
			throw new Exception(error);
		Marshal.FreeHGlobal(asmFilename);
		
		CompileSFileToExe(filename + ".s", filename + ".exe");
		RunExe(filename + ".exe");
		
		generator.Dispose();
	}
	
	private static void CompileSFileToExe(string sFilePath, string exeFilePath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "clang",
			Arguments = $"{sFilePath} -o {exeFilePath}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		
		using Process? process = Process.Start(startInfo);
		if (process == null)
			throw new Exception("Failed to start the compilation process");
		
		process.WaitForExit();
		
		if (process.ExitCode != 0)
		{
			string error = process.StandardError.ReadToEnd();
			throw new Exception($"Compilation failed with exit code {process.ExitCode}: {error}");
		}
	}
	
	private static void RunExe(string exeFilePath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = exeFilePath,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		
		using Process? process = Process.Start(startInfo);
		if (process == null)
			throw new Exception("Failed to start the execution process");
		
		process.WaitForExit();
		
		string output = process.StandardOutput.ReadToEnd();
		Console.Write(output);
	}
}