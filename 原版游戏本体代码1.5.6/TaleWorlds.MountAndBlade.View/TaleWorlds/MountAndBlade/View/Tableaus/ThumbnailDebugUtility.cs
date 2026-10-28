using System.Text;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

internal static class ThumbnailDebugUtility
{
	internal static string CreateDebugIdFrom(string renderId, string typeId, string additionalInfo = "")
	{
		string value = Common.CreateNanoIdFrom(renderId);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("uit");
		stringBuilder.Append('_');
		stringBuilder.Append(typeId);
		stringBuilder.Append('_');
		stringBuilder.Append(value);
		stringBuilder.Append('_');
		stringBuilder.Append(additionalInfo);
		string text = stringBuilder.ToString();
		if (text.Length > 127)
		{
			text = text.Substring(0, 127);
		}
		return text;
	}
}
