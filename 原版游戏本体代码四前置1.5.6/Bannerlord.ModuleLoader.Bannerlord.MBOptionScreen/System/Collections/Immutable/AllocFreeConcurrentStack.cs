using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable;

internal static class AllocFreeConcurrentStack<T>
{
	private const int MaxSize = 35;

	private static readonly Type s_typeOfT = typeof(T);

	private static Stack<RefAsValueType<T>> ThreadLocalStack
	{
		get
		{
			Dictionary<Type, object> dictionary = AllocFreeConcurrentStack.t_stacks ?? (AllocFreeConcurrentStack.t_stacks = new Dictionary<Type, object>());
			if (!dictionary.TryGetValue(s_typeOfT, out var value))
			{
				value = new Stack<RefAsValueType<T>>(35);
				dictionary.Add(s_typeOfT, value);
			}
			return (Stack<RefAsValueType<T>>)value;
		}
	}

	public static void TryAdd(T item)
	{
		Stack<RefAsValueType<T>> threadLocalStack = ThreadLocalStack;
		if (threadLocalStack.Count < 35)
		{
			threadLocalStack.Push(new RefAsValueType<T>(item));
		}
	}

	public static bool TryTake([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMaybeNullWhen(false)] out T item)
	{
		Stack<RefAsValueType<T>> threadLocalStack = ThreadLocalStack;
		if (threadLocalStack != null && threadLocalStack.Count > 0)
		{
			item = threadLocalStack.Pop().Value;
			return true;
		}
		item = default(T);
		return false;
	}
}
internal static class AllocFreeConcurrentStack
{
	[ThreadStatic]
	internal static Dictionary<Type, object>? t_stacks;
}
