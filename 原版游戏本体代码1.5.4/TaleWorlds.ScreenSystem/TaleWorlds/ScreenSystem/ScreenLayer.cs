using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.ScreenSystem;

public abstract class ScreenLayer : IComparable
{
	public string Name { get; private set; }

	public float Scale => ScreenManager.Scale;

	public Vec2 UsableArea => ScreenManager.UsableArea;

	public InputContext Input { get; private set; }

	public InputRestrictions InputRestrictions { get; private set; }

	public bool LastActiveState { get; set; }

	public bool IsFinalized { get; private set; }

	public bool IsActive { get; private set; }

	public bool IsHitThisFrame { get; internal set; }

	public bool IsFocusLayer { get; set; }

	public CursorType ActiveCursor { get; set; }

	protected InputType _usedInputs { get; set; }

	public int ScreenOrderInLastFrame { get; internal set; }

	public InputUsageMask InputUsageMask => InputRestrictions.InputUsageMask;

	public static event Action<ScreenLayer> OnLayerActiveStateChanged;

	protected ScreenLayer(string name, int localOrder)
	{
		InputRestrictions = new InputRestrictions(localOrder);
		Input = new InputContext();
		Name = name;
		LastActiveState = true;
		IsFinalized = false;
		IsActive = false;
		IsFocusLayer = false;
		_usedInputs = InputType.None;
		ActiveCursor = CursorType.Default;
	}

	protected internal virtual void Tick(float dt)
	{
	}

	protected internal virtual void LateUpdate(float dt)
	{
	}

	protected internal virtual void RenderTick(float dt)
	{
	}

	protected internal virtual void Update(IReadOnlyList<int> lastKeysPressed)
	{
	}

	internal void HandleFinalize()
	{
		if (IsFinalized)
		{
			Debug.FailedAssert("Screen layer is already finalized", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.ScreenSystem\\ScreenLayer.cs", "HandleFinalize", 74);
			return;
		}
		OnFinalize();
		IsFinalized = true;
	}

	internal void HandleGainFocus()
	{
		Input.ResetLastDownKeys();
		OnGainFocus();
	}

	internal void HandleLoseFocus()
	{
		Input.ResetLastDownKeys();
		OnLoseFocus();
	}

	protected virtual void OnActivate()
	{
		IsFinalized = false;
	}

	protected virtual void OnDeactivate()
	{
	}

	protected internal virtual void OnGainFocus()
	{
	}

	protected internal virtual void OnLoseFocus()
	{
	}

	internal void HandleActivate()
	{
		if (!IsActive)
		{
			IsActive = true;
			OnActivate();
			ScreenLayer.OnLayerActiveStateChanged?.Invoke(this);
		}
	}

	internal void HandleDeactivate()
	{
		if (IsActive)
		{
			OnDeactivate();
			IsActive = false;
			ScreenManager.TryLoseFocus(this);
			ScreenLayer.OnLayerActiveStateChanged?.Invoke(this);
		}
	}

	protected virtual void OnFinalize()
	{
	}

	protected internal virtual void RefreshGlobalOrder(ref int currentOrder)
	{
	}

	public virtual void DrawDebugInfo()
	{
		ScreenManager.EngineInterface.DrawDebugText($"Order: {InputRestrictions.Order}");
		ScreenManager.EngineInterface.DrawDebugText($"Is Layer Focusable: {IsFocusLayer}");
		ScreenManager.EngineInterface.DrawDebugText($"Is FocusedLayer: {this == ScreenManager.FocusedLayer}");
		ScreenManager.EngineInterface.DrawDebugText($"Keys Allowed: {Input.IsKeysAllowed}");
		ScreenManager.EngineInterface.DrawDebugText($"Controller Allowed: {Input.IsControllerAllowed}");
		ScreenManager.EngineInterface.DrawDebugText($"Mouse Button Allowed: {Input.IsMouseButtonAllowed}");
		ScreenManager.EngineInterface.DrawDebugText($"Mouse Wheel Allowed: {Input.IsMouseWheelAllowed}");
	}

	public virtual void EarlyProcessEvents(InputType handledInputs)
	{
		_usedInputs = handledInputs;
	}

	public virtual void ProcessEvents()
	{
		Input.IsKeysAllowed = _usedInputs.HasAnyFlag(InputType.Key);
		Input.IsMouseButtonAllowed = _usedInputs.HasAnyFlag(InputType.MouseButton);
		Input.IsMouseWheelAllowed = _usedInputs.HasAnyFlag(InputType.MouseWheel);
	}

	public virtual bool HitTest(Vector2 position)
	{
		return false;
	}

	public virtual bool HitTest()
	{
		return false;
	}

	public virtual bool FocusTest()
	{
		return false;
	}

	public virtual bool IsFocusedOnInput()
	{
		return false;
	}

	public virtual void OnOnScreenKeyboardDone(string inputText)
	{
	}

	public virtual void OnOnScreenKeyboardCanceled()
	{
	}

	public int CompareTo(object obj)
	{
		if (!(obj is ScreenLayer screenLayer))
		{
			return 1;
		}
		if (screenLayer == this)
		{
			return 0;
		}
		if (InputRestrictions.Order == screenLayer.InputRestrictions.Order)
		{
			return InputRestrictions.Id.CompareTo(screenLayer.InputRestrictions.Id);
		}
		return InputRestrictions.Order.CompareTo(screenLayer.InputRestrictions.Order);
	}

	public virtual void UpdateLayout()
	{
	}
}
