using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Used by <see cref="T:Newtonsoft.Json.JsonSerializer" /> to resolve a <see cref="T:Newtonsoft.Json.Serialization.JsonContract" /> for a given <see cref="T:System.Type" />.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="ReducingSerializedJsonSizeContractResolverObject" title="IContractResolver Class" />
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="ReducingSerializedJsonSizeContractResolverExample" title="IContractResolver Example" />
	/// </example>
	// Token: 0x0200007F RID: 127
	[NullableContext(1)]
	public interface IContractResolver
	{
		/// <summary>
		/// Resolves the contract for a given type.
		/// </summary>
		/// <param name="type">The type to resolve a contract for.</param>
		/// <returns>The contract for a given type.</returns>
		// Token: 0x06000684 RID: 1668
		JsonContract ResolveContract(Type type);
	}
}
