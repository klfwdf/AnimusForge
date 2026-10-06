using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class RandomTimer : Timer
{
	private float durationMin;

	private float durationMax;

	public RandomTimer(float gameTime, float durationMin, float durationMax)
		: base(gameTime, MBRandom.RandomFloatRanged(durationMin, durationMax))
	{
		this.durationMin = durationMin;
		this.durationMax = durationMax;
	}

	public override bool Check(float gameTime)
	{
		bool result = false;
		bool flag;
		do
		{
			flag = base.Check(gameTime);
			if (flag)
			{
				RecomputeDuration();
				result = true;
			}
		}
		while (flag);
		return result;
	}

	public void ChangeDuration(float min, float max)
	{
		durationMin = min;
		durationMax = max;
		RecomputeDuration();
	}

	public void RecomputeDuration()
	{
		base.Duration = MBRandom.RandomFloatRanged(durationMin, durationMax);
	}
}
