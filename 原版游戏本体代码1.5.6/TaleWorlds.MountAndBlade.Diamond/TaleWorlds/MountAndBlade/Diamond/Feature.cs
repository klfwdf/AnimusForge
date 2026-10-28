using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class Feature : Attribute
{
	public Features FeatureFlag { get; private set; }

	public Feature(Features flag)
	{
		FeatureFlag = flag;
	}
}
