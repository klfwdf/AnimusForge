using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Contract details for a <see cref="T:System.Type" /> used by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x0200008F RID: 143
	public class JsonISerializableContract : JsonContainerContract
	{
		/// <summary>
		/// Gets or sets the <see cref="T:System.Runtime.Serialization.ISerializable" /> object constructor.
		/// </summary>
		/// <value>The <see cref="T:System.Runtime.Serialization.ISerializable" /> object constructor.</value>
		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000700 RID: 1792 RVA: 0x0001D4B9 File Offset: 0x0001B6B9
		// (set) Token: 0x06000701 RID: 1793 RVA: 0x0001D4C1 File Offset: 0x0001B6C1
		[Nullable(new byte[] { 2, 1 })]
		public ObjectConstructor<object> ISerializableCreator
		{
			[return: Nullable(new byte[] { 2, 1 })]
			get;
			[param: Nullable(new byte[] { 2, 1 })]
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonISerializableContract" /> class.
		/// </summary>
		/// <param name="underlyingType">The underlying type for the contract.</param>
		// Token: 0x06000702 RID: 1794 RVA: 0x0001D4CA File Offset: 0x0001B6CA
		[NullableContext(1)]
		public JsonISerializableContract(Type underlyingType)
			: base(underlyingType)
		{
			this.ContractType = JsonContractType.Serializable;
		}
	}
}
