using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed class AnimusForgeNativeConversationOverlay
{
	private const int WaitingDotsIntervalMilliseconds = 350;

	private const string EncyclopediaLayerName = "EncyclopediaBar";

	private const string MissionEscapeMenuLayerName = "MissionEscapeMenu";

	private const string MissionOptionsLayerName = "MissionOptions";

	private const string MapEscapeMenuLayerName = "MapEscapeMenu";

	private const string MapCampaignOptionsLayerName = "MapCampaignOptions";

	private const string MapConversationLayerName = "MapConversation";

	private const string MissionConversationLayerName = "MissionConversation";

	private static readonly FieldInfo _screenLayersField = typeof(ScreenBase).GetField("_layers", BindingFlags.Instance | BindingFlags.NonPublic);

	private static AnimusForgeNativeConversationOverlay _activeOverlay;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private GauntletMovieIdentifier _movieIdentifier;

	private readonly AnimusForgeNativeConversationOverlayVM _dataSource;

	private bool _isClosed;

	private bool _isSubmitting;

	private bool _npcOpeningAutoStarted;

	private int _submitGeneration;

	private bool _waitingDotsActive;

	private int _waitingDotsGeneration;

	private int _waitingDotsPhase;

	private long _nextWaitingDotsUpdateUtcTicks;

	private readonly object _postprocessNoticeLock = new object();

	private bool _hasPendingPostprocessNotice;

	private string _pendingPostprocessNoticeNpcName;

	private int _pendingPostprocessNoticeGeneration = -1;

	private int _queuedPostprocessNoticeGeneration = -1;

	private bool _temporarySystemUiActive;

	private bool _isHiddenForTemporarySystemUi;

	private int _postRestoreForceRestoreTicks;

	public static bool IsOpen => _activeOverlay != null && !_activeOverlay._isClosed;

	private AnimusForgeNativeConversationOverlay(ScreenBase screen)
	{
		_screen = screen;
		_dataSource = new AnimusForgeNativeConversationOverlayVM(HandleSubmitRequested, HandleSwitchTalkRequested, HandleShowHistoryRequested, HandleGiveShowRequested, HandleEditPersonaRequested, HandleTagTestRequested);
		_layer = new GauntletLayer("AnimusForgeNativeConversationOverlay", 350, false);
	}

	public static void OnApplicationTick()
	{
		try
		{
			if (!ShoutBehavior.CanSubmitNativeConversationForExternal() || ShoutTextInputPopup.IsOpen)
			{
				CloseActive();
				return;
			}
			ScreenBase topScreen = ScreenManager.TopScreen;
			if (topScreen == null)
			{
				return;
			}
			if (_activeOverlay == null || _activeOverlay._isClosed)
			{
				if (IsKnownTemporarySystemScreen(topScreen))
				{
					return;
				}
				Show(topScreen);
				return;
			}
			if (_activeOverlay.TickTemporarySystemUiIfNeeded(topScreen))
			{
				return;
			}
			if (!ReferenceEquals(_activeOverlay._screen, topScreen))
			{
				CloseActive();
				if (!IsKnownTemporarySystemScreen(topScreen))
				{
					Show(topScreen);
				}
				return;
			}
			_activeOverlay.Tick();
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] OnApplicationTick failed: " + ex.Message);
		}
	}

	public static void CloseActive()
	{
		_activeOverlay?.Close(silent: true);
	}

	private static bool Show(ScreenBase screen)
	{
		try
		{
			AnimusForgeNativeConversationOverlay overlay = new AnimusForgeNativeConversationOverlay(screen);
			overlay.Open();
			_activeOverlay = overlay;
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[ERROR] Failed to open overlay: " + ex);
			CloseActive();
			return false;
		}
	}

	private void Open()
	{
		_movieIdentifier = _layer.LoadMovie("AnimusForgeNativeConversationOverlay", _dataSource);
		SetLayerForButtonsOnly();
		_screen.AddLayer(_layer);
	}

	private void Tick()
	{
		if (_isClosed)
		{
			return;
		}
		ProcessPostRestoreNativeAnswerRestore();
		FlushPendingPostprocessNotice();
		UpdateWaitingDotsAnimation();
		_dataSource.SetPersonaEditVisible(ShoutBehavior.CanEditNativeConversationNpcForExternal());
		_dataSource.SetTagTestVisible(ShoutBehavior.CanOpenNativeConversationTagTestForExternal());
		TryStartPendingNpcOpening();
		if (!_dataSource.IsCustomAnswerVisible)
		{
			RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: false);
		}
		else
		{
			NativeConversationAnswerAreaController.SetSuppressed(true);
		}
	}

	private void HandleSwitchTalkRequested()
	{
		if (_isClosed)
		{
			return;
		}
		SetInputVisible(!_dataSource.IsCustomAnswerVisible);
	}

	private void SetInputVisible(bool isVisible)
	{
		try
		{
			if (!isVisible)
			{
				StopWaitingDotsAnimation();
				ClearPendingPostprocessNotice();
				_submitGeneration++;
			}
			_dataSource.SetInputVisible(isVisible);
			if (isVisible)
			{
				NativeConversationAnswerAreaController.SetSuppressed(true);
				ShoutBehavior.OpenNativeConversationInputSilentlyForExternal();
				FocusInputIfVisible();
			}
			else
			{
				ShoutBehavior.CloseNativeConversationInputForExternal();
				_postRestoreForceRestoreTicks = 8;
				RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to switch input visibility: " + ex.Message);
		}
	}

	private void SetLayerForButtonsOnly()
	{
		UpdateButtonsOnlyInputRestrictions();
	}

	private bool TickTemporarySystemUiIfNeeded(ScreenBase topScreen)
	{
		if (_isClosed)
		{
			return false;
		}
		if (IsTemporarySystemUiBlocking(topScreen))
		{
			BeginTemporarySystemUiInterruption();
			return true;
		}
		if (_temporarySystemUiActive)
		{
			RestoreOverlayAfterTemporarySystemUi();
		}
		return false;
	}

	private void BeginTemporarySystemUiInterruption()
	{
		if (_isClosed)
		{
			return;
		}
		if (!_temporarySystemUiActive)
		{
			Logger.LogTrace("NativeConversationOverlay", "Temporary system UI interruption detected; releasing overlay input and native answer suppression.");
		}
		_temporarySystemUiActive = true;
		HideOverlayForTemporarySystemUi();
	}

	private bool IsTemporarySystemUiBlocking(ScreenBase topScreen)
	{
		if (topScreen == null)
		{
			return false;
		}
		if (!ReferenceEquals(topScreen, _screen))
		{
			return true;
		}
		return IsKnownTemporarySystemScreen(topScreen) || IsKnownTemporarySystemScreen(_screen);
	}

	private static bool IsKnownTemporarySystemScreen(ScreenBase screen)
	{
		if (screen == null)
		{
			return false;
		}
		try
		{
			MapScreen mapScreen = screen as MapScreen;
			if (mapScreen != null && (mapScreen.IsEscapeMenuOpened || mapScreen.IsInCampaignOptions))
			{
				return true;
			}
		}
		catch
		{
		}
		if (HasLayerNamed(screen, MapEscapeMenuLayerName)
			|| HasLayerNamed(screen, MapCampaignOptionsLayerName)
			|| HasLayerNamed(screen, MissionEscapeMenuLayerName)
			|| HasLayerNamed(screen, MissionOptionsLayerName)
			|| HasLayerNamed(screen, EncyclopediaLayerName))
		{
			return true;
		}
		try
		{
			string typeName = screen.GetType()?.FullName ?? "";
			return typeName.IndexOf("Options", StringComparison.OrdinalIgnoreCase) >= 0
				|| typeName.IndexOf("SaveLoad", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool HasLayerNamed(ScreenBase screen, string layerName)
	{
		return TryFindLayerNamed(screen, layerName) != null;
	}

	private static ScreenLayer TryFindLayerNamed(ScreenBase screen, string layerName)
	{
		if (screen == null || string.IsNullOrEmpty(layerName))
		{
			return null;
		}
		try
		{
			if (!(_screenLayersField?.GetValue(screen) is IEnumerable layers))
			{
				return null;
			}
			foreach (object item in layers)
			{
				if (item is ScreenLayer layer && string.Equals(layer.Name, layerName, StringComparison.Ordinal))
				{
					return layer;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private void HideOverlayForTemporarySystemUi()
	{
		if (!_isHiddenForTemporarySystemUi)
		{
			_isHiddenForTemporarySystemUi = true;
			try
			{
				_movieIdentifier?.Movie?.RootWidget?.Hide();
			}
			catch
			{
			}
			try
			{
				_layer.TwoDimensionView.SetEnable(false);
			}
			catch
			{
			}
		}
		try
		{
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		NativeConversationAnswerAreaController.SetSuppressed(false);
		NativeConversationAnswerAreaController.ForceRestoreAll();
	}

	private void RestoreOverlayAfterTemporarySystemUi()
	{
		if (_isClosed)
		{
			return;
		}
		_temporarySystemUiActive = false;
		_postRestoreForceRestoreTicks = 8;
		if (_isHiddenForTemporarySystemUi)
		{
			try
			{
				_layer.TwoDimensionView.SetEnable(true);
				_movieIdentifier?.Movie?.RootWidget?.Show();
			}
			catch
			{
			}
			_isHiddenForTemporarySystemUi = false;
		}
		if (_dataSource.IsCustomAnswerVisible)
		{
			NativeConversationAnswerAreaController.SetSuppressed(true);
			ShoutBehavior.OpenNativeConversationInputSilentlyForExternal();
			if (_isSubmitting)
			{
				SetLayerForButtonsOnly();
			}
			else
			{
				FocusInputIfVisible();
			}
		}
		else
		{
			RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
		}
		Logger.LogTrace("NativeConversationOverlay", "Temporary system UI interruption ended; restored overlay state.");
	}

	private void ProcessPostRestoreNativeAnswerRestore()
	{
		if (_postRestoreForceRestoreTicks <= 0)
		{
			return;
		}
		_postRestoreForceRestoreTicks--;
		if (!_dataSource.IsCustomAnswerVisible)
		{
			RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
		}
	}

	private void RestoreNativeConversationInputAfterOrdinaryMode(bool forceAnswerRestore)
	{
		NativeConversationAnswerAreaController.SetSuppressed(false);
		if (forceAnswerRestore)
		{
			NativeConversationAnswerAreaController.ForceRestoreAll();
		}
		SetLayerForButtonsOnly();
		if (!IsMouseOverTopRightButtons())
		{
			TryFocusNativeConversationLayer();
		}
	}

	private void TryFocusNativeConversationLayer()
	{
		try
		{
			ScreenLayer nativeLayer = TryFindLayerNamed(_screen, MapConversationLayerName) ?? TryFindLayerNamed(_screen, MissionConversationLayerName);
			if (nativeLayer == null)
			{
				return;
			}
			nativeLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(nativeLayer);
		}
		catch
		{
		}
	}

	private void UpdateButtonsOnlyInputRestrictions()
	{
		try
		{
			if (IsMouseOverTopRightButtons())
			{
				_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.MouseButtons);
			}
			else
			{
				_layer.InputRestrictions.ResetInputRestrictions();
			}
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
	}

	private bool IsMouseOverTopRightButtons()
	{
		try
		{
			Vec2 mouse = Input.MousePositionPixel;
			float width = TaleWorlds.Engine.Screen.RealScreenResolutionWidth;
			if (width <= 0f)
			{
				return false;
			}
			float bottom = _dataSource?.IsTagTestVisible == true ? 335f : (_dataSource?.IsPersonaEditVisible == true ? 285f : 235f);
			return mouse.x >= width - 330f && mouse.y >= 60f && mouse.y <= bottom;
		}
		catch
		{
			return false;
		}
	}

	private void HandleShowHistoryRequested()
	{
		if (_isClosed)
		{
			return;
		}
		try
		{
			HideOverlayRoot();
			if (!AnimusForgeConversationHistoryLogPopup.ShowForNativeConversation(RestoreFocusAfterHistory))
			{
				ShowOverlayRoot();
			}
		}
		catch (Exception ex)
		{
			ShowOverlayRoot();
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to open history: " + ex.Message);
		}
	}

	private void HandleGiveShowRequested()
	{
		if (_isClosed)
		{
			return;
		}
		try
		{
			HideOverlayRoot();
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			if (!ShoutBehavior.OpenNativeConversationGiveShowForExternal(RestoreFocusAfterHistory))
			{
				ShowOverlayRoot();
				RestoreFocusAfterHistory();
			}
		}
		catch (Exception ex)
		{
			ShowOverlayRoot();
			RestoreFocusAfterHistory();
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to open give/show menu: " + ex.Message);
		}
	}

	private void HandleEditPersonaRequested()
	{
		if (_isClosed)
		{
			return;
		}
		try
		{
			HideOverlayRoot();
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			if (!ShoutBehavior.OpenNativeConversationNpcEditorForExternal(RestoreFocusAfterHistory))
			{
				ShowOverlayRoot();
				RestoreFocusAfterHistory();
			}
		}
		catch (Exception ex)
		{
			ShowOverlayRoot();
			RestoreFocusAfterHistory();
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to open NPC editor: " + ex.Message);
		}
	}

	private void HandleTagTestRequested()
	{
		if (_isClosed)
		{
			return;
		}
		try
		{
			HideOverlayRoot();
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
			if (!ShoutBehavior.OpenNativeConversationTagTestForExternal(RestoreFocusAfterHistory))
			{
				ShowOverlayRoot();
				RestoreFocusAfterHistory();
			}
		}
		catch (Exception ex)
		{
			ShowOverlayRoot();
			RestoreFocusAfterHistory();
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to open tag test input: " + ex.Message);
		}
	}

	private void RestoreFocusAfterHistory()
	{
		if (_isClosed)
		{
			return;
		}
		ShowOverlayRoot();
		if (_dataSource.IsCustomAnswerVisible)
		{
			FocusInputIfVisible();
		}
		else
		{
			RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
		}
	}

	private void FocusInputIfVisible()
	{
		if (_isClosed || !_dataSource.IsCustomAnswerVisible)
		{
			return;
		}
		try
		{
			_layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			_layer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(_layer);
			_dataSource.RequestInputFocus();
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to focus native input: " + ex.Message);
		}
	}

	private void HideOverlayRoot()
	{
		try
		{
			_movieIdentifier?.Movie?.RootWidget?.Hide();
		}
		catch
		{
		}
	}

	private void ShowOverlayRoot()
	{
		try
		{
			_movieIdentifier?.Movie?.RootWidget?.Show();
		}
		catch
		{
		}
	}

	private void HandleSubmitRequested(string inputText)
	{
		if (_isClosed || _isSubmitting || !_dataSource.IsCustomAnswerVisible)
		{
			return;
		}
		string text = (inputText ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		_ = SubmitAsync(text);
	}

	private void TryStartPendingNpcOpening()
	{
		if (_isClosed || _isSubmitting || _npcOpeningAutoStarted)
		{
			return;
		}
		if (!ProactiveNpcRequestBehavior.HasPendingNativeOpeningForCurrentConversation())
		{
			return;
		}
		_npcOpeningAutoStarted = true;
		SetInputVisible(true);
		_ = SubmitNpcInitiatedOpeningAsync();
	}

	private async Task SubmitNpcInitiatedOpeningAsync()
	{
		int generation = ++_submitGeneration;
		string originalDialogText = ConversationHelper.GetCurrentDialogText();
		bool receivedVisibleText = false;
		_isSubmitting = true;
		ClearPendingPostprocessNotice();
		_dataSource.SetBusy(true);
		_dataSource.InputText = "";
		SetLayerForButtonsOnly();
		ConversationHelper.BeginStreaming();
		StartWaitingDotsAnimation(generation);
		bool suppressVisibleStreamingForTts = ShoutBehavior.ShouldSuppressNativeConversationVisibleStreamingForTtsExternal();
		try
		{
			string reply = await ShoutBehavior.SubmitNativeConversationNpcInitiatedOpeningForExternalAsync(delegate(string partial)
			{
				if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(partial))
				{
					if (suppressVisibleStreamingForTts)
					{
						return;
					}
					receivedVisibleText = true;
					StopWaitingDotsAnimation(generation);
					ConversationHelper.UpdateDialogText(partial);
				}
			}, originalDialogText, delegate(string npcName)
			{
				QueuePostprocessNotice(generation, npcName);
			});
			if (_isClosed)
			{
				return;
			}
			reply = (reply ?? "").Replace("\r", "").Trim();
			if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(reply))
			{
				receivedVisibleText = true;
				StopWaitingDotsAnimation(generation);
				if (!suppressVisibleStreamingForTts || !ConversationHelper.IsTypewriterActive)
				{
					ConversationHelper.UpdateDialogText(reply);
				}
			}
			else if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
			{
				ConversationHelper.UpdateDialogText(originalDialogText ?? "");
			}
			if (suppressVisibleStreamingForTts && IsSubmitGenerationActive(generation))
			{
				await ShoutBehavior.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();
			}
		}
		catch (Exception ex)
		{
			StopWaitingDotsAnimation(generation);
			if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
			{
				ConversationHelper.UpdateDialogText(originalDialogText ?? "");
			}
			Logger.Log("NativeConversationOverlay", "[ERROR] NPC initiated opening failed: " + ex);
			try
			{
				InformationManager.DisplayMessage(new InformationMessage("AnimusForge NPC主动开口失败：" + ex.Message, new Color(1f, 0.35f, 0.25f)));
			}
			catch
			{
			}
		}
		finally
		{
			StopWaitingDotsAnimation(generation);
			ConversationHelper.EndStreaming();
			_isSubmitting = false;
			if (!_isClosed && generation == _submitGeneration)
			{
				_dataSource.SetBusy(false);
				if (_dataSource.IsCustomAnswerVisible)
				{
					ShowInputReadyMessage();
					PlayInputReadySound();
					FocusInputIfVisible();
				}
				else
				{
					_postRestoreForceRestoreTicks = 8;
					RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
				}
			}
			else if (!_isClosed && !_dataSource.IsCustomAnswerVisible)
			{
				_postRestoreForceRestoreTicks = 8;
				RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
			}
		}
	}

	private async Task SubmitAsync(string text)
	{
		int generation = ++_submitGeneration;
		string originalDialogText = ConversationHelper.GetCurrentDialogText();
		bool receivedVisibleText = false;
		_isSubmitting = true;
		ClearPendingPostprocessNotice();
		_dataSource.SetBusy(true);
		_dataSource.InputText = "";
		SetLayerForButtonsOnly();
		ConversationHelper.BeginStreaming();
		StartWaitingDotsAnimation(generation);
		bool suppressVisibleStreamingForTts = ShoutBehavior.ShouldSuppressNativeConversationVisibleStreamingForTtsExternal();
		try
		{
			string reply = await ShoutBehavior.SubmitNativeConversationTextForExternalAsync(text, delegate(string partial)
			{
				if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(partial))
				{
					if (suppressVisibleStreamingForTts)
					{
						return;
					}
					receivedVisibleText = true;
					StopWaitingDotsAnimation(generation);
					ConversationHelper.UpdateDialogText(partial);
				}
			}, originalDialogText, delegate(string npcName)
			{
				QueuePostprocessNotice(generation, npcName);
			});
			if (_isClosed)
			{
				return;
			}
			reply = (reply ?? "").Replace("\r", "").Trim();
			if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(reply))
			{
				receivedVisibleText = true;
				StopWaitingDotsAnimation(generation);
				if (!suppressVisibleStreamingForTts || !ConversationHelper.IsTypewriterActive)
				{
					ConversationHelper.UpdateDialogText(reply);
				}
			}
			else if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
			{
				ConversationHelper.UpdateDialogText(originalDialogText ?? "");
			}
			if (suppressVisibleStreamingForTts && IsSubmitGenerationActive(generation))
			{
				await ShoutBehavior.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();
			}
		}
		catch (Exception ex)
		{
			StopWaitingDotsAnimation(generation);
			if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
			{
				ConversationHelper.UpdateDialogText(originalDialogText ?? "");
			}
			Logger.Log("NativeConversationOverlay", "[ERROR] Submit failed: " + ex);
			try
			{
				InformationManager.DisplayMessage(new InformationMessage("AnimusForge 自由对话提交失败：" + ex.Message, new Color(1f, 0.35f, 0.25f)));
			}
			catch
			{
			}
		}
		finally
		{
			StopWaitingDotsAnimation(generation);
			ConversationHelper.EndStreaming();
			_isSubmitting = false;
			if (!_isClosed && generation == _submitGeneration)
			{
				_dataSource.SetBusy(false);
				if (_dataSource.IsCustomAnswerVisible)
				{
					ShowInputReadyMessage();
					PlayInputReadySound();
					FocusInputIfVisible();
				}
				else
				{
					_postRestoreForceRestoreTicks = 8;
					RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
				}
			}
			else if (!_isClosed && !_dataSource.IsCustomAnswerVisible)
			{
				_postRestoreForceRestoreTicks = 8;
				RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
			}
		}
	}

	private void StartWaitingDotsAnimation(int generation)
	{
		_waitingDotsGeneration = generation;
		_waitingDotsPhase = 0;
		_nextWaitingDotsUpdateUtcTicks = 0L;
		_waitingDotsActive = true;
		UpdateWaitingDotsAnimation(force: true);
	}

	private void StopWaitingDotsAnimation(int generation)
	{
		if (generation == _waitingDotsGeneration)
		{
			StopWaitingDotsAnimation();
		}
	}

	private void StopWaitingDotsAnimation()
	{
		_waitingDotsActive = false;
		_nextWaitingDotsUpdateUtcTicks = 0L;
	}

	private void UpdateWaitingDotsAnimation(bool force = false)
	{
		if (!_waitingDotsActive || _isClosed || _waitingDotsGeneration != _submitGeneration || !_dataSource.IsCustomAnswerVisible)
		{
			return;
		}
		long ticks = DateTime.UtcNow.Ticks;
		if (!force && _nextWaitingDotsUpdateUtcTicks > 0L && ticks < _nextWaitingDotsUpdateUtcTicks)
		{
			return;
		}
		ConversationHelper.UpdateDialogText(GetWaitingDotsText(_waitingDotsPhase));
		_waitingDotsPhase = (_waitingDotsPhase + 1) % 4;
		_nextWaitingDotsUpdateUtcTicks = ticks + TimeSpan.FromMilliseconds(WaitingDotsIntervalMilliseconds).Ticks;
	}

	private static string GetWaitingDotsText(int phase)
	{
		switch (phase)
		{
		case 0:
			return ".";
		case 1:
			return "..";
		case 2:
			return "...";
		default:
			return "";
		}
	}

	private void QueuePostprocessNotice(int generation, string npcName)
	{
		if (!IsSubmitGenerationActive(generation))
		{
			return;
		}
		lock (_postprocessNoticeLock)
		{
			if (_queuedPostprocessNoticeGeneration == generation)
			{
				return;
			}
			_queuedPostprocessNoticeGeneration = generation;
			_pendingPostprocessNoticeGeneration = generation;
			_pendingPostprocessNoticeNpcName = string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim();
			_hasPendingPostprocessNotice = true;
		}
	}

	private void FlushPendingPostprocessNotice()
	{
		string npcName = null;
		int generation = -1;
		lock (_postprocessNoticeLock)
		{
			if (!_hasPendingPostprocessNotice)
			{
				return;
			}
			npcName = _pendingPostprocessNoticeNpcName;
			generation = _pendingPostprocessNoticeGeneration;
			_pendingPostprocessNoticeNpcName = null;
			_pendingPostprocessNoticeGeneration = -1;
			_hasPendingPostprocessNotice = false;
		}
		if (!IsSubmitGenerationActive(generation))
		{
			return;
		}
		try
		{
			InformationManager.DisplayMessage(new InformationMessage("正在处理NPC（" + (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim()) + "）的行为", new Color(1f, 0.95f, 0.25f)));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to show postprocess notice: " + ex.Message);
		}
	}

	private static void PlayInputReadySound()
	{
		string[] soundEvents =
		{
			"event:/ui/notification/quest_update",
			"event:/ui/notification/quest_start",
			"event:/ui/notification/relation",
			"event:/ui/default"
		};
		string lastError = null;
		foreach (string soundEvent in soundEvents)
		{
			try
			{
				UISoundsHelper.PlayUISound(soundEvent);
				return;
			}
			catch (Exception ex)
			{
				lastError = ex.Message;
			}
		}
		if (!string.IsNullOrWhiteSpace(lastError))
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to play input ready sound: " + lastError);
		}
	}

	private static void ShowInputReadyMessage()
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage("你现在可以回复了！", new Color(0.35f, 1f, 0.35f)));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to show input ready message: " + ex.Message);
		}
	}

	private void ClearPendingPostprocessNotice()
	{
		lock (_postprocessNoticeLock)
		{
			_hasPendingPostprocessNotice = false;
			_pendingPostprocessNoticeNpcName = null;
			_pendingPostprocessNoticeGeneration = -1;
			_queuedPostprocessNoticeGeneration = -1;
		}
	}

	private bool IsSubmitGenerationActive(int generation)
	{
		return !_isClosed && generation == _submitGeneration && _dataSource.IsCustomAnswerVisible;
	}

	private void Close(bool silent)
	{
		if (_isClosed)
		{
			return;
		}
		_isClosed = true;
		StopWaitingDotsAnimation();
		ClearPendingPostprocessNotice();
		_submitGeneration++;
		try
		{
			ShoutBehavior.CloseNativeConversationInputForExternal();
			NativeConversationAnswerAreaController.SetSuppressed(false);
			NativeConversationAnswerAreaController.ForceRestoreAll();
			_layer.InputRestrictions.ResetInputRestrictions();
			_layer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_layer);
		}
		catch
		{
		}
		try
		{
			_screen.RemoveLayer(_layer);
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				Logger.Log("NativeConversationOverlay", "[WARN] Failed to remove overlay layer: " + ex.Message);
			}
		}
		_dataSource?.OnFinalize();
		if (ReferenceEquals(_activeOverlay, this))
		{
			_activeOverlay = null;
		}
	}
}
