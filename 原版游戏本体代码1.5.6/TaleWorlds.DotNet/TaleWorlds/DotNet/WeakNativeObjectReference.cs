using System;

namespace TaleWorlds.DotNet;

public sealed class WeakNativeObjectReference
{
	private readonly UIntPtr _pointer;

	private readonly Func<NativeObject> _constructor;

	private readonly WeakReference _weakReferenceCache;

	public WeakNativeObjectReference(NativeObject nativeObject)
	{
		if (nativeObject != null)
		{
			_pointer = nativeObject.Pointer;
			_constructor = (Func<NativeObject>)Managed.GetConstructorDelegateOfWeakReferenceClass(nativeObject.GetType());
			_weakReferenceCache = new WeakReference(nativeObject);
		}
	}

	public void ManualInvalidate()
	{
		NativeObject nativeObject = (NativeObject)_weakReferenceCache.Target;
		if (nativeObject != null)
		{
			nativeObject.ManualInvalidate();
		}
	}

	public NativeObject GetNativeObject()
	{
		if (_pointer != UIntPtr.Zero)
		{
			NativeObject nativeObject = (NativeObject)_weakReferenceCache.Target;
			if (nativeObject != null)
			{
				return nativeObject;
			}
			NativeObject nativeObject2 = _constructor();
			nativeObject2.Construct(_pointer);
			_weakReferenceCache.Target = nativeObject2;
			return nativeObject2;
		}
		return null;
	}
}
public sealed class WeakNativeObjectReference<T> where T : NativeObject
{
	private readonly UIntPtr _pointer;

	private WeakReference<T> _weakReferenceCache;

	public WeakNativeObjectReference(T nativeObject)
	{
		if (nativeObject != null)
		{
			_pointer = nativeObject.Pointer;
			_weakReferenceCache = new WeakReference<T>(nativeObject);
		}
	}

	public void ManualInvalidate()
	{
		if (_weakReferenceCache.TryGetTarget(out var target) && target != null)
		{
			target.ManualInvalidate();
		}
	}

	public NativeObject GetNativeObject()
	{
		if (_pointer != UIntPtr.Zero)
		{
			if (_weakReferenceCache.TryGetTarget(out var target) && target != null)
			{
				return target;
			}
			T val = (T)Activator.CreateInstance(typeof(T), _pointer);
			_weakReferenceCache.SetTarget(val);
			return val;
		}
		return null;
	}
}
