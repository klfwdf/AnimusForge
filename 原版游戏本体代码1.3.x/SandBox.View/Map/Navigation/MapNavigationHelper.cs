using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation
{
	// Token: 0x02000068 RID: 104
	public static class MapNavigationHelper
	{
		// Token: 0x06000472 RID: 1138 RVA: 0x000245DC File Offset: 0x000227DC
		public static InquiryData GetUnsavedChangedInquiry(Action openNewScreenAction)
		{
			return new InquiryData(string.Empty, GameTexts.FindText("str_unsaved_changes", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
			{
				MapNavigationHelper.ApplyCurrentChanges();
				MapNavigationHelper.SwitchToANewScreen(openNewScreenAction);
			}, delegate()
			{
				MapNavigationHelper.SwitchToANewScreen(openNewScreenAction);
			}, "", 0f, null, null, null);
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x00024658 File Offset: 0x00022858
		public static InquiryData GetUnapplicableChangedInquiry()
		{
			return new InquiryData(string.Empty, GameTexts.FindText("str_unapplicable_changes", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), null, null, "", 0f, null, null, null);
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x000246B0 File Offset: 0x000228B0
		public static bool IsMapTopScreen()
		{
			return ScreenManager.TopScreen is MapScreen;
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x000246C0 File Offset: 0x000228C0
		public static bool IsNavigationBarEnabled(MapNavigationHandler handler)
		{
			if (Hero.MainHero != null)
			{
				Hero mainHero = Hero.MainHero;
				if (mainHero == null || !mainHero.IsDead)
				{
					Campaign campaign = Campaign.Current;
					if (campaign == null || !campaign.SaveHandler.IsSaving)
					{
						if (handler != null && handler.IsNavigationLocked)
						{
							return false;
						}
						if (PlayerEncounter.CurrentBattleSimulation != null)
						{
							return false;
						}
						MapScreen mapScreen;
						if ((mapScreen = ScreenManager.TopScreen as MapScreen) != null && (mapScreen.IsInArmyManagement || mapScreen.IsMarriageOfferPopupActive || mapScreen.IsHeirSelectionPopupActive || mapScreen.IsMapCheatsActive || mapScreen.IsMapIncidentActive || mapScreen.EncyclopediaScreenManager.IsEncyclopediaOpen))
						{
							return false;
						}
						if (handler != null && handler.IsEscapeMenuActive)
						{
							return false;
						}
						INavigationElement[] elements = handler.GetElements();
						for (int i = 0; i < elements.Length; i++)
						{
							if (elements[i].IsLockingNavigation)
							{
								return false;
							}
						}
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x0002478C File Offset: 0x0002298C
		private static void ApplyCurrentChanges()
		{
			IChangeableScreen changeableScreen;
			if ((changeableScreen = ScreenManager.TopScreen as IChangeableScreen) != null && changeableScreen.AnyUnsavedChanges())
			{
				if (changeableScreen.CanChangesBeApplied())
				{
					changeableScreen.ApplyChanges();
					return;
				}
				changeableScreen.ResetChanges();
			}
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x000247C4 File Offset: 0x000229C4
		public static void SwitchToANewScreen(Action openNewScreenAction)
		{
			if (!MapNavigationHelper.IsMapTopScreen())
			{
				Game.Current.GameStateManager.PopState(0);
			}
			if (openNewScreenAction != null)
			{
				openNewScreenAction();
			}
		}
	}
}
