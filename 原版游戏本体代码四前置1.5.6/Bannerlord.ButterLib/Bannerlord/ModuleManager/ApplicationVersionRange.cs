namespace Bannerlord.ModuleManager;

internal record ApplicationVersionRange
{
	public static ApplicationVersionRange Empty => new ApplicationVersionRange();

	public ApplicationVersion Min { get; set; } = ApplicationVersion.Empty;

	public ApplicationVersion Max { get; set; } = ApplicationVersion.Empty;

	public ApplicationVersionRange()
	{
	}

	public ApplicationVersionRange(ApplicationVersion min, ApplicationVersion max)
	{
		Max = max;
		Min = min;
	}

	public bool IsSame(ApplicationVersionRange? other)
	{
		if (Min.IsSame(other?.Min))
		{
			return Max.IsSame(other?.Max);
		}
		return false;
	}

	public bool IsSameWithChangeSet(ApplicationVersionRange? other)
	{
		if (Min.IsSameWithChangeSet(other?.Min))
		{
			return Max.IsSameWithChangeSet(other?.Max);
		}
		return false;
	}

	public override string ToString()
	{
		return $"{Min} - {Max}";
	}

	public static bool TryParse(string versionRangeAsString, out ApplicationVersionRange versionRange)
	{
		versionRange = Empty;
		if (string.IsNullOrWhiteSpace(versionRangeAsString))
		{
			return false;
		}
		versionRangeAsString = versionRangeAsString.Replace(" ", string.Empty);
		int num = versionRangeAsString.IndexOf('-');
		if (num < 0)
		{
			return false;
		}
		string versionAsString = versionRangeAsString.Substring(0, num);
		string versionAsString2 = versionRangeAsString.Substring(num + 1, versionRangeAsString.Length - 1 - num);
		if (ApplicationVersion.TryParse(versionAsString, out ApplicationVersion version, asMin: true) && ApplicationVersion.TryParse(versionAsString2, out ApplicationVersion version2, asMin: false))
		{
			versionRange = new ApplicationVersionRange
			{
				Min = version,
				Max = version2
			};
			return true;
		}
		return false;
	}
}
