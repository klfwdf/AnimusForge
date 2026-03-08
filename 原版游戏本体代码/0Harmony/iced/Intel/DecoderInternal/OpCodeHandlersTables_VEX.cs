using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C0 RID: 1728
	[NullableContext(1)]
	[Nullable(0)]
	internal static class OpCodeHandlersTables_VEX
	{
		// Token: 0x0600245B RID: 9307 RVA: 0x0007776C File Offset: 0x0007596C
		static OpCodeHandlersTables_VEX()
		{
			VexOpCodeHandlerReader handlerReader = new VexOpCodeHandlerReader();
			TableDeserializer deserializer = new TableDeserializer(handlerReader, 16, OpCodeHandlersTables_VEX.GetSerializedTables());
			deserializer.Deserialize();
			OpCodeHandlersTables_VEX.Handlers_0F = deserializer.GetTable(15U);
			OpCodeHandlersTables_VEX.Handlers_0F38 = deserializer.GetTable(12U);
			OpCodeHandlersTables_VEX.Handlers_0F3A = deserializer.GetTable(13U);
		}

		// Token: 0x0600245C RID: 9308 RVA: 0x000777BF File Offset: 0x000759BF
		private unsafe static ReadOnlySpan<byte> GetSerializedTables()
		{
			return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.B081B50E102A74FCF59603CD7779F68C5513793D1E1BF7D19655D078BBDC30A6), 6767);
		}

		// Token: 0x0400369A RID: 13978
		internal static readonly OpCodeHandler[] Handlers_0F;

		// Token: 0x0400369B RID: 13979
		internal static readonly OpCodeHandler[] Handlers_0F38;

		// Token: 0x0400369C RID: 13980
		internal static readonly OpCodeHandler[] Handlers_0F3A;

		// Token: 0x0400369D RID: 13981
		private const int MaxIdNames = 16;

		// Token: 0x0400369E RID: 13982
		private const uint Handlers_MAP0Index = 14U;

		// Token: 0x0400369F RID: 13983
		private const uint Handlers_0FIndex = 15U;

		// Token: 0x040036A0 RID: 13984
		private const uint Handlers_0F38Index = 12U;

		// Token: 0x040036A1 RID: 13985
		private const uint Handlers_0F3AIndex = 13U;
	}
}
