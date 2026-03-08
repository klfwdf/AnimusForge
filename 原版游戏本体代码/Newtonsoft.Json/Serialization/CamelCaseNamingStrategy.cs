using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// A camel case naming strategy.
	/// </summary>
	// Token: 0x02000073 RID: 115
	public class CamelCaseNamingStrategy : NamingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.CamelCaseNamingStrategy" /> class.
		/// </summary>
		/// <param name="processDictionaryKeys">
		/// A flag indicating whether dictionary keys should be processed.
		/// </param>
		/// <param name="overrideSpecifiedNames">
		/// A flag indicating whether explicitly specified property names should be processed,
		/// e.g. a property name customized with a <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" />.
		/// </param>
		// Token: 0x06000613 RID: 1555 RVA: 0x00019B6D File Offset: 0x00017D6D
		public CamelCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames)
		{
			base.ProcessDictionaryKeys = processDictionaryKeys;
			base.OverrideSpecifiedNames = overrideSpecifiedNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.CamelCaseNamingStrategy" /> class.
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
		// Token: 0x06000614 RID: 1556 RVA: 0x00019B83 File Offset: 0x00017D83
		public CamelCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames)
			: this(processDictionaryKeys, overrideSpecifiedNames)
		{
			base.ProcessExtensionDataNames = processExtensionDataNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.CamelCaseNamingStrategy" /> class.
		/// </summary>
		// Token: 0x06000615 RID: 1557 RVA: 0x00019B94 File Offset: 0x00017D94
		public CamelCaseNamingStrategy()
		{
		}

		/// <summary>
		/// Resolves the specified property name.
		/// </summary>
		/// <param name="name">The property name to resolve.</param>
		/// <returns>The resolved property name.</returns>
		// Token: 0x06000616 RID: 1558 RVA: 0x00019B9C File Offset: 0x00017D9C
		[NullableContext(1)]
		protected override string ResolvePropertyName(string name)
		{
			return StringUtils.ToCamelCase(name);
		}
	}
}
