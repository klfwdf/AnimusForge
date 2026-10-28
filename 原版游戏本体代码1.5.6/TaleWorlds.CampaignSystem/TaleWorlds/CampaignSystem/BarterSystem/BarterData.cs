using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.BarterSystem;

public class BarterData
{
	public readonly Hero OffererHero;

	public readonly Hero OtherHero;

	public readonly PartyBase OffererParty;

	public readonly PartyBase OtherParty;

	private List<Barterable> _barterables;

	private List<BarterGroup> _barterGroups;

	public readonly BarterManager.BarterContextInitializer ContextInitializer;

	public readonly int PersuasionCostReduction;

	public IFaction OffererMapFaction => OffererHero?.MapFaction ?? OffererParty.MapFaction;

	public IFaction OtherMapFaction => OtherHero?.MapFaction ?? OtherParty.MapFaction;

	public bool IsAiBarter { get; }

	public BarterData(Hero offerer, Hero other, PartyBase offererParty, PartyBase otherParty, BarterManager.BarterContextInitializer contextInitializer = null, int persuasionCostReduction = 0, bool isAiBarter = false)
	{
		OffererParty = offererParty;
		OtherParty = otherParty;
		OffererHero = offerer;
		OtherHero = other;
		ContextInitializer = contextInitializer;
		PersuasionCostReduction = persuasionCostReduction;
		_barterables = new List<Barterable>(16);
		_barterGroups = Campaign.Current.Models.DiplomacyModel.GetBarterGroups().ToList();
		IsAiBarter = isAiBarter;
	}

	public void AddBarterable<T>(Barterable barterable, bool isContextDependent = false)
	{
		foreach (BarterGroup barterGroup in _barterGroups)
		{
			if (barterGroup is T)
			{
				barterable.Initialize(barterGroup, isContextDependent);
				_barterables.Add(barterable);
				break;
			}
		}
	}

	public void AddBarterGroup(BarterGroup barterGroup)
	{
		_barterGroups.Add(barterGroup);
	}

	public List<BarterGroup> GetBarterGroups()
	{
		return _barterGroups;
	}

	public List<Barterable> GetBarterables()
	{
		return _barterables;
	}

	public BarterGroup GetBarterGroup<T>()
	{
		IEnumerable<T> source = _barterGroups.OfType<T>();
		if (source.IsEmpty())
		{
			return null;
		}
		return source.First() as BarterGroup;
	}

	public List<Barterable> GetOfferedBarterables()
	{
		return (from barterable in GetBarterables()
			where barterable.IsOffered
			select barterable).ToList();
	}
}
