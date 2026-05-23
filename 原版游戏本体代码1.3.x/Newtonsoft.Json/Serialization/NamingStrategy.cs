using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// A base class for resolving how property names and dictionary keys are serialized.
	/// </summary>
	// Token: 0x0200009D RID: 157
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class NamingStrategy
	{
		/// <summary>
		/// A flag indicating whether dictionary keys should be processed.
		/// Defaults to <c>false</c>.
		/// </summary>
		// Token: 0x17000161 RID: 353
		// (get) Token: 0x06000829 RID: 2089 RVA: 0x0002405C File Offset: 0x0002225C
		// (set) Token: 0x0600082A RID: 2090 RVA: 0x00024064 File Offset: 0x00022264
		public bool ProcessDictionaryKeys { get; set; }

		/// <summary>
		/// A flag indicating whether extension data names should be processed.
		/// Defaults to <c>false</c>.
		/// </summary>
		// Token: 0x17000162 RID: 354
		// (get) Token: 0x0600082B RID: 2091 RVA: 0x0002406D File Offset: 0x0002226D
		// (set) Token: 0x0600082C RID: 2092 RVA: 0x00024075 File Offset: 0x00022275
		public bool ProcessExtensionDataNames { get; set; }

		/// <summary>
		/// A flag indicating whether explicitly specified property names,
		/// e.g. a property name customized with a <see cref="T:Newtonsoft.Json.JsonPropertyAttribute" />, should be processed.
		/// Defaults to <c>false</c>.
		/// </summary>
		// Token: 0x17000163 RID: 355
		// (get) Token: 0x0600082D RID: 2093 RVA: 0x0002407E File Offset: 0x0002227E
		// (set) Token: 0x0600082E RID: 2094 RVA: 0x00024086 File Offset: 0x00022286
		public bool OverrideSpecifiedNames { get; set; }

		/// <summary>
		/// Gets the serialized name for a given property name.
		/// </summary>
		/// <param name="name">The initial property name.</param>
		/// <param name="hasSpecifiedName">A flag indicating whether the property has had a name explicitly specified.</param>
		/// <returns>The serialized property name.</returns>
		// Token: 0x0600082F RID: 2095 RVA: 0x0002408F File Offset: 0x0002228F
		public virtual string GetPropertyName(string name, bool hasSpecifiedName)
		{
			if (hasSpecifiedName && !this.OverrideSpecifiedNames)
			{
				return name;
			}
			return this.ResolvePropertyName(name);
		}

		/// <summary>
		/// Gets the serialized name for a given extension data name.
		/// </summary>
		/// <param name="name">The initial extension data name.</param>
		/// <returns>The serialized extension data name.</returns>
		// Token: 0x06000830 RID: 2096 RVA: 0x000240A5 File Offset: 0x000222A5
		public virtual string GetExtensionDataName(string name)
		{
			if (!this.ProcessExtensionDataNames)
			{
				return name;
			}
			return this.ResolvePropertyName(name);
		}

		/// <summary>
		/// Gets the serialized key for a given dictionary key.
		/// </summary>
		/// <param name="key">The initial dictionary key.</param>
		/// <returns>The serialized dictionary key.</returns>
		// Token: 0x06000831 RID: 2097 RVA: 0x000240B8 File Offset: 0x000222B8
		public virtual string GetDictionaryKey(string key)
		{
			if (!this.ProcessDictionaryKeys)
			{
				return key;
			}
			return this.ResolvePropertyName(key);
		}

		/// <summary>
		/// Resolves the specified property name.
		/// </summary>
		/// <param name="name">The property name to resolve.</param>
		/// <returns>The resolved property name.</returns>
		// Token: 0x06000832 RID: 2098
		protected abstract string ResolvePropertyName(string name);

		/// <summary>
		/// Hash code calculation
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000833 RID: 2099 RVA: 0x000240CC File Offset: 0x000222CC
		public override int GetHashCode()
		{
			return (((((base.GetType().GetHashCode() * 397) ^ this.ProcessDictionaryKeys.GetHashCode()) * 397) ^ this.ProcessExtensionDataNames.GetHashCode()) * 397) ^ this.OverrideSpecifiedNames.GetHashCode();
		}

		/// <summary>
		/// Object equality implementation
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		// Token: 0x06000834 RID: 2100 RVA: 0x00024123 File Offset: 0x00022323
		[NullableContext(2)]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as NamingStrategy);
		}

		/// <summary>
		/// Compare to another NamingStrategy
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		// Token: 0x06000835 RID: 2101 RVA: 0x00024134 File Offset: 0x00022334
		[NullableContext(2)]
		protected bool Equals(NamingStrategy other)
		{
			return other != null && (base.GetType() == other.GetType() && this.ProcessDictionaryKeys == other.ProcessDictionaryKeys && this.ProcessExtensionDataNames == other.ProcessExtensionDataNames) && this.OverrideSpecifiedNames == other.OverrideSpecifiedNames;
		}
	}
}
