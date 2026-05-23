using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Provides an interface for using pooled arrays.
	/// </summary>
	/// <typeparam name="T">The array type content.</typeparam>
	// Token: 0x02000014 RID: 20
	[NullableContext(1)]
	public interface IArrayPool<[Nullable(2)] T>
	{
		/// <summary>
		/// Rent an array from the pool. This array must be returned when it is no longer needed.
		/// </summary>
		/// <param name="minimumLength">The minimum required length of the array. The returned array may be longer.</param>
		/// <returns>The rented array from the pool. This array must be returned when it is no longer needed.</returns>
		// Token: 0x06000014 RID: 20
		T[] Rent(int minimumLength);

		/// <summary>
		/// Return an array to the pool.
		/// </summary>
		/// <param name="array">The array that is being returned.</param>
		// Token: 0x06000015 RID: 21
		void Return([Nullable(new byte[] { 2, 1 })] T[] array);
	}
}
