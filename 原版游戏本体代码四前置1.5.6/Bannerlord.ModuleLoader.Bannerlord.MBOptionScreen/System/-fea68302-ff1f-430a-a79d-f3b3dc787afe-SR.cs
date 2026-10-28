using System.Resources;
using FxResources.System.Collections.Immutable;

namespace System;

internal static class _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR
{
	private static readonly bool s_usingResourceKeys = GetUsingResourceKeysSwitchValue();

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey");

	internal static string ArrayInitializedStateNotEqual => GetResourceString("ArrayInitializedStateNotEqual");

	internal static string ArrayLengthsNotEqual => GetResourceString("ArrayLengthsNotEqual");

	internal static string CannotFindOldValue => GetResourceString("CannotFindOldValue");

	internal static string CapacityMustBeGreaterThanOrEqualToCount => GetResourceString("CapacityMustBeGreaterThanOrEqualToCount");

	internal static string CapacityMustEqualCountOnMove => GetResourceString("CapacityMustEqualCountOnMove");

	internal static string CollectionModifiedDuringEnumeration => GetResourceString("CollectionModifiedDuringEnumeration");

	internal static string DuplicateKey => GetResourceString("DuplicateKey");

	internal static string InvalidEmptyOperation => GetResourceString("InvalidEmptyOperation");

	internal static string InvalidOperationOnDefaultArray => GetResourceString("InvalidOperationOnDefaultArray");

	internal static string Arg_HTCapacityOverflow => GetResourceString("Arg_HTCapacityOverflow");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string Arg_NonZeroLowerBound => GetResourceString("Arg_NonZeroLowerBound");

	internal static string Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall");

	internal static string Argument_IncompatibleArrayType => GetResourceString("Argument_IncompatibleArrayType");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string InvalidOperation_IncompatibleComparer => GetResourceString("InvalidOperation_IncompatibleComparer");

	private static bool GetUsingResourceKeysSwitchValue()
	{
		if (!AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled))
		{
			return false;
		}
		return isEnabled;
	}

	internal static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	private static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	private static string GetResourceString(string resourceKey, string defaultString)
	{
		string resourceString = GetResourceString(resourceKey);
		if (!(resourceKey == resourceString) && resourceString != null)
		{
			return resourceString;
		}
		return defaultString;
	}

	internal static string Format(string resourceFormat, object? p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object? p1, object? p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object? p1, object? p2, object? p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(string resourceFormat, params object?[]? args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(provider, resourceFormat, p1);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1, object? p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, object? p1, object? p2, object? p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(provider, resourceFormat, p1, p2, p3);
	}

	internal static string Format(IFormatProvider? provider, string resourceFormat, params object?[]? args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(provider, resourceFormat, args);
		}
		return resourceFormat;
	}
}
