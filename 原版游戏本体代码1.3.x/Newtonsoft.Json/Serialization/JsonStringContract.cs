using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000099 RID: 153
	public class JsonStringContract : JsonPrimitiveContract
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonStringContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x06000809 RID: 2057 RVA: 0x000238FD File Offset: 0x00021AFD
		[NullableContext(1)]
		public JsonStringContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.String;
		}
	}
}
