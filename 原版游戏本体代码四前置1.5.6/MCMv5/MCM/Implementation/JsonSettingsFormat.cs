using System.Collections.Generic;
using BUTR.DependencyInjection.Logger;

namespace MCM.Implementation;

public sealed class JsonSettingsFormat : BaseJsonSettingsFormat
{
	public override IEnumerable<string> FormatTypes => new string[2] { "json", "json2" };

	public JsonSettingsFormat(IBUTRLogger<JsonSettingsFormat> logger)
		: base(logger)
	{
	}
}
