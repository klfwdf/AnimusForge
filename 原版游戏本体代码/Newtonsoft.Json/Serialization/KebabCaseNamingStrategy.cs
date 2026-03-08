using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// A kebab case naming strategy.
	/// </summary>
	// Token: 0x0200009B RID: 155
	public class KebabCaseNamingStrategy : NamingStrategy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.KebabCaseNamingStrategy" /> class.
		/// </summary>
		/// <param name="processDictionaryKeys">
		/// A flag indicating whether dictionary keys should be processed.
		/// </param>
		/// <param name="overrideSpecifiedNames">
		/// A flag indicating whether explicitly specified property names should be processed,
		/// e.g. a property name customized with a <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" />.
		/// </param>
		// Token: 0x0600081F RID: 2079 RVA: 0x00023E82 File Offset: 0x00022082
		public KebabCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames)
		{
			base.ProcessDictionaryKeys = processDictionaryKeys;
			base.OverrideSpecifiedNames = overrideSpecifiedNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.KebabCaseNamingStrategy" /> class.
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
		// Token: 0x06000820 RID: 2080 RVA: 0x00023E98 File Offset: 0x00022098
		public KebabCaseNamingStrategy(bool processDictionaryKeys, bool overrideSpecifiedNames, bool processExtensionDataNames)
			: this(processDictionaryKeys, overrideSpecifiedNames)
		{
			base.ProcessExtensionDataNames = processExtensionDataNames;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.KebabCaseNamingStrategy" /> class.
		/// </summary>
		// Token: 0x06000821 RID: 2081 RVA: 0x00023EA9 File Offset: 0x000220A9
		public KebabCaseNamingStrategy()
		{
		}

		/// <summary>
		/// Resolves the specified property name.
		/// </summary>
		/// <param name="name">The property name to resolve.</param>
		/// <returns>The resolved property name.</returns>
		// Token: 0x06000822 RID: 2082 RVA: 0x00023EB1 File Offset: 0x000220B1
		[NullableContext(1)]
		protected override string ResolvePropertyName(string name)
		{
			return StringUtils.ToKebabCase(name);
		}
	}
}
