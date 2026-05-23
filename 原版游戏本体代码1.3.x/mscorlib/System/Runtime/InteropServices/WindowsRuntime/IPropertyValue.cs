using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A04 RID: 2564
	[Guid("4bd682dd-7554-40e9-9a9b-82654ede7e62")]
	[ComImport]
	internal interface IPropertyValue
	{
		// Token: 0x1700116B RID: 4459
		// (get) Token: 0x06006553 RID: 25939
		PropertyType Type { get; }

		// Token: 0x1700116C RID: 4460
		// (get) Token: 0x06006554 RID: 25940
		bool IsNumericScalar { get; }

		// Token: 0x06006555 RID: 25941
		byte GetUInt8();

		// Token: 0x06006556 RID: 25942
		short GetInt16();

		// Token: 0x06006557 RID: 25943
		ushort GetUInt16();

		// Token: 0x06006558 RID: 25944
		int GetInt32();

		// Token: 0x06006559 RID: 25945
		uint GetUInt32();

		// Token: 0x0600655A RID: 25946
		long GetInt64();

		// Token: 0x0600655B RID: 25947
		ulong GetUInt64();

		// Token: 0x0600655C RID: 25948
		float GetSingle();

		// Token: 0x0600655D RID: 25949
		double GetDouble();

		// Token: 0x0600655E RID: 25950
		char GetChar16();

		// Token: 0x0600655F RID: 25951
		bool GetBoolean();

		// Token: 0x06006560 RID: 25952
		string GetString();

		// Token: 0x06006561 RID: 25953
		Guid GetGuid();

		// Token: 0x06006562 RID: 25954
		DateTimeOffset GetDateTime();

		// Token: 0x06006563 RID: 25955
		TimeSpan GetTimeSpan();

		// Token: 0x06006564 RID: 25956
		Point GetPoint();

		// Token: 0x06006565 RID: 25957
		Size GetSize();

		// Token: 0x06006566 RID: 25958
		Rect GetRect();

		// Token: 0x06006567 RID: 25959
		byte[] GetUInt8Array();

		// Token: 0x06006568 RID: 25960
		short[] GetInt16Array();

		// Token: 0x06006569 RID: 25961
		ushort[] GetUInt16Array();

		// Token: 0x0600656A RID: 25962
		int[] GetInt32Array();

		// Token: 0x0600656B RID: 25963
		uint[] GetUInt32Array();

		// Token: 0x0600656C RID: 25964
		long[] GetInt64Array();

		// Token: 0x0600656D RID: 25965
		ulong[] GetUInt64Array();

		// Token: 0x0600656E RID: 25966
		float[] GetSingleArray();

		// Token: 0x0600656F RID: 25967
		double[] GetDoubleArray();

		// Token: 0x06006570 RID: 25968
		char[] GetChar16Array();

		// Token: 0x06006571 RID: 25969
		bool[] GetBooleanArray();

		// Token: 0x06006572 RID: 25970
		string[] GetStringArray();

		// Token: 0x06006573 RID: 25971
		object[] GetInspectableArray();

		// Token: 0x06006574 RID: 25972
		Guid[] GetGuidArray();

		// Token: 0x06006575 RID: 25973
		DateTimeOffset[] GetDateTimeArray();

		// Token: 0x06006576 RID: 25974
		TimeSpan[] GetTimeSpanArray();

		// Token: 0x06006577 RID: 25975
		Point[] GetPointArray();

		// Token: 0x06006578 RID: 25976
		Size[] GetSizeArray();

		// Token: 0x06006579 RID: 25977
		Rect[] GetRectArray();
	}
}
