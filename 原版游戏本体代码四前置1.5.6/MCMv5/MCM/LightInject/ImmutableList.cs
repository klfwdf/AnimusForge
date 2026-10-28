using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal sealed class ImmutableList<T>
{
	public static readonly ImmutableList<T> Empty = new ImmutableList<T>();

	public readonly T[] Items;

	public readonly int Count;

	public ImmutableList(ImmutableList<T> previousList, T value)
	{
		Items = new T[previousList.Items.Length + 1];
		Array.Copy(previousList.Items, Items, previousList.Items.Length);
		Items[Items.Length - 1] = value;
		Count = Items.Length;
	}

	private ImmutableList()
	{
		Items = new T[0];
	}

	public ImmutableList<T> Add(T value)
	{
		return new ImmutableList<T>(this, value);
	}
}
