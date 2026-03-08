using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Provides methods to get attributes from a <see cref="T:System.Type" />, <see cref="T:System.Reflection.MemberInfo" />, <see cref="T:System.Reflection.ParameterInfo" /> or <see cref="T:System.Reflection.Assembly" />.
	/// </summary>
	// Token: 0x020000A0 RID: 160
	[NullableContext(1)]
	[Nullable(0)]
	public class ReflectionAttributeProvider : IAttributeProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Newtonsoft.Json.Serialization.ReflectionAttributeProvider" /> class.
		/// </summary>
		/// <param name="attributeProvider">The instance to get attributes for. This parameter should be a <see cref="T:System.Type" />, <see cref="T:System.Reflection.MemberInfo" />, <see cref="T:System.Reflection.ParameterInfo" /> or <see cref="T:System.Reflection.Assembly" />.</param>
		// Token: 0x0600083C RID: 2108 RVA: 0x00024195 File Offset: 0x00022395
		public ReflectionAttributeProvider(object attributeProvider)
		{
			ValidationUtils.ArgumentNotNull(attributeProvider, "attributeProvider");
			this._attributeProvider = attributeProvider;
		}

		/// <summary>
		/// Returns a collection of all of the attributes, or an empty collection if there are no attributes.
		/// </summary>
		/// <param name="inherit">When <c>true</c>, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <returns>A collection of <see cref="T:System.Attribute" />s, or an empty collection.</returns>
		// Token: 0x0600083D RID: 2109 RVA: 0x000241AF File Offset: 0x000223AF
		public IList<Attribute> GetAttributes(bool inherit)
		{
			return ReflectionUtils.GetAttributes(this._attributeProvider, null, inherit);
		}

		/// <summary>
		/// Returns a collection of attributes, identified by type, or an empty collection if there are no attributes.
		/// </summary>
		/// <param name="attributeType">The type of the attributes.</param>
		/// <param name="inherit">When <c>true</c>, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <returns>A collection of <see cref="T:System.Attribute" />s, or an empty collection.</returns>
		// Token: 0x0600083E RID: 2110 RVA: 0x000241BE File Offset: 0x000223BE
		public IList<Attribute> GetAttributes(Type attributeType, bool inherit)
		{
			return ReflectionUtils.GetAttributes(this._attributeProvider, attributeType, inherit);
		}

		// Token: 0x040002E3 RID: 739
		private readonly object _attributeProvider;
	}
}
