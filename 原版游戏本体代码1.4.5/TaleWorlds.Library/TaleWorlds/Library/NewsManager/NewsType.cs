using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TaleWorlds.Library.NewsManager;

public struct NewsType
{
	[JsonConverter(typeof(StringEnumConverter))]
	public NewsItem.NewsTypes Type { get; set; }

	public int Index { get; set; }
}
