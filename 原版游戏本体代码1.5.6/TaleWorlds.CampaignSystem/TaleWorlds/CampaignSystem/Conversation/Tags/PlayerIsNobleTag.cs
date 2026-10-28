using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public class PlayerIsNobleTag : ConversationTag
{
	public const string Id = "PlayerIsNobleTag";

	public override string StringId => "PlayerIsNobleTag";

	public override bool IsApplicableTo(CharacterObject character)
	{
		return Settlement.All.Any((Settlement x) => x.OwnerClan == Hero.MainHero.Clan);
	}
}
