using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMapTrackModel : MapTrackModel
{
	private const float MinimumTrackSize = 0.1f;

	private const float MaximumTrackSize = 1f;

	private static TextObject _defaultTrackTitle = new TextObject("{=maptrack}Track");

	public override float MaxTrackLife => 28f;

	public override float GetMaxTrackSpottingDistanceForMainParty()
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions: false, null);
		SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingRadius, MobileParty.MainParty, ref explainedNumber);
		if (!MobileParty.MainParty.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Ranger, MobileParty.MainParty, isPrimaryBonus: true, ref explainedNumber);
		}
		return explainedNumber.ResultNumber;
	}

	public override bool CanPartyLeaveTrack(MobileParty mobileParty)
	{
		if (mobileParty.SiegeEvent == null && mobileParty.MapEvent == null && mobileParty.IsActive && !mobileParty.IsCurrentlyAtSea && !mobileParty.IsGarrison && !mobileParty.IsMilitia && !mobileParty.IsBanditBossParty && !mobileParty.IsMainParty)
		{
			return mobileParty.AttachedTo == null;
		}
		return false;
	}

	public override int GetTrackLife(MobileParty mobileParty)
	{
		bool flag = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace) == TerrainType.Snow;
		int num = mobileParty.MemberRoster.TotalManCount + mobileParty.PrisonRoster.TotalManCount;
		float num2 = MathF.Min(1f, (0.5f * MBRandom.RandomFloat + 0.5f + (float)num * 0.007f) / 2f) * (flag ? 0.5f : 1f);
		if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.Tracker) && !mobileParty.IsCurrentlyAtSea)
		{
			num2 = MathF.Min(1f, num2 * (1f + DefaultPerks.Scouting.Tracker.PrimaryBonus));
		}
		return MathF.Round(Campaign.Current.Models.MapTrackModel.MaxTrackLife * num2);
	}

	public override float GetTrackDetectionDifficultyForMainParty(Track track, float trackSpottingDistance)
	{
		int size = track.Size;
		float elapsedHoursUntilNow = track.CreationTime.ElapsedHoursUntilNow;
		float num = (track.Position.ToVec2() - MobileParty.MainParty.Position.ToVec2()).Length / trackSpottingDistance;
		float num2 = -75f + elapsedHoursUntilNow / MaxTrackLife * 100f + num * 100f + MathF.Max(0f, 100f - (float)size) * (CampaignTime.Now.IsNightTime ? 10f : 1f);
		if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.Ranger, checkSecondaryRole: true) && !MobileParty.MainParty.IsCurrentlyAtSea)
		{
			num2 -= num2 * DefaultPerks.Scouting.Ranger.SecondaryBonus;
		}
		return num2;
	}

	public override float GetSkillFromTrackDetected(Track track)
	{
		float num = 0.2f * (1f + track.CreationTime.ElapsedHoursUntilNow) * (1f + 0.02f * MathF.Max(0f, 100f - (float)track.NumberOfAllMembers));
		if (track.IsEnemy)
		{
			num *= ((track.PartyType == Track.PartyTypeEnum.Lord) ? 10f : ((track.PartyType == Track.PartyTypeEnum.Bandit) ? 4f : ((track.PartyType == Track.PartyTypeEnum.Caravan) ? 3f : 2f)));
		}
		return num;
	}

	public override float GetSkipTrackChance(MobileParty mobileParty)
	{
		float num = mobileParty.MemberRoster.TotalManCount + mobileParty.PrisonRoster.TotalManCount;
		return MathF.Max(0.5f - num * 0.01f, 0f);
	}

	public override TextObject TrackTitle(Track track)
	{
		if (!track.IsPointer)
		{
			Hero effectiveScout = MobileParty.MainParty.EffectiveScout;
			if (effectiveScout == null || effectiveScout.GetSkillValue(DefaultSkills.Scouting) <= 270)
			{
				return _defaultTrackTitle;
			}
			return track.PartyName;
		}
		return track.PartyName;
	}

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
		return num3 + "-" + num4;
	}

	public override IEnumerable<(TextObject, string)> GetTrackDescription(Track track)
	{
		List<(TextObject, string)> list = new List<(TextObject, string)>();
		if (!track.IsPointer && track.IsAlive)
		{
			int skillLevel = MobileParty.MainParty.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
			ExplainedNumber explainedNumber = default(ExplainedNumber);
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingTrackInformation, MobileParty.MainParty, ref explainedNumber);
			int num = MathF.Floor(explainedNumber.ResultNumber);
			if (num >= 1)
			{
				int num2 = track.NumberOfAllMembers + track.NumberOfPrisoners;
				list.Add((new TextObject("{=rmydcPP3}Party Size:"), UncertainifyNumber(num2, 10, skillLevel)));
			}
			if (num >= 2)
			{
				TextObject textObject = new TextObject("{=Lak0x7Sa}{HOURS} {?HOURS==1}hour{?}hours{\\?}");
				int variable = MathF.Ceiling(track.CreationTime.ElapsedHoursUntilNow);
				textObject.SetTextVariable("HOURS", variable);
				list.Add((new TextObject("{=0aU9dtvV}Time:"), textObject.ToString()));
			}
			if (num >= 3)
			{
				list.Add((new TextObject("{=PThYJE2U}Party Speed:"), UncertainifyNumber(MathF.Round(track.Speed, 2), 1f, skillLevel)));
			}
			if (num >= 4)
			{
				list.Add((new TextObject("{=ZULIWupm}Mounted Troops:"), UncertainifyNumber(track.NumberOfMenWithHorse, 10, skillLevel)));
			}
			if (num >= 5 && num < 10)
			{
				list.Add((new TextObject("{=1pdBdqKn}Party Type:"), GameTexts.FindText("str_party_type", track.PartyType.ToString()).ToString()));
			}
			if (num >= 6)
			{
				list.Add((new TextObject("{=pHrxeTdc}Prisoners:"), UncertainifyNumber(track.NumberOfPrisoners, 10, skillLevel)));
			}
			if (num >= 7)
			{
				list.Add((new TextObject("{=aa1yFm6q}Pack Animals:"), UncertainifyNumber(track.NumberOfPackAnimals, 10, skillLevel)));
			}
			if (num >= 8)
			{
				TextObject textObject2 = (track.IsEnemy ? GameTexts.FindText("str_yes") : GameTexts.FindText("str_no"));
				list.Add((new TextObject("{=6REUNz1g}Enemy Party:"), textObject2.ToString()));
			}
			if (num >= 9)
			{
				list.Add((new TextObject("{=dicpCcb2}Party Culture:"), track.Culture.Name.ToString()));
			}
			if (num >= 10)
			{
				list.Add((new TextObject("{=BVIm1HPw}Party Name:"), track.PartyName.ToString()));
			}
		}
		return list;
	}

	public override uint GetTrackColor(Track track)
	{
		if (track.IsPointer)
		{
			return new Vec3(1f, 1f, 1f).ToARGB;
		}
		Vec3 vec = new Vec3(0.6f, 0.95f, 0.2f);
		Vec3 vec2 = new Vec3(0.45f, 0.55f, 0.2f);
		Vec3 vec3 = new Vec3(0.15f, 0.25f, 0.4f);
		Vec3 zero = Vec3.Zero;
		if (track.IsEnemy)
		{
			Hero effectiveScout = MobileParty.MainParty.EffectiveScout;
			if (effectiveScout != null && effectiveScout.GetSkillValue(DefaultSkills.Scouting) > 240)
			{
				vec = new Vec3(0.99f, 0.5f, 0.1f);
				vec2 = new Vec3(0.75f, 0.4f, 0.3f);
				vec3 = new Vec3(0.5f, 0.1f, 0.4f);
			}
		}
		float num = MathF.Min(track.CreationTime.ElapsedHoursUntilNow / Campaign.Current.Models.MapTrackModel.MaxTrackLife, 1f);
		if (num < 0.35f)
		{
			num /= 0.35f;
			zero = num * vec2 + (1f - num) * vec;
		}
		else
		{
			num -= 0.35f;
			num /= 0.65f;
			zero = num * vec3 + (1f - num) * vec2;
		}
		return zero.ToARGB;
	}

	public override float GetTrackScale(Track track)
	{
		if (track.IsPointer)
		{
			return 1f;
		}
		float b = 0.1f + 0.001f * (float)(track.NumberOfAllMembers + track.NumberOfPrisoners);
		return MathF.Min(1f, b);
	}
}
