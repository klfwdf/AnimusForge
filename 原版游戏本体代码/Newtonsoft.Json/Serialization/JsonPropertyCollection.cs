using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// A collection of <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> objects.
	/// </summary>
	// Token: 0x02000094 RID: 148
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1, 1 })]
	public class JsonPropertyCollection : KeyedCollection<string, JsonProperty>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.JsonPropertyCollection" /> class.
		/// </summary>
		/// <param name="type">The type.</param>
		// Token: 0x06000762 RID: 1890 RVA: 0x0001DB64 File Offset: 0x0001BD64
		public JsonPropertyCollection(Type type)
			: base(StringComparer.Ordinal)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			this._type = type;
			this._list = (List<JsonProperty>)base.Items;
		}

		/// <summary>
		/// When implemented in a derived class, extracts the key from the specified element.
		/// </summary>
		/// <param name="item">The element from which to extract the key.</param>
		/// <returns>The key for the specified element.</returns>
		// Token: 0x06000763 RID: 1891 RVA: 0x0001DB94 File Offset: 0x0001BD94
		protected override string GetKeyForItem(JsonProperty item)
		{
			return item.PropertyName;
		}

		/// <summary>
		/// Adds a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> object.
		/// </summary>
		/// <param name="property">The property to add to the collection.</param>
		// Token: 0x06000764 RID: 1892 RVA: 0x0001DB9C File Offset: 0x0001BD9C
		public void AddProperty(JsonProperty property)
		{
			if (base.Contains(property.PropertyName))
			{
				if (property.Ignored)
				{
					return;
				}
				JsonProperty jsonProperty = base[property.PropertyName];
				bool flag = true;
				if (jsonProperty.Ignored)
				{
					base.Remove(jsonProperty);
					flag = false;
				}
				else if (property.DeclaringType != null && jsonProperty.DeclaringType != null)
				{
					if (property.DeclaringType.IsSubclassOf(jsonProperty.DeclaringType) || (jsonProperty.DeclaringType.IsInterface() && property.DeclaringType.ImplementInterface(jsonProperty.DeclaringType)))
					{
						base.Remove(jsonProperty);
						flag = false;
					}
					if (jsonProperty.DeclaringType.IsSubclassOf(property.DeclaringType) || (property.DeclaringType.IsInterface() && jsonProperty.DeclaringType.ImplementInterface(property.DeclaringType)))
					{
						return;
					}
					if (this._type.ImplementInterface(jsonProperty.DeclaringType) && this._type.ImplementInterface(property.DeclaringType))
					{
						return;
					}
				}
				if (flag)
				{
					throw new JsonSerializationException("A member with the name '{0}' already exists on '{1}'. Use the JsonPropertyAttribute to specify another name.".FormatWith(CultureInfo.InvariantCulture, property.PropertyName, this._type));
				}
			}
			base.Add(property);
		}

		/// <summary>
		/// Gets the closest matching <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> object.
		/// First attempts to get an exact case match of <paramref name="propertyName" /> and then
		/// a case insensitive match.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>A matching property if found.</returns>
		// Token: 0x06000765 RID: 1893 RVA: 0x0001DCD0 File Offset: 0x0001BED0
		[return: Nullable(2)]
		public JsonProperty GetClosestMatchProperty(string propertyName)
		{
			JsonProperty property = this.GetProperty(propertyName, StringComparison.Ordinal);
			if (property == null)
			{
				property = this.GetProperty(propertyName, StringComparison.OrdinalIgnoreCase);
			}
			return property;
		}

		// Token: 0x06000766 RID: 1894 RVA: 0x0001DCF3 File Offset: 0x0001BEF3
		private bool TryGetProperty(string key, [Nullable(2)] [NotNullWhen(true)] out JsonProperty item)
		{
			if (base.Dictionary == null)
			{
				item = null;
				return false;
			}
			return base.Dictionary.TryGetValue(key, out item);
		}

		/// <summary>
		/// Gets a property by property name.
		/// </summary>
		/// <param name="propertyName">The name of the property to get.</param>
		/// <param name="comparisonType">Type property name string comparison.</param>
		/// <returns>A matching property if found.</returns>
		// Token: 0x06000767 RID: 1895 RVA: 0x0001DD10 File Offset: 0x0001BF10
		[return: Nullable(2)]
		public JsonProperty GetProperty(string propertyName, StringComparison comparisonType)
		{
			if (comparisonType != StringComparison.Ordinal)
			{
				for (int i = 0; i < this._list.Count; i++)
				{
					JsonProperty jsonProperty = this._list[i];
					if (string.Equals(propertyName, jsonProperty.PropertyName, comparisonType))
					{
						return jsonProperty;
					}
				}
				return null;
			}
			JsonProperty result;
			if (this.TryGetProperty(propertyName, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x040002C3 RID: 707
		private readonly Type _type;

		// Token: 0x040002C4 RID: 708
		private readonly List<JsonProperty> _list;
	}
}
