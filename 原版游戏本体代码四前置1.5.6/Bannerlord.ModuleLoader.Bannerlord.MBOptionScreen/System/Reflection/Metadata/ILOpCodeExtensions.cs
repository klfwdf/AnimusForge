namespace System.Reflection.Metadata;

internal static class ILOpCodeExtensions
{
	public static bool IsBranch(this ILOpCode opCode)
	{
		if (opCode - 43 <= ILOpCode.Ldc_i4_3 || opCode - 221 <= ILOpCode.Break)
		{
			return true;
		}
		return false;
	}

	public static int GetBranchOperandSize(this ILOpCode opCode)
	{
		switch (opCode)
		{
		case ILOpCode.Br_s:
		case ILOpCode.Brfalse_s:
		case ILOpCode.Brtrue_s:
		case ILOpCode.Beq_s:
		case ILOpCode.Bge_s:
		case ILOpCode.Bgt_s:
		case ILOpCode.Ble_s:
		case ILOpCode.Blt_s:
		case ILOpCode.Bne_un_s:
		case ILOpCode.Bge_un_s:
		case ILOpCode.Bgt_un_s:
		case ILOpCode.Ble_un_s:
		case ILOpCode.Blt_un_s:
		case ILOpCode.Leave_s:
			return 1;
		case ILOpCode.Br:
		case ILOpCode.Brfalse:
		case ILOpCode.Brtrue:
		case ILOpCode.Beq:
		case ILOpCode.Bge:
		case ILOpCode.Bgt:
		case ILOpCode.Ble:
		case ILOpCode.Blt:
		case ILOpCode.Bne_un:
		case ILOpCode.Bge_un:
		case ILOpCode.Bgt_un:
		case ILOpCode.Ble_un:
		case ILOpCode.Blt_un:
		case ILOpCode.Leave:
			return 4;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedOpCode, opCode), "opCode");
		}
	}

	public static ILOpCode GetShortBranch(this ILOpCode opCode)
	{
		switch (opCode)
		{
		case ILOpCode.Br_s:
		case ILOpCode.Brfalse_s:
		case ILOpCode.Brtrue_s:
		case ILOpCode.Beq_s:
		case ILOpCode.Bge_s:
		case ILOpCode.Bgt_s:
		case ILOpCode.Ble_s:
		case ILOpCode.Blt_s:
		case ILOpCode.Bne_un_s:
		case ILOpCode.Bge_un_s:
		case ILOpCode.Bgt_un_s:
		case ILOpCode.Ble_un_s:
		case ILOpCode.Blt_un_s:
		case ILOpCode.Leave_s:
			return opCode;
		case ILOpCode.Br:
			return ILOpCode.Br_s;
		case ILOpCode.Brfalse:
			return ILOpCode.Brfalse_s;
		case ILOpCode.Brtrue:
			return ILOpCode.Brtrue_s;
		case ILOpCode.Beq:
			return ILOpCode.Beq_s;
		case ILOpCode.Bge:
			return ILOpCode.Bge_s;
		case ILOpCode.Bgt:
			return ILOpCode.Bgt_s;
		case ILOpCode.Ble:
			return ILOpCode.Ble_s;
		case ILOpCode.Blt:
			return ILOpCode.Blt_s;
		case ILOpCode.Bne_un:
			return ILOpCode.Bne_un_s;
		case ILOpCode.Bge_un:
			return ILOpCode.Bge_un_s;
		case ILOpCode.Bgt_un:
			return ILOpCode.Bgt_un_s;
		case ILOpCode.Ble_un:
			return ILOpCode.Ble_un_s;
		case ILOpCode.Blt_un:
			return ILOpCode.Blt_un_s;
		case ILOpCode.Leave:
			return ILOpCode.Leave_s;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedOpCode, opCode), "opCode");
		}
	}

	public static ILOpCode GetLongBranch(this ILOpCode opCode)
	{
		switch (opCode)
		{
		case ILOpCode.Br:
		case ILOpCode.Brfalse:
		case ILOpCode.Brtrue:
		case ILOpCode.Beq:
		case ILOpCode.Bge:
		case ILOpCode.Bgt:
		case ILOpCode.Ble:
		case ILOpCode.Blt:
		case ILOpCode.Bne_un:
		case ILOpCode.Bge_un:
		case ILOpCode.Bgt_un:
		case ILOpCode.Ble_un:
		case ILOpCode.Blt_un:
		case ILOpCode.Leave:
			return opCode;
		case ILOpCode.Br_s:
			return ILOpCode.Br;
		case ILOpCode.Brfalse_s:
			return ILOpCode.Brfalse;
		case ILOpCode.Brtrue_s:
			return ILOpCode.Brtrue;
		case ILOpCode.Beq_s:
			return ILOpCode.Beq;
		case ILOpCode.Bge_s:
			return ILOpCode.Bge;
		case ILOpCode.Bgt_s:
			return ILOpCode.Bgt;
		case ILOpCode.Ble_s:
			return ILOpCode.Ble;
		case ILOpCode.Blt_s:
			return ILOpCode.Blt;
		case ILOpCode.Bne_un_s:
			return ILOpCode.Bne_un;
		case ILOpCode.Bge_un_s:
			return ILOpCode.Bge_un;
		case ILOpCode.Bgt_un_s:
			return ILOpCode.Bgt_un;
		case ILOpCode.Ble_un_s:
			return ILOpCode.Ble_un;
		case ILOpCode.Blt_un_s:
			return ILOpCode.Blt_un;
		case ILOpCode.Leave_s:
			return ILOpCode.Leave;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedOpCode, opCode), "opCode");
		}
	}
}
