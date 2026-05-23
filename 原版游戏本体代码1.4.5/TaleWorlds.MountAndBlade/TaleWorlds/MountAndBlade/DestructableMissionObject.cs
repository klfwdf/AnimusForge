using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[Obsolete]
public class DestructableMissionObject : MissionObject
{
	protected internal override void OnEditorInit()
	{
		Debug.FailedAssert("This scene is using old prefabs with the old destruction system, please update!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\DestructableMissionObject.cs", "OnEditorInit", 18);
	}

	protected internal override void OnInit()
	{
		Debug.FailedAssert("This scene is using old prefabs with the old destruction system, please update! The game will now close!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\DestructableMissionObject.cs", "OnInit", 23);
		Environment.Exit(0);
	}
}
