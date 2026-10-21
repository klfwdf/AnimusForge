using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public class FloatingTextMissionView : MissionView
{
	private class FloatingTextItemState
	{
		public Agent Agent;

		public FloatingTextItemVM VM;

		public float Lifetime;

		public float MaxLifetime;

		public bool IsFading;

		public string FullTargetText;

		public string CurrentDisplayedText;

		public float TypingTimer;

		public float TypingInterval = 0.05f;

		public const float DEFAULT_TYPING_INTERVAL = 0.05f;
	}

	private GauntletLayer _layer;

	private FloatingTextVM _dataSource;

	private Dictionary<Agent, FloatingTextItemState> _agentStates = new Dictionary<Agent, FloatingTextItemState>();

	private const float FADE_OUT_TIME = 1.5f;

	private const float FADE_SPEED = 2f;

	private const float BUBBLE_MIN_WIDTH = 140f;

	private const float BUBBLE_MAX_WIDTH = 300f;

	private const float BUBBLE_PADDING_WIDTH = 24f;

	private const float AVG_CHAR_WIDTH = 12.5f;

	private bool _isInitialized = false;

	private int _initRetryCount = 0;

	private const int MAX_INIT_RETRIES = 300;

	private bool _debugLoggedOnce = false;

	private bool _positionErrorLogged = false;

	private const float WORLD_ANCHOR_VERTICAL_OFFSET = 0.15f;

	private const float SCREEN_ANCHOR_VERTICAL_PADDING = 18f;

	private bool _typingPaused = false;

	public bool IsBubbleReady()
	{
		return _isInitialized && _layer != null && _dataSource != null;
	}

	public void SetTypingPaused(bool paused)
	{
		_typingPaused = paused;
	}

	public void StopTypingForAll(bool fadeSoon = false)
	{
		List<Agent> list = new List<Agent>();
		foreach (KeyValuePair<Agent, FloatingTextItemState> agentState in _agentStates)
		{
			Agent key = agentState.Key;
			FloatingTextItemState value = agentState.Value;
			if (value == null)
			{
				if (key != null)
				{
					list.Add(key);
				}
				continue;
			}
			StopTypingForState(value, fadeSoon);
			if (string.IsNullOrWhiteSpace(value.CurrentDisplayedText) && string.IsNullOrWhiteSpace(value.FullTargetText) && key != null)
			{
				list.Add(key);
			}
		}
		foreach (Agent item in list)
		{
			RemoveItem(item);
		}
	}

	private void StopTypingForState(FloatingTextItemState state, bool fadeSoon)
	{
		if (state == null)
		{
			return;
		}
		string text = state.CurrentDisplayedText ?? "";
		state.FullTargetText = text;
		state.CurrentDisplayedText = text;
		state.TypingTimer = 0f;
		if (state.VM != null)
		{
			state.VM.Text = text;
		}
		UpdateBubbleWidth(state);
		if (fadeSoon)
		{
			state.IsFading = true;
			state.Lifetime = Math.Max(state.Lifetime, Math.Max(0f, state.MaxLifetime - 0.35f));
		}
	}

	private MissionScreen GetMissionScreen()
	{
		if (base.MissionScreen != null)
		{
			return base.MissionScreen;
		}
		return ScreenManager.TopScreen as MissionScreen;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		try
		{
			if (FloatingTextManager.Instance.MissionView != this)
			{
				FloatingTextManager.Instance.MissionView = this;
			}
		}
		catch
		{
		}
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		Logger.Log("FloatingText", "[Init] OnMissionScreenInitialize called");
		TryInitializeLayer();
		FloatingTextManager.Instance.MissionView = this;
		Logger.Log("FloatingText", "[Init] MissionView registered to FloatingTextManager");
	}

	private void TryInitializeLayer()
	{
		if (_isInitialized)
		{
			return;
		}
		try
		{
			MissionScreen missionScreen = GetMissionScreen();
			if (missionScreen == null)
			{
				if (_initRetryCount == 0)
				{
					Logger.Log("FloatingText", "[Init] MissionScreen is null, will retry in Tick...");
				}
				_initRetryCount++;
				return;
			}
			Logger.Log("FloatingText", "[Init] MissionScreen found: " + missionScreen.GetType().Name + ", attempting layer creation...");
			_dataSource = new FloatingTextVM();
			_layer = new GauntletLayer("FloatingTextLayer", 1000);
			Logger.Log("FloatingText", "[Init] GauntletLayer created, loading movie...");
			_layer.LoadMovie("FloatingTextLayer", _dataSource);
			Logger.Log("FloatingText", "[Init] Movie loaded, adding layer to MissionScreen...");
			missionScreen.AddLayer(_layer);
			_isInitialized = true;
			Logger.Log("FloatingText", $"[Init] ✅ Layer initialized successfully! Retries needed: {_initRetryCount}");
		}
		catch (Exception ex)
		{
			Logger.Log("FloatingText", "[ERROR] Failed to initialize layer: " + ex.ToString());
			_layer = null;
			_dataSource = null;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		Logger.Log("FloatingText", "[Finalize] OnMissionScreenFinalize called");
		if (FloatingTextManager.Instance.MissionView == this)
		{
			FloatingTextManager.Instance.MissionView = null;
		}
		if (_layer != null)
		{
			GetMissionScreen()?.RemoveLayer(_layer);
			_layer = null;
			_dataSource = null;
		}
		_agentStates.Clear();
		_isInitialized = false;
		_initRetryCount = 0;
		_debugLoggedOnce = false;
		base.OnMissionScreenFinalize();
	}

	public override void OnMissionTick(float dt)
	{
		using PerfProbe.ScopeToken perfScope = PerfProbe.Scope("Mission.FloatingTextMissionView.OnMissionTick.total");
		base.OnMissionTick(dt);
		if (!_isInitialized)
		{
			if (_initRetryCount < 300)
			{
				TryInitializeLayer();
			}
			if (!_isInitialized)
			{
				return;
			}
		}
		if (_layer == null)
		{
			return;
		}
		if (_agentStates.Count == 0)
		{
			return;
		}
		List<Agent> list = new List<Agent>();
		foreach (KeyValuePair<Agent, FloatingTextItemState> agentState in _agentStates)
		{
			FloatingTextItemState value = agentState.Value;
			Agent agent = value.Agent;
			if (agent == null || !agent.IsActive())
			{
				list.Add(agent);
				continue;
			}
			if (!_typingPaused && value.CurrentDisplayedText.Length < value.FullTargetText.Length)
			{
				value.TypingTimer += dt;
				while (value.TypingTimer >= value.TypingInterval)
				{
					value.TypingTimer -= value.TypingInterval;
					if (value.CurrentDisplayedText.Length < value.FullTargetText.Length)
					{
						value.CurrentDisplayedText += value.FullTargetText[value.CurrentDisplayedText.Length];
					}
				}
				value.VM.Text = value.CurrentDisplayedText;
				UpdateBubbleWidth(value);
				value.Lifetime = 0f;
			}
			else if (!_typingPaused)
			{
				value.Lifetime += dt;
			}
			if (value.Lifetime > value.MaxLifetime)
			{
				value.IsFading = true;
			}
			if (value.IsFading)
			{
				float num = value.VM.Alpha - dt * 2f;
				if (num <= 0f)
				{
					list.Add(agent);
					continue;
				}
				value.VM.Alpha = num;
			}
			else
			{
				value.VM.Alpha = 1f;
			}
			try
			{
				UpdatePosition(value);
			}
			catch (Exception ex)
			{
				if (!_positionErrorLogged)
				{
					Logger.Log("FloatingText", "[ERROR] UpdatePosition failed: " + ex.Message);
					_positionErrorLogged = true;
				}
				list.Add(agent);
			}
		}
		foreach (Agent item in list)
		{
			RemoveItem(item);
		}
	}

	private void UpdatePosition(FloatingTextItemState state)
	{
		MissionScreen missionScreen = GetMissionScreen();
		Camera camera = missionScreen?.CombatCamera;
		if (camera == null || _layer?.UIContext == null)
		{
			HideBubble(state);
			return;
		}
		Vec3 bubbleAnchor = GetBubbleAnchorWorldPosition(state.Agent);
		if (!IsFinite(bubbleAnchor) || Vec3.DotProduct(camera.Direction, bubbleAnchor - camera.Position) < 0.1f)
		{
			HideBubble(state);
			return;
		}
		// Native agent markers use top-left framebuffer pixels. Gauntlet offsets
		// are layout units: subtract this layer's safe-area origin, then undo scale.
		float screenX = 0f, screenY = 0f, depth = 0f;
		MBWindowManager.WorldToScreen(camera, bubbleAnchor, ref screenX, ref screenY, ref depth);
		float width = Screen.RealScreenResolutionWidth;
		float height = Screen.RealScreenResolutionHeight;
		float inverseScale = _layer.UIContext.CustomInverseScale;
		Vec2 usableArea = _layer.UsableArea;
		if (!IsFinite(screenX) || !IsFinite(screenY) || !IsFinite(depth) || depth < 0f
			|| !IsFinite(inverseScale) || inverseScale <= 0f || width <= 0f || height <= 0f
			|| !IsFinite(usableArea) || usableArea.x <= 0f || usableArea.y <= 0f
			|| screenX < -0.25f * width || screenX > 1.25f * width
			|| screenY < -0.25f * height || screenY > 1.25f * height)
		{
			HideBubble(state);
			return;
		}
		float left = width * (1f - usableArea.x) * 0.5f;
		float top = height * (1f - usableArea.y) * 0.5f;
		float bubbleWidth = state.VM.BubbleWidth;
		if (bubbleWidth <= 0f) bubbleWidth = BUBBLE_MIN_WIDTH;
		float bubbleHeight = EstimateBubbleHeight(state, bubbleWidth);
		state.VM.X = (screenX - left) * inverseScale - bubbleWidth * 0.5f;
		state.VM.Y = (screenY - top) * inverseScale - bubbleHeight - SCREEN_ANCHOR_VERTICAL_PADDING;
		if (!_debugLoggedOnce)
		{
			Logger.Log("FloatingText", $"[Position] agent={state.Agent.Index} anchor=({bubbleAnchor.x:F2},{bubbleAnchor.y:F2},{bubbleAnchor.z:F2}) pixel=({screenX:F1},{screenY:F1}) ui=({state.VM.X:F1},{state.VM.Y:F1}) inverseScale={inverseScale:F4} safeOrigin=({left:F1},{top:F1}) screenRes=({width:F0},{height:F0})");
			_debugLoggedOnce = true;
		}
	}
	private static Vec3 GetBubbleAnchorWorldPosition(Agent agent)
	{
		if (agent == null)
		{
			return default(Vec3);
		}
		Vec3 groundVec = agent.GetWorldPosition().GetGroundVec3();
		float eyeGlobalHeight = agent.GetEyeGlobalHeight();
		if (!IsFinite(eyeGlobalHeight) || eyeGlobalHeight < 0.5f || eyeGlobalHeight > 4.5f)
		{
			eyeGlobalHeight = 1.7f;
		}
		float num = groundVec.z + Math.Max(0.75f, eyeGlobalHeight * 0.45f);
		float num2 = groundVec.z + Math.Max(3.5f, eyeGlobalHeight + 1.25f);
		Vec3 result = default(Vec3);
		bool hasCandidate = false;
		TryUseAnchorCandidate(agent.GetEyeGlobalPosition(), WORLD_ANCHOR_VERTICAL_OFFSET, num, num2, ref result, ref hasCandidate);
		TryUseAnchorCandidate(agent.GetChestGlobalPosition(), 0.35f, groundVec.z + 0.55f, groundVec.z + 3f, ref result, ref hasCandidate);
		if (hasCandidate)
		{
			return result;
		}
		groundVec.z += eyeGlobalHeight + 0.45f;
		return groundVec;
	}

	private static void TryUseAnchorCandidate(Vec3 candidate, float zOffset, float minZ, float maxZ, ref Vec3 result, ref bool hasCandidate)
	{
		if (!IsFinite(candidate))
		{
			return;
		}
		candidate.z += zOffset;
		if (candidate.z < minZ || candidate.z > maxZ)
		{
			return;
		}
		if (!hasCandidate || candidate.z > result.z)
		{
			result = candidate;
			hasCandidate = true;
		}
	}

	private static float EstimateBubbleHeight(FloatingTextItemState state, float bubbleWidth)
	{
		string text = state?.CurrentDisplayedText;
		if (string.IsNullOrEmpty(text))
		{
			text = state?.FullTargetText ?? "";
		}
		int fontSize = state?.VM?.FontSize ?? 14;
		float usableWidth = Math.Max(48f, bubbleWidth - 24f);
		float averageCharWidth = Math.Max(8f, (float)fontSize * 0.95f);
		int maxCharsPerLine = Math.Max(1, (int)(usableWidth / averageCharWidth));
		int lineCount = 1;
		int currentLineLength = 0;
		foreach (char c in text)
		{
			if (c == '\n')
			{
				lineCount++;
				currentLineLength = 0;
				continue;
			}
			currentLineLength++;
			if (currentLineLength > maxCharsPerLine)
			{
				lineCount++;
				currentLineLength = 1;
			}
		}
		float lineHeight = Math.Max(fontSize + 6, 18);
		return (float)lineCount * lineHeight + 12f;
	}

	private static bool IsFinite(Vec2 value)
	{
		return IsFinite(value.x) && IsFinite(value.y);
	}

	private static bool IsFinite(Vec3 value)
	{
		return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
	}

	private static bool IsFinite(float value)
	{
		return !float.IsNaN(value) && !float.IsInfinity(value);
	}

	private static void HideBubble(FloatingTextItemState state)
	{
		state.VM.X = -10000f;
		state.VM.Y = -10000f;
	}

	public void AddOrUpdateText(Agent agent, string text, bool isAppend = false, float typingDurationSeconds = -1f)
	{
		if (agent == null)
		{
			return;
		}
		if (_layer == null || _dataSource == null)
		{
			Logger.Log("FloatingText", $"[WARN] AddOrUpdateText called but layer={_layer != null} dataSource={_dataSource != null} initialized={_isInitialized}");
			return;
		}
		if (!_agentStates.TryGetValue(agent, out var value))
		{
			value = CreateState(agent);
			_agentStates[agent] = value;
			Logger.Log("FloatingText", $"[Add] Created bubble for agent '{agent.Name}' (Index={agent.Index}), total items={_dataSource.Items.Count}");
		}
		if (isAppend)
		{
			value.FullTargetText += text;
		}
		else
		{
			value.FullTargetText = text;
			value.CurrentDisplayedText = "";
			value.TypingTimer = 0f;
			if (value.VM != null)
			{
				value.VM.Text = "";
			}
		}
		int num = ((!string.IsNullOrEmpty(value.FullTargetText)) ? value.FullTargetText.Length : 0);
		if (typingDurationSeconds > 0f && num > 0)
		{
			float num2 = typingDurationSeconds / (float)num;
			if (num2 < 0.01f)
			{
				num2 = 0.01f;
			}
			value.TypingInterval = num2;
		}
		else
		{
			value.TypingInterval = 0.05f;
		}
		value.Lifetime = 0f;
		value.IsFading = false;
		value.VM.Alpha = 1f;
		UpdateBubbleWidth(value);
		value.MaxLifetime = Math.Max(3f, (float)value.FullTargetText.Length * 0.2f);
	}

	private void UpdateBubbleWidth(FloatingTextItemState state)
	{
		if (state == null || state.VM == null)
		{
			return;
		}
		string text = state.CurrentDisplayedText;
		if (string.IsNullOrEmpty(text))
		{
			text = state.FullTargetText ?? "";
		}
		int num = 0;
		int num2 = 0;
		string text2 = text;
		foreach (char c in text2)
		{
			if (c == '\n')
			{
				if (num2 > num)
				{
					num = num2;
				}
				num2 = 0;
			}
			else
			{
				num2++;
			}
		}
		if (num2 > num)
		{
			num = num2;
		}
		if (num <= 0)
		{
			num = 1;
		}
		float num3 = (float)num * 12.5f + 24f;
		if (num3 < 140f)
		{
			num3 = 140f;
		}
		if (num3 > 300f)
		{
			num3 = 300f;
		}
		state.VM.BubbleWidth = num3;
	}

	private FloatingTextItemState CreateState(Agent agent)
	{
		FloatingTextItemVM floatingTextItemVM = new FloatingTextItemVM();
		floatingTextItemVM.Alpha = 1f;
		floatingTextItemVM.Text = "";
		floatingTextItemVM.BubbleWidth = 140f;
		floatingTextItemVM.FontSize = DuelSettings.GetSettings().BubbleFontSize;
		floatingTextItemVM.X = 0f;
		floatingTextItemVM.Y = 0f;
		_dataSource.Items.Add(floatingTextItemVM);
		return new FloatingTextItemState
		{
			Agent = agent,
			VM = floatingTextItemVM,
			FullTargetText = "",
			CurrentDisplayedText = "",
			MaxLifetime = 5f
		};
	}

	private void RemoveItem(Agent agent)
	{
		if (agent != null && _dataSource?.Items != null && _agentStates.TryGetValue(agent, out var value))
		{
			_dataSource.Items.Remove(value.VM);
			_agentStates.Remove(agent);
		}
	}

	public void ClearAll()
	{
		_dataSource?.Items?.Clear();
		_agentStates.Clear();
	}
}
