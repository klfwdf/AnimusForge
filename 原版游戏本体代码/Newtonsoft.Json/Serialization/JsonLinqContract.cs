using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000090 RID: 144
	public class JsonLinqContract : JsonContract
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonLinqContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x06000703 RID: 1795 RVA: 0x0001D4DA File Offset: 0x0001B6DA
		[NullableContext(1)]
		public JsonLinqContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.Linq;
		}
	}
}
