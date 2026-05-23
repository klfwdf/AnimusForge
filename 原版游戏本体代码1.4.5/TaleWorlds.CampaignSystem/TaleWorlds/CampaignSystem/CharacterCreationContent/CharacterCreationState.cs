using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public class CharacterCreationState : PlayerGameState
{
	private CharacterCreationManager _characterCreationManager;

	private ICharacterCreationStateHandler _handler;

	public CharacterCreationManager CharacterCreationManager
	{
		get
		{
			return _characterCreationManager;
		}
		private set
		{
			_characterCreationManager = value;
		}
	}

	public ICharacterCreationStateHandler Handler
	{
		get
		{
			return _handler;
		}
		set
		{
			_handler = value;
		}
	}

	public CharacterCreationState()
	{
		CharacterCreationManager = new CharacterCreationManager(this);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		CharacterCreationManager.OnStateActivated();
	}

	public void FinalizeCharacterCreationState()
	{
		Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
		Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<MapState>());
		PartyBase.MainParty.SetVisualAsDirty();
		_handler?.OnCharacterCreationFinalized();
		CampaignEventDispatcher.Instance.OnCharacterCreationIsOver();
	}

	public void Refresh()
	{
		_handler?.OnRefresh();
	}

	public void OnStageActivated(CharacterCreationStageBase stage)
	{
		_handler?.OnStageCreated(stage);
	}
}
