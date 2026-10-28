using System.Collections.Generic;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;

namespace MCM.Abstractions;

public sealed class NoneSettingsFormat : ISettingsFormat
{
	public IEnumerable<string> FormatTypes { get; } = new string[1] { "none" };

	public BaseSettings Load(BaseSettings settings, GameDirectory directory, string filename)
	{
		return settings;
	}

	public bool Save(BaseSettings settings, GameDirectory directory, string filename)
	{
		return true;
	}
}
