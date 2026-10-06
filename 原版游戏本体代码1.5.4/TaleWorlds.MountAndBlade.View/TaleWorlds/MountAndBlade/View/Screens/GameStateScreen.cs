using System;

namespace TaleWorlds.MountAndBlade.View.Screens;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GameStateScreen : Attribute
{
	public Type GameStateType { get; private set; }

	public GameStateScreen(Type gameStateType)
	{
		GameStateType = gameStateType;
	}
}
