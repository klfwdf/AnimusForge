using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace TaleWorlds.Library;

public struct MBStringBuilder
{
	private static class CachedStringBuilder
	{
		private const int MaxBuilderSize = 4096;

		[ThreadStatic]
		private static StringBuilder _cachedStringBuilder;

		public static StringBuilder Acquire(int capacity = 16)
		{
			if (capacity <= 4096 && _cachedStringBuilder != null)
			{
				StringBuilder cachedStringBuilder = _cachedStringBuilder;
				_cachedStringBuilder = null;
				cachedStringBuilder.EnsureCapacity(capacity);
				return cachedStringBuilder;
			}
			return new StringBuilder(capacity);
		}

		public static void Release(StringBuilder sb)
		{
			if (sb.Capacity <= 4096)
			{
				_cachedStringBuilder = sb;
				_cachedStringBuilder.Clear();
			}
		}

		public static string GetStringAndReleaseBuilder(StringBuilder sb)
		{
			string result = sb.ToString();
			Release(sb);
			return result;
		}
	}

	private StringBuilder _cachedStringBuilder;

	public int Length => _cachedStringBuilder.Length;

	public void Initialize(int capacity = 16, [CallerMemberName] string callerMemberName = "")
	{
		_cachedStringBuilder = CachedStringBuilder.Acquire(capacity);
	}

	public string ToStringAndRelease()
	{
		string result = _cachedStringBuilder.ToString();
		Release();
		return result;
	}

	public void Release()
	{
		CachedStringBuilder.Release(_cachedStringBuilder);
		_cachedStringBuilder = null;
	}

	public MBStringBuilder Append(char value)
	{
		_cachedStringBuilder.Append(value);
		return this;
	}

	public MBStringBuilder Append(int value)
	{
		_cachedStringBuilder.Append(value);
		return this;
	}

	public MBStringBuilder Append(uint value)
	{
		_cachedStringBuilder.Append(value);
		return this;
	}

	public MBStringBuilder Append(float value)
	{
		_cachedStringBuilder.Append(value);
		return this;
	}

	public MBStringBuilder Append(double value)
	{
		_cachedStringBuilder.Append(value);
		return this;
	}

	public MBStringBuilder Append<T>(T value)
	{
		_cachedStringBuilder.Append(value);
		return this;
	}

	public MBStringBuilder AppendLine()
	{
		_cachedStringBuilder.AppendLine();
		return this;
	}

	public MBStringBuilder AppendLine<T>(T value)
	{
		Append(value);
		AppendLine();
		return this;
	}

	public override string ToString()
	{
		Debug.FailedAssert("Don't use this. Use ToStringAndRelease instead!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\CachedStringBuilder.cs", "ToString", 190);
		return null;
	}
}
