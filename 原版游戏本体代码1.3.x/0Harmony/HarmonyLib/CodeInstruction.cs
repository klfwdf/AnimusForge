using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MonoMod.Utils;

namespace HarmonyLib
{
	/// <summary>An abstract wrapper around OpCode and their operands. Used by transpilers</summary>
	// Token: 0x02000076 RID: 118
	public class CodeInstruction
	{
		// Token: 0x06000217 RID: 535 RVA: 0x0000D846 File Offset: 0x0000BA46
		internal CodeInstruction()
		{
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0000D864 File Offset: 0x0000BA64
		internal static CodeInstruction Annotation(string annotation)
		{
			return new CodeInstruction(OpCodes.Nop, annotation);
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0000D871 File Offset: 0x0000BA71
		internal string IsAnnotation()
		{
			if (!(this.opcode == OpCodes.Nop))
			{
				return null;
			}
			return this.operand as string;
		}

		/// <summary>Creates a new CodeInstruction with a given opcode and optional operand</summary>
		/// <param name="opcode">The opcode</param>
		/// <param name="operand">The operand</param>
		// Token: 0x0600021A RID: 538 RVA: 0x0000D892 File Offset: 0x0000BA92
		public CodeInstruction(OpCode opcode, object operand = null)
		{
			this.opcode = opcode;
			this.operand = operand;
		}

		/// <summary>Create a full copy (including labels and exception blocks) of a CodeInstruction</summary>
		/// <param name="instruction">The <see cref="T:HarmonyLib.CodeInstruction" /> to copy</param>
		// Token: 0x0600021B RID: 539 RVA: 0x0000D8C0 File Offset: 0x0000BAC0
		public CodeInstruction(CodeInstruction instruction)
		{
			this.opcode = instruction.opcode;
			this.operand = instruction.operand;
			this.labels = instruction.labels.ToList<Label>();
			this.blocks = instruction.blocks.ToList<ExceptionBlock>();
		}

		/// <summary>Clones a CodeInstruction and resets its labels and exception blocks</summary>
		/// <returns>A lightweight copy of this code instruction</returns>
		// Token: 0x0600021C RID: 540 RVA: 0x0000D923 File Offset: 0x0000BB23
		public CodeInstruction Clone()
		{
			return new CodeInstruction(this)
			{
				labels = new List<Label>(),
				blocks = new List<ExceptionBlock>()
			};
		}

		/// <summary>Clones a CodeInstruction, resets labels and exception blocks and sets its opcode</summary>
		/// <param name="opcode">The opcode</param>
		/// <returns>A copy of this CodeInstruction with a new opcode</returns>
		// Token: 0x0600021D RID: 541 RVA: 0x0000D944 File Offset: 0x0000BB44
		public CodeInstruction Clone(OpCode opcode)
		{
			CodeInstruction instruction = this.Clone();
			instruction.opcode = opcode;
			return instruction;
		}

		/// <summary>Clones a CodeInstruction, resets labels and exception blocks and sets its operand</summary>
		/// <param name="operand">The operand</param>
		/// <returns>A copy of this CodeInstruction with a new operand</returns>
		// Token: 0x0600021E RID: 542 RVA: 0x0000D960 File Offset: 0x0000BB60
		public CodeInstruction Clone(object operand)
		{
			CodeInstruction instruction = this.Clone();
			instruction.operand = operand;
			return instruction;
		}

		/// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
		/// <param name="type">The class/type where the method is declared</param>
		/// <param name="name">The name of the method (case sensitive)</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A code instruction that calls the method matching the arguments</returns>
		// Token: 0x0600021F RID: 543 RVA: 0x0000D97C File Offset: 0x0000BB7C
		public static CodeInstruction Call(Type type, string name, Type[] parameters = null, Type[] generics = null)
		{
			MethodInfo method = AccessTools.Method(type, name, parameters, generics);
			if (method == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 4);
				defaultInterpolatedStringHandler.AppendLiteral("No method found for type=");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(", name=");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				defaultInterpolatedStringHandler.AppendLiteral(", parameters=");
				defaultInterpolatedStringHandler.AppendFormatted(parameters.Description());
				defaultInterpolatedStringHandler.AppendLiteral(", generics=");
				defaultInterpolatedStringHandler.AppendFormatted(generics.Description());
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return new CodeInstruction(OpCodes.Call, method);
		}

		/// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
		/// <param name="typeColonMethodname">The target method in the form <c>TypeFullName:MethodName</c>, where the type name matches a form recognized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype">Type.GetType</a> like <c>Some.Namespace.Type</c>.</param>
		/// <param name="parameters">Optional parameters to target a specific overload of the method</param>
		/// <param name="generics">Optional list of types that define the generic version of the method</param>
		/// <returns>A code instruction that calls the method matching the arguments</returns>
		// Token: 0x06000220 RID: 544 RVA: 0x0000DA14 File Offset: 0x0000BC14
		public static CodeInstruction Call(string typeColonMethodname, Type[] parameters = null, Type[] generics = null)
		{
			MethodInfo method = AccessTools.Method(typeColonMethodname, parameters, generics);
			if (method == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 3);
				defaultInterpolatedStringHandler.AppendLiteral("No method found for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeColonMethodname);
				defaultInterpolatedStringHandler.AppendLiteral(", parameters=");
				defaultInterpolatedStringHandler.AppendFormatted(parameters.Description());
				defaultInterpolatedStringHandler.AppendLiteral(", generics=");
				defaultInterpolatedStringHandler.AppendFormatted(generics.Description());
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return new CodeInstruction(OpCodes.Call, method);
		}

		/// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>A new Codeinstruction</returns>
		// Token: 0x06000221 RID: 545 RVA: 0x0000DA95 File Offset: 0x0000BC95
		public static CodeInstruction Call(Expression<Action> expression)
		{
			return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));
		}

		/// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>A new Codeinstruction</returns>
		// Token: 0x06000222 RID: 546 RVA: 0x0000DAA7 File Offset: 0x0000BCA7
		public static CodeInstruction Call<T>(Expression<Action<T>> expression)
		{
			return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo<T>(expression));
		}

		/// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>A new Codeinstruction</returns>
		// Token: 0x06000223 RID: 547 RVA: 0x0000DAB9 File Offset: 0x0000BCB9
		public static CodeInstruction Call<T, TResult>(Expression<Func<T, TResult>> expression)
		{
			return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo<T, TResult>(expression));
		}

		/// <summary>Creates a CodeInstruction calling a method (CALL)</summary>
		/// <param name="expression">The lambda expression using the method</param>
		/// <returns>A new Codeinstruction</returns>
		// Token: 0x06000224 RID: 548 RVA: 0x0000DACB File Offset: 0x0000BCCB
		public static CodeInstruction Call(LambdaExpression expression)
		{
			return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(expression));
		}

		/// <summary>Returns an instruction to call the specified closure</summary>
		/// <typeparam name="T">The delegate type to emit</typeparam>
		/// <param name="closure">The closure that defines the method to call</param>
		/// <returns>A <see cref="T:HarmonyLib.CodeInstruction" /> that calls the closure as a method</returns>
		// Token: 0x06000225 RID: 549 RVA: 0x0000DAE0 File Offset: 0x0000BCE0
		public static CodeInstruction CallClosure<T>(T closure) where T : Delegate
		{
			if (closure.Method.IsStatic && closure.Target == null)
			{
				return new CodeInstruction(OpCodes.Call, closure.Method);
			}
			Type[] parameters = (from x in closure.Method.GetParameters()
				select x.ParameterType).ToArray<Type>();
			DynamicMethodDefinition closureMethod = new DynamicMethodDefinition(closure.Method.Name, closure.Method.ReturnType, parameters);
			ILGenerator il = closureMethod.GetILGenerator();
			Type targetType = closure.Target.GetType();
			bool flag;
			if (closure.Target != null)
			{
				flag = targetType.GetFields().Any((FieldInfo x) => !x.IsStatic);
			}
			else
			{
				flag = false;
			}
			bool preserveContext = flag;
			if (preserveContext)
			{
				CodeInstruction.State.closureCache.Add(closure);
				il.Emit(OpCodes.Ldsfld, AccessTools.Field(typeof(CodeInstruction.State), "closureCache"));
				il.Emit(OpCodes.Ldc_I4, CodeInstruction.State.closureCache.Count - 1);
				il.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<Delegate>), "Item"));
			}
			else
			{
				if (closure.Target == null)
				{
					il.Emit(OpCodes.Ldnull);
				}
				else
				{
					il.Emit(OpCodes.Newobj, AccessTools.FirstConstructor(targetType, (ConstructorInfo x) => !x.IsStatic && x.GetParameters().Length == 0));
				}
				il.Emit(OpCodes.Ldftn, closure.Method);
				il.Emit(OpCodes.Newobj, AccessTools.Constructor(typeof(T), new Type[]
				{
					typeof(object),
					typeof(IntPtr)
				}, false));
			}
			for (int i = 0; i < parameters.Length; i++)
			{
				il.Emit(OpCodes.Ldarg, i);
			}
			il.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(T), "Invoke", null, null));
			il.Emit(OpCodes.Ret);
			return new CodeInstruction(OpCodes.Call, closureMethod.Generate());
		}

		/// <summary>Creates a CodeInstruction loading a field (LD[S]FLD[A])</summary>
		/// <param name="type">The class/type where the field is defined</param>
		/// <param name="name">The name of the field (case sensitive)</param>
		/// <param name="useAddress">Use address of field</param>
		/// <returns>A new Codeinstruction</returns>
		// Token: 0x06000226 RID: 550 RVA: 0x0000DD3C File Offset: 0x0000BF3C
		public static CodeInstruction LoadField(Type type, string name, bool useAddress = false)
		{
			FieldInfo field = AccessTools.Field(type, name);
			if (field == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 2);
				defaultInterpolatedStringHandler.AppendLiteral("No field found for ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return new CodeInstruction(useAddress ? (field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda) : (field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld), field);
		}

		/// <summary>Creates a CodeInstruction storing to a field (ST[S]FLD)</summary>
		/// <param name="type">The class/type where the field is defined</param>
		/// <param name="name">The name of the field (case sensitive)</param>
		/// <returns>A new Codeinstruction</returns>
		// Token: 0x06000227 RID: 551 RVA: 0x0000DDC8 File Offset: 0x0000BFC8
		public static CodeInstruction StoreField(Type type, string name)
		{
			FieldInfo field = AccessTools.Field(type, name);
			if (field == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 2);
				defaultInterpolatedStringHandler.AppendLiteral("No field found for ");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
				defaultInterpolatedStringHandler.AppendLiteral(" and ");
				defaultInterpolatedStringHandler.AppendFormatted(name);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return new CodeInstruction(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
		}

		/// <summary>Creates a CodeInstruction loading a local with the given index, using the shorter forms when possible</summary>
		/// <param name="index">The index where the local is stored</param>
		/// <param name="useAddress">Use address of local</param>
		/// <returns>A new Codeinstruction</returns>
		/// <seealso cref="M:HarmonyLib.CodeInstructionExtensions.LocalIndex(HarmonyLib.CodeInstruction)" />
		// Token: 0x06000228 RID: 552 RVA: 0x0000DE3C File Offset: 0x0000C03C
		public static CodeInstruction LoadLocal(int index, bool useAddress = false)
		{
			if (useAddress)
			{
				if (index < 256)
				{
					return new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(index));
				}
				return new CodeInstruction(OpCodes.Ldloca, index);
			}
			else
			{
				if (index == 0)
				{
					return new CodeInstruction(OpCodes.Ldloc_0, null);
				}
				if (index == 1)
				{
					return new CodeInstruction(OpCodes.Ldloc_1, null);
				}
				if (index == 2)
				{
					return new CodeInstruction(OpCodes.Ldloc_2, null);
				}
				if (index == 3)
				{
					return new CodeInstruction(OpCodes.Ldloc_3, null);
				}
				if (index < 256)
				{
					return new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(index));
				}
				return new CodeInstruction(OpCodes.Ldloc, index);
			}
		}

		/// <summary>Creates a CodeInstruction storing to a local with the given index, using the shorter forms when possible</summary>
		/// <param name="index">The index where the local is stored</param>
		/// <returns>A new Codeinstruction</returns>
		/// <seealso cref="M:HarmonyLib.CodeInstructionExtensions.LocalIndex(HarmonyLib.CodeInstruction)" />
		// Token: 0x06000229 RID: 553 RVA: 0x0000DEE8 File Offset: 0x0000C0E8
		public static CodeInstruction StoreLocal(int index)
		{
			if (index == 0)
			{
				return new CodeInstruction(OpCodes.Stloc_0, null);
			}
			if (index == 1)
			{
				return new CodeInstruction(OpCodes.Stloc_1, null);
			}
			if (index == 2)
			{
				return new CodeInstruction(OpCodes.Stloc_2, null);
			}
			if (index == 3)
			{
				return new CodeInstruction(OpCodes.Stloc_3, null);
			}
			if (index < 256)
			{
				return new CodeInstruction(OpCodes.Stloc_S, Convert.ToByte(index));
			}
			return new CodeInstruction(OpCodes.Stloc, index);
		}

		/// <summary>Creates a CodeInstruction loading an argument with the given index, using the shorter forms when possible</summary>
		/// <param name="index">The index of the argument</param>
		/// <param name="useAddress">Use address of argument</param>
		/// <returns>A new Codeinstruction</returns>
		/// <seealso cref="M:HarmonyLib.CodeInstructionExtensions.ArgumentIndex(HarmonyLib.CodeInstruction)" />
		// Token: 0x0600022A RID: 554 RVA: 0x0000DF64 File Offset: 0x0000C164
		public static CodeInstruction LoadArgument(int index, bool useAddress = false)
		{
			if (useAddress)
			{
				if (index < 256)
				{
					return new CodeInstruction(OpCodes.Ldarga_S, Convert.ToByte(index));
				}
				return new CodeInstruction(OpCodes.Ldarga, index);
			}
			else
			{
				if (index == 0)
				{
					return new CodeInstruction(OpCodes.Ldarg_0, null);
				}
				if (index == 1)
				{
					return new CodeInstruction(OpCodes.Ldarg_1, null);
				}
				if (index == 2)
				{
					return new CodeInstruction(OpCodes.Ldarg_2, null);
				}
				if (index == 3)
				{
					return new CodeInstruction(OpCodes.Ldarg_3, null);
				}
				if (index < 256)
				{
					return new CodeInstruction(OpCodes.Ldarg_S, Convert.ToByte(index));
				}
				return new CodeInstruction(OpCodes.Ldarg, index);
			}
		}

		/// <summary>Creates a CodeInstruction storing to an argument with the given index, using the shorter forms when possible</summary>
		/// <param name="index">The index of the argument</param>
		/// <returns>A new Codeinstruction</returns>
		/// <seealso cref="M:HarmonyLib.CodeInstructionExtensions.ArgumentIndex(HarmonyLib.CodeInstruction)" />
		// Token: 0x0600022B RID: 555 RVA: 0x0000E010 File Offset: 0x0000C210
		public static CodeInstruction StoreArgument(int index)
		{
			if (index < 256)
			{
				return new CodeInstruction(OpCodes.Starg_S, Convert.ToByte(index));
			}
			return new CodeInstruction(OpCodes.Starg, index);
		}

		/// <summary>Checks if a CodeInstruction contains a given exception block type</summary>
		/// <param name="type">Type of the exception block to check for</param>
		/// <returns>True if the instruction contains the exception block type, false otherwise</returns>
		// Token: 0x0600022C RID: 556 RVA: 0x0000E040 File Offset: 0x0000C240
		public bool HasBlock(ExceptionBlockType type)
		{
			List<ExceptionBlock> list = this.blocks;
			return list != null && list.Any((ExceptionBlock block) => block.blockType == type);
		}

		/// <summary>Returns a string representation of the code instruction</summary>
		/// <returns>A string representation of the code instruction</returns>
		// Token: 0x0600022D RID: 557 RVA: 0x0000E078 File Offset: 0x0000C278
		public override string ToString()
		{
			List<string> list = new List<string>();
			foreach (Label label in this.labels)
			{
				List<string> list2 = list;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Label");
				defaultInterpolatedStringHandler.AppendFormatted<int>(label.GetHashCode());
				list2.Add(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			foreach (ExceptionBlock block in this.blocks)
			{
				list.Add("EX_" + block.blockType.ToString().Replace("Block", ""));
			}
			string extras = ((list.Count > 0) ? (" [" + string.Join(", ", list.ToArray()) + "]") : "");
			string operandStr = Emitter.FormatOperand(this.operand);
			if (operandStr.Length > 0)
			{
				operandStr = " " + operandStr;
			}
			OpCode opCode = this.opcode;
			return opCode.ToString() + operandStr + extras;
		}

		/// <summary>The opcode</summary>
		// Token: 0x0400017E RID: 382
		public OpCode opcode;

		/// <summary>The operand</summary>
		// Token: 0x0400017F RID: 383
		public object operand;

		/// <summary>All labels defined on this instruction</summary>
		// Token: 0x04000180 RID: 384
		public List<Label> labels = new List<Label>();

		/// <summary>All exception block boundaries defined on this instruction</summary>
		// Token: 0x04000181 RID: 385
		public List<ExceptionBlock> blocks = new List<ExceptionBlock>();

		// Token: 0x02000077 RID: 119
		internal static class State
		{
			// Token: 0x04000182 RID: 386
			internal static readonly List<Delegate> closureCache = new List<Delegate>();
		}
	}
}
