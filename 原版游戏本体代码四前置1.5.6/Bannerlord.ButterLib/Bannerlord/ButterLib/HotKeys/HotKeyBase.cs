using System;
using TaleWorlds.InputSystem;

namespace Bannerlord.ButterLib.HotKeys;

public abstract class HotKeyBase
{
	private bool _isKeyDown;

	internal int Id { get; set; }

	internal GameKey? GameKey { get; set; }

	protected internal string Uid { get; }

	protected internal virtual string DisplayName { get; }

	protected internal virtual string Description { get; } = "No Description Set.";

	protected internal virtual InputKey DefaultKey { get; } = InputKey.Invalid;

	protected internal virtual string Category { get; } = string.Empty;

	public Func<bool>? Predicate { get; set; }

	public bool IsEnabled { get; set; } = true;

	public event Action? OnPressedEvent;

	public event Action? OnReleasedEvent;

	public event Action? IsDownEvent;

	public event Action? IsDownAndReleasedEvent;

	protected internal HotKeyBase(string uid)
	{
		Uid = uid;
		DisplayName = Uid;
	}

	protected internal HotKeyBase(string uid, string displayName, string description, InputKey defaultKey, string category)
	{
		Uid = uid;
		DisplayName = displayName;
		Description = description;
		DefaultKey = defaultKey;
		Category = category;
	}

	public static implicit operator GameKey(HotKeyBase hotKey)
	{
		return hotKey.GameKey ?? throw new Exception();
	}

	protected virtual void OnPressed()
	{
	}

	protected virtual void OnReleased()
	{
	}

	protected virtual void IsDown()
	{
	}

	protected virtual void IsDownAndReleased()
	{
	}

	internal bool ShouldExecute()
	{
		if (IsEnabled)
		{
			if (Predicate != null)
			{
				if (Predicate != null)
				{
					return Predicate();
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal void OnPressedInternal()
	{
		OnPressedEvent?.Invoke();
		OnPressed();
	}

	internal void OnReleasedInternal()
	{
		if (_isKeyDown)
		{
			_isKeyDown = false;
			IsDownAndReleasedInternal();
		}
		OnReleasedEvent?.Invoke();
		OnReleased();
	}

	internal void IsDownInternal()
	{
		_isKeyDown = true;
		IsDownEvent?.Invoke();
		IsDown();
	}

	internal void IsDownAndReleasedInternal()
	{
		IsDownAndReleasedEvent?.Invoke();
		IsDownAndReleased();
	}
}
