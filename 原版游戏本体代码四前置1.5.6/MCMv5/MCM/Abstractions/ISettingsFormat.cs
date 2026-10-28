using System.Collections.Generic;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;

namespace MCM.Abstractions;

public interface ISettingsFormat
{
	IEnumerable<string> FormatTypes { get; }

	bool Save(BaseSettings settings, GameDirectory directory, string filename);

	BaseSettings Load(BaseSettings settings, GameDirectory directory, string filename);
}
