using System.Collections.Generic;
using System.Linq;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.CampaignBehaviors;

public class SettlementMusiciansCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
	}

	private void OnMissionStarted(IMission mission)
	{
		if (!(mission is Mission mission2) || CampaignMission.Current == null || PlayerEncounter.LocationEncounter == null || PlayerEncounter.LocationEncounter.Settlement == null || CampaignMission.Current.Location == null || (Campaign.Current.IsMainHeroDisguised && !(CampaignMission.Current.Location.StringId != "center")))
		{
			return;
		}
		IEnumerable<MusicianGroup> enumerable = mission2.MissionObjects.FindAllWithType<MusicianGroup>();
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		foreach (MusicianGroup item in enumerable)
		{
			List<SettlementMusicData> playList = CreateRandomPlayList(settlement);
			item.SetPlayList(playList);
		}
	}

	private List<SettlementMusicData> CreateRandomPlayList(Settlement settlement)
	{
		List<string> listOfLocationTags = new List<string>();
		string stringId = CampaignMission.Current.Location.StringId;
		if (stringId == "center")
		{
			listOfLocationTags.Add("lordshall");
			listOfLocationTags.Add("tavern");
		}
		else
		{
			listOfLocationTags.Add(stringId);
			if (stringId == "port")
			{
				listOfLocationTags.Add("tavern");
			}
		}
		Dictionary<CultureObject, float> dictionary = new Dictionary<CultureObject, float>();
		MBReadOnlyList<CultureObject> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<CultureObject>();
		float num = (settlement.Town?.Loyalty ?? settlement.Village?.Bound.Town.Loyalty ?? 100f) * 0.01f;
		float num2 = 0f;
		foreach (CultureObject c in objectTypeList)
		{
			dictionary.Add(c, 0f);
			float num3 = Kingdom.All.Sum((Kingdom k) => (c != k.Culture) ? 0f : k.CurrentTotalStrength);
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		foreach (Kingdom item in Kingdom.All)
		{
			float num4 = (Campaign.MapDiagonal - Campaign.Current.Models.MapDistanceModel.GetDistance(item.FactionMidSettlement, settlement.MapFaction.FactionMidSettlement, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.All)) / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType.All);
			float num5 = num4 * num4 * num4 * 2f;
			num5 += (settlement.MapFaction.IsAtWarWith(item) ? 1f : 2f) * num;
			dictionary[item.Culture] = MathF.Max(dictionary[item.Culture], num5);
		}
		foreach (Kingdom item2 in Kingdom.All)
		{
			dictionary[item2.Culture] += item2.CurrentTotalStrength / num2 * 0.5f;
		}
		foreach (Town allTown in Town.AllTowns)
		{
			float num6 = (Campaign.MapDiagonal - Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, allTown.Settlement, isFromPort: false, isTargetingPort: false, (!settlement.HasPort || !allTown.Settlement.HasPort) ? MobileParty.NavigationType.Default : MobileParty.NavigationType.All)) / Campaign.MapDiagonal;
			float num7 = num6 * num6 * num6;
			num7 *= MathF.Min(allTown.Prosperity, 5000f) * 0.0002f;
			dictionary[allTown.Culture] += num7;
		}
		dictionary[settlement.Culture] += 10f;
		dictionary[settlement.MapFaction.Culture] += num * 5f;
		List<SettlementMusicData> list = (from x in MBObjectManager.Instance.GetObjectTypeList<SettlementMusicData>()
			where listOfLocationTags.Contains(x.LocationId)
			select x).ToList();
		KeyValuePair<CultureObject, float> maxWeightedCulture = TaleWorlds.Core.Extensions.MaxBy(dictionary, (KeyValuePair<CultureObject, float> x) => x.Value);
		float num8 = (float)list.Count((SettlementMusicData x) => x.Culture == maxWeightedCulture.Key) / maxWeightedCulture.Value;
		List<SettlementMusicData> list2 = new List<SettlementMusicData>();
		foreach (KeyValuePair<CultureObject, float> item3 in dictionary)
		{
			int num9 = MBRandom.RoundRandomized(num8 * item3.Value);
			if (num9 > 0)
			{
				PopulatePlayList(list2, list, item3.Key, num9);
			}
		}
		if (list2.IsEmpty())
		{
			list2 = list;
		}
		list2.Shuffle();
		return list2;
	}

	private void PopulatePlayList(List<SettlementMusicData> playList, List<SettlementMusicData> settlementMusicDatas, CultureObject culture, int count)
	{
		List<SettlementMusicData> list = settlementMusicDatas.Where((SettlementMusicData x) => x.Culture == culture).ToList();
		list.Shuffle();
		for (int num = 0; num < count && num < list.Count; num++)
		{
			playList.Add(list[num]);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
