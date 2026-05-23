using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Represents a reader that provides fast, non-cached, forward-only access to serialized JSON data.
	/// </summary>
	// Token: 0x020000C9 RID: 201
	[NullableContext(1)]
	[Nullable(0)]
	public class JTokenReader : JsonReader, IJsonLineInfo
	{
		/// <summary>
		/// Gets the <see cref="T:Newtonsoft.Json.Linq.JToken" /> at the reader's current position.
		/// </summary>
		// Token: 0x17000203 RID: 515
		// (get) Token: 0x06000B7A RID: 2938 RVA: 0x0002DA28 File Offset: 0x0002BC28
		[Nullable(2)]
		public JToken CurrentToken
		{
			[NullableContext(2)]
			get
			{
				return this._current;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JTokenReader" /> class.
		/// </summary>
		/// <param name="token">The token to read from.</param>
		// Token: 0x06000B7B RID: 2939 RVA: 0x0002DA30 File Offset: 0x0002BC30
		public JTokenReader(JToken token)
		{
			ValidationUtils.ArgumentNotNull(token, "token");
			this._root = token;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Linq.JTokenReader" /> class.
		/// </summary>
		/// <param name="token">The token to read from.</param>
		/// <param name="initialPath">The initial path of the token. It is prepended to the returned <see cref="P:Newtonsoft.Json.Linq.JTokenReader.Path" />.</param>
		// Token: 0x06000B7C RID: 2940 RVA: 0x0002DA4A File Offset: 0x0002BC4A
		public JTokenReader(JToken token, string initialPath)
			: this(token)
		{
			this._initialPath = initialPath;
		}

		/// <summary>
		/// Reads the next JSON token from the underlying <see cref="T:Newtonsoft.Json.Linq.JToken" />.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.
		/// </returns>
		// Token: 0x06000B7D RID: 2941 RVA: 0x0002DA5C File Offset: 0x0002BC5C
		public override bool Read()
		{
			if (base.CurrentState != JsonReader.State.Start)
			{
				if (this._current == null)
				{
					return false;
				}
				JContainer jcontainer = this._current as JContainer;
				if (jcontainer != null && this._parent != jcontainer)
				{
					return this.ReadInto(jcontainer);
				}
				return this.ReadOver(this._current);
			}
			else
			{
				if (this._current == this._root)
				{
					return false;
				}
				this._current = this._root;
				this.SetToken(this._current);
				return true;
			}
		}

		// Token: 0x06000B7E RID: 2942 RVA: 0x0002DAD4 File Offset: 0x0002BCD4
		private bool ReadOver(JToken t)
		{
			if (t == this._root)
			{
				return this.ReadToEnd();
			}
			JToken next = t.Next;
			if (next != null && next != t && t != t.Parent.Last)
			{
				this._current = next;
				this.SetToken(this._current);
				return true;
			}
			if (t.Parent == null)
			{
				return this.ReadToEnd();
			}
			return this.SetEnd(t.Parent);
		}

		// Token: 0x06000B7F RID: 2943 RVA: 0x0002DB3D File Offset: 0x0002BD3D
		private bool ReadToEnd()
		{
			this._current = null;
			base.SetToken(JsonToken.None);
			return false;
		}

		// Token: 0x06000B80 RID: 2944 RVA: 0x0002DB50 File Offset: 0x0002BD50
		private JsonToken? GetEndToken(JContainer c)
		{
			switch (c.Type)
			{
			case JTokenType.Object:
				return new JsonToken?(JsonToken.EndObject);
			case JTokenType.Array:
				return new JsonToken?(JsonToken.EndArray);
			case JTokenType.Constructor:
				return new JsonToken?(JsonToken.EndConstructor);
			case JTokenType.Property:
				return null;
			default:
				throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", c.Type, "Unexpected JContainer type.");
			}
		}

		// Token: 0x06000B81 RID: 2945 RVA: 0x0002DBBC File Offset: 0x0002BDBC
		private bool ReadInto(JContainer c)
		{
			JToken first = c.First;
			if (first == null)
			{
				return this.SetEnd(c);
			}
			this.SetToken(first);
			this._current = first;
			this._parent = c;
			return true;
		}

		// Token: 0x06000B82 RID: 2946 RVA: 0x0002DBF4 File Offset: 0x0002BDF4
		private bool SetEnd(JContainer c)
		{
			JsonToken? endToken = this.GetEndToken(c);
			if (endToken != null)
			{
				base.SetToken(endToken.GetValueOrDefault());
				this._current = c;
				this._parent = c;
				return true;
			}
			return this.ReadOver(c);
		}

		// Token: 0x06000B83 RID: 2947 RVA: 0x0002DC38 File Offset: 0x0002BE38
		private void SetToken(JToken token)
		{
			switch (token.Type)
			{
			case JTokenType.Object:
				base.SetToken(JsonToken.StartObject);
				return;
			case JTokenType.Array:
				base.SetToken(JsonToken.StartArray);
				return;
			case JTokenType.Constructor:
				base.SetToken(JsonToken.StartConstructor, ((JConstructor)token).Name);
				return;
			case JTokenType.Property:
				base.SetToken(JsonToken.PropertyName, ((JProperty)token).Name);
				return;
			case JTokenType.Comment:
				base.SetToken(JsonToken.Comment, ((JValue)token).Value);
				return;
			case JTokenType.Integer:
				base.SetToken(JsonToken.Integer, ((JValue)token).Value);
				return;
			case JTokenType.Float:
				base.SetToken(JsonToken.Float, ((JValue)token).Value);
				return;
			case JTokenType.String:
				base.SetToken(JsonToken.String, ((JValue)token).Value);
				return;
			case JTokenType.Boolean:
				base.SetToken(JsonToken.Boolean, ((JValue)token).Value);
				return;
			case JTokenType.Null:
				base.SetToken(JsonToken.Null, ((JValue)token).Value);
				return;
			case JTokenType.Undefined:
				base.SetToken(JsonToken.Undefined, ((JValue)token).Value);
				return;
			case JTokenType.Date:
			{
				object obj = ((JValue)token).Value;
				if (obj is DateTime)
				{
					DateTime value = (DateTime)obj;
					obj = DateTimeUtils.EnsureDateTime(value, base.DateTimeZoneHandling);
				}
				base.SetToken(JsonToken.Date, obj);
				return;
			}
			case JTokenType.Raw:
				base.SetToken(JsonToken.Raw, ((JValue)token).Value);
				return;
			case JTokenType.Bytes:
				base.SetToken(JsonToken.Bytes, ((JValue)token).Value);
				return;
			case JTokenType.Guid:
				base.SetToken(JsonToken.String, this.SafeToString(((JValue)token).Value));
				return;
			case JTokenType.Uri:
			{
				object value2 = ((JValue)token).Value;
				JsonToken newToken = JsonToken.String;
				Uri uri = value2 as Uri;
				base.SetToken(newToken, (uri != null) ? uri.OriginalString : this.SafeToString(value2));
				return;
			}
			case JTokenType.TimeSpan:
				base.SetToken(JsonToken.String, this.SafeToString(((JValue)token).Value));
				return;
			default:
				throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", token.Type, "Unexpected JTokenType.");
			}
		}

		// Token: 0x06000B84 RID: 2948 RVA: 0x0002DE39 File Offset: 0x0002C039
		[NullableContext(2)]
		private string SafeToString(object value)
		{
			if (value == null)
			{
				return null;
			}
			return value.ToString();
		}

		// Token: 0x06000B85 RID: 2949 RVA: 0x0002DE48 File Offset: 0x0002C048
		bool IJsonLineInfo.HasLineInfo()
		{
			if (base.CurrentState == JsonReader.State.Start)
			{
				return false;
			}
			IJsonLineInfo current = this._current;
			return current != null && current.HasLineInfo();
		}

		// Token: 0x17000204 RID: 516
		// (get) Token: 0x06000B86 RID: 2950 RVA: 0x0002DE74 File Offset: 0x0002C074
		int IJsonLineInfo.LineNumber
		{
			get
			{
				if (base.CurrentState == JsonReader.State.Start)
				{
					return 0;
				}
				IJsonLineInfo current = this._current;
				if (current != null)
				{
					return current.LineNumber;
				}
				return 0;
			}
		}

		// Token: 0x17000205 RID: 517
		// (get) Token: 0x06000B87 RID: 2951 RVA: 0x0002DEA0 File Offset: 0x0002C0A0
		int IJsonLineInfo.LinePosition
		{
			get
			{
				if (base.CurrentState == JsonReader.State.Start)
				{
					return 0;
				}
				IJsonLineInfo current = this._current;
				if (current != null)
				{
					return current.LinePosition;
				}
				return 0;
			}
		}

		/// <summary>
		/// Gets the path of the current JSON token. 
		/// </summary>
		// Token: 0x17000206 RID: 518
		// (get) Token: 0x06000B88 RID: 2952 RVA: 0x0002DECC File Offset: 0x0002C0CC
		public override string Path
		{
			get
			{
				string text = base.Path;
				if (this._initialPath == null)
				{
					this._initialPath = this._root.Path;
				}
				if (!StringUtils.IsNullOrEmpty(this._initialPath))
				{
					if (StringUtils.IsNullOrEmpty(text))
					{
						return this._initialPath;
					}
					if (text.StartsWith('['))
					{
						text = this._initialPath + text;
					}
					else
					{
						text = this._initialPath + "." + text;
					}
				}
				return text;
			}
		}

		// Token: 0x040003A3 RID: 931
		private readonly JToken _root;

		// Token: 0x040003A4 RID: 932
		[Nullable(2)]
		private string _initialPath;

		// Token: 0x040003A5 RID: 933
		[Nullable(2)]
		private JToken _parent;

		// Token: 0x040003A6 RID: 934
		[Nullable(2)]
		private JToken _current;
	}
}
