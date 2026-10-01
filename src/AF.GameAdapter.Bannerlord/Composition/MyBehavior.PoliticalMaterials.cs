using System.Collections.Generic;

namespace AnimusForge;

public partial class MyBehavior
{
	private static string NormalizePoliticalReasonText(string text)
		=> WeeklyPoliticalMaterialPolicy.NormalizePoliticalReasonText(text);

	private static string BuildPoliticalReasonSentence(string label, IEnumerable<string> parts)
		=> WeeklyPoliticalMaterialPolicy.BuildPoliticalReasonSentence(label, parts);

	private static string TrimPoliticalReasonPart(string text, int maxLength)
		=> WeeklyPoliticalMaterialPolicy.TrimPoliticalReasonPart(text, maxLength);
}
