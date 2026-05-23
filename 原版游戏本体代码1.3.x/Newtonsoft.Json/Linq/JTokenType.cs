using System;

namespace Newtonsoft.Json.Linq
{
	/// <summary>
	/// Specifies the type of token.
	/// </summary>
	// Token: 0x020000CA RID: 202
	public enum JTokenType
	{
		/// <summary>
		/// No token type has been set.
		/// </summary>
		// Token: 0x040003A8 RID: 936
		None,
		/// <summary>
		/// A JSON object.
		/// </summary>
		// Token: 0x040003A9 RID: 937
		Object,
		/// <summary>
		/// A JSON array.
		/// </summary>
		// Token: 0x040003AA RID: 938
		Array,
		/// <summary>
		/// A JSON constructor.
		/// </summary>
		// Token: 0x040003AB RID: 939
		Constructor,
		/// <summary>
		/// A JSON object property.
		/// </summary>
		// Token: 0x040003AC RID: 940
		Property,
		/// <summary>
		/// A comment.
		/// </summary>
		// Token: 0x040003AD RID: 941
		Comment,
		/// <summary>
		/// An integer value.
		/// </summary>
		// Token: 0x040003AE RID: 942
		Integer,
		/// <summary>
		/// A float value.
		/// </summary>
		// Token: 0x040003AF RID: 943
		Float,
		/// <summary>
		/// A string value.
		/// </summary>
		// Token: 0x040003B0 RID: 944
		String,
		/// <summary>
		/// A boolean value.
		/// </summary>
		// Token: 0x040003B1 RID: 945
		Boolean,
		/// <summary>
		/// A null value.
		/// </summary>
		// Token: 0x040003B2 RID: 946
		Null,
		/// <summary>
		/// An undefined value.
		/// </summary>
		// Token: 0x040003B3 RID: 947
		Undefined,
		/// <summary>
		/// A date value.
		/// </summary>
		// Token: 0x040003B4 RID: 948
		Date,
		/// <summary>
		/// A raw JSON value.
		/// </summary>
		// Token: 0x040003B5 RID: 949
		Raw,
		/// <summary>
		/// A collection of bytes value.
		/// </summary>
		// Token: 0x040003B6 RID: 950
		Bytes,
		/// <summary>
		/// A Guid value.
		/// </summary>
		// Token: 0x040003B7 RID: 951
		Guid,
		/// <summary>
		/// A Uri value.
		/// </summary>
		// Token: 0x040003B8 RID: 952
		Uri,
		/// <summary>
		/// A TimeSpan value.
		/// </summary>
		// Token: 0x040003B9 RID: 953
		TimeSpan
	}
}
