using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x020008B4 RID: 2228
	[NullableContext(1)]
	[Nullable(0)]
	internal static class Extensions
	{
		// Token: 0x06002DDA RID: 11738 RVA: 0x0009A0AC File Offset: 0x000982AC
		[NullableContext(2)]
		public static TypeDefinition SafeResolve(this TypeReference r)
		{
			TypeDefinition result;
			try
			{
				result = ((r != null) ? r.Resolve() : null);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06002DDB RID: 11739 RVA: 0x0009A0E0 File Offset: 0x000982E0
		[NullableContext(2)]
		public static FieldDefinition SafeResolve(this FieldReference r)
		{
			FieldDefinition result;
			try
			{
				result = ((r != null) ? r.Resolve() : null);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06002DDC RID: 11740 RVA: 0x0009A114 File Offset: 0x00098314
		[NullableContext(2)]
		public static MethodDefinition SafeResolve(this MethodReference r)
		{
			MethodDefinition result;
			try
			{
				result = ((r != null) ? r.Resolve() : null);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06002DDD RID: 11741 RVA: 0x0009A148 File Offset: 0x00098348
		[NullableContext(2)]
		public static PropertyDefinition SafeResolve(this PropertyReference r)
		{
			PropertyDefinition result;
			try
			{
				result = ((r != null) ? r.Resolve() : null);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06002DDE RID: 11742 RVA: 0x0009A17C File Offset: 0x0009837C
		[return: Nullable(2)]
		public static CustomAttribute GetCustomAttribute(this Mono.Cecil.ICustomAttributeProvider cap, string attribute)
		{
			if (cap == null || !cap.HasCustomAttributes)
			{
				return null;
			}
			foreach (CustomAttribute attrib in cap.CustomAttributes)
			{
				if (attrib.AttributeType.FullName == attribute)
				{
					return attrib;
				}
			}
			return null;
		}

		// Token: 0x06002DDF RID: 11743 RVA: 0x0009A1F0 File Offset: 0x000983F0
		public static bool HasCustomAttribute(this Mono.Cecil.ICustomAttributeProvider cap, string attribute)
		{
			return cap.GetCustomAttribute(attribute) != null;
		}

		// Token: 0x06002DE0 RID: 11744 RVA: 0x0009A1FC File Offset: 0x000983FC
		public static int GetInt(this Instruction instr)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			Mono.Cecil.Cil.OpCode op = instr.OpCode;
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_M1)
			{
				return -1;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_0)
			{
				return 0;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_1)
			{
				return 1;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_2)
			{
				return 2;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_3)
			{
				return 3;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_4)
			{
				return 4;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_5)
			{
				return 5;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_6)
			{
				return 6;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_7)
			{
				return 7;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_8)
			{
				return 8;
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_S)
			{
				return (int)((sbyte)instr.Operand);
			}
			return (int)instr.Operand;
		}

		// Token: 0x06002DE1 RID: 11745 RVA: 0x0009A2D8 File Offset: 0x000984D8
		public static int? GetIntOrNull(this Instruction instr)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			Mono.Cecil.Cil.OpCode op = instr.OpCode;
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_M1)
			{
				return new int?(-1);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_0)
			{
				return new int?(0);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_1)
			{
				return new int?(1);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_2)
			{
				return new int?(2);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_3)
			{
				return new int?(3);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_4)
			{
				return new int?(4);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_5)
			{
				return new int?(5);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_6)
			{
				return new int?(6);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_7)
			{
				return new int?(7);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_8)
			{
				return new int?(8);
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_S)
			{
				return new int?((int)((sbyte)instr.Operand));
			}
			if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4)
			{
				return new int?((int)instr.Operand);
			}
			return null;
		}

		// Token: 0x06002DE2 RID: 11746 RVA: 0x0009A404 File Offset: 0x00098604
		public static bool IsBaseMethodCall(this Mono.Cecil.Cil.MethodBody body, [Nullable(2)] MethodReference called)
		{
			Helpers.ThrowIfArgumentNull<Mono.Cecil.Cil.MethodBody>(body, "body");
			MethodDefinition caller = body.Method;
			if (called == null)
			{
				return false;
			}
			TypeReference calledType = called.DeclaringType;
			for (;;)
			{
				TypeSpecification typeSpec = calledType as TypeSpecification;
				if (typeSpec == null)
				{
					break;
				}
				calledType = typeSpec.ElementType;
			}
			string calledTypeName = calledType.GetPatchFullName();
			bool callingBaseType = false;
			try
			{
				TypeDefinition baseType = caller.DeclaringType;
				do
				{
					TypeReference baseType2 = baseType.BaseType;
					if ((baseType = ((baseType2 != null) ? baseType2.SafeResolve() : null)) == null)
					{
						goto IL_72;
					}
				}
				while (!(baseType.GetPatchFullName() == calledTypeName));
				callingBaseType = true;
				IL_72:;
			}
			catch
			{
				callingBaseType = caller.DeclaringType.GetPatchFullName() == calledTypeName;
			}
			return callingBaseType;
		}

		// Token: 0x06002DE3 RID: 11747 RVA: 0x0009A4B0 File Offset: 0x000986B0
		public static bool IsCallvirt(this MethodReference method)
		{
			Helpers.ThrowIfArgumentNull<MethodReference>(method, "method");
			return method.HasThis && !method.DeclaringType.IsValueType;
		}

		// Token: 0x06002DE4 RID: 11748 RVA: 0x0009A4D7 File Offset: 0x000986D7
		public static bool IsStruct(this TypeReference type)
		{
			Helpers.ThrowIfArgumentNull<TypeReference>(type, "type");
			return type.IsValueType && !type.IsPrimitive;
		}

		// Token: 0x06002DE5 RID: 11749 RVA: 0x0009A4FC File Offset: 0x000986FC
		public static Mono.Cecil.Cil.OpCode ToLongOp(this Mono.Cecil.Cil.OpCode op)
		{
			string name = Enum.GetName(Extensions.t_Code, op.Code);
			if (name == null || !name.EndsWith("_S", StringComparison.Ordinal))
			{
				return op;
			}
			Dictionary<int, Mono.Cecil.Cil.OpCode> toLongOp = Extensions._ToLongOp;
			Mono.Cecil.Cil.OpCode result;
			lock (toLongOp)
			{
				Mono.Cecil.Cil.OpCode found;
				if (Extensions._ToLongOp.TryGetValue((int)op.Code, out found))
				{
					result = found;
				}
				else
				{
					Dictionary<int, Mono.Cecil.Cil.OpCode> toLongOp2 = Extensions._ToLongOp;
					int code = (int)op.Code;
					FieldInfo field = Extensions.t_OpCodes.GetField(name.Substring(0, name.Length - 2));
					result = (toLongOp2[code] = ((Mono.Cecil.Cil.OpCode?)((field != null) ? field.GetValue(null) : null)).GetValueOrDefault(op));
				}
			}
			return result;
		}

		// Token: 0x06002DE6 RID: 11750 RVA: 0x0009A5C8 File Offset: 0x000987C8
		public static Mono.Cecil.Cil.OpCode ToShortOp(this Mono.Cecil.Cil.OpCode op)
		{
			string name = Enum.GetName(Extensions.t_Code, op.Code);
			if (name == null || name.EndsWith("_S", StringComparison.Ordinal))
			{
				return op;
			}
			Dictionary<int, Mono.Cecil.Cil.OpCode> toShortOp = Extensions._ToShortOp;
			Mono.Cecil.Cil.OpCode result;
			lock (toShortOp)
			{
				Mono.Cecil.Cil.OpCode found;
				if (Extensions._ToShortOp.TryGetValue((int)op.Code, out found))
				{
					result = found;
				}
				else
				{
					Dictionary<int, Mono.Cecil.Cil.OpCode> toShortOp2 = Extensions._ToShortOp;
					int code = (int)op.Code;
					FieldInfo field = Extensions.t_OpCodes.GetField(name + "_S");
					result = (toShortOp2[code] = ((Mono.Cecil.Cil.OpCode?)((field != null) ? field.GetValue(null) : null)).GetValueOrDefault(op));
				}
			}
			return result;
		}

		// Token: 0x06002DE7 RID: 11751 RVA: 0x0009A690 File Offset: 0x00098890
		public static void RecalculateILOffsets(this MethodDefinition method)
		{
			Helpers.ThrowIfArgumentNull<MethodDefinition>(method, "method");
			if (!method.HasBody)
			{
				return;
			}
			int offs = 0;
			for (int i = 0; i < method.Body.Instructions.Count; i++)
			{
				Instruction instr = method.Body.Instructions[i];
				instr.Offset = offs;
				offs += instr.GetSize();
			}
		}

		// Token: 0x06002DE8 RID: 11752 RVA: 0x0009A6F0 File Offset: 0x000988F0
		public static void FixShortLongOps(this MethodDefinition method)
		{
			Helpers.ThrowIfArgumentNull<MethodDefinition>(method, "method");
			if (!method.HasBody)
			{
				return;
			}
			for (int i = 0; i < method.Body.Instructions.Count; i++)
			{
				Instruction instr = method.Body.Instructions[i];
				if (instr.Operand is Instruction)
				{
					instr.OpCode = instr.OpCode.ToLongOp();
				}
			}
			method.RecalculateILOffsets();
			bool optimized;
			do
			{
				optimized = false;
				for (int j = 0; j < method.Body.Instructions.Count; j++)
				{
					Instruction instr2 = method.Body.Instructions[j];
					Instruction target = instr2.Operand as Instruction;
					if (target != null)
					{
						int num = target.Offset - (instr2.Offset + instr2.GetSize());
						if (num == (int)((sbyte)num))
						{
							Mono.Cecil.Cil.OpCode opCode = instr2.OpCode;
							instr2.OpCode = instr2.OpCode.ToShortOp();
							optimized = opCode != instr2.OpCode;
						}
					}
				}
			}
			while (optimized);
		}

		// Token: 0x06002DE9 RID: 11753 RVA: 0x0009A7EC File Offset: 0x000989EC
		[NullableContext(2)]
		public static bool Is(this MemberInfo minfo, MemberReference mref)
		{
			return mref.Is(minfo);
		}

		// Token: 0x06002DEA RID: 11754 RVA: 0x0009A7F8 File Offset: 0x000989F8
		[NullableContext(2)]
		public static bool Is(this MemberReference mref, MemberInfo minfo)
		{
			if (mref == null)
			{
				return false;
			}
			if (minfo == null)
			{
				return false;
			}
			TypeReference mrefDecl = mref.DeclaringType;
			if (((mrefDecl != null) ? mrefDecl.FullName : null) == "<Module>")
			{
				mrefDecl = null;
			}
			GenericParameter genParamRef = mref as GenericParameter;
			if (genParamRef != null)
			{
				Type genParamInfo = minfo as Type;
				if (genParamInfo == null)
				{
					return false;
				}
				if (!genParamInfo.IsGenericParameter)
				{
					IGenericInstance genParamRefOwner = genParamRef.Owner as IGenericInstance;
					return genParamRefOwner != null && genParamRefOwner.GenericArguments[genParamRef.Position].Is(genParamInfo);
				}
				return genParamRef.Position == genParamInfo.GenericParameterPosition;
			}
			else
			{
				if (minfo.DeclaringType != null)
				{
					if (mrefDecl == null)
					{
						return false;
					}
					Type declType = minfo.DeclaringType;
					if (minfo is Type && declType.IsGenericType && !declType.IsGenericTypeDefinition)
					{
						declType = declType.GetGenericTypeDefinition();
					}
					if (!mrefDecl.Is(declType))
					{
						return false;
					}
				}
				else if (mrefDecl != null)
				{
					return false;
				}
				if (!(mref is TypeSpecification) && mref.Name != minfo.Name)
				{
					return false;
				}
				TypeReference typeRef = mref as TypeReference;
				if (typeRef != null)
				{
					Type typeInfo = minfo as Type;
					if (typeInfo == null)
					{
						return false;
					}
					if (typeInfo.IsGenericParameter)
					{
						return false;
					}
					GenericInstanceType genTypeRef = mref as GenericInstanceType;
					if (genTypeRef != null)
					{
						if (!typeInfo.IsGenericType)
						{
							return false;
						}
						Collection<TypeReference> gparamRefs = genTypeRef.GenericArguments;
						Type[] gparamInfos = typeInfo.GetGenericArguments();
						if (gparamRefs.Count != gparamInfos.Length)
						{
							return false;
						}
						for (int i = 0; i < gparamRefs.Count; i++)
						{
							if (!gparamRefs[i].Is(gparamInfos[i]))
							{
								return false;
							}
						}
						return genTypeRef.ElementType.Is(typeInfo.GetGenericTypeDefinition());
					}
					else
					{
						if (typeRef.HasGenericParameters)
						{
							if (!typeInfo.IsGenericType)
							{
								return false;
							}
							Collection<GenericParameter> gparamRefs2 = typeRef.GenericParameters;
							Type[] gparamInfos2 = typeInfo.GetGenericArguments();
							if (gparamRefs2.Count != gparamInfos2.Length)
							{
								return false;
							}
							for (int j = 0; j < gparamRefs2.Count; j++)
							{
								if (!gparamRefs2[j].Is(gparamInfos2[j]))
								{
									return false;
								}
							}
						}
						else if (typeInfo.IsGenericType)
						{
							return false;
						}
						ArrayType arrayTypeRef = mref as ArrayType;
						if (arrayTypeRef != null)
						{
							return typeInfo.IsArray && arrayTypeRef.Dimensions.Count == typeInfo.GetArrayRank() && arrayTypeRef.ElementType.Is(typeInfo.GetElementType());
						}
						ByReferenceType byRefTypeRef = mref as ByReferenceType;
						if (byRefTypeRef != null)
						{
							return typeInfo.IsByRef && byRefTypeRef.ElementType.Is(typeInfo.GetElementType());
						}
						PointerType ptrTypeRef = mref as PointerType;
						if (ptrTypeRef != null)
						{
							return typeInfo.IsPointer && ptrTypeRef.ElementType.Is(typeInfo.GetElementType());
						}
						TypeSpecification typeSpecRef = mref as TypeSpecification;
						if (typeSpecRef != null)
						{
							return typeSpecRef.ElementType.Is(typeInfo.HasElementType ? typeInfo.GetElementType() : typeInfo);
						}
						if (mrefDecl != null)
						{
							return mref.Name == typeInfo.Name;
						}
						string fullName = mref.FullName;
						string fullName2 = typeInfo.FullName;
						return fullName == ((fullName2 != null) ? fullName2.Replace("+", "/", StringComparison.Ordinal) : null);
					}
				}
				else
				{
					if (minfo is Type)
					{
						return false;
					}
					MethodReference methodRef = mref as MethodReference;
					if (methodRef == null)
					{
						return !(minfo is MethodInfo) && mref is FieldReference == minfo is FieldInfo && mref is PropertyReference == minfo is PropertyInfo && mref is EventReference == minfo is EventInfo;
					}
					MethodBase methodInfo = minfo as MethodBase;
					if (methodInfo == null)
					{
						return false;
					}
					Collection<ParameterDefinition> paramRefs = methodRef.Parameters;
					ParameterInfo[] paramInfos = methodInfo.GetParameters();
					if (paramRefs.Count != paramInfos.Length)
					{
						return false;
					}
					GenericInstanceMethod genMethodRef = mref as GenericInstanceMethod;
					if (genMethodRef == null)
					{
						if (methodRef.HasGenericParameters)
						{
							if (!methodInfo.IsGenericMethod)
							{
								return false;
							}
							Collection<GenericParameter> gparamRefs3 = methodRef.GenericParameters;
							Type[] gparamInfos3 = methodInfo.GetGenericArguments();
							if (gparamRefs3.Count != gparamInfos3.Length)
							{
								return false;
							}
							for (int k = 0; k < gparamRefs3.Count; k++)
							{
								if (!gparamRefs3[k].Is(gparamInfos3[k]))
								{
									return false;
								}
							}
						}
						else if (methodInfo.IsGenericMethod)
						{
							return false;
						}
						Relinker resolver = delegate(IMetadataTokenProvider paramMemberRef, [Nullable(2)] IGenericParameterProvider ctx)
						{
							TypeReference paramTypeRef = paramMemberRef as TypeReference;
							if (paramTypeRef == null)
							{
								return paramMemberRef;
							}
							return base.<Is>g__ResolveParameter|1(paramTypeRef);
						};
						MemberReference mref2 = methodRef.ReturnType.Relink(resolver, null);
						MethodInfo methodInfo2 = methodInfo as MethodInfo;
						if (!mref2.Is(((methodInfo2 != null) ? methodInfo2.ReturnType : null) ?? typeof(void)))
						{
							MemberReference returnType = methodRef.ReturnType;
							MethodInfo methodInfo3 = methodInfo as MethodInfo;
							if (!returnType.Is(((methodInfo3 != null) ? methodInfo3.ReturnType : null) ?? typeof(void)))
							{
								return false;
							}
						}
						for (int l = 0; l < paramRefs.Count; l++)
						{
							if (!paramRefs[l].ParameterType.Relink(resolver, null).Is(paramInfos[l].ParameterType) && !paramRefs[l].ParameterType.Is(paramInfos[l].ParameterType))
							{
								return false;
							}
						}
						return true;
					}
					if (!methodInfo.IsGenericMethod)
					{
						return false;
					}
					Collection<TypeReference> gparamRefs4 = genMethodRef.GenericArguments;
					Type[] gparamInfos4 = methodInfo.GetGenericArguments();
					if (gparamRefs4.Count != gparamInfos4.Length)
					{
						return false;
					}
					for (int m = 0; m < gparamRefs4.Count; m++)
					{
						if (!gparamRefs4[m].Is(gparamInfos4[m]))
						{
							return false;
						}
					}
					MemberReference elementMethod = genMethodRef.ElementMethod;
					MethodInfo methodInfo4 = methodInfo as MethodInfo;
					return elementMethod.Is(((methodInfo4 != null) ? methodInfo4.GetGenericMethodDefinition() : null) ?? methodInfo);
				}
			}
		}

		// Token: 0x06002DEB RID: 11755 RVA: 0x0009AD98 File Offset: 0x00098F98
		public static IMetadataTokenProvider ImportReference(this ModuleDefinition mod, IMetadataTokenProvider mtp)
		{
			Helpers.ThrowIfArgumentNull<ModuleDefinition>(mod, "mod");
			TypeReference type = mtp as TypeReference;
			if (type != null)
			{
				return mod.ImportReference(type);
			}
			FieldReference field = mtp as FieldReference;
			if (field != null)
			{
				return mod.ImportReference(field);
			}
			MethodReference method = mtp as MethodReference;
			if (method != null)
			{
				return mod.ImportReference(method);
			}
			Mono.Cecil.CallSite callsite = mtp as Mono.Cecil.CallSite;
			if (callsite != null)
			{
				return mod.ImportReference(callsite);
			}
			return mtp;
		}

		// Token: 0x06002DEC RID: 11756 RVA: 0x0009ADFC File Offset: 0x00098FFC
		public static Mono.Cecil.CallSite ImportReference(this ModuleDefinition mod, Mono.Cecil.CallSite callsite)
		{
			Helpers.ThrowIfArgumentNull<ModuleDefinition>(mod, "mod");
			Helpers.ThrowIfArgumentNull<Mono.Cecil.CallSite>(callsite, "callsite");
			Mono.Cecil.CallSite cs = new Mono.Cecil.CallSite(mod.ImportReference(callsite.ReturnType));
			cs.CallingConvention = callsite.CallingConvention;
			cs.ExplicitThis = callsite.ExplicitThis;
			cs.HasThis = callsite.HasThis;
			foreach (ParameterDefinition param in callsite.Parameters)
			{
				ParameterDefinition p = new ParameterDefinition(mod.ImportReference(param.ParameterType))
				{
					Name = param.Name,
					Attributes = param.Attributes,
					Constant = param.Constant,
					MarshalInfo = param.MarshalInfo
				};
				cs.Parameters.Add(p);
			}
			return cs;
		}

		// Token: 0x06002DED RID: 11757 RVA: 0x0009AEE4 File Offset: 0x000990E4
		public static void AddRange<[Nullable(2)] T>(this Collection<T> list, IEnumerable<T> other)
		{
			Helpers.ThrowIfArgumentNull<Collection<T>>(list, "list");
			foreach (T entry in Helpers.ThrowIfNull<IEnumerable<T>>(other, "other"))
			{
				list.Add(entry);
			}
		}

		// Token: 0x06002DEE RID: 11758 RVA: 0x0009AF44 File Offset: 0x00099144
		public static void AddRange(this IDictionary dict, IDictionary other)
		{
			Helpers.ThrowIfArgumentNull<IDictionary>(dict, "dict");
			foreach (object obj in Helpers.ThrowIfNull<IDictionary>(other, "other"))
			{
				DictionaryEntry entry = (DictionaryEntry)obj;
				dict.Add(entry.Key, entry.Value);
			}
		}

		// Token: 0x06002DEF RID: 11759 RVA: 0x0009AFBC File Offset: 0x000991BC
		public static void AddRange<[Nullable(2)] TKey, [Nullable(2)] TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other)
		{
			Helpers.ThrowIfArgumentNull<IDictionary<TKey, TValue>>(dict, "dict");
			foreach (KeyValuePair<TKey, TValue> entry in Helpers.ThrowIfNull<IDictionary<TKey, TValue>>(other, "other"))
			{
				dict.Add(entry.Key, entry.Value);
			}
		}

		// Token: 0x06002DF0 RID: 11760 RVA: 0x0009B028 File Offset: 0x00099228
		public static void AddRange<TKey, [Nullable(2)] TValue>(this Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> other)
		{
			Helpers.ThrowIfArgumentNull<Dictionary<TKey, TValue>>(dict, "dict");
			foreach (KeyValuePair<TKey, TValue> entry in Helpers.ThrowIfNull<Dictionary<TKey, TValue>>(other, "other"))
			{
				dict.Add(entry.Key, entry.Value);
			}
		}

		// Token: 0x06002DF1 RID: 11761 RVA: 0x0009B098 File Offset: 0x00099298
		public static void InsertRange<[Nullable(2)] T>(this Collection<T> list, int index, IEnumerable<T> other)
		{
			Helpers.ThrowIfArgumentNull<Collection<T>>(list, "list");
			foreach (T entry in Helpers.ThrowIfNull<IEnumerable<T>>(other, "other"))
			{
				list.Insert(index++, entry);
			}
		}

		// Token: 0x06002DF2 RID: 11762 RVA: 0x0009B0FC File Offset: 0x000992FC
		public static bool IsCompatible(this Type type, Type other)
		{
			return Helpers.ThrowIfNull<Type>(type, "type")._IsCompatible(Helpers.ThrowIfNull<Type>(other, "other")) || other._IsCompatible(type);
		}

		// Token: 0x06002DF3 RID: 11763 RVA: 0x0009B124 File Offset: 0x00099324
		private static bool _IsCompatible(this Type type, Type other)
		{
			return type == other || ((!other.IsEnum || !(type == typeof(Enum))) && (!other.IsValueType || !(type == typeof(ValueType))) && (type.IsAssignableFrom(other) || (other.IsEnum && type.IsCompatible(Enum.GetUnderlyingType(other))) || ((other.IsPointer || other.IsByRef) && type == typeof(IntPtr)) || (type.IsPointer && other.IsPointer) || (type.IsByRef && other.IsPointer)));
		}

		// Token: 0x06002DF4 RID: 11764 RVA: 0x0009B1E0 File Offset: 0x000993E0
		public static T GetDeclaredMember<[Nullable(0)] T>(this T member) where T : MemberInfo
		{
			Helpers.ThrowIfArgumentNull<T>(member, "member");
			if (member.DeclaringType == member.ReflectedType)
			{
				return member;
			}
			if (member.DeclaringType != null)
			{
				int mt = member.MetadataToken;
				foreach (MemberInfo other in member.DeclaringType.GetMembers((BindingFlags)(-1)))
				{
					if (other.MetadataToken == mt)
					{
						return (T)((object)other);
					}
				}
			}
			return member;
		}

		// Token: 0x06002DF5 RID: 11765 RVA: 0x0009B268 File Offset: 0x00099468
		public unsafe static void SetMonoCorlibInternal(this Assembly asm, bool value)
		{
			if (PlatformDetection.Runtime != RuntimeKind.Mono)
			{
				return;
			}
			Helpers.ThrowIfArgumentNull<Assembly>(asm, "asm");
			Type asmType = asm.GetType();
			if (asmType == null)
			{
				return;
			}
			Dictionary<Type, FieldInfo> obj = Extensions.fmap_mono_assembly;
			FieldInfo f_mono_assembly;
			lock (obj)
			{
				if (!Extensions.fmap_mono_assembly.TryGetValue(asmType, out f_mono_assembly))
				{
					FieldInfo field;
					if ((field = asmType.GetField("_mono_assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) == null && (field = asmType.GetField("dynamic_assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) == null)
					{
						throw new InvalidOperationException("Could not find assembly field for Mono");
					}
					f_mono_assembly = field;
					Extensions.fmap_mono_assembly[asmType] = f_mono_assembly;
				}
			}
			if (f_mono_assembly == null)
			{
				return;
			}
			AssemblyName name = asm.GetName();
			Dictionary<string, WeakReference> assemblyCache = ReflectionHelper.AssemblyCache;
			lock (assemblyCache)
			{
				WeakReference asmRef = new WeakReference(asm);
				ReflectionHelper.AssemblyCache[asm.GetRuntimeHashedFullName()] = asmRef;
				ReflectionHelper.AssemblyCache[name.FullName] = asmRef;
				if (name.Name != null)
				{
					ReflectionHelper.AssemblyCache[name.Name] = asmRef;
				}
			}
			long asmPtr = 0L;
			object value2 = f_mono_assembly.GetValue(asm);
			if (value2 is IntPtr)
			{
				IntPtr i = (IntPtr)value2;
				asmPtr = (long)i;
			}
			else if (value2 is UIntPtr)
			{
				UIntPtr u = (UIntPtr)value2;
				asmPtr = (long)(ulong)u;
			}
			int offs = IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + IntPtr.Size + 20 + 4 + 4 + 4 + ((!Extensions._MonoAssemblyNameHasArch) ? (ReflectionHelper.IsCoreBCL ? 16 : 8) : (ReflectionHelper.IsCoreBCL ? ((IntPtr.Size == 4) ? 20 : 24) : ((IntPtr.Size == 4) ? 12 : 16))) + IntPtr.Size + IntPtr.Size + 1 + 1 + 1;
			byte* corlibInternalPtr = asmPtr + offs;
			*corlibInternalPtr = ((value > false) ? 1 : 0);
		}

		// Token: 0x06002DF6 RID: 11766 RVA: 0x0009B46C File Offset: 0x0009966C
		public static bool IsDynamicMethod(this MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			if (Extensions._RTDynamicMethod != null)
			{
				return method is DynamicMethod || method.GetType() == Extensions._RTDynamicMethod;
			}
			if (method is DynamicMethod)
			{
				return true;
			}
			if (method.MetadataToken != 0 || !method.IsStatic || !method.IsPublic || (method.Attributes & System.Reflection.MethodAttributes.PrivateScope) != System.Reflection.MethodAttributes.PrivateScope)
			{
				return false;
			}
			if (method.DeclaringType != null)
			{
				foreach (MethodInfo other in method.DeclaringType.GetMethods(BindingFlags.Static | BindingFlags.Public))
				{
					if (method == other)
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06002DF7 RID: 11767 RVA: 0x0009B510 File Offset: 0x00099710
		[return: Nullable(2)]
		public static object SafeGetTarget(this WeakReference weak)
		{
			Helpers.ThrowIfArgumentNull<WeakReference>(weak, "weak");
			object result;
			try
			{
				result = weak.Target;
			}
			catch (InvalidOperationException)
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06002DF8 RID: 11768 RVA: 0x0009B548 File Offset: 0x00099748
		public static bool SafeGetIsAlive(this WeakReference weak)
		{
			Helpers.ThrowIfArgumentNull<WeakReference>(weak, "weak");
			bool result;
			try
			{
				result = weak.IsAlive;
			}
			catch (InvalidOperationException)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06002DF9 RID: 11769 RVA: 0x0009B580 File Offset: 0x00099780
		public static T CreateDelegate<[Nullable(0)] T>(this MethodBase method) where T : Delegate
		{
			return (T)((object)method.CreateDelegate(typeof(T), null));
		}

		// Token: 0x06002DFA RID: 11770 RVA: 0x0009B598 File Offset: 0x00099798
		public static T CreateDelegate<[Nullable(0)] T>(this MethodBase method, [Nullable(2)] object target) where T : Delegate
		{
			return (T)((object)method.CreateDelegate(typeof(T), target));
		}

		// Token: 0x06002DFB RID: 11771 RVA: 0x0009B5B0 File Offset: 0x000997B0
		public static Delegate CreateDelegate(this MethodBase method, Type delegateType)
		{
			return method.CreateDelegate(delegateType, null);
		}

		// Token: 0x06002DFC RID: 11772 RVA: 0x0009B5BC File Offset: 0x000997BC
		public static Delegate CreateDelegate(this MethodBase method, Type delegateType, [Nullable(2)] object target)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			Helpers.ThrowIfArgumentNull<Type>(delegateType, "delegateType");
			if (!typeof(Delegate).IsAssignableFrom(delegateType))
			{
				throw new ArgumentException("Type argument must be a delegate type!");
			}
			DynamicMethod dm = method as DynamicMethod;
			if (dm != null)
			{
				return dm.CreateDelegate(delegateType, target);
			}
			MethodInfo mi = method as MethodInfo;
			if (mi != null)
			{
				return Delegate.CreateDelegate(delegateType, target, mi);
			}
			RuntimeMethodHandle handle = method.MethodHandle;
			RuntimeHelpers.PrepareMethod(handle);
			IntPtr ptr = handle.GetFunctionPointer();
			return (Delegate)Activator.CreateInstance(delegateType, new object[] { target, ptr });
		}

		// Token: 0x06002DFD RID: 11773 RVA: 0x0009B658 File Offset: 0x00099858
		[NullableContext(2)]
		public static T TryCreateDelegate<[Nullable(0)] T>(this MethodInfo mi) where T : Delegate
		{
			T t;
			try
			{
				T t2;
				if (mi == null)
				{
					t = default(T);
					t2 = t;
				}
				else
				{
					t2 = mi.CreateDelegate<T>();
				}
				t = t2;
			}
			catch
			{
				t = default(T);
			}
			return t;
		}

		// Token: 0x06002DFE RID: 11774 RVA: 0x0009B69C File Offset: 0x0009989C
		[return: Nullable(2)]
		public static MethodDefinition FindMethod(this TypeDefinition type, string id, bool simple = true)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			Helpers.ThrowIfArgumentNull<string>(id, "id");
			if (simple && !id.Contains(' ', StringComparison.Ordinal))
			{
				foreach (MethodDefinition method in type.Methods)
				{
					if (method.GetID(null, null, true, true) == id)
					{
						return method;
					}
				}
				foreach (MethodDefinition method2 in type.Methods)
				{
					if (method2.GetID(null, null, false, true) == id)
					{
						return method2;
					}
				}
			}
			foreach (MethodDefinition method3 in type.Methods)
			{
				if (method3.GetID(null, null, true, false) == id)
				{
					return method3;
				}
			}
			foreach (MethodDefinition method4 in type.Methods)
			{
				if (method4.GetID(null, null, false, false) == id)
				{
					return method4;
				}
			}
			return null;
		}

		// Token: 0x06002DFF RID: 11775 RVA: 0x0009B82C File Offset: 0x00099A2C
		[return: Nullable(2)]
		public static MethodDefinition FindMethodDeep(this TypeDefinition type, string id, bool simple = true)
		{
			MethodDefinition result;
			if ((result = Helpers.ThrowIfNull<TypeDefinition>(type, "type").FindMethod(id, simple)) == null)
			{
				TypeReference baseType = type.BaseType;
				if (baseType == null)
				{
					return null;
				}
				TypeDefinition typeDefinition = baseType.Resolve();
				if (typeDefinition == null)
				{
					return null;
				}
				result = typeDefinition.FindMethodDeep(id, simple);
			}
			return result;
		}

		// Token: 0x06002E00 RID: 11776 RVA: 0x0009B864 File Offset: 0x00099A64
		[return: Nullable(2)]
		public static MethodInfo FindMethod(this Type type, string id, bool simple = true)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			Helpers.ThrowIfArgumentNull<string>(id, "id");
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (simple && !id.Contains(' ', StringComparison.Ordinal))
			{
				foreach (MethodInfo method in methods)
				{
					if (method.GetID(null, null, true, false, true) == id)
					{
						return method;
					}
				}
				foreach (MethodInfo method2 in methods)
				{
					if (method2.GetID(null, null, false, false, true) == id)
					{
						return method2;
					}
				}
			}
			foreach (MethodInfo method3 in methods)
			{
				if (method3.GetID(null, null, true, false, false) == id)
				{
					return method3;
				}
			}
			foreach (MethodInfo method4 in methods)
			{
				if (method4.GetID(null, null, false, false, false) == id)
				{
					return method4;
				}
			}
			return null;
		}

		// Token: 0x06002E01 RID: 11777 RVA: 0x0009B94C File Offset: 0x00099B4C
		[return: Nullable(2)]
		public static MethodInfo FindMethodDeep(this Type type, string id, bool simple = true)
		{
			MethodInfo result;
			if ((result = type.FindMethod(id, simple)) == null)
			{
				Type baseType = type.BaseType;
				if (baseType == null)
				{
					return null;
				}
				result = baseType.FindMethodDeep(id, simple);
			}
			return result;
		}

		// Token: 0x06002E02 RID: 11778 RVA: 0x0009B970 File Offset: 0x00099B70
		[return: Nullable(2)]
		public static PropertyDefinition FindProperty(this TypeDefinition type, string name)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			foreach (PropertyDefinition prop in type.Properties)
			{
				if (prop.Name == name)
				{
					return prop;
				}
			}
			return null;
		}

		// Token: 0x06002E03 RID: 11779 RVA: 0x0009B9DC File Offset: 0x00099BDC
		[return: Nullable(2)]
		public static PropertyDefinition FindPropertyDeep(this TypeDefinition type, string name)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			PropertyDefinition result;
			if ((result = type.FindProperty(name)) == null)
			{
				TypeReference baseType = type.BaseType;
				if (baseType == null)
				{
					return null;
				}
				TypeDefinition typeDefinition = baseType.Resolve();
				if (typeDefinition == null)
				{
					return null;
				}
				result = typeDefinition.FindPropertyDeep(name);
			}
			return result;
		}

		// Token: 0x06002E04 RID: 11780 RVA: 0x0009BA14 File Offset: 0x00099C14
		[return: Nullable(2)]
		public static FieldDefinition FindField(this TypeDefinition type, string name)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			foreach (FieldDefinition field in type.Fields)
			{
				if (field.Name == name)
				{
					return field;
				}
			}
			return null;
		}

		// Token: 0x06002E05 RID: 11781 RVA: 0x0009BA80 File Offset: 0x00099C80
		[return: Nullable(2)]
		public static FieldDefinition FindFieldDeep(this TypeDefinition type, string name)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			FieldDefinition result;
			if ((result = type.FindField(name)) == null)
			{
				TypeReference baseType = type.BaseType;
				if (baseType == null)
				{
					return null;
				}
				TypeDefinition typeDefinition = baseType.Resolve();
				if (typeDefinition == null)
				{
					return null;
				}
				result = typeDefinition.FindFieldDeep(name);
			}
			return result;
		}

		// Token: 0x06002E06 RID: 11782 RVA: 0x0009BAB8 File Offset: 0x00099CB8
		[return: Nullable(2)]
		public static EventDefinition FindEvent(this TypeDefinition type, string name)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			foreach (EventDefinition eventDef in type.Events)
			{
				if (eventDef.Name == name)
				{
					return eventDef;
				}
			}
			return null;
		}

		// Token: 0x06002E07 RID: 11783 RVA: 0x0009BB24 File Offset: 0x00099D24
		[return: Nullable(2)]
		public static EventDefinition FindEventDeep(this TypeDefinition type, string name)
		{
			Helpers.ThrowIfArgumentNull<TypeDefinition>(type, "type");
			EventDefinition result;
			if ((result = type.FindEvent(name)) == null)
			{
				TypeReference baseType = type.BaseType;
				if (baseType == null)
				{
					return null;
				}
				TypeDefinition typeDefinition = baseType.Resolve();
				if (typeDefinition == null)
				{
					return null;
				}
				result = typeDefinition.FindEventDeep(name);
			}
			return result;
		}

		// Token: 0x06002E08 RID: 11784 RVA: 0x0009BB5C File Offset: 0x00099D5C
		public static string GetID(this MethodReference method, [Nullable(2)] string name = null, [Nullable(2)] string type = null, bool withType = true, bool simple = false)
		{
			Helpers.ThrowIfArgumentNull<MethodReference>(method, "method");
			StringBuilder builder = new StringBuilder();
			if (simple)
			{
				if (withType && (type != null || method.DeclaringType != null))
				{
					builder.Append(type ?? method.DeclaringType.GetPatchFullName()).Append("::");
				}
				builder.Append(name ?? method.Name);
				return builder.ToString();
			}
			builder.Append(method.ReturnType.GetPatchFullName()).Append(' ');
			if (withType && (type != null || method.DeclaringType != null))
			{
				builder.Append(type ?? method.DeclaringType.GetPatchFullName()).Append("::");
			}
			builder.Append(name ?? method.Name);
			GenericInstanceMethod gim = method as GenericInstanceMethod;
			if (gim != null && gim.GenericArguments.Count != 0)
			{
				builder.Append('<');
				Collection<TypeReference> arguments = gim.GenericArguments;
				for (int i = 0; i < arguments.Count; i++)
				{
					if (i > 0)
					{
						builder.Append(',');
					}
					builder.Append(arguments[i].GetPatchFullName());
				}
				builder.Append('>');
			}
			else if (method.GenericParameters.Count != 0)
			{
				builder.Append('<');
				Collection<GenericParameter> arguments2 = method.GenericParameters;
				for (int j = 0; j < arguments2.Count; j++)
				{
					if (j > 0)
					{
						builder.Append(',');
					}
					builder.Append(arguments2[j].Name);
				}
				builder.Append('>');
			}
			builder.Append('(');
			if (method.HasParameters)
			{
				Collection<ParameterDefinition> parameters = method.Parameters;
				for (int k = 0; k < parameters.Count; k++)
				{
					ParameterDefinition parameter = parameters[k];
					if (k > 0)
					{
						builder.Append(',');
					}
					if (parameter.ParameterType.IsSentinel)
					{
						builder.Append("...,");
					}
					builder.Append(parameter.ParameterType.GetPatchFullName());
				}
			}
			builder.Append(')');
			return builder.ToString();
		}

		// Token: 0x06002E09 RID: 11785 RVA: 0x0009BD68 File Offset: 0x00099F68
		public static string GetID(this Mono.Cecil.CallSite method)
		{
			Helpers.ThrowIfArgumentNull<Mono.Cecil.CallSite>(method, "method");
			StringBuilder builder = new StringBuilder();
			builder.Append(method.ReturnType.GetPatchFullName()).Append(' ');
			builder.Append('(');
			if (method.HasParameters)
			{
				Collection<ParameterDefinition> parameters = method.Parameters;
				for (int i = 0; i < parameters.Count; i++)
				{
					ParameterDefinition parameter = parameters[i];
					if (i > 0)
					{
						builder.Append(',');
					}
					if (parameter.ParameterType.IsSentinel)
					{
						builder.Append("...,");
					}
					builder.Append(parameter.ParameterType.GetPatchFullName());
				}
			}
			builder.Append(')');
			return builder.ToString();
		}

		// Token: 0x06002E0A RID: 11786 RVA: 0x0009BE18 File Offset: 0x0009A018
		public static string GetID(this MethodBase method, [Nullable(2)] string name = null, [Nullable(2)] string type = null, bool withType = true, bool proxyMethod = false, bool simple = false)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			for (;;)
			{
				MethodInfo mi = method as MethodInfo;
				if (mi == null || !method.IsGenericMethod || method.IsGenericMethodDefinition)
				{
					break;
				}
				method = mi.GetGenericMethodDefinition();
			}
			StringBuilder builder = new StringBuilder();
			if (simple)
			{
				if (withType && (type != null || method.DeclaringType != null))
				{
					builder.Append(type ?? method.DeclaringType.FullName).Append("::");
				}
				builder.Append(name ?? method.Name);
				return builder.ToString();
			}
			StringBuilder stringBuilder = builder;
			MethodInfo methodInfo = method as MethodInfo;
			string text;
			if (methodInfo == null)
			{
				text = null;
			}
			else
			{
				Type returnType = methodInfo.ReturnType;
				text = ((returnType != null) ? returnType.FullName : null);
			}
			stringBuilder.Append(text ?? "System.Void").Append(' ');
			if (withType && (type != null || method.DeclaringType != null))
			{
				StringBuilder stringBuilder2 = builder;
				string value = type;
				if (type == null)
				{
					string fullName = method.DeclaringType.FullName;
					value = ((fullName != null) ? fullName.Replace("+", "/", StringComparison.Ordinal) : null);
				}
				stringBuilder2.Append(value).Append("::");
			}
			builder.Append(name ?? method.Name);
			if (method.ContainsGenericParameters)
			{
				builder.Append('<');
				Type[] arguments = method.GetGenericArguments();
				for (int i = 0; i < arguments.Length; i++)
				{
					if (i > 0)
					{
						builder.Append(',');
					}
					builder.Append(arguments[i].Name);
				}
				builder.Append('>');
			}
			builder.Append('(');
			ParameterInfo[] parameters = method.GetParameters();
			for (int j = ((proxyMethod > false) ? 1 : 0); j < parameters.Length; j++)
			{
				ParameterInfo parameter = parameters[j];
				if (j > ((proxyMethod > false) ? 1 : 0))
				{
					builder.Append(',');
				}
				bool defined;
				try
				{
					defined = parameter.GetCustomAttributes(Extensions.t_ParamArrayAttribute, false).Length != 0;
				}
				catch (NotSupportedException)
				{
					defined = false;
				}
				if (defined)
				{
					builder.Append("...,");
				}
				builder.Append(parameter.ParameterType.FullName);
			}
			builder.Append(')');
			return builder.ToString();
		}

		// Token: 0x06002E0B RID: 11787 RVA: 0x0009C02C File Offset: 0x0009A22C
		public static string GetPatchName(this MemberReference mr)
		{
			Helpers.ThrowIfArgumentNull<MemberReference>(mr, "mr");
			Mono.Cecil.ICustomAttributeProvider customAttributeProvider = mr as Mono.Cecil.ICustomAttributeProvider;
			return ((customAttributeProvider != null) ? customAttributeProvider.GetPatchName() : null) ?? mr.Name;
		}

		// Token: 0x06002E0C RID: 11788 RVA: 0x0009C055 File Offset: 0x0009A255
		public static string GetPatchFullName(this MemberReference mr)
		{
			Helpers.ThrowIfArgumentNull<MemberReference>(mr, "mr");
			Mono.Cecil.ICustomAttributeProvider customAttributeProvider = mr as Mono.Cecil.ICustomAttributeProvider;
			return ((customAttributeProvider != null) ? customAttributeProvider.GetPatchFullName(mr) : null) ?? mr.FullName;
		}

		// Token: 0x06002E0D RID: 11789 RVA: 0x0009C080 File Offset: 0x0009A280
		private static string GetPatchName(this Mono.Cecil.ICustomAttributeProvider cap)
		{
			Helpers.ThrowIfArgumentNull<Mono.Cecil.ICustomAttributeProvider>(cap, "cap");
			CustomAttribute patchAttrib = cap.GetCustomAttribute("MonoMod.MonoModPatch");
			string name;
			if (patchAttrib != null)
			{
				name = (string)patchAttrib.ConstructorArguments[0].Value;
				int dotIndex = name.LastIndexOf('.');
				if (dotIndex != -1 && dotIndex != name.Length - 1)
				{
					name = name.Substring(dotIndex + 1);
				}
				return name;
			}
			name = ((MemberReference)cap).Name;
			if (!name.StartsWith("patch_", StringComparison.Ordinal))
			{
				return name;
			}
			return name.Substring(6);
		}

		// Token: 0x06002E0E RID: 11790 RVA: 0x0009C108 File Offset: 0x0009A308
		private static string GetPatchFullName(this Mono.Cecil.ICustomAttributeProvider cap, MemberReference mr)
		{
			Helpers.ThrowIfArgumentNull<Mono.Cecil.ICustomAttributeProvider>(cap, "cap");
			Helpers.ThrowIfArgumentNull<MemberReference>(mr, "mr");
			TypeReference type = cap as TypeReference;
			if (type != null)
			{
				CustomAttribute patchAttrib = cap.GetCustomAttribute("MonoMod.MonoModPatch");
				string name;
				if (patchAttrib != null)
				{
					name = (string)patchAttrib.ConstructorArguments[0].Value;
				}
				else
				{
					name = ((MemberReference)cap).Name;
					name = (name.StartsWith("patch_", StringComparison.Ordinal) ? name.Substring(6) : name);
				}
				if (name.StartsWith("global::", StringComparison.Ordinal))
				{
					name = name.Substring(8);
				}
				else if (!name.Contains('.', StringComparison.Ordinal) && !name.Contains('/', StringComparison.Ordinal))
				{
					if (!string.IsNullOrEmpty(type.Namespace))
					{
						name = type.Namespace + "." + name;
					}
					else if (type.IsNested)
					{
						name = type.DeclaringType.GetPatchFullName() + "/" + name;
					}
				}
				TypeSpecification specification = mr as TypeSpecification;
				if (specification != null)
				{
					List<TypeSpecification> formats = new List<TypeSpecification>();
					TypeSpecification ts = specification;
					do
					{
						formats.Add(ts);
					}
					while ((ts = ts.ElementType as TypeSpecification) != null);
					StringBuilder builder = new StringBuilder(name.Length + formats.Count * 4);
					builder.Append(name);
					for (int formati = formats.Count - 1; formati > -1; formati--)
					{
						ts = formats[formati];
						if (ts.IsByReference)
						{
							builder.Append('&');
						}
						else if (ts.IsPointer)
						{
							builder.Append('*');
						}
						else if (!ts.IsPinned && !ts.IsSentinel)
						{
							if (ts.IsArray)
							{
								ArrayType array = (ArrayType)ts;
								if (array.IsVector)
								{
									builder.Append("[]");
								}
								else
								{
									builder.Append('[');
									for (int i = 0; i < array.Dimensions.Count; i++)
									{
										if (i > 0)
										{
											builder.Append(',');
										}
										builder.Append(array.Dimensions[i].ToString());
									}
									builder.Append(']');
								}
							}
							else if (ts.IsRequiredModifier)
							{
								builder.Append("modreq(").Append(((RequiredModifierType)ts).ModifierType).Append(')');
							}
							else if (ts.IsOptionalModifier)
							{
								builder.Append("modopt(").Append(((OptionalModifierType)ts).ModifierType).Append(')');
							}
							else if (ts.IsGenericInstance)
							{
								GenericInstanceType gen = (GenericInstanceType)ts;
								builder.Append('<');
								for (int j = 0; j < gen.GenericArguments.Count; j++)
								{
									if (j > 0)
									{
										builder.Append(',');
									}
									builder.Append(gen.GenericArguments[j].GetPatchFullName());
								}
								builder.Append('>');
							}
							else
							{
								if (!ts.IsFunctionPointer)
								{
									DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
									defaultInterpolatedStringHandler.AppendLiteral("MonoMod can't handle TypeSpecification: ");
									defaultInterpolatedStringHandler.AppendFormatted(type.FullName);
									defaultInterpolatedStringHandler.AppendLiteral(" (");
									defaultInterpolatedStringHandler.AppendFormatted<Type>(type.GetType());
									defaultInterpolatedStringHandler.AppendLiteral(")");
									throw new NotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
								}
								FunctionPointerType fpt = (FunctionPointerType)ts;
								builder.Append(' ').Append(fpt.ReturnType.GetPatchFullName()).Append(" *(");
								if (fpt.HasParameters)
								{
									for (int k = 0; k < fpt.Parameters.Count; k++)
									{
										ParameterDefinition parameter = fpt.Parameters[k];
										if (k > 0)
										{
											builder.Append(',');
										}
										if (parameter.ParameterType.IsSentinel)
										{
											builder.Append("...,");
										}
										builder.Append(parameter.ParameterType.FullName);
									}
								}
								builder.Append(')');
							}
						}
					}
					name = builder.ToString();
				}
				return name;
			}
			FieldReference field = cap as FieldReference;
			if (field != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(3, 3);
				defaultInterpolatedStringHandler2.AppendFormatted(field.FieldType.GetPatchFullName());
				defaultInterpolatedStringHandler2.AppendLiteral(" ");
				defaultInterpolatedStringHandler2.AppendFormatted(field.DeclaringType.GetPatchFullName());
				defaultInterpolatedStringHandler2.AppendLiteral("::");
				defaultInterpolatedStringHandler2.AppendFormatted(cap.GetPatchName());
				return defaultInterpolatedStringHandler2.ToStringAndClear();
			}
			if (cap is MethodReference)
			{
				throw new InvalidOperationException("GetPatchFullName not supported on MethodReferences - use GetID instead");
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(39, 1);
			defaultInterpolatedStringHandler3.AppendLiteral("GetPatchFullName not supported on type ");
			defaultInterpolatedStringHandler3.AppendFormatted<Type>(cap.GetType());
			throw new InvalidOperationException(defaultInterpolatedStringHandler3.ToStringAndClear());
		}

		// Token: 0x06002E0F RID: 11791 RVA: 0x0009C5E8 File Offset: 0x0009A7E8
		[NullableContext(2)]
		[return: NotNullIfNotNull("o")]
		public static MethodDefinition Clone(this MethodDefinition o, MethodDefinition c = null)
		{
			if (o == null)
			{
				return null;
			}
			if (c == null)
			{
				c = new MethodDefinition(o.Name, o.Attributes, o.ReturnType);
			}
			c.Name = o.Name;
			c.Attributes = o.Attributes;
			c.ReturnType = o.ReturnType;
			c.DeclaringType = o.DeclaringType;
			c.MetadataToken = c.MetadataToken;
			MethodDefinition methodDefinition = c;
			Mono.Cecil.Cil.MethodBody body = o.Body;
			methodDefinition.Body = ((body != null) ? body.Clone(c) : null);
			c.Attributes = o.Attributes;
			c.ImplAttributes = o.ImplAttributes;
			c.PInvokeInfo = o.PInvokeInfo;
			c.IsPreserveSig = o.IsPreserveSig;
			c.IsPInvokeImpl = o.IsPInvokeImpl;
			foreach (GenericParameter genParam in o.GenericParameters)
			{
				c.GenericParameters.Add(genParam.Clone());
			}
			foreach (ParameterDefinition param in o.Parameters)
			{
				c.Parameters.Add(param.Clone());
			}
			foreach (CustomAttribute attrib in o.CustomAttributes)
			{
				c.CustomAttributes.Add(attrib.Clone());
			}
			foreach (MethodReference @override in o.Overrides)
			{
				c.Overrides.Add(@override);
			}
			if (c.Body != null)
			{
				foreach (Instruction ci in c.Body.Instructions)
				{
					GenericParameter genParam2 = ci.Operand as GenericParameter;
					int foundIndex;
					if (genParam2 != null && (foundIndex = o.GenericParameters.IndexOf(genParam2)) != -1)
					{
						ci.Operand = c.GenericParameters[foundIndex];
					}
					else
					{
						ParameterDefinition param2 = ci.Operand as ParameterDefinition;
						if (param2 != null && (foundIndex = o.Parameters.IndexOf(param2)) != -1)
						{
							ci.Operand = c.Parameters[foundIndex];
						}
					}
				}
			}
			return c;
		}

		// Token: 0x06002E10 RID: 11792 RVA: 0x0009C8A4 File Offset: 0x0009AAA4
		[NullableContext(2)]
		[return: NotNullIfNotNull("bo")]
		public static Mono.Cecil.Cil.MethodBody Clone(this Mono.Cecil.Cil.MethodBody bo, [Nullable(1)] MethodDefinition m)
		{
			Helpers.ThrowIfArgumentNull<MethodDefinition>(m, "m");
			if (bo == null)
			{
				return null;
			}
			Mono.Cecil.Cil.MethodBody bc = new Mono.Cecil.Cil.MethodBody(m);
			bc.MaxStackSize = bo.MaxStackSize;
			bc.InitLocals = bo.InitLocals;
			bc.LocalVarToken = bo.LocalVarToken;
			bc.Instructions.AddRange(bo.Instructions.Select(delegate(Instruction o)
			{
				Instruction instruction2 = Instruction.Create(Mono.Cecil.Cil.OpCodes.Nop);
				instruction2.OpCode = o.OpCode;
				instruction2.Operand = o.Operand;
				instruction2.Offset = o.Offset;
				return instruction2;
			}));
			bc.ExceptionHandlers.AddRange(from o in bo.ExceptionHandlers
				select new Mono.Cecil.Cil.ExceptionHandler(o.HandlerType)
				{
					TryStart = ((o.TryStart == null) ? null : bc.Instructions[bo.Instructions.IndexOf(o.TryStart)]),
					TryEnd = ((o.TryEnd == null) ? null : bc.Instructions[bo.Instructions.IndexOf(o.TryEnd)]),
					FilterStart = ((o.FilterStart == null) ? null : bc.Instructions[bo.Instructions.IndexOf(o.FilterStart)]),
					HandlerStart = ((o.HandlerStart == null) ? null : bc.Instructions[bo.Instructions.IndexOf(o.HandlerStart)]),
					HandlerEnd = ((o.HandlerEnd == null) ? null : bc.Instructions[bo.Instructions.IndexOf(o.HandlerEnd)]),
					CatchType = o.CatchType
				});
			bc.Variables.AddRange(from o in bo.Variables
				select new VariableDefinition(o.VariableType));
			Func<InstructionOffset, InstructionOffset> <>9__6;
			Func<InstructionOffset, InstructionOffset> <>9__7;
			Func<StateMachineScope, StateMachineScope> <>9__8;
			m.CustomDebugInformations.AddRange(bo.Method.CustomDebugInformations.Select(delegate(CustomDebugInformation o)
			{
				AsyncMethodBodyDebugInformation ao = o as AsyncMethodBodyDebugInformation;
				if (ao != null)
				{
					AsyncMethodBodyDebugInformation c2 = new AsyncMethodBodyDebugInformation();
					if (ao.CatchHandler.Offset >= 0)
					{
						c2.CatchHandler = (ao.CatchHandler.IsEndOfMethod ? default(InstructionOffset) : new InstructionOffset(base.<Clone>g__ResolveInstrOff|3(ao.CatchHandler.Offset)));
					}
					Collection<InstructionOffset> yields = c2.Yields;
					IEnumerable<InstructionOffset> yields2 = ao.Yields;
					Func<InstructionOffset, InstructionOffset> selector2;
					if ((selector2 = <>9__6) == null)
					{
						selector2 = (<>9__6 = delegate(InstructionOffset off)
						{
							if (!off.IsEndOfMethod)
							{
								return new InstructionOffset(base.<Clone>g__ResolveInstrOff|3(off.Offset));
							}
							return default(InstructionOffset);
						});
					}
					yields.AddRange(yields2.Select(selector2));
					Collection<InstructionOffset> resumes = c2.Resumes;
					IEnumerable<InstructionOffset> resumes2 = ao.Resumes;
					Func<InstructionOffset, InstructionOffset> selector3;
					if ((selector3 = <>9__7) == null)
					{
						selector3 = (<>9__7 = delegate(InstructionOffset off)
						{
							if (!off.IsEndOfMethod)
							{
								return new InstructionOffset(base.<Clone>g__ResolveInstrOff|3(off.Offset));
							}
							return default(InstructionOffset);
						});
					}
					resumes.AddRange(resumes2.Select(selector3));
					c2.ResumeMethods.AddRange(ao.ResumeMethods);
					return c2;
				}
				StateMachineScopeDebugInformation so = o as StateMachineScopeDebugInformation;
				if (so != null)
				{
					StateMachineScopeDebugInformation stateMachineScopeDebugInformation = new StateMachineScopeDebugInformation();
					Collection<StateMachineScope> scopes = stateMachineScopeDebugInformation.Scopes;
					IEnumerable<StateMachineScope> scopes2 = so.Scopes;
					Func<StateMachineScope, StateMachineScope> selector4;
					if ((selector4 = <>9__8) == null)
					{
						selector4 = (<>9__8 = (StateMachineScope s) => new StateMachineScope(base.<Clone>g__ResolveInstrOff|3(s.Start.Offset), s.End.IsEndOfMethod ? null : base.<Clone>g__ResolveInstrOff|3(s.End.Offset)));
					}
					scopes.AddRange(scopes2.Select(selector4));
					return stateMachineScopeDebugInformation;
				}
				return o;
			}));
			m.DebugInformation.SequencePoints.AddRange(from o in bo.Method.DebugInformation.SequencePoints
				select new SequencePoint(base.<Clone>g__ResolveInstrOff|3(o.Offset), o.Document)
				{
					StartLine = o.StartLine,
					StartColumn = o.StartColumn,
					EndLine = o.EndLine,
					EndColumn = o.EndColumn
				});
			Func<Instruction, Instruction> <>9__9;
			foreach (Instruction c in bc.Instructions)
			{
				Instruction target = c.Operand as Instruction;
				if (target != null)
				{
					c.Operand = bc.Instructions[bo.Instructions.IndexOf(target)];
				}
				else
				{
					Instruction[] targets = c.Operand as Instruction[];
					if (targets != null)
					{
						Instruction instruction = c;
						IEnumerable<Instruction> source = targets;
						Func<Instruction, Instruction> selector;
						if ((selector = <>9__9) == null)
						{
							selector = (<>9__9 = (Instruction i) => bc.Instructions[bo.Instructions.IndexOf(i)]);
						}
						instruction.Operand = source.Select(selector).ToArray<Instruction>();
					}
					else
					{
						VariableDefinition vardef = c.Operand as VariableDefinition;
						if (vardef != null)
						{
							c.Operand = bc.Variables[vardef.Index];
						}
					}
				}
			}
			return bc;
		}

		// Token: 0x06002E11 RID: 11793 RVA: 0x0009CB2C File Offset: 0x0009AD2C
		public static GenericParameter Update(this GenericParameter param, int position, GenericParameterType type)
		{
			Extensions.f_GenericParameter_position.SetValue(param, position);
			Extensions.f_GenericParameter_type.SetValue(param, type);
			return param;
		}

		// Token: 0x06002E12 RID: 11794 RVA: 0x0009CB54 File Offset: 0x0009AD54
		[return: Nullable(2)]
		public static GenericParameter ResolveGenericParameter(this IGenericParameterProvider provider, GenericParameter orig)
		{
			Helpers.ThrowIfArgumentNull<IGenericParameterProvider>(provider, "provider");
			Helpers.ThrowIfArgumentNull<GenericParameter>(orig, "orig");
			GenericParameter genericParam = provider as GenericParameter;
			if (genericParam != null && genericParam.Name == orig.Name)
			{
				return genericParam;
			}
			foreach (GenericParameter param in provider.GenericParameters)
			{
				if (param.Name == orig.Name)
				{
					return param;
				}
			}
			int index = orig.Position;
			if (provider is MethodReference && orig.DeclaringMethod != null)
			{
				if (index < provider.GenericParameters.Count)
				{
					return provider.GenericParameters[index];
				}
				return orig.Clone().Update(index, GenericParameterType.Method);
			}
			else
			{
				if (!(provider is TypeReference) || orig.DeclaringType == null)
				{
					TypeSpecification typeSpecification = provider as TypeSpecification;
					GenericParameter result;
					if ((result = ((typeSpecification != null) ? typeSpecification.ElementType.ResolveGenericParameter(orig) : null)) == null)
					{
						MemberReference memberReference = provider as MemberReference;
						if (memberReference == null)
						{
							return null;
						}
						TypeReference declaringType = memberReference.DeclaringType;
						if (declaringType == null)
						{
							return null;
						}
						result = declaringType.ResolveGenericParameter(orig);
					}
					return result;
				}
				if (index < provider.GenericParameters.Count)
				{
					return provider.GenericParameters[index];
				}
				return orig.Clone().Update(index, GenericParameterType.Type);
			}
			GenericParameter result2;
			return result2;
		}

		// Token: 0x06002E13 RID: 11795 RVA: 0x0009CCA8 File Offset: 0x0009AEA8
		[return: Nullable(2)]
		[return: NotNullIfNotNull("mtp")]
		public static IMetadataTokenProvider Relink([Nullable(2)] this IMetadataTokenProvider mtp, Relinker relinker, IGenericParameterProvider context)
		{
			TypeReference tr = mtp as TypeReference;
			IMetadataTokenProvider result;
			if (tr == null)
			{
				GenericParameterConstraint constraint = mtp as GenericParameterConstraint;
				if (constraint == null)
				{
					MethodReference mr = mtp as MethodReference;
					if (mr == null)
					{
						FieldReference fr = mtp as FieldReference;
						if (fr == null)
						{
							ParameterDefinition pd = mtp as ParameterDefinition;
							if (pd == null)
							{
								Mono.Cecil.CallSite cs = mtp as Mono.Cecil.CallSite;
								if (cs == null)
								{
									if (mtp != null)
									{
										DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 1);
										defaultInterpolatedStringHandler.AppendLiteral("MonoMod can't handle metadata token providers of the type ");
										defaultInterpolatedStringHandler.AppendFormatted<Type>(mtp.GetType());
										throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
									}
									result = null;
								}
								else
								{
									result = cs.Relink(relinker, context);
								}
							}
							else
							{
								result = pd.Relink(relinker, context);
							}
						}
						else
						{
							result = fr.Relink(relinker, context);
						}
					}
					else
					{
						result = mr.Relink(relinker, context);
					}
				}
				else
				{
					result = constraint.Relink(relinker, context);
				}
			}
			else
			{
				result = tr.Relink(relinker, context);
			}
			return result;
		}

		// Token: 0x06002E14 RID: 11796 RVA: 0x0009CD7C File Offset: 0x0009AF7C
		[NullableContext(2)]
		[return: NotNullIfNotNull("type")]
		public static TypeReference Relink(this TypeReference type, [Nullable(1)] Relinker relinker, IGenericParameterProvider context)
		{
			if (type == null)
			{
				return null;
			}
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			TypeSpecification ts = type as TypeSpecification;
			if (ts != null)
			{
				TypeReference relinkedElem = ts.ElementType.Relink(relinker, context);
				if (type.IsSentinel)
				{
					return new SentinelType(relinkedElem);
				}
				if (type.IsByReference)
				{
					return new ByReferenceType(relinkedElem);
				}
				if (type.IsPointer)
				{
					return new PointerType(relinkedElem);
				}
				if (type.IsPinned)
				{
					return new PinnedType(relinkedElem);
				}
				if (type.IsArray)
				{
					ArrayType at = new ArrayType(relinkedElem, ((ArrayType)type).Rank);
					for (int i = 0; i < at.Rank; i++)
					{
						at.Dimensions[i] = ((ArrayType)type).Dimensions[i];
					}
					return at;
				}
				if (type.IsRequiredModifier)
				{
					return new RequiredModifierType(((RequiredModifierType)type).ModifierType.Relink(relinker, context), relinkedElem);
				}
				if (type.IsOptionalModifier)
				{
					return new OptionalModifierType(((OptionalModifierType)type).ModifierType.Relink(relinker, context), relinkedElem);
				}
				if (type.IsGenericInstance)
				{
					GenericInstanceType git = new GenericInstanceType(relinkedElem);
					foreach (TypeReference genArg in ((GenericInstanceType)type).GenericArguments)
					{
						git.GenericArguments.Add((genArg != null) ? genArg.Relink(relinker, context) : null);
					}
					return git;
				}
				if (type.IsFunctionPointer)
				{
					FunctionPointerType fp = (FunctionPointerType)type;
					fp.ReturnType = fp.ReturnType.Relink(relinker, context);
					for (int j = 0; j < fp.Parameters.Count; j++)
					{
						fp.Parameters[j].ParameterType = fp.Parameters[j].ParameterType.Relink(relinker, context);
					}
					return fp;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
				defaultInterpolatedStringHandler.AppendLiteral("MonoMod can't handle TypeSpecification: ");
				defaultInterpolatedStringHandler.AppendFormatted(type.FullName);
				defaultInterpolatedStringHandler.AppendLiteral(" (");
				defaultInterpolatedStringHandler.AppendFormatted<Type>(type.GetType());
				defaultInterpolatedStringHandler.AppendLiteral(")");
				throw new NotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				if (!type.IsGenericParameter || context == null)
				{
					return (TypeReference)relinker(type, context);
				}
				GenericParameter genericParameter = context.ResolveGenericParameter((GenericParameter)type);
				if (genericParameter == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(13, 3);
					defaultInterpolatedStringHandler2.AppendFormatted("MonoMod relinker failed finding");
					defaultInterpolatedStringHandler2.AppendLiteral(" ");
					defaultInterpolatedStringHandler2.AppendFormatted(type.FullName);
					defaultInterpolatedStringHandler2.AppendLiteral(" (context: ");
					defaultInterpolatedStringHandler2.AppendFormatted<IGenericParameterProvider>(context);
					defaultInterpolatedStringHandler2.AppendLiteral(")");
					throw new RelinkTargetNotFoundException(defaultInterpolatedStringHandler2.ToStringAndClear(), type, context);
				}
				GenericParameter genParam = genericParameter;
				for (int k = 0; k < genParam.Constraints.Count; k++)
				{
					if (!genParam.Constraints[k].GetConstraintType().IsGenericInstance)
					{
						genParam.Constraints[k] = genParam.Constraints[k].Relink(relinker, context);
					}
				}
				return genParam;
			}
		}

		// Token: 0x06002E15 RID: 11797 RVA: 0x0009D0A0 File Offset: 0x0009B2A0
		[return: Nullable(2)]
		[return: NotNullIfNotNull("constraint")]
		public static GenericParameterConstraint Relink([Nullable(2)] this GenericParameterConstraint constraint, Relinker relinker, IGenericParameterProvider context)
		{
			if (constraint == null)
			{
				return null;
			}
			GenericParameterConstraint relink = new GenericParameterConstraint(constraint.ConstraintType.Relink(relinker, context));
			foreach (CustomAttribute attrib in constraint.CustomAttributes)
			{
				relink.CustomAttributes.Add(attrib.Relink(relinker, context));
			}
			return relink;
		}

		// Token: 0x06002E16 RID: 11798 RVA: 0x0009D118 File Offset: 0x0009B318
		public static IMetadataTokenProvider Relink(this MethodReference method, Relinker relinker, IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<MethodReference>(method, "method");
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			if (method.IsGenericInstance)
			{
				GenericInstanceMethod genericInstanceMethod = (GenericInstanceMethod)method;
				GenericInstanceMethod gim = new GenericInstanceMethod((MethodReference)genericInstanceMethod.ElementMethod.Relink(relinker, context));
				foreach (TypeReference arg in genericInstanceMethod.GenericArguments)
				{
					gim.GenericArguments.Add(arg.Relink(relinker, context));
				}
				return (MethodReference)relinker(gim, context);
			}
			MethodReference relink = new MethodReference(method.Name, method.ReturnType, method.DeclaringType.Relink(relinker, context));
			relink.CallingConvention = method.CallingConvention;
			relink.ExplicitThis = method.ExplicitThis;
			relink.HasThis = method.HasThis;
			foreach (GenericParameter param in method.GenericParameters)
			{
				relink.GenericParameters.Add(param.Relink(relinker, context));
			}
			MethodReference methodReference = relink;
			TypeReference returnType = relink.ReturnType;
			methodReference.ReturnType = ((returnType != null) ? returnType.Relink(relinker, relink) : null);
			foreach (ParameterDefinition param2 in method.Parameters)
			{
				param2.ParameterType = param2.ParameterType.Relink(relinker, method);
				relink.Parameters.Add(param2);
			}
			return (MethodReference)relinker(relink, context);
		}

		// Token: 0x06002E17 RID: 11799 RVA: 0x0009D2DC File Offset: 0x0009B4DC
		public static Mono.Cecil.CallSite Relink(this Mono.Cecil.CallSite method, Relinker relinker, IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<Mono.Cecil.CallSite>(method, "method");
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			Mono.Cecil.CallSite relink = new Mono.Cecil.CallSite(method.ReturnType);
			relink.CallingConvention = method.CallingConvention;
			relink.ExplicitThis = method.ExplicitThis;
			relink.HasThis = method.HasThis;
			Mono.Cecil.CallSite callSite = relink;
			TypeReference returnType = relink.ReturnType;
			callSite.ReturnType = ((returnType != null) ? returnType.Relink(relinker, context) : null);
			foreach (ParameterDefinition param in method.Parameters)
			{
				param.ParameterType = param.ParameterType.Relink(relinker, context);
				relink.Parameters.Add(param);
			}
			return (Mono.Cecil.CallSite)relinker(relink, context);
		}

		// Token: 0x06002E18 RID: 11800 RVA: 0x0009D3B4 File Offset: 0x0009B5B4
		public static IMetadataTokenProvider Relink(this FieldReference field, Relinker relinker, IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<FieldReference>(field, "field");
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			TypeReference declaringType = field.DeclaringType.Relink(relinker, context);
			return relinker(new FieldReference(field.Name, field.FieldType.Relink(relinker, declaringType), declaringType), context);
		}

		// Token: 0x06002E19 RID: 11801 RVA: 0x0009D408 File Offset: 0x0009B608
		public static ParameterDefinition Relink(this ParameterDefinition param, Relinker relinker, IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<ParameterDefinition>(param, "param");
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			MethodReference methodReference = param.Method as MethodReference;
			param = ((methodReference != null) ? methodReference.Parameters[param.Index] : null) ?? param;
			ParameterDefinition newParam = new ParameterDefinition(param.Name, param.Attributes, param.ParameterType.Relink(relinker, context))
			{
				IsIn = param.IsIn,
				IsLcid = param.IsLcid,
				IsOptional = param.IsOptional,
				IsOut = param.IsOut,
				IsReturnValue = param.IsReturnValue,
				MarshalInfo = param.MarshalInfo
			};
			if (param.HasConstant)
			{
				newParam.Constant = param.Constant;
			}
			return newParam;
		}

		// Token: 0x06002E1A RID: 11802 RVA: 0x0009D4D0 File Offset: 0x0009B6D0
		public static ParameterDefinition Clone(this ParameterDefinition param)
		{
			Helpers.ThrowIfArgumentNull<ParameterDefinition>(param, "param");
			ParameterDefinition newParam = new ParameterDefinition(param.Name, param.Attributes, param.ParameterType)
			{
				IsIn = param.IsIn,
				IsLcid = param.IsLcid,
				IsOptional = param.IsOptional,
				IsOut = param.IsOut,
				IsReturnValue = param.IsReturnValue,
				MarshalInfo = param.MarshalInfo
			};
			if (param.HasConstant)
			{
				newParam.Constant = param.Constant;
			}
			foreach (CustomAttribute attrib in param.CustomAttributes)
			{
				newParam.CustomAttributes.Add(attrib.Clone());
			}
			return newParam;
		}

		// Token: 0x06002E1B RID: 11803 RVA: 0x0009D5B0 File Offset: 0x0009B7B0
		public static CustomAttribute Relink(this CustomAttribute attrib, Relinker relinker, IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<CustomAttribute>(attrib, "attrib");
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			CustomAttribute newAttrib = new CustomAttribute((MethodReference)attrib.Constructor.Relink(relinker, context));
			foreach (CustomAttributeArgument attribArg in attrib.ConstructorArguments)
			{
				newAttrib.ConstructorArguments.Add(new CustomAttributeArgument(attribArg.Type.Relink(relinker, context), attribArg.Value));
			}
			foreach (Mono.Cecil.CustomAttributeNamedArgument attribArg2 in attrib.Fields)
			{
				newAttrib.Fields.Add(new Mono.Cecil.CustomAttributeNamedArgument(attribArg2.Name, new CustomAttributeArgument(attribArg2.Argument.Type.Relink(relinker, context), attribArg2.Argument.Value)));
			}
			foreach (Mono.Cecil.CustomAttributeNamedArgument attribArg3 in attrib.Properties)
			{
				newAttrib.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument(attribArg3.Name, new CustomAttributeArgument(attribArg3.Argument.Type.Relink(relinker, context), attribArg3.Argument.Value)));
			}
			return newAttrib;
		}

		// Token: 0x06002E1C RID: 11804 RVA: 0x0009D750 File Offset: 0x0009B950
		public static CustomAttribute Clone(this CustomAttribute attrib)
		{
			Helpers.ThrowIfArgumentNull<CustomAttribute>(attrib, "attrib");
			CustomAttribute newAttrib = new CustomAttribute(attrib.Constructor);
			foreach (CustomAttributeArgument attribArg in attrib.ConstructorArguments)
			{
				newAttrib.ConstructorArguments.Add(new CustomAttributeArgument(attribArg.Type, attribArg.Value));
			}
			foreach (Mono.Cecil.CustomAttributeNamedArgument attribArg2 in attrib.Fields)
			{
				newAttrib.Fields.Add(new Mono.Cecil.CustomAttributeNamedArgument(attribArg2.Name, new CustomAttributeArgument(attribArg2.Argument.Type, attribArg2.Argument.Value)));
			}
			foreach (Mono.Cecil.CustomAttributeNamedArgument attribArg3 in attrib.Properties)
			{
				newAttrib.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument(attribArg3.Name, new CustomAttributeArgument(attribArg3.Argument.Type, attribArg3.Argument.Value)));
			}
			return newAttrib;
		}

		// Token: 0x06002E1D RID: 11805 RVA: 0x0009D8C4 File Offset: 0x0009BAC4
		public static GenericParameter Relink(this GenericParameter param, Relinker relinker, IGenericParameterProvider context)
		{
			Helpers.ThrowIfArgumentNull<GenericParameter>(param, "param");
			Helpers.ThrowIfArgumentNull<Relinker>(relinker, "relinker");
			GenericParameter newParam = new GenericParameter(param.Name, param.Owner)
			{
				Attributes = param.Attributes
			}.Update(param.Position, param.Type);
			foreach (CustomAttribute attr in param.CustomAttributes)
			{
				newParam.CustomAttributes.Add(attr.Relink(relinker, context));
			}
			foreach (GenericParameterConstraint constraint in param.Constraints)
			{
				newParam.Constraints.Add(constraint.Relink(relinker, context));
			}
			return newParam;
		}

		// Token: 0x06002E1E RID: 11806 RVA: 0x0009D9BC File Offset: 0x0009BBBC
		public static GenericParameter Clone(this GenericParameter param)
		{
			Helpers.ThrowIfArgumentNull<GenericParameter>(param, "param");
			GenericParameter newParam = new GenericParameter(param.Name, param.Owner)
			{
				Attributes = param.Attributes
			}.Update(param.Position, param.Type);
			foreach (CustomAttribute attr in param.CustomAttributes)
			{
				newParam.CustomAttributes.Add(attr.Clone());
			}
			foreach (GenericParameterConstraint constraint in param.Constraints)
			{
				newParam.Constraints.Add(constraint);
			}
			return newParam;
		}

		// Token: 0x06002E1F RID: 11807 RVA: 0x0009DAA0 File Offset: 0x0009BCA0
		public static int GetManagedSize(this Type t)
		{
			if (!Helpers.ThrowIfNull<Type>(t, "t").IsByRef && !t.IsPointer)
			{
				ConcurrentDictionary<Type, int> getManagedSizeCache = Extensions._GetManagedSizeCache;
				Type key = Helpers.ThrowIfNull<Type>(t, "t");
				Func<Type, int> valueFactory;
				if ((valueFactory = Extensions.<>O.<0>__ComputeManagedSize) == null)
				{
					valueFactory = (Extensions.<>O.<0>__ComputeManagedSize = new Func<Type, int>(Extensions.ComputeManagedSize));
				}
				return getManagedSizeCache.GetOrAdd(key, valueFactory);
			}
			return IntPtr.Size;
		}

		// Token: 0x06002E20 RID: 11808 RVA: 0x0009DB00 File Offset: 0x0009BD00
		private static int ComputeManagedSize(Type t)
		{
			MethodInfo szHelper = Extensions._GetManagedSizeHelper;
			if (szHelper == null)
			{
				szHelper = (Extensions._GetManagedSizeHelper = typeof(Unsafe).GetMethod("SizeOf"));
			}
			if (t.IsByRef || t.IsPointer || t.IsByRefLike())
			{
				return Extensions.GenerateAndInvokeSizeofHelper(t);
			}
			return szHelper.MakeGenericMethod(new Type[] { t }).CreateDelegate<Func<int>>()();
		}

		// Token: 0x06002E21 RID: 11809 RVA: 0x0009DB6C File Offset: 0x0009BD6C
		private static int GenerateAndInvokeSizeofHelper(Type t)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
			defaultInterpolatedStringHandler.AppendLiteral("SizeOf<");
			defaultInterpolatedStringHandler.AppendFormatted<Type>(t);
			defaultInterpolatedStringHandler.AppendLiteral(">");
			int result;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition(defaultInterpolatedStringHandler.ToStringAndClear(), typeof(int), new Type[0]))
			{
				ILProcessor il = dmd.GetILProcessor();
				il.Emit(Mono.Cecil.Cil.OpCodes.Sizeof, il.Import(t));
				il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
				result = (int)dmd.Generate().Invoke(null, null);
			}
			return result;
		}

		// Token: 0x06002E22 RID: 11810 RVA: 0x0009DC14 File Offset: 0x0009BE14
		public static Type GetThisParamType(this MethodBase method)
		{
			Type type = Helpers.ThrowIfNull<MethodBase>(method, "method").DeclaringType;
			if (type.IsValueType)
			{
				type = type.MakeByRefType();
			}
			return type;
		}

		// Token: 0x06002E23 RID: 11811 RVA: 0x0009DC44 File Offset: 0x0009BE44
		public static IntPtr GetLdftnPointer(this MethodBase m)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(m, "m");
			Func<IntPtr> func;
			if (Extensions._GetLdftnPointerCache.TryGetValue(m, out func))
			{
				return func();
			}
			FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(17, 1);
			formatInterpolatedStringHandler.AppendLiteral("GetLdftnPointer<");
			formatInterpolatedStringHandler.AppendFormatted<MethodBase>(m);
			formatInterpolatedStringHandler.AppendLiteral(">");
			IntPtr result;
			using (DynamicMethodDefinition dmd = new DynamicMethodDefinition(DebugFormatter.Format(ref formatInterpolatedStringHandler), typeof(IntPtr), Type.EmptyTypes))
			{
				ILProcessor ilprocessor = dmd.GetILProcessor();
				ilprocessor.Emit(Mono.Cecil.Cil.OpCodes.Ldftn, dmd.Definition.Module.ImportReference(m));
				ilprocessor.Emit(Mono.Cecil.Cil.OpCodes.Ret);
				Dictionary<MethodBase, Func<IntPtr>> getLdftnPointerCache = Extensions._GetLdftnPointerCache;
				lock (getLdftnPointerCache)
				{
					result = (Extensions._GetLdftnPointerCache[m] = dmd.Generate().CreateDelegate<Func<IntPtr>>())();
				}
			}
			return result;
		}

		// Token: 0x06002E24 RID: 11812 RVA: 0x0009DD4C File Offset: 0x0009BF4C
		public static string ToHexadecimalString(this byte[] data)
		{
			return BitConverter.ToString(data).Replace("-", string.Empty, StringComparison.Ordinal);
		}

		// Token: 0x06002E25 RID: 11813 RVA: 0x0009DD64 File Offset: 0x0009BF64
		[return: Nullable(2)]
		public static T InvokePassing<[Nullable(2)] T>(this MulticastDelegate md, T val, [Nullable(new byte[] { 1, 2 })] params object[] args)
		{
			if (md == null)
			{
				return val;
			}
			Helpers.ThrowIfArgumentNull<object[]>(args, "args");
			object[] args_ = new object[args.Length + 1];
			args_[0] = val;
			Array.Copy(args, 0, args_, 1, args.Length);
			Delegate[] ds = md.GetInvocationList();
			for (int i = 0; i < ds.Length; i++)
			{
				args_[0] = ds[i].DynamicInvoke(args_);
			}
			return (T)((object)args_[0]);
		}

		// Token: 0x06002E26 RID: 11814 RVA: 0x0009DDCC File Offset: 0x0009BFCC
		public static bool InvokeWhileTrue(this MulticastDelegate md, params object[] args)
		{
			if (md == null)
			{
				return true;
			}
			Helpers.ThrowIfArgumentNull<object[]>(args, "args");
			Delegate[] ds = md.GetInvocationList();
			for (int i = 0; i < ds.Length; i++)
			{
				if (!(bool)ds[i].DynamicInvoke(args))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002E27 RID: 11815 RVA: 0x0009DE14 File Offset: 0x0009C014
		public static bool InvokeWhileFalse(this MulticastDelegate md, params object[] args)
		{
			if (md == null)
			{
				return false;
			}
			Helpers.ThrowIfArgumentNull<object[]>(args, "args");
			Delegate[] ds = md.GetInvocationList();
			for (int i = 0; i < ds.Length; i++)
			{
				if ((bool)ds[i].DynamicInvoke(args))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002E28 RID: 11816 RVA: 0x0009DE5C File Offset: 0x0009C05C
		[return: Nullable(2)]
		public static T InvokeWhileNull<T>([Nullable(2)] this MulticastDelegate md, params object[] args) where T : class
		{
			if (md == null)
			{
				return default(T);
			}
			Helpers.ThrowIfArgumentNull<object[]>(args, "args");
			Delegate[] ds = md.GetInvocationList();
			for (int i = 0; i < ds.Length; i++)
			{
				T result = (T)((object)ds[i].DynamicInvoke(args));
				if (result != null)
				{
					return result;
				}
			}
			return default(T);
		}

		// Token: 0x06002E29 RID: 11817 RVA: 0x0009DEB8 File Offset: 0x0009C0B8
		public static string SpacedPascalCase(this string input)
		{
			Helpers.ThrowIfArgumentNull<string>(input, "input");
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < input.Length; i++)
			{
				char c = input[i];
				if (i > 0 && char.IsUpper(c))
				{
					builder.Append(' ');
				}
				builder.Append(c);
			}
			return builder.ToString();
		}

		// Token: 0x06002E2A RID: 11818 RVA: 0x0009DF14 File Offset: 0x0009C114
		public static string ReadNullTerminatedString(this BinaryReader stream)
		{
			Helpers.ThrowIfArgumentNull<BinaryReader>(stream, "stream");
			string text = "";
			char c;
			while ((c = stream.ReadChar()) != '\0')
			{
				text += c.ToString();
			}
			return text;
		}

		// Token: 0x06002E2B RID: 11819 RVA: 0x0009DF50 File Offset: 0x0009C150
		public static void WriteNullTerminatedString(this BinaryWriter stream, string text)
		{
			Helpers.ThrowIfArgumentNull<BinaryWriter>(stream, "stream");
			Helpers.ThrowIfArgumentNull<string>(text, "text");
			if (text != null)
			{
				foreach (char c in text)
				{
					stream.Write(c);
				}
			}
			stream.Write('\0');
		}

		// Token: 0x06002E2C RID: 11820 RVA: 0x0009DF9D File Offset: 0x0009C19D
		private static MethodBase GetRealMethod(MethodBase method)
		{
			if (Extensions.RTDynamicMethod_m_owner != null && method.GetType() == Extensions.RTDynamicMethod)
			{
				return (MethodBase)Extensions.RTDynamicMethod_m_owner.GetValue(method);
			}
			return method;
		}

		// Token: 0x06002E2D RID: 11821 RVA: 0x0009DFCA File Offset: 0x0009C1CA
		public static T CastDelegate<[Nullable(0)] T>(this Delegate source) where T : Delegate
		{
			return (T)((object)Helpers.ThrowIfNull<Delegate>(source, "source").CastDelegate(typeof(T)));
		}

		// Token: 0x06002E2E RID: 11822 RVA: 0x0009DFEC File Offset: 0x0009C1EC
		[NullableContext(2)]
		[return: NotNullIfNotNull("source")]
		public static Delegate CastDelegate(this Delegate source, [Nullable(1)] Type type)
		{
			if (source == null)
			{
				return null;
			}
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			if (type.IsAssignableFrom(source.GetType()))
			{
				return source;
			}
			Delegate[] delegates = source.GetInvocationList();
			if (delegates.Length == 1)
			{
				return Extensions.GetRealMethod(delegates[0].Method).CreateDelegate(type, delegates[0].Target);
			}
			Delegate[] delegatesDest = new Delegate[delegates.Length];
			for (int i = 0; i < delegates.Length; i++)
			{
				delegatesDest[i] = Extensions.GetRealMethod(delegates[i].Method).CreateDelegate(type, delegates[i].Target);
			}
			return Delegate.Combine(delegatesDest);
		}

		// Token: 0x06002E2F RID: 11823 RVA: 0x0009E080 File Offset: 0x0009C280
		public static bool TryCastDelegate<[Nullable(0)] T>(this Delegate source, [MaybeNullWhen(false)] out T result) where T : Delegate
		{
			if (source == null)
			{
				result = default(T);
				return false;
			}
			T cast = source as T;
			if (cast != null)
			{
				result = cast;
				return true;
			}
			Delegate resultDel;
			bool result2 = source.TryCastDelegate(typeof(T), out resultDel);
			result = (T)((object)resultDel);
			return result2;
		}

		// Token: 0x06002E30 RID: 11824 RVA: 0x0009E0D4 File Offset: 0x0009C2D4
		public static bool TryCastDelegate(this Delegate source, Type type, [Nullable(2)] [MaybeNullWhen(false)] out Delegate result)
		{
			result = null;
			if (source == null)
			{
				return false;
			}
			bool result2;
			try
			{
				result = source.CastDelegate(type);
				result2 = true;
			}
			catch (Exception e)
			{
				bool flag;
				MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new MMDbgLog.DebugLogWarningStringHandler(43, 3, ref flag);
				if (flag)
				{
					debugLogWarningStringHandler.AppendLiteral("Exception thrown in TryCastDelegate(");
					debugLogWarningStringHandler.AppendFormatted<Type>(source.GetType());
					debugLogWarningStringHandler.AppendLiteral(" -> ");
					debugLogWarningStringHandler.AppendFormatted<Type>(type);
					debugLogWarningStringHandler.AppendLiteral("): ");
					debugLogWarningStringHandler.AppendFormatted<Exception>(e);
				}
				MMDbgLog.Warning(ref debugLogWarningStringHandler);
				result2 = false;
			}
			return result2;
		}

		// Token: 0x06002E31 RID: 11825 RVA: 0x0009E164 File Offset: 0x0009C364
		[return: Nullable(2)]
		public static MethodInfo GetStateMachineTarget(this MethodInfo method)
		{
			if (Extensions.p_StateMachineType == null || Extensions.t_StateMachineAttribute == null)
			{
				return null;
			}
			Helpers.ThrowIfArgumentNull<MethodInfo>(method, "method");
			object[] customAttributes = method.GetCustomAttributes(false);
			int i = 0;
			while (i < customAttributes.Length)
			{
				Attribute attrib = (Attribute)customAttributes[i];
				if (Extensions.t_StateMachineAttribute.IsCompatible(attrib.GetType()))
				{
					Type type = Extensions.p_StateMachineType.GetValue(attrib, null) as Type;
					if (type == null)
					{
						return null;
					}
					return type.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				else
				{
					i++;
				}
			}
			return null;
		}

		// Token: 0x06002E32 RID: 11826 RVA: 0x0009E1E2 File Offset: 0x0009C3E2
		public static MethodBase GetActualGenericMethodDefinition(this MethodInfo method)
		{
			Helpers.ThrowIfArgumentNull<MethodInfo>(method, "method");
			return (method.IsGenericMethod ? method.GetGenericMethodDefinition() : method).GetUnfilledMethodOnGenericType();
		}

		// Token: 0x06002E33 RID: 11827 RVA: 0x0009E208 File Offset: 0x0009C408
		public static MethodBase GetUnfilledMethodOnGenericType(this MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			if (method.DeclaringType != null && method.DeclaringType.IsGenericType)
			{
				Type type = method.DeclaringType.GetGenericTypeDefinition();
				method = MethodBase.GetMethodFromHandle(method.MethodHandle, type.TypeHandle);
			}
			return method;
		}

		// Token: 0x06002E34 RID: 11828 RVA: 0x0009E25B File Offset: 0x0009C45B
		public static bool Is(this MemberReference member, string fullName)
		{
			Helpers.ThrowIfArgumentNull<string>(fullName, "fullName");
			return member != null && member.FullName.Replace("+", "/", StringComparison.Ordinal) == fullName.Replace("+", "/", StringComparison.Ordinal);
		}

		// Token: 0x06002E35 RID: 11829 RVA: 0x0009E29C File Offset: 0x0009C49C
		public static bool Is(this MemberReference member, string typeFullName, string name)
		{
			Helpers.ThrowIfArgumentNull<string>(typeFullName, "typeFullName");
			Helpers.ThrowIfArgumentNull<string>(name, "name");
			return member != null && member.DeclaringType.FullName.Replace("+", "/", StringComparison.Ordinal) == typeFullName.Replace("+", "/", StringComparison.Ordinal) && member.Name == name;
		}

		// Token: 0x06002E36 RID: 11830 RVA: 0x0009E308 File Offset: 0x0009C508
		public static bool Is(this MemberReference member, Type type, string name)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			Helpers.ThrowIfArgumentNull<string>(name, "name");
			if (member == null)
			{
				return false;
			}
			string a = member.DeclaringType.FullName.Replace("+", "/", StringComparison.Ordinal);
			string fullName = type.FullName;
			return a == ((fullName != null) ? fullName.Replace("+", "/", StringComparison.Ordinal) : null) && member.Name == name;
		}

		// Token: 0x06002E37 RID: 11831 RVA: 0x0009E380 File Offset: 0x0009C580
		public static bool Is(this MethodReference method, string fullName)
		{
			Helpers.ThrowIfArgumentNull<string>(fullName, "fullName");
			if (method == null)
			{
				return false;
			}
			if (fullName.Contains(' ', StringComparison.Ordinal))
			{
				if (method.GetID(null, null, true, true).Replace("+", "/", StringComparison.Ordinal) == fullName.Replace("+", "/", StringComparison.Ordinal))
				{
					return true;
				}
				if (method.GetID(null, null, true, false).Replace("+", "/", StringComparison.Ordinal) == fullName.Replace("+", "/", StringComparison.Ordinal))
				{
					return true;
				}
			}
			return method.FullName.Replace("+", "/", StringComparison.Ordinal) == fullName.Replace("+", "/", StringComparison.Ordinal);
		}

		// Token: 0x06002E38 RID: 11832 RVA: 0x0009E43C File Offset: 0x0009C63C
		public static bool Is(this MethodReference method, string typeFullName, string name)
		{
			Helpers.ThrowIfArgumentNull<string>(typeFullName, "typeFullName");
			Helpers.ThrowIfArgumentNull<string>(name, "name");
			return method != null && ((name.Contains(' ', StringComparison.Ordinal) && method.DeclaringType.FullName.Replace("+", "/", StringComparison.Ordinal) == typeFullName.Replace("+", "/", StringComparison.Ordinal) && method.GetID(null, null, false, false).Replace("+", "/", StringComparison.Ordinal) == name.Replace("+", "/", StringComparison.Ordinal)) || (method.DeclaringType.FullName.Replace("+", "/", StringComparison.Ordinal) == typeFullName.Replace("+", "/", StringComparison.Ordinal) && method.Name == name));
		}

		// Token: 0x06002E39 RID: 11833 RVA: 0x0009E518 File Offset: 0x0009C718
		public static bool Is(this MethodReference method, Type type, string name)
		{
			Helpers.ThrowIfArgumentNull<Type>(type, "type");
			Helpers.ThrowIfArgumentNull<string>(name, "name");
			if (method == null)
			{
				return false;
			}
			if (name.Contains(' ', StringComparison.Ordinal))
			{
				string a = method.DeclaringType.FullName.Replace("+", "/", StringComparison.Ordinal);
				string fullName = type.FullName;
				if (a == ((fullName != null) ? fullName.Replace("+", "/", StringComparison.Ordinal) : null) && method.GetID(null, null, false, false).Replace("+", "/", StringComparison.Ordinal) == name.Replace("+", "/", StringComparison.Ordinal))
				{
					return true;
				}
			}
			string a2 = method.DeclaringType.FullName.Replace("+", "/", StringComparison.Ordinal);
			string fullName2 = type.FullName;
			return a2 == ((fullName2 != null) ? fullName2.Replace("+", "/", StringComparison.Ordinal) : null) && method.Name == name;
		}

		// Token: 0x06002E3A RID: 11834 RVA: 0x0009E60C File Offset: 0x0009C80C
		[NullableContext(2)]
		public static void ReplaceOperands([Nullable(1)] this ILProcessor il, object from, object to)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			foreach (Instruction instr in il.Body.Instructions)
			{
				object operand = instr.Operand;
				if ((operand != null) ? operand.Equals(from) : (from == null))
				{
					instr.Operand = to;
				}
			}
		}

		// Token: 0x06002E3B RID: 11835 RVA: 0x0009E688 File Offset: 0x0009C888
		public static FieldReference Import(this ILProcessor il, FieldInfo field)
		{
			return Helpers.ThrowIfNull<ILProcessor>(il, "il").Body.Method.Module.ImportReference(field);
		}

		// Token: 0x06002E3C RID: 11836 RVA: 0x0009E6AA File Offset: 0x0009C8AA
		public static MethodReference Import(this ILProcessor il, MethodBase method)
		{
			return Helpers.ThrowIfNull<ILProcessor>(il, "il").Body.Method.Module.ImportReference(method);
		}

		// Token: 0x06002E3D RID: 11837 RVA: 0x0009E6CC File Offset: 0x0009C8CC
		public static TypeReference Import(this ILProcessor il, Type type)
		{
			return Helpers.ThrowIfNull<ILProcessor>(il, "il").Body.Method.Module.ImportReference(type);
		}

		// Token: 0x06002E3E RID: 11838 RVA: 0x0009E6F0 File Offset: 0x0009C8F0
		public static MemberReference Import(this ILProcessor il, MemberInfo member)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			Helpers.ThrowIfArgumentNull<MemberInfo>(member, "member");
			FieldInfo info = member as FieldInfo;
			if (info != null)
			{
				return il.Import(info);
			}
			MethodBase info2 = member as MethodBase;
			if (info2 != null)
			{
				return il.Import(info2);
			}
			Type info3 = member as Type;
			if (info3 == null)
			{
				throw new NotSupportedException("Unsupported member type " + member.GetType().FullName);
			}
			return il.Import(info3);
		}

		// Token: 0x06002E3F RID: 11839 RVA: 0x0009E765 File Offset: 0x0009C965
		public static Instruction Create(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, FieldInfo field)
		{
			return Helpers.ThrowIfNull<ILProcessor>(il, "il").Create(opcode, il.Import(field));
		}

		// Token: 0x06002E40 RID: 11840 RVA: 0x0009E77F File Offset: 0x0009C97F
		public static Instruction Create(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			return il.Create(opcode, il.Import(method));
		}

		// Token: 0x06002E41 RID: 11841 RVA: 0x0009E79A File Offset: 0x0009C99A
		public static Instruction Create(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, Type type)
		{
			return Helpers.ThrowIfNull<ILProcessor>(il, "il").Create(opcode, il.Import(type));
		}

		// Token: 0x06002E42 RID: 11842 RVA: 0x0009E7B4 File Offset: 0x0009C9B4
		public static Instruction Create(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, object operand)
		{
			Instruction instruction = Helpers.ThrowIfNull<ILProcessor>(il, "il").Create(Mono.Cecil.Cil.OpCodes.Nop);
			instruction.OpCode = opcode;
			instruction.Operand = operand;
			return instruction;
		}

		// Token: 0x06002E43 RID: 11843 RVA: 0x0009E7DC File Offset: 0x0009C9DC
		public static Instruction Create(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, MemberInfo member)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			Helpers.ThrowIfArgumentNull<MemberInfo>(member, "member");
			FieldInfo info = member as FieldInfo;
			if (info != null)
			{
				return il.Create(opcode, info);
			}
			MethodBase info2 = member as MethodBase;
			if (info2 != null)
			{
				return il.Create(opcode, info2);
			}
			Type info3 = member as Type;
			if (info3 == null)
			{
				throw new NotSupportedException("Unsupported member type " + member.GetType().FullName);
			}
			return il.Create(opcode, info3);
		}

		// Token: 0x06002E44 RID: 11844 RVA: 0x0009E854 File Offset: 0x0009CA54
		public static void Emit(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, FieldInfo field)
		{
			Helpers.ThrowIfNull<ILProcessor>(il, "il").Emit(opcode, il.Import(field));
		}

		// Token: 0x06002E45 RID: 11845 RVA: 0x0009E86E File Offset: 0x0009CA6E
		public static void Emit(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, MethodBase method)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			Helpers.ThrowIfArgumentNull<MethodBase>(method, "method");
			il.Emit(opcode, il.Import(method));
		}

		// Token: 0x06002E46 RID: 11846 RVA: 0x0009E894 File Offset: 0x0009CA94
		public static void Emit(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, Type type)
		{
			Helpers.ThrowIfNull<ILProcessor>(il, "il").Emit(opcode, il.Import(type));
		}

		// Token: 0x06002E47 RID: 11847 RVA: 0x0009E8B0 File Offset: 0x0009CAB0
		public static void Emit(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, MemberInfo member)
		{
			Helpers.ThrowIfArgumentNull<ILProcessor>(il, "il");
			Helpers.ThrowIfArgumentNull<MemberInfo>(member, "member");
			FieldInfo info = member as FieldInfo;
			if (info != null)
			{
				il.Emit(opcode, info);
				return;
			}
			MethodBase info2 = member as MethodBase;
			if (info2 != null)
			{
				il.Emit(opcode, info2);
				return;
			}
			Type info3 = member as Type;
			if (info3 == null)
			{
				throw new NotSupportedException("Unsupported member type " + member.GetType().FullName);
			}
			il.Emit(opcode, info3);
		}

		// Token: 0x06002E48 RID: 11848 RVA: 0x0009E928 File Offset: 0x0009CB28
		public static void Emit(this ILProcessor il, Mono.Cecil.Cil.OpCode opcode, object operand)
		{
			Helpers.ThrowIfNull<ILProcessor>(il, "il").Append(il.Create(opcode, operand));
		}

		// Token: 0x06002E49 RID: 11849 RVA: 0x0009E944 File Offset: 0x0009CB44
		// Note: this type is marked as 'beforefieldinit'.
		static Extensions()
		{
			FieldInfo field = typeof(GenericParameter).GetField("position", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				throw new InvalidOperationException("No field 'position' on GenericParameter");
			}
			Extensions.f_GenericParameter_position = field;
			FieldInfo field2 = typeof(GenericParameter).GetField("type", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field2 == null)
			{
				throw new InvalidOperationException("No field 'type' on GenericParameter");
			}
			Extensions.f_GenericParameter_type = field2;
			Extensions._GetManagedSizeCache = new ConcurrentDictionary<Type, int>(new KeyValuePair<Type, int>[]
			{
				new KeyValuePair<Type, int>(typeof(void), 0)
			});
			Extensions._GetLdftnPointerCache = new Dictionary<MethodBase, Func<IntPtr>>();
			Extensions.RTDynamicMethod = typeof(DynamicMethod).GetNestedType("RTDynamicMethod", BindingFlags.NonPublic);
			Type rtdynamicMethod = Extensions.RTDynamicMethod;
			Extensions.RTDynamicMethod_m_owner = ((rtdynamicMethod != null) ? rtdynamicMethod.GetField("m_owner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) : null);
			Extensions.t_StateMachineAttribute = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.StateMachineAttribute");
			Type type = Extensions.t_StateMachineAttribute;
			Extensions.p_StateMachineType = ((type != null) ? type.GetProperty("StateMachineType") : null);
		}

		// Token: 0x04003AFE RID: 15102
		private static readonly Type t_Code = typeof(Code);

		// Token: 0x04003AFF RID: 15103
		private static readonly Type t_OpCodes = typeof(Mono.Cecil.Cil.OpCodes);

		// Token: 0x04003B00 RID: 15104
		private static readonly Dictionary<int, Mono.Cecil.Cil.OpCode> _ToLongOp = new Dictionary<int, Mono.Cecil.Cil.OpCode>();

		// Token: 0x04003B01 RID: 15105
		private static readonly Dictionary<int, Mono.Cecil.Cil.OpCode> _ToShortOp = new Dictionary<int, Mono.Cecil.Cil.OpCode>();

		// Token: 0x04003B02 RID: 15106
		private static readonly Dictionary<Type, FieldInfo> fmap_mono_assembly = new Dictionary<Type, FieldInfo>();

		// Token: 0x04003B03 RID: 15107
		private static readonly bool _MonoAssemblyNameHasArch = new AssemblyName("Dummy, ProcessorArchitecture=MSIL").ProcessorArchitecture == ProcessorArchitecture.MSIL;

		// Token: 0x04003B04 RID: 15108
		[Nullable(2)]
		private static readonly Type _RTDynamicMethod = typeof(DynamicMethod).GetNestedType("RTDynamicMethod", BindingFlags.Public | BindingFlags.NonPublic);

		// Token: 0x04003B05 RID: 15109
		private static readonly Type t_ParamArrayAttribute = typeof(ParamArrayAttribute);

		// Token: 0x04003B06 RID: 15110
		private static readonly FieldInfo f_GenericParameter_position;

		// Token: 0x04003B07 RID: 15111
		private static readonly FieldInfo f_GenericParameter_type;

		// Token: 0x04003B08 RID: 15112
		private static readonly ConcurrentDictionary<Type, int> _GetManagedSizeCache;

		// Token: 0x04003B09 RID: 15113
		[Nullable(2)]
		private static MethodInfo _GetManagedSizeHelper;

		// Token: 0x04003B0A RID: 15114
		private static readonly Dictionary<MethodBase, Func<IntPtr>> _GetLdftnPointerCache;

		// Token: 0x04003B0B RID: 15115
		[Nullable(2)]
		private static readonly Type RTDynamicMethod;

		// Token: 0x04003B0C RID: 15116
		[Nullable(2)]
		private static readonly FieldInfo RTDynamicMethod_m_owner;

		// Token: 0x04003B0D RID: 15117
		[Nullable(2)]
		private static readonly Type t_StateMachineAttribute;

		// Token: 0x04003B0E RID: 15118
		[Nullable(2)]
		private static readonly PropertyInfo p_StateMachineType;

		// Token: 0x020008B5 RID: 2229
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x04003B0F RID: 15119
			[Nullable(new byte[] { 0, 1 })]
			public static Func<Type, int> <0>__ComputeManagedSize;
		}
	}
}
