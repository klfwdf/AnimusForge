using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C1 RID: 1729
	[NullableContext(1)]
	[Nullable(0)]
	internal static class OpCodeHandlersTables_XOP
	{
		// Token: 0x0600245D RID: 9309 RVA: 0x000777D0 File Offset: 0x000759D0
		static OpCodeHandlersTables_XOP()
		{
			VexOpCodeHandlerReader handlerReader = new VexOpCodeHandlerReader();
			TableDeserializer deserializer = new TableDeserializer(handlerReader, 7, OpCodeHandlersTables_XOP.GetSerializedTables());
			deserializer.Deserialize();
			OpCodeHandlersTables_XOP.Handlers_MAP8 = deserializer.GetTable(4U);
			OpCodeHandlersTables_XOP.Handlers_MAP9 = deserializer.GetTable(5U);
			OpCodeHandlersTables_XOP.Handlers_MAP10 = deserializer.GetTable(6U);
		}

		// Token: 0x0600245E RID: 9310 RVA: 0x0007781F File Offset: 0x00075A1F
		private unsafe static ReadOnlySpan<byte> GetSerializedTables()
		{
			return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.2DC269275D4CFA9BCF8160F362F35004DDCE0867005D0BB1888BBD51488F59DE), 768);
		}

		// Token: 0x040036A2 RID: 13986
		internal static readonly OpCodeHandler[] Handlers_MAP8;

		// Token: 0x040036A3 RID: 13987
		internal static readonly OpCodeHandler[] Handlers_MAP9;

		// Token: 0x040036A4 RID: 13988
		internal static readonly OpCodeHandler[] Handlers_MAP10;

		// Token: 0x040036A5 RID: 13989
		private const int MaxIdNames = 7;

		// Token: 0x040036A6 RID: 13990
		private const uint Handlers_MAP8Index = 4U;

		// Token: 0x040036A7 RID: 13991
		private const uint Handlers_MAP9Index = 5U;

		// Token: 0x040036A8 RID: 13992
		private const uint Handlers_MAP10Index = 6U;
	}
}
