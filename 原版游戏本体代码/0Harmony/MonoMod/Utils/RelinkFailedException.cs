using System;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;

namespace MonoMod.Utils
{
	// Token: 0x020008E5 RID: 2277
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	internal class RelinkFailedException : Exception
	{
		// Token: 0x1700085A RID: 2138
		// (get) Token: 0x06002F72 RID: 12146 RVA: 0x000A5286 File Offset: 0x000A3486
		public IMetadataTokenProvider MTP { get; }

		// Token: 0x1700085B RID: 2139
		// (get) Token: 0x06002F73 RID: 12147 RVA: 0x000A528E File Offset: 0x000A348E
		[Nullable(2)]
		public IMetadataTokenProvider Context
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x06002F74 RID: 12148 RVA: 0x000A5296 File Offset: 0x000A3496
		public RelinkFailedException(IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context = null)
			: this(RelinkFailedException.Format("MonoMod failed relinking", mtp, context), mtp, context)
		{
		}

		// Token: 0x06002F75 RID: 12149 RVA: 0x000A52AC File Offset: 0x000A34AC
		public RelinkFailedException(string message, IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context = null)
			: base(message)
		{
			this.MTP = mtp;
			this.Context = context;
		}

		// Token: 0x06002F76 RID: 12150 RVA: 0x000A52C3 File Offset: 0x000A34C3
		public RelinkFailedException(string message, Exception innerException, IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context = null)
			: base(message ?? RelinkFailedException.Format("MonoMod failed relinking", mtp, context), innerException)
		{
			this.MTP = mtp;
			this.Context = context;
		}

		// Token: 0x06002F77 RID: 12151 RVA: 0x000A52F0 File Offset: 0x000A34F0
		protected static string Format(string message, IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context)
		{
			if (mtp == null && context == null)
			{
				return message;
			}
			StringBuilder builder = new StringBuilder(message);
			builder.Append(' ');
			if (mtp != null)
			{
				builder.Append(mtp.ToString());
			}
			if (context != null)
			{
				builder.Append(' ');
			}
			if (context != null)
			{
				builder.Append("(context: ").Append(context.ToString()).Append(')');
			}
			return builder.ToString();
		}

		// Token: 0x04003BA9 RID: 15273
		public const string DefaultMessage = "MonoMod failed relinking";
	}
}
