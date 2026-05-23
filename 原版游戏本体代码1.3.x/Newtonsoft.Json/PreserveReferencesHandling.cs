using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies reference handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// Note that references cannot be preserved when a value is set via a non-default constructor such as types that implement <see cref="T:System.Runtime.Serialization.ISerializable" />.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="PreservingObjectReferencesOn" title="Preserve Object References" />       
	/// </example>
	// Token: 0x02000039 RID: 57
	[Flags]
	public enum PreserveReferencesHandling
	{
		/// <summary>
		/// Do not preserve references when serializing types.
		/// </summary>
		// Token: 0x04000129 RID: 297
		None = 0,
		/// <summary>
		/// Preserve references when serializing into a JSON object structure.
		/// </summary>
		// Token: 0x0400012A RID: 298
		Objects = 1,
		/// <summary>
		/// Preserve references when serializing into a JSON array structure.
		/// </summary>
		// Token: 0x0400012B RID: 299
		Arrays = 2,
		/// <summary>
		/// Preserve references when serializing.
		/// </summary>
		// Token: 0x0400012C RID: 300
		All = 3
	}
}
