using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Converters
{
	/// <summary>
	/// Provides a base class for converting a <see cref="T:System.DateTime" /> to and from JSON.
	/// </summary>
	// Token: 0x020000E5 RID: 229
	public abstract class DateTimeConverterBase : JsonConverter
	{
		/// <summary>
		/// Determines whether this instance can convert the specified object type.
		/// </summary>
		/// <param name="objectType">Type of the object.</param>
		/// <returns>
		/// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000C54 RID: 3156 RVA: 0x00031AE4 File Offset: 0x0002FCE4
		[NullableContext(1)]
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(DateTime) || objectType == typeof(DateTime?) || (objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?));
		}
	}
}
