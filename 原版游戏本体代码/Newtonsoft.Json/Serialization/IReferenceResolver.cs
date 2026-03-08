using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Used to resolve references when serializing and deserializing JSON by the <see cref="T:Newtonsoft.Json.JsonSerializer" />.
	/// </summary>
	// Token: 0x02000080 RID: 128
	[NullableContext(1)]
	public interface IReferenceResolver
	{
		/// <summary>
		/// Resolves a reference to its object.
		/// </summary>
		/// <param name="context">The serialization context.</param>
		/// <param name="reference">The reference to resolve.</param>
		/// <returns>The object that was resolved from the reference.</returns>
		// Token: 0x06000685 RID: 1669
		object ResolveReference(object context, string reference);

		/// <summary>
		/// Gets the reference for the specified object.
		/// </summary>
		/// <param name="context">The serialization context.</param>
		/// <param name="value">The object to get a reference for.</param>
		/// <returns>The reference to the object.</returns>
		// Token: 0x06000686 RID: 1670
		string GetReference(object context, object value);

		/// <summary>
		/// Determines whether the specified object is referenced.
		/// </summary>
		/// <param name="context">The serialization context.</param>
		/// <param name="value">The object to test for a reference.</param>
		/// <returns>
		/// 	<c>true</c> if the specified object is referenced; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x06000687 RID: 1671
		bool IsReferenced(object context, object value);

		/// <summary>
		/// Adds a reference to the specified object.
		/// </summary>
		/// <param name="context">The serialization context.</param>
		/// <param name="reference">The reference.</param>
		/// <param name="value">The object to reference.</param>
		// Token: 0x06000688 RID: 1672
		void AddReference(object context, string reference, object value);
	}
}
