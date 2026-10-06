namespace TaleWorlds.Core;

public abstract class MBGameModel<T> : GameModel where T : GameModel
{
	protected T BaseModel { get; private set; }

	public void Initialize(T baseModel)
	{
		BaseModel = baseModel;
	}
}
