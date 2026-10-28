using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class InitialChildGenerationCampaignBehavior : CampaignBehaviorBase
{
	private const float FemaleChildrenChance = 0.49f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int index)
	{
		if (index != 0)
		{
			return;
		}
		foreach (Clan clan in Clan.All)
		{
			if (clan.IsBanditFaction || clan.IsMinorFaction || clan.IsEliminated || clan == Clan.PlayerClan)
			{
				continue;
			}
			List<Hero> list = new List<Hero>();
			MBList<Hero> mBList = new MBList<Hero>();
			MBList<Hero> mBList2 = new MBList<Hero>();
			foreach (Hero aliveLord in clan.AliveLords)
			{
				if (aliveLord.IsChild)
				{
					list.Add(aliveLord);
				}
				else if (aliveLord.IsFemale)
				{
					mBList.Add(aliveLord);
				}
				else
				{
					mBList2.Add(aliveLord);
				}
			}
			int num = MathF.Ceiling((float)(mBList2.Count + mBList.Count) / 2f) - list.Count;
			float num2 = 0.49f;
			if (mBList2.Count == 0)
			{
				num2 = -1f;
			}
			for (int i = 0; i < num; i++)
			{
				bool isFemale = MBRandom.RandomFloat <= num2;
				Hero hero = (isFemale ? mBList.GetRandomElement() : mBList2.GetRandomElement());
				if (hero == null)
				{
					MBList<Clan> e = Clan.NonBanditFactions.Where((Clan t) => t != clan && t.Culture == clan.Culture).ToMBList();
					for (int num3 = 0; num3 < 10; num3++)
					{
						hero = e.GetRandomElement().AliveLords.Where((Hero t) => t.IsFemale == isFemale).ToMBList().GetRandomElement();
						if (hero != null)
						{
							break;
						}
					}
				}
				if (hero != null)
				{
					int age = MBRandom.RandomInt(2, 18);
					Hero hero2 = HeroCreator.CreateChild(hero.CharacterObject, clan.HomeSettlement, clan, age);
					hero2.UpdateHomeSettlement();
					hero2.HeroDeveloper.InitializeHeroDeveloper();
					Equipment equipmentForInitialChildrenGeneration = Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentForInitialChildrenGeneration(hero2);
					EquipmentHelper.AssignHeroEquipmentFromEquipment(hero2, equipmentForInitialChildrenGeneration);
					Equipment equipment = new Equipment(Equipment.EquipmentType.Battle);
					equipment.FillFrom(equipmentForInitialChildrenGeneration, useSourceEquipmentType: false);
					EquipmentHelper.AssignHeroEquipmentFromEquipment(hero2, equipment);
				}
				if (num2 <= 0f)
				{
					num2 = 0.49f;
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
