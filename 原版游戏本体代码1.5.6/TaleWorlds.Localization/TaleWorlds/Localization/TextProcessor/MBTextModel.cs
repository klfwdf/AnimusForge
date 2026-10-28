using TaleWorlds.Library;
using TaleWorlds.Localization.Expressions;

namespace TaleWorlds.Localization.TextProcessor;

public class MBTextModel
{
	internal MBList<TextExpression> _rootExpressions = new MBList<TextExpression>();

	internal MBReadOnlyList<TextExpression> RootExpressions => _rootExpressions;

	internal void AddRootExpression(TextExpression newExp)
	{
		_rootExpressions.Add(newExp);
	}
}
