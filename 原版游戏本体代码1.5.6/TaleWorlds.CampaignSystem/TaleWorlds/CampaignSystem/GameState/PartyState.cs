using System;
using Helpers;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState;

public class PartyState : PlayerGameState
{
	public override bool IsMenuState => true;

	public PartyScreenLogic PartyScreenLogic { get; set; }

	public PartyScreenHelper.PartyScreenMode PartyScreenMode { get; set; }

	public bool IsDonating { get; set; }

	public IPartyScreenLogicHandler Handler { get; set; }

	public void RequestUserInput(string text, Action accept, Action cancel)
	{
		if (Handler != null)
		{
			Handler.RequestUserInput(text, accept, cancel);
		}
	}
}
