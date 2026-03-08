using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;

namespace SandBox.View.Map
{
	// Token: 0x02000046 RID: 70
	public class MapConversationTableau
	{
		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000253 RID: 595 RVA: 0x00015F14 File Offset: 0x00014114
		// (set) Token: 0x06000254 RID: 596 RVA: 0x00015F1C File Offset: 0x0001411C
		public Texture Texture { get; private set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000255 RID: 597 RVA: 0x00015F25 File Offset: 0x00014125
		private TableauView View
		{
			get
			{
				Texture texture = this.Texture;
				if (texture == null)
				{
					return null;
				}
				return texture.TableauView;
			}
		}

		// Token: 0x06000256 RID: 598 RVA: 0x00015F38 File Offset: 0x00014138
		public MapConversationTableau()
		{
			this._changeIdleActionTimer = new Timer(Game.Current.ApplicationTime, 8f, true);
			this._agentVisuals = new List<AgentVisuals>();
			TableauView view = this.View;
			if (view != null)
			{
				view.SetEnable(this._isEnabled);
			}
			this._dataProvider = SandBoxViewSubModule.MapConversationDataProvider;
		}

		// Token: 0x06000257 RID: 599 RVA: 0x0001602C File Offset: 0x0001422C
		public void SetEnabled(bool enabled)
		{
			if (this._isEnabled == enabled)
			{
				return;
			}
			if (enabled)
			{
				TableauView view = this.View;
				if (view != null)
				{
					view.SetEnable(false);
				}
				TableauView view2 = this.View;
				if (view2 != null)
				{
					view2.AddClearTask(true);
				}
				Texture texture = this.Texture;
				if (texture != null)
				{
					texture.Release();
				}
				this.Texture = TableauView.AddTableau(string.Format("MapConvTableau_{0}", MapConversationTableau._tableauIndex++), new RenderTargetComponent.TextureUpdateEventHandler(this.CharacterTableauContinuousRenderFunction), this._tableauScene, this._tableauSizeX, this._tableauSizeY);
				this.Texture.TableauView.SetSceneUsesContour(false);
				this.Texture.TableauView.SetPointlightResolutionMultiplier(0f);
			}
			else
			{
				TableauView view3 = this.View;
				if (view3 != null)
				{
					view3.SetEnable(false);
				}
				TableauView view4 = this.View;
				if (view4 != null)
				{
					view4.ClearAll(false, false);
				}
				this.RemovePreviousAgentsSoundEvent();
				this.StopConversationSoundEvent();
				ThumbnailCacheManager.Current.ReturnCachedMapConversationTableauScene();
			}
			this._isEnabled = enabled;
		}

		// Token: 0x06000258 RID: 600 RVA: 0x00016130 File Offset: 0x00014330
		public void SetData(object data)
		{
			if (this._data == data)
			{
				return;
			}
			if (this._data != null)
			{
				this._initialized = false;
				foreach (AgentVisuals agentVisuals in this._agentVisuals)
				{
					agentVisuals.Reset();
				}
				this._agentVisuals.Clear();
				MapScreen instance = MapScreen.Instance;
				((instance != null) ? instance.GetMapView<MapConversationView>() : null).ConversationMission.SetConversationTableau(null);
			}
			this._data = data as MapConversationTableauData;
		}

		// Token: 0x06000259 RID: 601 RVA: 0x000161CC File Offset: 0x000143CC
		public void SetTargetSize(int width, int height)
		{
			int num;
			int num2;
			if (width <= 0 || height <= 0)
			{
				num = 10;
				num2 = 10;
			}
			else
			{
				this.RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
				num = (int)((float)width * this.RenderScale);
				num2 = (int)((float)height * this.RenderScale);
			}
			if (num != this._tableauSizeX || num2 != this._tableauSizeY)
			{
				this._tableauSizeX = num;
				this._tableauSizeY = num2;
				this._cameraRatio = (float)this._tableauSizeX / (float)this._tableauSizeY;
				TableauView view = this.View;
				if (view != null)
				{
					view.SetEnable(false);
				}
				TableauView view2 = this.View;
				if (view2 != null)
				{
					view2.AddClearTask(true);
				}
				Texture texture = this.Texture;
				if (texture != null)
				{
					texture.Release();
				}
				this.Texture = TableauView.AddTableau(string.Format("MapConvTableau_{0}", MapConversationTableau._tableauIndex++), new RenderTargetComponent.TextureUpdateEventHandler(this.CharacterTableauContinuousRenderFunction), this._tableauScene, this._tableauSizeX, this._tableauSizeY);
			}
		}

		// Token: 0x0600025A RID: 602 RVA: 0x000162C8 File Offset: 0x000144C8
		public void OnFinalize(bool clearNextFrame)
		{
			TableauView view = this.View;
			if (view != null)
			{
				view.SetEnable(false);
			}
			this.RemovePreviousAgentsSoundEvent();
			this.StopConversationSoundEvent();
			Camera continuousRenderCamera = this._continuousRenderCamera;
			if (continuousRenderCamera != null)
			{
				continuousRenderCamera.ReleaseCameraEntity();
			}
			this._continuousRenderCamera = null;
			foreach (AgentVisuals agentVisuals in this._agentVisuals)
			{
				agentVisuals.ResetNextFrame();
			}
			this._agentVisuals = null;
			this.View.ClearAll(false, false);
			this.Texture.Release();
			this.Texture = null;
			IEnumerable<GameEntity> enumerable = this._tableauScene.FindEntitiesWithTag(this._cachedAtmosphereName);
			this._cachedAtmosphereName = "";
			foreach (GameEntity gameEntity in enumerable)
			{
				gameEntity.SetVisibilityExcludeParents(false);
			}
			ThumbnailCacheManager.Current.ReturnCachedMapConversationTableauScene();
			this._tableauScene = null;
		}

		// Token: 0x0600025B RID: 603 RVA: 0x000163D8 File Offset: 0x000145D8
		public void OnTick(float dt)
		{
			if (!this._isEnabled)
			{
				return;
			}
			if (this._data != null && !this._initialized)
			{
				this.FirstTimeInit();
				MapScreen instance = MapScreen.Instance;
				((instance != null) ? instance.GetMapView<MapConversationView>() : null).ConversationMission.SetConversationTableau(this);
			}
			if (this._conversationSoundEvent != null && !this._conversationSoundEvent.IsPlaying())
			{
				this.RemovePreviousAgentsSoundEvent();
				this._conversationSoundEvent.Stop();
				this._conversationSoundEvent = null;
			}
			if (this._animationFrequencyThreshold > this._animationGap)
			{
				this._animationGap += dt;
			}
			TableauView view = this.View;
			if (view != null)
			{
				if (this._continuousRenderCamera == null)
				{
					this._continuousRenderCamera = Camera.CreateCamera();
				}
				view.SetDoNotRenderThisFrame(false);
			}
			if (this._agentVisuals != null && this._agentVisuals.Count > 0)
			{
				this._agentVisuals[0].TickVisuals();
			}
			if (this._agentVisuals[0].GetEquipment().CalculateEquipmentCode() != this._opponentLeaderEquipmentCache)
			{
				this._initialized = false;
				foreach (AgentVisuals agentVisuals in this._agentVisuals)
				{
					agentVisuals.Reset();
				}
				this._agentVisuals.Clear();
			}
		}

		// Token: 0x0600025C RID: 604 RVA: 0x00016538 File Offset: 0x00014738
		private void FirstTimeInit()
		{
			if (this._tableauScene == null)
			{
				this._tableauScene = ThumbnailCacheManager.Current.GetCachedMapConversationTableauScene();
			}
			string atmosphereNameFromData = this._dataProvider.GetAtmosphereNameFromData(this._data);
			this._tableauScene.SetAtmosphereWithName(atmosphereNameFromData);
			IEnumerable<GameEntity> enumerable = this._tableauScene.FindEntitiesWithTag(atmosphereNameFromData);
			this._cachedAtmosphereName = atmosphereNameFromData;
			foreach (GameEntity gameEntity in enumerable)
			{
				gameEntity.SetVisibilityExcludeParents(true);
			}
			if (this._continuousRenderCamera == null)
			{
				this._continuousRenderCamera = Camera.CreateCamera();
				this._cameraEntity = this._tableauScene.FindEntityWithTag("player_infantry_to_infantry");
				Vec3 vec = default(Vec3);
				this._cameraEntity.GetCameraParamsFromCameraScript(this._continuousRenderCamera, ref vec);
				this._baseCameraFOV = this._continuousRenderCamera.HorizontalFov;
			}
			this.SpawnOpponentLeader();
			PartyBase party = this._data.ConversationPartnerData.Party;
			bool flag;
			if (party == null)
			{
				flag = false;
			}
			else
			{
				TroopRoster memberRoster = party.MemberRoster;
				int? num = ((memberRoster != null) ? new int?(memberRoster.TotalManCount) : null);
				int num2 = 1;
				flag = (num.GetValueOrDefault() > num2) & (num != null);
			}
			if (flag)
			{
				int num3 = MathF.Min(2, this._data.ConversationPartnerData.Party.MemberRoster.ToFlattenedRoster().Count<FlattenedTroopRosterElement>() - 1);
				IOrderedEnumerable<TroopRosterElement> orderedEnumerable = from t in this._data.ConversationPartnerData.Party.MemberRoster.GetTroopRoster()
					orderby t.Character.Level descending
					select t;
				foreach (TroopRosterElement troopRosterElement in orderedEnumerable)
				{
					CharacterObject character = troopRosterElement.Character;
					if (character != this._data.ConversationPartnerData.Character && !character.IsPlayerCharacter)
					{
						num3--;
						this.SpawnOpponentBodyguardCharacter(character, num3, this._data.ConversationPartnerData.Party);
					}
					if (num3 == 0)
					{
						break;
					}
				}
				if (num3 == 1)
				{
					num3--;
					TroopRosterElement troopRosterElement2 = orderedEnumerable.FirstOrDefault((TroopRosterElement troop) => !troop.Character.IsHero);
					if (troopRosterElement2.Character != null)
					{
						this.SpawnOpponentBodyguardCharacter(troopRosterElement2.Character, num3, this._data.ConversationPartnerData.Party);
					}
				}
			}
			this._agentVisuals.ForEach(delegate(AgentVisuals a)
			{
				a.SetAgentLodZeroOrMaxExternal(true);
			});
			this._tableauScene.ForceLoadResources(true);
			this._cameraRatio = Screen.RealScreenResolutionWidth / Screen.RealScreenResolutionHeight;
			this.SetTargetSize((int)Screen.RealScreenResolutionWidth, (int)Screen.RealScreenResolutionHeight);
			uint num4 = uint.MaxValue;
			num4 &= 4294966271U;
			TableauView view = this.View;
			if (view != null)
			{
				view.SetPostfxConfigParams((int)num4);
			}
			this._tableauScene.FindEntityWithTag(this.RainingEntityTag).SetVisibilityExcludeParents(this._data.IsRaining);
			this._tableauScene.FindEntityWithTag(this.SnowingEntityTag).SetVisibilityExcludeParents(this._data.IsSnowing);
			this._tableauScene.Tick(3f);
			TableauView view2 = this.View;
			if (view2 != null)
			{
				view2.SetEnable(true);
			}
			this._initialized = true;
		}

		// Token: 0x0600025D RID: 605 RVA: 0x000168A4 File Offset: 0x00014AA4
		private void SpawnOpponentLeader()
		{
			CharacterObject character = this._data.ConversationPartnerData.Character;
			if (character != null)
			{
				GameEntity gameEntity = this._tableauScene.FindEntityWithTag("player_infantry_spawn");
				MapConversationTableau.DefaultConversationAnimationData defaultAnimForCharacter = this.GetDefaultAnimForCharacter(character, false, this._data.ConversationPartnerData.Party);
				this._opponentLeaderEquipmentCache = null;
				Equipment equipment;
				if (this._data.ConversationPartnerData.IsCivilianEquipmentRequiredForLeader)
				{
					equipment = (this._data.ConversationPartnerData.Character.IsHero ? character.FirstCivilianEquipment : character.CivilianEquipments.ElementAt(this._data.ConversationPartnerData.Character.GetDefaultFaceSeed(0) % character.CivilianEquipments.Count<Equipment>()));
				}
				else
				{
					equipment = (this._data.ConversationPartnerData.Character.IsHero ? character.FirstBattleEquipment : character.BattleEquipments.ElementAt(this._data.ConversationPartnerData.Character.GetDefaultFaceSeed(0) % character.BattleEquipments.Count<Equipment>()));
				}
				equipment = equipment.Clone(false);
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
				{
					if (!equipment[equipmentIndex].IsEmpty && equipment[equipmentIndex].Item.Type == ItemObject.ItemTypeEnum.Banner)
					{
						equipment[equipmentIndex] = EquipmentElement.Invalid;
						break;
					}
				}
				int seed = -1;
				if (this._data.ConversationPartnerData.Party != null)
				{
					seed = CharacterHelper.GetPartyMemberFaceSeed(this._data.ConversationPartnerData.Party, character, 0);
				}
				ValueTuple<uint, uint> deterministicColorsForCharacter = CharacterHelper.GetDeterministicColorsForCharacter(character);
				Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
				AgentVisualsData agentVisualsData = new AgentVisualsData();
				Hero heroObject = character.HeroObject;
				AgentVisualsData agentVisualsData2 = agentVisualsData.Banner((heroObject != null) ? heroObject.ClanBanner : null).Equipment(equipment).Race(character.Race);
				Hero heroObject2 = character.HeroObject;
				AgentVisualsData agentVisualsData3 = agentVisualsData2.BodyProperties((heroObject2 != null) ? heroObject2.BodyProperties : character.GetBodyProperties(equipment, seed)).Frame(gameEntity.GetGlobalFrame()).UseMorphAnims(true)
					.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_warrior"));
				ActionIndexCache actionIndexCache = ActionIndexCache.Create(defaultAnimForCharacter.ActionName);
				AgentVisuals agentVisuals = AgentVisuals.Create(agentVisualsData3.ActionCode(actionIndexCache).Scene(this._tableauScene).Monster(baseMonsterFromRace)
					.PrepareImmediately(true)
					.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
					.ClothColor1(deterministicColorsForCharacter.Item1)
					.ClothColor2(deterministicColorsForCharacter.Item2), "MapConversationTableau", true, false, false);
				agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, this._frame, true);
				Vec3 globalStableEyePoint = agentVisuals.GetVisuals().GetGlobalStableEyePoint(true);
				agentVisuals.SetLookDirection(this._cameraEntity.GetGlobalFrame().origin - globalStableEyePoint);
				string defaultFaceIdle = CharacterHelper.GetDefaultFaceIdle(character);
				agentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, defaultFaceIdle, false, true);
				this._agentVisuals.Add(agentVisuals);
				this._opponentLeaderEquipmentCache = ((equipment != null) ? equipment.CalculateEquipmentCode() : null);
			}
		}

		// Token: 0x0600025E RID: 606 RVA: 0x00016BA0 File Offset: 0x00014DA0
		private void SpawnOpponentBodyguardCharacter(CharacterObject character, int indexOfBodyguard, PartyBase party)
		{
			if (indexOfBodyguard >= 0 && indexOfBodyguard <= 1)
			{
				GameEntity gameEntity = this._tableauScene.FindEntitiesWithTag("player_bodyguard_infantry_spawn").ElementAt(indexOfBodyguard);
				MapConversationTableau.DefaultConversationAnimationData defaultAnimForCharacter = this.GetDefaultAnimForCharacter(character, true, party);
				int num = (indexOfBodyguard + 10) * 5;
				Equipment equipment;
				if (this._data.ConversationPartnerData.IsCivilianEquipmentRequiredForBodyGuardCharacters)
				{
					equipment = (this._data.ConversationPartnerData.Character.IsHero ? character.FirstCivilianEquipment : character.CivilianEquipments.ElementAt(num % character.CivilianEquipments.Count<Equipment>()));
				}
				else
				{
					equipment = (this._data.ConversationPartnerData.Character.IsHero ? character.FirstBattleEquipment : character.BattleEquipments.ElementAt(num % character.BattleEquipments.Count<Equipment>()));
				}
				int seed = -1;
				if (this._data.ConversationPartnerData.Party != null)
				{
					seed = CharacterHelper.GetPartyMemberFaceSeed(this._data.ConversationPartnerData.Party, this._data.ConversationPartnerData.Character, num);
				}
				Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
				AgentVisualsData agentVisualsData = new AgentVisualsData();
				PartyBase party2 = this._data.ConversationPartnerData.Party;
				Banner banner;
				if (party2 == null)
				{
					banner = null;
				}
				else
				{
					Hero leaderHero = party2.LeaderHero;
					banner = ((leaderHero != null) ? leaderHero.ClanBanner : null);
				}
				AgentVisualsData agentVisualsData2 = agentVisualsData.Banner(banner).Equipment(equipment).Race(character.Race)
					.BodyProperties(character.GetBodyProperties(equipment, seed))
					.Frame(gameEntity.GetGlobalFrame())
					.UseMorphAnims(true)
					.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_warrior"));
				ActionIndexCache actionIndexCache = ActionIndexCache.Create(defaultAnimForCharacter.ActionName);
				AgentVisualsData agentVisualsData3 = agentVisualsData2.ActionCode(actionIndexCache).Scene(this._tableauScene).Monster(baseMonsterFromRace)
					.PrepareImmediately(true)
					.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male);
				PartyBase party3 = this._data.ConversationPartnerData.Party;
				uint? num2;
				if (party3 == null)
				{
					num2 = null;
				}
				else
				{
					Hero leaderHero2 = party3.LeaderHero;
					num2 = ((leaderHero2 != null) ? new uint?(leaderHero2.MapFaction.Color) : null);
				}
				AgentVisualsData agentVisualsData4 = agentVisualsData3.ClothColor1(num2 ?? uint.MaxValue);
				PartyBase party4 = this._data.ConversationPartnerData.Party;
				uint? num3;
				if (party4 == null)
				{
					num3 = null;
				}
				else
				{
					Hero leaderHero3 = party4.LeaderHero;
					num3 = ((leaderHero3 != null) ? new uint?(leaderHero3.MapFaction.Color2) : null);
				}
				AgentVisuals agentVisuals = AgentVisuals.Create(agentVisualsData4.ClothColor2(num3 ?? uint.MaxValue), "MapConversationTableau", true, false, false);
				agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, this._frame, true);
				Vec3 globalStableEyePoint = agentVisuals.GetVisuals().GetGlobalStableEyePoint(true);
				agentVisuals.SetLookDirection(this._cameraEntity.GetGlobalFrame().origin - globalStableEyePoint);
				string defaultFaceIdle = CharacterHelper.GetDefaultFaceIdle(character);
				agentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, defaultFaceIdle, false, true);
				this._agentVisuals.Add(agentVisuals);
			}
		}

		// Token: 0x0600025F RID: 607 RVA: 0x00016EA8 File Offset: 0x000150A8
		internal void CharacterTableauContinuousRenderFunction(Texture sender, EventArgs e)
		{
			Scene scene = (Scene)sender.UserData;
			this.Texture = sender;
			TableauView tableauView = sender.TableauView;
			if (scene == null)
			{
				tableauView.SetContinuousRendering(false);
				tableauView.SetDeleteAfterRendering(true);
				return;
			}
			scene.EnsurePostfxSystem();
			scene.SetDofMode(true);
			scene.SetMotionBlurMode(false);
			scene.SetBloom(true);
			scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.31f);
			tableauView.SetRenderWithPostfx(true);
			uint num = uint.MaxValue;
			num &= 4294966271U;
			if (tableauView != null)
			{
				tableauView.SetPostfxConfigParams((int)num);
			}
			if (this._continuousRenderCamera != null)
			{
				float num2 = this._cameraRatio / 1.7777778f;
				this._continuousRenderCamera.SetFovHorizontal(num2 * this._baseCameraFOV, this._cameraRatio, 0.2f, 200f);
				tableauView.SetCamera(this._continuousRenderCamera);
				tableauView.SetScene(scene);
				tableauView.SetSceneUsesSkybox(true);
				tableauView.SetDeleteAfterRendering(false);
				tableauView.SetContinuousRendering(true);
				tableauView.SetClearColor(0U);
				tableauView.SetClearGbuffer(true);
				tableauView.DoNotClear(false);
				tableauView.SetFocusedShadowmap(true, ref this._frame.origin, 1.55f);
				scene.ForceLoadResources(true);
				bool flag = true;
				do
				{
					flag = true;
					foreach (AgentVisuals agentVisuals in this._agentVisuals)
					{
						flag = flag && agentVisuals.GetVisuals().CheckResources(true);
					}
				}
				while (!flag);
			}
		}

		// Token: 0x06000260 RID: 608 RVA: 0x00017028 File Offset: 0x00015228
		private MapConversationTableau.DefaultConversationAnimationData GetDefaultAnimForCharacter(CharacterObject character, bool preferLoopAnimationIfAvailable, PartyBase party)
		{
			MapConversationTableau.DefaultConversationAnimationData invalid = MapConversationTableau.DefaultConversationAnimationData.Invalid;
			CultureObject culture = character.Culture;
			if (culture != null && culture.IsBandit)
			{
				invalid.ActionName = "aggressive";
			}
			else
			{
				Hero heroObject = character.HeroObject;
				if (heroObject != null && heroObject.IsWounded)
				{
					PlayerEncounter playerEncounter = PlayerEncounter.Current;
					if (playerEncounter != null && playerEncounter.EncounterState == PlayerEncounterState.CaptureHeroes)
					{
						invalid.ActionName = "weary";
						goto IL_6E;
					}
				}
				invalid.ActionName = CharacterHelper.GetStandingBodyIdle(character, party);
			}
			IL_6E:
			ConversationAnimData conversationAnimData;
			if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(invalid.ActionName, out conversationAnimData))
			{
				bool flag = !string.IsNullOrEmpty(conversationAnimData.IdleAnimStart);
				bool flag2 = !string.IsNullOrEmpty(conversationAnimData.IdleAnimLoop);
				invalid.ActionName = (((preferLoopAnimationIfAvailable && flag2) || !flag) ? conversationAnimData.IdleAnimLoop : conversationAnimData.IdleAnimStart);
				invalid.AnimationData = conversationAnimData;
				invalid.AnimationDataValid = true;
			}
			else
			{
				invalid.ActionName = MapConversationTableau.fallbackAnimActName;
				if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(invalid.ActionName, out conversationAnimData))
				{
					invalid.AnimationData = conversationAnimData;
					invalid.AnimationDataValid = true;
				}
			}
			return invalid;
		}

		// Token: 0x06000261 RID: 609 RVA: 0x00017154 File Offset: 0x00015354
		public void OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
		{
			if (!this._initialized)
			{
				Debug.FailedAssert("Conversation Tableau shouldn't play before initialization", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapConversationTableau.cs", "OnConversationPlay", 604);
				return;
			}
			if (!Campaign.Current.ConversationManager.SpeakerAgent.Character.IsPlayerCharacter)
			{
				bool flag = false;
				bool flag2 = string.IsNullOrEmpty(idleActionId);
				ConversationAnimData animationData;
				if (flag2)
				{
					MapConversationTableau.DefaultConversationAnimationData defaultAnimForCharacter = this.GetDefaultAnimForCharacter(this._data.ConversationPartnerData.Character, false, this._data.ConversationPartnerData.Party);
					animationData = defaultAnimForCharacter.AnimationData;
					flag = defaultAnimForCharacter.AnimationDataValid;
				}
				else if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(idleActionId, out animationData))
				{
					flag = true;
				}
				if (flag)
				{
					if (!string.IsNullOrEmpty(reactionId))
					{
						AgentVisuals agentVisuals = this._agentVisuals[0];
						ActionIndexCache actionIndexCache = ActionIndexCache.Create(animationData.Reactions[reactionId]);
						agentVisuals.SetAction(actionIndexCache, 0f, false);
					}
					else if (!flag2 || this._changeIdleActionTimer.Check(Game.Current.ApplicationTime))
					{
						ActionIndexCache actionIndexCache2 = ActionIndexCache.Create(animationData.IdleAnimStart);
						if (!this._agentVisuals[0].DoesActionContinueWithCurrentAction(actionIndexCache2))
						{
							this._changeIdleActionTimer.Reset(Game.Current.ApplicationTime);
							this._agentVisuals[0].SetAction(actionIndexCache2, 0f, false);
						}
					}
				}
				if (!string.IsNullOrEmpty(reactionFaceAnimId))
				{
					this._agentVisuals[0].GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, reactionFaceAnimId, false, false);
				}
				else if (!string.IsNullOrEmpty(idleFaceAnimId))
				{
					this._agentVisuals[0].GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, idleFaceAnimId, false, true);
				}
			}
			this.RemovePreviousAgentsSoundEvent();
			this.StopConversationSoundEvent();
			if (!string.IsNullOrEmpty(soundPath))
			{
				this.PlayConversationSoundEvent(soundPath);
			}
		}

		// Token: 0x06000262 RID: 610 RVA: 0x00017317 File Offset: 0x00015517
		public void RemovePreviousAgentsSoundEvent()
		{
			if (this._conversationSoundEvent != null)
			{
				this._agentVisuals[0].StartRhubarbRecord("", -1);
			}
		}

		// Token: 0x06000263 RID: 611 RVA: 0x00017338 File Offset: 0x00015538
		private void PlayConversationSoundEvent(string soundPath)
		{
			Debug.Print("Conversation sound playing: " + soundPath, 5, Debug.DebugColor.White, 17592186044416UL);
			this._conversationSoundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", soundPath, this._tableauScene, true, false);
			this._conversationSoundEvent.Play();
			int soundId = this._conversationSoundEvent.GetSoundId();
			string rhubarbXmlPathFromSoundPath = this.GetRhubarbXmlPathFromSoundPath(soundPath);
			this._agentVisuals[0].StartRhubarbRecord(rhubarbXmlPathFromSoundPath, soundId);
		}

		// Token: 0x06000264 RID: 612 RVA: 0x000173AD File Offset: 0x000155AD
		public void StopConversationSoundEvent()
		{
			if (this._conversationSoundEvent != null)
			{
				this._conversationSoundEvent.Stop();
				this._conversationSoundEvent = null;
			}
		}

		// Token: 0x06000265 RID: 613 RVA: 0x000173CC File Offset: 0x000155CC
		private string GetRhubarbXmlPathFromSoundPath(string soundPath)
		{
			int length = soundPath.LastIndexOf('.');
			return soundPath.Substring(0, length) + ".xml";
		}

		// Token: 0x0400013F RID: 319
		private static int _tableauIndex;

		// Token: 0x04000140 RID: 320
		private const float MinimumTimeRequiredToChangeIdleAction = 8f;

		// Token: 0x04000142 RID: 322
		private Scene _tableauScene;

		// Token: 0x04000143 RID: 323
		private float _animationFrequencyThreshold = 2.5f;

		// Token: 0x04000144 RID: 324
		private MatrixFrame _frame;

		// Token: 0x04000145 RID: 325
		private GameEntity _cameraEntity;

		// Token: 0x04000146 RID: 326
		private SoundEvent _conversationSoundEvent;

		// Token: 0x04000147 RID: 327
		private Camera _continuousRenderCamera;

		// Token: 0x04000148 RID: 328
		private MapConversationTableauData _data;

		// Token: 0x04000149 RID: 329
		private float _cameraRatio;

		// Token: 0x0400014A RID: 330
		private IMapConversationDataProvider _dataProvider;

		// Token: 0x0400014B RID: 331
		private bool _initialized;

		// Token: 0x0400014C RID: 332
		private Timer _changeIdleActionTimer;

		// Token: 0x0400014D RID: 333
		private int _tableauSizeX;

		// Token: 0x0400014E RID: 334
		private int _tableauSizeY;

		// Token: 0x0400014F RID: 335
		private uint _clothColor1 = new Color(1f, 1f, 1f, 1f).ToUnsignedInteger();

		// Token: 0x04000150 RID: 336
		private uint _clothColor2 = new Color(1f, 1f, 1f, 1f).ToUnsignedInteger();

		// Token: 0x04000151 RID: 337
		private List<AgentVisuals> _agentVisuals;

		// Token: 0x04000152 RID: 338
		private static readonly string fallbackAnimActName = "act_inventory_idle_start";

		// Token: 0x04000153 RID: 339
		private readonly string RainingEntityTag = "raining_entity";

		// Token: 0x04000154 RID: 340
		private readonly string SnowingEntityTag = "snowing_entity";

		// Token: 0x04000155 RID: 341
		private float _animationGap;

		// Token: 0x04000156 RID: 342
		private bool _isEnabled = true;

		// Token: 0x04000157 RID: 343
		private float RenderScale = 1f;

		// Token: 0x04000158 RID: 344
		private const float _baseCameraRatio = 1.7777778f;

		// Token: 0x04000159 RID: 345
		private float _baseCameraFOV = -1f;

		// Token: 0x0400015A RID: 346
		private string _cachedAtmosphereName = "";

		// Token: 0x0400015B RID: 347
		private string _opponentLeaderEquipmentCache;

		// Token: 0x020000A2 RID: 162
		private struct DefaultConversationAnimationData
		{
			// Token: 0x04000345 RID: 837
			public static readonly MapConversationTableau.DefaultConversationAnimationData Invalid = new MapConversationTableau.DefaultConversationAnimationData
			{
				ActionName = "",
				AnimationDataValid = false
			};

			// Token: 0x04000346 RID: 838
			public ConversationAnimData AnimationData;

			// Token: 0x04000347 RID: 839
			public string ActionName;

			// Token: 0x04000348 RID: 840
			public bool AnimationDataValid;
		}
	}
}
