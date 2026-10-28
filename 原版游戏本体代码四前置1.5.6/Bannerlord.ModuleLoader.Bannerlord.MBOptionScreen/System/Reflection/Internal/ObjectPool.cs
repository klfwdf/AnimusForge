using System.Threading;

namespace System.Reflection.Internal;

internal sealed class ObjectPool<T> where T : class
{
	private struct Element
	{
		internal T Value;
	}

	private readonly System.Reflection.Internal.ObjectPool<T>.Element[] _items;

	private readonly Func<T> _factory;

	internal ObjectPool(Func<T> factory)
		: this(factory, Environment.ProcessorCount * 2)
	{
	}

	internal ObjectPool(Func<T> factory, int size)
	{
		_factory = factory;
		_items = new Element[size];
	}

	private T CreateInstance()
	{
		return _factory();
	}

	internal T Allocate()
	{
		Element[] items = _items;
		int num = 0;
		T val;
		while (true)
		{
			if (num < items.Length)
			{
				val = items[num].Value;
				if (val != null && val == Interlocked.CompareExchange(ref items[num].Value, null, val))
				{
					break;
				}
				num++;
				continue;
			}
			val = CreateInstance();
			break;
		}
		return val;
	}

	internal void Free(T obj)
	{
		Element[] items = _items;
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i].Value == null)
			{
				items[i].Value = obj;
				break;
			}
		}
	}
}
