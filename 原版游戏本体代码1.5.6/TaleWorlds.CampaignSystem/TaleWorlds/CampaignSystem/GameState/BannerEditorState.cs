using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class BannerEditorState : TaleWorlds.Core.GameState
{
	private IBannerEditorStateHandler _handler;

	private Action _onEndAction;

	public override bool IsMenuState => true;

	public IBannerEditorStateHandler Handler
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

	public BannerEditorState()
	{
	}

	public BannerEditorState(Action endAction)
	{
		_onEndAction = endAction;
	}

	public Clan GetClan()
	{
		return Clan.PlayerClan;
	}

	public CharacterObject GetCharacter()
	{
		return CharacterObject.PlayerCharacter;
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_onEndAction?.Invoke();
	}
}
