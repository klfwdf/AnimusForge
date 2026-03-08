using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006BE RID: 1726
	[NullableContext(1)]
	[Nullable(0)]
	internal static class OpCodeHandlersTables_EVEX
	{
		// Token: 0x06002457 RID: 9303 RVA: 0x000776A8 File Offset: 0x000758A8
		static OpCodeHandlersTables_EVEX()
		{
			EvexOpCodeHandlerReader handlerReader = new EvexOpCodeHandlerReader();
			TableDeserializer deserializer = new TableDeserializer(handlerReader, 10, OpCodeHandlersTables_EVEX.GetSerializedTables());
			deserializer.Deserialize();
			OpCodeHandlersTables_EVEX.Handlers_0F = deserializer.GetTable(9U);
			OpCodeHandlersTables_EVEX.Handlers_0F38 = deserializer.GetTable(5U);
			OpCodeHandlersTables_EVEX.Handlers_0F3A = deserializer.GetTable(6U);
			OpCodeHandlersTables_EVEX.Handlers_MAP5 = deserializer.GetTable(7U);
			OpCodeHandlersTables_EVEX.Handlers_MAP6 = deserializer.GetTable(8U);
		}

		// Token: 0x06002458 RID: 9304 RVA: 0x00077713 File Offset: 0x00075913
		private unsafe static ReadOnlySpan<byte> GetSerializedTables()
		{
			return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.7979334842423742EE34478883EFCF2BE7DC055D484045F7D6F6F6FDAF3D9C6B), 12430);
		}

		// Token: 0x0400368C RID: 13964
		internal static readonly OpCodeHandler[] Handlers_0F;

		// Token: 0x0400368D RID: 13965
		internal static readonly OpCodeHandler[] Handlers_0F38;

		// Token: 0x0400368E RID: 13966
		internal static readonly OpCodeHandler[] Handlers_0F3A;

		// Token: 0x0400368F RID: 13967
		internal static readonly OpCodeHandler[] Handlers_MAP5;

		// Token: 0x04003690 RID: 13968
		internal static readonly OpCodeHandler[] Handlers_MAP6;

		// Token: 0x04003691 RID: 13969
		private const int MaxIdNames = 10;

		// Token: 0x04003692 RID: 13970
		private const uint Handlers_0FIndex = 9U;

		// Token: 0x04003693 RID: 13971
		private const uint Handlers_0F38Index = 5U;

		// Token: 0x04003694 RID: 13972
		private const uint Handlers_0F3AIndex = 6U;

		// Token: 0x04003695 RID: 13973
		private const uint Handlers_MAP5Index = 7U;

		// Token: 0x04003696 RID: 13974
		private const uint Handlers_MAP6Index = 8U;
	}
}
