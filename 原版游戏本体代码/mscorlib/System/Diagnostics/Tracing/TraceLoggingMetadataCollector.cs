using System;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x02000485 RID: 1157
	internal class TraceLoggingMetadataCollector
	{
		// Token: 0x06003746 RID: 14150 RVA: 0x000D4D97 File Offset: 0x000D2F97
		internal TraceLoggingMetadataCollector()
		{
			this.impl = new TraceLoggingMetadataCollector.Impl();
		}

		// Token: 0x06003747 RID: 14151 RVA: 0x000D4DB5 File Offset: 0x000D2FB5
		private TraceLoggingMetadataCollector(TraceLoggingMetadataCollector other, FieldMetadata group)
		{
			this.impl = other.impl;
			this.currentGroup = group;
		}

		// Token: 0x17000812 RID: 2066
		// (get) Token: 0x06003748 RID: 14152 RVA: 0x000D4DDB File Offset: 0x000D2FDB
		// (set) Token: 0x06003749 RID: 14153 RVA: 0x000D4DE3 File Offset: 0x000D2FE3
		internal EventFieldTags Tags { get; set; }

		// Token: 0x17000813 RID: 2067
		// (get) Token: 0x0600374A RID: 14154 RVA: 0x000D4DEC File Offset: 0x000D2FEC
		internal int ScratchSize
		{
			get
			{
				return (int)this.impl.scratchSize;
			}
		}

		// Token: 0x17000814 RID: 2068
		// (get) Token: 0x0600374B RID: 14155 RVA: 0x000D4DF9 File Offset: 0x000D2FF9
		internal int DataCount
		{
			get
			{
				return (int)this.impl.dataCount;
			}
		}

		// Token: 0x17000815 RID: 2069
		// (get) Token: 0x0600374C RID: 14156 RVA: 0x000D4E06 File Offset: 0x000D3006
		internal int PinCount
		{
			get
			{
				return (int)this.impl.pinCount;
			}
		}

		// Token: 0x17000816 RID: 2070
		// (get) Token: 0x0600374D RID: 14157 RVA: 0x000D4E13 File Offset: 0x000D3013
		private bool BeginningBufferedArray
		{
			get
			{
				return this.bufferedArrayFieldCount == 0;
			}
		}

		// Token: 0x0600374E RID: 14158 RVA: 0x000D4E20 File Offset: 0x000D3020
		public TraceLoggingMetadataCollector AddGroup(string name)
		{
			TraceLoggingMetadataCollector result = this;
			if (name != null || this.BeginningBufferedArray)
			{
				FieldMetadata fieldMetadata = new FieldMetadata(name, TraceLoggingDataType.Struct, this.Tags, this.BeginningBufferedArray);
				this.AddField(fieldMetadata);
				result = new TraceLoggingMetadataCollector(this, fieldMetadata);
			}
			return result;
		}

		// Token: 0x0600374F RID: 14159 RVA: 0x000D4E60 File Offset: 0x000D3060
		public void AddScalar(string name, TraceLoggingDataType type)
		{
			TraceLoggingDataType traceLoggingDataType = type & (TraceLoggingDataType)31;
			int size;
			switch (traceLoggingDataType)
			{
			case TraceLoggingDataType.Int8:
			case TraceLoggingDataType.UInt8:
				break;
			case TraceLoggingDataType.Int16:
			case TraceLoggingDataType.UInt16:
				goto IL_6F;
			case TraceLoggingDataType.Int32:
			case TraceLoggingDataType.UInt32:
			case TraceLoggingDataType.Float:
			case TraceLoggingDataType.Boolean32:
			case TraceLoggingDataType.HexInt32:
				size = 4;
				goto IL_8B;
			case TraceLoggingDataType.Int64:
			case TraceLoggingDataType.UInt64:
			case TraceLoggingDataType.Double:
			case TraceLoggingDataType.FileTime:
			case TraceLoggingDataType.HexInt64:
				size = 8;
				goto IL_8B;
			case TraceLoggingDataType.Binary:
			case (TraceLoggingDataType)16:
			case (TraceLoggingDataType)19:
				goto IL_80;
			case TraceLoggingDataType.Guid:
			case TraceLoggingDataType.SystemTime:
				size = 16;
				goto IL_8B;
			default:
				if (traceLoggingDataType != TraceLoggingDataType.Char8)
				{
					if (traceLoggingDataType != TraceLoggingDataType.Char16)
					{
						goto IL_80;
					}
					goto IL_6F;
				}
				break;
			}
			size = 1;
			goto IL_8B;
			IL_6F:
			size = 2;
			goto IL_8B;
			IL_80:
			throw new ArgumentOutOfRangeException("type");
			IL_8B:
			this.impl.AddScalar(size);
			this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
		}

		// Token: 0x06003750 RID: 14160 RVA: 0x000D4F20 File Offset: 0x000D3120
		public void AddBinary(string name, TraceLoggingDataType type)
		{
			TraceLoggingDataType traceLoggingDataType = type & (TraceLoggingDataType)31;
			if (traceLoggingDataType != TraceLoggingDataType.Binary && traceLoggingDataType - TraceLoggingDataType.CountedUtf16String > 1)
			{
				throw new ArgumentOutOfRangeException("type");
			}
			this.impl.AddScalar(2);
			this.impl.AddNonscalar();
			this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
		}

		// Token: 0x06003751 RID: 14161 RVA: 0x000D4F7C File Offset: 0x000D317C
		public void AddArray(string name, TraceLoggingDataType type)
		{
			TraceLoggingDataType traceLoggingDataType = type & (TraceLoggingDataType)31;
			switch (traceLoggingDataType)
			{
			case TraceLoggingDataType.Utf16String:
			case TraceLoggingDataType.MbcsString:
			case TraceLoggingDataType.Int8:
			case TraceLoggingDataType.UInt8:
			case TraceLoggingDataType.Int16:
			case TraceLoggingDataType.UInt16:
			case TraceLoggingDataType.Int32:
			case TraceLoggingDataType.UInt32:
			case TraceLoggingDataType.Int64:
			case TraceLoggingDataType.UInt64:
			case TraceLoggingDataType.Float:
			case TraceLoggingDataType.Double:
			case TraceLoggingDataType.Boolean32:
			case TraceLoggingDataType.Guid:
			case TraceLoggingDataType.FileTime:
			case TraceLoggingDataType.HexInt32:
			case TraceLoggingDataType.HexInt64:
				goto IL_7C;
			case TraceLoggingDataType.Binary:
			case (TraceLoggingDataType)16:
			case TraceLoggingDataType.SystemTime:
			case (TraceLoggingDataType)19:
				break;
			default:
				if (traceLoggingDataType == TraceLoggingDataType.Char8 || traceLoggingDataType == TraceLoggingDataType.Char16)
				{
					goto IL_7C;
				}
				break;
			}
			throw new ArgumentOutOfRangeException("type");
			IL_7C:
			if (this.BeginningBufferedArray)
			{
				throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedNestedArraysEnums"));
			}
			this.impl.AddScalar(2);
			this.impl.AddNonscalar();
			this.AddField(new FieldMetadata(name, type, this.Tags, true));
		}

		// Token: 0x06003752 RID: 14162 RVA: 0x000D5048 File Offset: 0x000D3248
		public void BeginBufferedArray()
		{
			if (this.bufferedArrayFieldCount >= 0)
			{
				throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedNestedArraysEnums"));
			}
			this.bufferedArrayFieldCount = 0;
			this.impl.BeginBuffered();
		}

		// Token: 0x06003753 RID: 14163 RVA: 0x000D5075 File Offset: 0x000D3275
		public void EndBufferedArray()
		{
			if (this.bufferedArrayFieldCount != 1)
			{
				throw new InvalidOperationException(Environment.GetResourceString("EventSource_IncorrentlyAuthoredTypeInfo"));
			}
			this.bufferedArrayFieldCount = int.MinValue;
			this.impl.EndBuffered();
		}

		// Token: 0x06003754 RID: 14164 RVA: 0x000D50A8 File Offset: 0x000D32A8
		public void AddCustom(string name, TraceLoggingDataType type, byte[] metadata)
		{
			if (this.BeginningBufferedArray)
			{
				throw new NotSupportedException(Environment.GetResourceString("EventSource_NotSupportedCustomSerializedData"));
			}
			this.impl.AddScalar(2);
			this.impl.AddNonscalar();
			this.AddField(new FieldMetadata(name, type, this.Tags, metadata));
		}

		// Token: 0x06003755 RID: 14165 RVA: 0x000D50F8 File Offset: 0x000D32F8
		internal byte[] GetMetadata()
		{
			int num = this.impl.Encode(null);
			byte[] array = new byte[num];
			this.impl.Encode(array);
			return array;
		}

		// Token: 0x06003756 RID: 14166 RVA: 0x000D5127 File Offset: 0x000D3327
		private void AddField(FieldMetadata fieldMetadata)
		{
			this.Tags = EventFieldTags.None;
			this.bufferedArrayFieldCount++;
			this.impl.fields.Add(fieldMetadata);
			if (this.currentGroup != null)
			{
				this.currentGroup.IncrementStructFieldCount();
			}
		}

		// Token: 0x040018A2 RID: 6306
		private readonly TraceLoggingMetadataCollector.Impl impl;

		// Token: 0x040018A3 RID: 6307
		private readonly FieldMetadata currentGroup;

		// Token: 0x040018A4 RID: 6308
		private int bufferedArrayFieldCount = int.MinValue;

		// Token: 0x02000BA0 RID: 2976
		private class Impl
		{
			// Token: 0x06006CA8 RID: 27816 RVA: 0x001780AD File Offset: 0x001762AD
			public void AddScalar(int size)
			{
				checked
				{
					if (this.bufferNesting == 0)
					{
						if (!this.scalar)
						{
							this.dataCount += 1;
						}
						this.scalar = true;
						this.scratchSize = (short)((int)this.scratchSize + size);
					}
				}
			}

			// Token: 0x06006CA9 RID: 27817 RVA: 0x001780E4 File Offset: 0x001762E4
			public void AddNonscalar()
			{
				checked
				{
					if (this.bufferNesting == 0)
					{
						this.scalar = false;
						this.pinCount += 1;
						this.dataCount += 1;
					}
				}
			}

			// Token: 0x06006CAA RID: 27818 RVA: 0x00178113 File Offset: 0x00176313
			public void BeginBuffered()
			{
				if (this.bufferNesting == 0)
				{
					this.AddNonscalar();
				}
				this.bufferNesting++;
			}

			// Token: 0x06006CAB RID: 27819 RVA: 0x00178131 File Offset: 0x00176331
			public void EndBuffered()
			{
				this.bufferNesting--;
			}

			// Token: 0x06006CAC RID: 27820 RVA: 0x00178144 File Offset: 0x00176344
			public int Encode(byte[] metadata)
			{
				int result = 0;
				foreach (FieldMetadata fieldMetadata in this.fields)
				{
					fieldMetadata.Encode(ref result, metadata);
				}
				return result;
			}

			// Token: 0x04003536 RID: 13622
			internal readonly List<FieldMetadata> fields = new List<FieldMetadata>();

			// Token: 0x04003537 RID: 13623
			internal short scratchSize;

			// Token: 0x04003538 RID: 13624
			internal sbyte dataCount;

			// Token: 0x04003539 RID: 13625
			internal sbyte pinCount;

			// Token: 0x0400353A RID: 13626
			private int bufferNesting;

			// Token: 0x0400353B RID: 13627
			private bool scalar;
		}
	}
}
