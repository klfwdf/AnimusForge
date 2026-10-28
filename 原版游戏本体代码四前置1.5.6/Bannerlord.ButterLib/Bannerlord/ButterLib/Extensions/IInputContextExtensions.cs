using System;
using System.Linq;
using Bannerlord.ButterLib.HotKeys;
using TaleWorlds.InputSystem;

namespace Bannerlord.ButterLib.Extensions;

public static class IInputContextExtensions
{
	private sealed class ActionOnDispose : IDisposable
	{
		private readonly Action _action;

		public ActionOnDispose(Action action)
		{
			_action = action;
		}

		public void Dispose()
		{
			_action?.Invoke();
		}
	}

	public static IDisposable? SubscribeToIsDownEvent<THotKeyBase>(this IInputContext inputContext, Action action) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKey = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKey == null || !hotKey.ShouldExecute())
		{
			return null;
		}
		hotKey.IsDownEvent += action;
		return new ActionOnDispose(delegate
		{
			hotKey.IsDownEvent -= action;
		});
	}

	public static IDisposable? SubscribeToOnPressedEvent<THotKeyBase>(this IInputContext inputContext, Action action) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKey = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKey == null || !hotKey.ShouldExecute())
		{
			return null;
		}
		hotKey.OnPressedEvent += action;
		return new ActionOnDispose(delegate
		{
			hotKey.OnPressedEvent -= action;
		});
	}

	public static IDisposable? SubscribeToOnReleasedEvent<THotKeyBase>(this IInputContext inputContext, Action action) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKey = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKey == null || !hotKey.ShouldExecute())
		{
			return null;
		}
		hotKey.OnReleasedEvent += action;
		return new ActionOnDispose(delegate
		{
			hotKey.OnReleasedEvent -= action;
		});
	}

	public static IDisposable? SubscribeToIsDownAndReleasedEvent<THotKeyBase>(this IInputContext inputContext, Action action) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKey = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKey == null || !hotKey.ShouldExecute())
		{
			return null;
		}
		hotKey.IsDownAndReleasedEvent += action;
		return new ActionOnDispose(delegate
		{
			hotKey.IsDownAndReleasedEvent -= action;
		});
	}

	public static bool IsHotKeyDown<THotKeyBase>(this IInputContext inputContext) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKeyBase = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKeyBase == null || !hotKeyBase.ShouldExecute() || hotKeyBase.GameKey == null)
		{
			return false;
		}
		InputKey? inputKey = hotKeyBase.GameKey.KeyboardKey?.InputKey;
		if (inputKey.HasValue)
		{
			InputKey valueOrDefault = inputKey.GetValueOrDefault();
			if (inputContext.IsKeyDown(valueOrDefault))
			{
				return true;
			}
		}
		inputKey = hotKeyBase.GameKey.ControllerKey?.InputKey;
		if (inputKey.HasValue)
		{
			InputKey valueOrDefault2 = inputKey.GetValueOrDefault();
			return inputContext.IsKeyDown(valueOrDefault2);
		}
		return false;
	}

	public static bool IsHotKeyPressed<THotKeyBase>(this IInputContext inputContext) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKeyBase = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKeyBase == null || !hotKeyBase.ShouldExecute() || hotKeyBase.GameKey == null)
		{
			return false;
		}
		InputKey? inputKey = hotKeyBase.GameKey.KeyboardKey?.InputKey;
		if (inputKey.HasValue)
		{
			InputKey valueOrDefault = inputKey.GetValueOrDefault();
			if (inputContext.IsKeyPressed(valueOrDefault))
			{
				return true;
			}
		}
		inputKey = hotKeyBase.GameKey.ControllerKey?.InputKey;
		if (inputKey.HasValue)
		{
			InputKey valueOrDefault2 = inputKey.GetValueOrDefault();
			return inputContext.IsKeyPressed(valueOrDefault2);
		}
		return false;
	}

	public static bool IsHotKeyReleased<THotKeyBase>(this IInputContext inputContext) where THotKeyBase : HotKeyBase
	{
		HotKeyBase hotKeyBase = Bannerlord.ButterLib.HotKeys.HotKeyManager.StaticInstance?.HotKeys.FirstOrDefault((HotKeyBase x) => x.GetType() == typeof(THotKeyBase));
		if (hotKeyBase == null || !hotKeyBase.ShouldExecute() || hotKeyBase.GameKey == null)
		{
			return false;
		}
		InputKey? inputKey = hotKeyBase.GameKey.KeyboardKey?.InputKey;
		if (inputKey.HasValue)
		{
			InputKey valueOrDefault = inputKey.GetValueOrDefault();
			if (inputContext.IsKeyReleased(valueOrDefault))
			{
				return true;
			}
		}
		inputKey = hotKeyBase.GameKey.ControllerKey?.InputKey;
		if (inputKey.HasValue)
		{
			InputKey valueOrDefault2 = inputKey.GetValueOrDefault();
			return inputContext.IsKeyReleased(valueOrDefault2);
		}
		return false;
	}
}
