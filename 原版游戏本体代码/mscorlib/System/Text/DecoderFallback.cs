using System;
using System.Threading;

namespace System.Text
{
	// Token: 0x02000A64 RID: 2660
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class DecoderFallback
	{
		// Token: 0x170011A5 RID: 4517
		// (get) Token: 0x060067A0 RID: 26528 RVA: 0x0015E1C0 File Offset: 0x0015C3C0
		private static object InternalSyncObject
		{
			get
			{
				if (DecoderFallback.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange<object>(ref DecoderFallback.s_InternalSyncObject, value, null);
				}
				return DecoderFallback.s_InternalSyncObject;
			}
		}

		// Token: 0x170011A6 RID: 4518
		// (get) Token: 0x060067A1 RID: 26529 RVA: 0x0015E1EC File Offset: 0x0015C3EC
		[__DynamicallyInvokable]
		public static DecoderFallback ReplacementFallback
		{
			[__DynamicallyInvokable]
			get
			{
				if (DecoderFallback.replacementFallback == null)
				{
					object internalSyncObject = DecoderFallback.InternalSyncObject;
					lock (internalSyncObject)
					{
						if (DecoderFallback.replacementFallback == null)
						{
							DecoderFallback.replacementFallback = new DecoderReplacementFallback();
						}
					}
				}
				return DecoderFallback.replacementFallback;
			}
		}

		// Token: 0x170011A7 RID: 4519
		// (get) Token: 0x060067A2 RID: 26530 RVA: 0x0015E24C File Offset: 0x0015C44C
		[__DynamicallyInvokable]
		public static DecoderFallback ExceptionFallback
		{
			[__DynamicallyInvokable]
			get
			{
				if (DecoderFallback.exceptionFallback == null)
				{
					object internalSyncObject = DecoderFallback.InternalSyncObject;
					lock (internalSyncObject)
					{
						if (DecoderFallback.exceptionFallback == null)
						{
							DecoderFallback.exceptionFallback = new DecoderExceptionFallback();
						}
					}
				}
				return DecoderFallback.exceptionFallback;
			}
		}

		// Token: 0x060067A3 RID: 26531
		[__DynamicallyInvokable]
		public abstract DecoderFallbackBuffer CreateFallbackBuffer();

		// Token: 0x170011A8 RID: 4520
		// (get) Token: 0x060067A4 RID: 26532
		[__DynamicallyInvokable]
		public abstract int MaxCharCount
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x170011A9 RID: 4521
		// (get) Token: 0x060067A5 RID: 26533 RVA: 0x0015E2AC File Offset: 0x0015C4AC
		internal bool IsMicrosoftBestFitFallback
		{
			get
			{
				return this.bIsMicrosoftBestFitFallback;
			}
		}

		// Token: 0x060067A6 RID: 26534 RVA: 0x0015E2B4 File Offset: 0x0015C4B4
		[__DynamicallyInvokable]
		protected DecoderFallback()
		{
		}

		// Token: 0x04002E4B RID: 11851
		internal bool bIsMicrosoftBestFitFallback;

		// Token: 0x04002E4C RID: 11852
		private static volatile DecoderFallback replacementFallback;

		// Token: 0x04002E4D RID: 11853
		private static volatile DecoderFallback exceptionFallback;

		// Token: 0x04002E4E RID: 11854
		private static object s_InternalSyncObject;
	}
}
