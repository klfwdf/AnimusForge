using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.BarterSystem.Barterables;

public class NoAttackBarterable : Barterable
{
	private readonly IFaction _otherFaction;

	private readonly CampaignTime _duration;

	private readonly Hero _otherHero;

	private readonly PartyBase _otherParty;

	public override string StringID => "no_attack_barterable";

	public override TextObject Name
	{
		get
		{
			TextObject textObject = new TextObject("{=Y3lGJT8H}{PARTY} won't attack {FACTION} for {DURATION} {?DURATION>1}days{?}day{\\?}.");
			textObject.SetTextVariable("PARTY", base.OriginalParty.Name);
			textObject.SetTextVariable("FACTION", _otherFaction.Name);
			textObject.SetTextVariable("DURATION", _duration.ToDays.ToString());
			return textObject;
		}
	}

	public NoAttackBarterable(Hero originalOwner, Hero otherHero, PartyBase ownerParty, PartyBase otherParty, CampaignTime duration)
		: base(originalOwner, ownerParty)
	{
		_otherFaction = otherParty.MapFaction;
		_duration = duration;
		_otherHero = otherHero;
		_otherParty = otherParty;
	}

	public override void Apply()
	{
		if (base.OriginalParty == MobileParty.MainParty.Party)
		{
			if (_otherFaction.NotAttackableByPlayerUntilTime.IsPast)
			{
				_otherFaction.NotAttackableByPlayerUntilTime = CampaignTime.Now;
			}
			_otherFaction.NotAttackableByPlayerUntilTime += _duration;
		}
	}

	public override int GetUnitValueForFaction(IFaction faction)
	{
		int result = 0;
		float militaryValueOfParty = Campaign.Current.Models.ValuationModel.GetMilitaryValueOfParty(base.OriginalParty.MobileParty);
		if (faction.MapFaction == _otherFaction.MapFaction && faction.MapFaction.IsAtWarWith(base.OriginalParty.MapFaction))
		{
			result = (int)(militaryValueOfParty * 0.1f);
		}
		else if (faction.MapFaction == base.OriginalParty.MapFaction)
		{
			result = -(int)(militaryValueOfParty * 0.1f);
		}
		return result;
	}

	public override ImageIdentifier GetVisualIdentifier()
	{
		return null;
	}
}
