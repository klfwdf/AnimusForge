using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000033 RID: 51
	public sealed class WeakNativeObjectReference<T> where T : NativeObject
	{
		// Token: 0x06000147 RID: 327 RVA: 0x0000593A File Offset: 0x00003B3A
		public WeakNativeObjectReference(T nativeObject)
		{
			if (nativeObject != null)
			{
				this._pointer = nativeObject.Pointer;
				this._weakReferenceCache = new WeakReference<T>(nativeObject);
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00005970 File Offset: 0x00003B70
		public void ManualInvalidate()
		{
			T t;
			if (this._weakReferenceCache.TryGetTarget(out t) && t != null)
			{
				t.ManualInvalidate();
			}
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000059A8 File Offset: 0x00003BA8
		public NativeObject GetNativeObject()
		{
			if (!(this._pointer != UIntPtr.Zero))
			{
				return null;
			}
			T t;
			if (this._weakReferenceCache.TryGetTarget(out t) && t != null)
			{
				return t;
			}
			T t2 = (T)((object)Activator.CreateInstance(typeof(T), new object[] { this._pointer }));
			this._weakReferenceCache.SetTarget(t2);
			return t2;
		}

		// Token: 0x0400007F RID: 127
		private readonly UIntPtr _pointer;

		// Token: 0x04000080 RID: 128
		private WeakReference<T> _weakReferenceCache;
	}
}
