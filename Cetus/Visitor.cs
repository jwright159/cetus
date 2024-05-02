using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using LLVMSharp;

namespace Cetus;

public class Visitor
{
	private LLVMModuleRef module;
	private LLVMBuilderRef builder;
	
	public static readonly TypedType IntType = new TypedTypeInt();
	public static readonly TypedType BoolType = new TypedTypeBool();
	public static readonly TypedType CharType = new TypedTypeChar();
	public static readonly TypedType FloatType = new TypedTypeFloat();
	public static readonly TypedType StringType = new TypedTypePointer(CharType);
	
	public static readonly TypedValue TrueValue = new TypedValueValue(BoolType, LLVM.ConstInt(LLVM.Int1Type(), 1, false));
	public static readonly TypedValue FalseValue = new TypedValueValue(BoolType, LLVM.ConstInt(LLVM.Int1Type(), 0, false));
	
	private Dictionary<string, TypedValue> autoDerefGlobalIdentifiers = new();
	private Dictionary<string, TypedValue> noDerefGlobalIdentifiers = new()
	{
		{ "void", new TypedValueType(new TypedTypeVoid()) },
		{ "float", new TypedValueType(FloatType) },
		{ "char", new TypedValueType(CharType) },
		{ "int", new TypedValueType(IntType) },
		{ "string", new TypedValueType(StringType) },
		{ "bool", new TypedValueType(BoolType) },
		{ "true", TrueValue },
		{ "false", FalseValue },
	};
	private Dictionary<string, TypedValue> autoDerefLocalIdentifiers = new();
	private Dictionary<string, TypedValue> noDerefLocalIdentifiers = new();
	
	private List<string> referencedLibs = [];
	
	public Visitor()
	{
		LLVM.LinkInMCJIT();
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();
		
		module = LLVM.ModuleCreateWithName("mainModule");
		builder = LLVM.CreateBuilder();
	}
	
	public void Generate(Parser.ProgramContext program)
	{
		VisitProgram(program);
		LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string _);
	}
	
	private void VisitProgram(Parser.ProgramContext context)
	{
		foreach (Parser.IProgramStatementContext statement in context.ProgramStatements)
			if (statement is Parser.FunctionDefinitionContext functionDefinition)
				VisitFunctionDefinition(functionDefinition);
			else if (statement is Parser.ExternFunctionDeclarationContext externFunctionDeclaration)
				VisitExternFunctionDeclaration(externFunctionDeclaration);
			else if (statement is Parser.ExternStructDeclarationContext externStructDeclaration)
				VisitExternStructDeclaration(externStructDeclaration);
			else if (statement is Parser.IncludeLibraryContext includeLibrary)
				VisitIncludeLibrary(includeLibrary);
			else if (statement is Parser.DelegateDeclarationContext delegateDeclaration)
				VisitDelegateDeclaration(delegateDeclaration);
			else
				throw new Exception("Unknown statement type: " + statement.GetType());
	}
	
	// public TypedValue VisitDecimalNumber(Parser.DecimalNumberContext context)
	// {
	// 	return new TypedValueValue(IntType, LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(context.digits.Text), true));
	// }
	//
	// public TypedValue VisitHexNumber(Parser.HexNumberContext context)
	// {
	// 	return new TypedValueValue(IntType, LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(context.digits.Text[2..], NumberStyles.HexNumber), true));
	// }
	//
	// public TypedValue VisitFloatNumber(Parser.FloatNumberContext context)
	// {
	// 	return new TypedValueValue(FloatType, LLVM.ConstReal(LLVM.FloatType(), float.Parse(context.digits.Text[..^1])));
	// }
	//
	// public TypedValue VisitDoubleNumber(Parser.DoubleNumberContext context)
	// {
	// 	return new TypedValueValue(DoubleType, LLVM.ConstReal(LLVM.DoubleType(), double.Parse(context.digits.Text)));
	// }
	//
	// public TypedValue VisitString(Parser.StringContext context)
	// {
	// 	string str = context.STRING().GetText()[1..^1];
	// 	str = System.Text.RegularExpressions.Regex.Unescape(str);
	// 	string name = string.Concat(str.Where(char.IsLetter));
	// 	return new TypedValueValue(StringType, LLVM.BuildGlobalStringPtr(builder, str, (name.Length == 0 ? "some" : name) + "String"));
	// }
	
	public void VisitDelegateDeclaration(Parser.DelegateDeclarationContext context)
	{
		string name = context.FunctionName;
		LLVMTypeRef returnType = VisitTypeIdentifier(context.ReturnType).LLVMType;
		LLVMTypeRef[] paramTypes = context.Parameters.Select(param => param.ParameterType).Select(VisitTypeIdentifier).Select(type => type.LLVMType).ToArray();
		bool isVarArg = context.IsVarArg;
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		TypedValue result = new TypedValueType(new TypedTypeWrap(functionType));
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	// public TypedValue VisitAddition(Parser.AdditionContext context)
	// {
	// 	TypedValue lhs = Visit(context.lhs);
	// 	TypedValue rhs = Visit(context.rhs);
	// 	return lhs.Type.BuildAdd(builder, lhs, rhs);
	// }
	//
	// public TypedValue VisitValueIdentifier(Parser.ValueIdentifierContext context)
	// {
	// 	string name = context.GetText();
	// 	
	// 	if (noDerefLocalIdentifiers.TryGetValue(name, out TypedValue? result))
	// 		return result;
	// 	
	// 	if (autoDerefLocalIdentifiers.TryGetValue(name, out result))
	// 		return new TypedValueValue(((TypedTypePointer)result.Type).PointerType!, LLVM.BuildLoad(builder, result.Value, "loadvartmp"));
	// 	
	// 	if (noDerefGlobalIdentifiers.TryGetValue(name, out result))
	// 		return result;
	// 	
	// 	if (autoDerefGlobalIdentifiers.TryGetValue(name, out result))
	// 		return new TypedValueValue(((TypedTypePointer)result.Type).PointerType!, LLVM.BuildLoad(builder, result.Value, "loadvartmp"));
	// 	
	// 	throw new Exception($"Identifier '{name}' not found");
	// }
	
	private TypedType VisitTypeIdentifier(Parser.TypeIdentifierContext context)
	{
		string name = context.TypeName;
		if (!noDerefGlobalIdentifiers.TryGetValue(name, out TypedValue? result))
			throw new Exception($"Type '{name}' not found");
		return result.Type; //(TypedValueType)context.pointerDepth.Aggregate(result.Type, (type, _) => LLVM.PointerType(type, 0));
	}
	
	// public TypedValue VisitFunctionCall(Parser.FunctionCallContext context)
	// {
	// 	TypedValue function = Visit(context.function);
	// 	string functionName = context.function.GetText();
	// 	LLVMTypeRef functionType = function.Type.TypeKind == LLVMTypeKind.LLVMPointerTypeKind ? function.Type.GetElementType() : function.Type;
	// 	
	// 	if (functionType.TypeKind != LLVMTypeKind.LLVMFunctionTypeKind)
	// 		throw new Exception($"Value '{functionName}' is a {functionType.TypeKind}, not a function");
	// 	
	// 	TypedValue[] args = context.arguments().expression().Select(Visit).ToArray();
	// 	
	// 	bool isVarArg = functionType.IsFunctionVarArg;
	// 	if (isVarArg ? args.Length < functionType.CountParamTypes() : args.Length != functionType.CountParamTypes())
	// 		throw new Exception($"Argument count mismatch in call to '{functionName}', expected {(isVarArg ? "at least " : "")}{functionType.CountParamTypes()} but got {args.Length}");
	// 	
	// 	foreach ((TypedValue arg, LLVMTypeRef type) in args.Zip(functionType.GetParamTypes()))
	// 		if (arg.Type.TypeKind != type.TypeKind)
	// 			throw new Exception($"Argument type mismatch in call to '{functionName}', expected {type.TypeKind} but got {arg.Type.TypeKind}");
	// 	
	// 	LLVM.BuildCall(builder, function.Value, args.Select(arg => arg.Value).ToArray(), functionType.GetReturnType().TypeKind == LLVMTypeKind.LLVMVoidTypeKind ? "" : functionName + "Call");
	// 	return default!;
	// }
	//
	// public TypedValue VisitIfStatement(Parser.IfStatementContext context)
	// {
	// 	LLVMBasicBlockRef functionBlock = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));
	// 	LLVMBasicBlockRef thenBlock = LLVM.AppendBasicBlock(functionBlock, "ifThen");
	// 	LLVMBasicBlockRef elseBlock = LLVM.AppendBasicBlock(functionBlock, "ifElse");
	// 	LLVMBasicBlockRef mergeBlock = LLVM.AppendBasicBlock(functionBlock, "ifMerge");
	// 	
	// 	LLVMValueRef condition = Visit(context.condition).Value;
	// 	LLVM.BuildCondBr(builder, condition, thenBlock, elseBlock);
	// 	
	// 	LLVM.PositionBuilderAtEnd(builder, thenBlock);
	// 	Visit(context.thenStatements);
	// 	LLVM.BuildBr(builder, mergeBlock);
	// 	
	// 	LLVM.PositionBuilderAtEnd(builder, elseBlock);
	// 	if (context.elseStatements != null)
	// 		Visit(context.elseStatements);
	// 	LLVM.BuildBr(builder, mergeBlock);
	// 	
	// 	LLVM.PositionBuilderAtEnd(builder, mergeBlock);
	// 	return default!;
	// }
	//
	// public TypedValue VisitWhileStatement(Parser.WhileStatementContext context)
	// {
	// 	LLVMBasicBlockRef functionBlock = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));
	// 	LLVMBasicBlockRef conditionBlock = LLVM.AppendBasicBlock(functionBlock, "whileCondition");
	// 	LLVMBasicBlockRef bodyBlock = LLVM.AppendBasicBlock(functionBlock, "whileBody");
	// 	LLVMBasicBlockRef mergeBlock = LLVM.AppendBasicBlock(functionBlock, "whileMerge");
	// 	
	// 	LLVM.BuildBr(builder, conditionBlock);
	// 	
	// 	LLVM.PositionBuilderAtEnd(builder, conditionBlock);
	// 	LLVMValueRef condition = Visit(context.condition).Value;
	// 	LLVM.BuildCondBr(builder, condition, bodyBlock, mergeBlock);
	// 	
	// 	LLVM.PositionBuilderAtEnd(builder, bodyBlock);
	// 	Visit(context.thenStatements);
	// 	LLVM.BuildBr(builder, conditionBlock);
	// 	
	// 	LLVM.PositionBuilderAtEnd(builder, mergeBlock);
	// 	return default!;
	// }
	
	private void VisitFunctionDefinition(Parser.FunctionDefinitionContext context)
	{
		string name = context.FunctionName;
		LLVMTypeRef returnType = VisitTypeIdentifier(context.ReturnType).LLVMType;
		LLVMTypeRef[] paramTypes = context.Parameters.Select(param => param.ParameterType).Select(VisitTypeIdentifier).Select(type => type.LLVMType).ToArray();
		bool isVarArg = context.IsVarArg;
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		LLVMValueRef function = LLVM.AddFunction(module, name, functionType);
		LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
		
		noDerefLocalIdentifiers.Clear();
		autoDerefLocalIdentifiers.Clear();
		
		for (int i = 0; i < context.Parameters.Count; ++i)
		{
			string parameterName = context.Parameters[i].ParameterName;
			TypedType parameterType = VisitTypeIdentifier(context.Parameters[i].ParameterType);
			LLVMValueRef param = function.GetParam((uint)i);
			LLVM.SetValueName(param, parameterName);
			noDerefLocalIdentifiers.Add(parameterName, new TypedValueValue(parameterType, param));
		}
		
		LLVM.PositionBuilderAtEnd(builder, function.AppendBasicBlock("entry"));
		
		try
		{
			// foreach (Parser.StatementContext? statement in context.statement())
			// 	Visit(statement);
			LLVM.BuildRetVoid(builder);
		}
		catch (Exception)
		{
			LLVM.DeleteFunction(function);
			throw;
		}
		
		LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		TypedValue result = new TypedValueValue(new TypedTypeWrap(functionType), function);
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	// public TypedValue VisitReturnStatement(Parser.ReturnStatementContext context)
	// {
	// 	if (context.expression() != null)
	// 		LLVM.BuildRet(builder, Visit(context.expression()).Value);
	// 	else
	// 		LLVM.BuildRetVoid(builder);
	// 	return default!;
	// }
	
	private void VisitIncludeLibrary(Parser.IncludeLibraryContext context)
	{
		referencedLibs.Add(context.LibraryName);
	}
	
	private void VisitExternFunctionDeclaration(Parser.ExternFunctionDeclarationContext context)
	{
		string name = context.FunctionName;
		LLVMTypeRef returnType = VisitTypeIdentifier(context.ReturnType).LLVMType;
		LLVMTypeRef[] paramTypes = context.Parameters.Select(param => param.ParameterType).Select(VisitTypeIdentifier).Select(type => type.LLVMType).ToArray();
		bool isVarArg = context.IsVarArg;
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		LLVMValueRef function = LLVM.AddFunction(module, name, functionType);
		TypedValue result = new TypedValueValue(new TypedTypeWrap(functionType), function);
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	public void VisitExternStructDeclaration(Parser.ExternStructDeclarationContext context)
	{
		string name = context.StructName;
		LLVMTypeRef @struct = LLVM.StructCreateNamed(LLVM.GetGlobalContext(), name);
		TypedValue result = new TypedValueType(new TypedTypeWrap(@struct));
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	// public TypedValue VisitExternVariableDeclaration(Parser.ExternVariableDeclarationContext context)
	// {
	// 	string name = context.name.Text;
	// 	LLVMTypeRef type = Visit(context.type).Type;
	// 	LLVMValueRef global = LLVM.AddGlobal(module, type, name);
	// 	global.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
	// 	TypedValue result = new TypedValueValue(type, global);
	// 	autoDerefGlobalIdentifiers.Add(name, result);
	// 	return default!;
	// }
	//
	// public TypedValue VisitDereference(Parser.DereferenceContext context)
	// {
	// 	TypedValue pointer = Visit(context.operators3());
	// 	if (pointer.Type is not TypedTypePointer)
	// 		throw new Exception("Cannot dereference a non-pointer type");
	// 	return (TypedValueValue)LLVM.BuildLoad(builder, pointer.Value, "loadtmp");
	// }
	//
	// public TypedValue VisitConstVariableDeclaration(Parser.ConstVariableDeclarationContext context)
	// {
	// 	string name = context.name.Text;
	// 	TypedValue value = Visit(context.value());
	// 	LLVMValueRef global = LLVM.AddGlobal(module, value.Type, name);
	// 	global.SetLinkage(LLVMLinkage.LLVMInternalLinkage);
	// 	global.SetInitializer(value.Value);
	// 	TypedValue result = new TypedValueValue(value.Type, global);
	// 	autoDerefGlobalIdentifiers.Add(name, result);
	// 	return default!;
	// }
	//
	// public TypedValue VisitAssignmentStatement(Parser.AssignmentStatementContext context)
	// {
	// 	TypedValue type = Visit(context.type);
	// 	string name = context.name.Text;
	// 	TypedValue value = Visit(context.val);
	// 	if (type.Type != value.Type)
	// 		throw new Exception($"Type mismatch in assignment to '{name}', expected {type.Type} but got {value.Type}");
	// 	LLVMValueRef variable = LLVM.BuildAlloca(builder, type.Type, name);
	// 	LLVM.BuildStore(builder, value.Value, variable);
	// 	TypedValue result = new TypedValueValue(type.Type, variable);
	// 	autoDerefLocalIdentifiers.Add(name, result);
	// 	return result;
	// }
	//
	// public TypedValue VisitNegation(Parser.NegationContext context)
	// {
	// 	return (TypedValueValue)LLVM.BuildNot(builder, Visit(context.operators3()).Value, "negtmp");
	// }
	//
	// public TypedValue VisitEquivalence(Parser.EquivalenceContext context)
	// {
	// 	TypedValue lhs = Visit(context.lhs);
	// 	TypedValue rhs = Visit(context.rhs);
	// 	return new TypedValueValue(BoolType, lhs.Type.BuildEqual(builder, lhs, rhs));
	// }
	//
	// public TypedValue VisitInequivalence(Parser.InequivalenceContext context)
	// {
	// 	TypedValue lhs = Visit(context.lhs);
	// 	TypedValue rhs = Visit(context.rhs);
	// 	return new TypedValueValue(BoolType, lhs.Type.BuildInqual(builder, lhs, rhs));
	// }
	
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
	
	private void CompileSFileToExe(string sFilePath, string exeFilePath)
	{
		ProcessStartInfo startInfo = new()
		{
			FileName = "clang",
			Arguments = $"{sFilePath} -o {exeFilePath} -L lib {string.Join(" ", referencedLibs.Select(lib => "-l" + lib))}",
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
		startInfo.EnvironmentVariables["PATH"] += ";lib";
		
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

public interface TypedValue
{
	public TypedType Type { get; }
	public LLVMValueRef Value { get; }
}

public readonly struct TypedValueValue(TypedType type, LLVMValueRef value) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => value;
	public override string ToString() => value.ToString();
}

public readonly struct TypedValueType(TypedType type) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a type");
	public override string ToString() => type.ToString()!;
}

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
	// public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs);
	// public TypedValue BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs);
	// public TypedValue BuildAdd(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs);
}

public static class TypedTypeExtensions
{
	public static void CheckForTypes<T>(this T type, TypedValue lhs, TypedValue rhs) where T : TypedType
	{
		if (lhs.Type is not T) throw new Exception($"Lhs is a {lhs.Type}, not a {type}");
		if (rhs.Type is not T) throw new Exception($"Rhs is a {lhs.Type}, not a {type}");
	}
}

public readonly struct TypedTypeWrap(LLVMTypeRef type) : TypedType
{
	public LLVMTypeRef LLVMType => type;
}

public readonly struct TypedTypePointer(TypedType pointerType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.PointerType(pointerType.LLVMType, 0);
	public TypedType PointerType => pointerType;
	// public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypePointer lhsPointer || lhsPointer.PointerType != pointerType ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypePointer rhsPointer || rhsPointer.PointerType != pointerType ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp");
	// public TypedValue BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypePointer lhsPointer || lhsPointer.PointerType != pointerType ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypePointer rhsPointer || rhsPointer.PointerType != pointerType ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp");
	public override string ToString() => pointerType + "*";
}

public readonly struct TypedTypeInt : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.Int32Type();
	// public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	// {
	// 	this.CheckForTypes(lhs, rhs);
	// 	return new TypedValueValue(new TypedTypeBool(), LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp"));
	// }
	// public TypedType BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeInt ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeInt ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp");
	//
	// public LLVMValueRef BuildAdd(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	// {
	// 	LLVM.BuildAdd(builder, Visit(context.lhs).Value, Visit(context.rhs).Value, "addtmp")
	// }
	
	public override string ToString() => "int";
}

public readonly struct TypedTypeBool : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.Int1Type();
	// public LLVMValueRef BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeBool ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeBool ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp");
	// public LLVMValueRef BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeBool ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeBool ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp");
	public override string ToString() => "bool";
}

public readonly struct TypedTypeChar : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.Int8Type();
	// public LLVMValueRef BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeChar ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeChar ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp");
	// public LLVMValueRef BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeChar ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeChar ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp");
	public override string ToString() => "char";
}

public readonly struct TypedTypeVoid : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.VoidType();
	// public LLVMValueRef BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) => throw new Exception("Cannot compare void types");
	// public LLVMValueRef BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) => throw new Exception("Cannot compare void types");
	public override string ToString() => "void";
}

public readonly struct TypedTypeFloat : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.FloatType();
	// public LLVMValueRef BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeFloat ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeFloat ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealOEQ, lhs.Value, rhs.Value, "eqtmp");
	// public LLVMValueRef BuildInqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) =>
	// 	lhs.Type is not TypedTypeFloat ? throw new Exception($"Lhs is a {lhs.Type}, not a {ToString()}")
	// 	: rhs.Type is not TypedTypeFloat ? throw new Exception($"Rhs is a {rhs.Type}, not a {ToString()}")
	// 	: LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealONE, lhs.Value, rhs.Value, "neqtmp");
	public override string ToString() => "float";
}