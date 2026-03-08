using System;

namespace Iced.Intel
{
	// Token: 0x02000663 RID: 1635
	internal static class RegisterExtensions
	{
		// Token: 0x0600237F RID: 9087 RVA: 0x00072B86 File Offset: 0x00070D86
		public static bool IsSegmentRegister(this Register register)
		{
			return Register.ES <= register && register <= Register.GS;
		}

		// Token: 0x06002380 RID: 9088 RVA: 0x00072B97 File Offset: 0x00070D97
		public static bool IsGPR(this Register register)
		{
			return Register.AL <= register && register <= Register.R15;
		}

		// Token: 0x06002381 RID: 9089 RVA: 0x00072BA7 File Offset: 0x00070DA7
		public static bool IsGPR8(this Register register)
		{
			return Register.AL <= register && register <= Register.R15L;
		}

		// Token: 0x06002382 RID: 9090 RVA: 0x00072BB7 File Offset: 0x00070DB7
		public static bool IsGPR16(this Register register)
		{
			return Register.AX <= register && register <= Register.R15W;
		}

		// Token: 0x06002383 RID: 9091 RVA: 0x00072BC8 File Offset: 0x00070DC8
		public static bool IsGPR32(this Register register)
		{
			return Register.EAX <= register && register <= Register.R15D;
		}

		// Token: 0x06002384 RID: 9092 RVA: 0x00072BD9 File Offset: 0x00070DD9
		public static bool IsGPR64(this Register register)
		{
			return Register.RAX <= register && register <= Register.R15;
		}

		// Token: 0x06002385 RID: 9093 RVA: 0x00072BEA File Offset: 0x00070DEA
		public static bool IsXMM(this Register register)
		{
			return Register.XMM0 <= register && register <= Register.XMM31;
		}

		// Token: 0x06002386 RID: 9094 RVA: 0x00072BFB File Offset: 0x00070DFB
		public static bool IsYMM(this Register register)
		{
			return Register.YMM0 <= register && register <= Register.YMM31;
		}

		// Token: 0x06002387 RID: 9095 RVA: 0x00072C0F File Offset: 0x00070E0F
		public static bool IsZMM(this Register register)
		{
			return Register.ZMM0 <= register && register <= Register.ZMM31;
		}

		// Token: 0x06002388 RID: 9096 RVA: 0x00072C26 File Offset: 0x00070E26
		public static bool IsIP(this Register register)
		{
			return register == Register.EIP || register == Register.RIP;
		}

		// Token: 0x06002389 RID: 9097 RVA: 0x00072C34 File Offset: 0x00070E34
		public static bool IsK(this Register register)
		{
			return Register.K0 <= register && register <= Register.K7;
		}

		// Token: 0x0600238A RID: 9098 RVA: 0x00072C4B File Offset: 0x00070E4B
		public static bool IsCR(this Register register)
		{
			return Register.CR0 <= register && register <= Register.CR15;
		}

		// Token: 0x0600238B RID: 9099 RVA: 0x00072C62 File Offset: 0x00070E62
		public static bool IsDR(this Register register)
		{
			return Register.DR0 <= register && register <= Register.DR15;
		}

		// Token: 0x0600238C RID: 9100 RVA: 0x00072C79 File Offset: 0x00070E79
		public static bool IsTR(this Register register)
		{
			return Register.TR0 <= register && register <= Register.TR7;
		}

		// Token: 0x0600238D RID: 9101 RVA: 0x00072C90 File Offset: 0x00070E90
		public static bool IsST(this Register register)
		{
			return Register.ST0 <= register && register <= Register.ST7;
		}

		// Token: 0x0600238E RID: 9102 RVA: 0x00072CA7 File Offset: 0x00070EA7
		public static bool IsBND(this Register register)
		{
			return Register.BND0 <= register && register <= Register.BND3;
		}

		// Token: 0x0600238F RID: 9103 RVA: 0x00072CBE File Offset: 0x00070EBE
		public static bool IsMM(this Register register)
		{
			return Register.MM0 <= register && register <= Register.MM7;
		}

		// Token: 0x06002390 RID: 9104 RVA: 0x00072CD5 File Offset: 0x00070ED5
		public static bool IsTMM(this Register register)
		{
			return Register.TMM0 <= register && register <= Register.TMM7;
		}

		// Token: 0x06002391 RID: 9105 RVA: 0x00072CEC File Offset: 0x00070EEC
		public static bool IsVectorRegister(this Register register)
		{
			return Register.XMM0 <= register && register <= Register.ZMM31;
		}
	}
}
