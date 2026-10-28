using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections;

internal static class ThrowHelper
{
	public static void ThrowIfNull(object arg, [CallerArgumentExpression("arg")] string? paramName = null)
	{
		if (arg == null)
		{
			ThrowArgumentNullException(paramName);
		}
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	public static void ThrowIfDestinationTooSmall()
	{
		throw new ArgumentException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.CapacityMustBeGreaterThanOrEqualToCount, "destination");
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	public static void ThrowArgumentNullException(string? paramName)
	{
		throw new ArgumentNullException(paramName);
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	public static void ThrowKeyNotFoundException()
	{
		throw new KeyNotFoundException();
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	public static void ThrowKeyNotFoundException<TKey>(TKey key)
	{
		throw new KeyNotFoundException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Format(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.Arg_KeyNotFoundWithKey, key));
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	public static void ThrowInvalidOperationException()
	{
		throw new InvalidOperationException();
	}

	[_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EDoesNotReturn]
	internal static void ThrowIncompatibleComparer()
	{
		throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidOperation_IncompatibleComparer);
	}
}
