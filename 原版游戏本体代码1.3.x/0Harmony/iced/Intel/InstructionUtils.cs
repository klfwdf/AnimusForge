using System;

namespace Iced.Intel
{
	// Token: 0x02000658 RID: 1624
	internal static class InstructionUtils
	{
		// Token: 0x0600235B RID: 9051 RVA: 0x000725B4 File Offset: 0x000707B4
		public static int GetAddressSizeInBytes(Register baseReg, Register indexReg, int displSize, CodeSize codeSize)
		{
			if ((Register.RAX <= baseReg && baseReg <= Register.R15) || (Register.RAX <= indexReg && indexReg <= Register.R15) || baseReg == Register.RIP)
			{
				return 8;
			}
			if ((Register.EAX <= baseReg && baseReg <= Register.R15D) || (Register.EAX <= indexReg && indexReg <= Register.R15D) || baseReg == Register.EIP)
			{
				return 4;
			}
			if ((Register.AX <= baseReg && baseReg <= Register.DI) || (Register.AX <= indexReg && indexReg <= Register.DI))
			{
				return 2;
			}
			if (displSize == 2 || displSize == 4 || displSize == 8)
			{
				return displSize;
			}
			int result;
			switch (codeSize)
			{
			case CodeSize.Code16:
				result = 2;
				break;
			case CodeSize.Code32:
				result = 4;
				break;
			case CodeSize.Code64:
				result = 8;
				break;
			default:
				result = 8;
				break;
			}
			return result;
		}
	}
}
