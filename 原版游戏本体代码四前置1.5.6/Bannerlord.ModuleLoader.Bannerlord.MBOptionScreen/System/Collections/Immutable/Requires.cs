using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

internal static class Requires
{
	[DebuggerStepThrough]
	public static void NotNull<T>([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNull] T value, string? parameterName) where T : class
	{
		if (value == null)
		{
			FailArgumentNullException(parameterName);
		}
	}

	[DebuggerStepThrough]
	public static T NotNullPassthrough<T>([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNull] T value, string? parameterName) where T : class
	{
		NotNull(value, parameterName);
		return value;
	}

	[DebuggerStepThrough]
	public static void NotNullAllowStructs<T>([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNull] T value, string? parameterName)
	{
		if (value == null)
		{
			FailArgumentNullException(parameterName);
		}
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	[DebuggerStepThrough]
	public static void FailArgumentNullException(string? parameterName)
	{
		throw new ArgumentNullException(parameterName);
	}

	[DebuggerStepThrough]
	public static void Range([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturnIf(false)] bool condition, string? parameterName, string? message = null)
	{
		if (!condition)
		{
			FailRange(parameterName, message);
		}
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	[DebuggerStepThrough]
	public static void FailRange(string? parameterName, string? message = null)
	{
		if (string.IsNullOrEmpty(message))
		{
			throw new ArgumentOutOfRangeException(parameterName);
		}
		throw new ArgumentOutOfRangeException(parameterName, message);
	}

	[DebuggerStepThrough]
	public static void Argument([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturnIf(false)] bool condition, string? parameterName, string? message)
	{
		if (!condition)
		{
			throw new ArgumentException(message, parameterName);
		}
	}

	[DebuggerStepThrough]
	public static void Argument([_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturnIf(false)] bool condition)
	{
		if (!condition)
		{
			throw new ArgumentException();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	[DebuggerStepThrough]
	public static void FailObjectDisposed<TDisposed>(TDisposed disposed)
	{
		throw new ObjectDisposedException(disposed.GetType().FullName);
	}
}
