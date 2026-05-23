using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies null value handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="ReducingSerializedJsonSizeNullValueHandlingObject" title="NullValueHandling Class" />
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="ReducingSerializedJsonSizeNullValueHandlingExample" title="NullValueHandling Ignore Example" />
	/// </example>
	// Token: 0x02000037 RID: 55
	public enum NullValueHandling
	{
		/// <summary>
		/// Include null values when serializing and deserializing objects.
		/// </summary>
		// Token: 0x04000122 RID: 290
		Include,
		/// <summary>
		/// Ignore null values when serializing and deserializing objects.
		/// </summary>
		// Token: 0x04000123 RID: 291
		Ignore
	}
}
