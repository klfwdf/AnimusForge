using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaleWorlds.DotNet;
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
		if (state != null)
		{
			string text = (state.CurrentDisplayedText = (state.FullTargetText = state.CurrentDisplayedText ?? ""));
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
	}

	private MissionScreen GetMissionScreen()
	{
		if (((MissionView)this).MissionScreen != null)
		{
			return ((MissionView)this).MissionScreen;
		}
		ScreenBase topScreen = ScreenManager.TopScreen;
		return (MissionScreen)(object)((topScreen is MissionScreen) ? topScreen : null);
	}

	public override void OnBehaviorInitialize()
	{
		((MissionBehavior)this).OnBehaviorInitialize();
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
		((MissionView)this).OnMissionScreenInitialize();
		Logger.Log("FloatingText", "[Init] OnMissionScreenInitialize called");
		TryInitializeLayer();
		FloatingTextManager.Instance.MissionView = this;
		Logger.Log("FloatingText", "[Init] MissionView registered to FloatingTextManager");
	}

	private void TryInitializeLayer()
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
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
			Logger.Log("FloatingText", "[Init] MissionScreen found: " + ((object)missionScreen).GetType().Name + ", attempting layer creation...");
			_dataSource = new FloatingTextVM();
			_layer = new GauntletLayer("FloatingTextLayer", 1000, false);
			Logger.Log("FloatingText", "[Init] GauntletLayer created, loading movie...");
			_layer.LoadMovie("FloatingTextLayer", (ViewModel)(object)_dataSource);
			Logger.Log("FloatingText", "[Init] Movie loaded, adding layer to MissionScreen...");
			((ScreenBase)missionScreen).AddLayer((ScreenLayer)(object)_layer);
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
			MissionScreen missionScreen = GetMissionScreen();
			if (missionScreen != null)
			{
				((ScreenBase)missionScreen).RemoveLayer((ScreenLayer)(object)_layer);
			}
			_layer = null;
			_dataSource = null;
		}
		_agentStates.Clear();
		_isInitialized = false;
		_initRetryCount = 0;
		_debugLoggedOnce = false;
		((MissionView)this).OnMissionScreenFinalize();
	}

	public override void OnMissionTick(float dt)
	{
		((MissionBehavior)this).OnMissionTick(dt);
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
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		MissionScreen missionScreen = GetMissionScreen();
		if (missionScreen == null)
		{
			return;
		}
		SceneView val = null;
		try
		{
			if (missionScreen.SceneLayer != null)
			{
				val = missionScreen.SceneLayer.SceneView;
			}
		}
		catch
		{
		}
		if ((NativeObject)(object)val == (NativeObject)null)
		{
			return;
		}
		Vec3 bubbleAnchorWorldPosition = GetBubbleAnchorWorldPosition(state.Agent);
		if (!IsFinite(bubbleAnchorWorldPosition))
		{
			HideBubble(state);
			return;
		}
		bool flag = false;
		if ((NativeObject)(object)missionScreen.CombatCamera != (NativeObject)null)
		{
			Vec3 direction = missionScreen.CombatCamera.Direction;
			Vec3 position = missionScreen.CombatCamera.Position;
			Vec3 val2 = bubbleAnchorWorldPosition - position;
			if (Vec3.DotProduct(direction, val2) < 0.1f)
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
		Vec2 val3 = val.WorldPointToScreenPoint(bubbleAnchorWorldPosition);
		if (!IsFinite(val3) || val3.x < -0.25f || val3.x > 1.25f || val3.y < -0.25f || val3.y > 1.25f)
		{
			HideBubble(state);
			return;
		}
		GetUiCanvasSize(out var width, out var height);
		float num = state.VM.BubbleWidth;
		if (num <= 0f)
		{
			num = 140f;
		}
		float num2 = EstimateBubbleHeight(state, num);
		state.VM.X = val3.x * width - num * 0.5f;
		state.VM.Y = val3.y * height - num2 - 18f;
		if (!_debugLoggedOnce && state.VM.X > 0f && state.VM.Y > 0f)
		{
			Logger.Log("FloatingText", $"[Position] First valid position: normalized=({val3.x:F3},{val3.y:F3}) -> ui=({state.VM.X:F0},{state.VM.Y:F0}), screenRes=({width:F0},{height:F0})");
			_debugLoggedOnce = true;
		}
	}

	private void GetUiCanvasSize(out float width, out float height)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		width = 1920f;
		height = 1080f;
		try
		{
			if (_layer != null && _layer.UIContext != null)
			{
				float num = Math.Max(0.0001f, _layer.UIContext.CustomInverseScale);
				float num2 = Math.Max(0.0001f, ((ScreenLayer)_layer).UsableArea.x);
				float num3 = Math.Max(0.0001f, ((ScreenLayer)_layer).UsableArea.y);
				width = Screen.RealScreenResolutionWidth * num2 * num;
				height = Screen.RealScreenResolutionHeight * num3 * num;
			}
		}
		catch
		{
		}
	}

	private static Vec3 GetBubbleAnchorWorldPosition(Agent agent)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		if (agent == null)
		{
			return default(Vec3);
		}
		WorldPosition worldPosition = agent.GetWorldPosition();
		Vec3 groundVec = ((WorldPosition)(ref worldPosition)).GetGroundVec3();
		float num = agent.GetEyeGlobalHeight();
		if (!IsFinite(num) || num < 0.5f || num > 4.5f)
		{
			num = 1.7f;
		}
		float minZ = groundVec.z + Math.Max(0.75f, num * 0.45f);
		float maxZ = groundVec.z + Math.Max(3.5f, num + 1.25f);
		Vec3 result = default(Vec3);
		bool hasCandidate = false;
		TryUseAnchorCandidate(agent.GetEyeGlobalPosition(), 0.15f, minZ, maxZ, ref result, ref hasCandidate);
		TryUseAnchorCandidate(agent.GetChestGlobalPosition(), 0.35f, groundVec.z + 0.55f, groundVec.z + 3f, ref result, ref hasCandidate);
		if (hasCandidate)
		{
			return result;
		}
		groundVec.z += num + 0.45f;
		return groundVec;
	}

	private static void TryUseAnchorCandidate(Vec3 candidate, float zOffset, float minZ, float maxZ, ref Vec3 result, ref bool hasCandidate)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (IsFinite(candidate))
		{
			candidate.z += zOffset;
			if (!(candidate.z < minZ) && !(candidate.z > maxZ) && (!hasCandidate || candidate.z > result.z))
			{
				result = candidate;
				hasCandidate = true;
			}
		}
	}

	private static float EstimateBubbleHeight(FloatingTextItemState state, float bubbleWidth)
	{
		string text = state?.CurrentDisplayedText;
		if (string.IsNullOrEmpty(text))
		{
			text = state?.FullTargetText ?? "";
		}
		int num = state?.VM?.FontSize ?? 14;
		float num2 = Math.Max(48f, bubbleWidth - 24f);
		float num3 = Math.Max(8f, (float)num * 0.95f);
		int num4 = Math.Max(1, (int)(num2 / num3));
		int num5 = 1;
		int num6 = 0;
		string text2 = text;
		foreach (char c in text2)
		{
			if (c == '\n')
			{
				num5++;
				num6 = 0;
				continue;
			}
			num6++;
			if (num6 > num4)
			{
				num5++;
				num6 = 1;
			}
		}
		float num7 = Math.Max(num + 6, 18);
		return (float)num5 * num7 + 12f;
	}

	private static bool IsFinite(Vec2 value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return IsFinite(value.x) && IsFinite(value.y);
	}

	private static bool IsFinite(Vec3 value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
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
			Logger.Log("FloatingText", $"[Add] Created bubble for agent '{agent.Name}' (Index={agent.Index}), total items={((Collection<FloatingTextItemVM>)(object)_dataSource.Items).Count}");
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
		string text3 = text2;
		foreach (char c in text3)
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
		((Collection<FloatingTextItemVM>)(object)_dataSource.Items).Add(floatingTextItemVM);
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
			((Collection<FloatingTextItemVM>)(object)_dataSource.Items).Remove(value.VM);
			_agentStates.Remove(agent);
		}
	}

	public void ClearAll()
	{
		((Collection<FloatingTextItemVM>)(object)_dataSource?.Items)?.Clear();
		_agentStates.Clear();
	}
}
