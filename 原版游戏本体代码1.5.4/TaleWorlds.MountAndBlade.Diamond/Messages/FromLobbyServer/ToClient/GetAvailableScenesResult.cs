using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetAvailableScenesResult : FunctionResult
{
	[JsonProperty]
	public AvailableScenes AvailableScenes { get; private set; }

	public GetAvailableScenesResult()
	{
	}

	public GetAvailableScenesResult(AvailableScenes scenes)
	{
		AvailableScenes = scenes;
	}
}
