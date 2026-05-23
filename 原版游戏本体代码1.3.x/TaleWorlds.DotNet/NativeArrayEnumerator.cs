using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000028 RID: 40
	public sealed class NativeArrayEnumerator<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T> where T : struct
	{
		// Token: 0x060000FE RID: 254 RVA: 0x00004F55 File Offset: 0x00003155
		public NativeArrayEnumerator(NativeArray nativeArray)
		{
			this._nativeArray = nativeArray;
		}

		// Token: 0x17000029 RID: 41
		public T this[int index]
		{
			get
			{
				return this._nativeArray.GetElementAt<T>(index);
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000100 RID: 256 RVA: 0x00004F72 File Offset: 0x00003172
		public int Count
		{
			get
			{
				return this._nativeArray.GetLength<T>();
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00004F7F File Offset: 0x0000317F
		IEnumerator<T> IEnumerable<!0>.GetEnumerator()
		{
			return this._nativeArray.GetEnumerator<T>().GetEnumerator();
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00004F91 File Offset: 0x00003191
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this._nativeArray.GetEnumerator<T>().GetEnumerator();
		}

		// Token: 0x0400006A RID: 106
		private readonly NativeArray _nativeArray;
	}
}
