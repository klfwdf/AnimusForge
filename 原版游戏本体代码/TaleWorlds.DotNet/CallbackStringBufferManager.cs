using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000005 RID: 5
	public static class CallbackStringBufferManager
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000021F7 File Offset: 0x000003F7
		public static byte[] StringBuffer0
		{
			get
			{
				byte[] result;
				if ((result = CallbackStringBufferManager._stringBuffer0) == null)
				{
					result = (CallbackStringBufferManager._stringBuffer0 = new byte[1024]);
				}
				return result;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000009 RID: 9 RVA: 0x00002212 File Offset: 0x00000412
		public static byte[] StringBuffer1
		{
			get
			{
				byte[] result;
				if ((result = CallbackStringBufferManager._stringBuffer1) == null)
				{
					result = (CallbackStringBufferManager._stringBuffer1 = new byte[1024]);
				}
				return result;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000A RID: 10 RVA: 0x0000222D File Offset: 0x0000042D
		public static byte[] StringBuffer2
		{
			get
			{
				byte[] result;
				if ((result = CallbackStringBufferManager._stringBuffer2) == null)
				{
					result = (CallbackStringBufferManager._stringBuffer2 = new byte[1024]);
				}
				return result;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000B RID: 11 RVA: 0x00002248 File Offset: 0x00000448
		public static byte[] StringBuffer3
		{
			get
			{
				byte[] result;
				if ((result = CallbackStringBufferManager._stringBuffer3) == null)
				{
					result = (CallbackStringBufferManager._stringBuffer3 = new byte[1024]);
				}
				return result;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000C RID: 12 RVA: 0x00002263 File Offset: 0x00000463
		public static byte[] StringBuffer4
		{
			get
			{
				byte[] result;
				if ((result = CallbackStringBufferManager._stringBuffer4) == null)
				{
					result = (CallbackStringBufferManager._stringBuffer4 = new byte[1024]);
				}
				return result;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000D RID: 13 RVA: 0x0000227E File Offset: 0x0000047E
		public static byte[] StringBuffer5
		{
			get
			{
				byte[] result;
				if ((result = CallbackStringBufferManager._stringBuffer5) == null)
				{
					result = (CallbackStringBufferManager._stringBuffer5 = new byte[1024]);
				}
				return result;
			}
		}

		// Token: 0x04000004 RID: 4
		internal const int CallbackStringBufferMaxSize = 1024;

		// Token: 0x04000005 RID: 5
		[ThreadStatic]
		private static byte[] _stringBuffer0;

		// Token: 0x04000006 RID: 6
		[ThreadStatic]
		private static byte[] _stringBuffer1;

		// Token: 0x04000007 RID: 7
		[ThreadStatic]
		private static byte[] _stringBuffer2;

		// Token: 0x04000008 RID: 8
		[ThreadStatic]
		private static byte[] _stringBuffer3;

		// Token: 0x04000009 RID: 9
		[ThreadStatic]
		private static byte[] _stringBuffer4;

		// Token: 0x0400000A RID: 10
		[ThreadStatic]
		private static byte[] _stringBuffer5;
	}
}
