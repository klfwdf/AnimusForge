using System;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;

namespace HarmonyLib
{
	/// <summary>A helper class to invoke method with delegates</summary>
	// Token: 0x0200000A RID: 10
	public static class MethodInvoker
	{
		/// <summary>Creates a fast invocation handler from a method</summary>
		/// <param name="methodInfo">The method to invoke</param>
		/// <param name="directBoxValueAccess">Controls if boxed value object is accessed/updated directly</param>
		/// <returns>The <see cref="T:HarmonyLib.FastInvokeHandler" /></returns>
		/// <remarks>
		///     <para>
		/// The <c>directBoxValueAccess</c> option controls how value types passed by reference (e.g. ref int, out my_struct) are handled in the arguments array
		/// passed to the fast invocation handler.
		/// Since the arguments array is an object array, any value types contained within it are actually references to a boxed value object.
		/// Like any other object, there can be other references to such boxed value objects, other than the reference within the arguments array.
		/// <example>For example,
		/// <code>
		/// var val = 5;
		/// var box = (object)val;
		/// var arr = new object[] { box };
		/// handler(arr); // for a method with parameter signature: ref/out/in int
		/// </code></example></para>
		///     <para>
		/// If <c>directBoxValueAccess</c> is <c>true</c>, the boxed value object is accessed (and potentially updated) directly when the handler is called,
		/// such that all references to the boxed object reflect the potentially updated value.
		/// In the above example, if the method associated with the handler updates the passed (boxed) value to 10, both <c>box</c> and <c>arr[0]</c>
		/// now reflect the value 10. Note that the original <c>val</c> is not updated, since boxing always copies the value into the new boxed value object.
		/// </para>
		///     <para>
		/// If <c>directBoxValueAccess</c> is <c>false</c> (default), the boxed value object in the arguments array is replaced with a "reboxed" value object,
		/// such that potential updates to the value are reflected only in the arguments array.
		/// In the above example, if the method associated with the handler updates the passed (boxed) value to 10, only <c>arr[0]</c> now reflects the value 10.
		/// </para>
		/// </remarks>
		// Token: 0x0600001E RID: 30 RVA: 0x000024BC File Offset: 0x000006BC
		public static FastInvokeHandler GetHandler(MethodInfo methodInfo, bool directBoxValueAccess = false)
		{
			DynamicMethodDefinition dynamicMethod = new DynamicMethodDefinition("FastInvoke_" + methodInfo.Name + "_" + (directBoxValueAccess ? "direct" : "indirect"), typeof(object), new Type[]
			{
				typeof(object),
				typeof(object[])
			});
			ILGenerator il = dynamicMethod.GetILGenerator();
			if (!methodInfo.IsStatic)
			{
				MethodInvoker.Emit(il, OpCodes.Ldarg_0);
				MethodInvoker.EmitUnboxIfNeeded(il, methodInfo.DeclaringType);
			}
			bool generateLocalBoxObject = true;
			ParameterInfo[] ps = methodInfo.GetParameters();
			for (int i = 0; i < ps.Length; i++)
			{
				Type argType = ps[i].ParameterType;
				bool argIsByRef = argType.IsByRef;
				if (argIsByRef)
				{
					argType = argType.GetElementType();
				}
				bool argIsValueType = argType.IsValueType;
				if (argIsByRef && argIsValueType && !directBoxValueAccess)
				{
					MethodInvoker.Emit(il, OpCodes.Ldarg_1);
					MethodInvoker.EmitFastInt(il, i);
				}
				MethodInvoker.Emit(il, OpCodes.Ldarg_1);
				MethodInvoker.EmitFastInt(il, i);
				if (argIsByRef && !argIsValueType)
				{
					MethodInvoker.Emit(il, OpCodes.Ldelema, typeof(object));
				}
				else
				{
					MethodInvoker.Emit(il, OpCodes.Ldelem_Ref);
					if (argIsValueType)
					{
						if (!argIsByRef || !directBoxValueAccess)
						{
							MethodInvoker.Emit(il, OpCodes.Unbox_Any, argType);
							if (argIsByRef)
							{
								MethodInvoker.Emit(il, OpCodes.Box, argType);
								MethodInvoker.Emit(il, OpCodes.Dup);
								if (generateLocalBoxObject)
								{
									generateLocalBoxObject = false;
									il.DeclareLocal(typeof(object), false);
								}
								MethodInvoker.Emit(il, OpCodes.Stloc_0);
								MethodInvoker.Emit(il, OpCodes.Stelem_Ref);
								MethodInvoker.Emit(il, OpCodes.Ldloc_0);
								MethodInvoker.Emit(il, OpCodes.Unbox, argType);
							}
						}
						else
						{
							MethodInvoker.Emit(il, OpCodes.Unbox, argType);
						}
					}
				}
			}
			if (methodInfo.IsStatic)
			{
				MethodInvoker.EmitCall(il, OpCodes.Call, methodInfo);
			}
			else
			{
				MethodInvoker.EmitCall(il, OpCodes.Callvirt, methodInfo);
			}
			if (methodInfo.ReturnType == typeof(void))
			{
				MethodInvoker.Emit(il, OpCodes.Ldnull);
			}
			else
			{
				MethodInvoker.EmitBoxIfNeeded(il, methodInfo.ReturnType);
			}
			MethodInvoker.Emit(il, OpCodes.Ret);
			return dynamicMethod.Generate().CreateDelegate<FastInvokeHandler>();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000026E2 File Offset: 0x000008E2
		internal static void Emit(ILGenerator il, OpCode opcode)
		{
			il.Emit(opcode);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000026EB File Offset: 0x000008EB
		internal static void Emit(ILGenerator il, OpCode opcode, Type type)
		{
			il.Emit(opcode, type);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x000026F5 File Offset: 0x000008F5
		internal static void EmitCall(ILGenerator il, OpCode opcode, MethodInfo methodInfo)
		{
			il.EmitCall(opcode, methodInfo, null);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002700 File Offset: 0x00000900
		private static void EmitUnboxIfNeeded(ILGenerator il, Type type)
		{
			if (type.IsValueType)
			{
				MethodInvoker.Emit(il, OpCodes.Unbox_Any, type);
			}
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002716 File Offset: 0x00000916
		private static void EmitBoxIfNeeded(ILGenerator il, Type type)
		{
			if (type.IsValueType)
			{
				MethodInvoker.Emit(il, OpCodes.Box, type);
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x0000272C File Offset: 0x0000092C
		internal static void EmitFastInt(ILGenerator il, int value)
		{
			switch (value)
			{
			case -1:
				il.Emit(OpCodes.Ldc_I4_M1);
				return;
			case 0:
				il.Emit(OpCodes.Ldc_I4_0);
				return;
			case 1:
				il.Emit(OpCodes.Ldc_I4_1);
				return;
			case 2:
				il.Emit(OpCodes.Ldc_I4_2);
				return;
			case 3:
				il.Emit(OpCodes.Ldc_I4_3);
				return;
			case 4:
				il.Emit(OpCodes.Ldc_I4_4);
				return;
			case 5:
				il.Emit(OpCodes.Ldc_I4_5);
				return;
			case 6:
				il.Emit(OpCodes.Ldc_I4_6);
				return;
			case 7:
				il.Emit(OpCodes.Ldc_I4_7);
				return;
			case 8:
				il.Emit(OpCodes.Ldc_I4_8);
				return;
			default:
				if (value > -129 && value < 128)
				{
					il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
					return;
				}
				il.Emit(OpCodes.Ldc_I4, value);
				return;
			}
		}
	}
}
