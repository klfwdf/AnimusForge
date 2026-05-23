using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x02000202 RID: 514
	public sealed class CharacterCreationContent
	{
		// Token: 0x170007CA RID: 1994
		// (get) Token: 0x06001F68 RID: 8040 RVA: 0x0008D659 File Offset: 0x0008B859
		// (set) Token: 0x06001F69 RID: 8041 RVA: 0x0008D661 File Offset: 0x0008B861
		public string SelectedTitleType { get; set; }

		// Token: 0x170007CB RID: 1995
		// (get) Token: 0x06001F6A RID: 8042 RVA: 0x0008D66A File Offset: 0x0008B86A
		// (set) Token: 0x06001F6B RID: 8043 RVA: 0x0008D672 File Offset: 0x0008B872
		public string SelectedParentOccupation { get; private set; }

		// Token: 0x170007CC RID: 1996
		// (get) Token: 0x06001F6C RID: 8044 RVA: 0x0008D67B File Offset: 0x0008B87B
		// (set) Token: 0x06001F6D RID: 8045 RVA: 0x0008D683 File Offset: 0x0008B883
		public string DefaultSelectedTitleType { get; set; }

		// Token: 0x170007CD RID: 1997
		// (get) Token: 0x06001F6E RID: 8046 RVA: 0x0008D68C File Offset: 0x0008B88C
		// (set) Token: 0x06001F6F RID: 8047 RVA: 0x0008D694 File Offset: 0x0008B894
		public TextObject ReviewPageDescription { get; private set; }

		// Token: 0x170007CE RID: 1998
		// (get) Token: 0x06001F70 RID: 8048 RVA: 0x0008D69D File Offset: 0x0008B89D
		// (set) Token: 0x06001F71 RID: 8049 RVA: 0x0008D6A5 File Offset: 0x0008B8A5
		public string MainCharacterName { get; private set; }

		// Token: 0x170007CF RID: 1999
		// (get) Token: 0x06001F72 RID: 8050 RVA: 0x0008D6AE File Offset: 0x0008B8AE
		// (set) Token: 0x06001F73 RID: 8051 RVA: 0x0008D6B6 File Offset: 0x0008B8B6
		public CultureObject SelectedCulture { get; private set; }

		// Token: 0x170007D0 RID: 2000
		// (get) Token: 0x06001F74 RID: 8052 RVA: 0x0008D6BF File Offset: 0x0008B8BF
		// (set) Token: 0x06001F75 RID: 8053 RVA: 0x0008D6C7 File Offset: 0x0008B8C7
		public Banner SelectedBanner { get; private set; }

		// Token: 0x06001F76 RID: 8054 RVA: 0x0008D6D0 File Offset: 0x0008B8D0
		public CharacterCreationContent()
		{
			this.SetMainHeroInitialStats();
		}

		// Token: 0x06001F77 RID: 8055 RVA: 0x0008D71D File Offset: 0x0008B91D
		public void AddCharacterCreationCulture(CultureObject culture, int focusToAddByCulture, int skillLevelToAddByCulture)
		{
			if (!this._characterCreationCultures.ContainsKey(culture))
			{
				this._characterCreationCultures.Add(culture, new KeyValuePair<int, int>(focusToAddByCulture, skillLevelToAddByCulture));
				return;
			}
			this._characterCreationCultures[culture] = new KeyValuePair<int, int>(focusToAddByCulture, skillLevelToAddByCulture);
		}

		// Token: 0x06001F78 RID: 8056 RVA: 0x0008D754 File Offset: 0x0008B954
		public int GetFocusToAddByCulture(CultureObject culture)
		{
			return this._characterCreationCultures[culture].Key;
		}

		// Token: 0x06001F79 RID: 8057 RVA: 0x0008D778 File Offset: 0x0008B978
		public int GetSkillLevelToAddByCulture(CultureObject culture)
		{
			return this._characterCreationCultures[culture].Value;
		}

		// Token: 0x06001F7A RID: 8058 RVA: 0x0008D799 File Offset: 0x0008B999
		public void ChangeReviewPageDescription(TextObject reviewPageDescription)
		{
			this.ReviewPageDescription = reviewPageDescription;
		}

		// Token: 0x06001F7B RID: 8059 RVA: 0x0008D7A2 File Offset: 0x0008B9A2
		public void SetMainCharacterName(string name)
		{
			this.MainCharacterName = name;
		}

		// Token: 0x06001F7C RID: 8060 RVA: 0x0008D7AB File Offset: 0x0008B9AB
		public void SetParentOccupation(string occupationType)
		{
			this.SelectedParentOccupation = occupationType;
		}

		// Token: 0x06001F7D RID: 8061 RVA: 0x0008D7B4 File Offset: 0x0008B9B4
		public void ApplySkillAndAttributeEffects(List<SkillObject> skills, int focusToAdd, int skillLevelToAdd, CharacterAttribute attribute, int attributeLevelToAdd, List<TraitObject> traits = null, int traitLevelToAdd = 0, int renownToAdd = 0, int goldToAdd = 0, int unspentFocusPoints = 0, int unspentAttributePoints = 0)
		{
			foreach (SkillObject skill in skills)
			{
				Hero.MainHero.HeroDeveloper.AddFocus(skill, focusToAdd, false);
				if (Hero.MainHero.GetSkillValue(skill) == 1)
				{
					Hero.MainHero.HeroDeveloper.ChangeSkillLevel(skill, skillLevelToAdd - 1, false);
				}
				else
				{
					Hero.MainHero.HeroDeveloper.ChangeSkillLevel(skill, skillLevelToAdd, false);
				}
			}
			Hero.MainHero.HeroDeveloper.UnspentFocusPoints += unspentFocusPoints;
			Hero.MainHero.HeroDeveloper.UnspentAttributePoints += unspentAttributePoints;
			if (attribute != null)
			{
				Hero.MainHero.HeroDeveloper.AddAttribute(attribute, attributeLevelToAdd, false);
			}
			if (traits != null && traitLevelToAdd > 0 && traits.Count > 0)
			{
				foreach (TraitObject trait in traits)
				{
					Hero.MainHero.SetTraitLevel(trait, Hero.MainHero.GetTraitLevel(trait) + traitLevelToAdd);
				}
			}
			if (renownToAdd > 0)
			{
				GainRenownAction.Apply(Hero.MainHero, (float)renownToAdd, true);
			}
			if (goldToAdd > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, goldToAdd, true);
			}
			Hero.MainHero.HeroDeveloper.SetInitialLevel(1);
		}

		// Token: 0x06001F7E RID: 8062 RVA: 0x0008D924 File Offset: 0x0008BB24
		public void SetMainClanBanner(Banner banner)
		{
			this.SelectedBanner = banner;
		}

		// Token: 0x06001F7F RID: 8063 RVA: 0x0008D930 File Offset: 0x0008BB30
		public void SetSelectedCulture(CultureObject culture, CharacterCreationManager characterCreationManager)
		{
			this.SelectedCulture = culture;
			characterCreationManager.ResetMenuOptions();
			this.SelectedTitleType = this.DefaultSelectedTitleType;
			TextObject textObject = FactionHelper.GenerateClanNameforPlayer();
			Clan.PlayerClan.ChangeClanName(textObject, textObject);
		}

		// Token: 0x06001F80 RID: 8064 RVA: 0x0008D968 File Offset: 0x0008BB68
		public void ApplyCulture(CharacterCreationManager characterCreationManager)
		{
			Hero.MainHero.Culture = this.SelectedCulture;
			Clan.PlayerClan.Culture = this.SelectedCulture;
			Clan.PlayerClan.ResetPlayerHomeAndFactionMidSettlement();
			Hero.MainHero.BornSettlement = Clan.PlayerClan.HomeSettlement;
		}

		// Token: 0x06001F81 RID: 8065 RVA: 0x0008D9A8 File Offset: 0x0008BBA8
		public IEnumerable<CultureObject> GetCultures()
		{
			foreach (KeyValuePair<CultureObject, KeyValuePair<int, int>> keyValuePair in this._characterCreationCultures)
			{
				yield return keyValuePair.Key;
			}
			Dictionary<CultureObject, KeyValuePair<int, int>>.Enumerator enumerator = default(Dictionary<CultureObject, KeyValuePair<int, int>>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06001F82 RID: 8066 RVA: 0x0008D9B8 File Offset: 0x0008BBB8
		private void SetMainHeroInitialStats()
		{
			Hero.MainHero.HeroDeveloper.ClearHero();
			Hero.MainHero.HitPoints = 100;
			foreach (SkillObject skill in Skills.All)
			{
				Hero.MainHero.HeroDeveloper.InitializeSkillXp(skill);
			}
			foreach (CharacterAttribute attrib in Attributes.All)
			{
				Hero.MainHero.HeroDeveloper.AddAttribute(attrib, 2, false);
			}
		}

		// Token: 0x06001F83 RID: 8067 RVA: 0x0008DA7C File Offset: 0x0008BC7C
		public void AddEquipmentToUseGetter(CharacterCreationContent.TryGetEquipmentIdDelegate tryGetEquipmentIdDelegate)
		{
			this._tryGetEquipmentIdDelegates.Add(tryGetEquipmentIdDelegate);
		}

		// Token: 0x06001F84 RID: 8068 RVA: 0x0008DA8C File Offset: 0x0008BC8C
		public bool TryGetEquipmentToUse(string occupationId, out string equipmentId)
		{
			for (int i = this._tryGetEquipmentIdDelegates.Count - 1; i >= 0; i--)
			{
				if (this._tryGetEquipmentIdDelegates[i](occupationId, out equipmentId))
				{
					return true;
				}
			}
			equipmentId = null;
			return false;
		}

		// Token: 0x04000924 RID: 2340
		public int FocusToAdd = 1;

		// Token: 0x04000925 RID: 2341
		public int SkillLevelToAdd = 10;

		// Token: 0x04000926 RID: 2342
		public int AttributeLevelToAdd = 1;

		// Token: 0x0400092E RID: 2350
		public int StartingAge = 20;

		// Token: 0x0400092F RID: 2351
		private readonly Dictionary<CultureObject, KeyValuePair<int, int>> _characterCreationCultures = new Dictionary<CultureObject, KeyValuePair<int, int>>();

		// Token: 0x04000930 RID: 2352
		private readonly List<CharacterCreationContent.TryGetEquipmentIdDelegate> _tryGetEquipmentIdDelegates = new List<CharacterCreationContent.TryGetEquipmentIdDelegate>();

		// Token: 0x020005F9 RID: 1529
		// (Invoke) Token: 0x06004F18 RID: 20248
		public delegate bool TryGetEquipmentIdDelegate(string occupationId, out string equipmentId);
	}
}
