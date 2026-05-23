using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Allows users to control class loading and mandate what class to load.
	/// </summary>
	// Token: 0x02000081 RID: 129
	[NullableContext(1)]
	public interface ISerializationBinder
	{
		/// <summary>
		/// When implemented, controls the binding of a serialized object to a type.
		/// </summary>
		/// <param name="assemblyName">Specifies the <see cref="T:System.Reflection.Assembly" /> name of the serialized object.</param>
		/// <param name="typeName">Specifies the <see cref="T:System.Type" /> name of the serialized object</param>
		/// <returns>The type of the object the formatter creates a new instance of.</returns>
		// Token: 0x06000689 RID: 1673
		Type BindToType([Nullable(2)] string assemblyName, string typeName);

		/// <summary>
		/// When implemented, controls the binding of a serialized object to a type.
		/// </summary>
		/// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
		/// <param name="assemblyName">Specifies the <see cref="T:System.Reflection.Assembly" /> name of the serialized object.</param>
		/// <param name="typeName">Specifies the <see cref="T:System.Type" /> name of the serialized object.</param>
		// Token: 0x0600068A RID: 1674
		[NullableContext(2)]
		void BindToName([Nullable(1)] Type serializedType, out string assemblyName, out string typeName);
	}
}
