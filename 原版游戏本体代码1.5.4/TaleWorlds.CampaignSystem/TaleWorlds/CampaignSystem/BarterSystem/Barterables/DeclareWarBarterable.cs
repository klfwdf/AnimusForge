using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables;

public class DeclareWarBarterable : Barterable
{
	public override string StringID => "declare_war_barterable";

	public IFaction DeclaringFaction { get; private set; }

	public IFaction OtherFaction { get; private set; }

	public override TextObject Name
	{
		get
		{
			TextObject textObject = new TextObject("{=GZwNgIon}Declare war against {OTHER_FACTION}");
			textObject.SetTextVariable("OTHER_FACTION", OtherFaction.Name);
			return textObject;
		}
	}

	public DeclareWarBarterable(IFaction declaringFaction, IFaction otherFaction)
		: base(declaringFaction.Leader, null)
	{
		DeclaringFaction = declaringFaction;
		OtherFaction = otherFaction;
	}

	public override void Apply()
	{
		DeclareWarAction.ApplyByDefault(base.OriginalOwner.MapFaction, OtherFaction.MapFaction);
	}

	public override int GetUnitValueForFaction(IFaction faction)
	{
		int result = 0;
		Clan evaluatingClan = ((faction is Clan) ? ((Clan)faction) : ((Kingdom)faction).RulingClan);
		TextObject reason;
		if (faction.MapFaction == base.OriginalOwner.MapFaction)
		{
			result = (int)Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(base.OriginalOwner.MapFaction, OtherFaction.MapFaction, evaluatingClan, out reason);
		}
		else if (faction.MapFaction == OtherFaction.MapFaction)
		{
			result = (int)Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(OtherFaction.MapFaction, base.OriginalOwner.MapFaction, evaluatingClan, out reason);
		}
		return result;
	}

	public override ImageIdentifier GetVisualIdentifier()
	{
		return null;
	}
}
