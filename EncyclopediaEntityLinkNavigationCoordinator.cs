using System;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// RichText click callbacks only queue navigation; layer removal and encyclopedia activation happen safely on the next application tick.
internal static class EncyclopediaEntityLinkNavigationCoordinator
{
	private static string _pendingLink;

	private static Action _pendingDismiss;

	internal static void Request(string link, Action dismissBeforeOpen)
	{
		string normalizedLink = (link ?? string.Empty).Trim();
		if (normalizedLink.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
		{
			normalizedLink = normalizedLink.Substring("event:".Length);
		}
		if (string.IsNullOrWhiteSpace(normalizedLink) || normalizedLink.IndexOf('<') >= 0 || normalizedLink.IndexOf('>') >= 0)
		{
			return;
		}
		// A single pending navigation prevents double-clicks from opening two encyclopedia layers.
		if (string.IsNullOrWhiteSpace(_pendingLink))
		{
			_pendingLink = normalizedLink;
			_pendingDismiss = dismissBeforeOpen;
		}
	}

	internal static void ProcessPending()
	{
		string link = _pendingLink;
		Action dismiss = _pendingDismiss;
		if (string.IsNullOrWhiteSpace(link))
		{
			return;
		}
		_pendingLink = null;
		_pendingDismiss = null;
		try
		{
			dismiss?.Invoke();
		}
		catch (Exception ex)
		{
			Logger.Log("EntityLinks", "[WARN] Failed to dismiss linked-text popup: " + ex.Message);
		}
		try
		{
			Campaign.Current?.EncyclopediaManager?.GoToLink(link);
		}
		catch (Exception ex)
		{
			Logger.Log("EntityLinks", "[WARN] Failed to open encyclopedia link: " + ex.Message);
		}
	}
}
