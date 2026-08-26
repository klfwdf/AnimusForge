using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
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

	private static readonly TimeSpan LongNpcReplyUnlockDelay = TimeSpan.FromMinutes(1.0);

	private const string EncyclopediaLayerName = "EncyclopediaBar";

	private const string MissionEscapeMenuLayerName = "MissionEscapeMenu";

	private const string MissionOptionsLayerName = "MissionOptions";

	private const string MapEscapeMenuLayerName = "MapEscapeMenu";

	private const string MapCampaignOptionsLayerName = "MapCampaignOptions";

	private const string MapConversationLayerName = "MapConversation";

	private const string MissionConversationLayerName = "MissionConversation";

	private static readonly FieldInfo _screenLayersField = typeof(ScreenBase).GetField("_layers", BindingFlags.Instance | BindingFlags.NonPublic);

	private static AnimusForgeNativeConversationOverlay _activeOverlay;

	private static int _mainThreadId;

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

	private long _waitingForReplyStartedUtcTicks;

	private bool _longWaitEscapeUnlockAvailable;

	private bool _longWaitEscapeNoticeShown;

	private readonly object _postprocessNoticeLock = new object();

	private bool _hasPendingPostprocessNotice;

	private string _pendingPostprocessNoticeNpcName;

	private int _pendingPostprocessNoticeGeneration = -1;

	private int _queuedPostprocessNoticeGeneration = -1;

	private bool _temporarySystemUiActive;

	private bool _isHiddenForTemporarySystemUi;

	private int _postRestoreForceRestoreTicks;

	private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

	public static bool IsOpen => _activeOverlay != null && !_activeOverlay._isClosed;

	private AnimusForgeNativeConversationOverlay(ScreenBase screen)
	{
		_screen = screen;
		_dataSource = new AnimusForgeNativeConversationOverlayVM(HandleSubmitRequested, HandleSwitchTalkRequested, HandleShowHistoryRequested, HandleGiveShowRequested, HandleEditPersonaRequested, HandleTagTestRequested);
		_layer = new GauntletLayer("AnimusForgeNativeConversationOverlay", 350, false);
	}

	public static void OnApplicationTick()
	{
		if (_mainThreadId == 0)
		{
			_mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}
		try
		{
			bool canSubmit;
			using (FreezeWatchdog.Scope("NativeConversationOverlay.CanSubmit"))
			{
				canSubmit = ShoutBehavior.CanSubmitNativeConversationForExternal();
			}
			bool shoutPopupOpen;
			using (FreezeWatchdog.Scope("NativeConversationOverlay.ShoutPopupState"))
			{
				shoutPopupOpen = ShoutTextInputPopup.IsOpen;
			}
			if (!canSubmit || shoutPopupOpen)
			{
				using (FreezeWatchdog.Scope("NativeConversationOverlay.CloseUnavailable"))
				{
					CloseActive();
				}
				return;
			}
			ScreenBase topScreen;
			using (FreezeWatchdog.Scope("NativeConversationOverlay.GetTopScreen"))
			{
				topScreen = ScreenManager.TopScreen;
			}
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
			bool temporaryUiHandled;
			using (FreezeWatchdog.Scope("NativeConversationOverlay.TemporarySystemUi"))
			{
				temporaryUiHandled = _activeOverlay.TickTemporarySystemUiIfNeeded(topScreen);
			}
			if (temporaryUiHandled)
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
			using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick"))
			{
				_activeOverlay.Tick();
			}
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
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.MainThreadActions"))
		{
			ProcessMainThreadActions();
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.PostRestore"))
		{
			ProcessPostRestoreNativeAnswerRestore();
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.PostprocessNotice"))
		{
			FlushPendingPostprocessNotice();
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.WaitingAnimation"))
		{
			UpdateWaitingDotsAnimation();
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.LongWaitUnlock"))
		{
			TickLongWaitEscapeUnlock();
		}
		bool personaEditVisible;
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.ResolvePersonaVisibility"))
		{
			personaEditVisible = ShoutBehavior.CanEditNativeConversationNpcForExternal();
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.ApplyPersonaVisibility"))
		{
			_dataSource.SetPersonaEditVisible(personaEditVisible);
		}
		bool tagTestVisible;
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.ResolveTagVisibility"))
		{
			tagTestVisible = ShoutBehavior.CanOpenNativeConversationTagTestForExternal();
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.ApplyTagVisibility"))
		{
			_dataSource.SetTagTestVisible(tagTestVisible);
		}
		using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.PendingNpcOpening"))
		{
			TryStartPendingNpcOpening();
		}
		if (!_dataSource.IsCustomAnswerVisible)
		{
			using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.RestoreOrdinaryInput"))
			{
				RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: false);
			}
		}
		else
		{
			using (FreezeWatchdog.Scope("NativeConversationOverlay.Tick.SuppressNativeAnswer"))
			{
				NativeConversationAnswerAreaController.SetSuppressed(true);
			}
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
			if (AnimusForgeConversationHistoryLogPopup.IsOpen)
			{
				// The encyclopedia already closed, but its history child still owns this conversation; do not restore/focus the parent underneath it.
				return true;
			}
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
		string text = AnimusForgeTextInputSanitizer.SanitizeSingleLine(inputText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		_ = SubmitAsync(text);
	}

	private void ShowNativeConversationPreprocessRetryInquiry(string playerText, int generation)
	{
		string retryText = AnimusForgeTextInputSanitizer.SanitizeSingleLine(playerText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars).Trim();
		if (_isClosed || generation != _submitGeneration || string.IsNullOrWhiteSpace(retryText))
		{
			return;
		}
		try
		{
			Logger.Log("NativeConversationOverlay", "Native preprocess timeout retry prompt shown. Generation=" + generation + " inputLen=" + retryText.Length);
			InformationManager.ShowInquiry(new InquiryData(
				"原生对话前处理超时",
				"ONNX/知识检索前处理超过等待上限，或上一轮前处理仍在后台运行。\n\n是否重试刚才这句话？",
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				"重试",
				"取消",
				delegate
				{
					RetryNativeConversationTextFromPopup(retryText);
				},
				delegate
				{
					RestoreNativeConversationRetryText(retryText);
				}),
				pauseGameActiveState: true,
				prioritize: true);
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to show preprocess retry prompt: " + ex.Message);
			RestoreNativeConversationRetryText(retryText);
		}
	}

	private void ShowNativeConversationNpcOpeningPreprocessRetryInquiry(int generation)
	{
		if (_isClosed || generation != _submitGeneration)
		{
			return;
		}
		try
		{
			Logger.Log("NativeConversationOverlay", "Native NPC opening preprocess timeout retry prompt shown. Generation=" + generation);
			InformationManager.ShowInquiry(new InquiryData(
				"NPC开口前处理超时",
				"ONNX/知识检索前处理超过等待上限，或上一轮前处理仍在后台运行。\n\n是否让NPC重新尝试开口？",
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: true,
				"重试",
				"取消",
				delegate
				{
					RetryNativeConversationNpcOpeningFromPopup();
				},
				delegate
				{
					FocusInputIfVisible();
				}),
				pauseGameActiveState: true,
				prioritize: true);
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to show NPC opening preprocess retry prompt: " + ex.Message);
			FocusInputIfVisible();
		}
	}

	private void RetryNativeConversationNpcOpeningFromPopup()
	{
		RunOnMainThread(delegate
		{
			if (_isClosed)
			{
				return;
			}
			if (!_dataSource.IsCustomAnswerVisible)
			{
				SetInputVisible(true);
			}
			if (_isSubmitting)
			{
				return;
			}
			_ = SubmitNpcInitiatedOpeningAsync();
		});
	}

	private void RetryNativeConversationTextFromPopup(string playerText)
	{
		string retryText = AnimusForgeTextInputSanitizer.SanitizeSingleLine(playerText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars).Trim();
		if (string.IsNullOrWhiteSpace(retryText))
		{
			return;
		}
		RunOnMainThread(delegate
		{
			if (_isClosed)
			{
				return;
			}
			if (!_dataSource.IsCustomAnswerVisible)
			{
				SetInputVisible(true);
			}
			_dataSource.InputText = retryText;
			if (_isSubmitting)
			{
				return;
			}
			_ = SubmitAsync(retryText);
		});
	}

	private void RestoreNativeConversationRetryText(string playerText)
	{
		string retryText = AnimusForgeTextInputSanitizer.SanitizeSingleLine(playerText, AnimusForgeTextInputSanitizer.MaxNativeConversationChars).Trim();
		RunOnMainThread(delegate
		{
			if (_isClosed)
			{
				return;
			}
			if (!_dataSource.IsCustomAnswerVisible)
			{
				SetInputVisible(true);
			}
			_dataSource.InputText = retryText;
			FocusInputIfVisible();
		});
	}

	private void TryStartPendingNpcOpening()
	{
		if (_isClosed || _isSubmitting || _npcOpeningAutoStarted)
		{
			return;
		}
		if (!NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation())
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
		bool offerPreprocessRetry = false;
		bool suppressReadyNotice = false;
		string completedDisplayReply = "";
		bool needsFinalDisplayAfterTts = false;
		string mainReplyBeforePostprocess = "";
		bool hasMainReplyBeforePostprocess = false;
		EncyclopediaEntityLinkFormatter.StreamingDisplaySession streamingLinkDisplaySession = null;
		Hero streamLinkTargetHero = null;
		CharacterObject streamLinkTargetCharacter = null;
		// Capture before the background stream begins, so an action that later changes target cannot redirect NPC links mid-reply.
		ShoutBehavior.TryGetNativeConversationLinkTargetForExternal(out streamLinkTargetHero, out streamLinkTargetCharacter);
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
				RunOnMainThread(delegate
				{
					if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(partial))
					{
						if (!ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
						{
							receivedVisibleText = false;
							suppressReadyNotice = true;
							StopWaitingDotsAnimation(generation);
							ClearPendingPostprocessNotice();
							ConversationHelper.UpdateDialogText(originalDialogText ?? "");
							return;
						}
						if (suppressVisibleStreamingForTts)
						{
							return;
						}
						receivedVisibleText = true;
						StopWaitingDotsAnimation(generation);
						// The snapshot is built once on the UI thread; later fragments probe just their appended tail.
						if (streamingLinkDisplaySession == null)
						{
							streamingLinkDisplaySession = EncyclopediaEntityLinkFormatter.CreateStreamingDisplaySession();
						}
						ConversationHelper.UpdateDialogText(streamingLinkDisplaySession.FormatStreamingText(partial, streamLinkTargetHero, streamLinkTargetCharacter));
					}
				});
			}, originalDialogText, delegate(string npcName)
			{
				RunOnMainThread(delegate
				{
					if (!IsSubmitGenerationActive(generation))
					{
						return;
					}
					if (!ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
					{
						receivedVisibleText = false;
						suppressReadyNotice = true;
						StopWaitingDotsAnimation(generation);
						ClearPendingPostprocessNotice();
						ConversationHelper.UpdateDialogText(originalDialogText ?? "");
						return;
					}
					QueuePostprocessNotice(generation, npcName);
				});
			}, delegate(string mainReply, Hero mainReplyTargetHero, CharacterObject mainReplyTargetCharacter)
			{
				mainReplyBeforePostprocess = (mainReply ?? "").Replace("\r", "").Trim();
				hasMainReplyBeforePostprocess = !string.IsNullOrWhiteSpace(mainReplyBeforePostprocess);
				if (!hasMainReplyBeforePostprocess || suppressVisibleStreamingForTts)
				{
					return;
				}
				RunOnMainThread(delegate
				{
					if (_isClosed || !IsSubmitGenerationActive(generation) || !ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
					{
						return;
					}
					receivedVisibleText = true;
					StopWaitingDotsAnimation(generation);
					// Reuse the streaming snapshot when possible, so the completed main reply does not rescan all campaign entities.
					completedDisplayReply = streamingLinkDisplaySession != null
						? streamingLinkDisplaySession.FormatStreamingText(mainReplyBeforePostprocess, mainReplyTargetHero, mainReplyTargetCharacter)
						: ShoutBehavior.FormatNativeConversationDisplayTextForExternal(mainReplyBeforePostprocess, mainReplyTargetHero, mainReplyTargetCharacter);
					ConversationHelper.UpdateDialogText(completedDisplayReply);
				});
			});
			string completedReply = reply;
			RunOnMainThread(delegate
			{
				if (_isClosed)
				{
					return;
				}
				if (IsSubmitGenerationActive(generation) && !ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
				{
					receivedVisibleText = false;
					suppressReadyNotice = true;
					StopWaitingDotsAnimation(generation);
					ClearPendingPostprocessNotice();
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
					return;
				}
				completedReply = (completedReply ?? "").Replace("\r", "").Trim();
				if (IsSubmitGenerationActive(generation) && ShoutBehavior.IsNativeConversationPreprocessUnavailableTextForExternal(completedReply))
				{
					offerPreprocessRetry = true;
					suppressReadyNotice = true;
					receivedVisibleText = false;
					StopWaitingDotsAnimation(generation);
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
					return;
				}
				bool finalReplyMatchesMainReply = !suppressVisibleStreamingForTts
					&& hasMainReplyBeforePostprocess
					&& string.Equals(completedReply, mainReplyBeforePostprocess, StringComparison.Ordinal);
				if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(completedReply) && !finalReplyMatchesMainReply)
				{
					receivedVisibleText = true;
					StopWaitingDotsAnimation(generation);
					// Only an action/postprocess-modified final reply needs a second display scan.
					completedDisplayReply = ShoutBehavior.FormatNativeConversationDisplayTextForExternal(completedReply);
					if (!suppressVisibleStreamingForTts || !ConversationHelper.IsTypewriterActive)
					{
						ConversationHelper.UpdateDialogText(completedDisplayReply);
					}
					else
					{
						// The raw TTS typewriter must finish before its final text can be replaced by trusted RichText markup.
						needsFinalDisplayAfterTts = true;
					}
				}
				else if (IsSubmitGenerationActive(generation) && !receivedVisibleText && !finalReplyMatchesMainReply)
				{
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
				}
			});
			if (suppressVisibleStreamingForTts && IsSubmitGenerationCurrent(generation))
			{
				await ShoutBehavior.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();
				RunOnMainThread(delegate
				{
					if (_isClosed || !needsFinalDisplayAfterTts || !IsSubmitGenerationActive(generation) || !ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
					{
						return;
					}
					// This one post-TTS update is intentionally not a rescan; it uses the completed UI-only RichText copy.
					ConversationHelper.UpdateDialogText(completedDisplayReply);
				});
			}
		}
		catch (Exception ex)
		{
			RunOnMainThread(delegate
			{
				StopWaitingDotsAnimation(generation);
				if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
				{
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
				}
				Logger.Log("NativeConversationOverlay", "[ERROR] NPC initiated opening failed: " + ex);
				try
				{
					LlmRetryPrompt.ShowFailurePopup("AnimusForge NPC主动开口失败", ex.Message);
				}
				catch
				{
				}
			});
		}
		finally
		{
			RunOnMainThread(delegate
			{
				StopWaitingDotsAnimation(generation);
				ConversationHelper.EndStreaming();
				_isSubmitting = false;
				if (!_isClosed && generation == _submitGeneration)
				{
					_dataSource.SetBusy(false);
					if (_dataSource.IsCustomAnswerVisible)
					{
						if (!suppressReadyNotice)
						{
							ShowInputReadyMessage();
							PlayInputReadySound();
						}
						FocusInputIfVisible();
					}
					else
					{
						_postRestoreForceRestoreTicks = 8;
						RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
					}
					if (offerPreprocessRetry)
					{
						ShowNativeConversationNpcOpeningPreprocessRetryInquiry(generation);
					}
				}
				else if (!_isClosed && !_dataSource.IsCustomAnswerVisible)
				{
					_postRestoreForceRestoreTicks = 8;
					RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
				}
			});
		}
	}

	private async Task SubmitAsync(string text)
	{
		int generation = ++_submitGeneration;
		string originalDialogText = ConversationHelper.GetCurrentDialogText();
		bool receivedVisibleText = false;
		bool offerPreprocessRetry = false;
		bool suppressReadyNotice = false;
		string completedDisplayReply = "";
		bool needsFinalDisplayAfterTts = false;
		string mainReplyBeforePostprocess = "";
		bool hasMainReplyBeforePostprocess = false;
		EncyclopediaEntityLinkFormatter.StreamingDisplaySession streamingLinkDisplaySession = null;
		Hero streamLinkTargetHero = null;
		CharacterObject streamLinkTargetCharacter = null;
		// Capture before the background stream begins, so an action that later changes target cannot redirect NPC links mid-reply.
		ShoutBehavior.TryGetNativeConversationLinkTargetForExternal(out streamLinkTargetHero, out streamLinkTargetCharacter);
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
				RunOnMainThread(delegate
				{
					if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(partial))
					{
						if (!ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
						{
							receivedVisibleText = false;
							suppressReadyNotice = true;
							StopWaitingDotsAnimation(generation);
							ClearPendingPostprocessNotice();
							ConversationHelper.UpdateDialogText(originalDialogText ?? "");
							return;
						}
						if (suppressVisibleStreamingForTts)
						{
							return;
						}
						receivedVisibleText = true;
						StopWaitingDotsAnimation(generation);
						// The snapshot is built once on the UI thread; later fragments probe just their appended tail.
						if (streamingLinkDisplaySession == null)
						{
							streamingLinkDisplaySession = EncyclopediaEntityLinkFormatter.CreateStreamingDisplaySession();
						}
						ConversationHelper.UpdateDialogText(streamingLinkDisplaySession.FormatStreamingText(partial, streamLinkTargetHero, streamLinkTargetCharacter));
					}
				});
			}, originalDialogText, delegate(string npcName)
			{
				RunOnMainThread(delegate
				{
					if (!IsSubmitGenerationActive(generation))
					{
						return;
					}
					if (!ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
					{
						receivedVisibleText = false;
						suppressReadyNotice = true;
						StopWaitingDotsAnimation(generation);
						ClearPendingPostprocessNotice();
						ConversationHelper.UpdateDialogText(originalDialogText ?? "");
						return;
					}
					QueuePostprocessNotice(generation, npcName);
				});
			}, delegate(string mainReply, Hero mainReplyTargetHero, CharacterObject mainReplyTargetCharacter)
			{
				mainReplyBeforePostprocess = (mainReply ?? "").Replace("\r", "").Trim();
				hasMainReplyBeforePostprocess = !string.IsNullOrWhiteSpace(mainReplyBeforePostprocess);
				if (!hasMainReplyBeforePostprocess || suppressVisibleStreamingForTts)
				{
					return;
				}
				RunOnMainThread(delegate
				{
					if (_isClosed || !IsSubmitGenerationActive(generation) || !ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
					{
						return;
					}
					receivedVisibleText = true;
					StopWaitingDotsAnimation(generation);
					// Reuse the streaming snapshot when possible, so the completed main reply does not rescan all campaign entities.
					completedDisplayReply = streamingLinkDisplaySession != null
						? streamingLinkDisplaySession.FormatStreamingText(mainReplyBeforePostprocess, mainReplyTargetHero, mainReplyTargetCharacter)
						: ShoutBehavior.FormatNativeConversationDisplayTextForExternal(mainReplyBeforePostprocess, mainReplyTargetHero, mainReplyTargetCharacter);
					ConversationHelper.UpdateDialogText(completedDisplayReply);
				});
			});
			string completedReply = reply;
			RunOnMainThread(delegate
			{
				if (_isClosed)
				{
					return;
				}
				if (IsSubmitGenerationActive(generation) && !ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
				{
					receivedVisibleText = false;
					suppressReadyNotice = true;
					StopWaitingDotsAnimation(generation);
					ClearPendingPostprocessNotice();
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
					return;
				}
				completedReply = (completedReply ?? "").Replace("\r", "").Trim();
				if (IsSubmitGenerationActive(generation) && ShoutBehavior.IsNativeConversationPreprocessUnavailableTextForExternal(completedReply))
				{
					offerPreprocessRetry = true;
					suppressReadyNotice = true;
					receivedVisibleText = false;
					StopWaitingDotsAnimation(generation);
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
					_dataSource.InputText = text;
					return;
				}
				bool finalReplyMatchesMainReply = !suppressVisibleStreamingForTts
					&& hasMainReplyBeforePostprocess
					&& string.Equals(completedReply, mainReplyBeforePostprocess, StringComparison.Ordinal);
				if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(completedReply) && !finalReplyMatchesMainReply)
				{
					receivedVisibleText = true;
					StopWaitingDotsAnimation(generation);
					// Only an action/postprocess-modified final reply needs a second display scan.
					completedDisplayReply = ShoutBehavior.FormatNativeConversationDisplayTextForExternal(completedReply);
					if (!suppressVisibleStreamingForTts || !ConversationHelper.IsTypewriterActive)
					{
						ConversationHelper.UpdateDialogText(completedDisplayReply);
					}
					else
					{
						// The raw TTS typewriter must finish before its final text can be replaced by trusted RichText markup.
						needsFinalDisplayAfterTts = true;
					}
				}
				else if (IsSubmitGenerationActive(generation) && !receivedVisibleText && !finalReplyMatchesMainReply)
				{
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
				}
			});
			if (suppressVisibleStreamingForTts && IsSubmitGenerationCurrent(generation))
			{
				await ShoutBehavior.WaitForNativeConversationTtsPlaybackFinishedForExternalAsync();
				RunOnMainThread(delegate
				{
					if (_isClosed || !needsFinalDisplayAfterTts || !IsSubmitGenerationActive(generation) || !ShoutBehavior.IsNativeConversationResponseTargetAvailableForExternal())
					{
						return;
					}
					// This one post-TTS update is intentionally not a rescan; it uses the completed UI-only RichText copy.
					ConversationHelper.UpdateDialogText(completedDisplayReply);
				});
			}
		}
		catch (Exception ex)
		{
			RunOnMainThread(delegate
			{
				StopWaitingDotsAnimation(generation);
				if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
				{
					ConversationHelper.UpdateDialogText(originalDialogText ?? "");
				}
				Logger.Log("NativeConversationOverlay", "[ERROR] Submit failed: " + ex);
				try
				{
					LlmRetryPrompt.ShowFailurePopup("AnimusForge 自由对话提交失败", ex.Message);
				}
				catch
				{
				}
			});
		}
		finally
		{
			RunOnMainThread(delegate
			{
				StopWaitingDotsAnimation(generation);
				ConversationHelper.EndStreaming();
				_isSubmitting = false;
				if (!_isClosed && generation == _submitGeneration)
				{
					_dataSource.SetBusy(false);
					if (_dataSource.IsCustomAnswerVisible)
					{
						if (!suppressReadyNotice)
						{
							ShowInputReadyMessage();
							PlayInputReadySound();
						}
						FocusInputIfVisible();
					}
					else
					{
						_postRestoreForceRestoreTicks = 8;
						RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
					}
					if (offerPreprocessRetry)
					{
						ShowNativeConversationPreprocessRetryInquiry(text, generation);
					}
				}
				else if (!_isClosed && !_dataSource.IsCustomAnswerVisible)
				{
					_postRestoreForceRestoreTicks = 8;
					RestoreNativeConversationInputAfterOrdinaryMode(forceAnswerRestore: true);
				}
			});
		}
	}

	private void RunOnMainThread(Action action)
	{
		if (action == null)
		{
			return;
		}
		if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId != _mainThreadId)
		{
			_mainThreadActions.Enqueue(action);
			return;
		}
		action();
	}

	private void ProcessMainThreadActions()
	{
		int processed = 0;
		while (processed < 128 && _mainThreadActions.TryDequeue(out var action))
		{
			processed++;
			try
			{
				string actionName = action?.Method == null
					? "unknown"
					: ((action.Method.DeclaringType?.Name ?? "unknown") + "." + action.Method.Name);
				using (FreezeWatchdog.Scope("NativeConversationOverlay.MainThreadAction." + actionName))
				{
					action?.Invoke();
				}
			}
			catch (Exception ex)
			{
				Logger.Log("NativeConversationOverlay", "[WARN] queued main-thread action failed: " + ex.Message);
			}
		}
	}

	private void StartWaitingDotsAnimation(int generation)
	{
		_waitingDotsGeneration = generation;
		_waitingDotsPhase = 0;
		_nextWaitingDotsUpdateUtcTicks = 0L;
		_waitingForReplyStartedUtcTicks = DateTime.UtcNow.Ticks;
		_longWaitEscapeUnlockAvailable = false;
		_longWaitEscapeNoticeShown = false;
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
		_waitingForReplyStartedUtcTicks = 0L;
		_longWaitEscapeUnlockAvailable = false;
		_longWaitEscapeNoticeShown = false;
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
		using (FreezeWatchdog.Scope("NativeConversationOverlay.WaitingAnimation.UpdateDialogText"))
		{
			ConversationHelper.UpdateDialogText(GetWaitingDotsText(_waitingDotsPhase));
		}
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

	private void TickLongWaitEscapeUnlock()
	{
		if (!_isSubmitting || !_waitingDotsActive || _waitingDotsGeneration != _submitGeneration || !_dataSource.IsCustomAnswerVisible)
		{
			_longWaitEscapeUnlockAvailable = false;
			return;
		}
		long startedTicks = _waitingForReplyStartedUtcTicks;
		if (startedTicks <= 0L)
		{
			return;
		}
		if (!_longWaitEscapeNoticeShown && DateTime.UtcNow.Ticks - startedTicks >= LongNpcReplyUnlockDelay.Ticks)
		{
			_longWaitEscapeNoticeShown = true;
			_longWaitEscapeUnlockAvailable = true;
			ShowLongWaitEscapeNotice();
		}
		if (_longWaitEscapeUnlockAvailable && ShouldUnlockLongWaitForEscapeKey())
		{
			ReleaseLongWaitUiLock();
		}
	}

	private bool ShouldUnlockLongWaitForEscapeKey()
	{
		try
		{
			if (_layer?.Input != null && (_layer.Input.IsHotKeyReleased("Exit") || _layer.Input.IsKeyReleased(InputKey.Escape)))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return Input.IsKeyReleased(InputKey.Escape);
		}
		catch
		{
			return false;
		}
	}

	private void ReleaseLongWaitUiLock()
	{
		if (_isClosed || !_longWaitEscapeUnlockAvailable)
		{
			return;
		}
		_longWaitEscapeUnlockAvailable = false;
		_longWaitEscapeNoticeShown = false;
		Logger.Log("NativeConversationOverlay", "Long NPC reply wait unlocked by ESC. Generation=" + _submitGeneration);
		SetInputVisible(false);
		try
		{
			InformationManager.DisplayMessage(new InformationMessage("已解除自由对话 UI 锁定。", new Color(0.35f, 1f, 0.35f)));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to show long-wait unlock message: " + ex.Message);
		}
	}

	private static void ShowLongWaitEscapeNotice()
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage("NPC长时间未回复，现在可以按ESC解除UI锁定限制。", new Color(1f, 0.95f, 0.25f)));
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to show long-wait escape notice: " + ex.Message);
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

	private bool IsSubmitGenerationCurrent(int generation)
	{
		return !_isClosed && generation == _submitGeneration;
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
