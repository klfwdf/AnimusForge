using System;

namespace psai.net;

internal class ThemeQueueEntry : ICloneable
{
	internal PsaiPlayMode playmode;

	internal int themeId;

	internal float startIntensity;

	internal int restTimeMillis;

	internal bool holdIntensity;

	internal int musicDuration;

	internal ThemeQueueEntry()
	{
		playmode = PsaiPlayMode.regular;
		themeId = -1;
		startIntensity = 1f;
		restTimeMillis = 0;
		holdIntensity = false;
	}

	public object Clone()
	{
		return (ThemeQueueEntry)MemberwiseClone();
	}

	public override string ToString()
	{
		return $"playmode:{playmode}  themeId:{themeId}  startIntensity:{startIntensity}  restTimeMillis:{restTimeMillis}  holdIntensity:{holdIntensity}  musicDuration:{musicDuration}";
	}
}
