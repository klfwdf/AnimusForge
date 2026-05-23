using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000126 RID: 294
	public class DefaultMapTrackModel : MapTrackModel
	{
		// Token: 0x17000670 RID: 1648
		// (get) Token: 0x06001851 RID: 6225 RVA: 0x0007480C File Offset: 0x00072A0C
		public override float MaxTrackLife
		{
			get
			{
				return 28f;
			}
		}

		// Token: 0x06001852 RID: 6226 RVA: 0x00074814 File Offset: 0x00072A14
		public override float GetMaxTrackSpottingDistanceForMainParty()
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, false, null);
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingRadius, MobileParty.MainParty, ref explainedNumber);
			if (!MobileParty.MainParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Ranger, MobileParty.MainParty, true, ref explainedNumber, false);
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001853 RID: 6227 RVA: 0x00074868 File Offset: 0x00072A68
		public override bool CanPartyLeaveTrack(MobileParty mobileParty)
		{
			return mobileParty.SiegeEvent == null && mobileParty.MapEvent == null && !mobileParty.IsCurrentlyAtSea && !mobileParty.IsGarrison && !mobileParty.IsMilitia && !mobileParty.IsBanditBossParty && !mobileParty.IsMainParty && mobileParty.AttachedTo == null;
		}

		// Token: 0x06001854 RID: 6228 RVA: 0x000748B8 File Offset: 0x00072AB8
		public override int GetTrackLife(MobileParty mobileParty)
		{
			bool flag = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace) == TerrainType.Snow;
			int num = mobileParty.MemberRoster.TotalManCount + mobileParty.PrisonRoster.TotalManCount;
			float num2 = MathF.Min(1f, (0.5f * MBRandom.RandomFloat + 0.5f + (float)num * 0.007f) / 2f) * (flag ? 0.5f : 1f);
			if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.Tracker, false) && !mobileParty.IsCurrentlyAtSea)
			{
				num2 = MathF.Min(1f, num2 * (1f + DefaultPerks.Scouting.Tracker.PrimaryBonus));
			}
			return MathF.Round(Campaign.Current.Models.MapTrackModel.MaxTrackLife * num2);
		}

		// Token: 0x06001855 RID: 6229 RVA: 0x00074984 File Offset: 0x00072B84
		public override float GetTrackDetectionDifficultyForMainParty(Track track, float trackSpottingDistance)
		{
			int size = track.Size;
			float elapsedHoursUntilNow = track.CreationTime.ElapsedHoursUntilNow;
			float num = (track.Position.ToVec2() - MobileParty.MainParty.Position.ToVec2()).Length / trackSpottingDistance;
			float num2 = -75f + elapsedHoursUntilNow / this.MaxTrackLife * 100f + num * 100f + MathF.Max(0f, 100f - (float)size) * (CampaignTime.Now.IsNightTime ? 10f : 1f);
			if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.Ranger, true) && !MobileParty.MainParty.IsCurrentlyAtSea)
			{
				num2 -= num2 * DefaultPerks.Scouting.Ranger.SecondaryBonus;
			}
			return num2;
		}

		// Token: 0x06001856 RID: 6230 RVA: 0x00074A50 File Offset: 0x00072C50
		public override float GetSkillFromTrackDetected(Track track)
		{
			float num = 0.2f * (1f + track.CreationTime.ElapsedHoursUntilNow) * (1f + 0.02f * MathF.Max(0f, 100f - (float)track.NumberOfAllMembers));
			if (track.IsEnemy)
			{
				num *= ((track.PartyType == Track.PartyTypeEnum.Lord) ? 10f : ((track.PartyType == Track.PartyTypeEnum.Bandit) ? 4f : ((track.PartyType == Track.PartyTypeEnum.Caravan) ? 3f : 2f)));
			}
			return num;
		}

		// Token: 0x06001857 RID: 6231 RVA: 0x00074ADC File Offset: 0x00072CDC
		public override float GetSkipTrackChance(MobileParty mobileParty)
		{
			float num = 0.5f;
			float num2 = (float)(mobileParty.MemberRoster.TotalManCount + mobileParty.PrisonRoster.TotalManCount);
			return MathF.Max(num - num2 * 0.01f, 0f);
		}

		// Token: 0x06001858 RID: 6232 RVA: 0x00074B1C File Offset: 0x00072D1C
		public override TextObject TrackTitle(Track track)
		{
			if (track.IsPointer)
			{
				return track.PartyName;
			}
			Hero effectiveScout = MobileParty.MainParty.EffectiveScout;
			if (effectiveScout == null || effectiveScout.GetSkillValue(DefaultSkills.Scouting) <= 270)
			{
				return DefaultMapTrackModel._defaultTrackTitle;
			}
			return track.PartyName;
		}

		// Token: 0x06001859 RID: 6233 RVA: 0x00074B68 File Offset: 0x00072D68
		private string UncertainifyNumber(float num, float baseNum, int skillLevel)
		{
			float num2 = baseNum * MathF.Max(0f, 1f - (float)(skillLevel / 30) * 0.1f);
			float num3 = num - num % num2;
			float num4 = num3 + num2;
			if (num2 < 0.0001f)
			{
				return num.ToString();
			}
			return num3.ToString("0.0") + "-" + num4.ToString("0.0");
		}

		// Token: 0x0600185A RID: 6234 RVA: 0x00074BD0 File Offset: 0x00072DD0
		private string UncertainifyNumber(int num, int baseNum, int skillLevel)
		{
			int num2 = MathF.Round((float)baseNum * MathF.Max(0f, 1f - (float)(skillLevel / 30) * 0.1f));
			if (num2 <= 1)
			{
				return num.ToString();
			}
			int num3 = num - num % num2;
			int num4 = num3 + num2;
			if (num3 == 0)
			{
				num3 = 1;
			}
			if (num3 >= num4)
			{
				return num.ToString();
			}
			return num3.ToString() + "-" + num4.ToString();
		}

		// Token: 0x0600185B RID: 6235 RVA: 0x00074C40 File Offset: 0x00072E40
		public override IEnumerable<ValueTuple<TextObject, string>> GetTrackDescription(Track track)
		{
			List<ValueTuple<TextObject, string>> list = new List<ValueTuple<TextObject, string>>();
			if (!track.IsPointer && track.IsAlive)
			{
				Hero effectiveScout = MobileParty.MainParty.EffectiveScout;
				int skillLevel = ((effectiveScout != null) ? effectiveScout.GetSkillValue(DefaultSkills.Scouting) : 0);
				ExplainedNumber explainedNumber = default(ExplainedNumber);
				SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingTrackInformation, MobileParty.MainParty, ref explainedNumber);
				int num = MathF.Floor(explainedNumber.ResultNumber);
				if (num >= 1)
				{
					int num2 = track.NumberOfAllMembers + track.NumberOfPrisoners;
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=rmydcPP3}Party Size:", null), this.UncertainifyNumber(num2, 10, skillLevel)));
				}
				if (num >= 2)
				{
					TextObject textObject = new TextObject("{=Lak0x7Sa}{HOURS} {?HOURS==1}hour{?}hours{\\?}", null);
					int variable = MathF.Ceiling(track.CreationTime.ElapsedHoursUntilNow);
					textObject.SetTextVariable("HOURS", variable);
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=0aU9dtvV}Time:", null), textObject.ToString()));
				}
				if (num >= 3)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=PThYJE2U}Party Speed:", null), this.UncertainifyNumber(MathF.Round(track.Speed, 2), 1f, skillLevel)));
				}
				if (num >= 4)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=ZULIWupm}Mounted Troops:", null), this.UncertainifyNumber(track.NumberOfMenWithHorse, 10, skillLevel)));
				}
				if (num >= 5 && num < 10)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=1pdBdqKn}Party Type:", null), GameTexts.FindText("str_party_type", track.PartyType.ToString()).ToString()));
				}
				if (num >= 6)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=pHrxeTdc}Prisoners:", null), this.UncertainifyNumber(track.NumberOfPrisoners, 10, skillLevel)));
				}
				if (num >= 7)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=aa1yFm6q}Pack Animals:", null), this.UncertainifyNumber(track.NumberOfPackAnimals, 10, skillLevel)));
				}
				if (num >= 8)
				{
					TextObject textObject2 = (track.IsEnemy ? GameTexts.FindText("str_yes", null) : GameTexts.FindText("str_no", null));
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=6REUNz1g}Enemy Party:", null), textObject2.ToString()));
				}
				if (num >= 9)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=dicpCcb2}Party Culture:", null), track.Culture.Name.ToString()));
				}
				if (num >= 10)
				{
					list.Add(new ValueTuple<TextObject, string>(new TextObject("{=BVIm1HPw}Party Name:", null), track.PartyName.ToString()));
				}
			}
			return list;
		}

		// Token: 0x0600185C RID: 6236 RVA: 0x00074EB0 File Offset: 0x000730B0
		public override uint GetTrackColor(Track track)
		{
			if (track.IsPointer)
			{
				return new Vec3(1f, 1f, 1f, -1f).ToARGB;
			}
			Vec3 v = new Vec3(0.6f, 0.95f, 0.2f, -1f);
			Vec3 v2 = new Vec3(0.45f, 0.55f, 0.2f, -1f);
			Vec3 v3 = new Vec3(0.15f, 0.25f, 0.4f, -1f);
			Vec3 vec = Vec3.Zero;
			if (track.IsEnemy)
			{
				Hero effectiveScout = MobileParty.MainParty.EffectiveScout;
				if (effectiveScout != null && effectiveScout.GetSkillValue(DefaultSkills.Scouting) > 240)
				{
					v = new Vec3(0.99f, 0.5f, 0.1f, -1f);
					v2 = new Vec3(0.75f, 0.4f, 0.3f, -1f);
					v3 = new Vec3(0.5f, 0.1f, 0.4f, -1f);
				}
			}
			float num = MathF.Min(track.CreationTime.ElapsedHoursUntilNow / Campaign.Current.Models.MapTrackModel.MaxTrackLife, 1f);
			if (num < 0.35f)
			{
				num /= 0.35f;
				vec = num * v2 + (1f - num) * v;
			}
			else
			{
				num -= 0.35f;
				num /= 0.65f;
				vec = num * v3 + (1f - num) * v2;
			}
			return vec.ToARGB;
		}

		// Token: 0x0600185D RID: 6237 RVA: 0x00075050 File Offset: 0x00073250
		public override float GetTrackScale(Track track)
		{
			if (track.IsPointer)
			{
				return 1f;
			}
			float b = 0.1f + 0.001f * (float)(track.NumberOfAllMembers + track.NumberOfPrisoners);
			return MathF.Min(1f, b);
		}

		// Token: 0x040007E3 RID: 2019
		private const float MinimumTrackSize = 0.1f;

		// Token: 0x040007E4 RID: 2020
		private const float MaximumTrackSize = 1f;

		// Token: 0x040007E5 RID: 2021
		private static TextObject _defaultTrackTitle = new TextObject("{=maptrack}Track", null);
	}
}
