using System;
using System.Text;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000449 RID: 1097
	internal class FieldMetadata
	{
		// Token: 0x06003640 RID: 13888 RVA: 0x000D2ACA File Offset: 0x000D0CCA
		public FieldMetadata(string name, TraceLoggingDataType type, EventFieldTags tags, bool variableCount)
			: this(name, type, tags, variableCount ? 64 : 0, 0, null)
		{
		}

		// Token: 0x06003641 RID: 13889 RVA: 0x000D2AE0 File Offset: 0x000D0CE0
		public FieldMetadata(string name, TraceLoggingDataType type, EventFieldTags tags, ushort fixedCount)
			: this(name, type, tags, 32, fixedCount, null)
		{
		}

		// Token: 0x06003642 RID: 13890 RVA: 0x000D2AF0 File Offset: 0x000D0CF0
		public FieldMetadata(string name, TraceLoggingDataType type, EventFieldTags tags, byte[] custom)
			: this(name, type, tags, 96, checked((ushort)((custom == null) ? 0 : custom.Length)), custom)
		{
		}

		// Token: 0x06003643 RID: 13891 RVA: 0x000D2B0C File Offset: 0x000D0D0C
		private FieldMetadata(string name, TraceLoggingDataType dataType, EventFieldTags tags, byte countFlags, ushort fixedCount = 0, byte[] custom = null)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name", "This usually means that the object passed to Write is of a type that does not support being used as the top-level object in an event, e.g. a primitive or built-in type.");
			}
			Statics.CheckName(name);
			int num = (int)(dataType & (TraceLoggingDataType)31);
			this.name = name;
			this.nameSize = Encoding.UTF8.GetByteCount(this.name) + 1;
			this.inType = (byte)(num | (int)countFlags);
			this.outType = (byte)((dataType >> 8) & (TraceLoggingDataType)127);
			this.tags = tags;
			this.fixedCount = fixedCount;
			this.custom = custom;
			if (countFlags != 0)
			{
				if (num == 0)
				{
					throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedArrayOfNil"));
				}
				if (num == 14)
				{
					throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedArrayOfBinary"));
				}
				if (num == 1 || num == 2)
				{
					throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedArrayOfNullTerminatedString"));
				}
			}
			if ((this.tags & (EventFieldTags)268435455) != EventFieldTags.None)
			{
				this.outType |= 128;
			}
			if (this.outType != 0)
			{
				this.inType |= 128;
			}
		}

		// Token: 0x06003644 RID: 13892 RVA: 0x000D2C0B File Offset: 0x000D0E0B
		public void IncrementStructFieldCount()
		{
			this.inType |= 128;
			this.outType += 1;
			if ((this.outType & 127) == 0)
			{
				throw new NotSupportedException(Environment.GetResourceString("EventSource_TooManyFields"));
			}
		}

		// Token: 0x06003645 RID: 13893 RVA: 0x000D2C4C File Offset: 0x000D0E4C
		public void Encode(ref int pos, byte[] metadata)
		{
			if (metadata != null)
			{
				Encoding.UTF8.GetBytes(this.name, 0, this.name.Length, metadata, pos);
			}
			pos += this.nameSize;
			if (metadata != null)
			{
				metadata[pos] = this.inType;
			}
			pos++;
			if ((this.inType & 128) != 0)
			{
				if (metadata != null)
				{
					metadata[pos] = this.outType;
				}
				pos++;
				if ((this.outType & 128) != 0)
				{
					Statics.EncodeTags((int)this.tags, ref pos, metadata);
				}
			}
			if ((this.inType & 32) != 0)
			{
				if (metadata != null)
				{
					metadata[pos] = (byte)this.fixedCount;
					metadata[pos + 1] = (byte)(this.fixedCount >> 8);
				}
				pos += 2;
				if (96 == (this.inType & 96) && this.fixedCount != 0)
				{
					if (metadata != null)
					{
						Buffer.BlockCopy(this.custom, 0, metadata, pos, (int)this.fixedCount);
					}
					pos += (int)this.fixedCount;
				}
			}
		}

		// Token: 0x04001843 RID: 6211
		private readonly string name;

		// Token: 0x04001844 RID: 6212
		private readonly int nameSize;

		// Token: 0x04001845 RID: 6213
		private readonly EventFieldTags tags;

		// Token: 0x04001846 RID: 6214
		private readonly byte[] custom;

		// Token: 0x04001847 RID: 6215
		private readonly ushort fixedCount;

		// Token: 0x04001848 RID: 6216
		private byte inType;

		// Token: 0x04001849 RID: 6217
		private byte outType;
	}
}
