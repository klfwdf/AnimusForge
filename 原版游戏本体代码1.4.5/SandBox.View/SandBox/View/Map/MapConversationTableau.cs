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

namespace SandBox.View.Map;

public class MapConversationTableau
{
	private struct DefaultConversationAnimationData
	{
		public static readonly DefaultConversationAnimationData Invalid = new DefaultConversationAnimationData
		{
			ActionName = "",
			AnimationDataValid = false
		};

		public ConversationAnimData AnimationData;

		public string ActionName;

		public bool AnimationDataValid;
	}

	private static int _tableauIndex;

	private const float MinimumTimeRequiredToChangeIdleAction = 8f;

	private Scene _tableauScene;

	private float _animationFrequencyThreshold = 2.5f;

	private MatrixFrame _frame;

	private GameEntity _cameraEntity;

	private SoundEvent _conversationSoundEvent;

	private Camera _continuousRenderCamera;

	private MapConversationTableauData _data;

	private float _cameraRatio;

	private IMapConversationDataProvider _dataProvider;

	private bool _initialized;

	private Timer _changeIdleActionTimer;

	private int _tableauSizeX;

	private int _tableauSizeY;

	private uint _clothColor1 = new Color(1f, 1f, 1f).ToUnsignedInteger();

	private uint _clothColor2 = new Color(1f, 1f, 1f).ToUnsignedInteger();

	private List<AgentVisuals> _agentVisuals;

	private static readonly string fallbackAnimActName = "act_inventory_idle_start";

	private readonly string RainingEntityTag = "raining_entity";

	private readonly string SnowingEntityTag = "snowing_entity";

	private float _animationGap;

	private bool _isEnabled = true;

	private float RenderScale = 1f;

	private const float _baseCameraRatio = 1.7777778f;

	private float _baseCameraFOV = -1f;

	private string _cachedAtmosphereName = "";

	private string _opponentLeaderEquipmentCache;

	public Texture Texture { get; private set; }

	private TableauView View => Texture?.TableauView;

	public MapConversationTableau()
	{
		_changeIdleActionTimer = new Timer(Game.Current.ApplicationTime, 8f);
		_agentVisuals = new List<AgentVisuals>();
		View?.SetEnable(_isEnabled);
		_dataProvider = SandBoxViewSubModule.MapConversationDataProvider;
	}

	public void SetEnabled(bool enabled)
	{
		if (_isEnabled != enabled)
		{
			if (enabled)
			{
				View?.SetEnable(value: false);
				View?.AddClearTask(clearOnlySceneview: true);
				Texture?.Release();
				Texture = TableauView.AddTableau($"MapConvTableau_{_tableauIndex++}", CharacterTableauContinuousRenderFunction, _tableauScene, _tableauSizeX, _tableauSizeY);
				Texture.TableauView.SetSceneUsesContour(value: false);
				Texture.TableauView.SetPointlightResolutionMultiplier(0f);
			}
			else
			{
				View?.SetEnable(value: false);
				View?.ClearAll(clearScene: false, removeTerrain: false);
				RemovePreviousAgentsSoundEvent();
				StopConversationSoundEvent();
				ThumbnailCacheManager.Current.ReturnCachedMapConversationTableauScene();
			}
			_isEnabled = enabled;
		}
	}

	public void SetData(object data)
	{
		if (_data == data)
		{
			return;
		}
		if (_data != null)
		{
			_initialized = false;
			foreach (AgentVisuals agentVisual in _agentVisuals)
			{
				agentVisual.Reset();
			}
			_agentVisuals.Clear();
			(MapScreen.Instance?.GetMapView<MapConversationView>()).ConversationMission.SetConversationTableau(null);
		}
		_data = data as MapConversationTableauData;
	}

	public void SetTargetSize(int width, int height)
	{
		int num = 0;
		int num2 = 0;
		if (width <= 0 || height <= 0)
		{
			num = 10;
			num2 = 10;
		}
		else
		{
			RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
			num = (int)((float)width * RenderScale);
			num2 = (int)((float)height * RenderScale);
		}
		if (num != _tableauSizeX || num2 != _tableauSizeY)
		{
			_tableauSizeX = num;
			_tableauSizeY = num2;
			_cameraRatio = (float)_tableauSizeX / (float)_tableauSizeY;
			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.Release();
			Texture = TableauView.AddTableau($"MapConvTableau_{_tableauIndex++}", CharacterTableauContinuousRenderFunction, _tableauScene, _tableauSizeX, _tableauSizeY);
		}
	}

	public void OnFinalize(bool clearNextFrame)
	{
		View?.SetEnable(value: false);
		RemovePreviousAgentsSoundEvent();
		StopConversationSoundEvent();
		_continuousRenderCamera?.ReleaseCameraEntity();
		_continuousRenderCamera = null;
		foreach (AgentVisuals agentVisual in _agentVisuals)
		{
			agentVisual.ResetNextFrame();
		}
		_agentVisuals = null;
		View.ClearAll(clearScene: false, removeTerrain: false);
		Texture.Release();
		Texture = null;
		IEnumerable<GameEntity> enumerable = _tableauScene.FindEntitiesWithTag(_cachedAtmosphereName);
		_cachedAtmosphereName = "";
		foreach (GameEntity item in enumerable)
		{
			item.SetVisibilityExcludeParents(visible: false);
		}
		ThumbnailCacheManager.Current.ReturnCachedMapConversationTableauScene();
		_tableauScene = null;
	}

	public void OnTick(float dt)
	{
		if (!_isEnabled)
		{
			return;
		}
		if (_data != null && !_initialized)
		{
			FirstTimeInit();
			(MapScreen.Instance?.GetMapView<MapConversationView>()).ConversationMission.SetConversationTableau(this);
		}
		if (_conversationSoundEvent != null && !_conversationSoundEvent.IsPlaying())
		{
			RemovePreviousAgentsSoundEvent();
			_conversationSoundEvent.Stop();
			_conversationSoundEvent = null;
		}
		if (_animationFrequencyThreshold > _animationGap)
		{
			_animationGap += dt;
		}
		TableauView view = View;
		if (view != null)
		{
			if (_continuousRenderCamera == null)
			{
				_continuousRenderCamera = Camera.CreateCamera();
			}
			view.SetDoNotRenderThisFrame(value: false);
		}
		if (_agentVisuals != null && _agentVisuals.Count > 0)
		{
			_agentVisuals[0].TickVisuals();
		}
		if (!(_agentVisuals[0].GetEquipment().CalculateEquipmentCode() != _opponentLeaderEquipmentCache))
		{
			return;
		}
		_initialized = false;
		foreach (AgentVisuals agentVisual in _agentVisuals)
		{
			agentVisual.Reset();
		}
		_agentVisuals.Clear();
	}

	private void FirstTimeInit()
	{
		if (_tableauScene == null)
		{
			_tableauScene = ThumbnailCacheManager.Current.GetCachedMapConversationTableauScene();
		}
		string atmosphereNameFromData = _dataProvider.GetAtmosphereNameFromData(_data);
		_tableauScene.SetAtmosphereWithName(atmosphereNameFromData);
		IEnumerable<GameEntity> enumerable = _tableauScene.FindEntitiesWithTag(atmosphereNameFromData);
		_cachedAtmosphereName = atmosphereNameFromData;
		foreach (GameEntity item in enumerable)
		{
			item.SetVisibilityExcludeParents(visible: true);
		}
		if (_continuousRenderCamera == null)
		{
			_continuousRenderCamera = Camera.CreateCamera();
			_cameraEntity = _tableauScene.FindEntityWithTag("player_infantry_to_infantry");
			Vec3 dofParams = default(Vec3);
			_cameraEntity.GetCameraParamsFromCameraScript(_continuousRenderCamera, ref dofParams);
			_baseCameraFOV = _continuousRenderCamera.HorizontalFov;
		}
		SpawnOpponentLeader();
		PartyBase party = _data.ConversationPartnerData.Party;
		if (party != null && party.MemberRoster?.TotalManCount > 1)
		{
			int num = TaleWorlds.Library.MathF.Min(2, _data.ConversationPartnerData.Party.MemberRoster.ToFlattenedRoster().Count() - 1);
			IOrderedEnumerable<TroopRosterElement> orderedEnumerable = from t in _data.ConversationPartnerData.Party.MemberRoster.GetTroopRoster()
				orderby t.Character.Level descending
				select t;
			foreach (TroopRosterElement item2 in orderedEnumerable)
			{
				CharacterObject character = item2.Character;
				if (character != _data.ConversationPartnerData.Character && !character.IsPlayerCharacter)
				{
					num--;
					SpawnOpponentBodyguardCharacter(character, num, _data.ConversationPartnerData.Party);
				}
				if (num == 0)
				{
					break;
				}
			}
			if (num == 1)
			{
				num--;
				TroopRosterElement troopRosterElement = orderedEnumerable.FirstOrDefault((TroopRosterElement troop) => !troop.Character.IsHero);
				if (troopRosterElement.Character != null)
				{
					SpawnOpponentBodyguardCharacter(troopRosterElement.Character, num, _data.ConversationPartnerData.Party);
				}
			}
		}
		_agentVisuals.ForEach(delegate(AgentVisuals a)
		{
			a.SetAgentLodZeroOrMaxExternal(makeZero: true);
		});
		_tableauScene.ForceLoadResources(checkInvisibleEntities: true);
		_cameraRatio = Screen.RealScreenResolutionWidth / Screen.RealScreenResolutionHeight;
		SetTargetSize((int)Screen.RealScreenResolutionWidth, (int)Screen.RealScreenResolutionHeight);
		uint num2 = uint.MaxValue;
		num2 &= 0xFFFFFBFFu;
		View?.SetPostfxConfigParams((int)num2);
		_tableauScene.FindEntityWithTag(RainingEntityTag).SetVisibilityExcludeParents(_data.IsRaining);
		_tableauScene.FindEntityWithTag(SnowingEntityTag).SetVisibilityExcludeParents(_data.IsSnowing);
		_tableauScene.Tick(3f);
		View?.SetEnable(value: true);
		_initialized = true;
	}

	private void SpawnOpponentLeader()
	{
		CharacterObject character = _data.ConversationPartnerData.Character;
		if (character == null)
		{
			return;
		}
		GameEntity gameEntity = _tableauScene.FindEntityWithTag("player_infantry_spawn");
		DefaultConversationAnimationData defaultAnimForCharacter = GetDefaultAnimForCharacter(character, preferLoopAnimationIfAvailable: false, _data.ConversationPartnerData.Party);
		_opponentLeaderEquipmentCache = null;
		Equipment equipment = null;
		equipment = ((!_data.ConversationPartnerData.IsCivilianEquipmentRequiredForLeader) ? (_data.ConversationPartnerData.Character.IsHero ? character.FirstBattleEquipment : character.BattleEquipments.ElementAt(_data.ConversationPartnerData.Character.GetDefaultFaceSeed(0) % character.BattleEquipments.Count())) : (_data.ConversationPartnerData.Character.IsHero ? character.FirstCivilianEquipment : character.CivilianEquipments.ElementAt(_data.ConversationPartnerData.Character.GetDefaultFaceSeed(0) % character.CivilianEquipments.Count())));
		equipment = equipment.Clone();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
		{
			if (!equipment[equipmentIndex].IsEmpty && equipment[equipmentIndex].Item.Type == ItemObject.ItemTypeEnum.Banner)
			{
				equipment[equipmentIndex] = EquipmentElement.Invalid;
				break;
			}
		}
		int seed = -1;
		if (_data.ConversationPartnerData.Party != null)
		{
			seed = CharacterHelper.GetPartyMemberFaceSeed(_data.ConversationPartnerData.Party, character, 0);
		}
		(uint, uint) deterministicColorsForCharacter = CharacterHelper.GetDeterministicColorsForCharacter(character, _data.ConversationPartnerData.Party);
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
		AgentVisuals agentVisuals = AgentVisuals.Create(new AgentVisualsData().Banner(character.HeroObject?.ClanBanner).Equipment(equipment).Race(character.Race)
			.BodyProperties(character.HeroObject?.BodyProperties ?? character.GetBodyProperties(equipment, seed))
			.Frame(gameEntity.GetGlobalFrame())
			.UseMorphAnims(useMorphAnims: true)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_warrior"))
			.ActionCode(ActionIndexCache.Create(defaultAnimForCharacter.ActionName))
			.Scene(_tableauScene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(prepareImmediately: true)
			.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.ClothColor1(deterministicColorsForCharacter.Item1)
			.ClothColor2(deterministicColorsForCharacter.Item2), "MapConversationTableau", isRandomProgress: true, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, _frame, tickAnimsForChildren: true);
		Vec3 globalStableEyePoint = agentVisuals.GetVisuals().GetGlobalStableEyePoint(isHumanoid: true);
		agentVisuals.SetLookDirection(_cameraEntity.GetGlobalFrame().origin - globalStableEyePoint);
		string defaultFaceIdle = CharacterHelper.GetDefaultFaceIdle(character);
		agentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, defaultFaceIdle, playSound: false, loop: true);
		_agentVisuals.Add(agentVisuals);
		_opponentLeaderEquipmentCache = equipment?.CalculateEquipmentCode();
	}

	private void SpawnOpponentBodyguardCharacter(CharacterObject character, int indexOfBodyguard, PartyBase party)
	{
		if (indexOfBodyguard >= 0 && indexOfBodyguard <= 1)
		{
			GameEntity gameEntity = _tableauScene.FindEntitiesWithTag("player_bodyguard_infantry_spawn").ElementAt(indexOfBodyguard);
			DefaultConversationAnimationData defaultAnimForCharacter = GetDefaultAnimForCharacter(character, preferLoopAnimationIfAvailable: true, party);
			int num = (indexOfBodyguard + 10) * 5;
			Equipment equipment = ((!_data.ConversationPartnerData.IsCivilianEquipmentRequiredForBodyGuardCharacters) ? (_data.ConversationPartnerData.Character.IsHero ? character.FirstBattleEquipment : character.BattleEquipments.ElementAt(num % character.BattleEquipments.Count())) : (_data.ConversationPartnerData.Character.IsHero ? character.FirstCivilianEquipment : character.CivilianEquipments.ElementAt(num % character.CivilianEquipments.Count())));
			int seed = -1;
			if (_data.ConversationPartnerData.Party != null)
			{
				seed = CharacterHelper.GetPartyMemberFaceSeed(_data.ConversationPartnerData.Party, _data.ConversationPartnerData.Character, num);
			}
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
			AgentVisuals agentVisuals = AgentVisuals.Create(new AgentVisualsData().Banner(_data.ConversationPartnerData.Party?.LeaderHero?.ClanBanner).Equipment(equipment).Race(character.Race)
				.BodyProperties(character.GetBodyProperties(equipment, seed))
				.Frame(gameEntity.GetGlobalFrame())
				.UseMorphAnims(useMorphAnims: true)
				.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_warrior"))
				.ActionCode(ActionIndexCache.Create(defaultAnimForCharacter.ActionName))
				.Scene(_tableauScene)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(prepareImmediately: true)
				.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
				.ClothColor1((uint)(((int?)_data.ConversationPartnerData.Party?.LeaderHero?.MapFaction.Color) ?? (-1)))
				.ClothColor2((uint)(((int?)_data.ConversationPartnerData.Party?.LeaderHero?.MapFaction.Color2) ?? (-1))), "MapConversationTableau", isRandomProgress: true, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, _frame, tickAnimsForChildren: true);
			Vec3 globalStableEyePoint = agentVisuals.GetVisuals().GetGlobalStableEyePoint(isHumanoid: true);
			agentVisuals.SetLookDirection(_cameraEntity.GetGlobalFrame().origin - globalStableEyePoint);
			string defaultFaceIdle = CharacterHelper.GetDefaultFaceIdle(character);
			agentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, defaultFaceIdle, playSound: false, loop: true);
			_agentVisuals.Add(agentVisuals);
		}
	}

	internal void CharacterTableauContinuousRenderFunction(Texture sender, EventArgs e)
	{
		Scene scene = (Scene)sender.UserData;
		Texture = sender;
		TableauView tableauView = sender.TableauView;
		if (scene == null)
		{
			tableauView.SetContinuousRendering(value: false);
			tableauView.SetDeleteAfterRendering(value: true);
			return;
		}
		scene.EnsurePostfxSystem();
		scene.SetDofMode(mode: true);
		scene.SetMotionBlurMode(mode: false);
		scene.SetBloom(mode: true);
		scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.31f);
		tableauView.SetRenderWithPostfx(value: true);
		uint num = uint.MaxValue;
		num &= 0xFFFFFBFFu;
		tableauView?.SetPostfxConfigParams((int)num);
		if (!(_continuousRenderCamera != null))
		{
			return;
		}
		float num2 = _cameraRatio / 1.7777778f;
		_continuousRenderCamera.SetFovHorizontal(num2 * _baseCameraFOV, _cameraRatio, 0.2f, 200f);
		tableauView.SetCamera(_continuousRenderCamera);
		tableauView.SetScene(scene);
		tableauView.SetSceneUsesSkybox(value: true);
		tableauView.SetDeleteAfterRendering(value: false);
		tableauView.SetContinuousRendering(value: true);
		tableauView.SetClearColor(0u);
		tableauView.SetClearGbuffer(value: true);
		tableauView.DoNotClear(value: false);
		tableauView.SetFocusedShadowmap(enable: true, ref _frame.origin, 1.55f);
		scene.ForceLoadResources(checkInvisibleEntities: true);
		bool flag = true;
		do
		{
			flag = true;
			foreach (AgentVisuals agentVisual in _agentVisuals)
			{
				flag = flag && agentVisual.GetVisuals().CheckResources(addToQueue: true);
			}
		}
		while (!flag);
	}

	private DefaultConversationAnimationData GetDefaultAnimForCharacter(CharacterObject character, bool preferLoopAnimationIfAvailable, PartyBase party)
	{
		DefaultConversationAnimationData invalid = DefaultConversationAnimationData.Invalid;
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
				PlayerEncounter current = PlayerEncounter.Current;
				if (current != null && current.EncounterState == PlayerEncounterState.CaptureHeroes)
				{
					invalid.ActionName = "weary";
					goto IL_006e;
				}
			}
			invalid.ActionName = CharacterHelper.GetStandingBodyIdle(character, party);
		}
		goto IL_006e;
		IL_006e:
		if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(invalid.ActionName, out var value))
		{
			bool flag = !string.IsNullOrEmpty(value.IdleAnimStart);
			bool flag2 = !string.IsNullOrEmpty(value.IdleAnimLoop);
			invalid.ActionName = (((preferLoopAnimationIfAvailable && flag2) || !flag) ? value.IdleAnimLoop : value.IdleAnimStart);
			invalid.AnimationData = value;
			invalid.AnimationDataValid = true;
		}
		else
		{
			invalid.ActionName = fallbackAnimActName;
			if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(invalid.ActionName, out value))
			{
				invalid.AnimationData = value;
				invalid.AnimationDataValid = true;
			}
		}
		return invalid;
	}

	public void OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
	{
		if (!_initialized)
		{
			Debug.FailedAssert("Conversation Tableau shouldn't play before initialization", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\MapConversationTableau.cs", "OnConversationPlay", 605);
			return;
		}
		if (!Campaign.Current.ConversationManager.SpeakerAgent.Character.IsPlayerCharacter)
		{
			bool flag = false;
			bool flag2 = string.IsNullOrEmpty(idleActionId);
			ConversationAnimData value;
			if (flag2)
			{
				DefaultConversationAnimationData defaultAnimForCharacter = GetDefaultAnimForCharacter(_data.ConversationPartnerData.Character, preferLoopAnimationIfAvailable: false, _data.ConversationPartnerData.Party);
				value = defaultAnimForCharacter.AnimationData;
				flag = defaultAnimForCharacter.AnimationDataValid;
			}
			else if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(idleActionId, out value))
			{
				flag = true;
			}
			if (flag)
			{
				if (!string.IsNullOrEmpty(reactionId))
				{
					_agentVisuals[0].SetAction(ActionIndexCache.Create(value.Reactions[reactionId]), 0f, forceFaceMorphRestart: false);
				}
				else if (!flag2 || _changeIdleActionTimer.Check(Game.Current.ApplicationTime))
				{
					ActionIndexCache actionIndex = ActionIndexCache.Create(value.IdleAnimStart);
					if (!_agentVisuals[0].DoesActionContinueWithCurrentAction(in actionIndex))
					{
						_changeIdleActionTimer.Reset(Game.Current.ApplicationTime);
						_agentVisuals[0].SetAction(in actionIndex, 0f, forceFaceMorphRestart: false);
					}
				}
			}
			if (!string.IsNullOrEmpty(reactionFaceAnimId))
			{
				_agentVisuals[0].GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, reactionFaceAnimId, playSound: false, loop: false);
			}
			else if (!string.IsNullOrEmpty(idleFaceAnimId))
			{
				_agentVisuals[0].GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, idleFaceAnimId, playSound: false, loop: true);
			}
		}
		RemovePreviousAgentsSoundEvent();
		StopConversationSoundEvent();
		if (!string.IsNullOrEmpty(soundPath))
		{
			PlayConversationSoundEvent(soundPath);
		}
	}

	public void RemovePreviousAgentsSoundEvent()
	{
		if (_conversationSoundEvent != null)
		{
			_agentVisuals[0].StartRhubarbRecord("", -1);
		}
	}

	private void PlayConversationSoundEvent(string soundPath)
	{
		Debug.Print("Conversation sound playing: " + soundPath, 5);
		_conversationSoundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", soundPath, _tableauScene, is3d: true, isBlocking: false);
		_conversationSoundEvent.Play();
		int soundId = _conversationSoundEvent.GetSoundId();
		string rhubarbXmlPathFromSoundPath = GetRhubarbXmlPathFromSoundPath(soundPath);
		_agentVisuals[0].StartRhubarbRecord(rhubarbXmlPathFromSoundPath, soundId);
	}

	public void StopConversationSoundEvent()
	{
		if (_conversationSoundEvent != null)
		{
			_conversationSoundEvent.Stop();
			_conversationSoundEvent = null;
		}
	}

	private string GetRhubarbXmlPathFromSoundPath(string soundPath)
	{
		return soundPath[..soundPath.LastIndexOf('.')] + ".xml";
	}
}
