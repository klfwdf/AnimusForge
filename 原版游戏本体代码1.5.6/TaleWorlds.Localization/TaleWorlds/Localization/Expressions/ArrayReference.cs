using TaleWorlds.Library;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class ArrayReference : TextExpression
{
	private TextExpression _indexExp;

	internal override TokenType TokenType => TokenType.ArrayAccess;

	public ArrayReference(string rawValue, TextExpression indexExp)
	{
		base.RawValue = rawValue;
		_indexExp = indexExp;
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		int index = EvaluateAsNumber(_indexExp, context, parent);
		MultiStatement arrayAccess = context.GetArrayAccess(base.RawValue, index);
		if (arrayAccess != null)
		{
			MBStringBuilder mBStringBuilder = default(MBStringBuilder);
			mBStringBuilder.Initialize(16, "EvaluateString");
			foreach (TextExpression subStatement in arrayAccess.SubStatements)
			{
				mBStringBuilder.Append(subStatement.EvaluateString(context, parent));
			}
			return mBStringBuilder.ToStringAndRelease();
		}
		return "";
	}
}
