using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000645 RID: 1605
	internal static class IcedConstants
	{
		// Token: 0x0600217C RID: 8572 RVA: 0x0001B69F File Offset: 0x0001989F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsMvex(Code code)
		{
			return false;
		}

		// Token: 0x04002A87 RID: 10887
		internal const int MaxOpCount = 5;

		// Token: 0x04002A88 RID: 10888
		internal const int MaxInstructionLength = 15;

		// Token: 0x04002A89 RID: 10889
		internal const int RegisterBits = 8;

		// Token: 0x04002A8A RID: 10890
		internal const Register VMM_first = Register.ZMM0;

		// Token: 0x04002A8B RID: 10891
		internal const Register VMM_last = Register.ZMM31;

		// Token: 0x04002A8C RID: 10892
		internal const int VMM_count = 32;

		// Token: 0x04002A8D RID: 10893
		internal const Register XMM_last = Register.XMM31;

		// Token: 0x04002A8E RID: 10894
		internal const Register YMM_last = Register.YMM31;

		// Token: 0x04002A8F RID: 10895
		internal const Register ZMM_last = Register.ZMM31;

		// Token: 0x04002A90 RID: 10896
		internal const Register TMM_last = Register.TMM7;

		// Token: 0x04002A91 RID: 10897
		internal const int MaxCpuidFeatureInternalValues = 199;

		// Token: 0x04002A92 RID: 10898
		internal const MemorySize FirstBroadcastMemorySize = MemorySize.Broadcast32_Float16;

		// Token: 0x04002A93 RID: 10899
		internal const uint MvexStart = 4611U;

		// Token: 0x04002A94 RID: 10900
		internal const uint MvexLength = 207U;

		// Token: 0x04002A95 RID: 10901
		internal const int CC_a_EnumCount = 2;

		// Token: 0x04002A96 RID: 10902
		internal const int CC_ae_EnumCount = 3;

		// Token: 0x04002A97 RID: 10903
		internal const int CC_b_EnumCount = 3;

		// Token: 0x04002A98 RID: 10904
		internal const int CC_be_EnumCount = 2;

		// Token: 0x04002A99 RID: 10905
		internal const int CC_e_EnumCount = 2;

		// Token: 0x04002A9A RID: 10906
		internal const int CC_g_EnumCount = 2;

		// Token: 0x04002A9B RID: 10907
		internal const int CC_ge_EnumCount = 2;

		// Token: 0x04002A9C RID: 10908
		internal const int CC_l_EnumCount = 2;

		// Token: 0x04002A9D RID: 10909
		internal const int CC_le_EnumCount = 2;

		// Token: 0x04002A9E RID: 10910
		internal const int CC_ne_EnumCount = 2;

		// Token: 0x04002A9F RID: 10911
		internal const int CC_np_EnumCount = 2;

		// Token: 0x04002AA0 RID: 10912
		internal const int CC_p_EnumCount = 2;

		// Token: 0x04002AA1 RID: 10913
		internal const int CodeEnumCount = 4936;

		// Token: 0x04002AA2 RID: 10914
		internal const int CodeSizeEnumCount = 4;

		// Token: 0x04002AA3 RID: 10915
		internal const int ConditionCodeEnumCount = 17;

		// Token: 0x04002AA4 RID: 10916
		internal const int CpuidFeatureEnumCount = 178;

		// Token: 0x04002AA5 RID: 10917
		internal const int DecoderErrorEnumCount = 3;

		// Token: 0x04002AA6 RID: 10918
		internal const int DecoratorKindEnumCount = 6;

		// Token: 0x04002AA7 RID: 10919
		internal const int EncodingKindEnumCount = 6;

		// Token: 0x04002AA8 RID: 10920
		internal const int FlowControlEnumCount = 10;

		// Token: 0x04002AA9 RID: 10921
		internal const int FormatterSyntaxEnumCount = 4;

		// Token: 0x04002AAA RID: 10922
		internal const int FormatterTextKindEnumCount = 16;

		// Token: 0x04002AAB RID: 10923
		internal const int MandatoryPrefixEnumCount = 5;

		// Token: 0x04002AAC RID: 10924
		internal const int MemorySizeEnumCount = 162;

		// Token: 0x04002AAD RID: 10925
		internal const int MemorySizeOptionsEnumCount = 4;

		// Token: 0x04002AAE RID: 10926
		internal const int MnemonicEnumCount = 1894;

		// Token: 0x04002AAF RID: 10927
		internal const int MvexConvFnEnumCount = 13;

		// Token: 0x04002AB0 RID: 10928
		internal const int MvexEHBitEnumCount = 3;

		// Token: 0x04002AB1 RID: 10929
		internal const int MvexRegMemConvEnumCount = 17;

		// Token: 0x04002AB2 RID: 10930
		internal const int MvexTupleTypeLutKindEnumCount = 14;

		// Token: 0x04002AB3 RID: 10931
		internal const int NumberBaseEnumCount = 4;

		// Token: 0x04002AB4 RID: 10932
		internal const int NumberKindEnumCount = 8;

		// Token: 0x04002AB5 RID: 10933
		internal const int OpAccessEnumCount = 8;

		// Token: 0x04002AB6 RID: 10934
		internal const int OpCodeOperandKindEnumCount = 109;

		// Token: 0x04002AB7 RID: 10935
		internal const int OpCodeTableKindEnumCount = 9;

		// Token: 0x04002AB8 RID: 10936
		internal const int OpKindEnumCount = 25;

		// Token: 0x04002AB9 RID: 10937
		internal const int PrefixKindEnumCount = 18;

		// Token: 0x04002ABA RID: 10938
		internal const int RegisterEnumCount = 256;

		// Token: 0x04002ABB RID: 10939
		internal const int RelocKindEnumCount = 1;

		// Token: 0x04002ABC RID: 10940
		internal const int RepPrefixKindEnumCount = 3;

		// Token: 0x04002ABD RID: 10941
		internal const int RoundingControlEnumCount = 5;

		// Token: 0x04002ABE RID: 10942
		internal const int TupleTypeEnumCount = 19;
	}
}
