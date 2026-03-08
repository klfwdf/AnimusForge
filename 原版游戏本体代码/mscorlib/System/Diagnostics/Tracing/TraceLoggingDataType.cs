using System;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000482 RID: 1154
	internal enum TraceLoggingDataType
	{
		// Token: 0x04001870 RID: 6256
		Nil,
		// Token: 0x04001871 RID: 6257
		Utf16String,
		// Token: 0x04001872 RID: 6258
		MbcsString,
		// Token: 0x04001873 RID: 6259
		Int8,
		// Token: 0x04001874 RID: 6260
		UInt8,
		// Token: 0x04001875 RID: 6261
		Int16,
		// Token: 0x04001876 RID: 6262
		UInt16,
		// Token: 0x04001877 RID: 6263
		Int32,
		// Token: 0x04001878 RID: 6264
		UInt32,
		// Token: 0x04001879 RID: 6265
		Int64,
		// Token: 0x0400187A RID: 6266
		UInt64,
		// Token: 0x0400187B RID: 6267
		Float,
		// Token: 0x0400187C RID: 6268
		Double,
		// Token: 0x0400187D RID: 6269
		Boolean32,
		// Token: 0x0400187E RID: 6270
		Binary,
		// Token: 0x0400187F RID: 6271
		Guid,
		// Token: 0x04001880 RID: 6272
		FileTime = 17,
		// Token: 0x04001881 RID: 6273
		SystemTime,
		// Token: 0x04001882 RID: 6274
		HexInt32 = 20,
		// Token: 0x04001883 RID: 6275
		HexInt64,
		// Token: 0x04001884 RID: 6276
		CountedUtf16String,
		// Token: 0x04001885 RID: 6277
		CountedMbcsString,
		// Token: 0x04001886 RID: 6278
		Struct,
		// Token: 0x04001887 RID: 6279
		Char16 = 518,
		// Token: 0x04001888 RID: 6280
		Char8 = 516,
		// Token: 0x04001889 RID: 6281
		Boolean8 = 772,
		// Token: 0x0400188A RID: 6282
		HexInt8 = 1028,
		// Token: 0x0400188B RID: 6283
		HexInt16 = 1030,
		// Token: 0x0400188C RID: 6284
		Utf16Xml = 2817,
		// Token: 0x0400188D RID: 6285
		MbcsXml,
		// Token: 0x0400188E RID: 6286
		CountedUtf16Xml = 2838,
		// Token: 0x0400188F RID: 6287
		CountedMbcsXml,
		// Token: 0x04001890 RID: 6288
		Utf16Json = 3073,
		// Token: 0x04001891 RID: 6289
		MbcsJson,
		// Token: 0x04001892 RID: 6290
		CountedUtf16Json = 3094,
		// Token: 0x04001893 RID: 6291
		CountedMbcsJson,
		// Token: 0x04001894 RID: 6292
		HResult = 3847
	}
}
