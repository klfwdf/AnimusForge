using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Specifies how constructors are used when initializing objects during deserialization by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x0200000B RID: 11
	public enum ConstructorHandling
	{
		/// <summary>
		/// First attempt to use the public default constructor, then fall back to a single parameterized constructor, then to the non-public default constructor.
		/// </summary>
		// Token: 0x04000006 RID: 6
		Default,
		/// <summary>
		/// Json.NET will use a non-public default constructor before falling back to a parameterized constructor.
		/// </summary>
		// Token: 0x04000007 RID: 7
		AllowNonPublicDefaultConstructor
	}
}
