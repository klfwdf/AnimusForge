using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Provides methods to get attributes.
	/// </summary>
	// Token: 0x0200007E RID: 126
	[NullableContext(1)]
	public interface IAttributeProvider
	{
		/// <summary>
		/// Returns a collection of all of the attributes, or an empty collection if there are no attributes.
		/// </summary>
		/// <param name="inherit">When <c>true</c>, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <returns>A collection of <see cref="T:System.Attribute" />s, or an empty collection.</returns>
		// Token: 0x06000682 RID: 1666
		IList<Attribute> GetAttributes(bool inherit);

		/// <summary>
		/// Returns a collection of attributes, identified by type, or an empty collection if there are no attributes.
		/// </summary>
		/// <param name="attributeType">The type of the attributes.</param>
		/// <param name="inherit">When <c>true</c>, look up the hierarchy chain for the inherited custom attribute.</param>
		/// <returns>A collection of <see cref="T:System.Attribute" />s, or an empty collection.</returns>
		// Token: 0x06000683 RID: 1667
		IList<Attribute> GetAttributes(Type attributeType, bool inherit);
	}
}
