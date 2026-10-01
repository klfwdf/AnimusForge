using System;
using System.Collections;
using System.Reflection;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

// RichText click callbacks only queue navigation; popup layers are suspended on the next application tick and restored after the encyclopedia closes.
internal static class EncyclopediaEntityLinkNavigationCoordinator
{
	// A short bounded fallback prevents a failed native link from leaving a popup hidden forever.
	private const int OpenObservationTimeoutTicks = 30;

	private static string _pendingLink;

	private static Action _pendingSuspend;

	private static Action _pendingResume;

	private static Action _activeResume;

	private static Game _eventGame;

	private static bool _pageChangedEventRegistered;

	private static bool _encyclopediaWasObserved;

	private static bool _resumeRequested;

	private static int _openObservationTicks;

	private static long _processSequence;

	private static long _resumeRequestedAtSequence;

	// The layer-name fallback also covers encyclopedia pages opened from a mission screen, where MapScreen is unavailable.
	private static readonly FieldInfo _screenLayersField = typeof(ScreenBase).GetField("_layers", BindingFlags.Instance | BindingFlags.NonPublic);

	internal static void Request(string link, Action suspendBeforeOpen, Action resumeAfterClose)
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
		// One active/pending navigation prevents double-clicks from suspending two unrelated popup layers.
		if (string.IsNullOrWhiteSpace(_pendingLink) && _activeResume == null)
		{
			_pendingLink = normalizedLink;
			_pendingSuspend = suspendBeforeOpen;
			_pendingResume = resumeAfterClose;
		}
	}

	internal static void ProcessPending()
	{
		_processSequence++;
		ProcessActiveNavigation();
		string link = _pendingLink;
		Action suspend = _pendingSuspend;
		Action resume = _pendingResume;
		if (string.IsNullOrWhiteSpace(link))
		{
			return;
		}
		_pendingLink = null;
		_pendingSuspend = null;
		_pendingResume = null;
		PrepareActiveNavigation(resume);
		try
		{
			// Suspending at this deferred point avoids mutating a Gauntlet layer from inside the RichText event callback.
			suspend?.Invoke();
		}
		catch (Exception ex)
		{
			Logger.Log("EntityLinks", "[WARN] Failed to suspend linked-text popup: " + ex.Message);
		}
		try
		{
			Campaign.Current?.EncyclopediaManager?.GoToLink(link);
		}
		catch (Exception ex)
		{
			Logger.Log("EntityLinks", "[WARN] Failed to open encyclopedia link: " + ex.Message);
			ResumeActiveNavigation();
		}
	}

	private static void PrepareActiveNavigation(Action resumeAfterClose)
	{
		_activeResume = resumeAfterClose;
		_encyclopediaWasObserved = false;
		_resumeRequested = false;
		_openObservationTicks = 0;
		_resumeRequestedAtSequence = 0L;
		// Subscribe only while a suspended popup needs restoring, so ordinary encyclopedia usage adds no recurring work.
		EnsurePageChangedEventRegistration();
	}

	private static void ProcessActiveNavigation()
	{
		if (_activeResume == null)
		{
			return;
		}
		if (_resumeRequested)
		{
			// The close event fires before the map view flips IsEncyclopediaOpen; wait until a later application tick.
			if (_processSequence > _resumeRequestedAtSequence && !IsEncyclopediaOpen())
			{
				ResumeActiveNavigation();
			}
			return;
		}
		if (IsEncyclopediaOpen())
		{
			_encyclopediaWasObserved = true;
			return;
		}
		if (_encyclopediaWasObserved)
		{
			// This is a compatibility fallback in case a third-party screen consumes the normal close event.
			_resumeRequested = true;
			_resumeRequestedAtSequence = _processSequence;
			return;
		}
		_openObservationTicks++;
		if (_openObservationTicks >= OpenObservationTimeoutTicks)
		{
			// GoToLink has no success return value, so restore after a bounded wait when no encyclopedia ever appeared.
			ResumeActiveNavigation();
		}
	}

	private static void EnsurePageChangedEventRegistration()
	{
		Game currentGame = Game.Current;
		if (ReferenceEquals(_eventGame, currentGame) && _pageChangedEventRegistered)
		{
			return;
		}
		UnregisterPageChangedEvent();
		if (currentGame == null)
		{
			return;
		}
		try
		{
			currentGame.EventManager.RegisterEvent<EncyclopediaPageChangedEvent>(OnEncyclopediaPageChanged);
			_eventGame = currentGame;
			_pageChangedEventRegistered = true;
		}
		catch (Exception ex)
		{
			Logger.Log("EntityLinks", "[WARN] Failed to subscribe to encyclopedia lifecycle: " + ex.Message);
		}
	}

	private static void UnregisterPageChangedEvent()
	{
		if (!_pageChangedEventRegistered || _eventGame == null)
		{
			_eventGame = null;
			_pageChangedEventRegistered = false;
			return;
		}
		try
		{
			_eventGame.EventManager.UnregisterEvent<EncyclopediaPageChangedEvent>(OnEncyclopediaPageChanged);
		}
		catch
		{
		}
		_eventGame = null;
		_pageChangedEventRegistered = false;
	}

	private static void OnEncyclopediaPageChanged(EncyclopediaPageChangedEvent pageChangedEvent)
	{
		if (_activeResume == null || pageChangedEvent == null)
		{
			return;
		}
		if (pageChangedEvent.NewPage != EncyclopediaPages.None)
		{
			_encyclopediaWasObserved = true;
			return;
		}
		if (_encyclopediaWasObserved)
		{
			// Defer restoration: this event is raised before the native map encyclopedia finishes closing its layer.
			_resumeRequested = true;
			_resumeRequestedAtSequence = _processSequence;
		}
	}

	private static bool IsEncyclopediaOpen()
	{
		try
		{
			ScreenBase topScreen = ScreenManager.TopScreen;
			if ((topScreen as MapScreen)?.EncyclopediaScreenManager?.IsEncyclopediaOpen == true)
			{
				return true;
			}
			return HasLayerNamed(topScreen, "EncyclopediaBar");
		}
		catch
		{
			return false;
		}
	}

	private static bool HasLayerNamed(ScreenBase screen, string layerName)
	{
		if (screen == null || string.IsNullOrEmpty(layerName))
		{
			return false;
		}
		try
		{
			if (_screenLayersField?.GetValue(screen) is not IEnumerable layers)
			{
				return false;
			}
			foreach (object item in layers)
			{
				if (item is ScreenLayer layer && string.Equals(layer.Name, layerName, StringComparison.Ordinal))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void ResumeActiveNavigation()
	{
		Action resume = _activeResume;
		_activeResume = null;
		_encyclopediaWasObserved = false;
		_resumeRequested = false;
		_openObservationTicks = 0;
		_resumeRequestedAtSequence = 0L;
		UnregisterPageChangedEvent();
		try
		{
			resume?.Invoke();
		}
		catch (Exception ex)
		{
			Logger.Log("EntityLinks", "[WARN] Failed to restore linked-text popup: " + ex.Message);
		}
	}
}
