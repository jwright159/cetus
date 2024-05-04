﻿using System.Diagnostics;
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
	public static readonly TypedType DoubleType = new TypedTypeDouble();
	public static readonly TypedType StringType = new TypedTypePointer(CharType);
	
	public static readonly TypedValue TrueValue = new TypedValueValue(BoolType, LLVM.ConstInt(LLVM.Int1Type(), 1, false));
	public static readonly TypedValue FalseValue = new TypedValueValue(BoolType, LLVM.ConstInt(LLVM.Int1Type(), 0, false));
	
	private Dictionary<string, TypedValue> autoDerefGlobalIdentifiers = new();
	private Dictionary<string, TypedValue> noDerefGlobalIdentifiers = new()
	{
		{ "void", new TypedValueType(new TypedTypeVoid()) },
		{ "float", new TypedValueType(FloatType) },
		{ "double", new TypedValueType(DoubleType) },
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
		LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMReturnStatusAction, out string output);
		Console.WriteLine(output);
	}
	
	private void VisitProgram(Parser.ProgramContext context)
	{
		foreach (Parser.IProgramStatementContext statement in context.ProgramStatements)
			VisitProgramStatement(statement);
	}
	
	private void VisitProgramStatement(Parser.IProgramStatementContext context)
	{
		if (context is Parser.FunctionDefinitionContext functionDefinition)
			VisitFunctionDefinition(functionDefinition);
		else if (context is Parser.ExternFunctionDeclarationContext externFunctionDeclaration)
			VisitExternFunctionDeclaration(externFunctionDeclaration);
		else if (context is Parser.ExternStructDeclarationContext externStructDeclaration)
			VisitExternStructDeclaration(externStructDeclaration);
		else if (context is Parser.IncludeLibraryContext includeLibrary)
			VisitIncludeLibrary(includeLibrary);
		else if (context is Parser.DelegateDeclarationContext delegateDeclaration)
			VisitDelegateDeclaration(delegateDeclaration);
		else if (context is Parser.ConstVariableDefinitionContext constVariableDefinition)
			VisitConstVariableDefinition(constVariableDefinition);
		else
			throw new Exception("Unknown program statement type: " + context.GetType());
	}
	
	private TypedValue VisitValue(Parser.IValueContext context, TypedType? typeHint = null)
	{
		if (context is Parser.IntegerContext integer)
			return VisitInteger(integer);
		if (context is Parser.FloatContext @float)
			return VisitFloat(@float);
		if (context is Parser.DoubleContext @double)
			return VisitDouble(@double);
		if (context is Parser.StringContext @string)
			return VisitString(@string);
		if (context is Parser.NullContext)
			return VisitNull(typeHint);
		if (context is Parser.ValueIdentifierContext valueIdentifier)
			return VisitValueIdentifier(valueIdentifier);
		throw new Exception("Unknown value type: " + context.GetType());
	}
	
	private TypedValue VisitInteger(Parser.IntegerContext context)
	{
		return new TypedValueValue(IntType, LLVM.ConstInt(LLVM.Int32Type(), (ulong)context.Value, true));
	}
	
	private TypedValue VisitFloat(Parser.FloatContext context)
	{
		return new TypedValueValue(FloatType, LLVM.ConstReal(LLVM.FloatType(), context.Value));
	}
	
	private TypedValue VisitDouble(Parser.DoubleContext context)
	{
		return new TypedValueValue(DoubleType, LLVM.ConstReal(LLVM.DoubleType(), context.Value));
	}
	
	private TypedValue VisitString(Parser.StringContext context)
	{
		string name = context.Value;
		return new TypedValueValue(StringType, LLVM.BuildGlobalStringPtr(builder, context.Value, name.Length == 0 ? "emptyString" : name));
	}
	
	private TypedValue VisitNull(TypedType? typeHint)
	{
		if (typeHint == null)
			throw new Exception("Cannot infer type of null");
		return new TypedValueValue(typeHint, LLVM.ConstNull(typeHint.LLVMType));
	}
	
	private TypedValue VisitValueIdentifier(Parser.ValueIdentifierContext context)
	{
		string name = context.ValueName;
		
		if (noDerefLocalIdentifiers.TryGetValue(name, out TypedValue? result))
			return result;
		
		if (autoDerefLocalIdentifiers.TryGetValue(name, out result))
			return new TypedValueValue(((TypedTypePointer)result.Type).PointerType, LLVM.BuildLoad(builder, result.Value, "loadvartmp"));
		
		if (noDerefGlobalIdentifiers.TryGetValue(name, out result))
			return result;
		
		if (autoDerefGlobalIdentifiers.TryGetValue(name, out result))
			return new TypedValueValue(((TypedTypePointer)result.Type).PointerType, LLVM.BuildLoad(builder, result.Value, "loadvartmp"));
		
		throw new Exception($"Identifier '{name}' not found");
	}
	
	private void VisitDelegateDeclaration(Parser.DelegateDeclarationContext context)
	{
		string name = context.FunctionName;
		LLVMTypeRef returnType = VisitTypeIdentifier(context.ReturnType).LLVMType;
		LLVMTypeRef[] paramTypes = context.Parameters.Select(param => param.ParameterType).Select(VisitTypeIdentifier).Select(type => type.LLVMType).ToArray();
		bool isVarArg = context.IsVarArg;
		LLVMTypeRef functionType = LLVM.FunctionType(returnType, paramTypes, isVarArg);
		TypedValue result = new TypedValueType(new TypedTypeFunction(functionType, name));
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	private TypedType VisitTypeIdentifier(Parser.TypeIdentifierContext context)
	{
		string name = context.TypeName;
		if (!noDerefGlobalIdentifiers.TryGetValue(name, out TypedValue? result))
			throw new Exception($"Type '{name}' not found");
		LLVMTypeRef type = result.Type.LLVMType;
		TypedType wrappedType = type.Wrap();
		for (int i = 0; i < context.PointerCount; ++i)
			wrappedType = new TypedTypePointer(wrappedType);
		return wrappedType;
	}
	
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
		
		VisitFunctionBlock(context.Statements);
		LLVM.BuildRetVoid(builder);
		
		LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
		
		TypedValue result = new TypedValueValue(new TypedTypeFunction(functionType, name), function);
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	private void VisitFunctionBlock(IEnumerable<Parser.IFunctionStatementContext> contexts)
	{
		foreach (Parser.IFunctionStatementContext? statement in contexts)
			VisitFunctionStatement(statement);
	}
	
	private void VisitFunctionStatement(Parser.IFunctionStatementContext context)
	{
		if (context is Parser.ReturnContext @return)
			VisitReturn(@return);
		else if (context is Parser.FunctionCallContext functionCall)
			VisitFunctionCall(functionCall);
		else if (context is Parser.AssignmentContext assignment)
			VisitAssignment(assignment);
		else if (context is Parser.IfContext @if)
			VisitIf(@if);
		else if (context is Parser.WhileContext @while)
			VisitWhile(@while);
		else
			throw new Exception("Unknown function statement type: " + context.GetType());
	}
	
	private void VisitReturn(Parser.ReturnContext context)
	{
		if (context.Value != null)
			LLVM.BuildRet(builder, VisitExpression(context.Value).Value);
		else
			LLVM.BuildRetVoid(builder);
	}
	
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
		TypedValue result = new TypedValueValue(new TypedTypeFunction(functionType, name), function);
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	private void VisitExternStructDeclaration(Parser.ExternStructDeclarationContext context)
	{
		string name = context.StructName;
		LLVMTypeRef @struct = LLVM.StructCreateNamed(LLVM.GetGlobalContext(), name);
		TypedValue result = new TypedValueType(new TypedTypeStruct(@struct));
		noDerefGlobalIdentifiers.Add(name, result);
	}
	
	private void VisitConstVariableDefinition(Parser.ConstVariableDefinitionContext context)
	{
		string name = context.VariableName;
		TypedType type = VisitTypeIdentifier(context.Type);
		TypedValue value = VisitValue(context.Value);
		LLVMValueRef global = LLVM.AddGlobal(module, type.LLVMType, name);
		global.SetLinkage(LLVMLinkage.LLVMInternalLinkage);
		global.SetInitializer(value.Value);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), global);
		autoDerefGlobalIdentifiers.Add(name, result);
	}
	
	private TypedValue VisitAssignment(Parser.AssignmentContext context)
	{
		TypedType type = VisitTypeIdentifier(context.Type);
		string name = context.VariableName;
		TypedValue value = VisitExpression(context.Value);
		if (!TypedTypeExtensions.TypesEqual(type, value.Type))
			throw new Exception($"Type mismatch in assignment to '{name}', expected {type.LLVMType} but got {value.Type.LLVMType}");
		LLVMValueRef variable = LLVM.BuildAlloca(builder, type.LLVMType, name);
		LLVM.BuildStore(builder, value.Value, variable);
		TypedValue result = new TypedValueValue(new TypedTypePointer(type), variable);
		autoDerefLocalIdentifiers.Add(name, result);
		return result;
	}
	
	private void VisitIf(Parser.IfContext context)
	{
		LLVMBasicBlockRef functionBlock = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));
		LLVMBasicBlockRef thenBlock = LLVM.AppendBasicBlock(functionBlock, "ifThen");
		LLVMBasicBlockRef elseBlock = LLVM.AppendBasicBlock(functionBlock, "ifElse");
		LLVMBasicBlockRef mergeBlock = LLVM.AppendBasicBlock(functionBlock, "ifMerge");
		
		LLVMValueRef condition = VisitExpression(context.Condition).Value;
		LLVM.BuildCondBr(builder, condition, thenBlock, elseBlock);
		
		LLVM.PositionBuilderAtEnd(builder, thenBlock);
		VisitFunctionBlock(context.ThenStatements);
		LLVM.BuildBr(builder, mergeBlock);
		
		LLVM.PositionBuilderAtEnd(builder, elseBlock);
		if (context.ElseStatements.Count > 0)
			VisitFunctionBlock(context.ElseStatements);
		LLVM.BuildBr(builder, mergeBlock);
		
		LLVM.PositionBuilderAtEnd(builder, mergeBlock);
	}
	
	private void VisitWhile(Parser.WhileContext context)
	{
		LLVMBasicBlockRef functionBlock = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));
		LLVMBasicBlockRef conditionBlock = LLVM.AppendBasicBlock(functionBlock, "whileCondition");
		LLVMBasicBlockRef bodyBlock = LLVM.AppendBasicBlock(functionBlock, "whileBody");
		LLVMBasicBlockRef mergeBlock = LLVM.AppendBasicBlock(functionBlock, "whileMerge");
		
		LLVM.BuildBr(builder, conditionBlock);
		
		LLVM.PositionBuilderAtEnd(builder, conditionBlock);
		LLVMValueRef condition = VisitExpression(context.Condition).Value;
		LLVM.BuildCondBr(builder, condition, bodyBlock, mergeBlock);
		
		LLVM.PositionBuilderAtEnd(builder, bodyBlock);
		VisitFunctionBlock(context.ThenStatements);
		LLVM.BuildBr(builder, conditionBlock);
		
		LLVM.PositionBuilderAtEnd(builder, mergeBlock);
	}
	
	private TypedValue VisitExpression(Parser.IExpressionContext context, TypedType? typeHint = null)
	{
		if (context is Parser.EquivalenceContext equivalence)
			return VisitEquivalence(equivalence);
		if (context is Parser.InequivalenceContext inequivalence)
			return VisitInequivalence(inequivalence);
		if (context is Parser.AdditionContext addition)
			return VisitAddition(addition);
		if (context is Parser.NotContext not)
			return VisitNot(not);
		if (context is Parser.DereferenceContext dereference)
			return VisitDereference(dereference);
		if (context is Parser.FunctionCallContext functionCall)
			return VisitFunctionCall(functionCall);
		if (context is Parser.IValueContext value)
			return VisitValue(value, typeHint);
		throw new Exception("Unknown expression type: " + context.GetType());
	}
	
	private TypedValue VisitEquivalence(Parser.EquivalenceContext context)
	{
		TypedValue lhs = VisitExpression(context.Lhs);
		TypedValue rhs = VisitExpression(context.Rhs, lhs.Type);
		return lhs.Type.BuildEqual(builder, lhs, rhs);
	}
	
	private TypedValue VisitInequivalence(Parser.InequivalenceContext context)
	{
		TypedValue lhs = VisitExpression(context.Lhs);
		TypedValue rhs = VisitExpression(context.Rhs, lhs.Type);
		return lhs.Type.BuildInequal(builder, lhs, rhs);
	}
	
	private TypedValue VisitAddition(Parser.AdditionContext context)
	{
		TypedValue lhs = VisitExpression(context.Lhs);
		TypedValue rhs = VisitExpression(context.Rhs, lhs.Type);
		LLVMValueRef result = LLVM.BuildAdd(builder, lhs.Value, rhs.Value, "addtmp");
		return new TypedValueValue(lhs.Type, result);
	}
	
	private TypedValue VisitNot(Parser.NotContext context)
	{
		TypedValue value = VisitExpression(context.Value);
		LLVMValueRef result = LLVM.BuildNot(builder, value.Value, "nottmp");
		return new TypedValueValue(value.Type, result);
	}
	
	private TypedValue VisitDereference(Parser.DereferenceContext context)
	{
		TypedValue pointer = VisitExpression(context.Value);
		if (pointer.Type is not TypedTypePointer typePointer)
			throw new Exception("Cannot dereference a non-pointer type");
		LLVMValueRef result = LLVM.BuildLoad(builder, pointer.Value, "loadtmp");
		return new TypedValueValue(typePointer.PointerType, result);
	}
	
	// private void VisitExternVariableDeclaration(Parser.ExternVariableDeclarationContext context)
	// {
	// 	string name = context.name.Text;
	// 	LLVMTypeRef type = Visit(context.type).Type;
	// 	LLVMValueRef global = LLVM.AddGlobal(module, type, name);
	// 	global.SetLinkage(LLVMLinkage.LLVMExternalLinkage);
	// 	TypedValue result = new TypedValueValue(type, global);
	// 	autoDerefGlobalIdentifiers.Add(name, result);
	// }
	
	private TypedValue VisitFunctionCall(Parser.FunctionCallContext context)
	{
		TypedValue function = VisitExpression(context.Function);
		TypedTypeFunction functionType = function.Type is TypedTypePointer functionPointerType ? (TypedTypeFunction)functionPointerType.PointerType : (TypedTypeFunction)function.Type;
		string functionName = functionType.FunctionName;
		LLVMTypeRef functionLLVMType = functionType.LLVMType;
		
		if (functionLLVMType.TypeKind != LLVMTypeKind.LLVMFunctionTypeKind)
			throw new Exception($"Value '{functionName}' is a {functionLLVMType.TypeKind}, not a function");
		
		bool isVarArg = functionLLVMType.IsFunctionVarArg;
		if (isVarArg ? context.Arguments.Count < functionLLVMType.CountParamTypes() : context.Arguments.Count != functionLLVMType.CountParamTypes())
			throw new Exception($"Argument count mismatch in call to '{functionName}', expected {(isVarArg ? "at least " : "")}{functionLLVMType.CountParamTypes()} but got {context.Arguments.Count}");
		
		IEnumerable<LLVMTypeRef?> paramTypeHints = functionLLVMType.GetParamTypes().Select(param => (LLVMTypeRef?)param);
		if (context.Arguments.Count > functionLLVMType.CountParamTypes())
			paramTypeHints = paramTypeHints.Concat(Enumerable.Range(0, context.Arguments.Count - (int)functionLLVMType.CountParamTypes()).Select(_ => (LLVMTypeRef?)null));
		TypedValue[] args = context.Arguments
			.Zip(paramTypeHints, (arg, param) => VisitExpression(arg, param?.Wrap()))
			.ToArray();
		
		foreach ((TypedValue arg, LLVMTypeRef type) in args.Zip(functionLLVMType.GetParamTypes()))
			if (!TypedTypeExtensions.TypesEqual(arg.Type.LLVMType, type))
				throw new Exception($"Argument type mismatch in call to '{functionName}', expected {type} but got {arg.Type.LLVMType}");
		
		LLVMValueRef result = LLVM.BuildCall(builder, function.Value, args.Select(arg => arg.Value).ToArray(), functionLLVMType.GetReturnType().TypeKind == LLVMTypeKind.LLVMVoidTypeKind ? "" : functionName + "Call");
		return new TypedValueValue(functionLLVMType.GetReturnType().Wrap(), result);
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
	
	public void Compile(string filename = "main")
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
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs);
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs);
}

public static class TypedTypeExtensions
{
	public static void CheckForTypes<T>(this T type, TypedValue lhs, TypedValue rhs) where T : TypedType
	{
		if (lhs.Type is not T) throw new Exception($"Lhs is a {lhs.Type}, not a {type}");
		if (rhs.Type is not T) throw new Exception($"Rhs is a {rhs.Type}, not a {type}");
	}
	
	public static bool TypesEqual(TypedValue lhs, TypedValue rhs) => TypesEqual(lhs.Type.LLVMType, rhs.Type.LLVMType);
	public static bool TypesEqual(TypedType lhs, TypedType rhs) => TypesEqual(lhs.LLVMType, rhs.LLVMType);
	public static bool TypesEqual(LLVMTypeRef lhs, LLVMTypeRef rhs)
	{
		while (lhs.TypeKind == LLVMTypeKind.LLVMPointerTypeKind && rhs.TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
		{
			lhs = lhs.GetElementType();
			rhs = rhs.GetElementType();
		}
		
		if (lhs.TypeKind != rhs.TypeKind)
			return false;
		
		if (lhs.TypeKind == LLVMTypeKind.LLVMIntegerTypeKind)
			return lhs.GetIntTypeWidth() == rhs.GetIntTypeWidth();
		
		return true;
	}
	
	public static TypedType Wrap(this LLVMTypeRef type)
	{
		if (type.TypeKind == LLVMTypeKind.LLVMIntegerTypeKind)
			if (type.GetIntTypeWidth() == 1)
				return new TypedTypeBool();
			else if (type.GetIntTypeWidth() == 8)
				return new TypedTypeChar();
			else if (type.GetIntTypeWidth() == 32)
				return new TypedTypeInt();
			else
				throw new Exception($"Unknown integer type width {type.GetIntTypeWidth()}");
		if (type.TypeKind == LLVMTypeKind.LLVMFloatTypeKind)
			return new TypedTypeFloat();
		if (type.TypeKind == LLVMTypeKind.LLVMDoubleTypeKind)
			return new TypedTypeDouble();
		if (type.TypeKind == LLVMTypeKind.LLVMPointerTypeKind)
			return new TypedTypePointer(type.GetElementType().Wrap());
		if (type.TypeKind == LLVMTypeKind.LLVMVoidTypeKind)
			return new TypedTypeVoid();
		if (type.TypeKind == LLVMTypeKind.LLVMFunctionTypeKind)
			return new TypedTypeFunction(type, type.ToString());
		if (type.TypeKind == LLVMTypeKind.LLVMStructTypeKind)
			return new TypedTypeStruct(type);
		throw new Exception($"Unknown type to wrap {type}");
	}
}

public readonly struct TypedTypePointer(TypedType pointerType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.PointerType(pointerType.LLVMType, 0);
	public TypedType PointerType => pointerType;
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp"));
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp"));
	}
	
	public override string ToString() => pointerType + "*";
	
	public void CheckForTypes(TypedValue lhs, TypedValue rhs)
	{
		if (lhs.Type is not TypedTypePointer lhsPointer) throw new Exception($"Lhs is a {lhs.Type}, not a {typeof(TypedTypePointer)}");
		if (!TypedTypeExtensions.TypesEqual(lhsPointer.PointerType, PointerType)) throw new Exception($"Lhs is a {lhsPointer.PointerType} pointer, not a {pointerType} pointer");
		if (rhs.Type is not TypedTypePointer rhsPointer) throw new Exception($"Rhs is a {rhs.Type}, not a {typeof(TypedTypePointer)}");
		if (!TypedTypeExtensions.TypesEqual(rhsPointer.PointerType, PointerType)) throw new Exception($"Rhs is a {rhsPointer.PointerType} pointer, not a {pointerType} pointer");
	}
}

public readonly struct TypedTypeInt : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.Int32Type();
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp"));
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp"));
	}
	
	public override string ToString() => "int";
}

public readonly struct TypedTypeBool : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.Int1Type();
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp"));
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp"));
	}
	
	public override string ToString() => "bool";
}

public readonly struct TypedTypeChar : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.Int8Type();
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntEQ, lhs.Value, rhs.Value, "eqtmp"));
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildICmp(builder, LLVMIntPredicate.LLVMIntNE, lhs.Value, rhs.Value, "neqtmp"));
	}
	
	public override string ToString() => "char";
}

public readonly struct TypedTypeVoid : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.VoidType();
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return Visitor.TrueValue;
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return Visitor.TrueValue;
	}
	
	public override string ToString() => "void";
}

public readonly struct TypedTypeFloat : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.FloatType();
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealOEQ, lhs.Value, rhs.Value, "eqtmp"));
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealONE, lhs.Value, rhs.Value, "neqtmp"));
	}
	
	public override string ToString() => "float";
}

public readonly struct TypedTypeDouble : TypedType
{
	public LLVMTypeRef LLVMType => LLVM.DoubleType();
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealOEQ, lhs.Value, rhs.Value, "eqtmp"));
	}
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs)
	{
		this.CheckForTypes(lhs, rhs);
		return new TypedValueValue(Visitor.BoolType, LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealONE, lhs.Value, rhs.Value, "neqtmp"));
	}
	
	public override string ToString() => "double";
}

public readonly struct TypedTypeFunction(LLVMTypeRef type, string name) : TypedType
{
	public LLVMTypeRef LLVMType => type;
	public string FunctionName => name;
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) => throw new Exception($"Cannot compare function type {LLVMType}");
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) => throw new Exception($"Cannot compare function type {LLVMType}");
	
	public override string ToString() => LLVMType.ToString();
}

public readonly struct TypedTypeStruct(LLVMTypeRef type) : TypedType
{
	public LLVMTypeRef LLVMType => type;
	
	public TypedValue BuildEqual(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) => throw new Exception($"Cannot compare struct type {LLVMType}");
	public TypedValue BuildInequal(LLVMBuilderRef builder, TypedValue lhs, TypedValue rhs) => throw new Exception($"Cannot compare struct type {LLVMType}");
	
	public override string ToString() => LLVMType.ToString();
}