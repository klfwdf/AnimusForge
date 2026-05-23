using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultInventoryCapacityModel : InventoryCapacityModel
{
	private const int _itemAverageWeight = 10;

	private const float TroopsFactor = 2f;

	private const float SpareMountsFactor = 2f;

	private const float PackAnimalsFactor = 10f;

	private static readonly TextObject _textTroops = new TextObject("{=5k4dxUEJ}Troops");

	private static readonly TextObject _textBase = new TextObject("{=basevalue}Base");

	private static readonly TextObject _textSpareMounts = new TextObject("{=rCiKbsyW}Spare Mounts");

	private static readonly TextObject _textPackAnimals = new TextObject("{=dI1AOyqh}Pack Animals");

	private static readonly TextObject _textMountsAndPackAnimals = new TextObject("{=Sb1MKbvP}Mounts and Pack Animals");

	private static readonly TextObject _textLiveStocksAnimals = new TextObject("{=KxUgSAKi}Live Stock Animals");

	private static readonly TextObject _textItems = new TextObject("{=U7er3V9s}Items");

	public override int GetItemAverageWeight()
	{
		return 10;
	}

	public override float GetItemEffectiveWeight(EquipmentElement equipmentElement, MobileParty mobileParty, bool isCurrentlyAtSea, out TextObject description)
	{
		if (equipmentElement.Item.HasHorseComponent)
		{
			description = _textMountsAndPackAnimals;
			return 0f;
		}
		description = _textItems;
		return equipmentElement.GetEquipmentElementWeight();
	}

	public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false, int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		PartyBase party = mobileParty.Party;
		int num = party.NumberOfMounts;
		int num2 = party.NumberOfHealthyMembers;
		int num3 = party.NumberOfPackAnimals;
		if (includeFollowers)
		{
			foreach (MobileParty attachedParty in mobileParty.AttachedParties)
			{
				num += attachedParty.Party.NumberOfMounts;
				num2 += attachedParty.Party.NumberOfHealthyMembers;
				num3 += attachedParty.Party.NumberOfPackAnimals;
			}
		}
		if (mobileParty.HasPerk(DefaultPerks.Steward.ArenicosHorses) && !isCurrentlyAtSea)
		{
			num2 += MathF.Round((float)num2 * DefaultPerks.Steward.ArenicosHorses.PrimaryBonus);
		}
		if (!mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Steward.ForcedLabor))
		{
			num2 += party.PrisonRoster.TotalHealthyCount;
		}
		result.Add(10f, _textBase);
		result.Add((float)num2 * 2f * 10f, _textTroops);
		if (!isCurrentlyAtSea)
		{
			result.Add((float)num * 2f * 10f, _textSpareMounts);
			ExplainedNumber explainedNumber = new ExplainedNumber((float)num3 * 10f * 10f);
			if (mobileParty.HasPerk(DefaultPerks.Scouting.BeastWhisperer, checkSecondaryRole: true))
			{
				explainedNumber.AddFactor(DefaultPerks.Scouting.BeastWhisperer.SecondaryBonus, DefaultPerks.Scouting.BeastWhisperer.Name);
			}
			if (mobileParty.HasPerk(DefaultPerks.Riding.DeeperSacks))
			{
				explainedNumber.AddFactor(DefaultPerks.Riding.DeeperSacks.PrimaryBonus, DefaultPerks.Riding.DeeperSacks.Name);
			}
			if (mobileParty.HasPerk(DefaultPerks.Steward.ArenicosMules))
			{
				explainedNumber.AddFactor(DefaultPerks.Steward.ArenicosMules.PrimaryBonus, DefaultPerks.Steward.ArenicosMules.Name);
			}
			result.Add(explainedNumber.ResultNumber, _textPackAnimals);
			if (mobileParty.HasPerk(DefaultPerks.Trade.CaravanMaster))
			{
				result.AddFactor(DefaultPerks.Trade.CaravanMaster.PrimaryBonus, DefaultPerks.Trade.CaravanMaster.Name);
			}
		}
		result.LimitMin(10f);
		return result;
	}

	public override ExplainedNumber CalculateTotalWeightCarried(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, _textItems);
		InventoryCapacityModel inventoryCapacityModel = Campaign.Current.Models.InventoryCapacityModel;
		foreach (ItemRosterElement item in mobileParty.ItemRoster)
		{
			TextObject description;
			float itemEffectiveWeight = inventoryCapacityModel.GetItemEffectiveWeight(item.EquipmentElement, mobileParty, isCurrentlyAtSea, out description);
			result.Add(itemEffectiveWeight * (float)item.Amount, description);
		}
		return result;
	}
}
