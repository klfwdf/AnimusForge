namespace Bannerlord.ModuleManager;

internal record ApplicationVersionRange
{
	public static Bannerlord.ModuleManager.ApplicationVersionRange Empty => new Bannerlord.ModuleManager.ApplicationVersionRange();

	public Bannerlord.ModuleManager.ApplicationVersion Min { get; set; } = Bannerlord.ModuleManager.ApplicationVersion.Empty;

	public Bannerlord.ModuleManager.ApplicationVersion Max { get; set; } = Bannerlord.ModuleManager.ApplicationVersion.Empty;

	public ApplicationVersionRange()
	{
	}

	public ApplicationVersionRange(Bannerlord.ModuleManager.ApplicationVersion min, Bannerlord.ModuleManager.ApplicationVersion max)
	{
		Max = max;
		Min = min;
	}

	public bool IsSame(Bannerlord.ModuleManager.ApplicationVersionRange? other)
	{
		if (Min.IsSame(other?.Min))
		{
			return Max.IsSame(other?.Max);
		}
		return false;
	}

	public bool IsSameWithChangeSet(Bannerlord.ModuleManager.ApplicationVersionRange? other)
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

	public static bool TryParse(string versionRangeAsString, out Bannerlord.ModuleManager.ApplicationVersionRange versionRange)
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
		if (Bannerlord.ModuleManager.ApplicationVersion.TryParse(versionAsString, out Bannerlord.ModuleManager.ApplicationVersion version, asMin: true) && Bannerlord.ModuleManager.ApplicationVersion.TryParse(versionAsString2, out Bannerlord.ModuleManager.ApplicationVersion version2, asMin: false))
		{
			versionRange = new Bannerlord.ModuleManager.ApplicationVersionRange
			{
				Min = version,
				Max = version2
			};
			return true;
		}
		return false;
	}
}
