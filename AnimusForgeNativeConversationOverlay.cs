using System;
using System.Threading.Tasks;
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

	private static AnimusForgeNativeConversationOverlay _activeOverlay;

	private readonly ScreenBase _screen;

	private readonly GauntletLayer _layer;

	private GauntletMovieIdentifier _movieIdentifier;

	private readonly AnimusForgeNativeConversationOverlayVM _dataSource;

	private bool _isClosed;

	private bool _isSubmitting;

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

	public static bool IsOpen => _activeOverlay != null && !_activeOverlay._isClosed;

	private AnimusForgeNativeConversationOverlay(ScreenBase screen)
	{
		_screen = screen;
		_dataSource = new AnimusForgeNativeConversationOverlayVM(HandleSubmitRequested, HandleSwitchTalkRequested, HandleShowHistoryRequested, HandleGiveShowRequested, HandleEditPersonaRequested);
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
				Show(topScreen);
				return;
			}
			if (!ReferenceEquals(_activeOverlay._screen, topScreen))
			{
				CloseActive();
				Show(topScreen);
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
		FlushPendingPostprocessNotice();
		UpdateWaitingDotsAnimation();
		_dataSource.SetPersonaEditVisible(ShoutBehavior.CanEditNativeConversationPersonaForExternal());
		if (!_dataSource.IsCustomAnswerVisible)
		{
			NativeConversationAnswerAreaController.SetSuppressed(false);
			UpdateButtonsOnlyInputRestrictions();
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
				NativeConversationAnswerAreaController.SetSuppressed(false);
				ShoutBehavior.CloseNativeConversationInputForExternal();
				SetLayerForButtonsOnly();
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
			float bottom = _dataSource?.IsPersonaEditVisible == true ? 285f : 235f;
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
			if (!ShoutBehavior.OpenNativeConversationPersonaEditorForExternal(RestoreFocusAfterHistory))
			{
				ShowOverlayRoot();
				RestoreFocusAfterHistory();
			}
		}
		catch (Exception ex)
		{
			ShowOverlayRoot();
			RestoreFocusAfterHistory();
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to open persona editor: " + ex.Message);
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
			SetLayerForButtonsOnly();
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
		try
		{
			string reply = await ShoutBehavior.SubmitNativeConversationTextForExternalAsync(text, delegate(string partial)
			{
				if (IsSubmitGenerationActive(generation) && !string.IsNullOrWhiteSpace(partial))
				{
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
				ConversationHelper.UpdateDialogText(reply);
			}
			else if (IsSubmitGenerationActive(generation) && !receivedVisibleText)
			{
				ConversationHelper.UpdateDialogText(originalDialogText ?? "");
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
			if (generation == _submitGeneration)
			{
				ConversationHelper.EndStreaming();
			}
			_isSubmitting = false;
			if (!_isClosed && generation == _submitGeneration)
			{
				_dataSource.SetBusy(false);
				if (_dataSource.IsCustomAnswerVisible)
				{
					PlayInputReadySound();
					FocusInputIfVisible();
				}
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
		try
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationOverlay", "[WARN] Failed to play input ready sound: " + ex.Message);
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
