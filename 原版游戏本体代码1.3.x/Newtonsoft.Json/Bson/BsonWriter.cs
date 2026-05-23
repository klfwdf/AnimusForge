using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Bson
{
	/// <summary>
	/// Represents a writer that provides a fast, non-cached, forward-only way of generating BSON data.
	/// </summary>
	// Token: 0x02000114 RID: 276
	[Obsolete("BSON reading and writing has been moved to its own package. See https://www.nuget.org/packages/Newtonsoft.Json.Bson for more details.")]
	public class BsonWriter : JsonWriter
	{
		/// <summary>
		/// Gets or sets the <see cref="T:System.DateTimeKind" /> used when writing <see cref="T:System.DateTime" /> values to BSON.
		/// When set to <see cref="F:System.DateTimeKind.Unspecified" /> no conversion will occur.
		/// </summary>
		/// <value>The <see cref="T:System.DateTimeKind" /> used when writing <see cref="T:System.DateTime" /> values to BSON.</value>
		// Token: 0x17000287 RID: 647
		// (get) Token: 0x06000DC7 RID: 3527 RVA: 0x000371ED File Offset: 0x000353ED
		// (set) Token: 0x06000DC8 RID: 3528 RVA: 0x000371FA File Offset: 0x000353FA
		public DateTimeKind DateTimeKindHandling
		{
			get
			{
				return this._writer.DateTimeKindHandling;
			}
			set
			{
				this._writer.DateTimeKindHandling = value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Bson.BsonWriter" /> class.
		/// </summary>
		/// <param name="stream">The <see cref="T:System.IO.Stream" /> to write to.</param>
		// Token: 0x06000DC9 RID: 3529 RVA: 0x00037208 File Offset: 0x00035408
		public BsonWriter(Stream stream)
		{
			ValidationUtils.ArgumentNotNull(stream, "stream");
			this._writer = new BsonBinaryWriter(new BinaryWriter(stream));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Bson.BsonWriter" /> class.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.IO.BinaryWriter" /> to write to.</param>
		// Token: 0x06000DCA RID: 3530 RVA: 0x0003722C File Offset: 0x0003542C
		public BsonWriter(BinaryWriter writer)
		{
			ValidationUtils.ArgumentNotNull(writer, "writer");
			this._writer = new BsonBinaryWriter(writer);
		}

		/// <summary>
		/// Flushes whatever is in the buffer to the underlying <see cref="T:System.IO.Stream" /> and also flushes the underlying stream.
		/// </summary>
		// Token: 0x06000DCB RID: 3531 RVA: 0x0003724B File Offset: 0x0003544B
		public override void Flush()
		{
			this._writer.Flush();
		}

		/// <summary>
		/// Writes the end.
		/// </summary>
		/// <param name="token">The token.</param>
		// Token: 0x06000DCC RID: 3532 RVA: 0x00037258 File Offset: 0x00035458
		protected override void WriteEnd(JsonToken token)
		{
			base.WriteEnd(token);
			this.RemoveParent();
			if (base.Top == 0)
			{
				this._writer.WriteToken(this._root);
			}
		}

		/// <summary>
		/// Writes a comment <c>/*...*/</c> containing the specified text.
		/// </summary>
		/// <param name="text">Text to place inside the comment.</param>
		// Token: 0x06000DCD RID: 3533 RVA: 0x00037280 File Offset: 0x00035480
		public override void WriteComment(string text)
		{
			throw JsonWriterException.Create(this, "Cannot write JSON comment as BSON.", null);
		}

		/// <summary>
		/// Writes the start of a constructor with the given name.
		/// </summary>
		/// <param name="name">The name of the constructor.</param>
		// Token: 0x06000DCE RID: 3534 RVA: 0x0003728E File Offset: 0x0003548E
		public override void WriteStartConstructor(string name)
		{
			throw JsonWriterException.Create(this, "Cannot write JSON constructor as BSON.", null);
		}

		/// <summary>
		/// Writes raw JSON.
		/// </summary>
		/// <param name="json">The raw JSON to write.</param>
		// Token: 0x06000DCF RID: 3535 RVA: 0x0003729C File Offset: 0x0003549C
		public override void WriteRaw(string json)
		{
			throw JsonWriterException.Create(this, "Cannot write raw JSON as BSON.", null);
		}

		/// <summary>
		/// Writes raw JSON where a value is expected and updates the writer's state.
		/// </summary>
		/// <param name="json">The raw JSON to write.</param>
		// Token: 0x06000DD0 RID: 3536 RVA: 0x000372AA File Offset: 0x000354AA
		public override void WriteRawValue(string json)
		{
			throw JsonWriterException.Create(this, "Cannot write raw JSON as BSON.", null);
		}

		/// <summary>
		/// Writes the beginning of a JSON array.
		/// </summary>
		// Token: 0x06000DD1 RID: 3537 RVA: 0x000372B8 File Offset: 0x000354B8
		public override void WriteStartArray()
		{
			base.WriteStartArray();
			this.AddParent(new BsonArray());
		}

		/// <summary>
		/// Writes the beginning of a JSON object.
		/// </summary>
		// Token: 0x06000DD2 RID: 3538 RVA: 0x000372CB File Offset: 0x000354CB
		public override void WriteStartObject()
		{
			base.WriteStartObject();
			this.AddParent(new BsonObject());
		}

		/// <summary>
		/// Writes the property name of a name/value pair on a JSON object.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		// Token: 0x06000DD3 RID: 3539 RVA: 0x000372DE File Offset: 0x000354DE
		public override void WritePropertyName(string name)
		{
			base.WritePropertyName(name);
			this._propertyName = name;
		}

		/// <summary>
		/// Closes this writer.
		/// If <see cref="P:Newtonsoft.Json.JsonWriter.CloseOutput" /> is set to <c>true</c>, the underlying <see cref="T:System.IO.Stream" /> is also closed.
		/// If <see cref="P:Newtonsoft.Json.JsonWriter.AutoCompleteOnClose" /> is set to <c>true</c>, the JSON is auto-completed.
		/// </summary>
		// Token: 0x06000DD4 RID: 3540 RVA: 0x000372EE File Offset: 0x000354EE
		public override void Close()
		{
			base.Close();
			if (base.CloseOutput)
			{
				BsonBinaryWriter writer = this._writer;
				if (writer == null)
				{
					return;
				}
				writer.Close();
			}
		}

		// Token: 0x06000DD5 RID: 3541 RVA: 0x0003730E File Offset: 0x0003550E
		private void AddParent(BsonToken container)
		{
			this.AddToken(container);
			this._parent = container;
		}

		// Token: 0x06000DD6 RID: 3542 RVA: 0x0003731E File Offset: 0x0003551E
		private void RemoveParent()
		{
			this._parent = this._parent.Parent;
		}

		// Token: 0x06000DD7 RID: 3543 RVA: 0x00037331 File Offset: 0x00035531
		private void AddValue(object value, BsonType type)
		{
			this.AddToken(new BsonValue(value, type));
		}

		// Token: 0x06000DD8 RID: 3544 RVA: 0x00037340 File Offset: 0x00035540
		internal void AddToken(BsonToken token)
		{
			if (this._parent != null)
			{
				BsonObject bsonObject = this._parent as BsonObject;
				if (bsonObject != null)
				{
					bsonObject.Add(this._propertyName, token);
					this._propertyName = null;
					return;
				}
				((BsonArray)this._parent).Add(token);
				return;
			}
			else
			{
				if (token.Type != BsonType.Object && token.Type != BsonType.Array)
				{
					throw JsonWriterException.Create(this, "Error writing {0} value. BSON must start with an Object or Array.".FormatWith(CultureInfo.InvariantCulture, token.Type), null);
				}
				this._parent = token;
				this._root = token;
				return;
			}
		}

		/// <summary>
		/// Writes a <see cref="T:System.Object" /> value.
		/// An error will raised if the value cannot be written as a single JSON token.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Object" /> value to write.</param>
		// Token: 0x06000DD9 RID: 3545 RVA: 0x000373D0 File Offset: 0x000355D0
		public override void WriteValue(object value)
		{
			if (value is BigInteger)
			{
				BigInteger bigInteger = (BigInteger)value;
				base.SetWriteState(JsonToken.Integer, null);
				this.AddToken(new BsonBinary(bigInteger.ToByteArray(), BsonBinaryType.Binary));
				return;
			}
			base.WriteValue(value);
		}

		/// <summary>
		/// Writes a null value.
		/// </summary>
		// Token: 0x06000DDA RID: 3546 RVA: 0x0003740F File Offset: 0x0003560F
		public override void WriteNull()
		{
			base.WriteNull();
			this.AddToken(BsonEmpty.Null);
		}

		/// <summary>
		/// Writes an undefined value.
		/// </summary>
		// Token: 0x06000DDB RID: 3547 RVA: 0x00037422 File Offset: 0x00035622
		public override void WriteUndefined()
		{
			base.WriteUndefined();
			this.AddToken(BsonEmpty.Undefined);
		}

		/// <summary>
		/// Writes a <see cref="T:System.String" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.String" /> value to write.</param>
		// Token: 0x06000DDC RID: 3548 RVA: 0x00037435 File Offset: 0x00035635
		public override void WriteValue(string value)
		{
			base.WriteValue(value);
			this.AddToken((value == null) ? BsonEmpty.Null : new BsonString(value, true));
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int32" /> value to write.</param>
		// Token: 0x06000DDD RID: 3549 RVA: 0x00037455 File Offset: 0x00035655
		public override void WriteValue(int value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt32" /> value to write.</param>
		// Token: 0x06000DDE RID: 3550 RVA: 0x0003746C File Offset: 0x0003566C
		[CLSCompliant(false)]
		public override void WriteValue(uint value)
		{
			if (value > 2147483647U)
			{
				throw JsonWriterException.Create(this, "Value is too large to fit in a signed 32 bit integer. BSON does not support unsigned values.", null);
			}
			base.WriteValue(value);
			this.AddValue(value, BsonType.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int64" /> value to write.</param>
		// Token: 0x06000DDF RID: 3551 RVA: 0x00037498 File Offset: 0x00035698
		public override void WriteValue(long value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Long);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt64" /> value to write.</param>
		// Token: 0x06000DE0 RID: 3552 RVA: 0x000374AF File Offset: 0x000356AF
		[CLSCompliant(false)]
		public override void WriteValue(ulong value)
		{
			if (value > 9223372036854775807UL)
			{
				throw JsonWriterException.Create(this, "Value is too large to fit in a signed 64 bit integer. BSON does not support unsigned values.", null);
			}
			base.WriteValue(value);
			this.AddValue(value, BsonType.Long);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Single" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Single" /> value to write.</param>
		// Token: 0x06000DE1 RID: 3553 RVA: 0x000374DF File Offset: 0x000356DF
		public override void WriteValue(float value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Number);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Double" /> value to write.</param>
		// Token: 0x06000DE2 RID: 3554 RVA: 0x000374F5 File Offset: 0x000356F5
		public override void WriteValue(double value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Number);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Boolean" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Boolean" /> value to write.</param>
		// Token: 0x06000DE3 RID: 3555 RVA: 0x0003750B File Offset: 0x0003570B
		public override void WriteValue(bool value)
		{
			base.WriteValue(value);
			this.AddToken(value ? BsonBoolean.True : BsonBoolean.False);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int16" /> value to write.</param>
		// Token: 0x06000DE4 RID: 3556 RVA: 0x00037529 File Offset: 0x00035729
		public override void WriteValue(short value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt16" /> value to write.</param>
		// Token: 0x06000DE5 RID: 3557 RVA: 0x00037540 File Offset: 0x00035740
		[CLSCompliant(false)]
		public override void WriteValue(ushort value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Char" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Char" /> value to write.</param>
		// Token: 0x06000DE6 RID: 3558 RVA: 0x00037558 File Offset: 0x00035758
		public override void WriteValue(char value)
		{
			base.WriteValue(value);
			string value2 = value.ToString(CultureInfo.InvariantCulture);
			this.AddToken(new BsonString(value2, true));
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" /> value to write.</param>
		// Token: 0x06000DE7 RID: 3559 RVA: 0x00037588 File Offset: 0x00035788
		public override void WriteValue(byte value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.SByte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.SByte" /> value to write.</param>
		// Token: 0x06000DE8 RID: 3560 RVA: 0x0003759F File Offset: 0x0003579F
		[CLSCompliant(false)]
		public override void WriteValue(sbyte value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Decimal" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Decimal" /> value to write.</param>
		// Token: 0x06000DE9 RID: 3561 RVA: 0x000375B6 File Offset: 0x000357B6
		public override void WriteValue(decimal value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Number);
		}

		/// <summary>
		/// Writes a <see cref="T:System.DateTime" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTime" /> value to write.</param>
		// Token: 0x06000DEA RID: 3562 RVA: 0x000375CC File Offset: 0x000357CC
		public override void WriteValue(DateTime value)
		{
			base.WriteValue(value);
			value = DateTimeUtils.EnsureDateTime(value, base.DateTimeZoneHandling);
			this.AddValue(value, BsonType.Date);
		}

		/// <summary>
		/// Writes a <see cref="T:System.DateTimeOffset" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTimeOffset" /> value to write.</param>
		// Token: 0x06000DEB RID: 3563 RVA: 0x000375F1 File Offset: 0x000357F1
		public override void WriteValue(DateTimeOffset value)
		{
			base.WriteValue(value);
			this.AddValue(value, BsonType.Date);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" />[] value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" />[] value to write.</param>
		// Token: 0x06000DEC RID: 3564 RVA: 0x00037608 File Offset: 0x00035808
		public override void WriteValue(byte[] value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.WriteValue(value);
			this.AddToken(new BsonBinary(value, BsonBinaryType.Binary));
		}

		/// <summary>
		/// Writes a <see cref="T:System.Guid" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Guid" /> value to write.</param>
		// Token: 0x06000DED RID: 3565 RVA: 0x00037628 File Offset: 0x00035828
		public override void WriteValue(Guid value)
		{
			base.WriteValue(value);
			this.AddToken(new BsonBinary(value.ToByteArray(), BsonBinaryType.Uuid));
		}

		/// <summary>
		/// Writes a <see cref="T:System.TimeSpan" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.TimeSpan" /> value to write.</param>
		// Token: 0x06000DEE RID: 3566 RVA: 0x00037644 File Offset: 0x00035844
		public override void WriteValue(TimeSpan value)
		{
			base.WriteValue(value);
			this.AddToken(new BsonString(value.ToString(), true));
		}

		/// <summary>
		/// Writes a <see cref="T:System.Uri" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Uri" /> value to write.</param>
		// Token: 0x06000DEF RID: 3567 RVA: 0x00037666 File Offset: 0x00035866
		public override void WriteValue(Uri value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.WriteValue(value);
			this.AddToken(new BsonString(value.ToString(), true));
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" />[] value that represents a BSON object id.
		/// </summary>
		/// <param name="value">The Object ID value to write.</param>
		// Token: 0x06000DF0 RID: 3568 RVA: 0x00037691 File Offset: 0x00035891
		public void WriteObjectId(byte[] value)
		{
			ValidationUtils.ArgumentNotNull(value, "value");
			if (value.Length != 12)
			{
				throw JsonWriterException.Create(this, "An object id must be 12 bytes", null);
			}
			base.SetWriteState(JsonToken.Undefined, null);
			this.AddValue(value, BsonType.Oid);
		}

		/// <summary>
		/// Writes a BSON regex.
		/// </summary>
		/// <param name="pattern">The regex pattern.</param>
		/// <param name="options">The regex options.</param>
		// Token: 0x06000DF1 RID: 3569 RVA: 0x000376C3 File Offset: 0x000358C3
		public void WriteRegex(string pattern, string options)
		{
			ValidationUtils.ArgumentNotNull(pattern, "pattern");
			base.SetWriteState(JsonToken.Undefined, null);
			this.AddToken(new BsonRegex(pattern, options));
		}

		// Token: 0x04000461 RID: 1121
		private readonly BsonBinaryWriter _writer;

		// Token: 0x04000462 RID: 1122
		private BsonToken _root;

		// Token: 0x04000463 RID: 1123
		private BsonToken _parent;

		// Token: 0x04000464 RID: 1124
		private string _propertyName;
	}
}
