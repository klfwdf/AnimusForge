using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization.Expressions;

internal class MultiStatement : TextExpression
{
	private MBList<TextExpression> _subStatements = new MBList<TextExpression>();

	public MBReadOnlyList<TextExpression> SubStatements => _subStatements;

	internal override TokenType TokenType => TokenType.MultiStatement;

	public MultiStatement(IEnumerable<TextExpression> subStatements)
	{
		_subStatements = subStatements.ToMBList();
	}

	public void AddStatement(TextExpression s2)
	{
		_subStatements.Add(s2);
	}

	internal override string EvaluateString(TextProcessingContext context, TextObject parent)
	{
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "EvaluateString");
		foreach (TextExpression subStatement in _subStatements)
		{
			if (subStatement != null)
			{
				mBStringBuilder.Append(subStatement.EvaluateString(context, parent));
			}
		}
		return mBStringBuilder.ToStringAndRelease();
	}
}
