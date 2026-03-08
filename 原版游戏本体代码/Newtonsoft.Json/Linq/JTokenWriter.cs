using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a writer that provides a fast, non-cached, forward-only way of generating JSON data.
	/// </summary>
	// Token: 0x020000CB RID: 203
	[NullableContext(2)]
	[Nullable(0)]
	public class JTokenWriter : JsonWriter
	{
		// Token: 0x06000B89 RID: 2953 RVA: 0x0002DF41 File Offset: 0x0002C141
		[NullableContext(1)]
		internal override Task WriteTokenAsync(JsonReader reader, bool writeChildren, bool writeDateConstructorAsDate, bool writeComments, CancellationToken cancellationToken)
		{
			if (reader is JTokenReader)
			{
				this.WriteToken(reader, writeChildren, writeDateConstructorAsDate, writeComments);
				return AsyncUtils.CompletedTask;
			}
			return base.WriteTokenSyncReadingAsync(reader, cancellationToken);
		}

		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> at the writer's current position.
		/// </summary>
		// Token: 0x17000207 RID: 519
		// (get) Token: 0x06000B8A RID: 2954 RVA: 0x0002DF65 File Offset: 0x0002C165
		public JToken CurrentToken
		{
			get
			{
				return this._current;
			}
		}

		/// <summary>
		/// Gets the token being written.
		/// </summary>
		/// <value>The token being written.</value>
		// Token: 0x17000208 RID: 520
		// (get) Token: 0x06000B8B RID: 2955 RVA: 0x0002DF6D File Offset: 0x0002C16D
		public JToken Token
		{
			get
			{
				if (this._token != null)
				{
					return this._token;
				}
				return this._value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JTokenWriter" /> class writing to the given <see cref="T:Newtonsoft.Json.Linq.JContainer" />.
		/// </summary>
		/// <param name="container">The container being written to.</param>
		// Token: 0x06000B8C RID: 2956 RVA: 0x0002DF84 File Offset: 0x0002C184
		[NullableContext(1)]
		public JTokenWriter(JContainer container)
		{
			ValidationUtils.ArgumentNotNull(container, "container");
			this._token = container;
			this._parent = container;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JTokenWriter" /> class.
		/// </summary>
		// Token: 0x06000B8D RID: 2957 RVA: 0x0002DFA5 File Offset: 0x0002C1A5
		public JTokenWriter()
		{
		}

		/// <summary>
		/// Flushes whatever is in the buffer to the underlying <see cref="T:Newtonsoft.Json.Linq.JContainer" />.
		/// </summary>
		// Token: 0x06000B8E RID: 2958 RVA: 0x0002DFAD File Offset: 0x0002C1AD
		public override void Flush()
		{
		}

		/// <summary>
		/// Closes this writer.
		/// If <see cref="P:Newtonsoft.Json.JsonWriter.AutoCompleteOnClose" /> is set to <c>true</c>, the JSON is auto-completed.
		/// </summary>
		/// <remarks>
		/// Setting <see cref="P:Newtonsoft.Json.JsonWriter.CloseOutput" /> to <c>true</c> has no additional effect, since the underlying <see cref="T:Newtonsoft.Json.Linq.JContainer" /> is a type that cannot be closed.
		/// </remarks>
		// Token: 0x06000B8F RID: 2959 RVA: 0x0002DFAF File Offset: 0x0002C1AF
		public override void Close()
		{
			base.Close();
		}

		/// <summary>
		/// Writes the beginning of a JSON object.
		/// </summary>
		// Token: 0x06000B90 RID: 2960 RVA: 0x0002DFB7 File Offset: 0x0002C1B7
		public override void WriteStartObject()
		{
			base.WriteStartObject();
			this.AddParent(new JObject());
		}

		// Token: 0x06000B91 RID: 2961 RVA: 0x0002DFCA File Offset: 0x0002C1CA
		[NullableContext(1)]
		private void AddParent(JContainer container)
		{
			if (this._parent == null)
			{
				this._token = container;
			}
			else
			{
				this._parent.AddAndSkipParentCheck(container);
			}
			this._parent = container;
			this._current = container;
		}

		// Token: 0x06000B92 RID: 2962 RVA: 0x0002DFF8 File Offset: 0x0002C1F8
		private void RemoveParent()
		{
			this._current = this._parent;
			this._parent = this._parent.Parent;
			if (this._parent != null && this._parent.Type == JTokenType.Property)
			{
				this._parent = this._parent.Parent;
			}
		}

		/// <summary>
		/// Writes the beginning of a JSON array.
		/// </summary>
		// Token: 0x06000B93 RID: 2963 RVA: 0x0002E049 File Offset: 0x0002C249
		public override void WriteStartArray()
		{
			base.WriteStartArray();
			this.AddParent(new JArray());
		}

		/// <summary>
		/// Writes the start of a constructor with the given name.
		/// </summary>
		/// <param name="name">The name of the constructor.</param>
		// Token: 0x06000B94 RID: 2964 RVA: 0x0002E05C File Offset: 0x0002C25C
		[NullableContext(1)]
		public override void WriteStartConstructor(string name)
		{
			base.WriteStartConstructor(name);
			this.AddParent(new JConstructor(name));
		}

		/// <summary>
		/// Writes the end.
		/// </summary>
		/// <param name="token">The token.</param>
		// Token: 0x06000B95 RID: 2965 RVA: 0x0002E071 File Offset: 0x0002C271
		protected override void WriteEnd(JsonToken token)
		{
			this.RemoveParent();
		}

		/// <summary>
		/// Writes the property name of a name/value pair on a JSON object.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		// Token: 0x06000B96 RID: 2966 RVA: 0x0002E079 File Offset: 0x0002C279
		[NullableContext(1)]
		public override void WritePropertyName(string name)
		{
			JObject jobject = this._parent as JObject;
			if (jobject != null)
			{
				jobject.Remove(name);
			}
			this.AddParent(new JProperty(name));
			base.WritePropertyName(name);
		}

		// Token: 0x06000B97 RID: 2967 RVA: 0x0002E0A6 File Offset: 0x0002C2A6
		private void AddRawValue(object value, JTokenType type, JsonToken token)
		{
			this.AddJValue(new JValue(value, type), token);
		}

		// Token: 0x06000B98 RID: 2968 RVA: 0x0002E0B8 File Offset: 0x0002C2B8
		internal void AddJValue(JValue value, JsonToken token)
		{
			if (this._parent != null)
			{
				if (this._parent.TryAdd(value))
				{
					this._current = this._parent.Last;
					if (this._parent.Type == JTokenType.Property)
					{
						this._parent = this._parent.Parent;
						return;
					}
				}
			}
			else
			{
				this._value = value ?? JValue.CreateNull();
				this._current = this._value;
			}
		}

		/// <summary>
		/// Writes a <see cref="T:System.Object" /> value.
		/// An error will be raised if the value cannot be written as a single JSON token.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Object" /> value to write.</param>
		// Token: 0x06000B99 RID: 2969 RVA: 0x0002E128 File Offset: 0x0002C328
		public override void WriteValue(object value)
		{
			if (value is BigInteger)
			{
				base.InternalWriteValue(JsonToken.Integer);
				this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
				return;
			}
			base.WriteValue(value);
		}

		/// <summary>
		/// Writes a null value.
		/// </summary>
		// Token: 0x06000B9A RID: 2970 RVA: 0x0002E14A File Offset: 0x0002C34A
		public override void WriteNull()
		{
			base.WriteNull();
			this.AddJValue(JValue.CreateNull(), JsonToken.Null);
		}

		/// <summary>
		/// Writes an undefined value.
		/// </summary>
		// Token: 0x06000B9B RID: 2971 RVA: 0x0002E15F File Offset: 0x0002C35F
		public override void WriteUndefined()
		{
			base.WriteUndefined();
			this.AddJValue(JValue.CreateUndefined(), JsonToken.Undefined);
		}

		/// <summary>
		/// Writes raw JSON.
		/// </summary>
		/// <param name="json">The raw JSON to write.</param>
		// Token: 0x06000B9C RID: 2972 RVA: 0x0002E174 File Offset: 0x0002C374
		public override void WriteRaw(string json)
		{
			base.WriteRaw(json);
			this.AddJValue(new JRaw(json), JsonToken.Raw);
		}

		/// <summary>
		/// Writes a comment <c>/*...*/</c> containing the specified text.
		/// </summary>
		/// <param name="text">Text to place inside the comment.</param>
		// Token: 0x06000B9D RID: 2973 RVA: 0x0002E18A File Offset: 0x0002C38A
		public override void WriteComment(string text)
		{
			base.WriteComment(text);
			this.AddJValue(JValue.CreateComment(text), JsonToken.Comment);
		}

		/// <summary>
		/// Writes a <see cref="T:System.String" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.String" /> value to write.</param>
		// Token: 0x06000B9E RID: 2974 RVA: 0x0002E1A0 File Offset: 0x0002C3A0
		public override void WriteValue(string value)
		{
			if (value == null)
			{
				this.WriteNull();
				return;
			}
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.String);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int32" /> value to write.</param>
		// Token: 0x06000B9F RID: 2975 RVA: 0x0002E1C1 File Offset: 0x0002C3C1
		public override void WriteValue(int value)
		{
			base.WriteValue(value);
			this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt32" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt32" /> value to write.</param>
		// Token: 0x06000BA0 RID: 2976 RVA: 0x0002E1D8 File Offset: 0x0002C3D8
		[CLSCompliant(false)]
		public override void WriteValue(uint value)
		{
			base.WriteValue(value);
			this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int64" /> value to write.</param>
		// Token: 0x06000BA1 RID: 2977 RVA: 0x0002E1EF File Offset: 0x0002C3EF
		public override void WriteValue(long value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt64" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt64" /> value to write.</param>
		// Token: 0x06000BA2 RID: 2978 RVA: 0x0002E205 File Offset: 0x0002C405
		[CLSCompliant(false)]
		public override void WriteValue(ulong value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Single" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Single" /> value to write.</param>
		// Token: 0x06000BA3 RID: 2979 RVA: 0x0002E21B File Offset: 0x0002C41B
		public override void WriteValue(float value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Double" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Double" /> value to write.</param>
		// Token: 0x06000BA4 RID: 2980 RVA: 0x0002E231 File Offset: 0x0002C431
		public override void WriteValue(double value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Boolean" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Boolean" /> value to write.</param>
		// Token: 0x06000BA5 RID: 2981 RVA: 0x0002E247 File Offset: 0x0002C447
		public override void WriteValue(bool value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Boolean);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Int16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Int16" /> value to write.</param>
		// Token: 0x06000BA6 RID: 2982 RVA: 0x0002E25E File Offset: 0x0002C45E
		public override void WriteValue(short value)
		{
			base.WriteValue(value);
			this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.UInt16" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.UInt16" /> value to write.</param>
		// Token: 0x06000BA7 RID: 2983 RVA: 0x0002E275 File Offset: 0x0002C475
		[CLSCompliant(false)]
		public override void WriteValue(ushort value)
		{
			base.WriteValue(value);
			this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Char" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Char" /> value to write.</param>
		// Token: 0x06000BA8 RID: 2984 RVA: 0x0002E28C File Offset: 0x0002C48C
		public override void WriteValue(char value)
		{
			base.WriteValue(value);
			string value2 = value.ToString(CultureInfo.InvariantCulture);
			this.AddJValue(new JValue(value2), JsonToken.String);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" /> value to write.</param>
		// Token: 0x06000BA9 RID: 2985 RVA: 0x0002E2BB File Offset: 0x0002C4BB
		public override void WriteValue(byte value)
		{
			base.WriteValue(value);
			this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.SByte" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.SByte" /> value to write.</param>
		// Token: 0x06000BAA RID: 2986 RVA: 0x0002E2D2 File Offset: 0x0002C4D2
		[CLSCompliant(false)]
		public override void WriteValue(sbyte value)
		{
			base.WriteValue(value);
			this.AddRawValue(value, JTokenType.Integer, JsonToken.Integer);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Decimal" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Decimal" /> value to write.</param>
		// Token: 0x06000BAB RID: 2987 RVA: 0x0002E2E9 File Offset: 0x0002C4E9
		public override void WriteValue(decimal value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Float);
		}

		/// <summary>
		/// Writes a <see cref="T:System.DateTime" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTime" /> value to write.</param>
		// Token: 0x06000BAC RID: 2988 RVA: 0x0002E2FF File Offset: 0x0002C4FF
		public override void WriteValue(DateTime value)
		{
			base.WriteValue(value);
			value = DateTimeUtils.EnsureDateTime(value, base.DateTimeZoneHandling);
			this.AddJValue(new JValue(value), JsonToken.Date);
		}

		/// <summary>
		/// Writes a <see cref="T:System.DateTimeOffset" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.DateTimeOffset" /> value to write.</param>
		// Token: 0x06000BAD RID: 2989 RVA: 0x0002E324 File Offset: 0x0002C524
		public override void WriteValue(DateTimeOffset value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.Date);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Byte" />[] value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Byte" />[] value to write.</param>
		// Token: 0x06000BAE RID: 2990 RVA: 0x0002E33B File Offset: 0x0002C53B
		public override void WriteValue(byte[] value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value, JTokenType.Bytes), JsonToken.Bytes);
		}

		/// <summary>
		/// Writes a <see cref="T:System.TimeSpan" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.TimeSpan" /> value to write.</param>
		// Token: 0x06000BAF RID: 2991 RVA: 0x0002E354 File Offset: 0x0002C554
		public override void WriteValue(TimeSpan value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.String);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Guid" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Guid" /> value to write.</param>
		// Token: 0x06000BB0 RID: 2992 RVA: 0x0002E36B File Offset: 0x0002C56B
		public override void WriteValue(Guid value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.String);
		}

		/// <summary>
		/// Writes a <see cref="T:System.Uri" /> value.
		/// </summary>
		/// <param name="value">The <see cref="T:System.Uri" /> value to write.</param>
		// Token: 0x06000BB1 RID: 2993 RVA: 0x0002E382 File Offset: 0x0002C582
		public override void WriteValue(Uri value)
		{
			base.WriteValue(value);
			this.AddJValue(new JValue(value), JsonToken.String);
		}

		// Token: 0x06000BB2 RID: 2994 RVA: 0x0002E39C File Offset: 0x0002C59C
		[NullableContext(1)]
		internal override void WriteToken(JsonReader reader, bool writeChildren, bool writeDateConstructorAsDate, bool writeComments)
		{
			JTokenReader jtokenReader = reader as JTokenReader;
			if (jtokenReader == null || !writeChildren || !writeDateConstructorAsDate || !writeComments)
			{
				base.WriteToken(reader, writeChildren, writeDateConstructorAsDate, writeComments);
				return;
			}
			if (jtokenReader.TokenType == JsonToken.None && !jtokenReader.Read())
			{
				return;
			}
			JToken jtoken = jtokenReader.CurrentToken.CloneToken(null);
			if (this._parent != null)
			{
				this._parent.Add(jtoken);
				this._current = this._parent.Last;
				if (this._parent.Type == JTokenType.Property)
				{
					this._parent = this._parent.Parent;
					base.InternalWriteValue(JsonToken.Null);
				}
			}
			else
			{
				this._current = jtoken;
				if (this._token == null && this._value == null)
				{
					this._token = jtoken as JContainer;
					this._value = jtoken as JValue;
				}
			}
			jtokenReader.Skip();
		}

		// Token: 0x040003BA RID: 954
		private JContainer _token;

		// Token: 0x040003BB RID: 955
		private JContainer _parent;

		// Token: 0x040003BC RID: 956
		private JValue _value;

		// Token: 0x040003BD RID: 957
		private JToken _current;
	}
}
