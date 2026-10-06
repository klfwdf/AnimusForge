using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public abstract class GameKeyContext
{
	public enum GameKeyContextType
	{
		Default,
		AuxiliaryNotSerialized,
		AuxiliarySerialized,
		AuxiliarySerializedAndShownInOptions
	}

	private readonly Dictionary<string, HotKey> _registeredHotKeys;

	private readonly MBList<GameKey> _registeredGameKeys;

	private readonly Dictionary<string, GameAxisKey> _registeredAxisKeys;

	private static bool _isRDownSwappedWithRRight = true;

	public string GameKeyCategoryId { get; private set; }

	public GameKeyContextType Type { get; private set; }

	public MBReadOnlyList<GameKey> RegisteredGameKeys => _registeredGameKeys;

	public Dictionary<string, HotKey>.ValueCollection RegisteredHotKeys => _registeredHotKeys.Values;

	public Dictionary<string, GameAxisKey>.ValueCollection RegisteredGameAxisKeys => _registeredAxisKeys.Values;

	protected GameKeyContext(string id, int gameKeysCount, GameKeyContextType type = GameKeyContextType.Default)
	{
		GameKeyCategoryId = id;
		Type = type;
		_registeredHotKeys = new Dictionary<string, HotKey>();
		_registeredAxisKeys = new Dictionary<string, GameAxisKey>();
		_registeredGameKeys = new MBList<GameKey>(gameKeysCount);
		for (int i = 0; i < gameKeysCount; i++)
		{
			_registeredGameKeys.Add(null);
		}
	}

	protected internal void RegisterHotKey(HotKey gameKey, bool addIfMissing = true)
	{
		if (_isRDownSwappedWithRRight)
		{
			for (int i = 0; i < gameKey.Keys.Count; i++)
			{
				Key key = gameKey.Keys[i];
				if ((object)key != null && key.InputKey == InputKey.ControllerRDown)
				{
					key.ChangeKey(InputKey.ControllerRRight);
				}
				else if ((object)key != null && key.InputKey == InputKey.ControllerRRight)
				{
					key.ChangeKey(InputKey.ControllerRDown);
				}
			}
		}
		if (_registeredHotKeys.ContainsKey(gameKey.Id))
		{
			_registeredHotKeys[gameKey.Id] = gameKey;
		}
		else if (addIfMissing)
		{
			_registeredHotKeys.Add(gameKey.Id, gameKey);
		}
	}

	protected internal void RegisterGameKey(GameKey gameKey, bool addIfMissing = true)
	{
		if (_isRDownSwappedWithRRight)
		{
			Key controllerKey = gameKey.ControllerKey;
			if ((object)controllerKey != null && controllerKey.InputKey == InputKey.ControllerRDown)
			{
				controllerKey.ChangeKey(InputKey.ControllerRRight);
			}
			else if ((object)controllerKey != null && controllerKey.InputKey == InputKey.ControllerRRight)
			{
				controllerKey.ChangeKey(InputKey.ControllerRDown);
			}
		}
		_registeredGameKeys[gameKey.Id] = gameKey;
	}

	protected internal void RegisterGameAxisKey(GameAxisKey gameKey, bool addIfMissing = true)
	{
		if (_registeredAxisKeys.ContainsKey(gameKey.Id))
		{
			_registeredAxisKeys[gameKey.Id] = gameKey;
		}
		else if (addIfMissing)
		{
			_registeredAxisKeys.Add(gameKey.Id, gameKey);
		}
	}

	internal static void SetIsRDownSwappedWithRRight(bool value)
	{
		_isRDownSwappedWithRRight = value;
	}

	public HotKey GetHotKey(string hotKeyId)
	{
		HotKey value = null;
		_registeredHotKeys.TryGetValue(hotKeyId, out value);
		return value;
	}

	public GameKey GetGameKey(int gameKeyId)
	{
		for (int i = 0; i < _registeredGameKeys.Count; i++)
		{
			GameKey gameKey = _registeredGameKeys[i];
			if (gameKey != null && gameKey.Id == gameKeyId)
			{
				return gameKey;
			}
		}
		Debug.FailedAssert($"Couldn't find {gameKeyId} in {GameKeyCategoryId}", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetGameKey", 125);
		return null;
	}

	internal GameKey GetGameKey(string gameKeyId)
	{
		for (int i = 0; i < _registeredGameKeys.Count; i++)
		{
			GameKey gameKey = _registeredGameKeys[i];
			if (gameKey != null && gameKey.StringId == gameKeyId)
			{
				return gameKey;
			}
		}
		Debug.FailedAssert("Couldn't find " + gameKeyId + " in " + GameKeyCategoryId, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetGameKey", 140);
		return null;
	}

	internal GameAxisKey GetGameAxisKey(string axisKeyId)
	{
		_registeredAxisKeys.TryGetValue(axisKeyId, out var value);
		return value;
	}

	public string GetHotKeyId(string hotKeyId)
	{
		if (_registeredHotKeys.TryGetValue(hotKeyId, out var value))
		{
			return value.ToString();
		}
		if (_registeredAxisKeys.TryGetValue(hotKeyId, out var value2))
		{
			return value2.ToString();
		}
		Debug.FailedAssert("HotKey with id: " + hotKeyId + " is not registered.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetHotKeyId", 163);
		return "";
	}

	public string GetHotKeyId(int gameKeyId)
	{
		GameKey gameKey = _registeredGameKeys[gameKeyId];
		if (gameKey != null)
		{
			return gameKey.ToString();
		}
		Debug.FailedAssert("GameKey with id: " + gameKeyId + " is not registered.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetHotKeyId", 175);
		return "";
	}
}
