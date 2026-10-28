using System;

namespace Bannerlord.UIExtenderEx.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class PrefabExtensionAttribute : BaseUIExtenderAttribute
{
	public string Movie { get; }

	public string? XPath { get; }

	[Obsolete("AutoGens are globally disabled for now. When the game will be released on Linux/OSX we'll reuse this property again.")]
	public string? AutoGenWidgetName { get; }

	public PrefabExtensionAttribute(string movie, string? xpath = null)
	{
		Movie = movie;
		XPath = xpath;
	}

	[Obsolete("AutoGens are globally disabled for now. When the game will be released on Linux/OSX we'll reuse this property again.")]
	public PrefabExtensionAttribute(string movie, string? xpath = null, string? autoGenWidgetName = null)
	{
		Movie = movie;
		XPath = xpath;
		AutoGenWidgetName = autoGenWidgetName;
	}
}
