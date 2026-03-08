using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	// Token: 0x020000A4 RID: 164
	[NullableContext(1)]
	[Nullable(0)]
	internal class TraceJsonReader : JsonReader, IJsonLineInfo
	{
		// Token: 0x06000849 RID: 2121 RVA: 0x0002432C File Offset: 0x0002252C
		public TraceJsonReader(JsonReader innerReader)
		{
			this._innerReader = innerReader;
			this._sw = new StringWriter(CultureInfo.InvariantCulture);
			this._sw.Write("Deserialized JSON: " + Environment.NewLine);
			this._textWriter = new JsonTextWriter(this._sw);
			this._textWriter.Formatting = Formatting.Indented;
		}

		// Token: 0x0600084A RID: 2122 RVA: 0x0002438D File Offset: 0x0002258D
		public string GetDeserializedJsonMessage()
		{
			return this._sw.ToString();
		}

		// Token: 0x0600084B RID: 2123 RVA: 0x0002439A File Offset: 0x0002259A
		public override bool Read()
		{
			bool result = this._innerReader.Read();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x0600084C RID: 2124 RVA: 0x000243AD File Offset: 0x000225AD
		public override int? ReadAsInt32()
		{
			int? result = this._innerReader.ReadAsInt32();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x0600084D RID: 2125 RVA: 0x000243C0 File Offset: 0x000225C0
		[NullableContext(2)]
		public override string ReadAsString()
		{
			string result = this._innerReader.ReadAsString();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x0600084E RID: 2126 RVA: 0x000243D3 File Offset: 0x000225D3
		[NullableContext(2)]
		public override byte[] ReadAsBytes()
		{
			byte[] result = this._innerReader.ReadAsBytes();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x0600084F RID: 2127 RVA: 0x000243E6 File Offset: 0x000225E6
		public override decimal? ReadAsDecimal()
		{
			decimal? result = this._innerReader.ReadAsDecimal();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x06000850 RID: 2128 RVA: 0x000243F9 File Offset: 0x000225F9
		public override double? ReadAsDouble()
		{
			double? result = this._innerReader.ReadAsDouble();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x06000851 RID: 2129 RVA: 0x0002440C File Offset: 0x0002260C
		public override bool? ReadAsBoolean()
		{
			bool? result = this._innerReader.ReadAsBoolean();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x06000852 RID: 2130 RVA: 0x0002441F File Offset: 0x0002261F
		public override DateTime? ReadAsDateTime()
		{
			DateTime? result = this._innerReader.ReadAsDateTime();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x06000853 RID: 2131 RVA: 0x00024432 File Offset: 0x00022632
		public override DateTimeOffset? ReadAsDateTimeOffset()
		{
			DateTimeOffset? result = this._innerReader.ReadAsDateTimeOffset();
			this.WriteCurrentToken();
			return result;
		}

		// Token: 0x06000854 RID: 2132 RVA: 0x00024445 File Offset: 0x00022645
		public void WriteCurrentToken()
		{
			this._textWriter.WriteToken(this._innerReader, false, false, true);
		}

		// Token: 0x17000164 RID: 356
		// (get) Token: 0x06000855 RID: 2133 RVA: 0x0002445B File Offset: 0x0002265B
		public override int Depth
		{
			get
			{
				return this._innerReader.Depth;
			}
		}

		// Token: 0x17000165 RID: 357
		// (get) Token: 0x06000856 RID: 2134 RVA: 0x00024468 File Offset: 0x00022668
		public override string Path
		{
			get
			{
				return this._innerReader.Path;
			}
		}

		// Token: 0x17000166 RID: 358
		// (get) Token: 0x06000857 RID: 2135 RVA: 0x00024475 File Offset: 0x00022675
		// (set) Token: 0x06000858 RID: 2136 RVA: 0x00024482 File Offset: 0x00022682
		public override char QuoteChar
		{
			get
			{
				return this._innerReader.QuoteChar;
			}
			protected internal set
			{
				this._innerReader.QuoteChar = value;
			}
		}

		// Token: 0x17000167 RID: 359
		// (get) Token: 0x06000859 RID: 2137 RVA: 0x00024490 File Offset: 0x00022690
		public override JsonToken TokenType
		{
			get
			{
				return this._innerReader.TokenType;
			}
		}

		// Token: 0x17000168 RID: 360
		// (get) Token: 0x0600085A RID: 2138 RVA: 0x0002449D File Offset: 0x0002269D
		[Nullable(2)]
		public override object Value
		{
			[NullableContext(2)]
			get
			{
				return this._innerReader.Value;
			}
		}

		// Token: 0x17000169 RID: 361
		// (get) Token: 0x0600085B RID: 2139 RVA: 0x000244AA File Offset: 0x000226AA
		[Nullable(2)]
		public override Type ValueType
		{
			[NullableContext(2)]
			get
			{
				return this._innerReader.ValueType;
			}
		}

		// Token: 0x0600085C RID: 2140 RVA: 0x000244B7 File Offset: 0x000226B7
		public override void Close()
		{
			this._innerReader.Close();
		}

		// Token: 0x0600085D RID: 2141 RVA: 0x000244C4 File Offset: 0x000226C4
		bool IJsonLineInfo.HasLineInfo()
		{
			IJsonLineInfo jsonLineInfo = this._innerReader as IJsonLineInfo;
			return jsonLineInfo != null && jsonLineInfo.HasLineInfo();
		}

		// Token: 0x1700016A RID: 362
		// (get) Token: 0x0600085E RID: 2142 RVA: 0x000244E8 File Offset: 0x000226E8
		int IJsonLineInfo.LineNumber
		{
			get
			{
				IJsonLineInfo jsonLineInfo = this._innerReader as IJsonLineInfo;
				if (jsonLineInfo == null)
				{
					return 0;
				}
				return jsonLineInfo.LineNumber;
			}
		}

		// Token: 0x1700016B RID: 363
		// (get) Token: 0x0600085F RID: 2143 RVA: 0x0002450C File Offset: 0x0002270C
		int IJsonLineInfo.LinePosition
		{
			get
			{
				IJsonLineInfo jsonLineInfo = this._innerReader as IJsonLineInfo;
				if (jsonLineInfo == null)
				{
					return 0;
				}
				return jsonLineInfo.LinePosition;
			}
		}

		// Token: 0x040002E6 RID: 742
		private readonly JsonReader _innerReader;

		// Token: 0x040002E7 RID: 743
		private readonly JsonTextWriter _textWriter;

		// Token: 0x040002E8 RID: 744
		private readonly StringWriter _sw;
	}
}
