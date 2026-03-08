using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C5 RID: 197
	public class MarriageSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000584 RID: 1412
		// (get) Token: 0x06001418 RID: 5144 RVA: 0x0005D07D File Offset: 0x0005B27D
		public Hero GroomHero { get; }

		// Token: 0x17000585 RID: 1413
		// (get) Token: 0x06001419 RID: 5145 RVA: 0x0005D085 File Offset: 0x0005B285
		public Hero BrideHero { get; }

		// Token: 0x17000586 RID: 1414
		// (get) Token: 0x0600141A RID: 5146 RVA: 0x0005D08D File Offset: 0x0005B28D
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_wedding";
			}
		}

		// Token: 0x17000587 RID: 1415
		// (get) Token: 0x0600141B RID: 5147 RVA: 0x0005D094 File Offset: 0x0005B294
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				Hero hero = ((this.GroomHero == Hero.MainHero) ? this.GroomHero : this.BrideHero);
				Hero hero2 = ((hero == this.GroomHero) ? this.BrideHero : this.GroomHero);
				GameTexts.SetVariable("FIRST_HERO", hero.Name);
				GameTexts.SetVariable("SECOND_HERO", hero2.Name);
				return GameTexts.FindText("str_marriage_notification", null);
			}
		}

		// Token: 0x17000588 RID: 1416
		// (get) Token: 0x0600141C RID: 5148 RVA: 0x0005D12D File Offset: 0x0005B32D
		public override SceneNotificationData.RelevantContextType RelevantContext { get; }

		// Token: 0x0600141D RID: 5149 RVA: 0x0005D138 File Offset: 0x0005B338
		public override Banner[] GetBanners()
		{
			return new Banner[]
			{
				(this.GroomHero.Father != null) ? this.GroomHero.Father.ClanBanner : this.GroomHero.ClanBanner,
				(this.BrideHero.Father != null) ? this.BrideHero.Father.ClanBanner : this.BrideHero.ClanBanner,
				(this.GroomHero.Father != null) ? this.GroomHero.Father.ClanBanner : this.GroomHero.ClanBanner,
				(this.BrideHero.Father != null) ? this.BrideHero.Father.ClanBanner : this.BrideHero.ClanBanner
			};
		}

		// Token: 0x0600141E RID: 5150 RVA: 0x0005D200 File Offset: 0x0005B400
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.GroomHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.GroomHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			Equipment overridenEquipment2;
			if (this.BrideHero.Culture.MarriageBrideEquipmentRoster != null)
			{
				overridenEquipment2 = this.BrideHero.Culture.MarriageBrideEquipmentRoster.DefaultEquipment.Clone(false);
			}
			else
			{
				overridenEquipment2 = MBEquipmentRoster.EmptyEquipment.Clone(false);
				Debug.FailedAssert("Could not find marriage equipment for culture: " + this.BrideHero.Culture.StringId + ".", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\SceneInformationPopupTypes\\MarriageSceneNotificationItem.cs", "GetSceneNotificationCharacters", 61);
			}
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.BrideHero, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("cutscene_monk");
			Equipment overriddenEquipment = @object.Equipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment, false, false);
			list.Add(new SceneNotificationData.SceneNotificationCharacter(@object, overriddenEquipment, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
			List<Hero> audienceMembers = this.GetAudienceMembers(this.BrideHero, this.GroomHero);
			for (int i = 0; i < audienceMembers.Count; i++)
			{
				Hero hero = audienceMembers[i];
				if (hero != null)
				{
					Equipment overridenEquipment3 = hero.CivilianEquipment.Clone(false);
					CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment3, false, false);
					list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(hero, overridenEquipment3, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
				}
				else
				{
					list.Add(new SceneNotificationData.SceneNotificationCharacter(null, null, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
				}
			}
			return list.ToArray();
		}

		// Token: 0x0600141F RID: 5151 RVA: 0x0005D3B7 File Offset: 0x0005B5B7
		public MarriageSceneNotificationItem(Hero groomHero, Hero brideHero, CampaignTime creationTime, SceneNotificationData.RelevantContextType relevantContextType = SceneNotificationData.RelevantContextType.Any)
		{
			this.GroomHero = groomHero;
			this.BrideHero = brideHero;
			this.RelevantContext = relevantContextType;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x06001420 RID: 5152 RVA: 0x0005D3DC File Offset: 0x0005B5DC
		private List<Hero> GetAudienceMembers(Hero brideHero, Hero groomHero)
		{
			Queue<Hero> groomSide = new Queue<Hero>();
			Queue<Hero> brideSide = new Queue<Hero>();
			List<Hero> list = new List<Hero>();
			Hero mother = groomHero.Mother;
			if (mother != null && mother.IsAlive)
			{
				groomSide.Enqueue(groomHero.Mother);
			}
			Hero father = groomHero.Father;
			if (father != null && father.IsAlive)
			{
				groomSide.Enqueue(groomHero.Father);
			}
			if (groomHero.Siblings != null)
			{
				foreach (Hero item in from s in groomHero.Siblings
					where s.IsAlive && !s.IsChild
					select s)
				{
					groomSide.Enqueue(item);
				}
			}
			if (groomHero.Children != null)
			{
				foreach (Hero item2 in from s in groomHero.Children
					where s.IsAlive && !s.IsChild
					select s)
				{
					groomSide.Enqueue(item2);
				}
			}
			Hero mother2 = brideHero.Mother;
			if (mother2 != null && mother2.IsAlive)
			{
				brideSide.Enqueue(brideHero.Mother);
			}
			Hero father2 = brideHero.Father;
			if (father2 != null && father2.IsAlive)
			{
				brideSide.Enqueue(brideHero.Father);
			}
			if (brideHero.Siblings != null)
			{
				foreach (Hero item3 in from s in brideHero.Siblings
					where s.IsAlive && !s.IsChild
					select s)
				{
					brideSide.Enqueue(item3);
				}
			}
			if (brideHero.Children != null)
			{
				foreach (Hero item4 in from s in brideHero.Children
					where s.IsAlive && !s.IsChild
					select s)
				{
					brideSide.Enqueue(item4);
				}
			}
			if (groomSide.Count < 3)
			{
				IEnumerable<Hero> allAliveHeroes = Hero.AllAliveHeroes;
				Func<Hero, bool> <>9__4;
				Func<Hero, bool> predicate;
				if ((predicate = <>9__4) == null)
				{
					predicate = (<>9__4 = (Hero h) => h.IsLord && !h.IsChild && h != groomHero && h != brideHero && h.IsFriend(groomHero) && !brideSide.Contains(h));
				}
				foreach (Hero item5 in allAliveHeroes.Where(predicate).Take(MathF.Ceiling(3f - (float)groomSide.Count)))
				{
					groomSide.Enqueue(item5);
				}
			}
			if (brideSide.Count < 3)
			{
				IEnumerable<Hero> allAliveHeroes2 = Hero.AllAliveHeroes;
				Func<Hero, bool> <>9__5;
				Func<Hero, bool> predicate2;
				if ((predicate2 = <>9__5) == null)
				{
					predicate2 = (<>9__5 = (Hero h) => h.IsLord && !h.IsChild && h != brideHero && h != groomHero && h.IsFriend(brideHero) && !groomSide.Contains(h));
				}
				foreach (Hero item6 in allAliveHeroes2.Where(predicate2).Take(MathF.Ceiling(3f - (float)brideSide.Count)))
				{
					brideSide.Enqueue(item6);
				}
			}
			for (int i = 0; i < 6; i++)
			{
				bool flag = i <= 1 || i == 4;
				Queue<Hero> queue = (flag ? brideSide : groomSide);
				if (queue.Count > 0 && queue.Peek() != null)
				{
					list.Add(queue.Dequeue());
				}
				else
				{
					list.Add(null);
				}
			}
			return list;
		}

		// Token: 0x040006AC RID: 1708
		private const int NumberOfAudienceHeroes = 6;

		// Token: 0x040006B0 RID: 1712
		private readonly CampaignTime _creationCampaignTime;
	}
}
