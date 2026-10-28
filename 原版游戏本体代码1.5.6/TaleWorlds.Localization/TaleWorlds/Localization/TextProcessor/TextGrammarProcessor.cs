using TaleWorlds.Library;
using TaleWorlds.Localization.Expressions;

namespace TaleWorlds.Localization.TextProcessor;

public static class TextGrammarProcessor
{
	public static string Process(MBTextModel dataRepresentation, TextProcessingContext textContext, TextObject parent = null)
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "Process");
		foreach (TextExpression rootExpression in dataRepresentation.RootExpressions)
		{
			if (rootExpression != null)
			{
				string value = rootExpression.EvaluateString(textContext, parent).ToString();
				mBStringBuilder.Append(value);
			}
			else
			{
				MBTextManager.ThrowLocalizationError("Exp should not be null!");
			}
		}
		return mBStringBuilder.ToStringAndRelease();
	}
}
