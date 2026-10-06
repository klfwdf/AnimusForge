using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade;

public class PlayerConnectionInfo
{
	private Dictionary<string, object> _parameters;

	public readonly PlayerId PlayerID;

	public int SessionKey { get; set; }

	public string Name { get; set; }

	public NetworkCommunicator NetworkPeer { get; set; }

	public PlayerConnectionInfo(PlayerId playerID)
	{
		PlayerID = playerID;
		_parameters = new Dictionary<string, object>();
	}

	public void AddParameter(string name, object parameter)
	{
		if (!_parameters.ContainsKey(name))
		{
			_parameters.Add(name, parameter);
		}
	}

	public T GetParameter<T>(string name) where T : class
	{
		if (_parameters.ContainsKey(name))
		{
			return _parameters[name] as T;
		}
		return null;
	}
}
