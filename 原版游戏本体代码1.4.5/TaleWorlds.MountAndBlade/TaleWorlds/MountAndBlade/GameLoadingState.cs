using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class GameLoadingState : GameState
{
	private bool _loadingFinished;

	private MBGameManager _gameLoader;

	public override bool IsMusicMenuState => true;

	public void SetLoadingParameters(MBGameManager gameLoader)
	{
		Game.OnGameCreated += OnGameCreated;
		_gameLoader = gameLoader;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!_loadingFinished)
		{
			_loadingFinished = _gameLoader.DoLoadingForGameManager();
			return;
		}
		GameStateManager.Current = Game.Current.GameStateManager;
		_gameLoader.OnLoadFinished();
	}

	private void OnGameCreated()
	{
		Game.OnGameCreated -= OnGameCreated;
		Game.Current.OnItemDeserializedEvent += delegate(ItemObject itemObject)
		{
			if (itemObject.Type == ItemObject.ItemTypeEnum.HandArmor)
			{
				Utilities.RegisterMeshForGPUMorph(itemObject.MultiMeshName);
			}
		};
	}
}
