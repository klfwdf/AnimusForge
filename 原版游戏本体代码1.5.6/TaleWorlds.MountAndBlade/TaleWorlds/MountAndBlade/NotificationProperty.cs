using System;

namespace TaleWorlds.MountAndBlade;

[AttributeUsage(AttributeTargets.Field)]
public class NotificationProperty : Attribute
{
	public string StringId { get; private set; }

	public string SoundIdOne { get; private set; }

	public string SoundIdTwo { get; private set; }

	public NotificationProperty(string stringId, string soundIdOne, string soundIdTwo = "")
	{
		StringId = stringId;
		SoundIdOne = soundIdOne;
		SoundIdTwo = soundIdTwo;
	}
}
