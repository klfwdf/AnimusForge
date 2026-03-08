using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// The default naming strategy. Property names and dictionary keys are unchanged.
	/// </summary>
	// Token: 0x02000076 RID: 118
	public class DefaultNamingStrategy : NamingStrategy
	{
		/// <summary>
		/// Resolves the specified property name.
		/// </summary>
		/// <param name="name">The property name to resolve.</param>
		/// <returns>The resolved property name.</returns>
		// Token: 0x0600065C RID: 1628 RVA: 0x0001BB1F File Offset: 0x00019D1F
		[NullableContext(1)]
		protected override string ResolvePropertyName(string name)
		{
			return name;
		}
	}
}
