using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006BF RID: 1727
	internal static class OpCodeHandlersTables_Legacy
	{
		// Token: 0x06002459 RID: 9305 RVA: 0x00077724 File Offset: 0x00075924
		static OpCodeHandlersTables_Legacy()
		{
			LegacyOpCodeHandlerReader handlerReader = new LegacyOpCodeHandlerReader();
			TableDeserializer deserializer = new TableDeserializer(handlerReader, 82, OpCodeHandlersTables_Legacy.GetSerializedTables());
			deserializer.Deserialize();
			OpCodeHandlersTables_Legacy.Handlers_MAP0 = deserializer.GetTable(81U);
		}

		// Token: 0x0600245A RID: 9306 RVA: 0x0007775B File Offset: 0x0007595B
		private unsafe static ReadOnlySpan<byte> GetSerializedTables()
		{
			return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.626B29EAFFA7CEB2E50FEB451C5916CC67638E9FBCCC6EB9056377A2DEE5A09E), 6392);
		}

		// Token: 0x04003697 RID: 13975
		[Nullable(1)]
		internal static readonly OpCodeHandler[] Handlers_MAP0;

		// Token: 0x04003698 RID: 13976
		private const int MaxIdNames = 82;

		// Token: 0x04003699 RID: 13977
		private const uint Handlers_MAP0Index = 81U;
	}
}
