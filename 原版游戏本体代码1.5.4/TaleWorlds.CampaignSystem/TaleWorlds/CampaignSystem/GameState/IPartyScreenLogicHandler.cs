using System;

namespace TaleWorlds.CampaignSystem.GameState;

public interface IPartyScreenLogicHandler
{
	void RequestUserInput(string text, Action accept, Action cancel);
}
