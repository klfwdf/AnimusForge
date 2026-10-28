using System.Globalization;
using System.Text;

namespace TaleWorlds.Localization.TextProcessor;

public class DefaultTextProcessor : LanguageSpecificTextProcessor
{
	public override CultureInfo CultureInfoForLanguage => CultureInfo.InvariantCulture;

	public override void ProcessToken(string sourceText, ref int cursorPos, string token, StringBuilder outputString)
	{
	}

	public override void ClearTemporaryData()
	{
	}
}
