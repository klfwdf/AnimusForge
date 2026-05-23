using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003EA RID: 1002
	public class EducationCampaignBehavior : CampaignBehaviorBase, IEducationLogic
	{
		// Token: 0x06003D9A RID: 15770 RVA: 0x0010C6E0 File Offset: 0x0010A8E0
		public override void RegisterEvents()
		{
			if (!CampaignOptions.IsLifeDeathCycleDisabled)
			{
				CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
				CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, new Action(this.OnCharacterCreationOver));
				CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
				CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroComesOfAge));
			}
		}

		// Token: 0x06003D9B RID: 15771 RVA: 0x0010C750 File Offset: 0x0010A950
		private void OnHeroComesOfAge(Hero hero)
		{
			Hero mother = hero.Mother;
			if (((mother != null) ? mother.Clan : null) != Clan.PlayerClan)
			{
				Hero father = hero.Father;
				if (((father != null) ? father.Clan : null) != Clan.PlayerClan)
				{
					return;
				}
			}
			this.DoEducationUntil(hero, EducationCampaignBehavior.ChildAgeState.Count);
			this._previousEducations.Remove(hero);
		}

		// Token: 0x06003D9C RID: 15772 RVA: 0x0010C7A4 File Offset: 0x0010A9A4
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Hero, short>>("_previousEducations", ref this._previousEducations);
		}

		// Token: 0x06003D9D RID: 15773 RVA: 0x0010C7B8 File Offset: 0x0010A9B8
		public void GetOptionProperties(Hero child, string optionKey, List<string> previousOptions, out TextObject optionTitle, out TextObject description, out TextObject effect, out ValueTuple<CharacterAttribute, int>[] attributes, out ValueTuple<SkillObject, int>[] skills, out ValueTuple<SkillObject, int>[] focusPoints, out EducationCampaignBehavior.EducationCharacterProperties[] educationCharacterProperties)
		{
			EducationCampaignBehavior.EducationStage stage = this.GetStage(child);
			EducationCampaignBehavior.EducationOption option = stage.GetOption(optionKey);
			description = option.Description;
			effect = option.Effect;
			optionTitle = option.Title;
			educationCharacterProperties = stage.GetCharacterPropertiesForOption(child, option, optionKey, previousOptions);
			if (option.Attributes == null)
			{
				attributes = null;
			}
			else
			{
				attributes = new ValueTuple<CharacterAttribute, int>[option.Attributes.Length];
				for (int i = 0; i < option.Attributes.Length; i++)
				{
					attributes[i] = new ValueTuple<CharacterAttribute, int>(option.Attributes[i], 1);
				}
			}
			if (option.Skills == null)
			{
				skills = null;
				focusPoints = null;
				return;
			}
			skills = new ValueTuple<SkillObject, int>[option.Skills.Length];
			focusPoints = new ValueTuple<SkillObject, int>[option.Skills.Length];
			for (int j = 0; j < option.Skills.Length; j++)
			{
				skills[j] = new ValueTuple<SkillObject, int>(option.Skills[j], 5);
				focusPoints[j] = new ValueTuple<SkillObject, int>(option.Skills[j], 1);
			}
		}

		// Token: 0x06003D9E RID: 15774 RVA: 0x0010C8B8 File Offset: 0x0010AAB8
		public void GetPageProperties(Hero child, List<string> previousChoices, out TextObject title, out TextObject description, out TextObject instruction, out EducationCampaignBehavior.EducationCharacterProperties[] defaultCharacterProperties, out string[] availableOptions)
		{
			EducationCampaignBehavior.EducationStage stage = this.GetStage(child);
			EducationCampaignBehavior.EducationPage page = stage.GetPage(previousChoices);
			defaultCharacterProperties = stage.GetCharacterPropertiesForPage(child, page, previousChoices);
			title = page.Title;
			description = page.Description;
			instruction = page.Instruction;
			availableOptions = page.GetAvailableOptions(stage.StringIdToEducationOption(previousChoices));
		}

		// Token: 0x06003D9F RID: 15775 RVA: 0x0010C90C File Offset: 0x0010AB0C
		public bool IsValidEducationNotification(EducationMapNotification data)
		{
			EducationCampaignBehavior.EducationStage stage = this.GetStage(data.Child);
			return data.Child.IsAlive && data.Age > 0 && data.Child.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && stage != null;
		}

		// Token: 0x06003DA0 RID: 15776 RVA: 0x0010C964 File Offset: 0x0010AB64
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail details, bool showNotifications)
		{
			if (victim.Clan == Clan.PlayerClan && this._previousEducations.ContainsKey(victim))
			{
				this._previousEducations.Remove(victim);
			}
		}

		// Token: 0x06003DA1 RID: 15777 RVA: 0x0010C990 File Offset: 0x0010AB90
		public void GetStageProperties(Hero child, out int pageCount)
		{
			EducationCampaignBehavior.EducationStage stage = this.GetStage(child);
			pageCount = stage.PageCount;
		}

		// Token: 0x06003DA2 RID: 15778 RVA: 0x0010C9AD File Offset: 0x0010ABAD
		private void OnCharacterCreationOver()
		{
			if (CampaignOptions.IsLifeDeathCycleDisabled)
			{
				CampaignEventDispatcher.Instance.RemoveListeners(this);
			}
		}

		// Token: 0x06003DA3 RID: 15779 RVA: 0x0010C9C4 File Offset: 0x0010ABC4
		private void OnDailyTick()
		{
			if (MapEvent.PlayerMapEvent == null)
			{
				foreach (Hero hero in Clan.PlayerClan.Heroes)
				{
					if (hero.IsAlive && hero != Hero.MainHero && hero.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
					{
						EducationCampaignBehavior.ChildAgeState lastDoneStage = this.GetLastDoneStage(hero);
						if (lastDoneStage != EducationCampaignBehavior.ChildAgeState.Year16)
						{
							EducationCampaignBehavior.ChildAgeState childAgeState = (EducationCampaignBehavior.ChildAgeState)MathF.Max((int)(lastDoneStage + 1), (int)this.GetClosestStage(hero));
							int num = EducationCampaignBehavior.ChildStateToAge(childAgeState);
							if ((hero.BirthDay + CampaignTime.Years((float)num)).IsPast && !this.HasNotificationForAge(hero, num))
							{
								this.DoEducationUntil(hero, childAgeState);
								if (!hero.IsDisabled)
								{
									this.ShowEducationNotification(hero, EducationCampaignBehavior.ChildStateToAge(childAgeState));
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06003DA4 RID: 15780 RVA: 0x0010CAC4 File Offset: 0x0010ACC4
		private EducationCampaignBehavior.ChildAgeState GetClosestStage(Hero child)
		{
			EducationCampaignBehavior.ChildAgeState result = EducationCampaignBehavior.ChildAgeState.Year2;
			int num = MathF.Round(child.Age);
			for (EducationCampaignBehavior.ChildAgeState childAgeState = EducationCampaignBehavior.ChildAgeState.Year2; childAgeState <= EducationCampaignBehavior.ChildAgeState.Year16; childAgeState += 1)
			{
				if (num >= EducationCampaignBehavior.ChildStateToAge(childAgeState))
				{
					result = childAgeState;
				}
			}
			return result;
		}

		// Token: 0x06003DA5 RID: 15781 RVA: 0x0010CAF8 File Offset: 0x0010ACF8
		private EducationCampaignBehavior.ChildAgeState GetLastDoneStage(Hero child)
		{
			short result;
			if (this._previousEducations.TryGetValue(child, out result))
			{
				return (EducationCampaignBehavior.ChildAgeState)result;
			}
			return EducationCampaignBehavior.ChildAgeState.Invalid;
		}

		// Token: 0x06003DA6 RID: 15782 RVA: 0x0010CB18 File Offset: 0x0010AD18
		private void OnFinalize(EducationCampaignBehavior.EducationStage stage, Hero child, List<string> chosenOptions)
		{
			foreach (string optionKey in chosenOptions)
			{
				stage.GetOption(optionKey).OnConsequence(child);
			}
			CampaignEventDispatcher.Instance.OnChildEducationCompleted(child, EducationCampaignBehavior.ChildStateToAge(stage.Target));
			short target = (short)stage.Target;
			if (this._previousEducations.ContainsKey(child))
			{
				this._previousEducations[child] = target;
			}
			else
			{
				this._previousEducations.Add(child, target);
			}
			this._activeStage = null;
			this._activeChild = null;
		}

		// Token: 0x06003DA7 RID: 15783 RVA: 0x0010CBC4 File Offset: 0x0010ADC4
		private bool HasNotificationForAge(Hero child, int age)
		{
			return Campaign.Current.CampaignInformationManager.InformationDataExists<EducationMapNotification>((EducationMapNotification notification) => notification.Child == child && notification.Age == age);
		}

		// Token: 0x06003DA8 RID: 15784 RVA: 0x0010CC00 File Offset: 0x0010AE00
		private void ShowEducationNotification(Hero child, int age)
		{
			TextObject textObject = GameTexts.FindText("str_education_notification_right", null);
			textObject.SetCharacterProperties("CHILD", child.CharacterObject, true);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new EducationMapNotification(child, age, textObject));
			if (child.Father == Hero.MainHero || child.Mother == Hero.MainHero)
			{
				Debug.Print(string.Format("Showing Education Notification, Hero: {0}: {1} - Age: {2}.", child.StringId, child.Name, age), 0, Debug.DebugColor.White, 17592186044416UL);
			}
			if (!this._previousEducations.ContainsKey(child))
			{
				this._previousEducations.Add(child, -1);
			}
		}

		// Token: 0x06003DA9 RID: 15785 RVA: 0x0010CCA4 File Offset: 0x0010AEA4
		private void DoEducationUntil(Hero child, EducationCampaignBehavior.ChildAgeState childAgeState)
		{
			short num;
			if (!this._previousEducations.TryGetValue(child, out num))
			{
				num = -1;
			}
			for (EducationCampaignBehavior.ChildAgeState childAgeState2 = (EducationCampaignBehavior.ChildAgeState)(num + 1); childAgeState2 < childAgeState; childAgeState2 += 1)
			{
				if (childAgeState2 != EducationCampaignBehavior.ChildAgeState.Invalid)
				{
					EducationCampaignBehavior.EducationStage stage = this.GetStage(child, childAgeState2);
					this.DoStage(child, stage);
				}
			}
		}

		// Token: 0x06003DAA RID: 15786 RVA: 0x0010CCE8 File Offset: 0x0010AEE8
		private void DoStage(Hero child, EducationCampaignBehavior.EducationStage stage)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < stage.PageCount; i++)
			{
				string[] availableOptions = stage.GetPage(list).GetAvailableOptions(stage.StringIdToEducationOption(list));
				list.Add(availableOptions.GetRandomElement<string>());
			}
			this.OnFinalize(stage, child, list);
		}

		// Token: 0x06003DAB RID: 15787 RVA: 0x0010CD35 File Offset: 0x0010AF35
		public void Finalize(Hero child, List<string> chosenOptions)
		{
			this.OnFinalize(this.GetStage(child), child, chosenOptions);
		}

		// Token: 0x06003DAC RID: 15788 RVA: 0x0010CD46 File Offset: 0x0010AF46
		private bool IsHeroChildOfPlayer(Hero child)
		{
			return Hero.MainHero.Children.Contains(child);
		}

		// Token: 0x06003DAD RID: 15789 RVA: 0x0010CD58 File Offset: 0x0010AF58
		private bool ChildCultureHasLorekeeper(Hero child)
		{
			return child.Culture.StringId == "khuzait" || child.Culture.StringId == "battania";
		}

		// Token: 0x06003DAE RID: 15790 RVA: 0x0010CD88 File Offset: 0x0010AF88
		private bool ChildCultureHasBard(Hero child)
		{
			return child.Culture.StringId == "battania";
		}

		// Token: 0x06003DAF RID: 15791 RVA: 0x0010CDA0 File Offset: 0x0010AFA0
		private EducationCampaignBehavior.EducationStage GetStage(Hero child)
		{
			short num;
			EducationCampaignBehavior.ChildAgeState state;
			if (this._previousEducations.TryGetValue(child, out num) && num != -1)
			{
				state = (EducationCampaignBehavior.ChildAgeState)(num + 1);
			}
			else
			{
				state = this.GetClosestStage(child);
			}
			return this.GetStage(child, state);
		}

		// Token: 0x06003DB0 RID: 15792 RVA: 0x0010CDD8 File Offset: 0x0010AFD8
		private EducationCampaignBehavior.EducationStage GetStage(Hero child, EducationCampaignBehavior.ChildAgeState state)
		{
			if (this._activeStage == null || this._activeStage.Target != state || child != this._activeChild)
			{
				this._activeChild = child;
				switch (state)
				{
				case EducationCampaignBehavior.ChildAgeState.Year2:
					this._activeStage = this.CreateStage2(child);
					break;
				case EducationCampaignBehavior.ChildAgeState.Year5:
					this._activeStage = this.CreateStage5(child);
					break;
				case EducationCampaignBehavior.ChildAgeState.Year8:
					this._activeStage = this.CreateStage8(child);
					break;
				case EducationCampaignBehavior.ChildAgeState.Year11:
					this._activeStage = this.CreateStage11(child);
					break;
				case EducationCampaignBehavior.ChildAgeState.Year14:
					this._activeStage = this.CreateStage14(child);
					break;
				case EducationCampaignBehavior.ChildAgeState.Year16:
					this._activeStage = this.CreateStage16(child);
					break;
				default:
					this._activeStage = null;
					this._activeChild = null;
					break;
				}
			}
			StringHelpers.SetCharacterProperties("CHILD", child.CharacterObject, null, false);
			return this._activeStage;
		}

		// Token: 0x06003DB1 RID: 15793 RVA: 0x0010CEB0 File Offset: 0x0010B0B0
		private EducationCampaignBehavior.EducationStage CreateStage2(Hero child)
		{
			EducationCampaignBehavior.EducationStage educationStage = new EducationCampaignBehavior.EducationStage(EducationCampaignBehavior.ChildAgeState.Year2);
			TextObject title = new TextObject("{=xc4ossl0}Infancy", null);
			Dictionary<CharacterAttribute, TextObject> dictionary = new Dictionary<CharacterAttribute, TextObject>
			{
				{
					DefaultCharacterAttributes.Vigor,
					new TextObject("{=h7aX2GOw}This child is quite strong, grabbing whatever {?CHILD.GENDER}she{?}he{\\?} likes climbing out of {?CHILD.GENDER}her{?}his{\\?} cradle whenever {?CHILD.GENDER}her{?}his{\\?} caretaker's back is turned.", null)
				},
				{
					DefaultCharacterAttributes.Control,
					new TextObject("{=pQSBdHC7}The child has exceptional coordination for someone {?CHILD.GENDER}her{?}his{\\?} age, and can catch a tossed ball and eat by {?CHILD.GENDER}herself{?}himself{\\?}.", null)
				},
				{
					DefaultCharacterAttributes.Endurance,
					new TextObject("{=xaNpQsjh}The child seems to never tire, running {?CHILD.GENDER}her{?}his{\\?} caretakers ragged, and is rarely cranky.", null)
				},
				{
					DefaultCharacterAttributes.Cunning,
					new TextObject("{=lF41sN5r}You see the glint of mischief on {?CHILD.GENDER}her{?}his{\\?} smiling face. Any sweet left unattended in the kitchen for even a few minutes is quickly stolen.", null)
				},
				{
					DefaultCharacterAttributes.Intelligence,
					new TextObject("{=KVDMVbT1}This child started speaking earlier than most of {?CHILD.GENDER}her{?}his{\\?} peers and can even string together simple sentences.", null)
				},
				{
					DefaultCharacterAttributes.Social,
					new TextObject("{=xXJnW5w0}The child pays close attention to anyone talking to {?CHILD.GENDER}her{?}him{\\?} and sometimes tries to comfort playmates in distress.", null)
				}
			};
			Dictionary<CharacterAttribute, EducationCampaignBehavior.EducationCharacterProperties> dictionary2 = new Dictionary<CharacterAttribute, EducationCampaignBehavior.EducationCharacterProperties>
			{
				{
					DefaultCharacterAttributes.Vigor,
					new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_vigor")
				},
				{
					DefaultCharacterAttributes.Control,
					new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_control")
				},
				{
					DefaultCharacterAttributes.Endurance,
					new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_endurance")
				},
				{
					DefaultCharacterAttributes.Cunning,
					new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_cunning")
				},
				{
					DefaultCharacterAttributes.Intelligence,
					new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_intelligence")
				},
				{
					DefaultCharacterAttributes.Social,
					new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_social")
				}
			};
			TextObject textObject = new TextObject("{=Il7pDS8i}People remark on how much {?PLAYER_CHILD}your{?}the{\\?} baby resembles {?CHILD.GENDER}her{?}his{\\?} parents. {CHILD.NAME} definitely has {?PLAYER_IS_FATHER}your{?}{?CHILD.GENDER}her{?}his{\\?} father's{\\?}...", null);
			textObject.SetTextVariable("PLAYER_IS_FATHER", (child.Father == Hero.MainHero) ? 1 : 0);
			EducationCampaignBehavior.EducationCharacterProperties childProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_sleep", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage = educationStage.AddPage(0, title, textObject, this._pickAttributeText, childProperties, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			ValueTuple<CharacterAttribute, int>[] array;
			this.GetHighestThreeAttributes(child.Father, out array);
			for (int i = 0; i < array.Length; i++)
			{
				EducationCampaignBehavior.EducationCharacterProperties childProperties2 = dictionary2[array[i].Item1];
				educationPage.AddOption(new EducationCampaignBehavior.EducationOption(array[i].Item1.Name, dictionary[array[i].Item1], null, null, null, new CharacterAttribute[] { array[i].Item1 }, null, childProperties2, default(EducationCampaignBehavior.EducationCharacterProperties)));
			}
			TextObject textObject2 = new TextObject("{=0vWaNd1m}At the same time, {?CHILD.GENDER}she{?}he{\\?} shows {?PLAYER_IS_MOTHER}your{?}{?CHILD.GENDER}her{?}his{\\?} mother's{\\?}....", null);
			textObject2.SetTextVariable("PLAYER_IS_MOTHER", (child.Mother == Hero.MainHero) ? 1 : 0);
			EducationCampaignBehavior.EducationCharacterProperties childProperties3 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_toddler_tantrum", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage2 = educationStage.AddPage(1, title, textObject2, this._pickAttributeText, childProperties3, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			ValueTuple<CharacterAttribute, int>[] array2;
			this.GetHighestThreeAttributes(child.Mother, out array2);
			for (int j = 0; j < array2.Length; j++)
			{
				EducationCampaignBehavior.EducationCharacterProperties childProperties4 = dictionary2[array2[j].Item1];
				educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(array2[j].Item1.Name, dictionary[array2[j].Item1], null, null, null, new CharacterAttribute[] { array2[j].Item1 }, null, childProperties4, default(EducationCampaignBehavior.EducationCharacterProperties)));
			}
			TextObject description = new TextObject("{=taJOTFrb}Despite its tender age, the baby already starts to show some aptitude in {ATR_1} and {ATR_2}.", null);
			int optionIndexer = 0;
			EducationCampaignBehavior.EducationPage stage_0_page_2 = educationStage.AddPage(2, title, description, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			for (int k = 0; k < Attributes.All.Count; k++)
			{
				CharacterAttribute attributeOne = Attributes.All[k];
				EducationCampaignBehavior.EducationCharacterProperties childProperties5 = dictionary2[attributeOne];
				List<CharacterAttribute> list = (from x in Attributes.All
					where x != attributeOne
					select x).ToList<CharacterAttribute>();
				for (int l = 0; l < Attributes.All.Count - 1; l++)
				{
					CharacterAttribute attributeTwo = list[l];
					int count = Attributes.All.Count;
					TextObject textObject3 = new TextObject("{=ISoR0vaR}{ATR_1} and {ATR_2}", null);
					TextObject textObject4 = new TextObject("{=KiOBbxr3}In addition to {?CHILD.GENDER}her{?}his{\\?} parents traits, this baby also shows promising {ATR_1} and {ATR_2}.", null);
					textObject3.SetTextVariable("ATR_1", attributeOne.Name);
					textObject4.SetTextVariable("ATR_1", attributeOne.Name);
					textObject3.SetTextVariable("ATR_2", attributeTwo.Name);
					textObject4.SetTextVariable("ATR_2", attributeTwo.Name);
					EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate condition = delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
					{
						int optionIndexer;
						if (o == stage_0_page_2.Options.FirstOrDefault<EducationCampaignBehavior.EducationOption>())
						{
							optionIndexer = 0;
						}
						int num = ((previousOptions[0].Attributes[0] == previousOptions[1].Attributes[0]) ? 1 : 2);
						object obj = previousOptions[0].Attributes.Contains(attributeOne) || previousOptions[1].Attributes.Contains(attributeOne) || previousOptions[0].Attributes.Contains(attributeTwo) || previousOptions[1].Attributes.Contains(attributeTwo);
						int num2 = (Attributes.All.Count - num - 1) * (Attributes.All.Count - num);
						int randomValue = previousOptions[0].RandomValue;
						int randomValue2 = previousOptions[1].RandomValue;
						int num3 = (randomValue % num2 + randomValue2 % num2) % num2;
						object obj2 = obj;
						if (obj2 == null)
						{
							optionIndexer = optionIndexer;
							optionIndexer++;
						}
						if (obj2 == null && num3 == optionIndexer % num2)
						{
							stage_0_page_2.Description.SetTextVariable("ATR_1", attributeOne.Name);
							stage_0_page_2.Description.SetTextVariable("ATR_2", attributeTwo.Name);
							return true;
						}
						return false;
					};
					stage_0_page_2.AddOption(new EducationCampaignBehavior.EducationOption(textObject3, textObject4, null, condition, null, new CharacterAttribute[] { attributeOne, attributeTwo }, null, childProperties5, default(EducationCampaignBehavior.EducationCharacterProperties)));
				}
			}
			return educationStage;
		}

		// Token: 0x06003DB2 RID: 15794 RVA: 0x0010D3A2 File Offset: 0x0010B5A2
		private static int ChildStateToAge(EducationCampaignBehavior.ChildAgeState state)
		{
			switch (state)
			{
			case EducationCampaignBehavior.ChildAgeState.Year2:
				return 2;
			case EducationCampaignBehavior.ChildAgeState.Year5:
				return 5;
			case EducationCampaignBehavior.ChildAgeState.Year8:
				return 8;
			case EducationCampaignBehavior.ChildAgeState.Year11:
				return 11;
			case EducationCampaignBehavior.ChildAgeState.Year14:
				return 14;
			case EducationCampaignBehavior.ChildAgeState.Year16:
				return 16;
			default:
				return -1;
			}
		}

		// Token: 0x06003DB3 RID: 15795 RVA: 0x0010D3D4 File Offset: 0x0010B5D4
		private void Stage2Selection(List<SkillObject> skills, EducationCampaignBehavior.EducationPage previousPage, EducationCampaignBehavior.EducationPage currentPage, EducationCampaignBehavior.EducationCharacterProperties[] childProperties, EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties)
		{
			for (int i = 0; i < skills.Count; i++)
			{
				int index = i;
				EducationCampaignBehavior.EducationCharacterProperties childProperties2 = childProperties[index];
				EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties = tutorProperties[index];
				SkillObject skill = skills[index];
				TextObject textObject = new TextObject("{=!}{SKILL}", null);
				TextObject textObject2 = new TextObject("{=!}{SKILL_DESC}", null);
				textObject.SetTextVariable("SKILL", skill.Name);
				textObject2.SetTextVariable("SKILL_DESC", previousPage.Options.First((EducationCampaignBehavior.EducationOption x) => x.Skills.Contains(skill)).Description);
				EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate condition = delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
				{
					int num = previousOptions[0].RandomValue % skills.Count;
					int num2 = previousOptions[1].RandomValue % skills.Count;
					bool flag = (num + num2) % skills.Count == index && previousOptions[1].Skills[0] != skill;
					if (flag)
					{
						currentPage.Description.SetTextVariable("SKILL", skill.Name);
					}
					return flag;
				};
				currentPage.AddOption(new EducationCampaignBehavior.EducationOption(textObject, textObject2, null, condition, null, null, new SkillObject[] { skill }, childProperties2, specialCharacterProperties));
				SkillObject alternativeSkill = skills[(index + MBRandom.RandomInt(1, 6)) % skills.Count];
				TextObject textObject3 = new TextObject("{=!}{SKILL}", null);
				TextObject textObject4 = new TextObject("{=!}{SKILL_DESC}", null);
				textObject3.SetTextVariable("SKILL", alternativeSkill.Name);
				textObject4.SetTextVariable("SKILL_DESC", previousPage.Options.First((EducationCampaignBehavior.EducationOption x) => x.Skills.Contains(alternativeSkill)).Description);
				EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate condition2 = delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
				{
					int num = previousOptions[0].RandomValue % skills.Count;
					int num2 = previousOptions[1].RandomValue % skills.Count;
					bool flag = (num + num2) % skills.Count == index && previousOptions[1].Skills[0] == skill;
					if (flag)
					{
						currentPage.Description.SetTextVariable("SKILL", alternativeSkill.Name);
					}
					return flag;
				};
				currentPage.AddOption(new EducationCampaignBehavior.EducationOption(textObject3, textObject4, null, condition2, null, null, new SkillObject[] { alternativeSkill }, childProperties2, specialCharacterProperties));
			}
		}

		// Token: 0x06003DB4 RID: 15796 RVA: 0x0010D5C8 File Offset: 0x0010B7C8
		private void Stage16Selection(ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple, EducationCampaignBehavior.EducationPage currentPage, EducationCampaignBehavior.EducationCharacterProperties[] childProperties)
		{
			for (int i = 0; i < titleDescSkillTuple.Length; i++)
			{
				int index = i;
				ValueTuple<TextObject, TextObject, SkillObject> container = titleDescSkillTuple[index];
				EducationCampaignBehavior.EducationCharacterProperties childProperties2 = childProperties[index];
				SkillObject skill = container.Item3;
				EducationCampaignBehavior.EducationOption option = new EducationCampaignBehavior.EducationOption(new TextObject("{=!}{OUTCOME_TITLE}", null), new TextObject("{=!}{OUTCOME_DESC}", null), null, delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
				{
					int num = previousOptions[0].RandomValue % titleDescSkillTuple.Length;
					int num2 = previousOptions[1].RandomValue % titleDescSkillTuple.Length;
					int num3 = (num + num2) % titleDescSkillTuple.Length;
					SkillObject previousPageSkill = previousOptions[1].Skills[0];
					bool flag = index == num3;
					if (flag)
					{
						int num4 = titleDescSkillTuple.FindIndex((ValueTuple<TextObject, TextObject, SkillObject> x) => x.Item3 == previousPageSkill);
						if (num3 == num4)
						{
							container = titleDescSkillTuple[(index + 1) % titleDescSkillTuple.Length];
						}
						currentPage.Description.SetTextVariable("RANDOM_OUTCOME", container.Item1);
						currentPage.Description.SetTextVariable("SKILL", skill.Name);
					}
					o.Title.SetTextVariable("OUTCOME_TITLE", container.Item1);
					o.Description.SetTextVariable("OUTCOME_DESC", container.Item2);
					return flag;
				}, null, null, new SkillObject[] { skill }, childProperties2, default(EducationCampaignBehavior.EducationCharacterProperties));
				currentPage.AddOption(option);
			}
		}

		// Token: 0x06003DB5 RID: 15797 RVA: 0x0010D6A8 File Offset: 0x0010B8A8
		private EducationCampaignBehavior.EducationStage CreateStage5(Hero child)
		{
			EducationCampaignBehavior.EducationStage educationStage = new EducationCampaignBehavior.EducationStage(EducationCampaignBehavior.ChildAgeState.Year5);
			TextObject title = new TextObject("{=8Yiwt1z6}Early Childhood", null);
			TextObject description = new TextObject("{=6PrmgKXa}{CHILD.NAME} is now old enough to play independently with the other children of the estate. You are particularly struck by how {?CHILD.GENDER}she{?}he{\\?}...", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_inventory_idle_start", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage = educationStage.AddPage(0, title, description, this._chooseTalentText, childProperties, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=aeWZRHy3}takes charge.", null), new TextObject("{=TyvEmZbC}{CHILD.NAME} is usually the one who decides what games {?CHILD.GENDER}her{?}his{\\?} friends will play, and leads them on imaginary adventures around the estate.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Social }, new SkillObject[] { DefaultSkills.Leadership }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader"), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=auCinAKs}never tires out.", null), new TextObject("{=ymt7Ol6x}{?CHILD.GENDER}She{?}He{\\?} seems to have limitless energy, continuing with games long after all the other children have taken a break.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Endurance }, new SkillObject[] { DefaultSkills.Athletics }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce"), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=fvutqQH6}always hits {?CHILD.GENDER}her{?}his{\\?} mark.", null), new TextObject("{=i2qOUBF4}The child will win any game that involves throwing and aiming, and the local crows have learned to keep their distance lest they want to be hit by a stone.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Control }, new SkillObject[] { DefaultSkills.Throwing }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_throw", "spear_new_f_1-9m", false), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=J80YXzR2}is fascinated by riddles.", null), new TextObject("{=QuffbfeU}The child asks any adults {?CHILD.GENDER}she{?}he{\\?} sees to ask {?CHILD.GENDER}her{?}him{\\?} one of the riddles that are loved by your people, and never gives up until it is solved.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence }, new SkillObject[] { DefaultSkills.Engineering }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_memory"), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=tMkMi5C7}wins wrestling matches.", null), new TextObject("{=zulqIQQw}Children play rough, and this child is usually the one who winds up on top in any tussle.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Vigor }, new SkillObject[] { DefaultSkills.TwoHanded }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=h1GdWZjk}avoids chores.", null), new TextObject("{=qbLkFkWr}Usually when there is work to be done this child is nowhere to be found. You've learned some of {?CHILD.GENDER}her{?}his{\\?} hiding places, but {?CHILD.GENDER}she{?}he{\\?} always seems to find more.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Cunning }, new SkillObject[] { DefaultSkills.Roguery }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_spoiled"), default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject description2 = new TextObject("{=GX0B9ngI}{CHILD.NAME} spends time by {?CHILD.GENDER}herself{?}himself{\\?} as well, frequently...", null);
			EducationCampaignBehavior.EducationPage educationPage2 = educationStage.AddPage(1, title, description2, this._chooseTalentText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=gkuS35ly}caring for your horses.", null), new TextObject("{=smdvQWvu}{?PLAYER_CHILD}Your{?}This{\\?} child loves animals of all kinds. You know that one day {?CHILD.GENDER}she{?}he{\\?} will be a great rider.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=1Ehm6o1d}shooting at targets.", null), new TextObject("{=RkQjllml}Given even a few minutes of free time, {?PLAYER_CHILD}your{?}this{\\?} child will line up targets and shoot them with {?CHILD.GENDER}her{?}his{\\?} home-made bow.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Bow }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=25vJYig0}trekking around nearby hills.", null), new TextObject("{=LJsckbyF}{?PLAYER_CHILD}Your{?}This{\\?} child spends hours exploring the edges of the estate, following animal tracks and looking for edible plants.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Scouting }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=54PfOh98}making {?CHILD.GENDER}her{?}his{\\?} own toys.", null), new TextObject("{=LfJhIQpb}With a few sticks and a bit of twine, {?CHILD.GENDER}she{?}he{\\?} can make recognizable animals, weapons or dolls.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Crafting }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_artisan", "carry_linen", false), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=hjR0Jvh1}fighting mock battles.", null), new TextObject("{=hGheavqY}{?PLAYER_CHILD}Your{?}This{\\?} child spends most of {?CHILD.GENDER}her{?}his{\\?} free time fighting imaginary monsters with {?CHILD.GENDER}her{?}his{\\?} wooden toy sword.", null), null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_play_2", "training_sword", false), default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=wX0uzvRE}playing board games.", null), new TextObject("{=H7NJ0QHQ}You see {?PLAYER_CHILD}your{?}the{\\?} child endlessly re-arranging the pieces, inventing new rules and playing out stories in {?CHILD.GENDER}her{?}his{\\?} head.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician"), default(EducationCampaignBehavior.EducationCharacterProperties)));
			foreach (EducationCampaignBehavior.EducationOption educationOption in educationPage2.Options)
			{
				educationOption.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject stage_1_page_2_description = new TextObject("{=Zoes3ojA}{?CHILD.GENDER}Her{?}His{\\?} tutors continue to remark on {CHILD.NAME}'s progress, commending {?CHILD.GENDER}her{?}his{\\?} {ATR}.", null);
			EducationCampaignBehavior.EducationPage educationPage3 = educationStage.AddPage(2, title, stage_1_page_2_description, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			ValueTuple<TextObject, TextObject, CharacterAttribute[]>[] stage_1_page_2_options = new ValueTuple<TextObject, TextObject, CharacterAttribute[]>[]
			{
				new ValueTuple<TextObject, TextObject, CharacterAttribute[]>(new TextObject("{=VdLkZthN}Mathematical Aptitude", null), new TextObject("{=23YF84ib}{?PLAYER_CHILD}Your{?}The{\\?} child quickly solves problems in {?CHILD.GENDER}her{?}his{\\?} head, helping adults with their calculations.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence }),
				new ValueTuple<TextObject, TextObject, CharacterAttribute[]>(new TextObject("{=6RckjM4S}Presence", null), new TextObject("{=Instmiut}Your child is blessed with a strong and sonorous voice, making {?CHILD.GENDER}her{?}him{\\?} seem older and wiser than {?CHILD.GENDER}her{?}his{\\?} years.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Social }),
				new ValueTuple<TextObject, TextObject, CharacterAttribute[]>(new TextObject("{=D1jGAX41}Courage", null), new TextObject("{=WOriPdox}When trouble comes, {?PLAYER_CHILD}your child{?}{?CHILD.GENDER}she{?}he{\\?}{\\?} faces it head on.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Vigor }),
				new ValueTuple<TextObject, TextObject, CharacterAttribute[]>(new TextObject("{=0D1ty8JA}Coordination", null), new TextObject("{=ZkkHPpT7}Your child excels at any task that requires {?CHILD.GENDER}her{?}him{\\?} to use {?CHILD.GENDER}her{?}his{\\?} hands.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Control }),
				new ValueTuple<TextObject, TextObject, CharacterAttribute[]>(new TextObject("{=JqaKRXNo}Craftiness", null), new TextObject("{=1KWhhurv}Your tutor throws up his hands in the air at {CHILD.NAME}'s ability to lie {?CHILD.GENDER}her{?}his{\\?} way out of trouble, but can't help but admire it a little.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Cunning }),
				new ValueTuple<TextObject, TextObject, CharacterAttribute[]>(new TextObject("{=la8jMuQ8}Energy", null), new TextObject("{=RUjPcZhF}{?PLAYER_CHILD}Your{?}The{\\?} child is rarely even winded after {?CHILD.GENDER}her{?}his{\\?} daily exercises.", null), new CharacterAttribute[] { DefaultCharacterAttributes.Endurance })
			};
			EducationCampaignBehavior.EducationCharacterProperties[] array = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp")
			};
			EducationCampaignBehavior.EducationCharacterProperties[] array2 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_numbers"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_genius"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce")
			};
			ValueTuple<TextObject, TextObject, CharacterAttribute[]>[] stage_1_page_2_options2 = stage_1_page_2_options;
			for (int i = 0; i < stage_1_page_2_options2.Length; i++)
			{
				stage_1_page_2_options2[i].Item2.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			for (int j = 0; j < stage_1_page_2_options.Length; j++)
			{
				int index = j;
				EducationCampaignBehavior.EducationCharacterProperties childProperties2 = array2[index];
				EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties = array[index];
				ValueTuple<TextObject, TextObject, CharacterAttribute[]> container = stage_1_page_2_options[j];
				EducationCampaignBehavior.EducationOption option = new EducationCampaignBehavior.EducationOption(container.Item1, container.Item2, null, delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
				{
					int num = previousOptions[0].RandomValue % stage_1_page_2_options.Length;
					int num2 = previousOptions[1].RandomValue % stage_1_page_2_options.Length;
					bool flag = (num + num2) % stage_1_page_2_options.Length == index;
					if (flag)
					{
						stage_1_page_2_description.SetTextVariable("ATR", container.Item3[0].Name);
					}
					return flag;
				}, null, container.Item3, null, childProperties2, specialCharacterProperties);
				educationPage3.AddOption(option);
			}
			return educationStage;
		}

		// Token: 0x06003DB6 RID: 15798 RVA: 0x0010DEA8 File Offset: 0x0010C0A8
		private EducationCampaignBehavior.EducationStage CreateStage8(Hero child)
		{
			TextObject title = new TextObject("{=CU3u0c02}Childhood", null);
			EducationCampaignBehavior.EducationStage educationStage = new EducationCampaignBehavior.EducationStage(EducationCampaignBehavior.ChildAgeState.Year8);
			TextObject description = new TextObject("{=lZSu0iOo}{CHILD.NAME} is now at an age when it is customary to assign a well-born child a tutor. You decided to entrust {?CHILD.GENDER}her{?}him{\\?} to your... ", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_inventory_idle_start", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage = educationStage.AddPage(0, title, description, this._chooseTutorText, childProperties, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			TextObject title2 = new TextObject("{=aZjz2GfX}Master-at-arms", null);
			TextObject title3 = new TextObject("{=stewardprofession}Steward", null);
			TextObject title4 = new TextObject("{=qfzkMuLj}Artisan", null);
			TextObject textObject = new TextObject("{=a869yxLt}{?IS_BATTANIA_OR_KHUZAIT}Lorekeeper{?}Scholar{\\?}", null);
			textObject.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", this.ChildCultureHasLorekeeper(child) ? 1 : 0);
			TextObject title5 = new TextObject("{=cpLFQzx0}Huntsman", null);
			TextObject textObject2 = new TextObject("{=vAjNG9yn}{?IS_BATTANIA}Bard{?}Minstrel{\\?}", null);
			textObject2.SetTextVariable("IS_BATTANIA", this.ChildCultureHasBard(child) ? 1 : 0);
			TextObject textObject3 = new TextObject("{=d2KpUKxc}The master-at-arms is responsible for the training and discipline of your troops. With his help {?PLAYER_CHILD}your{?}the{\\?} child should grow to be a strong warrior.", null);
			TextObject textObject4 = new TextObject("{=rB96005A}The steward is in charge of running the day-to-day affairs of your estate. {?PLAYER_CHILD}Your{?}The{\\?} child should learn early how people and supplies are managed.", null);
			TextObject textObject5 = new TextObject("{=9sdTLp49}The master artisan supervises the work of any smiths, carpenters or masons that you hire. {?PLAYER_CHILD}Your{?}The{\\?} child will join in the hard work and learn both stamina and craftsmanship.", null);
			TextObject textObject6 = new TextObject("{=G7yjkafk}The lorekeeper is responsible for teaching children the ancestral knowledge of the clan, including genealogy, law, and medicine. {?PLAYER_CHILD}Your{?}The{\\?} child should gain a respect for learning.", null);
			TextObject textObject7 = new TextObject("{=XD39Ra5X}The scholar advises a lord on subjects such as history, medicine, heraldry and even engineering. {?PLAYER_CHILD}Your{?}The{\\?} child should gain a respect for learning.", null);
			TextObject textObject8 = new TextObject("{=!}{INTELLIGENCE_DESC}", null);
			TextObject description2 = new TextObject("{=gqUtaPRM}The huntsman organizes hunts and keeps watch for poachers and thieves. Your child should learn how to handle hunting bows and crossbows and the basics of scouting and tracking.", null);
			TextObject textObject9 = new TextObject("{=yHB4plew}The {?IS_BATTANIA}bard{?}minstrel{\\?} sings and plays the lute, shawm or the vielle, and chants epic poems of daring deeds and impossible romances. They are also known to show their wards a bit about the seemier side of life.", null);
			textObject9.SetTextVariable("IS_BATTANIA", this.ChildCultureHasBard(child) ? 1 : 0);
			TextObject[] array = new TextObject[] { textObject3, textObject4, textObject5, textObject8, textObject6 };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			bool flag = this.ChildCultureHasLorekeeper(child);
			textObject8.SetTextVariable("INTELLIGENCE_DESC", flag ? textObject6 : textObject7);
			EducationCampaignBehavior.EducationCharacterProperties childProperties2 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms_2", "training_sword", false);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			EducationCampaignBehavior.EducationCharacterProperties childProperties3 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties2 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			EducationCampaignBehavior.EducationCharacterProperties childProperties4 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_artisan", "carry_linen", false);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties3 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			EducationCampaignBehavior.EducationCharacterProperties childProperties5 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_book", "character_creation_notebook", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties4 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			EducationCampaignBehavior.EducationCharacterProperties childProperties6 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties5 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			EducationCampaignBehavior.EducationCharacterProperties childProperties7 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties6 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			EducationCampaignBehavior.EducationOption stage_2_page_0_option_masterAtArms = new EducationCampaignBehavior.EducationOption(title2, textObject3, null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Vigor }, null, childProperties2, specialCharacterProperties);
			EducationCampaignBehavior.EducationOption stage_2_page_0_option_steward = new EducationCampaignBehavior.EducationOption(title3, textObject4, null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Social }, null, childProperties3, specialCharacterProperties2);
			EducationCampaignBehavior.EducationOption stage_2_page_0_option_artisan = new EducationCampaignBehavior.EducationOption(title4, textObject5, null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Endurance }, null, childProperties4, specialCharacterProperties3);
			EducationCampaignBehavior.EducationOption stage_2_page_0_option_intelligence = new EducationCampaignBehavior.EducationOption(textObject, textObject8, null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence }, null, childProperties5, specialCharacterProperties4);
			EducationCampaignBehavior.EducationOption stage_2_page_0_option_huntsman = new EducationCampaignBehavior.EducationOption(title5, description2, null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Control }, null, childProperties6, specialCharacterProperties5);
			EducationCampaignBehavior.EducationOption stage_2_page_0_option_cunning = new EducationCampaignBehavior.EducationOption(textObject2, textObject9, null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Cunning }, null, childProperties7, specialCharacterProperties6);
			educationPage.AddOption(stage_2_page_0_option_masterAtArms);
			educationPage.AddOption(stage_2_page_0_option_steward);
			educationPage.AddOption(stage_2_page_0_option_artisan);
			educationPage.AddOption(stage_2_page_0_option_intelligence);
			educationPage.AddOption(stage_2_page_0_option_huntsman);
			educationPage.AddOption(stage_2_page_0_option_cunning);
			TextObject description3 = new TextObject("{=ZjWDqx2Y}You trusted the child of the clan to the master-at-arms, an experienced warrior who no longer rides to the battlefield but has forgotten none of his skills. You had him to focus on...", null);
			EducationCampaignBehavior.EducationPage educationPage2 = educationStage.AddPage(1, title, description3, this._guideTutorText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_masterAtArms));
			EducationCampaignBehavior.EducationCharacterProperties childProperties8 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties9 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties10 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_focus", "training_sword", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties11 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce");
			EducationCampaignBehavior.EducationCharacterProperties childProperties12 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms_2", "training_sword", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties13 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties7 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties8 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties9 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties10 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties11 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties12 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor");
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Riding.Name, new TextObject("{=K08ed2LS}You asked your master-at-arms to make sure that as a noble, this {?CHILD.GENDER}daughter{?}son{\\?} of the clan knows how to hold {?CHILD.GENDER}herself{?}himself{\\?} properly on the saddle.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, childProperties8, specialCharacterProperties7));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Polearm.Name, new TextObject("{=LE0NCSPP}Every warrior needs to know the basics. Polearms are used by warriors of all classes, from feudal levies and urban militias to elite lancers.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Polearm }, childProperties9, specialCharacterProperties8));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.OneHanded.Name, new TextObject("{=PpuRM82X}The sword is the weapon of the noble warrior. It is useful both in the battlefield and in court, where it symbolizes a wearer's willingness to fight to protect {?CHILD.GENDER}her{?}his{\\?} honor.", null), null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, childProperties10, specialCharacterProperties9));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=C5UrdSgj}Fast legs and stamina are as vital to a warrior's survival as {?CHILD.GENDER}her{?}his{\\?} strength and skill.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Athletics }, childProperties11, specialCharacterProperties10));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.TwoHanded.Name, new TextObject("{=Adah5bay}The most powerful weapons require years of practice and conditioning to use properly. Their wielders need to start early.", null), null, null, null, null, new SkillObject[] { DefaultSkills.TwoHanded }, childProperties12, specialCharacterProperties11));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Bow.Name, new TextObject("{=QakanPLW}It will be years before the child can develop the strength, breathing techniques, and patience needed to wield a powerful bow, but it never hurts to start early.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Bow }, childProperties13, specialCharacterProperties12));
			foreach (EducationCampaignBehavior.EducationOption educationOption in educationPage2.Options)
			{
				educationOption.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject description4 = new TextObject("{=BTdTSzxF}You felt that your child should learn first and foremost how to manage the family property and govern your retainers.", null);
			EducationCampaignBehavior.EducationPage educationPage3 = educationStage.AddPage(1, title, description4, this._guideTutorText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_steward));
			EducationCampaignBehavior.EducationCharacterProperties childProperties14 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_numbers");
			EducationCampaignBehavior.EducationCharacterProperties childProperties15 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_memory");
			EducationCampaignBehavior.EducationCharacterProperties childProperties16 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3");
			EducationCampaignBehavior.EducationCharacterProperties childProperties17 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties18 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties19 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_decisive");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties13 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties14 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties15 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties16 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties17 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties18 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor");
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Steward.Name, new TextObject("{=XsQ5rabf}The first thing that a steward must know is to how to count add numbers together. ", null), null, null, null, null, new SkillObject[] { DefaultSkills.Steward }, childProperties14, specialCharacterProperties13));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Trade.Name, new TextObject("{=T0LaDdAs}It is important that the child of the clan learns about the goods and their prices, that kind of understanding will be useful for all {?CHILD.GENDER}her{?}his{\\?} life.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Trade }, childProperties15, specialCharacterProperties14));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Charm.Name, new TextObject("{=pHmz4GZN}Everything is easier when people are pleased by your presence. Proper grace and etiquette will be useful even among enemies.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties16, specialCharacterProperties15));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Riding.Name, new TextObject("{=hB72eE6b}Horses are among your clan's most valuable assets, so you encouraged your steward to take the child along on his frequent inspections of the stables.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, childProperties17, specialCharacterProperties16));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=N1WyvVHY}You instructed the child to pay close attention to how the steward exerts authority.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties18, specialCharacterProperties17));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Roguery.Name, new TextObject("{=SA4JbVo2}Your steward acts as magistrate in the lord's absence. {?PLAYER_CHILD}Your{?}The{\\?} child will sit at his side as he rules on land disputes, family feuds, and cases of alleged banditry, giving {?CHILD.GENDER}her{?}him{\\?} a look at the darker side of life in Calradia.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Roguery }, childProperties19, specialCharacterProperties18));
			foreach (EducationCampaignBehavior.EducationOption educationOption2 in educationPage3.Options)
			{
				educationOption2.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject description5 = new TextObject("{=6apaXF8k}You wanted this child to learn most from those who worked with their hands for a living. You asked an artisan in your employ to teach {?CHILD.GENDER}her{?}him{\\?}...", null);
			EducationCampaignBehavior.EducationPage educationPage4 = educationStage.AddPage(1, title, description5, this._guideTutorText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_artisan));
			EducationCampaignBehavior.EducationCharacterProperties childProperties20 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties21 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_memory");
			EducationCampaignBehavior.EducationCharacterProperties childProperties22 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms", "training_sword", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties23 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book_left", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties24 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties childProperties25 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_militia", "carry_hammer", false);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties19 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties20 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties21 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties22 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties23 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties24 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor");
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Crafting.Name, new TextObject("{=bx6Jhhui}The artisan should make sure that the child knows the basics of smithing, fletching and pole-turning, and that men who work with their hands take pride in their work.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Crafting }, childProperties20, specialCharacterProperties19));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Trade.Name, new TextObject("{=xHstOUBx}You made sure your artisan taught this child to recognize quality work, as well as its worth in denars.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Trade }, childProperties21, specialCharacterProperties20));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.OneHanded.Name, new TextObject("{=Ir7BP5bD}The best smiths are those that understand how their tools are used. Your child will be tasked with sparring with the weapons and testing their edges. ", null), null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, childProperties22, specialCharacterProperties21));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Engineering.Name, new TextObject("{=N0eHkPV2}A craftsman makes parts and an engineer fits them together. The best artisans learn to do both.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Engineering }, childProperties23, specialCharacterProperties22));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Steward.Name, new TextObject("{=tyzzUNN7}You instructed the artisan to teach your the child the basics of managing a workshop.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Steward }, childProperties24, specialCharacterProperties23));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=biqBLGwa}You told the artisan to make sure that {?PLAYER_CHILD}your{?}the{\\?} child works hard, and that {?CHILD.GENDER}she{?}he{\\?} swings the hammer for hours in the blazing heat of the forge. It tempers the soul, you believe.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Athletics }, childProperties25, specialCharacterProperties24));
			foreach (EducationCampaignBehavior.EducationOption educationOption3 in educationPage4.Options)
			{
				educationOption3.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject textObject10 = new TextObject("{=pb8fmbVl}You asked your {?IS_BATTANIA_OR_KHUZAIT}lorekeeper{?}scholar{\\?} to focus particularly on the art of...", null);
			textObject10.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", this.ChildCultureHasLorekeeper(child) ? 1 : 0);
			EducationCampaignBehavior.EducationPage educationPage5 = educationStage.AddPage(1, title, textObject10, this._guideTutorText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_intelligence));
			EducationCampaignBehavior.EducationCharacterProperties childProperties26 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3", "carry_book", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties27 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_clever_2", "carry_scroll", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties28 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties29 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties childProperties30 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_appearances");
			EducationCampaignBehavior.EducationCharacterProperties childProperties31 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties25 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties26 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties27 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties28 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties29 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties30 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor");
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Medicine.Name, new TextObject("{=5pGCtBhd}You asked that your child be schooled over the next few years in all the treatises that can be found on human body, its ailments, and their treatments.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Medicine }, childProperties26, specialCharacterProperties25));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Engineering.Name, new TextObject("{=CtImGA4A}Most tutors teach mathematics from tracing the path of stars in the sky, but you wanted your {?IS_BATTANIA_OR_KHUZAIT}lorekeeper{?}scholar{\\?} to focus on the more practical architectural treatises.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Engineering }, childProperties27, specialCharacterProperties26));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=Nn8bmGUX}You make sure that the child would be taught the deeds of the leaders of your people, and memorize the rhetoric they used to inspire their followers.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties28, specialCharacterProperties27));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Charm.Name, new TextObject("{=MgPubWOa}The epics are full of men and women who preferred words to the sword, and who could win the friendship of allies and the admiration of enemies.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties29, specialCharacterProperties28));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Steward.Name, new TextObject("{=c4NPLD0S}Any child of a noble clan needs to know how some kings and emperors accumulated wealth and how others squandered it.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Steward }, childProperties30, specialCharacterProperties29));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Tactics.Name, new TextObject("{=3iED3Ca9}To craft a tactic one must first have an idea of what has worked in the past and what has failed, and any commander should know the course and outcome of as many battles as possible.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties31, specialCharacterProperties30));
			foreach (EducationCampaignBehavior.EducationOption educationOption4 in educationPage5.Options)
			{
				educationOption4.Description.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", this.ChildCultureHasLorekeeper(child) ? 1 : 0);
				educationOption4.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject description6 = new TextObject("{=ed5JVSQm}You asked the huntsman to spend as much time as possible outdoors, accustomizing the child to the dangers and hardships of the wild. You asked him to teach the child...", null);
			EducationCampaignBehavior.EducationPage educationPage6 = educationStage.AddPage(1, title, description6, this._guideTutorText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_huntsman));
			EducationCampaignBehavior.EducationCharacterProperties childProperties32 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties33 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties34 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties35 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3", "carry_book", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties36 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties37 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties31 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties32 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties33 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties34 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties35 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties36 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor");
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Scouting.Name, new TextObject("{=lbHswWdb}Hunting is all about being aware of your surroundings and tracking your prey. You felt that the same skills would serve a lord well in journeys across the wild.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Scouting }, childProperties32, specialCharacterProperties31));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Bow.Name, new TextObject("{=ItS9F61H}You told {CHILD.NAME} that any young noble worth {?CHILD.GENDER}her{?}his{\\?} salt should be able to bring down a deer from fifty paces away, or hit a rabbit on the run.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Bow }, childProperties33, specialCharacterProperties32));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Polearm.Name, new TextObject("{=bZlhwK0a}Bows are all very well for small game, but those who pursue bears, boar or wolves bring spears along.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Polearm }, childProperties34, specialCharacterProperties33));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Medicine.Name, new TextObject("{=rPP0OSBm}Treating sprained ankles, broken bones and lamed horses are as much a part of a hunting expedition as skinning and gutting your quarry.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Medicine }, childProperties35, specialCharacterProperties34));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=EbRDo6Lv}A hunter should be able to endure long treks over rough ground and pursue a wounded quarry until it drops.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Athletics }, childProperties36, specialCharacterProperties35));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Tactics.Name, new TextObject("{=b9nvCgYF}A hunter knows how to prepare a trap and how to draw a prey to it, and, when dangerous predators are about, how to avoid becoming the hunted.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties37, specialCharacterProperties36));
			foreach (EducationCampaignBehavior.EducationOption educationOption5 in educationPage6.Options)
			{
				educationOption5.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject textObject11 = new TextObject("{=CZuYne8X}Singing, dancing and playing an instrument is important but the {?IS_BATTANIA}bard{?}minstrel{\\?} knows more. You asked him to make sure the child is also skilled in...", null);
			textObject11.SetTextVariable("IS_BATTANIA", this.ChildCultureHasBard(child) ? 1 : 0);
			EducationCampaignBehavior.EducationPage educationPage7 = educationStage.AddPage(1, title, textObject11, this._guideTutorText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_cunning));
			EducationCampaignBehavior.EducationCharacterProperties childProperties38 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties childProperties39 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce");
			EducationCampaignBehavior.EducationCharacterProperties childProperties40 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties41 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties42 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_throw", "spear_new_f_1-9m", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties43 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties37 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties38 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties39 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties40 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties41 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties42 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true);
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Charm.Name, new TextObject("{=fxDr44K4}A silvered tongue gets you far. You want {CHILD.NAME} to learn how to ferret out people's motivations, flatter their egos, and convince them that their interests are aligned.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties38, specialCharacterProperties37));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Athletics.Name, new TextObject("{=9m5M3Yf9}Some nobles may consider it vulgar and common, but the art of acrobatics is demanding and often has very practical applications.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Athletics }, childProperties39, specialCharacterProperties38));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Scouting.Name, new TextObject("{=dqg4nlo9}{?IS_BATTANIA}Bard{?}Minstrel{\\?}s frequently sing of nature and the hunt and are often great travellers, skilled at living rough on the land. The child was encouraged to accompany the {?IS_BATTANIA}bard{?}minstrel{\\?} on his roamings.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Scouting }, childProperties40, specialCharacterProperties39));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=KC7rLf4b}The {?IS_BATTANIA}bard{?}minstrel{\\?} had the child memorize the speeches of epic heroes, teaching {?CHILD.GENDER}her{?}him{\\?} how to appeal to your people's pride in their ancestors.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties41, specialCharacterProperties40));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Throwing.Name, new TextObject("{=1oPRugXl}Trick knife throws and swift evasions can entertain a marketplace crowd, but are useful on the battlefield as well. ", null), null, null, null, null, new SkillObject[] { DefaultSkills.Throwing }, childProperties42, specialCharacterProperties41));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Roguery.Name, new TextObject("{=XDafe4Bg}The {?IS_BATTANIA}bard{?}minstrel{\\?} will take the child along on his adventures in town and you've indicated that you'll turn a blind eye, considering it a useful skill to know how to get in and out of trouble.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Roguery }, childProperties43, specialCharacterProperties42));
			foreach (EducationCampaignBehavior.EducationOption educationOption6 in educationPage7.Options)
			{
				educationOption6.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
				educationOption6.Description.SetTextVariable("IS_BATTANIA", this.ChildCultureHasBard(child) ? 1 : 0);
			}
			TextObject description7 = new TextObject("{=KbnGyw0v}Your master-at-arms is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.", null);
			EducationCampaignBehavior.EducationPage currentPage = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_masterAtArms));
			List<SkillObject> skills = new List<SkillObject>
			{
				DefaultSkills.Riding,
				DefaultSkills.Athletics,
				DefaultSkills.Bow,
				DefaultSkills.Polearm,
				DefaultSkills.TwoHanded,
				DefaultSkills.OneHanded
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties44 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms_2", "training_sword", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_focus", "training_sword", false)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_closed_tutor")
			};
			this.Stage2Selection(skills, educationPage2, currentPage, childProperties44, tutorProperties);
			TextObject description8 = new TextObject("{=GvSVxyO0}Your steward is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.", null);
			EducationCampaignBehavior.EducationPage currentPage2 = educationStage.AddPage(2, title, description8, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_steward));
			List<SkillObject> skills2 = new List<SkillObject>
			{
				DefaultSkills.Riding,
				DefaultSkills.Steward,
				DefaultSkills.Trade,
				DefaultSkills.Charm,
				DefaultSkills.Leadership,
				DefaultSkills.Roguery
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties45 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_genius"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader_2"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties2 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident2_tutor")
			};
			this.Stage2Selection(skills2, educationPage3, currentPage2, childProperties45, tutorProperties2);
			TextObject description9 = new TextObject("{=kwgCnueo}Your master artisan is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.", null);
			EducationCampaignBehavior.EducationPage currentPage3 = educationStage.AddPage(2, title, description9, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_artisan));
			List<SkillObject> skills3 = new List<SkillObject>
			{
				DefaultSkills.Crafting,
				DefaultSkills.OneHanded,
				DefaultSkills.Trade,
				DefaultSkills.Engineering,
				DefaultSkills.Steward,
				DefaultSkills.Athletics
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties46 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_artisan", "carry_linen", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_focus", "training_sword", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_genius"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete")
			};
			EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties3 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_confident_tutor")
			};
			this.Stage2Selection(skills3, educationPage4, currentPage3, childProperties46, tutorProperties3);
			TextObject textObject12 = new TextObject("{=gp1qYbgb}Your {?IS_BATTANIA_OR_KHUZAIT}lorekeeper{?}scholar{\\?} is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.", null);
			textObject12.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", this.ChildCultureHasLorekeeper(child) ? 1 : 0);
			EducationCampaignBehavior.EducationPage educationPage8 = educationStage.AddPage(2, title, textObject12, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_intelligence));
			List<SkillObject> skills4 = new List<SkillObject>
			{
				DefaultSkills.Medicine,
				DefaultSkills.Charm,
				DefaultSkills.Tactics,
				DefaultSkills.Engineering,
				DefaultSkills.Steward,
				DefaultSkills.Leadership
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties47 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book_left", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_clever_2", "carry_scroll", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader_2")
			};
			EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties4 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_demure_tutor")
			};
			this.Stage2Selection(skills4, educationPage5, educationPage8, childProperties47, tutorProperties4);
			foreach (EducationCampaignBehavior.EducationOption educationOption7 in educationPage8.Options)
			{
				educationOption7.Title.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", this.ChildCultureHasLorekeeper(child) ? 1 : 0);
				educationOption7.Description.SetTextVariable("IS_BATTANIA_OR_KHUZAIT", this.ChildCultureHasLorekeeper(child) ? 1 : 0);
			}
			TextObject description10 = new TextObject("{=JGEQ68jc}Your huntsman is happy with the child's progress. He informs you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.", null);
			EducationCampaignBehavior.EducationPage currentPage4 = educationStage.AddPage(2, title, description10, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_huntsman));
			List<SkillObject> skills5 = new List<SkillObject>
			{
				DefaultSkills.Scouting,
				DefaultSkills.Bow,
				DefaultSkills.Polearm,
				DefaultSkills.Medicine,
				DefaultSkills.Athletics,
				DefaultSkills.Tactics
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties48 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book_left", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician")
			};
			EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties5 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hip_tutor")
			};
			this.Stage2Selection(skills5, educationPage6, currentPage4, childProperties48, tutorProperties5);
			TextObject textObject13 = new TextObject("{=iSppZBje}You don't need to know the full story of the child's escapades in the {?IS_BATTANIA}bard{?}minstrel{\\?}'s company, but the {?IS_BATTANIA}bard{?}minstrel{\\?} does inform you that {?CHILD.GENDER}she{?}he{\\?} shows some natural talent in {SKILL}.", null);
			textObject13.SetTextVariable("IS_BATTANIA", this.ChildCultureHasBard(child) ? 1 : 0);
			EducationCampaignBehavior.EducationPage educationPage9 = educationStage.AddPage(2, title, textObject13, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_2_page_0_option_cunning));
			List<SkillObject> skills6 = new List<SkillObject>
			{
				DefaultSkills.Scouting,
				DefaultSkills.Charm,
				DefaultSkills.Leadership,
				DefaultSkills.Throwing,
				DefaultSkills.Athletics,
				DefaultSkills.Roguery
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties49 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader_2"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_throw", "spear_new_f_1-9m", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] tutorProperties6 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_guitar", true)
			};
			this.Stage2Selection(skills6, educationPage7, educationPage9, childProperties49, tutorProperties6);
			foreach (EducationCampaignBehavior.EducationOption educationOption8 in educationPage9.Options)
			{
				educationOption8.Title.SetTextVariable("CUNNING_PROFESSION", this.ChildCultureHasBard(child) ? 1 : 0);
				educationOption8.Description.SetTextVariable("CUNNING_PROFESSION", this.ChildCultureHasBard(child) ? 1 : 0);
			}
			return educationStage;
		}

		// Token: 0x06003DB7 RID: 15799 RVA: 0x0010FC60 File Offset: 0x0010DE60
		private EducationCampaignBehavior.EducationStage CreateStage11(Hero child)
		{
			TextObject title = new TextObject("{=ok8lSW6M}Youth", null);
			EducationCampaignBehavior.EducationStage educationStage = new EducationCampaignBehavior.EducationStage(EducationCampaignBehavior.ChildAgeState.Year11);
			TextObject description = new TextObject("{=Rmbd2OkI}You are usually away from your estate, but when you are able to spend time with {CHILD.NAME}, you encouraged {?CHILD.GENDER}her{?}him{\\?} to develop...", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_inventory_idle_start", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage = educationStage.AddPage(0, title, description, this._chooseFocusText, childProperties, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties2 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete");
			EducationCampaignBehavior.EducationCharacterProperties childProperties3 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce");
			EducationCampaignBehavior.EducationCharacterProperties childProperties4 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_book", "notebook", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties5 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties childProperties6 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties7 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=m4kr4RXD}{?CHILD.GENDER}her{?}his{\\?} strength.", null), new TextObject("{=QbwNmxrn}You told {?CHILD.GENDER}her{?}him{\\?} that skill and brains are all very well, but in many situations there is no substitute for sheer brute might.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Vigor }, null, childProperties2, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=3fvj2Eti}{?CHILD.GENDER}her{?}his{\\?} endurance.", null), new TextObject("{=GARAmzFT}To thrive you must learn to travel faster, work longer, and fight harder than anyone who stands against you.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Endurance }, null, childProperties3, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=Gitd3DN8}a thirst for knowledge.", null), new TextObject("{=JHMHcZEp}You have seen many marvels on your travels and heard of many more, and one day, you believe, the philosophers of science will rule the world.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence }, null, childProperties4, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=7P3wiojT}a sharp mind.", null), new TextObject("{=78dta9Ys}You told {?CHILD.GENDER}her{?}him{\\?} that it is cheaper, safer and wiser to win a battle without fighting, if it is possible to do so.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Cunning }, null, childProperties5, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=LEoZ2DV5}a good eye and a steady hand.", null), new TextObject("{=8mk6gwvh}You told {?CHILD.GENDER}her{?}him{\\?} that a good eye can spot dangers from afar, and a steady hand can take them down before they get close.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Control }, null, childProperties6, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=ulFmGvtj}an interest in people.", null), new TextObject("{=HaAjWFrt}You told {?CHILD.GENDER}her{?}him{\\?} that understanding people's motivations and turning them to your advantage is the difference between a warrior and a king.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Social }, null, childProperties7, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject textObject = new TextObject("{=Y8jAaICu}One day {?PLAYER_CHILD}your{?}the{\\?} child asks you which of your skills was most useful to you. You thought for a while, and answered...", null);
			textObject.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			EducationCampaignBehavior.EducationPage educationPage2 = educationStage.AddPage(1, title, textObject, this._chooseSkillText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties8 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties9 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties childProperties10 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties childProperties11 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties12 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader_2");
			EducationCampaignBehavior.EducationCharacterProperties childProperties13 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_genius");
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Polearm.Name, new TextObject("{=3lDTSlpU}Even the most skilled swordsman or the strongest axe-wielder is to a lancer as the rabbit is to the eagle.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Polearm }, childProperties8, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Tactics.Name, new TextObject("{=2s6cH2kE}Even the finest swordsman can perish if his lord is ignorant of the difference between necessary risks and reckless, unnecessary ones.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties9, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Steward.Name, new TextObject("{=ePNba38W}Armies win battles, but farms and towns win wars.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Steward }, childProperties10, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Engineering.Name, new TextObject("{=bnPgkPv2}Towers, domes and gates are wonders that we must never take for granted.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Engineering }, childProperties11, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Leadership.Name, new TextObject("{=jqstr7ZL}Learn to fire men's pride, stoke their anger and dispel their fears, and they will turn from men into lions who will do anything for you.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties12, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(DefaultSkills.Trade.Name, new TextObject("{=T3vJp4PG}Whoever grasps how prices work can pluck silver out of thin air.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Trade }, childProperties13, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject textObject2 = new TextObject("{=LUro3Sqr}While spending time with {?PLAYER_CHILD}your{?}the{\\?} child, you notice that {?CHILD.GENDER}she{?}he{\\?} shows real ability in {RANDOM_OUTCOME}.", null);
			textObject2.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			EducationCampaignBehavior.EducationPage stage_3_page_2 = educationStage.AddPage(2, title, textObject2, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			ValueTuple<TextObject, TextObject, SkillObject>[] stage_3_page_2_options = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(DefaultSkills.Athletics.Name, new TextObject("{=tMUla7vz}{?CHILD.GENDER}She{?}He{\\?} is light but strong, and can outrun almost all of {?CHILD.GENDER}her{?}his{\\?} peers.", null), DefaultSkills.Athletics),
				new ValueTuple<TextObject, TextObject, SkillObject>(DefaultSkills.Riding.Name, new TextObject("{=MdjZ56VR}{?CHILD.GENDER}She{?}He{\\?} sits on the saddle so comfortably, like {?CHILD.GENDER}she{?}he{\\?} was born on it.", null), DefaultSkills.Riding),
				new ValueTuple<TextObject, TextObject, SkillObject>(DefaultSkills.Crafting.Name, new TextObject("{=OrbOwQGB}Sometimes you spot {?PLAYER_CHILD}your{?}the{\\?} child carving a piece of wood into a face or a fantastic beast, and you could not help but notice the subtle mastery of {?CHILD.GENDER}her{?}his{\\?} hands.", null), DefaultSkills.Crafting),
				new ValueTuple<TextObject, TextObject, SkillObject>(DefaultSkills.Medicine.Name, new TextObject("{=6EaKsRtu}Accidents are inevitable, and {?PLAYER_CHILD}your{?}the{\\?} child isn't bothered by the sight of blood. {?CHILD.GENDER}She{?}He{\\?} acts quickly to staunch bleeding and prevent infection.", null), DefaultSkills.Medicine),
				new ValueTuple<TextObject, TextObject, SkillObject>(DefaultSkills.Scouting.Name, new TextObject("{=7Hd5yW7B}{?CHILD.GENDER}She{?}He{\\?} is at home in the wilderness. {?CHILD.GENDER}She{?}He{\\?} moves like a cat, and has very keen ears and eyes.", null), DefaultSkills.Scouting),
				new ValueTuple<TextObject, TextObject, SkillObject>(DefaultSkills.Charm.Name, new TextObject("{=0FIbRZsi}You can not help but notice how people are put at ease by {?PLAYER_CHILD}your {?CHILD.GENDER}daughter{?}son{\\?}{?}the {?CHILD.GENDER}girl{?}boy{\\?}{\\?} and seek {?CHILD.GENDER}her{?}his{\\?} company.", null), DefaultSkills.Charm)
			};
			ValueTuple<TextObject, TextObject, SkillObject>[] stage_3_page_2_options2 = stage_3_page_2_options;
			for (int i = 0; i < stage_3_page_2_options2.Length; i++)
			{
				stage_3_page_2_options2[i].Item2.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			EducationCampaignBehavior.EducationCharacterProperties[] array = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3", "carry_book", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners")
			};
			for (int j = 0; j < stage_3_page_2_options.Length; j++)
			{
				int index = j;
				EducationCampaignBehavior.EducationCharacterProperties childProperties14 = array[index];
				ValueTuple<TextObject, TextObject, SkillObject> valueTuple = stage_3_page_2_options[index];
				TextObject optionTitle = valueTuple.Item1;
				TextObject item = valueTuple.Item2;
				SkillObject item2 = valueTuple.Item3;
				EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate condition = delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
				{
					int num = previousOptions[0].RandomValue % stage_3_page_2_options.Length;
					int num2 = previousOptions[1].RandomValue % stage_3_page_2_options.Length;
					bool flag = (num + num2) % stage_3_page_2_options.Length == index;
					if (flag)
					{
						stage_3_page_2.Description.SetTextVariable("RANDOM_OUTCOME", optionTitle);
					}
					return flag;
				};
				stage_3_page_2.AddOption(new EducationCampaignBehavior.EducationOption(optionTitle, item, null, condition, null, null, new SkillObject[] { item2 }, childProperties14, default(EducationCampaignBehavior.EducationCharacterProperties)));
			}
			return educationStage;
		}

		// Token: 0x06003DB8 RID: 15800 RVA: 0x00110388 File Offset: 0x0010E588
		private EducationCampaignBehavior.EducationStage CreateStage14(Hero child)
		{
			TextObject title = new TextObject("{=rcoueCmk}Adolescence", null);
			EducationCampaignBehavior.EducationStage educationStage = new EducationCampaignBehavior.EducationStage(EducationCampaignBehavior.ChildAgeState.Year14);
			TextObject description = new TextObject("{=3O1Pg3Ie}At {?CHILD.GENDER}her{?}his{\\?} 14th birthday you gave {CHILD.NAME} a special present. You have seen {?CHILD.GENDER}her{?}him{\\?} treasure it and believe it will shape who {?CHILD.GENDER}she{?}he{\\?} is. You gave {?CHILD.GENDER}her{?}him{\\?}...", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_inventory_idle_start", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage = educationStage.AddPage(0, title, description, this._chooseGiftText, childProperties, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties2 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms", "vlandia_twohanded_sword_c", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties3 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties4 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_clever_2", "carry_scroll", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties5 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_sharp", "carry_game_left", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties6 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties7 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_appearances");
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=BYadInfL}a well-balanced sword.", null), new TextObject("{=K31F1yPI}When you pick up this blade you can almost feel it dance in your hand. Its edge can cut through a stout poll or a human hair drifting on the wind. ", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Vigor }, new SkillObject[] { DefaultSkills.OneHanded }, childProperties2, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=l5aOTKUi}a magnificent steed.", null), new TextObject("{=DfbYtRyj}You had second thoughts about giving {?PLAYER_CHILD}your{?}the{\\?} child such a spirited animal, but {?CHILD.GENDER}she{?}he{\\?} lept upon its back and galloped like the wind across your pastures.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Endurance }, new SkillObject[] { DefaultSkills.Riding }, childProperties3, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=bwYRMGIN}a treatise on siegecraft.", null), new TextObject("{=cr2jmYxg}You remember poring over the hand-drawn schematics of mangonels and towers for hours, imagining that one day you might build one of these awesome instruments of destruction yourself.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence }, new SkillObject[] { DefaultSkills.Engineering }, childProperties4, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=iMtCbEb3}a finely carved gameboard.", null), new TextObject("{=fzxJZZEH}Each piece is a work of art. Even without an opponent one could set it up and gaze upon it for hours, contemplating moves and counter-moves.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Cunning }, new SkillObject[] { DefaultSkills.Tactics }, childProperties5, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=ocl6ECmt}a well-tempered bow.", null), new TextObject("{=S3RZLOsv}You can sense the power in the laminated sinews and wood of this weapon. It was made for the calloused hands of a veteran archer and {CHILD.NAME} may need years to be able to fully draw it back, but you know {?CHILD.GENDER}she{?}he{\\?} will be motivated to master it.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Control }, new SkillObject[] { DefaultSkills.Bow }, childProperties6, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=UpgqdJC0}a trip to your realm's court.", null), new TextObject("{=bTj16PqR}Every well-born youth wants to see the center of it all, where the lords and ladies gather in splendor to converse and connive. You invited the child to see the spectacle first hand, and provided the elegant clothes {?CHILD.GENDER}she{?}he{\\?}'d need to be part of it. ", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Social }, new SkillObject[] { DefaultSkills.Charm }, childProperties7, default(EducationCampaignBehavior.EducationCharacterProperties)));
			foreach (EducationCampaignBehavior.EducationOption educationOption in educationPage.Options)
			{
				educationOption.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject description2 = new TextObject("{=GOt2uRcJ}In adolescence, {?CHILD.GENDER}she{?}he{\\?} began to take on serious responsibilities and compete with adults as a near-equal. {?CHILD.GENDER}She{?}He{\\?} managed to...", null);
			EducationCampaignBehavior.EducationPage educationPage2 = educationStage.AddPage(1, title, description2, this._chooseAchievementText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties8 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties9 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties10 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties11 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3", "carry_book", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties12 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties13 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_clever_2");
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=IMaTTgPJ}defeat {?CHILD.GENDER}her{?}his{\\?} fencing instructor.", null), new TextObject("{=cnbeJh6Y}After many tries {?CHILD.GENDER}she{?}he{\\?} successfully beat {?CHILD.GENDER}her{?}his{\\?} tutor, fair and square, during sparring.", null), null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, childProperties8, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=EP0gEG0L}win a race.", null), new TextObject("{=7mbE4z2v}{?CHILD.GENDER}She{?}He{\\?} won a friendly horse racing competition, and was rewarded with a magnificent saddle.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, childProperties9, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=ksXTQXOX}craft a weapon.", null), new TextObject("{=6WUaFzlv}{?PLAYER_CHILD}Your{?}The{\\?} child forged a sword - blade, hilt and pommel. The artisan said that he has never seen such dedication and patience in one so young.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Engineering }, childProperties10, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=bX841Jau}learn the arts of healing.", null), new TextObject("{=3hFT34l1}{?PLAYER_CHILD}Your{?}The{\\?} child helps out the local physician, binding and cleaning wounds and, when the master is absent, prescribing remedies.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Medicine }, childProperties11, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=ltbU06Fi}become a crack shot.", null), new TextObject("{=7JoiyH1Y}You heard that {CHILD.NAME} could put an arrow through an arm ring from 100 paces away, but you didn't believe it until you saw it.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Bow }, childProperties12, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=pHbnAZtt}trade like a veteran merchant.", null), new TextObject("{=oaXAY8bw}{?PLAYER_CHILD}Your{?}The{\\?} child asked to borrow money to trade with a passing caravan. You figure {?CHILD.GENDER}she{?}he{\\?}'d be sharped and learn a lesson, but in fact the {?CHILD.GENDER}girl{?}boy{\\?} secured a very lucrative deal.", null), null, null, null, null, new SkillObject[] { DefaultSkills.Trade }, childProperties13, default(EducationCampaignBehavior.EducationCharacterProperties)));
			foreach (EducationCampaignBehavior.EducationOption educationOption2 in educationPage2.Options)
			{
				educationOption2.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			}
			TextObject stage_4_page_2_description = new TextObject("{=jukOfT2Z}Outside events also intruded on {CHILD.NAME}'s adolescence. You believe {?CHILD.GENDER}she{?}he{\\?} was particularly shaped by {RANDOM_OUTCOME}. This event increased {?CHILD.GENDER}her{?}his{\\?} skills in {SKILL_1} and {SKILL_2}.", null);
			EducationCampaignBehavior.EducationPage educationPage3 = educationStage.AddPage(2, title, stage_4_page_2_description, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), null);
			ValueTuple<TextObject, TextObject, SkillObject[]>[] stage_4_page_2_option = new ValueTuple<TextObject, TextObject, SkillObject[]>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject[]>(new TextObject("{=43zzbI1J}enemy incursions", null), new TextObject("{=AaahFmxv}War was never too far away. On more than one occasion, {CHILD.NAME} spotted foes on the horizon and needed to ride away as quickly as possible.", null), new SkillObject[]
				{
					DefaultSkills.Athletics,
					DefaultSkills.Riding
				}),
				new ValueTuple<TextObject, TextObject, SkillObject[]>(new TextObject("{=y9RcWMbQ}a local rivalry", null), new TextObject("{=R7A3aPwi}A neighboring noble's brutish son took an interest in persecuting {CHILD.NAME}, but the young {?CHILD.GENDER}woman{?}man{\\?} gave him reason to look elsewhere for his prey.", null), new SkillObject[]
				{
					DefaultSkills.OneHanded,
					DefaultSkills.TwoHanded
				}),
				new ValueTuple<TextObject, TextObject, SkillObject[]>(new TextObject("{=gHgUwteP}natural disasters", null), new TextObject("{=lDMrl7sd}Your district saw more than its share of floods and fires, and {CHILD.NAME} joined in the effort to stem the destruction and rebuild afterwards.", null), new SkillObject[]
				{
					DefaultSkills.Crafting,
					DefaultSkills.Engineering
				}),
				new ValueTuple<TextObject, TextObject, SkillObject[]>(new TextObject("{=mzu6exTe}an outbreak of plague", null), new TextObject("{=kAtxHSu9}A great illness swept through your lands. Your child applied what {?CHILD.GENDER}she{?}he{\\?} had read in books on nursing fevers and isolating the sick to minimize deaths on your estate.", null), new SkillObject[]
				{
					DefaultSkills.Medicine,
					DefaultSkills.Steward
				}),
				new ValueTuple<TextObject, TextObject, SkillObject[]>(new TextObject("{=Jc1YXXjN}an influx of wild beasts", null), new TextObject("{=MX7toYav}{CHILD.NAME} joined a hunting pursuit in pursuit of a pack of wolves who had been ravaging the local livestock. {?CHILD.GENDER}She{?}He{\\?} tracked and took down one of the beasts with an arrow.", null), new SkillObject[]
				{
					DefaultSkills.Scouting,
					DefaultSkills.Bow
				}),
				new ValueTuple<TextObject, TextObject, SkillObject[]>(new TextObject("{=aiBQo2MR}an outbreak of unrest", null), new TextObject("{=yym5kCG5}After a particularly hard winter your tenants began to murmur about rising up and seizing your granaries. {CHILD.NAME} convinced them to be patient and wait for relief.", null), new SkillObject[]
				{
					DefaultSkills.Charm,
					DefaultSkills.Leadership
				})
			};
			EducationCampaignBehavior.EducationCharacterProperties[] array = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms_2", "vlandia_twohanded_sword_c", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book_left", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_honor")
			};
			for (int i = 0; i < stage_4_page_2_option.Length; i++)
			{
				int index = i;
				EducationCampaignBehavior.EducationCharacterProperties childProperties14 = array[index];
				ValueTuple<TextObject, TextObject, SkillObject[]> valueTuple = stage_4_page_2_option[index];
				TextObject optionTitle = valueTuple.Item1;
				TextObject item = valueTuple.Item2;
				SkillObject[] skills = valueTuple.Item3;
				EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate condition = delegate(EducationCampaignBehavior.EducationOption o, List<EducationCampaignBehavior.EducationOption> previousOptions)
				{
					int num = previousOptions[0].RandomValue % stage_4_page_2_option.Length;
					int num2 = previousOptions[1].RandomValue % stage_4_page_2_option.Length;
					bool flag = (num + num2) % stage_4_page_2_option.Length == index;
					if (flag)
					{
						stage_4_page_2_description.SetTextVariable("RANDOM_OUTCOME", optionTitle);
						stage_4_page_2_description.SetTextVariable("SKILL_1", skills[0].Name);
						stage_4_page_2_description.SetTextVariable("SKILL_2", skills[1].Name);
					}
					return flag;
				};
				educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(optionTitle, item, null, condition, null, null, skills, childProperties14, default(EducationCampaignBehavior.EducationCharacterProperties)));
			}
			return educationStage;
		}

		// Token: 0x06003DB9 RID: 15801 RVA: 0x00110BD0 File Offset: 0x0010EDD0
		private EducationCampaignBehavior.EducationStage CreateStage16(Hero child)
		{
			TextObject title = new TextObject("{=Ww3uU5e6}Young Adulthood", null);
			EducationCampaignBehavior.EducationStage educationStage = new EducationCampaignBehavior.EducationStage(EducationCampaignBehavior.ChildAgeState.Year16);
			TextObject textObject = new TextObject("{=yJ0XRD9g}Eventually it was time for {?PLAYER_CHILD}your{?}the{\\?} child to travel far from home. You sent the young {?CHILD.GENDER}woman{?}man{\\?} away...", null);
			textObject.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
			EducationCampaignBehavior.EducationCharacterProperties childProperties = new EducationCampaignBehavior.EducationCharacterProperties("act_inventory_idle_start", string.Empty, false);
			EducationCampaignBehavior.EducationPage educationPage = educationStage.AddPage(0, title, textObject, this._chooseTaskText, childProperties, default(EducationCampaignBehavior.EducationCharacterProperties), null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties2 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties3 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_genius");
			EducationCampaignBehavior.EducationCharacterProperties childProperties4 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties5 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_honor");
			EducationCampaignBehavior.EducationCharacterProperties childProperties6 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties7 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationOption stage_5_page_0_warriorOption = new EducationCampaignBehavior.EducationOption(new TextObject("{=Tbv2txJV}as a squire.", null), new TextObject("{=4CuMDzvd}You asked your best warrior to take {?CHILD.GENDER}her{?}him{\\?} under his wings and make sure {?CHILD.GENDER}she{?}he{\\?} gets the taste of battle without being seriously harmed.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Vigor }, new SkillObject[] { DefaultSkills.OneHanded }, childProperties2, default(EducationCampaignBehavior.EducationCharacterProperties));
			EducationCampaignBehavior.EducationOption stage_5_page_0_merchantOption = new EducationCampaignBehavior.EducationOption(new TextObject("{=N62qjb8s}as an aide.", null), new TextObject("{=rZkQfBxt}You asked one of the local merchants to take the young {?CHILD.GENDER}woman{?}man{\\?} along with one of his caravans and make {?CHILD.GENDER}her{?}him{\\?} learn the secrets of the trade.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Cunning }, new SkillObject[] { DefaultSkills.Trade }, childProperties3, default(EducationCampaignBehavior.EducationCharacterProperties));
			EducationCampaignBehavior.EducationOption stage_5_page_0_siegeEngineerOption = new EducationCampaignBehavior.EducationOption(new TextObject("{=ezfehKrT}as an apprentice.", null), new TextObject("{=DOhOpmhV}You found {CHILD.NAME} an apprenticeship with a siege engineer. Even if it can be a bit dangerous, there's no substitute for that kind of practical experience.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Intelligence }, new SkillObject[] { DefaultSkills.Engineering }, childProperties4, default(EducationCampaignBehavior.EducationCharacterProperties));
			EducationCampaignBehavior.EducationOption stage_5_page_0_nobleOption = new EducationCampaignBehavior.EducationOption(new TextObject("{=SF244Jxx}as a noble's ward.", null), new TextObject("{=KgJaq5dx}You sent the young {?CHILD.GENDER}woman{?}man{\\?} to the hall of one of your fellow lords. There {?CHILD.GENDER}she{?}he{\\?} will learn how a different clan runs its affairs.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Control }, new SkillObject[] { DefaultSkills.Bow }, childProperties5, default(EducationCampaignBehavior.EducationCharacterProperties));
			EducationCampaignBehavior.EducationOption stage_5_page_0_ownWayOption = new EducationCampaignBehavior.EducationOption(new TextObject("{=sdQoXkKq}to find {?CHILD.GENDER}her{?}his{\\?} own way.", null), new TextObject("{=6q8tY6Sz}You remember your freebooting days fondly. You want {CHILD.NAME} to experience life as you did.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Endurance }, new SkillObject[] { DefaultSkills.Athletics }, childProperties6, default(EducationCampaignBehavior.EducationCharacterProperties));
			EducationCampaignBehavior.EducationOption stage_5_page_0_diplomatOption = new EducationCampaignBehavior.EducationOption(new TextObject("{=bai3aiDB}as an envoy.", null), new TextObject("{=hjOxidJj}The young {?CHILD.GENDER}woman{?}man{\\?} will spend {?CHILD.GENDER}her{?}his{\\?} time at a diplomatic mission, where {?CHILD.GENDER}she{?}he{\\?} will have chance to interact with people from foreign lands.", null), null, null, null, new CharacterAttribute[] { DefaultCharacterAttributes.Social }, new SkillObject[] { DefaultSkills.Charm }, childProperties7, default(EducationCampaignBehavior.EducationCharacterProperties));
			educationPage.AddOption(stage_5_page_0_warriorOption);
			educationPage.AddOption(stage_5_page_0_merchantOption);
			educationPage.AddOption(stage_5_page_0_siegeEngineerOption);
			educationPage.AddOption(stage_5_page_0_nobleOption);
			educationPage.AddOption(stage_5_page_0_ownWayOption);
			educationPage.AddOption(stage_5_page_0_diplomatOption);
			TextObject description = new TextObject("{=V0QmrGda}Before {CHILD.NAME} left, you asked your warrior to make sure that the young {?CHILD.GENDER}woman{?}man{\\?}...", null);
			EducationCampaignBehavior.EducationPage educationPage2 = educationStage.AddPage(1, title, description, this._chooseRequestText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_warriorOption));
			TextObject textObject2 = new TextObject("{=WJSapWab}{?CHILD.GENDER}She{?}He{\\?} had a chance to fight in a few minor skirmishes without getting seriously injured.", null);
			TextObject textObject3 = new TextObject("{=La7wkS5M}The young {?CHILD.GENDER}woman{?}man{\\?} defeated several opponents and did honor to your clan's name.", null);
			TextObject textObject4 = new TextObject("{=fsr2yhrr}While there were few military feats to accomplish, {CHILD.NAME} still became quite popular among the troops and improved their morale in the process.", null);
			TextObject textObject5 = new TextObject("{=CpcWqufH}The young {?CHILD.GENDER}woman{?}man{\\?} was placed in command of a small group of militiamen, including some nearly twice {?CHILD.GENDER}her{?}his{\\?} age, and earned their respect.", null);
			TextObject textObject6 = new TextObject("{=VTzvlEV8}Sparring with total strangers, including some who would happily break {?CHILD.GENDER}her{?}his{\\?} teeth if {?CHILD.GENDER}she{?}he{\\?} let them, was a new and valuable experience for {CHILD.NAME}.", null);
			TextObject textObject7 = new TextObject("{=FekWDIUm}{?CHILD.GENDER}She{?}He{\\?} helped plan a small expedition in pursuit of a group of brigands.", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties8 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce", "vlandia_twohanded_sword_c", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties9 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties10 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_militia", "vlandia_twohanded_sword_c", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties11 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader_2");
			EducationCampaignBehavior.EducationCharacterProperties childProperties12 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms_2", "vlandia_twohanded_sword_c", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties13 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=6LpwsxYP}gets bloodied.", null), textObject2, null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, childProperties8, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=hes54oh0}joins in a tournament.", null), textObject3, null, null, null, null, new SkillObject[] { DefaultSkills.Polearm }, childProperties9, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=z50GdIa3}learns to inspire the soldiers.", null), textObject4, null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties10, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=MpdPpIEY}leads a patrol.", null), textObject5, null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties11, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=flCA80Ex}trains really hard.", null), textObject6, null, null, null, null, new SkillObject[] { DefaultSkills.TwoHanded }, childProperties12, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage2.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=UuQDXIFD}hunts down bandits.", null), textObject7, null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties13, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject description2 = new TextObject("{=cDVNFn4p}Before {CHILD.NAME} left, you told the merchant that {?CHILD.GENDER}she{?}he{\\?} should be entrusted with...", null);
			EducationCampaignBehavior.EducationPage educationPage3 = educationStage.AddPage(1, title, description2, this._chooseRequestText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_merchantOption));
			TextObject textObject8 = new TextObject("{=rVIzHhLO}The young {?CHILD.GENDER}woman{?}man{\\?}'s social graces helped put other merchants at ease as they exchanged information and worked out deals.", null);
			TextObject textObject9 = new TextObject("{=UYdvhdDl}When cargo went missing, the young {?CHILD.GENDER}woman{?}man{\\?} made the inquiries at the local black market that were necessary to get it back.", null);
			TextObject textObject10 = new TextObject("{=bCZbgrLU}The smith came down sick and the young {?CHILD.GENDER}woman{?}man{\\?} had to quickly learn how to craft horseshoes and nails.", null);
			TextObject textObject11 = new TextObject("{=1vQul57Y}The caravan got lost and it was {CHILD.NAME} who spotted the landmarks that got it back on track.", null);
			TextObject textObject12 = new TextObject("{=cyQXm7VA}The young {?CHILD.GENDER}woman{?}man{\\?} was charged with ensuring they had the food, saddles, animals, teamsters and guards for the journey.", null);
			TextObject textObject13 = new TextObject("{=egXc7J72}Caravan guards are not elite troops, and may slack off if not held to a high standard of discipline. {CHILD.NAME} made sure that they stayed sober and alert.", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties14 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties childProperties15 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce", "blacksmith_sword", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties16 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_artisan", "carry_linen", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties17 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			EducationCampaignBehavior.EducationCharacterProperties childProperties18 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties childProperties19 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader");
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=9NbAzOER}dealing with business partners.", null), textObject8, null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties14, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=QVZMiaV2}helping recover stolen goods.", null), textObject9, null, null, null, null, new SkillObject[] { DefaultSkills.Roguery }, childProperties15, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=hfaw7Xx9}helping the artisan.", null), textObject10, null, null, null, null, new SkillObject[] { DefaultSkills.Crafting }, childProperties16, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=XYRZIPD8}guiding the caravan.", null), textObject11, null, null, null, null, new SkillObject[] { DefaultSkills.Scouting }, childProperties17, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=JX13Wd65}managing the logistics of travel.", null), textObject12, null, null, null, null, new SkillObject[] { DefaultSkills.Steward }, childProperties18, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage3.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=rvqVuM5o}supervising the guards.", null), textObject13, null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties19, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject description3 = new TextObject("{=U9nsz7pg}Before {CHILD.NAME} left, you asked the siege engineer to make sure that the young {?CHILD.GENDER}woman{?}man{\\?}...", null);
			EducationCampaignBehavior.EducationPage educationPage4 = educationStage.AddPage(1, title, description3, this._chooseRequestText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_siegeEngineerOption));
			TextObject textObject14 = new TextObject("{=MUaFiBva}{CHILD.NAME} paid close attention to the experienced engineers and became competent in a profession where small mistakes can have deadly consequences.", null);
			TextObject textObject15 = new TextObject("{=3ZZtAYXm}The young {?CHILD.GENDER}woman{?}man{\\?} learned to treat the mangled fingers and concussions without which no siege is complete.", null);
			TextObject textObject16 = new TextObject("{=VVBx07cK}The siege engines needed gears and bracings, and the master assigned {?PLAYER_CHILD}your{?}the{\\?} child to assist in forging them.", null);
			TextObject textObject17 = new TextObject("{=oCiWOutS}Sieges required tools, oil, good quality timber and, of course, food. It was young {?CHILD.GENDER}woman{?}man{\\?}'s responsibility to have them gathered, stocked and distributed.", null);
			TextObject textObject18 = new TextObject("{=ijootCu7}It was easy to get bogged down in the details of hurling rocks and assembling towers, but {CHILD.NAME} kept a keen eye on why some sieges succeeded and others failed.", null);
			TextObject textObject19 = new TextObject("{=igkNYcSI}The young {?CHILD.GENDER}woman{?}man{\\?} tells you how engineers have become an unofficial guild that transcends borders. Even men on opposite sides of a siege are known to discuss the technical details of their craft.", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties20 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book_left", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties21 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_gracious", "carry_book", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties22 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties23 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_peddlers_2", "carry_sticks", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties24 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties childProperties25 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=L1o5mb69}learns to construct siege engines.", null), textObject14, null, null, null, null, new SkillObject[] { DefaultSkills.Engineering }, childProperties20, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=74pYaU18}treats injuries.", null), textObject15, null, null, null, null, new SkillObject[] { DefaultSkills.Medicine }, childProperties21, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=vsFVlFa4}assists the smiths.", null), textObject16, null, null, null, null, new SkillObject[] { DefaultSkills.Crafting }, childProperties22, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=jL8ntljn}procures supplies.", null), textObject17, null, null, null, null, new SkillObject[] { DefaultSkills.Trade }, childProperties23, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=I6oqLFXh}focuses on the big picture.", null), textObject18, null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties24, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage4.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=XTmpkNNg}socializes with other engineers.", null), textObject19, null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties25, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject description4 = new TextObject("{=53xrtZst}Before {CHILD.NAME} left, you asked the lord to make sure that the young {?CHILD.GENDER}woman{?}man{\\?}...", null);
			EducationCampaignBehavior.EducationPage educationPage5 = educationStage.AddPage(1, title, description4, this._chooseRequestText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_nobleOption));
			TextObject textObject20 = new TextObject("{=aCO0Md8z}When {?CHILD.GENDER}she{?}he{\\?} was ambused on patrol, {CHILD.NAME} fought the attackers off with sheer willpower.", null);
			TextObject textObject21 = new TextObject("{=0XOii9tg}The noble did not let {CHILD.NAME} go hand-to-hand with other warriors, but the young {?CHILD.GENDER}woman{?}man{\\?} joined the archers as they traded shots with enemy scouts.", null);
			TextObject textObject22 = new TextObject("{=9BoHka98}The young {?CHILD.GENDER}woman{?}man{\\?} pursued enemy scouts while avoiding their main force. There was little fighting but a great deal of riding.", null);
			TextObject textObject23 = new TextObject("{=WD6soDM1}It was a minor skirmish but a clear victory nonetheless. {CHILD.NAME} wasn't just there when the commander came up with the winning strategy but also took part in executing it.", null);
			TextObject textObject24 = new TextObject("{=1NCVDNri}The noble gave the young adult command of a small group of scouts. {?CHILD.GENDER}She{?}He{\\?} took them on patrol and even though some were twice {?CHILD.GENDER}her{?}his{\\?} age, {?CHILD.GENDER}she{?}he{\\?} won their respect.", null);
			TextObject textObject25 = new TextObject("{=EXwX0zrx}The young {?CHILD.GENDER}woman{?}man{\\?} faced a rival clan's outrider in single combat, and handed him a defeat that he wouldn't forget.", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties26 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete");
			EducationCampaignBehavior.EducationCharacterProperties childProperties27 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready_bow", "bow_roman_a", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties28 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties29 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			EducationCampaignBehavior.EducationCharacterProperties childProperties30 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_leader_2");
			EducationCampaignBehavior.EducationCharacterProperties childProperties31 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_fierce");
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=AWuEV99O}unleashes {?CHILD.GENDER}her{?}his{\\?} fighting spirit.", null), textObject20, null, null, null, null, new SkillObject[] { DefaultSkills.Athletics }, childProperties26, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=Mk6dZBQa}skirmishes from a distance.", null), textObject21, null, null, null, null, new SkillObject[] { DefaultSkills.Bow }, childProperties27, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=Mp7dT2u2}chases and is chased.", null), textObject22, null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, childProperties28, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=4pOb934Y}understands how a victory is won.", null), textObject23, null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties29, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=eXLxHSls}leads men into enemy territory.", null), textObject24, null, null, null, null, new SkillObject[] { DefaultSkills.Leadership }, childProperties30, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage5.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=1BFk0wAV}defeats an enemy.", null), textObject25, null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, childProperties31, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject description5 = new TextObject("{=4Bl1LWmZ}Before {CHILD.NAME} left, you encouraged {?CHILD.GENDER}her{?}him{\\?} to...", null);
			EducationCampaignBehavior.EducationPage educationPage6 = educationStage.AddPage(1, title, description5, this._chooseRequestText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_ownWayOption));
			TextObject textObject26 = new TextObject("{=WL1zcgYx}{?CHILD.GENDER}She{?}He{\\?} travelled on foot in the company of other pilgrims, often relying on the kindness of strangers for food and shelter.", null);
			TextObject textObject27 = new TextObject("{=K1HrvDLO}{?CHILD.GENDER}She{?}He{\\?} won't talk much about it, but you know that {?CHILD.GENDER}she{?}he{\\?} has seen the darker side of Calradia.", null);
			TextObject textObject28 = new TextObject("{=Fn7Pgia7}{?CHILD.GENDER}She{?}He{\\?} told you that {?CHILD.GENDER}her{?}his{\\?} skill with a lance paid for much of {?CHILD.GENDER}her{?}his{\\?} journey.", null);
			TextObject textObject29 = new TextObject("{=2EOvlPc2}Gripped by wanderlust, {?PLAYER_CHILD}your{?}the{\\?} child rode from the freezing woods of Sturgia to the blazing Nahasa.", null);
			TextObject textObject30 = new TextObject("{=KB0fv5Me}The young {?CHILD.GENDER}woman{?}man{\\?} charmed {?CHILD.GENDER}her{?}his{\\?} way into a circle of nobles and was a welcome guest at well-set tables.", null);
			TextObject textObject31 = new TextObject("{=UGda4FDZ}{CHILD.NAME} found work with an artisan for a season, keeping {?CHILD.GENDER}her{?}his{\\?} high birth a secret.", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties32 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties33 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_streets", "carry_bostaff_rogue1", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties34 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties35 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties36 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_appearances");
			EducationCampaignBehavior.EducationCharacterProperties childProperties37 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_strenght", "carry_sticks", false);
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=1LWWvGTT}participate in a pilgrimage.", null), textObject26, null, null, null, null, new SkillObject[] { DefaultSkills.Athletics }, childProperties32, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=8alDLq0s}not to be too choosy about travelling companions.", null), textObject27, null, null, null, null, new SkillObject[] { DefaultSkills.Roguery }, childProperties33, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=RxqCiO7F}competes in tournaments.", null), textObject28, null, null, null, null, new SkillObject[] { DefaultSkills.Polearm }, childProperties34, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=ETfx08db}see as much of the world as {?CHILD.GENDER}she{?}he{\\?} could.", null), textObject29, null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, childProperties35, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=RhJdnAQY}enjoy the high life.", null), textObject30, null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties36, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage6.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=dObod6IZ}do some honest work.", null), textObject31, null, null, null, null, new SkillObject[] { DefaultSkills.Crafting }, childProperties37, default(EducationCampaignBehavior.EducationCharacterProperties)));
			TextObject description6 = new TextObject("{=TaAHFZfd}You asked your head of expedition to make sure that your {?CHILD.GENDER}daughter{?}son{\\?}...", null);
			EducationCampaignBehavior.EducationPage educationPage7 = educationStage.AddPage(1, title, description6, this._chooseRequestText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_diplomatOption));
			TextObject textObject32 = new TextObject("{=Q39oLjSu}One of the entourage was thrown from a horse on the road. The young {?CHILD.GENDER}woman{?}man{\\?} set the broken bone and ensured it healed cleanly.", null);
			TextObject textObject33 = new TextObject("{=8AjlID6z}You hear talk of incriminating letters and loose tongues paid to stay silent. You're not sure exactly what happened, but {?PLAYER_CHILD}your{?}the{\\?} child seems more worldly than {?CHILD.GENDER}she{?}he{\\?} did when {?CHILD.GENDER}she{?}he{\\?} left.", null);
			TextObject textObject34 = new TextObject("{=4nUhnWZ3}There was a great deal of feasting and speech-making. From what you hear, {?CHILD.GENDER}she{?}he{\\?} acquitted {?CHILD.GENDER}herself{?}himself{\\?} well.", null);
			TextObject textObject35 = new TextObject("{=aCMZafK5}The fight was over a minor insult, and only to the first blood. But you're still relieved that the first blood in question belonged to the other youth.", null);
			TextObject textObject36 = new TextObject("{=5vosR2YO}The host was the kind of man who likes to do his negotiations from the saddle in pursuit of deer, and {CHILD.NAME} joined in every expedition.", null);
			TextObject textObject37 = new TextObject("{=taKIDPxj}Embassies carry gifts and attract bandits. Fighting them off made the trip much more exciting than {?CHILD.GENDER}she{?}he{\\?} had expected.", null);
			EducationCampaignBehavior.EducationCharacterProperties childProperties38 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book_left", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties39 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_hardened");
			EducationCampaignBehavior.EducationCharacterProperties childProperties40 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners");
			EducationCampaignBehavior.EducationCharacterProperties childProperties41 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_apprentice", "training_sword", false);
			EducationCampaignBehavior.EducationCharacterProperties childProperties42 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true);
			EducationCampaignBehavior.EducationCharacterProperties childProperties43 = new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician");
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=6jgjJ3Ts}help anyone in need.", null), textObject32, null, null, null, null, new SkillObject[] { DefaultSkills.Medicine }, childProperties38, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=FfZpLoVD}dabble in intrigue.", null), textObject33, null, null, null, null, new SkillObject[] { DefaultSkills.Roguery }, childProperties39, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=EzbzOsJo}burnish {?CHILD.GENDER}her{?}his{\\?} social skills.", null), textObject34, null, null, null, null, new SkillObject[] { DefaultSkills.Charm }, childProperties40, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=bdYtcgyB}never back down from a challenge.", null), textObject35, null, null, null, null, new SkillObject[] { DefaultSkills.OneHanded }, childProperties41, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=hrlBxgzs}enjoy the pleasures of the hunt.", null), textObject36, null, null, null, null, new SkillObject[] { DefaultSkills.Riding }, childProperties42, default(EducationCampaignBehavior.EducationCharacterProperties)));
			educationPage7.AddOption(new EducationCampaignBehavior.EducationOption(new TextObject("{=XMaeSoRq}never let down {?CHILD.GENDER}her{?}his{\\?} guard.", null), textObject37, null, null, null, null, new SkillObject[] { DefaultSkills.Tactics }, childProperties43, default(EducationCampaignBehavior.EducationCharacterProperties)));
			EducationCampaignBehavior.EducationPage[] array = new EducationCampaignBehavior.EducationPage[] { educationPage2, educationPage3, educationPage5, educationPage6, educationPage4, educationPage7 };
			for (int i = 0; i < array.Length; i++)
			{
				foreach (EducationCampaignBehavior.EducationOption educationOption in array[i].Options)
				{
					educationOption.Description.SetTextVariable("PLAYER_CHILD", this.IsHeroChildOfPlayer(child) ? 1 : 0);
				}
			}
			TextObject item = new TextObject("{=4VxFPTT0}got bloodied.", null);
			TextObject item2 = new TextObject("{=VAFmTAtx}won a tournament.", null);
			TextObject item3 = new TextObject("{=DiE5Mh2J}learned to inspire the soldiers.", null);
			TextObject item4 = new TextObject("{=hYTcXbyS}led a patrol.", null);
			TextObject item5 = new TextObject("{=webjbgTa}trained really hard.", null);
			TextObject item6 = new TextObject("{=SCpINAZ1}hunted down bandits.", null);
			TextObject item7 = new TextObject("{=YVyId3wX}dealt with business partners.", null);
			TextObject item8 = new TextObject("{=SBHwIaVP}recovered stolen goods.", null);
			TextObject item9 = new TextObject("{=o9XKbana}helped the artisan.", null);
			TextObject item10 = new TextObject("{=brixvCjC}guided the caravan.", null);
			TextObject item11 = new TextObject("{=CVVttuuQ}managed the logistics of travel.", null);
			TextObject item12 = new TextObject("{=xXOLhxRl}supervised the guards.", null);
			TextObject item13 = new TextObject("{=MHsa7s99}learned to construct siege engines.", null);
			TextObject item14 = new TextObject("{=SXbW99CH}treated injuries.", null);
			TextObject item15 = new TextObject("{=SGVW6NXR}assisted the smith.", null);
			TextObject item16 = new TextObject("{=9mWxhzDG}procured supplies.", null);
			TextObject item17 = new TextObject("{=a1PPbzbi}focused on the big picture.", null);
			TextObject item18 = new TextObject("{=nyatGUkU}socialized with other engineers.", null);
			TextObject item19 = new TextObject("{=g7lNg1ea}unleashed {?CHILD.GENDER}her{?}his{\\?} fighting spirit.", null);
			TextObject item20 = new TextObject("{=KjR0HhJv}skirmished from a distance.", null);
			TextObject item21 = new TextObject("{=SSavEblm}chased and was chased.", null);
			TextObject item22 = new TextObject("{=plYfFg4A}learned how a victory is won.", null);
			TextObject item23 = new TextObject("{=2OdXZUg9}led men into enemy territory.", null);
			TextObject item24 = new TextObject("{=SnEjsXsH}defeated an enemy.", null);
			TextObject item25 = new TextObject("{=9QLPm1o6}joined a pilgrimage.", null);
			TextObject item26 = new TextObject("{=E90ArBnZ}fell in with the wrong crowd.", null);
			TextObject item27 = new TextObject("{=VAFmTAtx}won a tournament.", null);
			TextObject item28 = new TextObject("{=7hYUleHh}rode to the edge of the world.", null);
			TextObject item29 = new TextObject("{=PgN4BFNs}enjoyed the high life.", null);
			TextObject item30 = new TextObject("{=lsddqaAx}did some honest work.", null);
			TextObject item31 = new TextObject("{=gCBZlAzs}treated an injury.", null);
			TextObject item32 = new TextObject("{=CNYoZKyf}dabbled in intrigue.", null);
			TextObject item33 = new TextObject("{=83n7Oa7e}burnished {?CHILD.GENDER}her{?}his{\\?} social skills.", null);
			TextObject item34 = new TextObject("{=p5Wo8rNb}won a duel.", null);
			TextObject item35 = new TextObject("{=O9amiAiB}joined a hunting party.", null);
			TextObject item36 = new TextObject("{=XFZcqNBB}battled brigands.", null);
			TextObject description7 = new TextObject("{=Yk3xrawy}When {?CHILD.GENDER}she{?}he{\\?} returns, {CHILD.NAME} tells you the story of how {?CHILD.GENDER}she{?}he{\\?} {RANDOM_OUTCOME} That event increased {?CHILD.GENDER}her{?}his{\\?} skill in {SKILL}.", null);
			EducationCampaignBehavior.EducationPage currentPage = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage p, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_warriorOption));
			ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(item, textObject2, DefaultSkills.OneHanded),
				new ValueTuple<TextObject, TextObject, SkillObject>(item2, textObject3, DefaultSkills.Polearm),
				new ValueTuple<TextObject, TextObject, SkillObject>(item3, textObject4, DefaultSkills.Charm),
				new ValueTuple<TextObject, TextObject, SkillObject>(item4, textObject5, DefaultSkills.Leadership),
				new ValueTuple<TextObject, TextObject, SkillObject>(item5, textObject6, DefaultSkills.TwoHanded),
				new ValueTuple<TextObject, TextObject, SkillObject>(item6, textObject7, DefaultSkills.Tactics)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties44 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_militia", "carry_bostaff_rogue1", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_confident_loop"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_hip_loop"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_arms_2", "vlandia_twohanded_sword_c", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_closed_loop")
			};
			this.Stage16Selection(titleDescSkillTuple, currentPage, childProperties44);
			EducationCampaignBehavior.EducationPage currentPage2 = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage p, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_merchantOption));
			ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple2 = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(item7, textObject8, DefaultSkills.Charm),
				new ValueTuple<TextObject, TextObject, SkillObject>(item8, textObject9, DefaultSkills.Roguery),
				new ValueTuple<TextObject, TextObject, SkillObject>(item9, textObject10, DefaultSkills.Crafting),
				new ValueTuple<TextObject, TextObject, SkillObject>(item10, textObject11, DefaultSkills.Scouting),
				new ValueTuple<TextObject, TextObject, SkillObject>(item11, textObject12, DefaultSkills.Steward),
				new ValueTuple<TextObject, TextObject, SkillObject>(item12, textObject13, DefaultSkills.Leadership)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties45 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_confident_loop"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_artisan", "carry_linen", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_vibrant"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_confident_loop"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_hip_loop")
			};
			this.Stage16Selection(titleDescSkillTuple2, currentPage2, childProperties45);
			EducationCampaignBehavior.EducationPage currentPage3 = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage p, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_siegeEngineerOption));
			ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple3 = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(item13, textObject14, DefaultSkills.Engineering),
				new ValueTuple<TextObject, TextObject, SkillObject>(item14, textObject15, DefaultSkills.Medicine),
				new ValueTuple<TextObject, TextObject, SkillObject>(item15, textObject16, DefaultSkills.Crafting),
				new ValueTuple<TextObject, TextObject, SkillObject>(item16, textObject17, DefaultSkills.Trade),
				new ValueTuple<TextObject, TextObject, SkillObject>(item17, textObject18, DefaultSkills.Tactics),
				new ValueTuple<TextObject, TextObject, SkillObject>(item18, textObject19, DefaultSkills.Charm)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties46 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners_3", "carry_book", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_artisan", "carry_linen", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_genius"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners")
			};
			this.Stage16Selection(titleDescSkillTuple3, currentPage3, childProperties46);
			EducationCampaignBehavior.EducationPage currentPage4 = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage p, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_nobleOption));
			ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple4 = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(item19, textObject20, DefaultSkills.Athletics),
				new ValueTuple<TextObject, TextObject, SkillObject>(item20, textObject21, DefaultSkills.Bow),
				new ValueTuple<TextObject, TextObject, SkillObject>(item21, textObject22, DefaultSkills.Riding),
				new ValueTuple<TextObject, TextObject, SkillObject>(item22, textObject23, DefaultSkills.Tactics),
				new ValueTuple<TextObject, TextObject, SkillObject>(item23, textObject24, DefaultSkills.Leadership),
				new ValueTuple<TextObject, TextObject, SkillObject>(item24, textObject25, DefaultSkills.OneHanded)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties47 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_confident2_loop"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_conversation_hip_loop"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_apprentice", "training_sword", false)
			};
			this.Stage16Selection(titleDescSkillTuple4, currentPage4, childProperties47);
			EducationCampaignBehavior.EducationPage currentPage5 = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage p, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_ownWayOption));
			ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple5 = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(item25, textObject26, DefaultSkills.Athletics),
				new ValueTuple<TextObject, TextObject, SkillObject>(item26, textObject27, DefaultSkills.Roguery),
				new ValueTuple<TextObject, TextObject, SkillObject>(item27, textObject28, DefaultSkills.Polearm),
				new ValueTuple<TextObject, TextObject, SkillObject>(item28, textObject29, DefaultSkills.Riding),
				new ValueTuple<TextObject, TextObject, SkillObject>(item29, textObject30, DefaultSkills.Charm),
				new ValueTuple<TextObject, TextObject, SkillObject>(item30, textObject31, DefaultSkills.Crafting)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties48 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_athlete"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_guard_up_staff", "carry_bostaff", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_grit", "carry_hammer", false)
			};
			this.Stage16Selection(titleDescSkillTuple5, currentPage5, childProperties48);
			EducationCampaignBehavior.EducationPage currentPage6 = educationStage.AddPage(2, title, description7, this._confirmResultsText, default(EducationCampaignBehavior.EducationCharacterProperties), default(EducationCampaignBehavior.EducationCharacterProperties), (EducationCampaignBehavior.EducationPage p, List<EducationCampaignBehavior.EducationOption> previousOptions) => previousOptions.Contains(stage_5_page_0_diplomatOption));
			ValueTuple<TextObject, TextObject, SkillObject>[] titleDescSkillTuple6 = new ValueTuple<TextObject, TextObject, SkillObject>[]
			{
				new ValueTuple<TextObject, TextObject, SkillObject>(item31, textObject32, DefaultSkills.Medicine),
				new ValueTuple<TextObject, TextObject, SkillObject>(item32, textObject33, DefaultSkills.Roguery),
				new ValueTuple<TextObject, TextObject, SkillObject>(item33, textObject34, DefaultSkills.Charm),
				new ValueTuple<TextObject, TextObject, SkillObject>(item34, textObject35, DefaultSkills.OneHanded),
				new ValueTuple<TextObject, TextObject, SkillObject>(item35, textObject36, DefaultSkills.Riding),
				new ValueTuple<TextObject, TextObject, SkillObject>(item36, textObject37, DefaultSkills.Tactics)
			};
			EducationCampaignBehavior.EducationCharacterProperties[] childProperties49 = new EducationCampaignBehavior.EducationCharacterProperties[]
			{
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_ready", "carry_book", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_roguery", "carry_bostaff_rogue1", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_manners"),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_apprentice", "vlandia_twohanded_sword_c", false),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_riding_2", "carry_saddle", true),
				new EducationCampaignBehavior.EducationCharacterProperties("act_childhood_tactician")
			};
			this.Stage16Selection(titleDescSkillTuple6, currentPage6, childProperties49);
			return educationStage;
		}

		// Token: 0x06003DBA RID: 15802 RVA: 0x001125F8 File Offset: 0x001107F8
		private void GetHighestThreeAttributes(Hero hero, out ValueTuple<CharacterAttribute, int>[] maxAttributes)
		{
			ValueTuple<CharacterAttribute, int>[] array = new ValueTuple<CharacterAttribute, int>[Attributes.All.Count];
			for (int i = 0; i < Attributes.All.Count; i++)
			{
				CharacterAttribute characterAttribute = Attributes.All[i];
				array[i] = new ValueTuple<CharacterAttribute, int>(characterAttribute, (hero != null) ? hero.GetAttributeValue(characterAttribute) : MBRandom.RandomInt(1, 10));
			}
			maxAttributes = (from x in array
				orderby x.Item2 descending
				select x).Take(3).ToArray<ValueTuple<CharacterAttribute, int>>();
		}

		// Token: 0x04001296 RID: 4758
		private const char Separator = ';';

		// Token: 0x04001297 RID: 4759
		private const int AttributeIncrease = 1;

		// Token: 0x04001298 RID: 4760
		private const int FocusIncrease = 1;

		// Token: 0x04001299 RID: 4761
		private const int SkillIncrease = 5;

		// Token: 0x0400129A RID: 4762
		private readonly TextObject _pickAttributeText = new TextObject("{=m7iBf6fQ}Pick an Attribute", null);

		// Token: 0x0400129B RID: 4763
		private readonly TextObject _confirmResultsText = new TextObject("{=La9qAlfi}Confirm the Results", null);

		// Token: 0x0400129C RID: 4764
		private readonly TextObject _chooseTalentText = new TextObject("{=K9fcqr0K}Choose a Talent", null);

		// Token: 0x0400129D RID: 4765
		private readonly TextObject _chooseTutorText = new TextObject("{=B7JVLc4u}Choose a Tutor", null);

		// Token: 0x0400129E RID: 4766
		private readonly TextObject _guideTutorText = new TextObject("{=VbWAsWWY}Guide the Tutor", null);

		// Token: 0x0400129F RID: 4767
		private readonly TextObject _chooseFocusText = new TextObject("{=HBZS0bug}Choose a Focus", null);

		// Token: 0x040012A0 RID: 4768
		private readonly TextObject _chooseSkillText = new TextObject("{=5BEGa9ZS}Choose a Skill", null);

		// Token: 0x040012A1 RID: 4769
		private readonly TextObject _chooseGiftText = new TextObject("{=DcoDtW2A}Choose a Gift", null);

		// Token: 0x040012A2 RID: 4770
		private readonly TextObject _chooseAchievementText = new TextObject("{=26sKJehk}Choose an Achievement", null);

		// Token: 0x040012A3 RID: 4771
		private Dictionary<Hero, short> _previousEducations = new Dictionary<Hero, short>();

		// Token: 0x040012A4 RID: 4772
		private readonly TextObject _chooseTaskText = new TextObject("{=SUNKjdZ9}Choose a Task", null);

		// Token: 0x040012A5 RID: 4773
		private readonly TextObject _chooseRequestText = new TextObject("{=jNBVoObj}Choose a Request", null);

		// Token: 0x040012A6 RID: 4774
		private Hero _activeChild;

		// Token: 0x040012A7 RID: 4775
		private EducationCampaignBehavior.EducationStage _activeStage;

		// Token: 0x020007DA RID: 2010
		private enum ChildAgeState : short
		{
			// Token: 0x04001F4D RID: 8013
			Invalid = -1,
			// Token: 0x04001F4E RID: 8014
			Year2,
			// Token: 0x04001F4F RID: 8015
			Year5,
			// Token: 0x04001F50 RID: 8016
			Year8,
			// Token: 0x04001F51 RID: 8017
			Year11,
			// Token: 0x04001F52 RID: 8018
			Year14,
			// Token: 0x04001F53 RID: 8019
			Year16,
			// Token: 0x04001F54 RID: 8020
			Count,
			// Token: 0x04001F55 RID: 8021
			First = 0,
			// Token: 0x04001F56 RID: 8022
			Last = 5
		}

		// Token: 0x020007DB RID: 2011
		private class EducationOption
		{
			// Token: 0x060062E4 RID: 25316 RVA: 0x001BBAAC File Offset: 0x001B9CAC
			public void OnConsequence(Hero child)
			{
				EducationCampaignBehavior.EducationOption.EducationOptionConsequenceDelegate consequence = this._consequence;
				if (consequence != null)
				{
					consequence(this);
				}
				foreach (CharacterAttribute attrib in this.Attributes)
				{
					child.HeroDeveloper.AddAttribute(attrib, 1, false);
				}
				foreach (SkillObject skill in this.Skills)
				{
					child.HeroDeveloper.AddFocus(skill, 1, false);
					child.HeroDeveloper.ChangeSkillLevel(skill, 5, true);
				}
			}

			// Token: 0x060062E5 RID: 25317 RVA: 0x001BBB2C File Offset: 0x001B9D2C
			public EducationOption(TextObject title, TextObject description, TextObject effect, EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate condition, EducationCampaignBehavior.EducationOption.EducationOptionConsequenceDelegate consequence, CharacterAttribute[] attributes, SkillObject[] skills, EducationCampaignBehavior.EducationCharacterProperties childProperties, EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties = default(EducationCampaignBehavior.EducationCharacterProperties))
			{
				this.Title = title;
				this.Description = description;
				this.Condition = condition;
				this._consequence = consequence;
				this.Attributes = attributes ?? new CharacterAttribute[0];
				this.Skills = skills ?? new SkillObject[0];
				this.Effect = this.GetEffectText(effect ?? TextObject.GetEmpty());
				this.ChildProperties = childProperties;
				this.SpecialCharacterProperties = specialCharacterProperties;
				this.RandomValue = MBRandom.RandomInt(0, int.MaxValue);
			}

			// Token: 0x060062E6 RID: 25318 RVA: 0x001BBBB8 File Offset: 0x001B9DB8
			private TextObject GetEffectText(TextObject effect)
			{
				TextObject textObject = new TextObject("{=JfBTbsX2}{EFFECT_DESCRIPTION}{NEW_LINE_1}{SKILL_DESCRIPTION}{NEW_LINE_2}{ATTRIBUTE_DESCRIPTION}", null);
				TextObject textObject2;
				if (this.Skills.Length == 1)
				{
					textObject2 = new TextObject("{=I88vSwpb}{SKILL1} gains {NUMBER_FP} Focus Point and {NUMBER_SP} Skill Points.", null);
					textObject2.SetTextVariable("SKILL1", this.Skills[0].Name);
				}
				else if (this.Skills.Length == 2)
				{
					textObject2 = new TextObject("{=bvRVu0fO}{SKILL1} and {SKILL2} gain {NUMBER_FP} Focus Point and {NUMBER_SP} Skill Points.", null);
					textObject2.SetTextVariable("SKILL1", this.Skills[0].Name);
					textObject2.SetTextVariable("SKILL2", this.Skills[1].Name);
				}
				else
				{
					textObject2 = TextObject.GetEmpty();
				}
				TextObject textObject3;
				if (this.Attributes.Length == 1)
				{
					textObject3 = new TextObject("{=bm2DzxEl}{ATTRIBUTE1} is increased by {NUMBER_AP}.", null);
					textObject3.SetTextVariable("ATTRIBUTE1", this.Attributes.ElementAt(0).Name);
				}
				else if (this.Attributes.Length == 2)
				{
					textObject3 = new TextObject("{=2sQQh02s}{ATTRIBUTE1} and {ATTRIBUTE2} are increased by {NUMBER_AP}.", null);
					textObject3.SetTextVariable("ATTRIBUTE1", this.Attributes[0].Name);
					textObject3.SetTextVariable("ATTRIBUTE2", this.Attributes[1].Name);
				}
				else
				{
					textObject3 = TextObject.GetEmpty();
				}
				if (!TextObject.IsNullOrEmpty(textObject3))
				{
					textObject3.SetTextVariable("NUMBER_AP", 1);
				}
				if (!TextObject.IsNullOrEmpty(textObject2))
				{
					textObject2.SetTextVariable("NUMBER_FP", 1);
					textObject2.SetTextVariable("NUMBER_SP", 5);
				}
				textObject.SetTextVariable("SKILL_DESCRIPTION", textObject2);
				textObject.SetTextVariable("ATTRIBUTE_DESCRIPTION", textObject3);
				textObject.SetTextVariable("EFFECT_DESCRIPTION", effect);
				if (!effect.IsEmpty() && (!textObject2.IsEmpty() || !textObject3.IsEmpty()))
				{
					textObject.SetTextVariable("NEW_LINE_1", "\n");
				}
				else
				{
					textObject.SetTextVariable("NEW_LINE_1", TextObject.GetEmpty());
				}
				if (!textObject2.IsEmpty() && !textObject3.IsEmpty())
				{
					textObject.SetTextVariable("NEW_LINE_2", "\n");
				}
				else
				{
					textObject.SetTextVariable("NEW_LINE_2", TextObject.GetEmpty());
				}
				return textObject;
			}

			// Token: 0x04001F57 RID: 8023
			public readonly EducationCampaignBehavior.EducationOption.EducationOptionConditionDelegate Condition;

			// Token: 0x04001F58 RID: 8024
			private readonly EducationCampaignBehavior.EducationOption.EducationOptionConsequenceDelegate _consequence;

			// Token: 0x04001F59 RID: 8025
			public readonly TextObject Title;

			// Token: 0x04001F5A RID: 8026
			public readonly TextObject Description;

			// Token: 0x04001F5B RID: 8027
			public readonly TextObject Effect;

			// Token: 0x04001F5C RID: 8028
			public readonly CharacterAttribute[] Attributes;

			// Token: 0x04001F5D RID: 8029
			public readonly SkillObject[] Skills;

			// Token: 0x04001F5E RID: 8030
			public readonly EducationCampaignBehavior.EducationCharacterProperties ChildProperties;

			// Token: 0x04001F5F RID: 8031
			public readonly EducationCampaignBehavior.EducationCharacterProperties SpecialCharacterProperties;

			// Token: 0x04001F60 RID: 8032
			public readonly int RandomValue;

			// Token: 0x020008F0 RID: 2288
			// (Invoke) Token: 0x060068FD RID: 26877
			public delegate bool EducationOptionConditionDelegate(EducationCampaignBehavior.EducationOption option, List<EducationCampaignBehavior.EducationOption> previousOptions);

			// Token: 0x020008F1 RID: 2289
			// (Invoke) Token: 0x06006901 RID: 26881
			public delegate bool EducationOptionConsequenceDelegate(EducationCampaignBehavior.EducationOption option);
		}

		// Token: 0x020007DC RID: 2012
		private class EducationStage
		{
			// Token: 0x1700151D RID: 5405
			// (get) Token: 0x060062E7 RID: 25319 RVA: 0x001BBDAA File Offset: 0x001B9FAA
			public int PageCount
			{
				get
				{
					return this._superPages.Count;
				}
			}

			// Token: 0x060062E8 RID: 25320 RVA: 0x001BBDB7 File Offset: 0x001B9FB7
			public EducationStage(EducationCampaignBehavior.ChildAgeState targetAge)
			{
				this.Target = targetAge;
				this._superPages = new List<List<EducationCampaignBehavior.EducationPage>>();
			}

			// Token: 0x060062E9 RID: 25321 RVA: 0x001BBDD4 File Offset: 0x001B9FD4
			public EducationCampaignBehavior.EducationPage AddPage(int pageIndex, TextObject title, TextObject description, TextObject instruction, EducationCampaignBehavior.EducationCharacterProperties childProperties = default(EducationCampaignBehavior.EducationCharacterProperties), EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties = default(EducationCampaignBehavior.EducationCharacterProperties), EducationCampaignBehavior.EducationPage.EducationPageConditionDelegate condition = null)
			{
				while (pageIndex >= this._superPages.Count)
				{
					this._superPages.Add(new List<EducationCampaignBehavior.EducationPage>());
				}
				EducationCampaignBehavior.EducationPage educationPage = new EducationCampaignBehavior.EducationPage(pageIndex.ToString() + ";" + this._superPages[pageIndex].Count, title, description, instruction, childProperties, specialCharacterProperties, condition);
				this._superPages[pageIndex].Add(educationPage);
				return educationPage;
			}

			// Token: 0x060062EA RID: 25322 RVA: 0x001BBE4C File Offset: 0x001BA04C
			private Equipment GetChildEquipmentForOption(Hero child, string optionKey, List<string> previousOptions)
			{
				string[] array = optionKey.Split(new char[] { ';' });
				int num;
				if (!int.TryParse(array[0], out num))
				{
					Debug.FailedAssert("/keys/ isnt set correctly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetChildEquipmentForOption", 221);
				}
				Equipment equipment = null;
				if (this.Target == EducationCampaignBehavior.ChildAgeState.Year8)
				{
					MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(string.Format("child_education_equipments_stage_{0}_page_0_branch_child_{1}", (int)this.Target, child.Culture.StringId));
					equipment = ((@object != null) ? @object.DefaultEquipment : null);
				}
				else if (this.Target == EducationCampaignBehavior.ChildAgeState.Year16 && num > 0)
				{
					string arg = previousOptions[0].Split(new char[] { ';' })[2];
					MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(string.Format("child_education_equipments_stage_{0}_page_0_branch_{1}_{2}", (int)this.Target, arg, child.Culture.StringId));
					equipment = ((object2 != null) ? object2.DefaultEquipment : null);
				}
				else if (this.Target != EducationCampaignBehavior.ChildAgeState.Year2)
				{
					MBEquipmentRoster object3 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(string.Format("child_education_equipments_stage_{0}_page_{1}_branch_{2}_{3}", new object[]
					{
						(int)this.Target,
						array[0],
						array[2],
						child.Culture.StringId
					}));
					equipment = ((object3 != null) ? object3.DefaultEquipment : null);
				}
				return equipment ?? MBEquipmentRoster.EmptyEquipment;
			}

			// Token: 0x060062EB RID: 25323 RVA: 0x001BBFA8 File Offset: 0x001BA1A8
			private Equipment GetChildEquipmentForPage(Hero child, EducationCampaignBehavior.EducationPage page, List<string> previousOptions)
			{
				Equipment equipment = null;
				if (this.Target == EducationCampaignBehavior.ChildAgeState.Year8 && previousOptions.Count == 0)
				{
					MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(string.Format("child_education_equipments_stage_{0}_page_0_branch_child_{1}", (int)this.Target, child.Culture.StringId));
					equipment = ((@object != null) ? @object.DefaultEquipment : null);
				}
				else if (previousOptions.Count > 0)
				{
					equipment = this.GetChildEquipmentForOption(child, previousOptions[0], previousOptions);
				}
				else if (this.Target != EducationCampaignBehavior.ChildAgeState.Year2)
				{
					MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(string.Format("child_education_equipments_stage_{0}_page_0_branch_default_{1}", (int)this.Target, child.Culture.StringId));
					equipment = ((object2 != null) ? object2.DefaultEquipment : null);
				}
				return equipment ?? MBEquipmentRoster.EmptyEquipment;
			}

			// Token: 0x060062EC RID: 25324 RVA: 0x001BC070 File Offset: 0x001BA270
			private EducationCampaignBehavior.EducationCharacterProperties GetChildPropertiesForOption(Hero child, string optionKey, List<string> previousOptions)
			{
				string[] array = optionKey.Split(new char[] { ';' });
				EducationCampaignBehavior.EducationOption option = this.GetOption(optionKey);
				int num;
				if (!int.TryParse(array[0], out num))
				{
					Debug.FailedAssert("/keys/ isnt set correctly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetChildPropertiesForOption", 270);
				}
				Equipment childEquipmentForOption = this.GetChildEquipmentForOption(child, optionKey, previousOptions);
				return new EducationCampaignBehavior.EducationCharacterProperties(child.CharacterObject, childEquipmentForOption, option.ChildProperties.ActionId, option.ChildProperties.PrefabId, option.ChildProperties.UseOffHand);
			}

			// Token: 0x060062ED RID: 25325 RVA: 0x001BC0F4 File Offset: 0x001BA2F4
			private EducationCampaignBehavior.EducationCharacterProperties GetChildPropertiesForPage(Hero child, EducationCampaignBehavior.EducationPage page, List<string> previousOptions)
			{
				EducationCampaignBehavior.EducationCharacterProperties childPropertiesForOption;
				if (previousOptions.Count == 0 || page.ChildProperties != EducationCampaignBehavior.EducationCharacterProperties.Invalid)
				{
					string actionId = page.ChildProperties.ActionId;
					string prefabId = page.ChildProperties.PrefabId;
					bool useOffHand = page.ChildProperties.UseOffHand;
					childPropertiesForOption = new EducationCampaignBehavior.EducationCharacterProperties(child.CharacterObject, this.GetChildEquipmentForPage(child, page, previousOptions), actionId, prefabId, useOffHand);
				}
				else
				{
					childPropertiesForOption = this.GetChildPropertiesForOption(child, previousOptions[0], previousOptions);
				}
				return childPropertiesForOption;
			}

			// Token: 0x060062EE RID: 25326 RVA: 0x001BC16C File Offset: 0x001BA36C
			private CharacterObject GetSpecialCharacterForOption(Hero child, string optionKey, List<string> previousOptions)
			{
				string[] array = optionKey.Split(new char[] { ';' });
				int num;
				if (!int.TryParse(array[0], out num))
				{
					Debug.FailedAssert("/keys/ isnt set correctly", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetSpecialCharacterForOption", 313);
				}
				CharacterObject result = null;
				if (this.Target == EducationCampaignBehavior.ChildAgeState.Year8)
				{
					if (num == 0)
					{
						result = Game.Current.ObjectManager.GetObject<CharacterObject>(string.Format("child_education_templates_stage_{0}_page_0_branch_{1}_{2}", (int)this.Target, array[2], child.Culture.StringId));
					}
					else
					{
						string arg = previousOptions[0].Split(new char[] { ';' })[2];
						result = Game.Current.ObjectManager.GetObject<CharacterObject>(string.Format("child_education_templates_stage_{0}_page_0_branch_{1}_{2}", (int)this.Target, arg, child.Culture.StringId));
					}
				}
				return result;
			}

			// Token: 0x060062EF RID: 25327 RVA: 0x001BC244 File Offset: 0x001BA444
			private EducationCampaignBehavior.EducationCharacterProperties GetSpecialCharacterPropertiesForPage(Hero child, EducationCampaignBehavior.EducationPage page, List<string> previousOptions)
			{
				EducationCampaignBehavior.EducationCharacterProperties result = EducationCampaignBehavior.EducationCharacterProperties.Invalid;
				if (this.Target == EducationCampaignBehavior.ChildAgeState.Year8)
				{
					if (page.SpecialCharacterProperties != EducationCampaignBehavior.EducationCharacterProperties.Invalid)
					{
						string actionId = page.SpecialCharacterProperties.ActionId;
						string prefabId = page.SpecialCharacterProperties.PrefabId;
						bool useOffHand = page.SpecialCharacterProperties.UseOffHand;
						CharacterObject specialCharacterForOption = this.GetSpecialCharacterForOption(child, previousOptions[0], previousOptions);
						result = new EducationCampaignBehavior.EducationCharacterProperties(specialCharacterForOption, specialCharacterForOption.Equipment, actionId, prefabId, useOffHand);
					}
					if (previousOptions.Count > 0)
					{
						result = this.GetSpecialCharacterPropertiesForOption(child, previousOptions[0], previousOptions);
					}
				}
				return result;
			}

			// Token: 0x060062F0 RID: 25328 RVA: 0x001BC2D4 File Offset: 0x001BA4D4
			private EducationCampaignBehavior.EducationCharacterProperties GetSpecialCharacterPropertiesForOption(Hero child, string optionKey, List<string> previousOptions)
			{
				EducationCampaignBehavior.EducationCharacterProperties invalid = EducationCampaignBehavior.EducationCharacterProperties.Invalid;
				if (this.Target == EducationCampaignBehavior.ChildAgeState.Year8)
				{
					EducationCampaignBehavior.EducationOption option = this.GetOption(optionKey);
					CharacterObject specialCharacterForOption = this.GetSpecialCharacterForOption(child, optionKey, previousOptions);
					invalid = new EducationCampaignBehavior.EducationCharacterProperties(specialCharacterForOption, specialCharacterForOption.Equipment, option.SpecialCharacterProperties.ActionId, option.SpecialCharacterProperties.PrefabId, option.SpecialCharacterProperties.UseOffHand);
				}
				return invalid;
			}

			// Token: 0x060062F1 RID: 25329 RVA: 0x001BC334 File Offset: 0x001BA534
			public EducationCampaignBehavior.EducationOption GetOption(string optionKey)
			{
				string[] array = optionKey.Split(new char[] { ';' });
				return this._superPages[int.Parse(array[0])][int.Parse(array[1])].GetOption(optionKey);
			}

			// Token: 0x060062F2 RID: 25330 RVA: 0x001BC37C File Offset: 0x001BA57C
			public EducationCampaignBehavior.EducationPage GetPage(List<string> previousOptionKeys)
			{
				List<EducationCampaignBehavior.EducationOption> list = this.StringIdToEducationOption(previousOptionKeys);
				int count = previousOptionKeys.Count;
				List<EducationCampaignBehavior.EducationPage> list2 = this._superPages[count];
				for (int i = 0; i < list2.Count; i++)
				{
					EducationCampaignBehavior.EducationPage educationPage = list2[i];
					if ((educationPage.Condition == null || educationPage.Condition(educationPage, list)) && educationPage.GetAvailableOptions(list).Length != 0)
					{
						return educationPage;
					}
				}
				Debug.FailedAssert("Education page not found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\EducationCampaignBehavior.cs", "GetPage", 404);
				return null;
			}

			// Token: 0x060062F3 RID: 25331 RVA: 0x001BC404 File Offset: 0x001BA604
			public List<EducationCampaignBehavior.EducationOption> StringIdToEducationOption(List<string> previousOptionKeys)
			{
				List<EducationCampaignBehavior.EducationOption> list = new List<EducationCampaignBehavior.EducationOption>();
				foreach (string optionKey in previousOptionKeys)
				{
					list.Add(this.GetOption(optionKey));
				}
				return list;
			}

			// Token: 0x060062F4 RID: 25332 RVA: 0x001BC460 File Offset: 0x001BA660
			public override string ToString()
			{
				return this.Target.ToString();
			}

			// Token: 0x060062F5 RID: 25333 RVA: 0x001BC474 File Offset: 0x001BA674
			internal EducationCampaignBehavior.EducationCharacterProperties[] GetCharacterPropertiesForPage(Hero child, EducationCampaignBehavior.EducationPage page, List<string> previousChoices)
			{
				EducationCampaignBehavior.EducationCharacterProperties childPropertiesForPage = this.GetChildPropertiesForPage(child, page, previousChoices);
				EducationCampaignBehavior.EducationCharacterProperties specialCharacterPropertiesForPage = this.GetSpecialCharacterPropertiesForPage(child, page, previousChoices);
				List<EducationCampaignBehavior.EducationCharacterProperties> list = new List<EducationCampaignBehavior.EducationCharacterProperties>();
				if (childPropertiesForPage != EducationCampaignBehavior.EducationCharacterProperties.Invalid)
				{
					list.Add(childPropertiesForPage);
				}
				if (specialCharacterPropertiesForPage != EducationCampaignBehavior.EducationCharacterProperties.Invalid)
				{
					list.Add(specialCharacterPropertiesForPage);
				}
				return list.ToArray();
			}

			// Token: 0x060062F6 RID: 25334 RVA: 0x001BC4CC File Offset: 0x001BA6CC
			internal EducationCampaignBehavior.EducationCharacterProperties[] GetCharacterPropertiesForOption(Hero child, EducationCampaignBehavior.EducationOption option, string optionKey, List<string> previousOptions)
			{
				EducationCampaignBehavior.EducationCharacterProperties childPropertiesForOption = this.GetChildPropertiesForOption(child, optionKey, previousOptions);
				EducationCampaignBehavior.EducationCharacterProperties specialCharacterPropertiesForOption = this.GetSpecialCharacterPropertiesForOption(child, optionKey, previousOptions);
				List<EducationCampaignBehavior.EducationCharacterProperties> list = new List<EducationCampaignBehavior.EducationCharacterProperties>();
				if (childPropertiesForOption != EducationCampaignBehavior.EducationCharacterProperties.Invalid)
				{
					list.Add(childPropertiesForOption);
				}
				if (specialCharacterPropertiesForOption != EducationCampaignBehavior.EducationCharacterProperties.Invalid)
				{
					list.Add(specialCharacterPropertiesForOption);
				}
				return list.ToArray();
			}

			// Token: 0x04001F61 RID: 8033
			private List<List<EducationCampaignBehavior.EducationPage>> _superPages;

			// Token: 0x04001F62 RID: 8034
			public readonly EducationCampaignBehavior.ChildAgeState Target;
		}

		// Token: 0x020007DD RID: 2013
		public struct EducationCharacterProperties
		{
			// Token: 0x060062F7 RID: 25335 RVA: 0x001BC523 File Offset: 0x001BA723
			public EducationCharacterProperties(CharacterObject character, Equipment equipment, string actionId, string prefabId, bool useOffHand)
			{
				this.Character = character;
				this.Equipment = equipment;
				this.ActionId = actionId;
				this.PrefabId = prefabId;
				this.UseOffHand = useOffHand;
			}

			// Token: 0x060062F8 RID: 25336 RVA: 0x001BC54A File Offset: 0x001BA74A
			public EducationCharacterProperties(string actionId, string prefabId, bool useOffHand)
			{
				this = new EducationCampaignBehavior.EducationCharacterProperties(null, null, actionId, prefabId, useOffHand);
			}

			// Token: 0x060062F9 RID: 25337 RVA: 0x001BC557 File Offset: 0x001BA757
			public EducationCharacterProperties(string actionId)
			{
				this = new EducationCampaignBehavior.EducationCharacterProperties(null, null, actionId, string.Empty, false);
			}

			// Token: 0x060062FA RID: 25338 RVA: 0x001BC568 File Offset: 0x001BA768
			public static bool operator ==(EducationCampaignBehavior.EducationCharacterProperties a, EducationCampaignBehavior.EducationCharacterProperties b)
			{
				return a.Character == b.Character && a.Equipment == b.Equipment && a.ActionId == b.ActionId && a.PrefabId == b.PrefabId;
			}

			// Token: 0x060062FB RID: 25339 RVA: 0x001BC5B7 File Offset: 0x001BA7B7
			public static bool operator !=(EducationCampaignBehavior.EducationCharacterProperties a, EducationCampaignBehavior.EducationCharacterProperties b)
			{
				return !(a == b);
			}

			// Token: 0x060062FC RID: 25340 RVA: 0x001BC5C4 File Offset: 0x001BA7C4
			public bool Equals(EducationCampaignBehavior.EducationCharacterProperties other)
			{
				return this.Character.Equals(other.Character) && this.Equipment.Equals(other.Equipment) && this.ActionId.Equals(other.ActionId) && this.PrefabId.Equals(other.PrefabId);
			}

			// Token: 0x060062FD RID: 25341 RVA: 0x001BC61D File Offset: 0x001BA81D
			public override bool Equals(object obj)
			{
				return obj != null && obj is EducationCampaignBehavior.EducationCharacterProperties && this.Equals((EducationCampaignBehavior.EducationCharacterProperties)obj);
			}

			// Token: 0x060062FE RID: 25342 RVA: 0x001BC638 File Offset: 0x001BA838
			public override int GetHashCode()
			{
				return (((((this.ActionId.GetHashCode() * 397) ^ this.Character.GetHashCode()) * 397) ^ this.Equipment.GetHashCode()) * 397) ^ this.PrefabId.GetHashCode();
			}

			// Token: 0x060062FF RID: 25343 RVA: 0x001BC686 File Offset: 0x001BA886
			public sbyte GetUsedHandBoneIndex()
			{
				if (!this.UseOffHand)
				{
					return FaceGen.GetBaseMonsterFromRace(this.Character.Race).MainHandItemBoneIndex;
				}
				return FaceGen.GetBaseMonsterFromRace(this.Character.Race).OffHandItemBoneIndex;
			}

			// Token: 0x04001F63 RID: 8035
			public readonly CharacterObject Character;

			// Token: 0x04001F64 RID: 8036
			public readonly Equipment Equipment;

			// Token: 0x04001F65 RID: 8037
			public readonly string ActionId;

			// Token: 0x04001F66 RID: 8038
			public readonly string PrefabId;

			// Token: 0x04001F67 RID: 8039
			public readonly bool UseOffHand;

			// Token: 0x04001F68 RID: 8040
			public static readonly EducationCampaignBehavior.EducationCharacterProperties Default = new EducationCampaignBehavior.EducationCharacterProperties("act_inventory_idle_start");

			// Token: 0x04001F69 RID: 8041
			public static readonly EducationCampaignBehavior.EducationCharacterProperties Invalid = default(EducationCampaignBehavior.EducationCharacterProperties);
		}

		// Token: 0x020007DE RID: 2014
		private class EducationPage
		{
			// Token: 0x1700151E RID: 5406
			// (get) Token: 0x06006301 RID: 25345 RVA: 0x001BC6D7 File Offset: 0x001BA8D7
			public IEnumerable<EducationCampaignBehavior.EducationOption> Options
			{
				get
				{
					return this._options.Values;
				}
			}

			// Token: 0x06006302 RID: 25346 RVA: 0x001BC6E4 File Offset: 0x001BA8E4
			public EducationPage(string id, TextObject title, TextObject description, TextObject instruction, EducationCampaignBehavior.EducationCharacterProperties childProperties, EducationCampaignBehavior.EducationCharacterProperties specialCharacterProperties, EducationCampaignBehavior.EducationPage.EducationPageConditionDelegate condition = null)
			{
				this._id = id;
				this.Condition = condition;
				this.Title = title;
				this.Description = description;
				this.Instruction = instruction;
				this._options = new Dictionary<string, EducationCampaignBehavior.EducationOption>();
				this._keyIndex = 0;
				this.ChildProperties = childProperties;
				this.SpecialCharacterProperties = specialCharacterProperties;
			}

			// Token: 0x06006303 RID: 25347 RVA: 0x001BC73E File Offset: 0x001BA93E
			public void AddOption(EducationCampaignBehavior.EducationOption option)
			{
				this._options.Add(this._id + ";" + this._keyIndex.ToString(), option);
				this._keyIndex++;
			}

			// Token: 0x06006304 RID: 25348 RVA: 0x001BC778 File Offset: 0x001BA978
			public EducationCampaignBehavior.EducationOption GetOption(string optionKey)
			{
				EducationCampaignBehavior.EducationOption result;
				this._options.TryGetValue(optionKey, out result);
				return result;
			}

			// Token: 0x06006305 RID: 25349 RVA: 0x001BC798 File Offset: 0x001BA998
			public string[] GetAvailableOptions(List<EducationCampaignBehavior.EducationOption> previousEducationOptions)
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, EducationCampaignBehavior.EducationOption> keyValuePair in this._options)
				{
					if (keyValuePair.Value.Condition == null || keyValuePair.Value.Condition(keyValuePair.Value, previousEducationOptions))
					{
						list.Add(keyValuePair.Key);
					}
				}
				return list.ToArray();
			}

			// Token: 0x04001F6A RID: 8042
			public readonly EducationCampaignBehavior.EducationPage.EducationPageConditionDelegate Condition;

			// Token: 0x04001F6B RID: 8043
			public readonly TextObject Title;

			// Token: 0x04001F6C RID: 8044
			public readonly TextObject Description;

			// Token: 0x04001F6D RID: 8045
			public readonly TextObject Instruction;

			// Token: 0x04001F6E RID: 8046
			private readonly string _id;

			// Token: 0x04001F6F RID: 8047
			private int _keyIndex;

			// Token: 0x04001F70 RID: 8048
			private readonly Dictionary<string, EducationCampaignBehavior.EducationOption> _options;

			// Token: 0x04001F71 RID: 8049
			public readonly EducationCampaignBehavior.EducationCharacterProperties ChildProperties;

			// Token: 0x04001F72 RID: 8050
			public readonly EducationCampaignBehavior.EducationCharacterProperties SpecialCharacterProperties;

			// Token: 0x020008F2 RID: 2290
			// (Invoke) Token: 0x06006905 RID: 26885
			public delegate bool EducationPageConditionDelegate(EducationCampaignBehavior.EducationPage page, List<EducationCampaignBehavior.EducationOption> previousOptions);
		}
	}
}
