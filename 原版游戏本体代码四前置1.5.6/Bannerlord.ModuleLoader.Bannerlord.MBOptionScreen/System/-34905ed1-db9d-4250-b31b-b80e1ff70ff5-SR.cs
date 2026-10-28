using System.Resources;
using System.Runtime.CompilerServices;
using FxResources.System.Numerics.Vectors;

namespace System;

internal static class _003C34905ed1_002Ddb9d_002D4250_002Db31b_002Db80e1ff70ff5_003ESR
{
	private static ResourceManager s_resourceManager;

	private static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(ResourceType));

	internal static Type ResourceType { get; } = typeof(SR);

	internal static string Arg_ArgumentOutOfRangeException => GetResourceString("Arg_ArgumentOutOfRangeException", null);

	internal static string Arg_ElementsInSourceIsGreaterThanDestination => GetResourceString("Arg_ElementsInSourceIsGreaterThanDestination", null);

	internal static string Arg_NullArgumentNullRef => GetResourceString("Arg_NullArgumentNullRef", null);

	internal static string Arg_TypeNotSupported => GetResourceString("Arg_TypeNotSupported", null);

	internal static string Arg_InsufficientNumberOfElements => GetResourceString("Arg_InsufficientNumberOfElements", null);

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
