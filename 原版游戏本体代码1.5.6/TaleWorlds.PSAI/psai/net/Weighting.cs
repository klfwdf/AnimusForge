namespace psai.net;

public class Weighting
{
	public float switchGroups;

	public float intensityVsVariety;

	public float lowPlaycountVsRandom;

	internal Weighting()
	{
		intensityVsVariety = 0.5f;
		lowPlaycountVsRandom = 0.9f;
		switchGroups = 0.5f;
	}
}
