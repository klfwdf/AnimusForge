using System.Resources;
using System.Runtime.CompilerServices;
using FxResources.System.Memory;

namespace System;

internal static class _003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR
{
	private static ResourceManager s_resourceManager;

	private static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(ResourceType));

	internal static Type ResourceType { get; } = typeof(SR);

	internal static string NotSupported_CannotCallEqualsOnSpan => GetResourceString("NotSupported_CannotCallEqualsOnSpan", null);

	internal static string NotSupported_CannotCallGetHashCodeOnSpan => GetResourceString("NotSupported_CannotCallGetHashCodeOnSpan", null);

	internal static string Argument_InvalidTypeWithPointersNotSupported => GetResourceString("Argument_InvalidTypeWithPointersNotSupported", null);

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort", null);

	internal static string MemoryDisposed => GetResourceString("MemoryDisposed", null);

	internal static string OutstandingReferences => GetResourceString("OutstandingReferences", null);

	internal static string Argument_BadFormatSpecifier => GetResourceString("Argument_BadFormatSpecifier", null);

	internal static string Argument_GWithPrecisionNotSupported => GetResourceString("Argument_GWithPrecisionNotSupported", null);

	internal static string Argument_CannotParsePrecision => GetResourceString("Argument_CannotParsePrecision", null);

	internal static string Argument_PrecisionTooLarge => GetResourceString("Argument_PrecisionTooLarge", null);

	internal static string Argument_OverlapAlignmentMismatch => GetResourceString("Argument_OverlapAlignmentMismatch", null);

	internal static string EndPositionNotReached => GetResourceString("EndPositionNotReached", null);

	internal static string UnexpectedSegmentType => GetResourceString("UnexpectedSegmentType", null);

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool UsingResourceKeys()
	{
		return false;
	}

	internal static string GetResourceString(string resourceKey, string defaultString)
	{
		string text = null;
		try
		{
			text = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		if (defaultString != null && resourceKey.Equals(text, StringComparison.Ordinal))
		{
			return defaultString;
		}
		return text;
	}

	internal static string Format(string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}
}
