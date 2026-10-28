using System.Text;

namespace System.Reflection.Internal;

internal sealed class PooledStringBuilder
{
	public readonly StringBuilder Builder = new StringBuilder();

	private readonly System.Reflection.Internal.ObjectPool<System.Reflection.Internal.PooledStringBuilder> _pool;

	private static readonly System.Reflection.Internal.ObjectPool<System.Reflection.Internal.PooledStringBuilder> s_poolInstance = CreatePool();

	public int Length => Builder.Length;

	private PooledStringBuilder(System.Reflection.Internal.ObjectPool<System.Reflection.Internal.PooledStringBuilder> pool)
	{
		_pool = pool;
	}

	public void Free()
	{
		StringBuilder builder = Builder;
		if (builder.Capacity <= 1024)
		{
			builder.Clear();
			_pool.Free(this);
		}
	}

	public string ToStringAndFree()
	{
		string result = Builder.ToString();
		Free();
		return result;
	}

	public static System.Reflection.Internal.ObjectPool<System.Reflection.Internal.PooledStringBuilder> CreatePool()
	{
		System.Reflection.Internal.ObjectPool<System.Reflection.Internal.PooledStringBuilder> pool = null;
		pool = new System.Reflection.Internal.ObjectPool<System.Reflection.Internal.PooledStringBuilder>(() => new System.Reflection.Internal.PooledStringBuilder(pool), 32);
		return pool;
	}

	public static System.Reflection.Internal.PooledStringBuilder GetInstance()
	{
		return s_poolInstance.Allocate();
	}
}
