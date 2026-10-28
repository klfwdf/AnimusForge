using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Load;

internal abstract class MemberLoadData : VariableLoadData
{
	public ObjectLoadData ObjectLoadData { get; private set; }

	protected MemberLoadData(ObjectLoadData objectLoadData, IReader reader)
		: base(objectLoadData.Context, reader)
	{
		ObjectLoadData = objectLoadData;
	}
}
