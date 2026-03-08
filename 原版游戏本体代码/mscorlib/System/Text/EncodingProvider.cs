using System;
using System.Runtime.InteropServices;

namespace System.Text
{
	// Token: 0x02000A76 RID: 2678
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public abstract class EncodingProvider
	{
		// Token: 0x0600688E RID: 26766 RVA: 0x00161296 File Offset: 0x0015F496
		[__DynamicallyInvokable]
		public EncodingProvider()
		{
		}

		// Token: 0x0600688F RID: 26767
		[__DynamicallyInvokable]
		public abstract Encoding GetEncoding(string name);

		// Token: 0x06006890 RID: 26768
		[__DynamicallyInvokable]
		public abstract Encoding GetEncoding(int codepage);

		// Token: 0x06006891 RID: 26769 RVA: 0x001612A0 File Offset: 0x0015F4A0
		[__DynamicallyInvokable]
		public virtual Encoding GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
		{
			Encoding encoding = this.GetEncoding(name);
			if (encoding != null)
			{
				encoding = (Encoding)this.GetEncoding(name).Clone();
				encoding.EncoderFallback = encoderFallback;
				encoding.DecoderFallback = decoderFallback;
			}
			return encoding;
		}

		// Token: 0x06006892 RID: 26770 RVA: 0x001612DC File Offset: 0x0015F4DC
		[__DynamicallyInvokable]
		public virtual Encoding GetEncoding(int codepage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
		{
			Encoding encoding = this.GetEncoding(codepage);
			if (encoding != null)
			{
				encoding = (Encoding)this.GetEncoding(codepage).Clone();
				encoding.EncoderFallback = encoderFallback;
				encoding.DecoderFallback = decoderFallback;
			}
			return encoding;
		}

		// Token: 0x06006893 RID: 26771 RVA: 0x00161318 File Offset: 0x0015F518
		internal static void AddProvider(EncodingProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			object obj = EncodingProvider.s_InternalSyncObject;
			lock (obj)
			{
				if (EncodingProvider.s_providers == null)
				{
					EncodingProvider.s_providers = new EncodingProvider[] { provider };
				}
				else if (Array.IndexOf<EncodingProvider>(EncodingProvider.s_providers, provider) < 0)
				{
					EncodingProvider[] array = new EncodingProvider[EncodingProvider.s_providers.Length + 1];
					Array.Copy(EncodingProvider.s_providers, array, EncodingProvider.s_providers.Length);
					array[array.Length - 1] = provider;
					EncodingProvider.s_providers = array;
				}
			}
		}

		// Token: 0x06006894 RID: 26772 RVA: 0x001613C4 File Offset: 0x0015F5C4
		internal static Encoding GetEncodingFromProvider(int codepage)
		{
			if (EncodingProvider.s_providers == null)
			{
				return null;
			}
			EncodingProvider[] array = EncodingProvider.s_providers;
			foreach (EncodingProvider encodingProvider in array)
			{
				Encoding encoding = encodingProvider.GetEncoding(codepage);
				if (encoding != null)
				{
					return encoding;
				}
			}
			return null;
		}

		// Token: 0x06006895 RID: 26773 RVA: 0x0016140C File Offset: 0x0015F60C
		internal static Encoding GetEncodingFromProvider(string encodingName)
		{
			if (EncodingProvider.s_providers == null)
			{
				return null;
			}
			EncodingProvider[] array = EncodingProvider.s_providers;
			foreach (EncodingProvider encodingProvider in array)
			{
				Encoding encoding = encodingProvider.GetEncoding(encodingName);
				if (encoding != null)
				{
					return encoding;
				}
			}
			return null;
		}

		// Token: 0x06006896 RID: 26774 RVA: 0x00161454 File Offset: 0x0015F654
		internal static Encoding GetEncodingFromProvider(int codepage, EncoderFallback enc, DecoderFallback dec)
		{
			if (EncodingProvider.s_providers == null)
			{
				return null;
			}
			EncodingProvider[] array = EncodingProvider.s_providers;
			foreach (EncodingProvider encodingProvider in array)
			{
				Encoding encoding = encodingProvider.GetEncoding(codepage, enc, dec);
				if (encoding != null)
				{
					return encoding;
				}
			}
			return null;
		}

		// Token: 0x06006897 RID: 26775 RVA: 0x0016149C File Offset: 0x0015F69C
		internal static Encoding GetEncodingFromProvider(string encodingName, EncoderFallback enc, DecoderFallback dec)
		{
			if (EncodingProvider.s_providers == null)
			{
				return null;
			}
			EncodingProvider[] array = EncodingProvider.s_providers;
			foreach (EncodingProvider encodingProvider in array)
			{
				Encoding encoding = encodingProvider.GetEncoding(encodingName, enc, dec);
				if (encoding != null)
				{
					return encoding;
				}
			}
			return null;
		}

		// Token: 0x04002EB9 RID: 11961
		private static object s_InternalSyncObject = new object();

		// Token: 0x04002EBA RID: 11962
		private static volatile EncodingProvider[] s_providers;
	}
}
