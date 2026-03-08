using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Instructs the <see cref="T:Newtonsoft.Json.JsonSerializer" /> how to serialize the collection.
	/// </summary>
	// Token: 0x0200001E RID: 30
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
	public sealed class JsonDictionaryAttribute : JsonContainerAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonDictionaryAttribute" /> class.
		/// </summary>
		// Token: 0x06000092 RID: 146 RVA: 0x00002FA4 File Offset: 0x000011A4
		public JsonDictionaryAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.JsonDictionaryAttribute" /> class with the specified container Id.
		/// </summary>
		/// <param name="id">The container Id.</param>
		// Token: 0x06000093 RID: 147 RVA: 0x00002FAC File Offset: 0x000011AC
		[NullableContext(1)]
		public JsonDictionaryAttribute(string id)
			: base(id)
		{
		}
	}
}
