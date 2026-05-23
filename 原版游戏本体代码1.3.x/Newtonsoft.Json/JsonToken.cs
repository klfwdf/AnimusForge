using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies the type of JSON token.
	/// </summary>
	// Token: 0x02000030 RID: 48
	public enum JsonToken
	{
		/// <summary>
		/// This is returned by the <see cref="T:Newtonsoft.Json.JsonReader" /> if a read method has not been called.
		/// </summary>
		// Token: 0x040000EE RID: 238
		None,
		/// <summary>
		/// An object start token.
		/// </summary>
		// Token: 0x040000EF RID: 239
		StartObject,
		/// <summary>
		/// An array start token.
		/// </summary>
		// Token: 0x040000F0 RID: 240
		StartArray,
		/// <summary>
		/// A constructor start token.
		/// </summary>
		// Token: 0x040000F1 RID: 241
		StartConstructor,
		/// <summary>
		/// An object property name.
		/// </summary>
		// Token: 0x040000F2 RID: 242
		PropertyName,
		/// <summary>
		/// A comment.
		/// </summary>
		// Token: 0x040000F3 RID: 243
		Comment,
		/// <summary>
		/// Raw JSON.
		/// </summary>
		// Token: 0x040000F4 RID: 244
		Raw,
		/// <summary>
		/// An integer.
		/// </summary>
		// Token: 0x040000F5 RID: 245
		Integer,
		/// <summary>
		/// A float.
		/// </summary>
		// Token: 0x040000F6 RID: 246
		Float,
		/// <summary>
		/// A string.
		/// </summary>
		// Token: 0x040000F7 RID: 247
		String,
		/// <summary>
		/// A boolean.
		/// </summary>
		// Token: 0x040000F8 RID: 248
		Boolean,
		/// <summary>
		/// A null token.
		/// </summary>
		// Token: 0x040000F9 RID: 249
		Null,
		/// <summary>
		/// An undefined token.
		/// </summary>
		// Token: 0x040000FA RID: 250
		Undefined,
		/// <summary>
		/// An object end token.
		/// </summary>
		// Token: 0x040000FB RID: 251
		EndObject,
		/// <summary>
		/// An array end token.
		/// </summary>
		// Token: 0x040000FC RID: 252
		EndArray,
		/// <summary>
		/// A constructor end token.
		/// </summary>
		// Token: 0x040000FD RID: 253
		EndConstructor,
		/// <summary>
		/// A Date.
		/// </summary>
		// Token: 0x040000FE RID: 254
		Date,
		/// <summary>
		/// Byte data.
		/// </summary>
		// Token: 0x040000FF RID: 255
		Bytes
	}
}
