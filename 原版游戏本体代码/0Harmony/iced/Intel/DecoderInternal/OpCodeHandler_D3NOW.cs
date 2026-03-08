using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C2 RID: 1730
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_D3NOW : OpCodeHandlerModRM
	{
		// Token: 0x0600245F RID: 9311 RVA: 0x00077830 File Offset: 0x00075A30
		private static Code[] CreateCodeValues()
		{
			Code[] array = new Code[256];
			array[191] = Code.D3NOW_Pavgusb_mm_mmm64;
			array[187] = Code.D3NOW_Pswapd_mm_mmm64;
			array[183] = Code.D3NOW_Pmulhrw_mm_mmm64;
			array[182] = Code.D3NOW_Pfrcpit2_mm_mmm64;
			array[180] = Code.D3NOW_Pfmul_mm_mmm64;
			array[176] = Code.D3NOW_Pfcmpeq_mm_mmm64;
			array[174] = Code.D3NOW_Pfacc_mm_mmm64;
			array[170] = Code.D3NOW_Pfsubr_mm_mmm64;
			array[167] = Code.D3NOW_Pfrsqit1_mm_mmm64;
			array[166] = Code.D3NOW_Pfrcpit1_mm_mmm64;
			array[164] = Code.D3NOW_Pfmax_mm_mmm64;
			array[160] = Code.D3NOW_Pfcmpgt_mm_mmm64;
			array[158] = Code.D3NOW_Pfadd_mm_mmm64;
			array[154] = Code.D3NOW_Pfsub_mm_mmm64;
			array[151] = Code.D3NOW_Pfrsqrt_mm_mmm64;
			array[150] = Code.D3NOW_Pfrcp_mm_mmm64;
			array[148] = Code.D3NOW_Pfmin_mm_mmm64;
			array[144] = Code.D3NOW_Pfcmpge_mm_mmm64;
			array[142] = Code.D3NOW_Pfpnacc_mm_mmm64;
			array[138] = Code.D3NOW_Pfnacc_mm_mmm64;
			array[135] = Code.D3NOW_Pfrsqrtv_mm_mmm64;
			array[134] = Code.D3NOW_Pfrcpv_mm_mmm64;
			array[29] = Code.D3NOW_Pf2id_mm_mmm64;
			array[28] = Code.D3NOW_Pf2iw_mm_mmm64;
			array[13] = Code.D3NOW_Pi2fd_mm_mmm64;
			array[12] = Code.D3NOW_Pi2fw_mm_mmm64;
			return array;
		}

		// Token: 0x06002460 RID: 9312 RVA: 0x00077974 File Offset: 0x00075B74
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op0Register = (int)decoder.state.reg + Register.MM0;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)decoder.state.rm + Register.MM0;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			Code code = this.codeValues[(int)decoder.ReadByte()];
			if (code - Code.D3NOW_Pfrcpv_mm_mmm64 <= 1 && ((decoder.options & DecoderOptions.Cyrix) == DecoderOptions.None || decoder.Bitness == 64))
			{
				code = Code.INVALID;
			}
			instruction.InternalSetCodeNoCheck(code);
			if (code == Code.INVALID)
			{
				decoder.SetInvalidInstruction();
			}
		}

		// Token: 0x040036A9 RID: 13993
		internal static readonly Code[] CodeValues = OpCodeHandler_D3NOW.CreateCodeValues();

		// Token: 0x040036AA RID: 13994
		private readonly Code[] codeValues = OpCodeHandler_D3NOW.CodeValues;
	}
}
