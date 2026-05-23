using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// A snake case naming strategy.
	/// </summary>
	// Token: 0x020000A3 RID: 163
	public class SnakeCaseNamingStrategy : NamingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy" /> class.
		/// </summary>
		/// <param name="processDictionaryKeys">
		/// A flag indicating whether dictionary keys should be processed.
		/// </param>
		/// <param name="overrideSpecifiedNames">
		/// A flag indicating whether explicitly specified property names should be processed,
		/// e.g. a property name customized with a <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" />.
		/// </param>
		// Token: 0x06000845 RID: 2117 RVA: 0x000242F2 File Offset: 0x000224F2
		public SnakeCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames)
		{
			base.ProcessDictionaryKeys = processDictionaryKeys;
			base.OverrideSpecifiedNames = overrideSpecifiedNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy" /> class.
		/// </summary>
		/// <param name="processDictionaryKeys">
		/// A flag indicating whether dictionary keys should be processed.
		/// </param>
		/// <param name="overrideSpecifiedNames">
		/// A flag indicating whether explicitly specified property names should be processed,
		/// e.g. a property name customized with a <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" />.
		/// </param>
		/// <param name="processExtensionDataNames">
		/// A flag indicating whether extension data names should be processed.
		/// </param>
		// Token: 0x06000846 RID: 2118 RVA: 0x00024308 File Offset: 0x00022508
		public SnakeCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames)
			: this(processDictionaryKeys, overrideSpecifiedNames)
		{
			base.ProcessExtensionDataNames = processExtensionDataNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy" /> class.
		/// </summary>
		// Token: 0x06000847 RID: 2119 RVA: 0x00024319 File Offset: 0x00022519
		public SnakeCaseNamingStrategy()
		{
		}

		/// <summary>
		/// Resolves the specified property name.
		/// </summary>
		/// <param name="name">The property name to resolve.</param>
		/// <returns>The resolved property name.</returns>
		// Token: 0x06000848 RID: 2120 RVA: 0x00024321 File Offset: 0x00022521
		[NullableContext(1)]
		protected override string ResolvePropertyName(string name)
		{
			return StringUtils.ToSnakeCase(name);
		}
	}
}
