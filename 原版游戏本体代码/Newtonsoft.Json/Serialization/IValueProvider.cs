using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Provides methods to get and set values.
	/// </summary>
	// Token: 0x02000083 RID: 131
	[NullableContext(1)]
	public interface IValueProvider
	{
		/// <summary>
		/// Sets the value.
		/// </summary>
		/// <param name="target">The target to set the value on.</param>
		/// <param name="value">The value to set on the target.</param>
		// Token: 0x0600068D RID: 1677
		void SetValue(object target, [Nullable(2)] object value);

		/// <summary>
		/// Gets the value.
		/// </summary>
		/// <param name="target">The target to get the value from.</param>
		/// <returns>The value.</returns>
		// Token: 0x0600068E RID: 1678
		[return: Nullable(2)]
		object GetValue(object target);
	}
}
