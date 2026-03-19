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

	public bool IsBubbleReady()
	{
		return _isInitialized && _layer != null && _dataSource != null;
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
			if (value.CurrentDisplayedText.Length < value.FullTargetText.Length)
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
			else
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
		if (missionScreen == null)
		{
			return;
		}
		SceneView sceneView = null;
		try
		{
			if (missionScreen.SceneLayer != null)
			{
				sceneView = missionScreen.SceneLayer.SceneView;
			}
		}
		catch
		{
		}
		if (sceneView == null)
		{
			return;
		}
		Vec3 bubbleAnchor = GetBubbleAnchorWorldPosition(state.Agent);
		if (!IsFinite(bubbleAnchor))
		{
			HideBubble(state);
			return;
		}
		bool flag = false;
		if (missionScreen.CombatCamera != null)
		{
			Vec3 direction = missionScreen.CombatCamera.Direction;
			Vec3 position = missionScreen.CombatCamera.Position;
			Vec3 v = bubbleAnchor - position;
			if (Vec3.DotProduct(direction, v) < 0.1f)
			{
				flag = true;
			}
		}
		if (flag)
		{
			state.VM.X = -10000f;
			state.VM.Y = -10000f;
			return;
		}
		float num = 0f;
		float num2 = 0f;
		Vec2 vec = sceneView.WorldPointToScreenPoint(bubbleAnchor);
		if (!IsFinite(vec) || vec.x < -0.25f || vec.x > 1.25f || vec.y < -0.25f || vec.y > 1.25f)
		{
			HideBubble(state);
			return;
		}
		num = Screen.RealScreenResolutionWidth;
		num2 = Screen.RealScreenResolutionHeight;
		float num3 = state.VM.BubbleWidth;
		if (num3 <= 0f)
		{
			num3 = 140f;
		}
		float bubbleHeight = EstimateBubbleHeight(state, num3);
		state.VM.X = vec.x * num - num3 * 0.5f;
		state.VM.Y = vec.y * num2 - bubbleHeight - 18f;
		if (!_debugLoggedOnce && state.VM.X > 0f && state.VM.Y > 0f)
		{
			Logger.Log("FloatingText", $"[Position] First valid position: normalized=({vec.x:F3},{vec.y:F3}) -> ui=({state.VM.X:F0},{state.VM.Y:F0}), screenRes=({num:F0},{num2:F0})");
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
