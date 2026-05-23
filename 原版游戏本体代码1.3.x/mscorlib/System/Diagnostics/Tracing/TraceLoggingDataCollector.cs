using System;
using System.Security;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000481 RID: 1153
	[SecuritySafeCritical]
	internal class TraceLoggingDataCollector
	{
		// Token: 0x06003714 RID: 14100 RVA: 0x000D47D0 File Offset: 0x000D29D0
		private TraceLoggingDataCollector()
		{
		}

		// Token: 0x06003715 RID: 14101 RVA: 0x000D47D8 File Offset: 0x000D29D8
		public int BeginBufferedArray()
		{
			return DataCollector.ThreadInstance.BeginBufferedArray();
		}

		// Token: 0x06003716 RID: 14102 RVA: 0x000D47E4 File Offset: 0x000D29E4
		public void EndBufferedArray(int bookmark, int count)
		{
			DataCollector.ThreadInstance.EndBufferedArray(bookmark, count);
		}

		// Token: 0x06003717 RID: 14103 RVA: 0x000D47F2 File Offset: 0x000D29F2
		public TraceLoggingDataCollector AddGroup()
		{
			return this;
		}

		// Token: 0x06003718 RID: 14104 RVA: 0x000D47F5 File Offset: 0x000D29F5
		public unsafe void AddScalar(bool value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 1);
		}

		// Token: 0x06003719 RID: 14105 RVA: 0x000D4805 File Offset: 0x000D2A05
		public unsafe void AddScalar(sbyte value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 1);
		}

		// Token: 0x0600371A RID: 14106 RVA: 0x000D4815 File Offset: 0x000D2A15
		public unsafe void AddScalar(byte value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 1);
		}

		// Token: 0x0600371B RID: 14107 RVA: 0x000D4825 File Offset: 0x000D2A25
		public unsafe void AddScalar(short value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 2);
		}

		// Token: 0x0600371C RID: 14108 RVA: 0x000D4835 File Offset: 0x000D2A35
		public unsafe void AddScalar(ushort value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 2);
		}

		// Token: 0x0600371D RID: 14109 RVA: 0x000D4845 File Offset: 0x000D2A45
		public unsafe void AddScalar(int value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 4);
		}

		// Token: 0x0600371E RID: 14110 RVA: 0x000D4855 File Offset: 0x000D2A55
		public unsafe void AddScalar(uint value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 4);
		}

		// Token: 0x0600371F RID: 14111 RVA: 0x000D4865 File Offset: 0x000D2A65
		public unsafe void AddScalar(long value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 8);
		}

		// Token: 0x06003720 RID: 14112 RVA: 0x000D4875 File Offset: 0x000D2A75
		public unsafe void AddScalar(ulong value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 8);
		}

		// Token: 0x06003721 RID: 14113 RVA: 0x000D4885 File Offset: 0x000D2A85
		public unsafe void AddScalar(IntPtr value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), IntPtr.Size);
		}

		// Token: 0x06003722 RID: 14114 RVA: 0x000D4899 File Offset: 0x000D2A99
		public unsafe void AddScalar(UIntPtr value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), UIntPtr.Size);
		}

		// Token: 0x06003723 RID: 14115 RVA: 0x000D48AD File Offset: 0x000D2AAD
		public unsafe void AddScalar(float value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 4);
		}

		// Token: 0x06003724 RID: 14116 RVA: 0x000D48BD File Offset: 0x000D2ABD
		public unsafe void AddScalar(double value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 8);
		}

		// Token: 0x06003725 RID: 14117 RVA: 0x000D48CD File Offset: 0x000D2ACD
		public unsafe void AddScalar(char value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 2);
		}

		// Token: 0x06003726 RID: 14118 RVA: 0x000D48DD File Offset: 0x000D2ADD
		public unsafe void AddScalar(Guid value)
		{
			DataCollector.ThreadInstance.AddScalar((void*)(&value), 16);
		}

		// Token: 0x06003727 RID: 14119 RVA: 0x000D48EE File Offset: 0x000D2AEE
		public void AddBinary(string value)
		{
			DataCollector.ThreadInstance.AddBinary(value, (value == null) ? 0 : (value.Length * 2));
		}

		// Token: 0x06003728 RID: 14120 RVA: 0x000D4909 File Offset: 0x000D2B09
		public void AddBinary(byte[] value)
		{
			DataCollector.ThreadInstance.AddBinary(value, (value == null) ? 0 : value.Length);
		}

		// Token: 0x06003729 RID: 14121 RVA: 0x000D491F File Offset: 0x000D2B1F
		public void AddArray(bool[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 1);
		}

		// Token: 0x0600372A RID: 14122 RVA: 0x000D4936 File Offset: 0x000D2B36
		public void AddArray(sbyte[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 1);
		}

		// Token: 0x0600372B RID: 14123 RVA: 0x000D494D File Offset: 0x000D2B4D
		public void AddArray(short[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 2);
		}

		// Token: 0x0600372C RID: 14124 RVA: 0x000D4964 File Offset: 0x000D2B64
		public void AddArray(ushort[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 2);
		}

		// Token: 0x0600372D RID: 14125 RVA: 0x000D497B File Offset: 0x000D2B7B
		public void AddArray(int[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 4);
		}

		// Token: 0x0600372E RID: 14126 RVA: 0x000D4992 File Offset: 0x000D2B92
		public void AddArray(uint[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 4);
		}

		// Token: 0x0600372F RID: 14127 RVA: 0x000D49A9 File Offset: 0x000D2BA9
		public void AddArray(long[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 8);
		}

		// Token: 0x06003730 RID: 14128 RVA: 0x000D49C0 File Offset: 0x000D2BC0
		public void AddArray(ulong[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 8);
		}

		// Token: 0x06003731 RID: 14129 RVA: 0x000D49D7 File Offset: 0x000D2BD7
		public void AddArray(IntPtr[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, IntPtr.Size);
		}

		// Token: 0x06003732 RID: 14130 RVA: 0x000D49F2 File Offset: 0x000D2BF2
		public void AddArray(UIntPtr[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, UIntPtr.Size);
		}

		// Token: 0x06003733 RID: 14131 RVA: 0x000D4A0D File Offset: 0x000D2C0D
		public void AddArray(float[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 4);
		}

		// Token: 0x06003734 RID: 14132 RVA: 0x000D4A24 File Offset: 0x000D2C24
		public void AddArray(double[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 8);
		}

		// Token: 0x06003735 RID: 14133 RVA: 0x000D4A3B File Offset: 0x000D2C3B
		public void AddArray(char[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 2);
		}

		// Token: 0x06003736 RID: 14134 RVA: 0x000D4A52 File Offset: 0x000D2C52
		public void AddArray(Guid[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 16);
		}

		// Token: 0x06003737 RID: 14135 RVA: 0x000D4A6A File Offset: 0x000D2C6A
		public void AddCustom(byte[] value)
		{
			DataCollector.ThreadInstance.AddArray(value, (value == null) ? 0 : value.Length, 1);
		}

		// Token: 0x0400186E RID: 6254
		internal static readonly TraceLoggingDataCollector Instance = new TraceLoggingDataCollector();
	}
}
