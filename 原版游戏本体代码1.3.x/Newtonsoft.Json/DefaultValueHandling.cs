using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies default value handling options for the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	/// <example>
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="ReducingSerializedJsonSizeDefaultValueHandlingObject" title="DefaultValueHandling Class" />
	///   <code lang="cs" source="..\Src\Newtonsoft.Json.Tests\Documentation\SerializationTests.cs" region="ReducingSerializedJsonSizeDefaultValueHandlingExample" title="DefaultValueHandling Ignore Example" />
	/// </example>
	// Token: 0x02000010 RID: 16
	[Flags]
	public enum DefaultValueHandling
	{
		/// <summary>
		/// Include members where the member value is the same as the member's default value when serializing objects.
		/// Included members are written to JSON. Has no effect when deserializing.
		/// </summary>
		// Token: 0x04000019 RID: 25
		Include = 0,
		/// <summary>
		/// Ignore members where the member value is the same as the member's default value when serializing objects
		/// so that it is not written to JSON.
		/// This option will ignore all default values (e.g. <c>null</c> for objects and nullable types; <c>0</c> for integers,
		/// decimals and floating point numbers; and <c>false</c> for booleans). The default value ignored can be changed by
		/// placing the <see cref="T:System.ComponentModel.DefaultValueAttribute" /> on the property.
		/// </summary>
		// Token: 0x0400001A RID: 26
		Ignore = 1,
		/// <summary>
		/// Members with a default value but no JSON will be set to their default value when deserializing.
		/// </summary>
		// Token: 0x0400001B RID: 27
		Populate = 2,
		/// <summary>
		/// Ignore members where the member value is the same as the member's default value when serializing objects
		/// and set members to their default value when deserializing.
		/// </summary>
		// Token: 0x0400001C RID: 28
		IgnoreAndPopulate = 3
	}
}
