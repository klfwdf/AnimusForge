using System;
using System.Threading;

namespace System.Text
{
	// Token: 0x02000A6F RID: 2671
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class EncoderFallback
	{
		// Token: 0x170011BD RID: 4541
		// (get) Token: 0x06006800 RID: 26624 RVA: 0x0015F5AC File Offset: 0x0015D7AC
		private static object InternalSyncObject
		{
			get
			{
				if (EncoderFallback.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange<object>(ref EncoderFallback.s_InternalSyncObject, value, null);
				}
				return EncoderFallback.s_InternalSyncObject;
			}
		}

		// Token: 0x170011BE RID: 4542
		// (get) Token: 0x06006801 RID: 26625 RVA: 0x0015F5D8 File Offset: 0x0015D7D8
		[__DynamicallyInvokable]
		public static EncoderFallback ReplacementFallback
		{
			[__DynamicallyInvokable]
			get
			{
				if (EncoderFallback.replacementFallback == null)
				{
					object internalSyncObject = EncoderFallback.InternalSyncObject;
					lock (internalSyncObject)
					{
						if (EncoderFallback.replacementFallback == null)
						{
							EncoderFallback.replacementFallback = new EncoderReplacementFallback();
						}
					}
				}
				return EncoderFallback.replacementFallback;
			}
		}

		// Token: 0x170011BF RID: 4543
		// (get) Token: 0x06006802 RID: 26626 RVA: 0x0015F638 File Offset: 0x0015D838
		[__DynamicallyInvokable]
		public static EncoderFallback ExceptionFallback
		{
			[__DynamicallyInvokable]
			get
			{
				if (EncoderFallback.exceptionFallback == null)
				{
					object internalSyncObject = EncoderFallback.InternalSyncObject;
					lock (internalSyncObject)
					{
						if (EncoderFallback.exceptionFallback == null)
						{
							EncoderFallback.exceptionFallback = new EncoderExceptionFallback();
						}
					}
				}
				return EncoderFallback.exceptionFallback;
			}
		}

		// Token: 0x06006803 RID: 26627
		[__DynamicallyInvokable]
		public abstract EncoderFallbackBuffer CreateFallbackBuffer();

		// Token: 0x170011C0 RID: 4544
		// (get) Token: 0x06006804 RID: 26628
		[__DynamicallyInvokable]
		public abstract int MaxCharCount
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06006805 RID: 26629 RVA: 0x0015F698 File Offset: 0x0015D898
		[__DynamicallyInvokable]
		protected EncoderFallback()
		{
		}

		// Token: 0x04002E67 RID: 11879
		internal bool bIsMicrosoftBestFitFallback;

		// Token: 0x04002E68 RID: 11880
		private static volatile EncoderFallback replacementFallback;

		// Token: 0x04002E69 RID: 11881
		private static volatile EncoderFallback exceptionFallback;

		// Token: 0x04002E6A RID: 11882
		private static object s_InternalSyncObject;
	}
}
