using System.Diagnostics;
using System.Runtime.InteropServices;
using CTes.Antlr;
using LLVMSharp;

namespace CTes;

public class CodeGenerator : CTesBaseVisitor<LLVMValueRef>
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	private LLVMValueRef mainFunction;
	
	private Dictionary<string, LLVMTypeRef> typeIdentifiers = new()
	{
		{ "double", LLVM.DoubleType() }
	};
	
	private Dictionary<string, LLVMValueRef> valueIdentifiers = new();
	
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
	
	public void Generate(CTesParser.ProgramContext program)
	{
		LLVMTypeRef[] paramTypes = [];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.VoidType(), paramTypes, false);
		mainFunction = LLVM.AddFunction(module, "main", functionType);
		
		LLVMBasicBlockRef entry = LLVM.AppendBasicBlock(mainFunction, "entry");
		LLVM.PositionBuilderAtEnd(builder, entry);
		
		GeneratePrintfBinding();
		
		Visit(program);
		
		LLVM.BuildRetVoid(builder);
	}
	
	private void GeneratePrintfBinding()
	{
		LLVMTypeRef[] paramTypes = [LLVM.PointerType(LLVM.Int8Type(), 0)];
		LLVMTypeRef functionType = LLVM.FunctionType(LLVM.Int32Type(), paramTypes, true);
		valueIdentifiers.Add("Print", LLVM.AddFunction(module, "printf", functionType));
	}
	
	public override LLVMValueRef VisitNumber(CTesParser.NumberContext context)
	{
		return LLVM.ConstReal(LLVM.DoubleType(), double.Parse(context.GetText()));
	}
	
	public override LLVMValueRef VisitString(CTesParser.StringContext context)
	{
		string str = string.Concat(context.CHARACTER().Select(ch => ch.GetText()));
		str = System.Text.RegularExpressions.Regex.Unescape(str);
		return LLVM.BuildGlobalStringPtr(builder, str, string.Concat(str.Where(char.IsLetter)) + "String");
	}
	
	public override LLVMValueRef VisitAdd(CTesParser.AddContext context)
	{
		return LLVM.BuildFAdd(builder, Visit(context.lhs), Visit(context.rhs), "addtmp");
	}
	
	public override LLVMValueRef VisitValueIdentifier(CTesParser.ValueIdentifierContext context)
	{
		string name = context.GetText();
		if (!valueIdentifiers.TryGetValue(name, out LLVMValueRef result))
			throw new Exception($"Identifier '{name}' not found");
		return result;
	}
	
	public override LLVMValueRef VisitFunctionCall(CTesParser.FunctionCallContext context)
	{
		LLVMValueRef function = Visit(context.function);
		string functionName = context.function.GetText();
		if (function.IsAFunction().IsNull())
			throw new Exception($"Value '{functionName}' is not a function");
		LLVMValueRef[] args = context.arguments().expression().Select(Visit).ToArray();
		foreach ((LLVMValueRef arg, LLVMTypeRef typ) in args.Zip(LLVM.GetParamTypes(LLVM.TypeOf(function))))
			if (LLVM.TypeOf(arg).TypeKind != typ.TypeKind)
				throw new Exception($"Argument type mismatch in call to '{functionName}'");
		return LLVM.BuildCall(builder, function, args, functionName + "Call");
	}
	
	public override LLVMValueRef VisitFunctionDefinition(CTesParser.FunctionDefinitionContext context)
	{
		string name = context.functionName.Text;
		LLVMValueRef function = LLVM.AddFunction(module, name, LLVM.FunctionType(LLVM.DoubleType(), context.parameters()._types.Select(typ => typeIdentifiers[typ.GetText()]).ToArray(), false));
		LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
		
		for (int i = 0; i < context.parameters()._args.Count; ++i)
		{
			string argumentName = context.parameters()._args[i].Text;
			
			LLVMValueRef param = LLVM.GetParam(function, (uint)i);
			LLVM.SetValueName(param, argumentName);
			
			valueIdentifiers.Add(argumentName, param);
		}
		
		LLVM.PositionBuilderAtEnd(builder, LLVM.AppendBasicBlock(function, "entry"));
		
		try
		{
			foreach (CTesParser.StatementContext? statement in context.statement())
				Visit(statement);
			
			if (context.@return() != null)
				LLVM.BuildRet(builder, Visit(context.@return().expression()));
			else
				LLVM.BuildRetVoid(builder);
		}
		catch (Exception)
		{
			LLVM.DeleteFunction(function);
			throw;
		}
		
		LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		valueIdentifiers.Add(name, function);
		
		LLVM.PositionBuilderAtEnd(builder, mainFunction.GetLastBasicBlock());
		
		return function;
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
	
	public void Dump()
	{
		LLVM.DumpModule(module);
	}
	
	public void CompileAndRun(string filename = "main")
	{
		const string targetTriple = "x86_64-pc-windows-msvc";
		if (LLVM.GetTargetFromTriple(targetTriple, out LLVMTargetRef target, out string error))
			throw new Exception(error);
		LLVMTargetMachineRef targetMachine = LLVM.CreateTargetMachine(target, targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);
		
		LLVM.SetModuleDataLayout(module, LLVM.CreateTargetDataLayout(targetMachine));
		LLVM.SetTarget(module, targetTriple);
		
		IntPtr asmFilename = Marshal.StringToHGlobalAnsi(filename + ".s");
		if (LLVM.TargetMachineEmitToFile(targetMachine, module, asmFilename, LLVMCodeGenFileType.LLVMAssemblyFile, out error))
			throw new Exception(error);
		Marshal.FreeHGlobal(asmFilename);
		
		CompileSFileToExe(filename + ".s", filename + ".exe");
		RunExe(filename + ".exe");
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
		string error = process.StandardError.ReadToEnd();
		Console.Write(error);
	}
}