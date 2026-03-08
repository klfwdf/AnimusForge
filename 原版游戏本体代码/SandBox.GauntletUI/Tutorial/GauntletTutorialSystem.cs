using System;
using System.Collections.Generic;
using System.Reflection;
using SandBox.View.Map;
using SandBox.ViewModelCollection.MapSiege;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Tutorial
{
	// Token: 0x02000014 RID: 20
	public class GauntletTutorialSystem : GlobalLayer
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000D8 RID: 216 RVA: 0x00007BA4 File Offset: 0x00005DA4
		// (set) Token: 0x060000D9 RID: 217 RVA: 0x00007BAC File Offset: 0x00005DAC
		public EncyclopediaPages CurrentEncyclopediaPageContext { get; private set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x060000DA RID: 218 RVA: 0x00007BB5 File Offset: 0x00005DB5
		// (set) Token: 0x060000DB RID: 219 RVA: 0x00007BBD File Offset: 0x00005DBD
		public bool IsCharacterPortraitPopupOpen { get; private set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x060000DC RID: 220 RVA: 0x00007BC6 File Offset: 0x00005DC6
		// (set) Token: 0x060000DD RID: 221 RVA: 0x00007BCE File Offset: 0x00005DCE
		public TutorialContexts CurrentContext { get; private set; }

		// Token: 0x060000DE RID: 222 RVA: 0x00007BD8 File Offset: 0x00005DD8
		public GauntletTutorialSystem()
		{
			this._isInitialized = true;
			this._dataSource = new TutorialVM(new Action(this.DisableTutorialStep));
			base.Layer = new GauntletLayer("TutorialScreen", 15300, false);
			GauntletLayer gauntletLayer = (GauntletLayer)base.Layer;
			this._movie = gauntletLayer.LoadMovie("TutorialScreen", this._dataSource);
			gauntletLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
			ScreenManager.AddGlobalLayer(this, true);
			this._mappedTutorialItems = new Dictionary<string, TutorialItemBase>();
			this._tutorialItemIdentifiers = new Dictionary<TutorialItemBase, string>();
			this._currentlyAvailableTutorialItems = new List<TutorialItemBase>();
			this._currentlyAvailableTutorialItemsCopy = new TutorialItemBase[0];
			this.RegisterEvents();
			this.RegisterTutorialTypes();
			this.UpdateKeytexts();
			this._currentCampaignTutorials = new List<CampaignTutorial>();
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00007CA0 File Offset: 0x00005EA0
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			if (!this._isInitialized)
			{
				return;
			}
			if (this._currentlyAvailableTutorialItemsCopy.Length != this._currentlyAvailableTutorialItems.Capacity)
			{
				this._currentlyAvailableTutorialItemsCopy = new TutorialItemBase[this._currentlyAvailableTutorialItems.Capacity];
			}
			this._currentlyAvailableTutorialItems.CopyTo(this._currentlyAvailableTutorialItemsCopy);
			int count = this._currentlyAvailableTutorialItems.Count;
			if (this._currentTutorial == null)
			{
				this._currentCampaignTutorials.Clear();
				this._currentlyAvailableTutorialItems.Clear();
				if (CampaignEventDispatcher.Instance != null)
				{
					CampaignEventDispatcher.Instance.CollectAvailableTutorials(ref this._currentCampaignTutorials);
					foreach (CampaignTutorial campaignTutorial in this._currentCampaignTutorials)
					{
						TutorialItemBase tutorialItemBase;
						if (this._mappedTutorialItems.TryGetValue(campaignTutorial.TutorialTypeId, out tutorialItemBase))
						{
							if (tutorialItemBase.GetTutorialsRelevantContext() == this.CurrentContext)
							{
								this._currentlyAvailableTutorialItems.Add(tutorialItemBase);
							}
							if (this._currentTutorial == null && tutorialItemBase.GetTutorialsRelevantContext() == this.CurrentContext && tutorialItemBase.IsConditionsMetForActivation())
							{
								this.SetCurrentTutorial(campaignTutorial, tutorialItemBase);
							}
						}
					}
				}
			}
			for (int i = 0; i < count; i++)
			{
				if (this._currentlyAvailableTutorialItems.IndexOf(this._currentlyAvailableTutorialItemsCopy[i]) < 0)
				{
					this._currentlyAvailableTutorialItemsCopy[i].OnDeactivate();
				}
			}
			if (this._currentlyAvailableTutorialItemsCopy.Length != this._currentlyAvailableTutorialItems.Capacity)
			{
				this._currentlyAvailableTutorialItemsCopy = new TutorialItemBase[this._currentlyAvailableTutorialItems.Capacity];
			}
			else
			{
				this._currentlyAvailableTutorialItemsCopy.Initialize();
			}
			this._currentlyAvailableTutorialItems.CopyTo(this._currentlyAvailableTutorialItemsCopy);
			for (int j = 0; j < this._currentlyAvailableTutorialItems.Count; j++)
			{
				TutorialItemBase tutorialItemBase2 = this._currentlyAvailableTutorialItemsCopy[j];
				if (tutorialItemBase2.IsConditionsMetForCompletion())
				{
					string text = this._tutorialItemIdentifiers[tutorialItemBase2];
					CampaignEventDispatcher.Instance.OnTutorialCompleted(text);
					this._currentlyAvailableTutorialItems.Remove(tutorialItemBase2);
					if (Mission.Current != null)
					{
						List<MissionBehavior> missionBehaviors = Mission.Current.MissionBehaviors;
						if (missionBehaviors != null)
						{
							for (int k = 0; k < missionBehaviors.Count; k++)
							{
								MissionBehavior missionBehavior = missionBehaviors[k];
								if (missionBehavior != null)
								{
									missionBehavior.OnTutorialCompleted(text);
								}
							}
						}
					}
					if (tutorialItemBase2 == this._currentTutorialVisualItem)
					{
						this.ResetCurrentTutorial();
					}
					else
					{
						Debug.Print("Completed a non-active tutorial: " + text, 0, Debug.DebugColor.White, 17592186044416UL);
					}
				}
			}
			this._currentlyAvailableTutorialItemsCopy.Initialize();
			TutorialItemBase currentTutorialVisualItem = this._currentTutorialVisualItem;
			if (currentTutorialVisualItem == null || currentTutorialVisualItem.IsConditionsMetForActivation())
			{
				TutorialItemBase currentTutorialVisualItem2 = this._currentTutorialVisualItem;
				TutorialContexts? tutorialContexts = ((currentTutorialVisualItem2 != null) ? new TutorialContexts?(currentTutorialVisualItem2.GetTutorialsRelevantContext()) : null);
				TutorialContexts currentContext = this.CurrentContext;
				if ((tutorialContexts.GetValueOrDefault() == currentContext) & (tutorialContexts != null))
				{
					goto IL_2D0;
				}
			}
			this.ResetCurrentTutorial();
			IL_2D0:
			if (this._currentTutorialVisualItem != null && this._currentlyAvailableTutorialItems.IndexOf(this._currentTutorialVisualItem) < 0)
			{
				this.ResetCurrentTutorial();
			}
			TutorialVM dataSource = this._dataSource;
			TutorialItemBase currentTutorialVisualItem3 = this._currentTutorialVisualItem;
			dataSource.IsVisible = currentTutorialVisualItem3 != null && currentTutorialVisualItem3.IsConditionsMetForVisibility();
			this._dataSource.Tick(dt);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00007FD8 File Offset: 0x000061D8
		private void SetCurrentTutorial(CampaignTutorial tutorial, TutorialItemBase tutorialItem)
		{
			this._currentTutorial = tutorial;
			this._currentTutorialVisualItem = tutorialItem;
			Game.Current.EventManager.TriggerEvent<TutorialNotificationElementChangeEvent>(new TutorialNotificationElementChangeEvent(this._currentTutorialVisualItem.HighlightedVisualElementID));
			this._dataSource.SetCurrentTutorial(tutorialItem.Placement, tutorial.TutorialTypeId, tutorialItem.MouseRequired);
			if (tutorialItem.MouseRequired)
			{
				base.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.MouseButtons);
			}
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x0000804C File Offset: 0x0000624C
		private void ResetCurrentTutorial()
		{
			this._currentTutorial = null;
			this._currentTutorialVisualItem = null;
			TutorialVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.CloseTutorialStep(false);
			}
			Game.Current.EventManager.TriggerEvent<TutorialNotificationElementChangeEvent>(new TutorialNotificationElementChangeEvent(string.Empty));
			ScreenLayer layer = base.Layer;
			if (layer == null)
			{
				return;
			}
			layer.InputRestrictions.ResetInputRestrictions();
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x000080A8 File Offset: 0x000062A8
		private void OnTutorialContextChanged(TutorialContextChangedEvent obj)
		{
			this.CurrentContext = obj.NewContext;
			this.IsCharacterPortraitPopupOpen = false;
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnTutorialContextChanged(obj);
			});
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x000080F1 File Offset: 0x000062F1
		private void DisableTutorialStep()
		{
			CampaignEventDispatcher.Instance.OnTutorialCompleted(this._currentTutorial.TutorialTypeId);
			this.ResetCurrentTutorial();
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000810E File Offset: 0x0000630E
		public static void OnInitialize()
		{
			if (GauntletTutorialSystem.Current == null)
			{
				GauntletTutorialSystem.Current = new GauntletTutorialSystem();
			}
			bool isInitialized = GauntletTutorialSystem.Current._isInitialized;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000812C File Offset: 0x0000632C
		public static void OnUnload()
		{
			if (GauntletTutorialSystem.Current != null)
			{
				if (GauntletTutorialSystem.Current._isInitialized)
				{
					GauntletTutorialSystem.Current.UnregisterEvents();
					GauntletTutorialSystem.Current._isInitialized = false;
					TutorialVM.Instance = null;
					GauntletTutorialSystem.Current._dataSource = null;
					ScreenManager.RemoveGlobalLayer(GauntletTutorialSystem.Current);
					(GauntletTutorialSystem.Current.Layer as GauntletLayer).ReleaseMovie(GauntletTutorialSystem.Current._movie);
				}
				GauntletTutorialSystem.Current = null;
			}
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x000081A0 File Offset: 0x000063A0
		private void OnEncyclopediaPageChanged(EncyclopediaPageChangedEvent obj)
		{
			this.CurrentEncyclopediaPageContext = obj.NewPage;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x000081B0 File Offset: 0x000063B0
		private void OnPerkSelectionToggle(PerkSelectionToggleEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPerkSelectionToggle(obj);
			});
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x000081E4 File Offset: 0x000063E4
		private void OnInventoryTransferItem(InventoryTransferItemEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnInventoryTransferItem(obj);
			});
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00008218 File Offset: 0x00006418
		private void OnInventoryEquipmentTypeChange(InventoryEquipmentTypeChangedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnInventoryEquipmentTypeChange(obj);
			});
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000824C File Offset: 0x0000644C
		private void OnFocusAddedByPlayer(FocusAddedByPlayerEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnFocusAddedByPlayer(obj);
			});
		}

		// Token: 0x060000EB RID: 235 RVA: 0x00008280 File Offset: 0x00006480
		private void OnPerkSelectedByPlayer(PerkSelectedByPlayerEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPerkSelectedByPlayer(obj);
			});
		}

		// Token: 0x060000EC RID: 236 RVA: 0x000082B4 File Offset: 0x000064B4
		private void OnPartyAddedToArmyByPlayer(PartyAddedToArmyByPlayerEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPartyAddedToArmyByPlayer(obj);
			});
		}

		// Token: 0x060000ED RID: 237 RVA: 0x000082E8 File Offset: 0x000064E8
		private void OnArmyCohesionByPlayerBoosted(ArmyCohesionBoostedByPlayerEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnArmyCohesionByPlayerBoosted(obj);
			});
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000831C File Offset: 0x0000651C
		private void OnInventoryFilterChanged(InventoryFilterChangedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnInventoryFilterChanged(obj);
			});
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00008350 File Offset: 0x00006550
		private void OnPlayerToggleTrackSettlementFromEncyclopedia(PlayerToggleTrackSettlementFromEncyclopediaEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerToggleTrackSettlementFromEncyclopedia(obj);
			});
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00008384 File Offset: 0x00006584
		private void OnMissionNameMarkerToggled(MissionNameMarkerToggleEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnMissionNameMarkerToggled(obj);
			});
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x000083B8 File Offset: 0x000065B8
		private void OnPlayerStartEngineConstruction(PlayerStartEngineConstructionEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerStartEngineConstruction(obj);
			});
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x000083EC File Offset: 0x000065EC
		private void OnPlayerInspectedPartySpeed(PlayerInspectedPartySpeedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerInspectedPartySpeed(obj);
			});
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00008420 File Offset: 0x00006620
		private void OnGameMenuOpened(MenuCallbackArgs obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnGameMenuOpened(obj);
			});
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00008454 File Offset: 0x00006654
		private void OnCharacterPortraitPopUpOpened(CharacterObject obj)
		{
			this.IsCharacterPortraitPopupOpen = true;
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnCharacterPortraitPopUpOpened(obj);
			});
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000848C File Offset: 0x0000668C
		private void OnCharacterPortraitPopUpClosed()
		{
			this.IsCharacterPortraitPopupOpen = false;
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00008498 File Offset: 0x00006698
		private void OnPlayerStartTalkFromMenuOverlay(Hero obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerStartTalkFromMenuOverlay(obj);
			});
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x000084CC File Offset: 0x000066CC
		private void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnGameMenuOptionSelected(gameMenuOption);
			});
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00008500 File Offset: 0x00006700
		private void OnPlayerStartRecruitment(CharacterObject obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerStartRecruitment(obj);
			});
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00008534 File Offset: 0x00006734
		private void OnNewCompanionAdded(Hero obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnNewCompanionAdded(obj);
			});
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00008568 File Offset: 0x00006768
		private void OnPlayerRecruitUnit(CharacterObject obj, int count)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerRecruitedUnit(obj, count);
			});
		}

		// Token: 0x060000FB RID: 251 RVA: 0x000085A0 File Offset: 0x000067A0
		private void OnPlayerInventoryExchange(List<ValueTuple<ItemRosterElement, int>> purchasedItems, List<ValueTuple<ItemRosterElement, int>> soldItems, bool isTrading)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerInventoryExchange(purchasedItems, soldItems, isTrading);
			});
		}

		// Token: 0x060000FC RID: 252 RVA: 0x000085E0 File Offset: 0x000067E0
		private void OnPlayerUpgradeTroop(PlayerRequestUpgradeTroopEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerUpgradeTroop(obj.SourceTroop, obj.TargetTroop, obj.Number);
			});
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00008614 File Offset: 0x00006814
		private void OnPlayerMoveTroop(PlayerMoveTroopEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerMoveTroop(obj);
			});
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00008648 File Offset: 0x00006848
		private void OnPlayerToggledUpgradePopup(PlayerToggledUpgradePopupEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerToggledUpgradePopup(obj);
			});
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000867C File Offset: 0x0000687C
		private void OnOrderOfBattleHeroAssignedToFormation(OrderOfBattleHeroAssignedToFormationEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnOrderOfBattleHeroAssignedToFormation(obj);
			});
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000086B0 File Offset: 0x000068B0
		private void OnPlayerMovementFlagsChanged(MissionPlayerMovementFlagsChangeEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerMovementFlagChanged(obj);
			});
		}

		// Token: 0x06000101 RID: 257 RVA: 0x000086E4 File Offset: 0x000068E4
		private void OnOrderOfBattleFormationClassChanged(OrderOfBattleFormationClassChangedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnOrderOfBattleFormationClassChanged(obj);
			});
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00008718 File Offset: 0x00006918
		private void OnOrderOfBattleFormationWeightChanged(OrderOfBattleFormationWeightChangedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnOrderOfBattleFormationWeightChanged(obj);
			});
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000874C File Offset: 0x0000694C
		private void OnCraftingWeaponClassSelectionOpened(CraftingWeaponClassSelectionOpenedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnCraftingWeaponClassSelectionOpened(obj);
			});
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00008780 File Offset: 0x00006980
		private void OnCraftingOnWeaponResultPopupOpened(CraftingWeaponResultPopupToggledEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnCraftingOnWeaponResultPopupOpened(obj);
			});
		}

		// Token: 0x06000105 RID: 261 RVA: 0x000087B4 File Offset: 0x000069B4
		private void OnCraftingOrderTabOpened(CraftingOrderTabOpenedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnCraftingOrderTabOpened(obj);
			});
		}

		// Token: 0x06000106 RID: 262 RVA: 0x000087E8 File Offset: 0x000069E8
		private void OnCraftingOrderSelectionOpened(CraftingOrderSelectionOpenedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnCraftingOrderSelectionOpened(obj);
			});
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000881C File Offset: 0x00006A1C
		private void OnInventoryItemInspected(InventoryItemInspectedEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnInventoryItemInspected(obj);
			});
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00008850 File Offset: 0x00006A50
		private void OnCrimeValueInspectedInSettlementOverlay(CrimeValueInspectedInSettlementOverlayEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnCrimeValueInspectedInSettlementOverlay(obj);
			});
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00008884 File Offset: 0x00006A84
		private void OnClanRoleAssignedThroughClanScreen(ClanRoleAssignedThroughClanScreenEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnClanRoleAssignedThroughClanScreen(obj);
			});
		}

		// Token: 0x0600010A RID: 266 RVA: 0x000088B8 File Offset: 0x00006AB8
		private void OnMainMapCameraMove(MapScreen.MainMapCameraMoveEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnMainMapCameraMove(obj);
			});
		}

		// Token: 0x0600010B RID: 267 RVA: 0x000088EC File Offset: 0x00006AEC
		private void OnPlayerSelectedAKingdomDecisionOption(PlayerSelectedAKingdomDecisionOptionEvent obj)
		{
			this._currentlyAvailableTutorialItems.ForEach(delegate(TutorialItemBase t)
			{
				t.OnPlayerSelectedAKingdomDecisionOption(obj);
			});
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000891D File Offset: 0x00006B1D
		private void OnResetAllTutorials(ResetAllTutorialsEvent obj)
		{
			this._mappedTutorialItems.Clear();
			this._tutorialItemIdentifiers.Clear();
			this.RegisterTutorialTypes();
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000893B File Offset: 0x00006B3B
		private void OnGamepadActiveStateChanged()
		{
			this.UpdateKeytexts();
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00008943 File Offset: 0x00006B43
		private void OnKeybindsChanged()
		{
			this.UpdateKeytexts();
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000894C File Offset: 0x00006B4C
		private void RegisterTutorialTypes()
		{
			foreach (Assembly assembly in ModuleHelper.GetActiveGameAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (typeof(TutorialItemBase).IsAssignableFrom(type) && !type.IsAbstract)
					{
						TutorialAttribute customAttribute = type.GetCustomAttribute<TutorialAttribute>();
						if (customAttribute == null)
						{
							Debug.FailedAssert("Tutorial: " + type.Name + " does not have a Tutorial attribute", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Tutorial\\GauntletTutorialSystem.cs", "RegisterTutorialTypes", 508);
						}
						else
						{
							ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
							if (constructor == null)
							{
								Debug.FailedAssert("Tutorial: " + type.Name + " does not have a parameterless constructor", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Tutorial\\GauntletTutorialSystem.cs", "RegisterTutorialTypes", 516);
							}
							else
							{
								TutorialItemBase tutorialItemBase = (TutorialItemBase)constructor.Invoke(new object[0]);
								string tutorialIdentifier = customAttribute.TutorialIdentifier;
								if (string.IsNullOrEmpty(tutorialIdentifier))
								{
									Debug.FailedAssert("Tutorial: " + type.Name + " does not have a valid identifier", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Tutorial\\GauntletTutorialSystem.cs", "RegisterTutorialTypes", 526);
								}
								else
								{
									this._mappedTutorialItems[tutorialIdentifier] = tutorialItemBase;
									this._tutorialItemIdentifiers[tutorialItemBase] = tutorialIdentifier;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00008AD4 File Offset: 0x00006CD4
		private void RegisterEvents()
		{
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
			Game.Current.EventManager.RegisterEvent<InventoryTransferItemEvent>(new Action<InventoryTransferItemEvent>(this.OnInventoryTransferItem));
			Game.Current.EventManager.RegisterEvent<InventoryEquipmentTypeChangedEvent>(new Action<InventoryEquipmentTypeChangedEvent>(this.OnInventoryEquipmentTypeChange));
			Game.Current.EventManager.RegisterEvent<FocusAddedByPlayerEvent>(new Action<FocusAddedByPlayerEvent>(this.OnFocusAddedByPlayer));
			Game.Current.EventManager.RegisterEvent<PerkSelectedByPlayerEvent>(new Action<PerkSelectedByPlayerEvent>(this.OnPerkSelectedByPlayer));
			Game.Current.EventManager.RegisterEvent<ArmyCohesionBoostedByPlayerEvent>(new Action<ArmyCohesionBoostedByPlayerEvent>(this.OnArmyCohesionByPlayerBoosted));
			Game.Current.EventManager.RegisterEvent<PartyAddedToArmyByPlayerEvent>(new Action<PartyAddedToArmyByPlayerEvent>(this.OnPartyAddedToArmyByPlayer));
			Game.Current.EventManager.RegisterEvent<InventoryFilterChangedEvent>(new Action<InventoryFilterChangedEvent>(this.OnInventoryFilterChanged));
			Game.Current.EventManager.RegisterEvent<EncyclopediaPageChangedEvent>(new Action<EncyclopediaPageChangedEvent>(this.OnEncyclopediaPageChanged));
			Game.Current.EventManager.RegisterEvent<PerkSelectionToggleEvent>(new Action<PerkSelectionToggleEvent>(this.OnPerkSelectionToggle));
			Game.Current.EventManager.RegisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(new Action<PlayerToggleTrackSettlementFromEncyclopediaEvent>(this.OnPlayerToggleTrackSettlementFromEncyclopedia));
			Game.Current.EventManager.RegisterEvent<TutorialContextChangedEvent>(new Action<TutorialContextChangedEvent>(this.OnTutorialContextChanged));
			Game.Current.EventManager.RegisterEvent<MissionNameMarkerToggleEvent>(new Action<MissionNameMarkerToggleEvent>(this.OnMissionNameMarkerToggled));
			Game.Current.EventManager.RegisterEvent<PlayerRequestUpgradeTroopEvent>(new Action<PlayerRequestUpgradeTroopEvent>(this.OnPlayerUpgradeTroop));
			Game.Current.EventManager.RegisterEvent<PlayerStartEngineConstructionEvent>(new Action<PlayerStartEngineConstructionEvent>(this.OnPlayerStartEngineConstruction));
			Game.Current.EventManager.RegisterEvent<PlayerInspectedPartySpeedEvent>(new Action<PlayerInspectedPartySpeedEvent>(this.OnPlayerInspectedPartySpeed));
			Game.Current.EventManager.RegisterEvent<MapScreen.MainMapCameraMoveEvent>(new Action<MapScreen.MainMapCameraMoveEvent>(this.OnMainMapCameraMove));
			Game.Current.EventManager.RegisterEvent<PlayerMoveTroopEvent>(new Action<PlayerMoveTroopEvent>(this.OnPlayerMoveTroop));
			Game.Current.EventManager.RegisterEvent<MissionPlayerMovementFlagsChangeEvent>(new Action<MissionPlayerMovementFlagsChangeEvent>(this.OnPlayerMovementFlagsChanged));
			Game.Current.EventManager.RegisterEvent<ResetAllTutorialsEvent>(new Action<ResetAllTutorialsEvent>(this.OnResetAllTutorials));
			Game.Current.EventManager.RegisterEvent<PlayerToggledUpgradePopupEvent>(new Action<PlayerToggledUpgradePopupEvent>(this.OnPlayerToggledUpgradePopup));
			Game.Current.EventManager.RegisterEvent<OrderOfBattleHeroAssignedToFormationEvent>(new Action<OrderOfBattleHeroAssignedToFormationEvent>(this.OnOrderOfBattleHeroAssignedToFormation));
			Game.Current.EventManager.RegisterEvent<OrderOfBattleFormationClassChangedEvent>(new Action<OrderOfBattleFormationClassChangedEvent>(this.OnOrderOfBattleFormationClassChanged));
			Game.Current.EventManager.RegisterEvent<OrderOfBattleFormationWeightChangedEvent>(new Action<OrderOfBattleFormationWeightChangedEvent>(this.OnOrderOfBattleFormationWeightChanged));
			Game.Current.EventManager.RegisterEvent<CraftingWeaponClassSelectionOpenedEvent>(new Action<CraftingWeaponClassSelectionOpenedEvent>(this.OnCraftingWeaponClassSelectionOpened));
			Game.Current.EventManager.RegisterEvent<CraftingOrderTabOpenedEvent>(new Action<CraftingOrderTabOpenedEvent>(this.OnCraftingOrderTabOpened));
			Game.Current.EventManager.RegisterEvent<CraftingOrderSelectionOpenedEvent>(new Action<CraftingOrderSelectionOpenedEvent>(this.OnCraftingOrderSelectionOpened));
			Game.Current.EventManager.RegisterEvent<CraftingWeaponResultPopupToggledEvent>(new Action<CraftingWeaponResultPopupToggledEvent>(this.OnCraftingOnWeaponResultPopupOpened));
			Game.Current.EventManager.RegisterEvent<InventoryItemInspectedEvent>(new Action<InventoryItemInspectedEvent>(this.OnInventoryItemInspected));
			Game.Current.EventManager.RegisterEvent<CrimeValueInspectedInSettlementOverlayEvent>(new Action<CrimeValueInspectedInSettlementOverlayEvent>(this.OnCrimeValueInspectedInSettlementOverlay));
			Game.Current.EventManager.RegisterEvent<ClanRoleAssignedThroughClanScreenEvent>(new Action<ClanRoleAssignedThroughClanScreenEvent>(this.OnClanRoleAssignedThroughClanScreen));
			Game.Current.EventManager.RegisterEvent<PlayerSelectedAKingdomDecisionOptionEvent>(new Action<PlayerSelectedAKingdomDecisionOptionEvent>(this.OnPlayerSelectedAKingdomDecisionOption));
			HotKeyManager.OnKeybindsChanged += this.OnKeybindsChanged;
			if (Campaign.Current != null && CampaignEventDispatcher.Instance != null)
			{
				CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
				CampaignEvents.CharacterPortraitPopUpOpenedEvent.AddNonSerializedListener(this, new Action<CharacterObject>(this.OnCharacterPortraitPopUpOpened));
				CampaignEvents.CharacterPortraitPopUpClosedEvent.AddNonSerializedListener(this, new Action(this.OnCharacterPortraitPopUpClosed));
				CampaignEvents.PlayerStartTalkFromMenu.AddNonSerializedListener(this, new Action<Hero>(this.OnPlayerStartTalkFromMenuOverlay));
				CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, new Action<GameMenu, GameMenuOption>(this.OnGameMenuOptionSelected));
				CampaignEvents.PlayerStartRecruitmentEvent.AddNonSerializedListener(this, new Action<CharacterObject>(this.OnPlayerStartRecruitment));
				CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, new Action<Hero>(this.OnNewCompanionAdded));
				CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, new Action<CharacterObject, int>(this.OnPlayerRecruitUnit));
				CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, new Action<List<ValueTuple<ItemRosterElement, int>>, List<ValueTuple<ItemRosterElement, int>>, bool>(this.OnPlayerInventoryExchange));
			}
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00008F3C File Offset: 0x0000713C
		private void UnregisterEvents()
		{
			Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(Input.OnGamepadActiveStateChanged, new Action(this.OnGamepadActiveStateChanged));
			Game game = Game.Current;
			if (game != null)
			{
				game.EventManager.UnregisterEvent<InventoryTransferItemEvent>(new Action<InventoryTransferItemEvent>(this.OnInventoryTransferItem));
			}
			Game game2 = Game.Current;
			if (game2 != null)
			{
				game2.EventManager.UnregisterEvent<InventoryEquipmentTypeChangedEvent>(new Action<InventoryEquipmentTypeChangedEvent>(this.OnInventoryEquipmentTypeChange));
			}
			Game game3 = Game.Current;
			if (game3 != null)
			{
				game3.EventManager.UnregisterEvent<FocusAddedByPlayerEvent>(new Action<FocusAddedByPlayerEvent>(this.OnFocusAddedByPlayer));
			}
			Game game4 = Game.Current;
			if (game4 != null)
			{
				game4.EventManager.UnregisterEvent<PerkSelectedByPlayerEvent>(new Action<PerkSelectedByPlayerEvent>(this.OnPerkSelectedByPlayer));
			}
			Game game5 = Game.Current;
			if (game5 != null)
			{
				game5.EventManager.UnregisterEvent<ArmyCohesionBoostedByPlayerEvent>(new Action<ArmyCohesionBoostedByPlayerEvent>(this.OnArmyCohesionByPlayerBoosted));
			}
			Game game6 = Game.Current;
			if (game6 != null)
			{
				game6.EventManager.UnregisterEvent<PartyAddedToArmyByPlayerEvent>(new Action<PartyAddedToArmyByPlayerEvent>(this.OnPartyAddedToArmyByPlayer));
			}
			Game game7 = Game.Current;
			if (game7 != null)
			{
				game7.EventManager.UnregisterEvent<InventoryFilterChangedEvent>(new Action<InventoryFilterChangedEvent>(this.OnInventoryFilterChanged));
			}
			Game game8 = Game.Current;
			if (game8 != null)
			{
				game8.EventManager.UnregisterEvent<EncyclopediaPageChangedEvent>(new Action<EncyclopediaPageChangedEvent>(this.OnEncyclopediaPageChanged));
			}
			Game game9 = Game.Current;
			if (game9 != null)
			{
				game9.EventManager.UnregisterEvent<PerkSelectionToggleEvent>(new Action<PerkSelectionToggleEvent>(this.OnPerkSelectionToggle));
			}
			Game game10 = Game.Current;
			if (game10 != null)
			{
				game10.EventManager.UnregisterEvent<PlayerToggleTrackSettlementFromEncyclopediaEvent>(new Action<PlayerToggleTrackSettlementFromEncyclopediaEvent>(this.OnPlayerToggleTrackSettlementFromEncyclopedia));
			}
			Game game11 = Game.Current;
			if (game11 != null)
			{
				game11.EventManager.UnregisterEvent<TutorialContextChangedEvent>(new Action<TutorialContextChangedEvent>(this.OnTutorialContextChanged));
			}
			Game game12 = Game.Current;
			if (game12 != null)
			{
				game12.EventManager.UnregisterEvent<MissionNameMarkerToggleEvent>(new Action<MissionNameMarkerToggleEvent>(this.OnMissionNameMarkerToggled));
			}
			Game game13 = Game.Current;
			if (game13 != null)
			{
				game13.EventManager.UnregisterEvent<PlayerRequestUpgradeTroopEvent>(new Action<PlayerRequestUpgradeTroopEvent>(this.OnPlayerUpgradeTroop));
			}
			Game game14 = Game.Current;
			if (game14 != null)
			{
				game14.EventManager.UnregisterEvent<PlayerStartEngineConstructionEvent>(new Action<PlayerStartEngineConstructionEvent>(this.OnPlayerStartEngineConstruction));
			}
			Game game15 = Game.Current;
			if (game15 != null)
			{
				game15.EventManager.UnregisterEvent<PlayerInspectedPartySpeedEvent>(new Action<PlayerInspectedPartySpeedEvent>(this.OnPlayerInspectedPartySpeed));
			}
			Game game16 = Game.Current;
			if (game16 != null)
			{
				game16.EventManager.UnregisterEvent<MapScreen.MainMapCameraMoveEvent>(new Action<MapScreen.MainMapCameraMoveEvent>(this.OnMainMapCameraMove));
			}
			Game game17 = Game.Current;
			if (game17 != null)
			{
				game17.EventManager.UnregisterEvent<PlayerMoveTroopEvent>(new Action<PlayerMoveTroopEvent>(this.OnPlayerMoveTroop));
			}
			Game game18 = Game.Current;
			if (game18 != null)
			{
				game18.EventManager.UnregisterEvent<MissionPlayerMovementFlagsChangeEvent>(new Action<MissionPlayerMovementFlagsChangeEvent>(this.OnPlayerMovementFlagsChanged));
			}
			Game game19 = Game.Current;
			if (game19 != null)
			{
				game19.EventManager.UnregisterEvent<ResetAllTutorialsEvent>(new Action<ResetAllTutorialsEvent>(this.OnResetAllTutorials));
			}
			Game game20 = Game.Current;
			if (game20 != null)
			{
				game20.EventManager.UnregisterEvent<PlayerToggledUpgradePopupEvent>(new Action<PlayerToggledUpgradePopupEvent>(this.OnPlayerToggledUpgradePopup));
			}
			Game game21 = Game.Current;
			if (game21 != null)
			{
				game21.EventManager.UnregisterEvent<OrderOfBattleHeroAssignedToFormationEvent>(new Action<OrderOfBattleHeroAssignedToFormationEvent>(this.OnOrderOfBattleHeroAssignedToFormation));
			}
			Game.Current.EventManager.UnregisterEvent<OrderOfBattleFormationClassChangedEvent>(new Action<OrderOfBattleFormationClassChangedEvent>(this.OnOrderOfBattleFormationClassChanged));
			Game.Current.EventManager.UnregisterEvent<OrderOfBattleFormationWeightChangedEvent>(new Action<OrderOfBattleFormationWeightChangedEvent>(this.OnOrderOfBattleFormationWeightChanged));
			Game.Current.EventManager.UnregisterEvent<CraftingWeaponClassSelectionOpenedEvent>(new Action<CraftingWeaponClassSelectionOpenedEvent>(this.OnCraftingWeaponClassSelectionOpened));
			Game.Current.EventManager.UnregisterEvent<CraftingWeaponResultPopupToggledEvent>(new Action<CraftingWeaponResultPopupToggledEvent>(this.OnCraftingOnWeaponResultPopupOpened));
			Game.Current.EventManager.UnregisterEvent<CraftingOrderTabOpenedEvent>(new Action<CraftingOrderTabOpenedEvent>(this.OnCraftingOrderTabOpened));
			Game.Current.EventManager.UnregisterEvent<CraftingOrderSelectionOpenedEvent>(new Action<CraftingOrderSelectionOpenedEvent>(this.OnCraftingOrderSelectionOpened));
			Game.Current.EventManager.UnregisterEvent<InventoryItemInspectedEvent>(new Action<InventoryItemInspectedEvent>(this.OnInventoryItemInspected));
			Game.Current.EventManager.UnregisterEvent<CrimeValueInspectedInSettlementOverlayEvent>(new Action<CrimeValueInspectedInSettlementOverlayEvent>(this.OnCrimeValueInspectedInSettlementOverlay));
			Game.Current.EventManager.UnregisterEvent<ClanRoleAssignedThroughClanScreenEvent>(new Action<ClanRoleAssignedThroughClanScreenEvent>(this.OnClanRoleAssignedThroughClanScreen));
			Game.Current.EventManager.UnregisterEvent<PlayerSelectedAKingdomDecisionOptionEvent>(new Action<PlayerSelectedAKingdomDecisionOptionEvent>(this.OnPlayerSelectedAKingdomDecisionOption));
			HotKeyManager.OnKeybindsChanged -= this.OnKeybindsChanged;
			if (Campaign.Current != null && CampaignEventDispatcher.Instance != null)
			{
				CampaignEvents.GameMenuOpened.ClearListeners(this);
				CampaignEvents.CharacterPortraitPopUpOpenedEvent.ClearListeners(this);
				CampaignEvents.CharacterPortraitPopUpClosedEvent.ClearListeners(this);
				CampaignEvents.PlayerStartTalkFromMenu.ClearListeners(this);
				CampaignEvents.GameMenuOptionSelectedEvent.ClearListeners(this);
				CampaignEvents.PlayerStartRecruitmentEvent.ClearListeners(this);
				CampaignEvents.NewCompanionAdded.ClearListeners(this);
				CampaignEvents.OnUnitRecruitedEvent.ClearListeners(this);
				CampaignEvents.PlayerInventoryExchangeEvent.ClearListeners(this);
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x000093B0 File Offset: 0x000075B0
		private void UpdateKeytexts()
		{
			string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 5), 1f);
			GameTexts.SetVariable("MISSION_INDICATORS_KEY", keyHyperlinkText);
			string keyHyperlinkText2 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4), 1f);
			GameTexts.SetVariable("LEAVE_MISSION_KEY", keyHyperlinkText2);
			string keyHyperlinkText3 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 87), 1f);
			GameTexts.SetVariable("HOLD_OPEN_ORDER_KEY", keyHyperlinkText3);
			string keyHyperlinkText4 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 69), 1f);
			GameTexts.SetVariable("FIRST_ORDER_CATEGORY_KEY", keyHyperlinkText4);
			string keyHyperlinkText5 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 70), 1f);
			GameTexts.SetVariable("SECOND_ORDER_CATEGORY_KEY", keyHyperlinkText5);
			string keyHyperlinkText6 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 71), 1f);
			GameTexts.SetVariable("THIRD_ORDER_CATEGORY_KEY", keyHyperlinkText6);
			string keyHyperlinkText7 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 72), 1f);
			GameTexts.SetVariable("FOURTH_ORDER_CATEGORY_KEY", keyHyperlinkText7);
			string keyHyperlinkText8 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 79), 1f);
			GameTexts.SetVariable("FIRST_GROUP_HEAR_KEY", keyHyperlinkText8);
			string keyHyperlinkText9 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 80), 1f);
			GameTexts.SetVariable("SECOND_GROUP_HEAR_KEY", keyHyperlinkText9);
			string keyHyperlinkText10 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 88), 1f);
			GameTexts.SetVariable("SELECT_LEFT_FORMATION_KEY", keyHyperlinkText10);
			string keyHyperlinkText11 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 89), 1f);
			GameTexts.SetVariable("SELECT_RIGHT_FORMATION_KEY", keyHyperlinkText11);
			string keyHyperlinkText12 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 0), 1f);
			GameTexts.SetVariable("FORWARD_KEY", keyHyperlinkText12);
			string keyHyperlinkText13 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 1), 1f);
			GameTexts.SetVariable("BACKWARDS_KEY", keyHyperlinkText13);
			string keyHyperlinkText14 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 2), 1f);
			GameTexts.SetVariable("LEFT_KEY", keyHyperlinkText14);
			string keyHyperlinkText15 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 3), 1f);
			GameTexts.SetVariable("RIGHT_KEY", keyHyperlinkText15);
			string keyHyperlinkText16 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13), 1f);
			GameTexts.SetVariable("INTERACTION_KEY", keyHyperlinkText16);
			string keyHyperlinkText17 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", 57), 1f);
			GameTexts.SetVariable("MAP_ZOOM_OUT_KEY", keyHyperlinkText17);
			string keyHyperlinkText18 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", 56), 1f);
			GameTexts.SetVariable("MAP_ZOOM_IN_KEY", keyHyperlinkText18);
			string keyHyperlinkText19 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MapHotKeyCategory", "MapClick"), 1f);
			GameTexts.SetVariable("CONSOLE_ACTION_KEY", keyHyperlinkText19);
			string keyHyperlinkText20 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 30), 1f);
			GameTexts.SetVariable("WALK_MODE_KEY", keyHyperlinkText20);
			string keyHyperlinkText21 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 15), 1f);
			GameTexts.SetVariable("CROUCH_KEY", keyHyperlinkText21);
			string keyHyperlinkText22 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("MissionOrderHotkeyCategory", 90), 1f);
			GameTexts.SetVariable("APPLY_SELECTION_KEY", keyHyperlinkText22);
			GameTexts.SetVariable("CONSOLE_MOVEMENT_KEY", HyperlinkTexts.GetKeyHyperlinkText("ControllerLStick", 1f));
			GameTexts.SetVariable("CONSOLE_CAMERA_KEY", HyperlinkTexts.GetKeyHyperlinkText("ControllerRStick", 1f));
			GameTexts.SetVariable("UPGRADE_ICON", "{=!}<img src=\"PartyScreen\\upgrade_icon\" extend=\"5\">");
		}

		// Token: 0x0400005E RID: 94
		public static GauntletTutorialSystem Current;

		// Token: 0x04000062 RID: 98
		private readonly Dictionary<string, TutorialItemBase> _mappedTutorialItems;

		// Token: 0x04000063 RID: 99
		private readonly Dictionary<TutorialItemBase, string> _tutorialItemIdentifiers;

		// Token: 0x04000064 RID: 100
		private CampaignTutorial _currentTutorial;

		// Token: 0x04000065 RID: 101
		private TutorialItemBase _currentTutorialVisualItem;

		// Token: 0x04000066 RID: 102
		private List<TutorialItemBase> _currentlyAvailableTutorialItems;

		// Token: 0x04000067 RID: 103
		private TutorialItemBase[] _currentlyAvailableTutorialItemsCopy;

		// Token: 0x04000068 RID: 104
		private TutorialVM _dataSource;

		// Token: 0x04000069 RID: 105
		private bool _isInitialized;

		// Token: 0x0400006A RID: 106
		private List<CampaignTutorial> _currentCampaignTutorials;

		// Token: 0x0400006B RID: 107
		private GauntletMovieIdentifier _movie;
	}
}
