using System;
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

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000DB RID: 219
	public class SettlementMusiciansCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000A6B RID: 2667 RVA: 0x0004EB84 File Offset: 0x0004CD84
		public override void RegisterEvents()
		{
			CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionStarted));
		}

		// Token: 0x06000A6C RID: 2668 RVA: 0x0004EBA0 File Offset: 0x0004CDA0
		private void OnMissionStarted(IMission mission)
		{
			Mission mission2;
			if ((mission2 = mission as Mission) != null && CampaignMission.Current != null && PlayerEncounter.LocationEncounter != null && PlayerEncounter.LocationEncounter.Settlement != null && CampaignMission.Current.Location != null && !Campaign.Current.IsMainHeroDisguised)
			{
				IEnumerable<MusicianGroup> enumerable = mission2.MissionObjects.FindAllWithType<MusicianGroup>();
				Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
				foreach (MusicianGroup musicianGroup in enumerable)
				{
					List<SettlementMusicData> playList = this.CreateRandomPlayList(settlement);
					musicianGroup.SetPlayList(playList);
				}
			}
		}

		// Token: 0x06000A6D RID: 2669 RVA: 0x0004EC40 File Offset: 0x0004CE40
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
			List<CultureObject> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<CultureObject>();
			Town town = settlement.Town;
			float num;
			if (town == null)
			{
				Village village = settlement.Village;
				num = ((village != null) ? village.Bound.Town.Loyalty : 100f);
			}
			else
			{
				num = town.Loyalty;
			}
			float num2 = num * 0.01f;
			float num3 = 0f;
			using (List<CultureObject>.Enumerator enumerator = objectTypeList.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CultureObject c = enumerator.Current;
					dictionary.Add(c, 0f);
					float num4 = Kingdom.All.Sum(delegate(Kingdom k)
					{
						if (c != k.Culture)
						{
							return 0f;
						}
						return k.CurrentTotalStrength;
					});
					if (num4 > num3)
					{
						num3 = num4;
					}
				}
			}
			foreach (Kingdom kingdom in Kingdom.All)
			{
				float num5 = (Campaign.MapDiagonal - Campaign.Current.Models.MapDistanceModel.GetDistance(kingdom.FactionMidSettlement, settlement.MapFaction.FactionMidSettlement, false, false, MobileParty.NavigationType.All)) / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(MobileParty.NavigationType.All);
				float num6 = num5 * num5 * num5 * 2f;
				num6 += (settlement.MapFaction.IsAtWarWith(kingdom) ? 1f : 2f) * num2;
				dictionary[kingdom.Culture] = MathF.Max(dictionary[kingdom.Culture], num6);
			}
			Dictionary<CultureObject, float> dictionary2;
			CultureObject culture;
			foreach (Kingdom kingdom2 in Kingdom.All)
			{
				dictionary2 = dictionary;
				culture = kingdom2.Culture;
				dictionary2[culture] += kingdom2.CurrentTotalStrength / num3 * 0.5f;
			}
			foreach (Town town2 in Town.AllTowns)
			{
				float num7 = (Campaign.MapDiagonal - Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, town2.Settlement, false, false, (settlement.HasPort && town2.Settlement.HasPort) ? MobileParty.NavigationType.All : MobileParty.NavigationType.Default)) / Campaign.MapDiagonal;
				float num8 = num7 * num7 * num7;
				num8 *= MathF.Min(town2.Prosperity, 5000f) * 0.0002f;
				dictionary2 = dictionary;
				culture = town2.Culture;
				dictionary2[culture] += num8;
			}
			dictionary2 = dictionary;
			culture = settlement.Culture;
			dictionary2[culture] += 10f;
			dictionary2 = dictionary;
			culture = settlement.MapFaction.Culture;
			dictionary2[culture] += num2 * 5f;
			List<SettlementMusicData> list = (from x in MBObjectManager.Instance.GetObjectTypeList<SettlementMusicData>()
				where listOfLocationTags.Contains(x.LocationId)
				select x).ToList<SettlementMusicData>();
			KeyValuePair<CultureObject, float> maxWeightedCulture = dictionary.MaxBy((KeyValuePair<CultureObject, float> x) => x.Value);
			float num9 = (float)list.Count((SettlementMusicData x) => x.Culture == maxWeightedCulture.Key) / maxWeightedCulture.Value;
			List<SettlementMusicData> list2 = new List<SettlementMusicData>();
			foreach (KeyValuePair<CultureObject, float> keyValuePair in dictionary)
			{
				int num10 = MBRandom.RoundRandomized(num9 * keyValuePair.Value);
				if (num10 > 0)
				{
					this.PopulatePlayList(list2, list, keyValuePair.Key, num10);
				}
			}
			if (list2.IsEmpty<SettlementMusicData>())
			{
				list2 = list;
			}
			list2.Shuffle<SettlementMusicData>();
			return list2;
		}

		// Token: 0x06000A6E RID: 2670 RVA: 0x0004F0E4 File Offset: 0x0004D2E4
		private void PopulatePlayList(List<SettlementMusicData> playList, List<SettlementMusicData> settlementMusicDatas, CultureObject culture, int count)
		{
			List<SettlementMusicData> list = (from x in settlementMusicDatas
				where x.Culture == culture
				select x).ToList<SettlementMusicData>();
			list.Shuffle<SettlementMusicData>();
			int num = 0;
			while (num < count && num < list.Count)
			{
				playList.Add(list[num]);
				num++;
			}
		}

		// Token: 0x06000A6F RID: 2671 RVA: 0x0004F13F File Offset: 0x0004D33F
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
