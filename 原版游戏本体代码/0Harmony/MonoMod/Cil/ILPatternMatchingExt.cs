using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.SourceGen.Attributes;
using MonoMod.Utils;

namespace MonoMod.Cil
{
	// Token: 0x02000873 RID: 2163
	[NullableContext(1)]
	[Nullable(0)]
	[EmitILOverloads("ILOpcodes.txt", "ILMatcher")]
	internal static class ILPatternMatchingExt
	{
		// Token: 0x06002AF3 RID: 10995 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(int l, int r)
		{
			return l == r;
		}

		// Token: 0x06002AF4 RID: 10996 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(int l, uint r)
		{
			return l == (int)r;
		}

		// Token: 0x06002AF5 RID: 10997 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(long l, long r)
		{
			return l == r;
		}

		// Token: 0x06002AF6 RID: 10998 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(long l, ulong r)
		{
			return l == (long)r;
		}

		// Token: 0x06002AF7 RID: 10999 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(float l, float r)
		{
			return l == r;
		}

		// Token: 0x06002AF8 RID: 11000 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(double l, double r)
		{
			return l == r;
		}

		// Token: 0x06002AF9 RID: 11001 RVA: 0x0008F969 File Offset: 0x0008DB69
		private static bool IsEquivalent(string l, string r)
		{
			return l == r;
		}

		// Token: 0x06002AFA RID: 11002 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(ILLabel l, ILLabel r)
		{
			return l == r;
		}

		// Token: 0x06002AFB RID: 11003 RVA: 0x0008F972 File Offset: 0x0008DB72
		private static bool IsEquivalent(ILLabel l, Instruction r)
		{
			return ILPatternMatchingExt.IsEquivalent(l.Target, r);
		}

		// Token: 0x06002AFC RID: 11004 RVA: 0x00087539 File Offset: 0x00085739
		[NullableContext(2)]
		private static bool IsEquivalent(Instruction l, Instruction r)
		{
			return l == r;
		}

		// Token: 0x06002AFD RID: 11005 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(TypeReference l, TypeReference r)
		{
			return l == r;
		}

		// Token: 0x06002AFE RID: 11006 RVA: 0x0008F980 File Offset: 0x0008DB80
		private static bool IsEquivalent(TypeReference l, Type r)
		{
			return l.Is(r);
		}

		// Token: 0x06002AFF RID: 11007 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(MethodReference l, MethodReference r)
		{
			return l == r;
		}

		// Token: 0x06002B00 RID: 11008 RVA: 0x0008F980 File Offset: 0x0008DB80
		private static bool IsEquivalent(MethodReference l, MethodBase r)
		{
			return l.Is(r);
		}

		// Token: 0x06002B01 RID: 11009 RVA: 0x0008F989 File Offset: 0x0008DB89
		private static bool IsEquivalent(MethodReference l, Type type, string name)
		{
			return l.DeclaringType.Is(type) && l.Name == name;
		}

		// Token: 0x06002B02 RID: 11010 RVA: 0x00087539 File Offset: 0x00085739
		private static bool IsEquivalent(FieldReference l, FieldReference r)
		{
			return l == r;
		}

		// Token: 0x06002B03 RID: 11011 RVA: 0x0008F980 File Offset: 0x0008DB80
		private static bool IsEquivalent(FieldReference l, FieldInfo r)
		{
			return l.Is(r);
		}

		// Token: 0x06002B04 RID: 11012 RVA: 0x0008F989 File Offset: 0x0008DB89
		private static bool IsEquivalent(FieldReference l, Type type, string name)
		{
			return l.DeclaringType.Is(type) && l.Name == name;
		}

		// Token: 0x06002B05 RID: 11013 RVA: 0x0008F9A7 File Offset: 0x0008DBA7
		private static bool IsEquivalent(ILLabel[] l, ILLabel[] r)
		{
			return l == r || l.SequenceEqual(r);
		}

		// Token: 0x06002B06 RID: 11014 RVA: 0x0008F9B8 File Offset: 0x0008DBB8
		private static bool IsEquivalent(ILLabel[] l, Instruction[] r)
		{
			if (l.Length != r.Length)
			{
				return false;
			}
			for (int i = 0; i < l.Length; i++)
			{
				if (!ILPatternMatchingExt.IsEquivalent(l[i].Target, r[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002B07 RID: 11015 RVA: 0x0008F9F4 File Offset: 0x0008DBF4
		private static bool IsEquivalent(IMethodSignature l, IMethodSignature r)
		{
			return l == r || (l.CallingConvention == r.CallingConvention && l.HasThis == r.HasThis && l.ExplicitThis == r.ExplicitThis && ILPatternMatchingExt.IsEquivalent(l.ReturnType, r.ReturnType) && ILPatternMatchingExt.CastParamsToRef(l).SequenceEqual(ILPatternMatchingExt.CastParamsToRef(r), ILPatternMatchingExt.ParameterRefEqualityComparer.Instance));
		}

		// Token: 0x06002B08 RID: 11016 RVA: 0x0008FA5C File Offset: 0x0008DC5C
		private static IEnumerable<ParameterReference> CastParamsToRef(IMethodSignature sig)
		{
			return sig.Parameters;
		}

		// Token: 0x06002B09 RID: 11017 RVA: 0x0008FA64 File Offset: 0x0008DC64
		private static bool IsEquivalent(IMetadataTokenProvider l, IMetadataTokenProvider r)
		{
			return l == r || l.MetadataToken == r.MetadataToken;
		}

		// Token: 0x06002B0A RID: 11018 RVA: 0x0008FA80 File Offset: 0x0008DC80
		private static bool IsEquivalent(IMetadataTokenProvider l, Type r)
		{
			TypeReference tl = l as TypeReference;
			return tl != null && ILPatternMatchingExt.IsEquivalent(tl, r);
		}

		// Token: 0x06002B0B RID: 11019 RVA: 0x0008FAA0 File Offset: 0x0008DCA0
		private static bool IsEquivalent(IMetadataTokenProvider l, FieldInfo r)
		{
			FieldReference fl = l as FieldReference;
			return fl != null && ILPatternMatchingExt.IsEquivalent(fl, r);
		}

		// Token: 0x06002B0C RID: 11020 RVA: 0x0008FAC0 File Offset: 0x0008DCC0
		private static bool IsEquivalent(IMetadataTokenProvider l, MethodBase r)
		{
			MethodReference ml = l as MethodReference;
			return ml != null && ILPatternMatchingExt.IsEquivalent(ml, r);
		}

		// Token: 0x06002B0D RID: 11021 RVA: 0x0008FAE0 File Offset: 0x0008DCE0
		public static bool Match(this Instruction instr, OpCode opcode)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == opcode;
		}

		// Token: 0x06002B0E RID: 11022 RVA: 0x0008FAF8 File Offset: 0x0008DCF8
		public static bool Match<[Nullable(2)] T>(this Instruction instr, OpCode opcode, T value)
		{
			T v;
			if (instr.Match(opcode, out v))
			{
				ref T ptr = ref v;
				T t = default(T);
				if (t == null)
				{
					t = v;
					ptr = ref t;
					if (t == null)
					{
						return value == null;
					}
				}
				return ptr.Equals(value);
			}
			return false;
		}

		// Token: 0x06002B0F RID: 11023 RVA: 0x0008FB50 File Offset: 0x0008DD50
		public static bool Match<[Nullable(2)] T>(this Instruction instr, OpCode opcode, [MaybeNullWhen(false)] out T value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == opcode)
			{
				object operand2 = instr.Operand;
				if (operand2 is T)
				{
					T operand = (T)((object)operand2);
					value = operand;
					return true;
				}
			}
			value = default(T);
			return false;
		}

		// Token: 0x06002B10 RID: 11024 RVA: 0x0008FBA0 File Offset: 0x0008DDA0
		[Obsolete("Leftover from legacy MonoMod, use MatchLeave instead")]
		public static bool MatchLeaveS(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchLeaveS(out v) && v == value;
		}

		// Token: 0x06002B11 RID: 11025 RVA: 0x0008FBBD File Offset: 0x0008DDBD
		[Obsolete("Leftover from legacy MonoMod, use MatchLeave instead")]
		public static bool MatchLeaveS(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Leave_S)
			{
				value = (ILLabel)instr.Operand;
				return true;
			}
			value = null;
			return false;
		}

		// Token: 0x06002B12 RID: 11026 RVA: 0x0008FBF0 File Offset: 0x0008DDF0
		public static bool MatchLdarg(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Ldarg || instr.OpCode == OpCodes.Ldarg_S)
			{
				value = ((ParameterReference)instr.Operand).Index;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldarg_0)
			{
				value = 0;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldarg_1)
			{
				value = 1;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldarg_2)
			{
				value = 2;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldarg_3)
			{
				value = 3;
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B13 RID: 11027 RVA: 0x0008FCA0 File Offset: 0x0008DEA0
		[NullableContext(2)]
		private static int ParameterToIndex(object obj)
		{
			int result;
			if (obj != null)
			{
				if (obj is int)
				{
					int i = (int)obj;
					result = i;
				}
				else if (obj is short)
				{
					short j = (short)obj;
					result = (int)j;
				}
				else if (obj is uint)
				{
					uint k = (uint)obj;
					result = (int)k;
				}
				else if (obj is ushort)
				{
					ushort l = (ushort)obj;
					result = (int)l;
				}
				else if (obj is byte)
				{
					byte m = (byte)obj;
					result = (int)m;
				}
				else if (obj is sbyte)
				{
					sbyte n = (sbyte)obj;
					result = (int)n;
				}
				else
				{
					ParameterReference pr = obj as ParameterReference;
					if (pr == null)
					{
						throw new InvalidCastException();
					}
					result = pr.Index;
				}
			}
			else
			{
				result = 0;
			}
			return result;
		}

		// Token: 0x06002B14 RID: 11028 RVA: 0x0008FD60 File Offset: 0x0008DF60
		public static bool MatchStarg(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Starg || instr.OpCode == OpCodes.Starg_S)
			{
				value = ILPatternMatchingExt.ParameterToIndex(instr.Operand);
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B15 RID: 11029 RVA: 0x0008FDB0 File Offset: 0x0008DFB0
		public static bool MatchLdarga(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Ldarga || instr.OpCode == OpCodes.Ldarga_S)
			{
				value = ILPatternMatchingExt.ParameterToIndex(instr.Operand);
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B16 RID: 11030 RVA: 0x0008FE00 File Offset: 0x0008E000
		[NullableContext(2)]
		private static int VarToIndex(object obj)
		{
			int result;
			if (obj != null)
			{
				if (obj is int)
				{
					int i = (int)obj;
					result = i;
				}
				else if (obj is short)
				{
					short j = (short)obj;
					result = (int)j;
				}
				else if (obj is uint)
				{
					uint k = (uint)obj;
					result = (int)k;
				}
				else if (obj is ushort)
				{
					ushort l = (ushort)obj;
					result = (int)l;
				}
				else if (obj is byte)
				{
					byte m = (byte)obj;
					result = (int)m;
				}
				else if (obj is sbyte)
				{
					sbyte n = (sbyte)obj;
					result = (int)n;
				}
				else
				{
					VariableReference vr = obj as VariableReference;
					if (vr == null)
					{
						throw new InvalidCastException();
					}
					result = vr.Index;
				}
			}
			else
			{
				result = 0;
			}
			return result;
		}

		// Token: 0x06002B17 RID: 11031 RVA: 0x0008FEC0 File Offset: 0x0008E0C0
		public static bool MatchLdloc(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Ldloc || instr.OpCode == OpCodes.Ldloc_S)
			{
				value = ILPatternMatchingExt.VarToIndex(instr.Operand);
				return true;
			}
			if (instr.OpCode == OpCodes.Ldloc_0)
			{
				value = 0;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldloc_1)
			{
				value = 1;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldloc_2)
			{
				value = 2;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldloc_3)
			{
				value = 3;
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B18 RID: 11032 RVA: 0x0008FF6C File Offset: 0x0008E16C
		public static bool MatchStloc(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Stloc || instr.OpCode == OpCodes.Stloc_S)
			{
				value = ILPatternMatchingExt.VarToIndex(instr.Operand);
				return true;
			}
			if (instr.OpCode == OpCodes.Stloc_0)
			{
				value = 0;
				return true;
			}
			if (instr.OpCode == OpCodes.Stloc_1)
			{
				value = 1;
				return true;
			}
			if (instr.OpCode == OpCodes.Stloc_2)
			{
				value = 2;
				return true;
			}
			if (instr.OpCode == OpCodes.Stloc_3)
			{
				value = 3;
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B19 RID: 11033 RVA: 0x00090018 File Offset: 0x0008E218
		public static bool MatchLdloca(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Ldloca || instr.OpCode == OpCodes.Ldloca_S)
			{
				value = ILPatternMatchingExt.VarToIndex(instr.Operand);
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B1A RID: 11034 RVA: 0x00090068 File Offset: 0x0008E268
		public static bool MatchLdcI4(this Instruction instr, out int value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Ldc_I4)
			{
				value = (int)instr.Operand;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_S)
			{
				value = (int)((sbyte)instr.Operand);
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_0)
			{
				value = 0;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_1)
			{
				value = 1;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_2)
			{
				value = 2;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_3)
			{
				value = 3;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_4)
			{
				value = 4;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_5)
			{
				value = 5;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_6)
			{
				value = 6;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_7)
			{
				value = 7;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_8)
			{
				value = 8;
				return true;
			}
			if (instr.OpCode == OpCodes.Ldc_I4_M1)
			{
				value = -1;
				return true;
			}
			value = 0;
			return false;
		}

		// Token: 0x06002B1B RID: 11035 RVA: 0x000901AC File Offset: 0x0008E3AC
		public static bool MatchCallOrCallvirt(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			Helpers.ThrowIfArgumentNull<Instruction>(instr, "instr");
			if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
			{
				MethodReference mr = instr.Operand as MethodReference;
				if (mr != null)
				{
					value = mr;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B1C RID: 11036 RVA: 0x00090200 File Offset: 0x0008E400
		public static bool MatchNewobj(this Instruction instr, Type type)
		{
			MethodReference v;
			return instr.MatchNewobj(out v) && v.DeclaringType.Is(type);
		}

		// Token: 0x06002B1D RID: 11037 RVA: 0x00090225 File Offset: 0x0008E425
		public static bool MatchNewobj<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchNewobj(typeof(T));
		}

		// Token: 0x06002B1E RID: 11038 RVA: 0x00090238 File Offset: 0x0008E438
		public static bool MatchNewobj(this Instruction instr, string typeFullName)
		{
			MethodReference v;
			return instr.MatchNewobj(out v) && v.DeclaringType.Is(typeFullName);
		}

		// Token: 0x06002B1F RID: 11039 RVA: 0x0009025D File Offset: 0x0008E45D
		public static bool MatchAdd(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Add;
		}

		// Token: 0x06002B20 RID: 11040 RVA: 0x00090279 File Offset: 0x0008E479
		public static bool MatchAddOvf(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Add_Ovf;
		}

		// Token: 0x06002B21 RID: 11041 RVA: 0x00090295 File Offset: 0x0008E495
		public static bool MatchAddOvfUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Add_Ovf_Un;
		}

		// Token: 0x06002B22 RID: 11042 RVA: 0x000902B1 File Offset: 0x0008E4B1
		public static bool MatchAnd(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.And;
		}

		// Token: 0x06002B23 RID: 11043 RVA: 0x000902CD File Offset: 0x0008E4CD
		public static bool MatchArglist(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Arglist;
		}

		// Token: 0x06002B24 RID: 11044 RVA: 0x000902EC File Offset: 0x0008E4EC
		public static bool MatchBeq(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Beq || instr.OpCode == OpCodes.Beq_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B25 RID: 11045 RVA: 0x00090340 File Offset: 0x0008E540
		public static bool MatchBeq(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBeq(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B26 RID: 11046 RVA: 0x00090360 File Offset: 0x0008E560
		public static bool MatchBeq(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBeq(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B27 RID: 11047 RVA: 0x00090380 File Offset: 0x0008E580
		public static bool MatchBge(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Bge || instr.OpCode == OpCodes.Bge_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B28 RID: 11048 RVA: 0x000903D4 File Offset: 0x0008E5D4
		public static bool MatchBge(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBge(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B29 RID: 11049 RVA: 0x000903F4 File Offset: 0x0008E5F4
		public static bool MatchBge(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBge(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B2A RID: 11050 RVA: 0x00090414 File Offset: 0x0008E614
		public static bool MatchBgeUn(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Bge_Un || instr.OpCode == OpCodes.Bge_Un_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B2B RID: 11051 RVA: 0x00090468 File Offset: 0x0008E668
		public static bool MatchBgeUn(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBgeUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B2C RID: 11052 RVA: 0x00090488 File Offset: 0x0008E688
		public static bool MatchBgeUn(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBgeUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B2D RID: 11053 RVA: 0x000904A8 File Offset: 0x0008E6A8
		public static bool MatchBgt(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Bgt || instr.OpCode == OpCodes.Bgt_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B2E RID: 11054 RVA: 0x000904FC File Offset: 0x0008E6FC
		public static bool MatchBgt(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBgt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B2F RID: 11055 RVA: 0x0009051C File Offset: 0x0008E71C
		public static bool MatchBgt(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBgt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B30 RID: 11056 RVA: 0x0009053C File Offset: 0x0008E73C
		public static bool MatchBgtUn(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Bgt_Un || instr.OpCode == OpCodes.Bgt_Un_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B31 RID: 11057 RVA: 0x00090590 File Offset: 0x0008E790
		public static bool MatchBgtUn(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBgtUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B32 RID: 11058 RVA: 0x000905B0 File Offset: 0x0008E7B0
		public static bool MatchBgtUn(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBgtUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B33 RID: 11059 RVA: 0x000905D0 File Offset: 0x0008E7D0
		public static bool MatchBle(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ble || instr.OpCode == OpCodes.Ble_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B34 RID: 11060 RVA: 0x00090624 File Offset: 0x0008E824
		public static bool MatchBle(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBle(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B35 RID: 11061 RVA: 0x00090644 File Offset: 0x0008E844
		public static bool MatchBle(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBle(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B36 RID: 11062 RVA: 0x00090664 File Offset: 0x0008E864
		public static bool MatchBleUn(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ble_Un || instr.OpCode == OpCodes.Ble_Un_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B37 RID: 11063 RVA: 0x000906B8 File Offset: 0x0008E8B8
		public static bool MatchBleUn(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBleUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B38 RID: 11064 RVA: 0x000906D8 File Offset: 0x0008E8D8
		public static bool MatchBleUn(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBleUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B39 RID: 11065 RVA: 0x000906F8 File Offset: 0x0008E8F8
		public static bool MatchBlt(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Blt || instr.OpCode == OpCodes.Blt_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B3A RID: 11066 RVA: 0x0009074C File Offset: 0x0008E94C
		public static bool MatchBlt(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBlt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B3B RID: 11067 RVA: 0x0009076C File Offset: 0x0008E96C
		public static bool MatchBlt(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBlt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B3C RID: 11068 RVA: 0x0009078C File Offset: 0x0008E98C
		public static bool MatchBltUn(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Blt_Un || instr.OpCode == OpCodes.Blt_Un_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B3D RID: 11069 RVA: 0x000907E0 File Offset: 0x0008E9E0
		public static bool MatchBltUn(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBltUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B3E RID: 11070 RVA: 0x00090800 File Offset: 0x0008EA00
		public static bool MatchBltUn(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBltUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B3F RID: 11071 RVA: 0x00090820 File Offset: 0x0008EA20
		public static bool MatchBneUn(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Bne_Un || instr.OpCode == OpCodes.Bne_Un_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B40 RID: 11072 RVA: 0x00090874 File Offset: 0x0008EA74
		public static bool MatchBneUn(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBneUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B41 RID: 11073 RVA: 0x00090894 File Offset: 0x0008EA94
		public static bool MatchBneUn(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBneUn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B42 RID: 11074 RVA: 0x000908B4 File Offset: 0x0008EAB4
		public static bool MatchBox(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Box)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B43 RID: 11075 RVA: 0x000908F8 File Offset: 0x0008EAF8
		public static bool MatchBox(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchBox(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B44 RID: 11076 RVA: 0x00090918 File Offset: 0x0008EB18
		public static bool MatchBox(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchBox(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B45 RID: 11077 RVA: 0x00090938 File Offset: 0x0008EB38
		public static bool MatchBox<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchBox(typeof(T));
		}

		// Token: 0x06002B46 RID: 11078 RVA: 0x0009094C File Offset: 0x0008EB4C
		public static bool MatchBox(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchBox(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002B47 RID: 11079 RVA: 0x0009096C File Offset: 0x0008EB6C
		public static bool MatchBr(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Br || instr.OpCode == OpCodes.Br_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B48 RID: 11080 RVA: 0x000909C0 File Offset: 0x0008EBC0
		public static bool MatchBr(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBr(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B49 RID: 11081 RVA: 0x000909E0 File Offset: 0x0008EBE0
		public static bool MatchBr(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBr(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B4A RID: 11082 RVA: 0x00090A00 File Offset: 0x0008EC00
		public static bool MatchBreak(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Break;
		}

		// Token: 0x06002B4B RID: 11083 RVA: 0x00090A1C File Offset: 0x0008EC1C
		public static bool MatchBrfalse(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Brfalse || instr.OpCode == OpCodes.Brfalse_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B4C RID: 11084 RVA: 0x00090A70 File Offset: 0x0008EC70
		public static bool MatchBrfalse(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBrfalse(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B4D RID: 11085 RVA: 0x00090A90 File Offset: 0x0008EC90
		public static bool MatchBrfalse(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBrfalse(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B4E RID: 11086 RVA: 0x00090AB0 File Offset: 0x0008ECB0
		public static bool MatchBrtrue(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Brtrue || instr.OpCode == OpCodes.Brtrue_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B4F RID: 11087 RVA: 0x00090B04 File Offset: 0x0008ED04
		public static bool MatchBrtrue(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchBrtrue(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B50 RID: 11088 RVA: 0x00090B24 File Offset: 0x0008ED24
		public static bool MatchBrtrue(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchBrtrue(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B51 RID: 11089 RVA: 0x00090B44 File Offset: 0x0008ED44
		public static bool MatchCall(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Call)
			{
				MethodReference op = instr.Operand as MethodReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B52 RID: 11090 RVA: 0x00090B88 File Offset: 0x0008ED88
		public static bool MatchCall(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchCall(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B53 RID: 11091 RVA: 0x00090BA8 File Offset: 0x0008EDA8
		public static bool MatchCall(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchCall(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B54 RID: 11092 RVA: 0x00090BC8 File Offset: 0x0008EDC8
		public static bool MatchCall(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchCall(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002B55 RID: 11093 RVA: 0x00090BE9 File Offset: 0x0008EDE9
		public static bool MatchCall<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchCall(typeof(T), name);
		}

		// Token: 0x06002B56 RID: 11094 RVA: 0x00090BFC File Offset: 0x0008EDFC
		public static bool MatchCall(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchCall(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002B57 RID: 11095 RVA: 0x00090C20 File Offset: 0x0008EE20
		public static bool MatchCalli(this Instruction instr, [MaybeNullWhen(false)] out IMethodSignature value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Calli)
			{
				IMethodSignature op = instr.Operand as IMethodSignature;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B58 RID: 11096 RVA: 0x00090C64 File Offset: 0x0008EE64
		public static bool MatchCalli(this Instruction instr, IMethodSignature value)
		{
			IMethodSignature v;
			return instr.MatchCalli(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B59 RID: 11097 RVA: 0x00090C84 File Offset: 0x0008EE84
		public static bool MatchCallvirt(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Callvirt)
			{
				MethodReference op = instr.Operand as MethodReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B5A RID: 11098 RVA: 0x00090CC8 File Offset: 0x0008EEC8
		public static bool MatchCallvirt(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchCallvirt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B5B RID: 11099 RVA: 0x00090CE8 File Offset: 0x0008EEE8
		public static bool MatchCallvirt(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchCallvirt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B5C RID: 11100 RVA: 0x00090D08 File Offset: 0x0008EF08
		public static bool MatchCallvirt(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchCallvirt(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002B5D RID: 11101 RVA: 0x00090D29 File Offset: 0x0008EF29
		public static bool MatchCallvirt<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchCallvirt(typeof(T), name);
		}

		// Token: 0x06002B5E RID: 11102 RVA: 0x00090D3C File Offset: 0x0008EF3C
		public static bool MatchCallvirt(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchCallvirt(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002B5F RID: 11103 RVA: 0x00090D60 File Offset: 0x0008EF60
		public static bool MatchCallOrCallvirt(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchCallOrCallvirt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B60 RID: 11104 RVA: 0x00090D80 File Offset: 0x0008EF80
		public static bool MatchCallOrCallvirt(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchCallOrCallvirt(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B61 RID: 11105 RVA: 0x00090DA0 File Offset: 0x0008EFA0
		public static bool MatchCallOrCallvirt(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchCallOrCallvirt(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002B62 RID: 11106 RVA: 0x00090DC1 File Offset: 0x0008EFC1
		public static bool MatchCallOrCallvirt<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchCallOrCallvirt(typeof(T), name);
		}

		// Token: 0x06002B63 RID: 11107 RVA: 0x00090DD4 File Offset: 0x0008EFD4
		public static bool MatchCallOrCallvirt(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchCallOrCallvirt(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002B64 RID: 11108 RVA: 0x00090DF8 File Offset: 0x0008EFF8
		public static bool MatchCastclass(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Castclass)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B65 RID: 11109 RVA: 0x00090E3C File Offset: 0x0008F03C
		public static bool MatchCastclass(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchCastclass(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B66 RID: 11110 RVA: 0x00090E5C File Offset: 0x0008F05C
		public static bool MatchCastclass(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchCastclass(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B67 RID: 11111 RVA: 0x00090E7C File Offset: 0x0008F07C
		public static bool MatchCastclass<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchCastclass(typeof(T));
		}

		// Token: 0x06002B68 RID: 11112 RVA: 0x00090E90 File Offset: 0x0008F090
		public static bool MatchCastclass(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchCastclass(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002B69 RID: 11113 RVA: 0x00090EB0 File Offset: 0x0008F0B0
		public static bool MatchCeq(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ceq;
		}

		// Token: 0x06002B6A RID: 11114 RVA: 0x00090ECC File Offset: 0x0008F0CC
		public static bool MatchCgt(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Cgt;
		}

		// Token: 0x06002B6B RID: 11115 RVA: 0x00090EE8 File Offset: 0x0008F0E8
		public static bool MatchCgtUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Cgt_Un;
		}

		// Token: 0x06002B6C RID: 11116 RVA: 0x00090F04 File Offset: 0x0008F104
		public static bool MatchCkfinite(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ckfinite;
		}

		// Token: 0x06002B6D RID: 11117 RVA: 0x00090F20 File Offset: 0x0008F120
		public static bool MatchClt(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Clt;
		}

		// Token: 0x06002B6E RID: 11118 RVA: 0x00090F3C File Offset: 0x0008F13C
		public static bool MatchCltUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Clt_Un;
		}

		// Token: 0x06002B6F RID: 11119 RVA: 0x00090F58 File Offset: 0x0008F158
		public static bool MatchConstrained(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Constrained)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B70 RID: 11120 RVA: 0x00090F9C File Offset: 0x0008F19C
		public static bool MatchConstrained(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchConstrained(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B71 RID: 11121 RVA: 0x00090FBC File Offset: 0x0008F1BC
		public static bool MatchConstrained(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchConstrained(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B72 RID: 11122 RVA: 0x00090FDC File Offset: 0x0008F1DC
		public static bool MatchConstrained<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchConstrained(typeof(T));
		}

		// Token: 0x06002B73 RID: 11123 RVA: 0x00090FF0 File Offset: 0x0008F1F0
		public static bool MatchConstrained(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchConstrained(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002B74 RID: 11124 RVA: 0x00091010 File Offset: 0x0008F210
		public static bool MatchConvI(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_I;
		}

		// Token: 0x06002B75 RID: 11125 RVA: 0x0009102C File Offset: 0x0008F22C
		public static bool MatchConvI1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_I1;
		}

		// Token: 0x06002B76 RID: 11126 RVA: 0x00091048 File Offset: 0x0008F248
		public static bool MatchConvI2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_I2;
		}

		// Token: 0x06002B77 RID: 11127 RVA: 0x00091064 File Offset: 0x0008F264
		public static bool MatchConvI4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_I4;
		}

		// Token: 0x06002B78 RID: 11128 RVA: 0x00091080 File Offset: 0x0008F280
		public static bool MatchConvI8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_I8;
		}

		// Token: 0x06002B79 RID: 11129 RVA: 0x0009109C File Offset: 0x0008F29C
		public static bool MatchConvOvfI(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I;
		}

		// Token: 0x06002B7A RID: 11130 RVA: 0x000910B8 File Offset: 0x0008F2B8
		public static bool MatchConvOvfIUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I_Un;
		}

		// Token: 0x06002B7B RID: 11131 RVA: 0x000910D4 File Offset: 0x0008F2D4
		public static bool MatchConvOvfI1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I1;
		}

		// Token: 0x06002B7C RID: 11132 RVA: 0x000910F0 File Offset: 0x0008F2F0
		public static bool MatchConvOvfI1Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I1_Un;
		}

		// Token: 0x06002B7D RID: 11133 RVA: 0x0009110C File Offset: 0x0008F30C
		public static bool MatchConvOvfI2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I2;
		}

		// Token: 0x06002B7E RID: 11134 RVA: 0x00091128 File Offset: 0x0008F328
		public static bool MatchConvOvfI2Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I2_Un;
		}

		// Token: 0x06002B7F RID: 11135 RVA: 0x00091144 File Offset: 0x0008F344
		public static bool MatchConvOvfI4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I4;
		}

		// Token: 0x06002B80 RID: 11136 RVA: 0x00091160 File Offset: 0x0008F360
		public static bool MatchConvOvfI4Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I4_Un;
		}

		// Token: 0x06002B81 RID: 11137 RVA: 0x0009117C File Offset: 0x0008F37C
		public static bool MatchConvOvfI8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I8;
		}

		// Token: 0x06002B82 RID: 11138 RVA: 0x00091198 File Offset: 0x0008F398
		public static bool MatchConvOvfI8Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_I8_Un;
		}

		// Token: 0x06002B83 RID: 11139 RVA: 0x000911B4 File Offset: 0x0008F3B4
		public static bool MatchConvOvfU(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U;
		}

		// Token: 0x06002B84 RID: 11140 RVA: 0x000911D0 File Offset: 0x0008F3D0
		public static bool MatchConvOvfUUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U_Un;
		}

		// Token: 0x06002B85 RID: 11141 RVA: 0x000911EC File Offset: 0x0008F3EC
		public static bool MatchConvOvfU1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U1;
		}

		// Token: 0x06002B86 RID: 11142 RVA: 0x00091208 File Offset: 0x0008F408
		public static bool MatchConvOvfU1Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U1_Un;
		}

		// Token: 0x06002B87 RID: 11143 RVA: 0x00091224 File Offset: 0x0008F424
		public static bool MatchConvOvfU2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U2;
		}

		// Token: 0x06002B88 RID: 11144 RVA: 0x00091240 File Offset: 0x0008F440
		public static bool MatchConvOvfU2Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U2_Un;
		}

		// Token: 0x06002B89 RID: 11145 RVA: 0x0009125C File Offset: 0x0008F45C
		public static bool MatchConvOvfU4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U4;
		}

		// Token: 0x06002B8A RID: 11146 RVA: 0x00091278 File Offset: 0x0008F478
		public static bool MatchConvOvfU4Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U4_Un;
		}

		// Token: 0x06002B8B RID: 11147 RVA: 0x00091294 File Offset: 0x0008F494
		public static bool MatchConvOvfU8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U8;
		}

		// Token: 0x06002B8C RID: 11148 RVA: 0x000912B0 File Offset: 0x0008F4B0
		public static bool MatchConvOvfU8Un(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_Ovf_U8_Un;
		}

		// Token: 0x06002B8D RID: 11149 RVA: 0x000912CC File Offset: 0x0008F4CC
		public static bool MatchConvRUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_R_Un;
		}

		// Token: 0x06002B8E RID: 11150 RVA: 0x000912E8 File Offset: 0x0008F4E8
		public static bool MatchConvR4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_R4;
		}

		// Token: 0x06002B8F RID: 11151 RVA: 0x00091304 File Offset: 0x0008F504
		public static bool MatchConvR8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_R8;
		}

		// Token: 0x06002B90 RID: 11152 RVA: 0x00091320 File Offset: 0x0008F520
		public static bool MatchConvU(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_U;
		}

		// Token: 0x06002B91 RID: 11153 RVA: 0x0009133C File Offset: 0x0008F53C
		public static bool MatchConvU1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_U1;
		}

		// Token: 0x06002B92 RID: 11154 RVA: 0x00091358 File Offset: 0x0008F558
		public static bool MatchConvU2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_U2;
		}

		// Token: 0x06002B93 RID: 11155 RVA: 0x00091374 File Offset: 0x0008F574
		public static bool MatchConvU4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_U4;
		}

		// Token: 0x06002B94 RID: 11156 RVA: 0x00091390 File Offset: 0x0008F590
		public static bool MatchConvU8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Conv_U8;
		}

		// Token: 0x06002B95 RID: 11157 RVA: 0x000913AC File Offset: 0x0008F5AC
		public static bool MatchCpblk(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Cpblk;
		}

		// Token: 0x06002B96 RID: 11158 RVA: 0x000913C8 File Offset: 0x0008F5C8
		public static bool MatchCpobj(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Cpobj)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002B97 RID: 11159 RVA: 0x0009140C File Offset: 0x0008F60C
		public static bool MatchCpobj(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchCpobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B98 RID: 11160 RVA: 0x0009142C File Offset: 0x0008F62C
		public static bool MatchCpobj(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchCpobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002B99 RID: 11161 RVA: 0x0009144C File Offset: 0x0008F64C
		public static bool MatchCpobj<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchCpobj(typeof(T));
		}

		// Token: 0x06002B9A RID: 11162 RVA: 0x00091460 File Offset: 0x0008F660
		public static bool MatchCpobj(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchCpobj(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002B9B RID: 11163 RVA: 0x00091480 File Offset: 0x0008F680
		public static bool MatchDiv(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Div;
		}

		// Token: 0x06002B9C RID: 11164 RVA: 0x0009149C File Offset: 0x0008F69C
		public static bool MatchDivUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Div_Un;
		}

		// Token: 0x06002B9D RID: 11165 RVA: 0x000914B8 File Offset: 0x0008F6B8
		public static bool MatchDup(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Dup;
		}

		// Token: 0x06002B9E RID: 11166 RVA: 0x000914D4 File Offset: 0x0008F6D4
		public static bool MatchEndfilter(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Endfilter;
		}

		// Token: 0x06002B9F RID: 11167 RVA: 0x000914F0 File Offset: 0x0008F6F0
		public static bool MatchEndfinally(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Endfinally;
		}

		// Token: 0x06002BA0 RID: 11168 RVA: 0x0009150C File Offset: 0x0008F70C
		public static bool MatchInitblk(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Initblk;
		}

		// Token: 0x06002BA1 RID: 11169 RVA: 0x00091528 File Offset: 0x0008F728
		public static bool MatchInitobj(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Initobj)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BA2 RID: 11170 RVA: 0x0009156C File Offset: 0x0008F76C
		public static bool MatchInitobj(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchInitobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BA3 RID: 11171 RVA: 0x0009158C File Offset: 0x0008F78C
		public static bool MatchInitobj(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchInitobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BA4 RID: 11172 RVA: 0x000915AC File Offset: 0x0008F7AC
		public static bool MatchInitobj<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchInitobj(typeof(T));
		}

		// Token: 0x06002BA5 RID: 11173 RVA: 0x000915C0 File Offset: 0x0008F7C0
		public static bool MatchInitobj(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchInitobj(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002BA6 RID: 11174 RVA: 0x000915E0 File Offset: 0x0008F7E0
		public static bool MatchIsinst(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Isinst)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BA7 RID: 11175 RVA: 0x00091624 File Offset: 0x0008F824
		public static bool MatchIsinst(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchIsinst(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BA8 RID: 11176 RVA: 0x00091644 File Offset: 0x0008F844
		public static bool MatchIsinst(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchIsinst(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BA9 RID: 11177 RVA: 0x00091664 File Offset: 0x0008F864
		public static bool MatchIsinst<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchIsinst(typeof(T));
		}

		// Token: 0x06002BAA RID: 11178 RVA: 0x00091678 File Offset: 0x0008F878
		public static bool MatchIsinst(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchIsinst(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002BAB RID: 11179 RVA: 0x00091698 File Offset: 0x0008F898
		public static bool MatchJmp(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Jmp)
			{
				MethodReference op = instr.Operand as MethodReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BAC RID: 11180 RVA: 0x000916DC File Offset: 0x0008F8DC
		public static bool MatchJmp(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchJmp(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BAD RID: 11181 RVA: 0x000916FC File Offset: 0x0008F8FC
		public static bool MatchJmp(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchJmp(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BAE RID: 11182 RVA: 0x0009171C File Offset: 0x0008F91C
		public static bool MatchJmp(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchJmp(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002BAF RID: 11183 RVA: 0x0009173D File Offset: 0x0008F93D
		public static bool MatchJmp<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchJmp(typeof(T), name);
		}

		// Token: 0x06002BB0 RID: 11184 RVA: 0x00091750 File Offset: 0x0008F950
		public static bool MatchJmp(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchJmp(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002BB1 RID: 11185 RVA: 0x00091771 File Offset: 0x0008F971
		public static bool MatchLdarg0(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldarg_0;
		}

		// Token: 0x06002BB2 RID: 11186 RVA: 0x0009178D File Offset: 0x0008F98D
		public static bool MatchLdarg1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldarg_1;
		}

		// Token: 0x06002BB3 RID: 11187 RVA: 0x000917A9 File Offset: 0x0008F9A9
		public static bool MatchLdarg2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldarg_2;
		}

		// Token: 0x06002BB4 RID: 11188 RVA: 0x000917C5 File Offset: 0x0008F9C5
		public static bool MatchLdarg3(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldarg_3;
		}

		// Token: 0x06002BB5 RID: 11189 RVA: 0x000917E4 File Offset: 0x0008F9E4
		public static bool MatchLdarg(this Instruction instr, int value)
		{
			int v;
			return instr.MatchLdarg(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BB6 RID: 11190 RVA: 0x00091804 File Offset: 0x0008FA04
		public static bool MatchLdarg(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchLdarg(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BB7 RID: 11191 RVA: 0x00091824 File Offset: 0x0008FA24
		public static bool MatchLdarga(this Instruction instr, int value)
		{
			int v;
			return instr.MatchLdarga(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BB8 RID: 11192 RVA: 0x00091844 File Offset: 0x0008FA44
		public static bool MatchLdarga(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchLdarga(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BB9 RID: 11193 RVA: 0x00091864 File Offset: 0x0008FA64
		public static bool MatchLdcI4(this Instruction instr, int value)
		{
			int v;
			return instr.MatchLdcI4(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BBA RID: 11194 RVA: 0x00091884 File Offset: 0x0008FA84
		public static bool MatchLdcI4(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchLdcI4(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BBB RID: 11195 RVA: 0x000918A4 File Offset: 0x0008FAA4
		public static bool MatchLdcI8(this Instruction instr, [MaybeNullWhen(false)] out long value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldc_I8)
			{
				object operand = instr.Operand;
				if (operand is long)
				{
					long op = (long)operand;
					value = op;
					return true;
				}
			}
			value = 0L;
			return false;
		}

		// Token: 0x06002BBC RID: 11196 RVA: 0x000918F0 File Offset: 0x0008FAF0
		public static bool MatchLdcI8(this Instruction instr, long value)
		{
			long v;
			return instr.MatchLdcI8(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BBD RID: 11197 RVA: 0x00091910 File Offset: 0x0008FB10
		public static bool MatchLdcI8(this Instruction instr, ulong value)
		{
			long v;
			return instr.MatchLdcI8(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BBE RID: 11198 RVA: 0x00091930 File Offset: 0x0008FB30
		public static bool MatchLdcR4(this Instruction instr, [MaybeNullWhen(false)] out float value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldc_R4)
			{
				object operand = instr.Operand;
				if (operand is float)
				{
					float op = (float)operand;
					value = op;
					return true;
				}
			}
			value = 0f;
			return false;
		}

		// Token: 0x06002BBF RID: 11199 RVA: 0x0009197C File Offset: 0x0008FB7C
		public static bool MatchLdcR4(this Instruction instr, float value)
		{
			float v;
			return instr.MatchLdcR4(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BC0 RID: 11200 RVA: 0x0009199C File Offset: 0x0008FB9C
		public static bool MatchLdcR8(this Instruction instr, [MaybeNullWhen(false)] out double value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldc_R8)
			{
				object operand = instr.Operand;
				if (operand is double)
				{
					double op = (double)operand;
					value = op;
					return true;
				}
			}
			value = 0.0;
			return false;
		}

		// Token: 0x06002BC1 RID: 11201 RVA: 0x000919EC File Offset: 0x0008FBEC
		public static bool MatchLdcR8(this Instruction instr, double value)
		{
			double v;
			return instr.MatchLdcR8(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BC2 RID: 11202 RVA: 0x00091A0C File Offset: 0x0008FC0C
		public static bool MatchLdelemAny(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_Any)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BC3 RID: 11203 RVA: 0x00091A50 File Offset: 0x0008FC50
		public static bool MatchLdelemAny(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchLdelemAny(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BC4 RID: 11204 RVA: 0x00091A70 File Offset: 0x0008FC70
		public static bool MatchLdelemAny(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchLdelemAny(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BC5 RID: 11205 RVA: 0x00091A90 File Offset: 0x0008FC90
		public static bool MatchLdelemAny<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchLdelemAny(typeof(T));
		}

		// Token: 0x06002BC6 RID: 11206 RVA: 0x00091AA4 File Offset: 0x0008FCA4
		public static bool MatchLdelemAny(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchLdelemAny(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002BC7 RID: 11207 RVA: 0x00091AC4 File Offset: 0x0008FCC4
		public static bool MatchLdelemI(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_I;
		}

		// Token: 0x06002BC8 RID: 11208 RVA: 0x00091AE0 File Offset: 0x0008FCE0
		public static bool MatchLdelemI1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_I1;
		}

		// Token: 0x06002BC9 RID: 11209 RVA: 0x00091AFC File Offset: 0x0008FCFC
		public static bool MatchLdelemI2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_I2;
		}

		// Token: 0x06002BCA RID: 11210 RVA: 0x00091B18 File Offset: 0x0008FD18
		public static bool MatchLdelemI4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_I4;
		}

		// Token: 0x06002BCB RID: 11211 RVA: 0x00091B34 File Offset: 0x0008FD34
		public static bool MatchLdelemI8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_I8;
		}

		// Token: 0x06002BCC RID: 11212 RVA: 0x00091B50 File Offset: 0x0008FD50
		public static bool MatchLdelemR4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_R4;
		}

		// Token: 0x06002BCD RID: 11213 RVA: 0x00091B6C File Offset: 0x0008FD6C
		public static bool MatchLdelemR8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_R8;
		}

		// Token: 0x06002BCE RID: 11214 RVA: 0x00091B88 File Offset: 0x0008FD88
		public static bool MatchLdelemRef(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_Ref;
		}

		// Token: 0x06002BCF RID: 11215 RVA: 0x00091BA4 File Offset: 0x0008FDA4
		public static bool MatchLdelemU1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_U1;
		}

		// Token: 0x06002BD0 RID: 11216 RVA: 0x00091BC0 File Offset: 0x0008FDC0
		public static bool MatchLdelemU2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_U2;
		}

		// Token: 0x06002BD1 RID: 11217 RVA: 0x00091BDC File Offset: 0x0008FDDC
		public static bool MatchLdelemU4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelem_U4;
		}

		// Token: 0x06002BD2 RID: 11218 RVA: 0x00091BF8 File Offset: 0x0008FDF8
		public static bool MatchLdelema(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldelema)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BD3 RID: 11219 RVA: 0x00091C3C File Offset: 0x0008FE3C
		public static bool MatchLdelema(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchLdelema(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BD4 RID: 11220 RVA: 0x00091C5C File Offset: 0x0008FE5C
		public static bool MatchLdelema(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchLdelema(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BD5 RID: 11221 RVA: 0x00091C7C File Offset: 0x0008FE7C
		public static bool MatchLdelema<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchLdelema(typeof(T));
		}

		// Token: 0x06002BD6 RID: 11222 RVA: 0x00091C90 File Offset: 0x0008FE90
		public static bool MatchLdelema(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchLdelema(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002BD7 RID: 11223 RVA: 0x00091CB0 File Offset: 0x0008FEB0
		public static bool MatchLdfld(this Instruction instr, [MaybeNullWhen(false)] out FieldReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldfld)
			{
				FieldReference op = instr.Operand as FieldReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BD8 RID: 11224 RVA: 0x00091CF4 File Offset: 0x0008FEF4
		public static bool MatchLdfld(this Instruction instr, FieldReference value)
		{
			FieldReference v;
			return instr.MatchLdfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BD9 RID: 11225 RVA: 0x00091D14 File Offset: 0x0008FF14
		public static bool MatchLdfld(this Instruction instr, FieldInfo value)
		{
			FieldReference v;
			return instr.MatchLdfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BDA RID: 11226 RVA: 0x00091D34 File Offset: 0x0008FF34
		public static bool MatchLdfld(this Instruction instr, Type type, string name)
		{
			FieldReference v;
			return instr.MatchLdfld(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002BDB RID: 11227 RVA: 0x00091D55 File Offset: 0x0008FF55
		public static bool MatchLdfld<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchLdfld(typeof(T), name);
		}

		// Token: 0x06002BDC RID: 11228 RVA: 0x00091D68 File Offset: 0x0008FF68
		public static bool MatchLdfld(this Instruction instr, string typeFullName, string name)
		{
			FieldReference v;
			return instr.MatchLdfld(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002BDD RID: 11229 RVA: 0x00091D8C File Offset: 0x0008FF8C
		public static bool MatchLdflda(this Instruction instr, [MaybeNullWhen(false)] out FieldReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldflda)
			{
				FieldReference op = instr.Operand as FieldReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BDE RID: 11230 RVA: 0x00091DD0 File Offset: 0x0008FFD0
		public static bool MatchLdflda(this Instruction instr, FieldReference value)
		{
			FieldReference v;
			return instr.MatchLdflda(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BDF RID: 11231 RVA: 0x00091DF0 File Offset: 0x0008FFF0
		public static bool MatchLdflda(this Instruction instr, FieldInfo value)
		{
			FieldReference v;
			return instr.MatchLdflda(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BE0 RID: 11232 RVA: 0x00091E10 File Offset: 0x00090010
		public static bool MatchLdflda(this Instruction instr, Type type, string name)
		{
			FieldReference v;
			return instr.MatchLdflda(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002BE1 RID: 11233 RVA: 0x00091E31 File Offset: 0x00090031
		public static bool MatchLdflda<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchLdflda(typeof(T), name);
		}

		// Token: 0x06002BE2 RID: 11234 RVA: 0x00091E44 File Offset: 0x00090044
		public static bool MatchLdflda(this Instruction instr, string typeFullName, string name)
		{
			FieldReference v;
			return instr.MatchLdflda(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002BE3 RID: 11235 RVA: 0x00091E68 File Offset: 0x00090068
		public static bool MatchLdftn(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldftn)
			{
				MethodReference op = instr.Operand as MethodReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BE4 RID: 11236 RVA: 0x00091EAC File Offset: 0x000900AC
		public static bool MatchLdftn(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchLdftn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BE5 RID: 11237 RVA: 0x00091ECC File Offset: 0x000900CC
		public static bool MatchLdftn(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchLdftn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BE6 RID: 11238 RVA: 0x00091EEC File Offset: 0x000900EC
		public static bool MatchLdftn(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchLdftn(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002BE7 RID: 11239 RVA: 0x00091F0D File Offset: 0x0009010D
		public static bool MatchLdftn<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchLdftn(typeof(T), name);
		}

		// Token: 0x06002BE8 RID: 11240 RVA: 0x00091F20 File Offset: 0x00090120
		public static bool MatchLdftn(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchLdftn(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002BE9 RID: 11241 RVA: 0x00091F41 File Offset: 0x00090141
		public static bool MatchLdindI(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_I;
		}

		// Token: 0x06002BEA RID: 11242 RVA: 0x00091F5D File Offset: 0x0009015D
		public static bool MatchLdindI1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_I1;
		}

		// Token: 0x06002BEB RID: 11243 RVA: 0x00091F79 File Offset: 0x00090179
		public static bool MatchLdindI2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_I2;
		}

		// Token: 0x06002BEC RID: 11244 RVA: 0x00091F95 File Offset: 0x00090195
		public static bool MatchLdindI4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_I4;
		}

		// Token: 0x06002BED RID: 11245 RVA: 0x00091FB1 File Offset: 0x000901B1
		public static bool MatchLdindI8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_I8;
		}

		// Token: 0x06002BEE RID: 11246 RVA: 0x00091FCD File Offset: 0x000901CD
		public static bool MatchLdindR4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_R4;
		}

		// Token: 0x06002BEF RID: 11247 RVA: 0x00091FE9 File Offset: 0x000901E9
		public static bool MatchLdindR8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_R8;
		}

		// Token: 0x06002BF0 RID: 11248 RVA: 0x00092005 File Offset: 0x00090205
		public static bool MatchLdindRef(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_Ref;
		}

		// Token: 0x06002BF1 RID: 11249 RVA: 0x00092021 File Offset: 0x00090221
		public static bool MatchLdindU1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_U1;
		}

		// Token: 0x06002BF2 RID: 11250 RVA: 0x0009203D File Offset: 0x0009023D
		public static bool MatchLdindU2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_U2;
		}

		// Token: 0x06002BF3 RID: 11251 RVA: 0x00092059 File Offset: 0x00090259
		public static bool MatchLdindU4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldind_U4;
		}

		// Token: 0x06002BF4 RID: 11252 RVA: 0x00092075 File Offset: 0x00090275
		public static bool MatchLdlen(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldlen;
		}

		// Token: 0x06002BF5 RID: 11253 RVA: 0x00092091 File Offset: 0x00090291
		public static bool MatchLdloc0(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldloc_0;
		}

		// Token: 0x06002BF6 RID: 11254 RVA: 0x000920AD File Offset: 0x000902AD
		public static bool MatchLdloc1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldloc_1;
		}

		// Token: 0x06002BF7 RID: 11255 RVA: 0x000920C9 File Offset: 0x000902C9
		public static bool MatchLdloc2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldloc_2;
		}

		// Token: 0x06002BF8 RID: 11256 RVA: 0x000920E5 File Offset: 0x000902E5
		public static bool MatchLdloc3(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldloc_3;
		}

		// Token: 0x06002BF9 RID: 11257 RVA: 0x00092104 File Offset: 0x00090304
		public static bool MatchLdloc(this Instruction instr, int value)
		{
			int v;
			return instr.MatchLdloc(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BFA RID: 11258 RVA: 0x00092124 File Offset: 0x00090324
		public static bool MatchLdloc(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchLdloc(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BFB RID: 11259 RVA: 0x00092144 File Offset: 0x00090344
		public static bool MatchLdloca(this Instruction instr, int value)
		{
			int v;
			return instr.MatchLdloca(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BFC RID: 11260 RVA: 0x00092164 File Offset: 0x00090364
		public static bool MatchLdloca(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchLdloca(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002BFD RID: 11261 RVA: 0x00092184 File Offset: 0x00090384
		public static bool MatchLdnull(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldnull;
		}

		// Token: 0x06002BFE RID: 11262 RVA: 0x000921A0 File Offset: 0x000903A0
		public static bool MatchLdobj(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldobj)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002BFF RID: 11263 RVA: 0x000921E4 File Offset: 0x000903E4
		public static bool MatchLdobj(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchLdobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C00 RID: 11264 RVA: 0x00092204 File Offset: 0x00090404
		public static bool MatchLdobj(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchLdobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C01 RID: 11265 RVA: 0x00092224 File Offset: 0x00090424
		public static bool MatchLdobj<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchLdobj(typeof(T));
		}

		// Token: 0x06002C02 RID: 11266 RVA: 0x00092238 File Offset: 0x00090438
		public static bool MatchLdobj(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchLdobj(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C03 RID: 11267 RVA: 0x00092258 File Offset: 0x00090458
		public static bool MatchLdsfld(this Instruction instr, [MaybeNullWhen(false)] out FieldReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldsfld)
			{
				FieldReference op = instr.Operand as FieldReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C04 RID: 11268 RVA: 0x0009229C File Offset: 0x0009049C
		public static bool MatchLdsfld(this Instruction instr, FieldReference value)
		{
			FieldReference v;
			return instr.MatchLdsfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C05 RID: 11269 RVA: 0x000922BC File Offset: 0x000904BC
		public static bool MatchLdsfld(this Instruction instr, FieldInfo value)
		{
			FieldReference v;
			return instr.MatchLdsfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C06 RID: 11270 RVA: 0x000922DC File Offset: 0x000904DC
		public static bool MatchLdsfld(this Instruction instr, Type type, string name)
		{
			FieldReference v;
			return instr.MatchLdsfld(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002C07 RID: 11271 RVA: 0x000922FD File Offset: 0x000904FD
		public static bool MatchLdsfld<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchLdsfld(typeof(T), name);
		}

		// Token: 0x06002C08 RID: 11272 RVA: 0x00092310 File Offset: 0x00090510
		public static bool MatchLdsfld(this Instruction instr, string typeFullName, string name)
		{
			FieldReference v;
			return instr.MatchLdsfld(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002C09 RID: 11273 RVA: 0x00092334 File Offset: 0x00090534
		public static bool MatchLdsflda(this Instruction instr, [MaybeNullWhen(false)] out FieldReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldsflda)
			{
				FieldReference op = instr.Operand as FieldReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C0A RID: 11274 RVA: 0x00092378 File Offset: 0x00090578
		public static bool MatchLdsflda(this Instruction instr, FieldReference value)
		{
			FieldReference v;
			return instr.MatchLdsflda(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C0B RID: 11275 RVA: 0x00092398 File Offset: 0x00090598
		public static bool MatchLdsflda(this Instruction instr, FieldInfo value)
		{
			FieldReference v;
			return instr.MatchLdsflda(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C0C RID: 11276 RVA: 0x000923B8 File Offset: 0x000905B8
		public static bool MatchLdsflda(this Instruction instr, Type type, string name)
		{
			FieldReference v;
			return instr.MatchLdsflda(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002C0D RID: 11277 RVA: 0x000923D9 File Offset: 0x000905D9
		public static bool MatchLdsflda<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchLdsflda(typeof(T), name);
		}

		// Token: 0x06002C0E RID: 11278 RVA: 0x000923EC File Offset: 0x000905EC
		public static bool MatchLdsflda(this Instruction instr, string typeFullName, string name)
		{
			FieldReference v;
			return instr.MatchLdsflda(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002C0F RID: 11279 RVA: 0x00092410 File Offset: 0x00090610
		public static bool MatchLdstr(this Instruction instr, [MaybeNullWhen(false)] out string value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldstr)
			{
				string op = instr.Operand as string;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C10 RID: 11280 RVA: 0x00092454 File Offset: 0x00090654
		public static bool MatchLdstr(this Instruction instr, string value)
		{
			string v;
			return instr.MatchLdstr(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C11 RID: 11281 RVA: 0x00092474 File Offset: 0x00090674
		public static bool MatchLdtoken(this Instruction instr, [MaybeNullWhen(false)] out IMetadataTokenProvider value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldtoken)
			{
				IMetadataTokenProvider op = instr.Operand as IMetadataTokenProvider;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C12 RID: 11282 RVA: 0x000924B8 File Offset: 0x000906B8
		public static bool MatchLdtoken(this Instruction instr, IMetadataTokenProvider value)
		{
			IMetadataTokenProvider v;
			return instr.MatchLdtoken(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C13 RID: 11283 RVA: 0x000924D8 File Offset: 0x000906D8
		public static bool MatchLdtoken(this Instruction instr, Type value)
		{
			IMetadataTokenProvider v;
			return instr.MatchLdtoken(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C14 RID: 11284 RVA: 0x000924F8 File Offset: 0x000906F8
		public static bool MatchLdtoken<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchLdtoken(typeof(T));
		}

		// Token: 0x06002C15 RID: 11285 RVA: 0x0009250C File Offset: 0x0009070C
		public static bool MatchLdtoken(this Instruction instr, FieldInfo value)
		{
			IMetadataTokenProvider v;
			return instr.MatchLdtoken(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C16 RID: 11286 RVA: 0x0009252C File Offset: 0x0009072C
		public static bool MatchLdtoken(this Instruction instr, MethodBase value)
		{
			IMetadataTokenProvider v;
			return instr.MatchLdtoken(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C17 RID: 11287 RVA: 0x0009254C File Offset: 0x0009074C
		public static bool MatchLdvirtftn(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ldvirtftn)
			{
				MethodReference op = instr.Operand as MethodReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C18 RID: 11288 RVA: 0x00092590 File Offset: 0x00090790
		public static bool MatchLdvirtftn(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchLdvirtftn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C19 RID: 11289 RVA: 0x000925B0 File Offset: 0x000907B0
		public static bool MatchLdvirtftn(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchLdvirtftn(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C1A RID: 11290 RVA: 0x000925D0 File Offset: 0x000907D0
		public static bool MatchLdvirtftn(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchLdvirtftn(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002C1B RID: 11291 RVA: 0x000925F1 File Offset: 0x000907F1
		public static bool MatchLdvirtftn<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchLdvirtftn(typeof(T), name);
		}

		// Token: 0x06002C1C RID: 11292 RVA: 0x00092604 File Offset: 0x00090804
		public static bool MatchLdvirtftn(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchLdvirtftn(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002C1D RID: 11293 RVA: 0x00092628 File Offset: 0x00090828
		public static bool MatchLeave(this Instruction instr, [MaybeNullWhen(false)] out ILLabel value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Leave || instr.OpCode == OpCodes.Leave_S)
			{
				ILLabel op = instr.Operand as ILLabel;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C1E RID: 11294 RVA: 0x0009267C File Offset: 0x0009087C
		public static bool MatchLeave(this Instruction instr, ILLabel value)
		{
			ILLabel v;
			return instr.MatchLeave(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C1F RID: 11295 RVA: 0x0009269C File Offset: 0x0009089C
		public static bool MatchLeave(this Instruction instr, Instruction value)
		{
			ILLabel v;
			return instr.MatchLeave(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C20 RID: 11296 RVA: 0x000926BC File Offset: 0x000908BC
		public static bool MatchLocalloc(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Localloc;
		}

		// Token: 0x06002C21 RID: 11297 RVA: 0x000926D8 File Offset: 0x000908D8
		public static bool MatchMkrefany(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Mkrefany)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C22 RID: 11298 RVA: 0x0009271C File Offset: 0x0009091C
		public static bool MatchMkrefany(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchMkrefany(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C23 RID: 11299 RVA: 0x0009273C File Offset: 0x0009093C
		public static bool MatchMkrefany(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchMkrefany(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C24 RID: 11300 RVA: 0x0009275C File Offset: 0x0009095C
		public static bool MatchMkrefany<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchMkrefany(typeof(T));
		}

		// Token: 0x06002C25 RID: 11301 RVA: 0x00092770 File Offset: 0x00090970
		public static bool MatchMkrefany(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchMkrefany(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C26 RID: 11302 RVA: 0x00092790 File Offset: 0x00090990
		public static bool MatchMul(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Mul;
		}

		// Token: 0x06002C27 RID: 11303 RVA: 0x000927AC File Offset: 0x000909AC
		public static bool MatchMulOvf(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Mul_Ovf;
		}

		// Token: 0x06002C28 RID: 11304 RVA: 0x000927C8 File Offset: 0x000909C8
		public static bool MatchMulOvfUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Mul_Ovf_Un;
		}

		// Token: 0x06002C29 RID: 11305 RVA: 0x000927E4 File Offset: 0x000909E4
		public static bool MatchNeg(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Neg;
		}

		// Token: 0x06002C2A RID: 11306 RVA: 0x00092800 File Offset: 0x00090A00
		public static bool MatchNewarr(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Newarr)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C2B RID: 11307 RVA: 0x00092844 File Offset: 0x00090A44
		public static bool MatchNewarr(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchNewarr(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C2C RID: 11308 RVA: 0x00092864 File Offset: 0x00090A64
		public static bool MatchNewarr(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchNewarr(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C2D RID: 11309 RVA: 0x00092884 File Offset: 0x00090A84
		public static bool MatchNewarr<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchNewarr(typeof(T));
		}

		// Token: 0x06002C2E RID: 11310 RVA: 0x00092898 File Offset: 0x00090A98
		public static bool MatchNewarr(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchNewarr(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C2F RID: 11311 RVA: 0x000928B8 File Offset: 0x00090AB8
		public static bool MatchNewobj(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Newobj)
			{
				MethodReference op = instr.Operand as MethodReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C30 RID: 11312 RVA: 0x000928FC File Offset: 0x00090AFC
		public static bool MatchNewobj(this Instruction instr, MethodReference value)
		{
			MethodReference v;
			return instr.MatchNewobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C31 RID: 11313 RVA: 0x0009291C File Offset: 0x00090B1C
		public static bool MatchNewobj(this Instruction instr, MethodBase value)
		{
			MethodReference v;
			return instr.MatchNewobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C32 RID: 11314 RVA: 0x0009293C File Offset: 0x00090B3C
		public static bool MatchNewobj(this Instruction instr, Type type, string name)
		{
			MethodReference v;
			return instr.MatchNewobj(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002C33 RID: 11315 RVA: 0x0009295D File Offset: 0x00090B5D
		public static bool MatchNewobj<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchNewobj(typeof(T), name);
		}

		// Token: 0x06002C34 RID: 11316 RVA: 0x00092970 File Offset: 0x00090B70
		public static bool MatchNewobj(this Instruction instr, string typeFullName, string name)
		{
			MethodReference v;
			return instr.MatchNewobj(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002C35 RID: 11317 RVA: 0x00092991 File Offset: 0x00090B91
		public static bool MatchNop(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Nop;
		}

		// Token: 0x06002C36 RID: 11318 RVA: 0x000929AD File Offset: 0x00090BAD
		public static bool MatchNot(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Not;
		}

		// Token: 0x06002C37 RID: 11319 RVA: 0x000929C9 File Offset: 0x00090BC9
		public static bool MatchOr(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Or;
		}

		// Token: 0x06002C38 RID: 11320 RVA: 0x000929E5 File Offset: 0x00090BE5
		public static bool MatchPop(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Pop;
		}

		// Token: 0x06002C39 RID: 11321 RVA: 0x00092A01 File Offset: 0x00090C01
		public static bool MatchReadonly(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Readonly;
		}

		// Token: 0x06002C3A RID: 11322 RVA: 0x00092A1D File Offset: 0x00090C1D
		public static bool MatchRefanytype(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Refanytype;
		}

		// Token: 0x06002C3B RID: 11323 RVA: 0x00092A3C File Offset: 0x00090C3C
		public static bool MatchRefanyval(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Refanyval)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C3C RID: 11324 RVA: 0x00092A80 File Offset: 0x00090C80
		public static bool MatchRefanyval(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchRefanyval(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C3D RID: 11325 RVA: 0x00092AA0 File Offset: 0x00090CA0
		public static bool MatchRefanyval(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchRefanyval(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C3E RID: 11326 RVA: 0x00092AC0 File Offset: 0x00090CC0
		public static bool MatchRefanyval<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchRefanyval(typeof(T));
		}

		// Token: 0x06002C3F RID: 11327 RVA: 0x00092AD4 File Offset: 0x00090CD4
		public static bool MatchRefanyval(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchRefanyval(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C40 RID: 11328 RVA: 0x00092AF4 File Offset: 0x00090CF4
		public static bool MatchRem(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Rem;
		}

		// Token: 0x06002C41 RID: 11329 RVA: 0x00092B10 File Offset: 0x00090D10
		public static bool MatchRemUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Rem_Un;
		}

		// Token: 0x06002C42 RID: 11330 RVA: 0x00092B2C File Offset: 0x00090D2C
		public static bool MatchRet(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Ret;
		}

		// Token: 0x06002C43 RID: 11331 RVA: 0x00092B48 File Offset: 0x00090D48
		public static bool MatchRethrow(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Rethrow;
		}

		// Token: 0x06002C44 RID: 11332 RVA: 0x00092B64 File Offset: 0x00090D64
		public static bool MatchShl(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Shl;
		}

		// Token: 0x06002C45 RID: 11333 RVA: 0x00092B80 File Offset: 0x00090D80
		public static bool MatchShr(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Shr;
		}

		// Token: 0x06002C46 RID: 11334 RVA: 0x00092B9C File Offset: 0x00090D9C
		public static bool MatchShrUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Shr_Un;
		}

		// Token: 0x06002C47 RID: 11335 RVA: 0x00092BB8 File Offset: 0x00090DB8
		public static bool MatchSizeof(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Sizeof)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C48 RID: 11336 RVA: 0x00092BFC File Offset: 0x00090DFC
		public static bool MatchSizeof(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchSizeof(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C49 RID: 11337 RVA: 0x00092C1C File Offset: 0x00090E1C
		public static bool MatchSizeof(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchSizeof(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C4A RID: 11338 RVA: 0x00092C3C File Offset: 0x00090E3C
		public static bool MatchSizeof<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchSizeof(typeof(T));
		}

		// Token: 0x06002C4B RID: 11339 RVA: 0x00092C50 File Offset: 0x00090E50
		public static bool MatchSizeof(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchSizeof(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C4C RID: 11340 RVA: 0x00092C70 File Offset: 0x00090E70
		public static bool MatchStarg(this Instruction instr, int value)
		{
			int v;
			return instr.MatchStarg(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C4D RID: 11341 RVA: 0x00092C90 File Offset: 0x00090E90
		public static bool MatchStarg(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchStarg(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C4E RID: 11342 RVA: 0x00092CB0 File Offset: 0x00090EB0
		public static bool MatchStelemAny(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_Any)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C4F RID: 11343 RVA: 0x00092CF4 File Offset: 0x00090EF4
		public static bool MatchStelemAny(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchStelemAny(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C50 RID: 11344 RVA: 0x00092D14 File Offset: 0x00090F14
		public static bool MatchStelemAny(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchStelemAny(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C51 RID: 11345 RVA: 0x00092D34 File Offset: 0x00090F34
		public static bool MatchStelemAny<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchStelemAny(typeof(T));
		}

		// Token: 0x06002C52 RID: 11346 RVA: 0x00092D48 File Offset: 0x00090F48
		public static bool MatchStelemAny(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchStelemAny(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C53 RID: 11347 RVA: 0x00092D68 File Offset: 0x00090F68
		public static bool MatchStelemI(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_I;
		}

		// Token: 0x06002C54 RID: 11348 RVA: 0x00092D84 File Offset: 0x00090F84
		public static bool MatchStelemI1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_I1;
		}

		// Token: 0x06002C55 RID: 11349 RVA: 0x00092DA0 File Offset: 0x00090FA0
		public static bool MatchStelemI2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_I2;
		}

		// Token: 0x06002C56 RID: 11350 RVA: 0x00092DBC File Offset: 0x00090FBC
		public static bool MatchStelemI4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_I4;
		}

		// Token: 0x06002C57 RID: 11351 RVA: 0x00092DD8 File Offset: 0x00090FD8
		public static bool MatchStelemI8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_I8;
		}

		// Token: 0x06002C58 RID: 11352 RVA: 0x00092DF4 File Offset: 0x00090FF4
		public static bool MatchStelemR4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_R4;
		}

		// Token: 0x06002C59 RID: 11353 RVA: 0x00092E10 File Offset: 0x00091010
		public static bool MatchStelemR8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_R8;
		}

		// Token: 0x06002C5A RID: 11354 RVA: 0x00092E2C File Offset: 0x0009102C
		public static bool MatchStelemRef(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stelem_Ref;
		}

		// Token: 0x06002C5B RID: 11355 RVA: 0x00092E48 File Offset: 0x00091048
		public static bool MatchStfld(this Instruction instr, [MaybeNullWhen(false)] out FieldReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stfld)
			{
				FieldReference op = instr.Operand as FieldReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C5C RID: 11356 RVA: 0x00092E8C File Offset: 0x0009108C
		public static bool MatchStfld(this Instruction instr, FieldReference value)
		{
			FieldReference v;
			return instr.MatchStfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C5D RID: 11357 RVA: 0x00092EAC File Offset: 0x000910AC
		public static bool MatchStfld(this Instruction instr, FieldInfo value)
		{
			FieldReference v;
			return instr.MatchStfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C5E RID: 11358 RVA: 0x00092ECC File Offset: 0x000910CC
		public static bool MatchStfld(this Instruction instr, Type type, string name)
		{
			FieldReference v;
			return instr.MatchStfld(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002C5F RID: 11359 RVA: 0x00092EED File Offset: 0x000910ED
		public static bool MatchStfld<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchStfld(typeof(T), name);
		}

		// Token: 0x06002C60 RID: 11360 RVA: 0x00092F00 File Offset: 0x00091100
		public static bool MatchStfld(this Instruction instr, string typeFullName, string name)
		{
			FieldReference v;
			return instr.MatchStfld(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002C61 RID: 11361 RVA: 0x00092F21 File Offset: 0x00091121
		public static bool MatchStindI(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_I;
		}

		// Token: 0x06002C62 RID: 11362 RVA: 0x00092F3D File Offset: 0x0009113D
		public static bool MatchStindI1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_I1;
		}

		// Token: 0x06002C63 RID: 11363 RVA: 0x00092F59 File Offset: 0x00091159
		public static bool MatchStindI2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_I2;
		}

		// Token: 0x06002C64 RID: 11364 RVA: 0x00092F75 File Offset: 0x00091175
		public static bool MatchStindI4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_I4;
		}

		// Token: 0x06002C65 RID: 11365 RVA: 0x00092F91 File Offset: 0x00091191
		public static bool MatchStindI8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_I8;
		}

		// Token: 0x06002C66 RID: 11366 RVA: 0x00092FAD File Offset: 0x000911AD
		public static bool MatchStindR4(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_R4;
		}

		// Token: 0x06002C67 RID: 11367 RVA: 0x00092FC9 File Offset: 0x000911C9
		public static bool MatchStindR8(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_R8;
		}

		// Token: 0x06002C68 RID: 11368 RVA: 0x00092FE5 File Offset: 0x000911E5
		public static bool MatchStindRef(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stind_Ref;
		}

		// Token: 0x06002C69 RID: 11369 RVA: 0x00093001 File Offset: 0x00091201
		public static bool MatchStloc0(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stloc_0;
		}

		// Token: 0x06002C6A RID: 11370 RVA: 0x0009301D File Offset: 0x0009121D
		public static bool MatchStloc1(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stloc_1;
		}

		// Token: 0x06002C6B RID: 11371 RVA: 0x00093039 File Offset: 0x00091239
		public static bool MatchStloc2(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stloc_2;
		}

		// Token: 0x06002C6C RID: 11372 RVA: 0x00093055 File Offset: 0x00091255
		public static bool MatchStloc3(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stloc_3;
		}

		// Token: 0x06002C6D RID: 11373 RVA: 0x00093074 File Offset: 0x00091274
		public static bool MatchStloc(this Instruction instr, int value)
		{
			int v;
			return instr.MatchStloc(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C6E RID: 11374 RVA: 0x00093094 File Offset: 0x00091294
		public static bool MatchStloc(this Instruction instr, uint value)
		{
			int v;
			return instr.MatchStloc(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C6F RID: 11375 RVA: 0x000930B4 File Offset: 0x000912B4
		public static bool MatchStobj(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stobj)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C70 RID: 11376 RVA: 0x000930F8 File Offset: 0x000912F8
		public static bool MatchStobj(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchStobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C71 RID: 11377 RVA: 0x00093118 File Offset: 0x00091318
		public static bool MatchStobj(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchStobj(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C72 RID: 11378 RVA: 0x00093138 File Offset: 0x00091338
		public static bool MatchStobj<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchStobj(typeof(T));
		}

		// Token: 0x06002C73 RID: 11379 RVA: 0x0009314C File Offset: 0x0009134C
		public static bool MatchStobj(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchStobj(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C74 RID: 11380 RVA: 0x0009316C File Offset: 0x0009136C
		public static bool MatchStsfld(this Instruction instr, [MaybeNullWhen(false)] out FieldReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Stsfld)
			{
				FieldReference op = instr.Operand as FieldReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C75 RID: 11381 RVA: 0x000931B0 File Offset: 0x000913B0
		public static bool MatchStsfld(this Instruction instr, FieldReference value)
		{
			FieldReference v;
			return instr.MatchStsfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C76 RID: 11382 RVA: 0x000931D0 File Offset: 0x000913D0
		public static bool MatchStsfld(this Instruction instr, FieldInfo value)
		{
			FieldReference v;
			return instr.MatchStsfld(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C77 RID: 11383 RVA: 0x000931F0 File Offset: 0x000913F0
		public static bool MatchStsfld(this Instruction instr, Type type, string name)
		{
			FieldReference v;
			return instr.MatchStsfld(out v) && ILPatternMatchingExt.IsEquivalent(v, type, name);
		}

		// Token: 0x06002C78 RID: 11384 RVA: 0x00093211 File Offset: 0x00091411
		public static bool MatchStsfld<[Nullable(2)] T>(this Instruction instr, string name)
		{
			return instr.MatchStsfld(typeof(T), name);
		}

		// Token: 0x06002C79 RID: 11385 RVA: 0x00093224 File Offset: 0x00091424
		public static bool MatchStsfld(this Instruction instr, string typeFullName, string name)
		{
			FieldReference v;
			return instr.MatchStsfld(out v) && v.Is(typeFullName, name);
		}

		// Token: 0x06002C7A RID: 11386 RVA: 0x00093245 File Offset: 0x00091445
		public static bool MatchSub(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Sub;
		}

		// Token: 0x06002C7B RID: 11387 RVA: 0x00093261 File Offset: 0x00091461
		public static bool MatchSubOvf(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Sub_Ovf;
		}

		// Token: 0x06002C7C RID: 11388 RVA: 0x0009327D File Offset: 0x0009147D
		public static bool MatchSubOvfUn(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Sub_Ovf_Un;
		}

		// Token: 0x06002C7D RID: 11389 RVA: 0x0009329C File Offset: 0x0009149C
		public static bool MatchSwitch(this Instruction instr, [MaybeNullWhen(false)] out ILLabel[] value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Switch)
			{
				ILLabel[] op = instr.Operand as ILLabel[];
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C7E RID: 11390 RVA: 0x000932E0 File Offset: 0x000914E0
		public static bool MatchSwitch(this Instruction instr, ILLabel[] value)
		{
			ILLabel[] v;
			return instr.MatchSwitch(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C7F RID: 11391 RVA: 0x00093300 File Offset: 0x00091500
		public static bool MatchSwitch(this Instruction instr, Instruction[] value)
		{
			ILLabel[] v;
			return instr.MatchSwitch(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C80 RID: 11392 RVA: 0x00093320 File Offset: 0x00091520
		public static bool MatchTail(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Tail;
		}

		// Token: 0x06002C81 RID: 11393 RVA: 0x0009333C File Offset: 0x0009153C
		public static bool MatchThrow(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Throw;
		}

		// Token: 0x06002C82 RID: 11394 RVA: 0x00093358 File Offset: 0x00091558
		public static bool MatchUnaligned(this Instruction instr, [MaybeNullWhen(false)] out byte value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Unaligned)
			{
				object operand = instr.Operand;
				if (operand is byte)
				{
					byte op = (byte)operand;
					value = op;
					return true;
				}
			}
			value = 0;
			return false;
		}

		// Token: 0x06002C83 RID: 11395 RVA: 0x000933A0 File Offset: 0x000915A0
		public static bool MatchUnaligned(this Instruction instr, byte value)
		{
			byte v;
			return instr.MatchUnaligned(out v) && ILPatternMatchingExt.IsEquivalent((int)v, (int)value);
		}

		// Token: 0x06002C84 RID: 11396 RVA: 0x000933C0 File Offset: 0x000915C0
		public static bool MatchUnbox(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Unbox)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C85 RID: 11397 RVA: 0x00093404 File Offset: 0x00091604
		public static bool MatchUnbox(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchUnbox(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C86 RID: 11398 RVA: 0x00093424 File Offset: 0x00091624
		public static bool MatchUnbox(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchUnbox(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C87 RID: 11399 RVA: 0x00093444 File Offset: 0x00091644
		public static bool MatchUnbox<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchUnbox(typeof(T));
		}

		// Token: 0x06002C88 RID: 11400 RVA: 0x00093458 File Offset: 0x00091658
		public static bool MatchUnbox(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchUnbox(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C89 RID: 11401 RVA: 0x00093478 File Offset: 0x00091678
		public static bool MatchUnboxAny(this Instruction instr, [MaybeNullWhen(false)] out TypeReference value)
		{
			if (Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Unbox_Any)
			{
				TypeReference op = instr.Operand as TypeReference;
				if (op != null)
				{
					value = op;
					return true;
				}
			}
			value = null;
			return false;
		}

		// Token: 0x06002C8A RID: 11402 RVA: 0x000934BC File Offset: 0x000916BC
		public static bool MatchUnboxAny(this Instruction instr, TypeReference value)
		{
			TypeReference v;
			return instr.MatchUnboxAny(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C8B RID: 11403 RVA: 0x000934DC File Offset: 0x000916DC
		public static bool MatchUnboxAny(this Instruction instr, Type value)
		{
			TypeReference v;
			return instr.MatchUnboxAny(out v) && ILPatternMatchingExt.IsEquivalent(v, value);
		}

		// Token: 0x06002C8C RID: 11404 RVA: 0x000934FC File Offset: 0x000916FC
		public static bool MatchUnboxAny<[Nullable(2)] T>(this Instruction instr)
		{
			return instr.MatchUnboxAny(typeof(T));
		}

		// Token: 0x06002C8D RID: 11405 RVA: 0x00093510 File Offset: 0x00091710
		public static bool MatchUnboxAny(this Instruction instr, string typeFullName)
		{
			TypeReference v;
			return instr.MatchUnboxAny(out v) && v.Is(typeFullName);
		}

		// Token: 0x06002C8E RID: 11406 RVA: 0x00093530 File Offset: 0x00091730
		public static bool MatchVolatile(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Volatile;
		}

		// Token: 0x06002C8F RID: 11407 RVA: 0x0009354C File Offset: 0x0009174C
		public static bool MatchXor(this Instruction instr)
		{
			return Helpers.ThrowIfNull<Instruction>(instr, "instr").OpCode == OpCodes.Xor;
		}

		// Token: 0x02000874 RID: 2164
		[Nullable(0)]
		private sealed class ParameterRefEqualityComparer : IEqualityComparer<ParameterReference>
		{
			// Token: 0x06002C90 RID: 11408 RVA: 0x00093568 File Offset: 0x00091768
			[NullableContext(2)]
			public bool Equals(ParameterReference x, ParameterReference y)
			{
				if (x == null)
				{
					return y == null;
				}
				return y != null && ILPatternMatchingExt.IsEquivalent(x.ParameterType, y.ParameterType);
			}

			// Token: 0x06002C91 RID: 11409 RVA: 0x00093588 File Offset: 0x00091788
			public int GetHashCode([DisallowNull] ParameterReference obj)
			{
				return obj.ParameterType.GetHashCode();
			}

			// Token: 0x04003A4C RID: 14924
			public static readonly ILPatternMatchingExt.ParameterRefEqualityComparer Instance = new ILPatternMatchingExt.ParameterRefEqualityComparer();
		}
	}
}
