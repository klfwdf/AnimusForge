using System;

namespace psai.Editor;

[Serializable]
public class ProjectProperties : ICloneable
{
	private float _volumeBoost;

	private int _exportSoundQualityInPercent = 100;

	public int WarningThresholdPreBeatMillis { get; set; }

	public bool DefaultCalculatePostAndPrebeatLengthBasedOnBeats { get; set; }

	public int DefaultSegmentSuitabilites { get; set; }

	public bool ForceFullRebuild { get; set; }

	public string ModuleIdPrefix { get; set; }

	public float VolumeBoost
	{
		get
		{
			return _volumeBoost;
		}
		set
		{
			if (value >= 0f && value <= 600f)
			{
				_volumeBoost = value;
			}
			else
			{
				Console.Out.WriteLine("invalid value for VolumeBoost");
			}
		}
	}

	public int ExportSoundQualityInPercent
	{
		get
		{
			return _exportSoundQualityInPercent;
		}
		set
		{
			if (value >= 1 && value <= 100)
			{
				_exportSoundQualityInPercent = value;
			}
		}
	}

	public float DefaultPrebeats { get; set; }

	public float DefaultPostbeats { get; set; }

	public float DefaultBpm { get; set; }

	public int DefaultPrebeatLengthInSamples { get; set; }

	public int DefaultPostbeatLengthInSamples { get; set; }

	public ProjectProperties()
	{
		DefaultBpm = 100f;
		DefaultPostbeats = 4f;
		DefaultPrebeats = 1f;
		WarningThresholdPreBeatMillis = 1500;
		DefaultSegmentSuitabilites = 3;
		ForceFullRebuild = true;
	}

	public ProjectProperties ShallowCopy()
	{
		return (ProjectProperties)MemberwiseClone();
	}

	public object Clone()
	{
		return MemberwiseClone();
	}
}
