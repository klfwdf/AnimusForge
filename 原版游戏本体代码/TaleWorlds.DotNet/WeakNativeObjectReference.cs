using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000032 RID: 50
	public sealed class WeakNativeObjectReference
	{
		// Token: 0x06000144 RID: 324 RVA: 0x00005866 File Offset: 0x00003A66
		public WeakNativeObjectReference(NativeObject nativeObject)
		{
			if (nativeObject != null)
			{
				this._pointer = nativeObject.Pointer;
				this._constructor = (Func<NativeObject>)Managed.GetConstructorDelegateOfWeakReferenceClass(nativeObject.GetType());
				this._weakReferenceCache = new WeakReference(nativeObject);
			}
		}

		// Token: 0x06000145 RID: 325 RVA: 0x000058A8 File Offset: 0x00003AA8
		public void ManualInvalidate()
		{
			NativeObject nativeObject = (NativeObject)this._weakReferenceCache.Target;
			if (nativeObject != null)
			{
				nativeObject.ManualInvalidate();
			}
		}

		// Token: 0x06000146 RID: 326 RVA: 0x000058D8 File Offset: 0x00003AD8
		public NativeObject GetNativeObject()
		{
			if (!(this._pointer != UIntPtr.Zero))
			{
				return null;
			}
			NativeObject nativeObject = (NativeObject)this._weakReferenceCache.Target;
			if (nativeObject != null)
			{
				return nativeObject;
			}
			NativeObject nativeObject2 = this._constructor();
			nativeObject2.Construct(this._pointer);
			this._weakReferenceCache.Target = nativeObject2;
			return nativeObject2;
		}

		// Token: 0x0400007C RID: 124
		private readonly UIntPtr _pointer;

		// Token: 0x0400007D RID: 125
		private readonly Func<NativeObject> _constructor;

		// Token: 0x0400007E RID: 126
		private readonly WeakReference _weakReferenceCache;
	}
}
