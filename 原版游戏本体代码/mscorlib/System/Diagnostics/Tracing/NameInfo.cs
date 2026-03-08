using System;
using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200044B RID: 1099
	internal sealed class NameInfo : ConcurrentSetItem<KeyValuePair<string, EventTags>, NameInfo>
	{
		// Token: 0x0600364B RID: 13899 RVA: 0x000D2F1C File Offset: 0x000D111C
		internal static void ReserveEventIDsBelow(int eventId)
		{
			int num;
			int num2;
			do
			{
				num = NameInfo.lastIdentity;
				num2 = (NameInfo.lastIdentity & -16777216) + eventId;
				num2 = Math.Max(num2, num);
			}
			while (Interlocked.CompareExchange(ref NameInfo.lastIdentity, num2, num) != num);
		}

		// Token: 0x0600364C RID: 13900 RVA: 0x000D2F54 File Offset: 0x000D1154
		public NameInfo(string name, EventTags tags, int typeMetadataSize)
		{
			this.name = name;
			this.tags = tags & (EventTags)268435455;
			this.identity = Interlocked.Increment(ref NameInfo.lastIdentity);
			int prefixSize = 0;
			Statics.EncodeTags((int)this.tags, ref prefixSize, null);
			this.nameMetadata = Statics.MetadataForString(name, prefixSize, 0, typeMetadataSize);
			prefixSize = 2;
			Statics.EncodeTags((int)this.tags, ref prefixSize, this.nameMetadata);
		}

		// Token: 0x0600364D RID: 13901 RVA: 0x000D2FBF File Offset: 0x000D11BF
		public override int Compare(NameInfo other)
		{
			return this.Compare(other.name, other.tags);
		}

		// Token: 0x0600364E RID: 13902 RVA: 0x000D2FD3 File Offset: 0x000D11D3
		public override int Compare(KeyValuePair<string, EventTags> key)
		{
			return this.Compare(key.Key, key.Value & (EventTags)268435455);
		}

		// Token: 0x0600364F RID: 13903 RVA: 0x000D2FF0 File Offset: 0x000D11F0
		private int Compare(string otherName, EventTags otherTags)
		{
			int num = StringComparer.Ordinal.Compare(this.name, otherName);
			if (num == 0 && this.tags != otherTags)
			{
				num = ((this.tags < otherTags) ? (-1) : 1);
			}
			return num;
		}

		// Token: 0x0400184C RID: 6220
		private static int lastIdentity = 184549376;

		// Token: 0x0400184D RID: 6221
		internal readonly string name;

		// Token: 0x0400184E RID: 6222
		internal readonly EventTags tags;

		// Token: 0x0400184F RID: 6223
		internal readonly int identity;

		// Token: 0x04001850 RID: 6224
		internal readonly byte[] nameMetadata;
	}
}
