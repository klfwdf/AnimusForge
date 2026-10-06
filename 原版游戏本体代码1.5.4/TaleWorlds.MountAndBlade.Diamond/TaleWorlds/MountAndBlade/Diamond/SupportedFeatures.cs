using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class SupportedFeatures
{
	public int Features;

	public SupportedFeatures()
	{
		Features = -1;
	}

	public SupportedFeatures(int features)
	{
		Features = features;
	}

	public bool SupportsFeatures(Features feature)
	{
		return ((uint)Features & (uint)feature) == (uint)feature;
	}
}
