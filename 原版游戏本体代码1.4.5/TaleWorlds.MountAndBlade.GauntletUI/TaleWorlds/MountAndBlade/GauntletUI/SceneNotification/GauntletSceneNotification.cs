using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.SceneNotification;
using TaleWorlds.MountAndBlade.View.Scripts;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;

public class GauntletSceneNotification : GlobalLayer
{
	private class SceneNotificationQueueItem
	{
		public SceneNotificationData Data;

		public int FramesUntilDisplay;
	}

	private readonly GauntletLayer _gauntletLayer;

	private readonly Queue<SceneNotificationQueueItem> _notificationQueue;

	private readonly List<ISceneNotificationContextProvider> _contextProviders;

	private SceneNotificationVM _dataSource;

	private bool _isActive;

	private bool _isLastActiveGameStatePaused;

	private bool _isPendingSceneLoad;

	private Scene _scene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private List<PopupSceneSpawnPoint> _sceneCharacterScripts;

	private PopupSceneCameraPath _cameraPathScript;

	private Dictionary<string, GameEntity> _customPrefabBannerEntities;

	public static GauntletSceneNotification Current { get; private set; }

	public bool IsActive => _isActive;

	private GauntletSceneNotification()
	{
		_dataSource = new SceneNotificationVM(OnPositiveAction, CloseNotification, GetContinueKeyText);
		_notificationQueue = new Queue<SceneNotificationQueueItem>();
		_contextProviders = new List<ISceneNotificationContextProvider>();
		_gauntletLayer = new GauntletLayer("SceneNotification", 19600);
		_gauntletLayer.LoadMovie("SceneNotification", _dataSource);
		base.Layer = _gauntletLayer;
		MBInformationManager.OnShowSceneNotification += OnShowSceneNotification;
		MBInformationManager.OnHideSceneNotification += OnHideSceneNotification;
		MBInformationManager.IsAnySceneNotificationActive += IsAnySceneNotifiationActive;
		_gauntletLayer.GamepadNavigationContext.GainNavigationAfterFrames(2, null);
	}

	private bool IsAnySceneNotifiationActive()
	{
		return _isActive;
	}

	public static void Initialize()
	{
		if (Current == null)
		{
			Current = new GauntletSceneNotification();
			ScreenManager.AddGlobalLayer(Current, isFocusable: false);
			ScreenManager.SetSuspendLayer(Current.Layer, isSuspended: true);
		}
	}

	private void OnHideSceneNotification()
	{
		CloseNotification();
	}

	private void OnShowSceneNotification(SceneNotificationData campaignNotification)
	{
		_notificationQueue.Enqueue(new SceneNotificationQueueItem
		{
			Data = campaignNotification,
			FramesUntilDisplay = 2
		});
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_isActive && (MBGameManager.Current == null || Game.Current == null || MBGameManager.Current == null || MBGameManager.Current.IsEnding))
		{
			_notificationQueue.Clear();
			CloseNotification();
			return;
		}
		if (_dataSource != null)
		{
			_dataSource.EndProgress = _cameraPathScript?.GetCameraFade() ?? 0f;
			_cameraPathScript?.SetIsReady(_dataSource.IsReady);
			if (_dataSource.IsReady && _scene != null)
			{
				_scene.WaitWaterRendererCPUSimulation();
				_scene.Tick(dt);
			}
		}
		if (_isPendingSceneLoad)
		{
			if (_isActive)
			{
				OpenScene();
				base.Layer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(base.Layer);
				base.Layer.InputRestrictions.SetInputRestrictions();
			}
			else
			{
				Debug.FailedAssert("Scene load was pending but scene notification is not active", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\SceneNotification\\GauntletSceneNotification.cs", "OnTick", 121);
			}
			_isPendingSceneLoad = false;
		}
		QueueTick();
	}

	private void QueueTick()
	{
		if (_isActive || _notificationQueue.Count <= 0)
		{
			return;
		}
		SceneNotificationQueueItem sceneNotificationQueueItem = _notificationQueue.Peek();
		if (sceneNotificationQueueItem.FramesUntilDisplay > 0)
		{
			sceneNotificationQueueItem.FramesUntilDisplay--;
			return;
		}
		SceneNotificationData.RelevantContextType relevantContext = sceneNotificationQueueItem.Data.RelevantContext;
		if (IsGivenContextApplicableToCurrentContext(relevantContext))
		{
			SceneNotificationQueueItem sceneNotificationQueueItem2 = _notificationQueue.Dequeue();
			CreateSceneNotification(sceneNotificationQueueItem2.Data);
		}
	}

	private void OnPositiveAction()
	{
		_cameraPathScript?.SetPositiveState();
		foreach (PopupSceneSpawnPoint sceneCharacterScript in _sceneCharacterScripts)
		{
			sceneCharacterScript.SetPositiveState();
		}
	}

	private void OpenScene()
	{
		SceneNotificationData activeData = _dataSource.ActiveData;
		SceneNotificationData.SceneNotificationCharacter[] sceneNotificationCharacters = activeData.GetSceneNotificationCharacters();
		Banner[] banners = activeData.GetBanners();
		SceneNotificationData.SceneNotificationShip[] ships = activeData.GetShips();
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: true, DecalAtlasGroup.Battle);
		_scene.SetUsesDeleteLaterSystem(value: true);
		SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
		initData.InitPhysicsWorld = activeData.SceneProperties.InitializePhysics;
		if (initData.InitPhysicsWorld)
		{
			_scene.EnableInclusiveAsyncPhysx();
		}
		_scene.Read(activeData.SceneID, ref initData);
		if (initData.InitPhysicsWorld)
		{
			_scene.EnableFixedTick();
			_scene.SetFixedTickCallbackActive(isActive: true);
		}
		_scene.DisableStaticShadows(activeData.SceneProperties.DisableStaticShadows);
		if (activeData.SceneProperties.OverriddenWaterStrength.HasValue)
		{
			_scene.SetWaterStrength(activeData.SceneProperties.OverriddenWaterStrength.Value);
		}
		_scene.SetClothSimulationState(state: true);
		_scene.SetShadow(shadowEnabled: true);
		_scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_scene);
		_agentRendererSceneController.SetEnforcedVisibilityForAllAgents(_scene);
		_sceneCharacterScripts = new List<PopupSceneSpawnPoint>();
		_customPrefabBannerEntities = new Dictionary<string, GameEntity>();
		_cameraPathScript = _scene.GetFirstEntityWithScriptComponent<PopupSceneCameraPath>()?.GetFirstScriptOfType<PopupSceneCameraPath>();
		_cameraPathScript?.Initialize();
		_cameraPathScript?.SetInitialState();
		if (sceneNotificationCharacters != null)
		{
			int num = 1;
			for (int i = 0; i < sceneNotificationCharacters.Length; i++)
			{
				SceneNotificationData.SceneNotificationCharacter sceneNotificationCharacter = sceneNotificationCharacters[i];
				BasicCharacterObject character = sceneNotificationCharacter.Character;
				if (character == null)
				{
					num++;
					continue;
				}
				string tag = "spawnpoint_player_" + num;
				GameEntity gameEntity = _scene.FindEntitiesWithTag(tag).ToList().FirstOrDefault();
				if (gameEntity == null)
				{
					num++;
					continue;
				}
				PopupSceneSpawnPoint firstScriptOfType = gameEntity.GetFirstScriptOfType<PopupSceneSpawnPoint>();
				MatrixFrame frame = gameEntity.GetFrame();
				Equipment equipment = character.FirstBattleEquipment;
				if (sceneNotificationCharacter.OverriddenEquipment != null)
				{
					equipment = sceneNotificationCharacter.OverriddenEquipment;
				}
				else if (sceneNotificationCharacter.UseCivilianEquipment)
				{
					equipment = character.FirstCivilianEquipment;
				}
				BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment);
				if (sceneNotificationCharacter.OverriddenBodyProperties != default(BodyProperties))
				{
					bodyProperties = sceneNotificationCharacter.OverriddenBodyProperties;
				}
				uint clothColor = character.Culture.Color;
				uint clothColor2 = character.Culture.Color2;
				if (sceneNotificationCharacter.CustomColor1 != uint.MaxValue)
				{
					clothColor = sceneNotificationCharacter.CustomColor1;
				}
				if (sceneNotificationCharacter.CustomColor2 != uint.MaxValue)
				{
					clothColor2 = sceneNotificationCharacter.CustomColor2;
				}
				Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
				AgentVisuals agentVisuals = AgentVisuals.Create(new AgentVisualsData().UseMorphAnims(useMorphAnims: true).Equipment(equipment).Race(character.Race)
					.BodyProperties(bodyProperties)
					.SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
					.Frame(frame)
					.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, character.IsFemale, "_facegen"))
					.Scene(_scene)
					.Monster(baseMonsterFromRace)
					.PrepareImmediately(prepareImmediately: true)
					.UseTranslucency(useTranslucency: true)
					.UseTesselation(useTesselation: true)
					.ClothColor1(clothColor)
					.ClothColor2(clothColor2), "notification_agent_visuals_" + num, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
				AgentVisuals agentVisuals2 = null;
				if (sceneNotificationCharacter.UseHorse)
				{
					ItemObject item = equipment[EquipmentIndex.ArmorItemEndSlot].Item;
					string randomMountKeyString = MountCreationKey.GetRandomMountKeyString(item, character.GetMountKeySeed());
					MBActionSet actionSet = MBGlobals.GetActionSet(item.HorseComponent.Monster.ActionSetCode);
					agentVisuals2 = AgentVisuals.Create(new AgentVisualsData().Equipment(equipment).Frame(frame).ActionSet(actionSet)
						.Scene(_scene)
						.Monster(item.HorseComponent.Monster)
						.Scale(item.ScaleFactor)
						.PrepareImmediately(prepareImmediately: true)
						.UseTranslucency(useTranslucency: true)
						.UseTesselation(useTesselation: true)
						.MountCreationKey(randomMountKeyString), "notification_mount_visuals_" + num, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
				}
				firstScriptOfType.InitializeWithAgentVisuals(agentVisuals, agentVisuals2);
				agentVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
				agentVisuals2?.SetAgentLodZeroOrMaxExternal(makeZero: true);
				firstScriptOfType.SetInitialState();
				_sceneCharacterScripts.Add(firstScriptOfType);
				if (!string.IsNullOrEmpty(firstScriptOfType.BannerTagToUseForAddedPrefab) && firstScriptOfType.AddedPrefabComponent != null)
				{
					_customPrefabBannerEntities.Add(firstScriptOfType.BannerTagToUseForAddedPrefab, GameEntity.CreateFromWeakEntity(firstScriptOfType.AddedPrefabComponent.GetEntity()));
				}
				num++;
			}
		}
		if (banners != null)
		{
			for (int j = 0; j < banners.Length; j++)
			{
				Banner banner = banners[j];
				string text = "banner_" + (j + 1);
				GameEntity bannerEntity = _scene.FindEntityWithTag(text);
				GameEntity entity;
				if (bannerEntity != null)
				{
					((BannerVisual)banner.BannerVisual).GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), delegate(Texture t)
					{
						OnBannerTableauRenderDone(bannerEntity, t);
					});
				}
				else if (_customPrefabBannerEntities.TryGetValue(text, out entity))
				{
					((BannerVisual)banner.BannerVisual).GetTableauTextureLarge(BannerDebugInfo.CreateManual(GetType().Name), delegate(Texture t)
					{
						OnBannerTableauRenderDone(entity, t);
					});
				}
			}
		}
		if (ships != null)
		{
			int num2 = 1;
			for (int num3 = 0; num3 < ships.Length; num3++)
			{
				SceneNotificationData.SceneNotificationShip sceneNotificationShip = ships[num3];
				if (string.IsNullOrEmpty(sceneNotificationShip.ShipPrefabId))
				{
					num2++;
					Debug.FailedAssert("Scene notification ship does not have a valid prefab", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\SceneNotification\\GauntletSceneNotification.cs", "OpenScene", 366);
					continue;
				}
				string text2 = "spawnpoint_ship_" + num2;
				GameEntity gameEntity2 = _scene.FindEntityWithTag(text2);
				if (gameEntity2 == null)
				{
					Debug.FailedAssert("Ship spawn point entity with tag: " + text2 + " was not found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\SceneNotification\\GauntletSceneNotification.cs", "OpenScene", 374);
					num2++;
					continue;
				}
				List<GameEntity> list = gameEntity2.GetChildren().ToList();
				for (int num4 = 0; num4 < list.Count; num4++)
				{
					GameEntity entity2 = list[num4];
					_scene.RemoveEntity(entity2, 62);
				}
				gameEntity2.GetFirstScriptOfType<PopupSceneShipSpawnPoint>();
				GameEntity gameEntity3 = VisualShipFactory.CreateVisualShip(sceneNotificationShip.ShipPrefabId, _scene, sceneNotificationShip.ShipUpgrades, sceneNotificationShip.ShipSeed, sceneNotificationShip.ShipHitPointRatio, sceneNotificationShip.SailColor1, sceneNotificationShip.SailColor2, createPhysics: true);
				gameEntity2.AddChild(gameEntity3);
				num2++;
			}
		}
		_dataSource.Scene = _scene;
	}

	private void OnBannerTableauRenderDone(GameEntity bannerEntity, Texture bannerTexture)
	{
		if (!(bannerEntity != null))
		{
			return;
		}
		foreach (Mesh item in bannerEntity.GetAllMeshesWithTag("banner_replacement_mesh"))
		{
			ApplyBannerTextureToMesh(item, bannerTexture);
		}
		if (bannerEntity.Skeleton?.GetAllMeshes() == null)
		{
			return;
		}
		foreach (Mesh item2 in bannerEntity.Skeleton?.GetAllMeshes())
		{
			if (item2.HasTag("banner_replacement_mesh"))
			{
				ApplyBannerTextureToMesh(item2, bannerTexture);
			}
		}
	}

	private void ApplyBannerTextureToMesh(Mesh bannerMesh, Texture bannerTexture)
	{
		if (bannerMesh != null)
		{
			Material material = bannerMesh.GetMaterial().CreateCopy();
			material.SetTexture(Material.MBTextureType.DiffuseMap2, bannerTexture);
			uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
			ulong shaderFlags = material.GetShaderFlags();
			material.SetShaderFlags(shaderFlags | num);
			bannerMesh.SetMaterial(material);
		}
	}

	private void CreateSceneNotification(SceneNotificationData data)
	{
		if (_isActive)
		{
			Debug.FailedAssert("Trying to create scene notification while another notification is playing", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\SceneNotification\\GauntletSceneNotification.cs", "CreateSceneNotification", 441);
			return;
		}
		_isActive = true;
		_dataSource.CreateNotification(data);
		ScreenManager.SetSuspendLayer(base.Layer, isSuspended: false);
		_isLastActiveGameStatePaused = data.PauseActiveState;
		if (_isLastActiveGameStatePaused)
		{
			GameStateManager.Current.RegisterActiveStateDisableRequest(this);
			MBCommon.PauseGameEngine();
		}
		_dataSource.EndProgress = 0f;
		_isPendingSceneLoad = true;
	}

	private void CloseNotification()
	{
		if (!_isActive)
		{
			return;
		}
		_dataSource.ClearData();
		_isActive = false;
		base.Layer.InputRestrictions.ResetInputRestrictions();
		ScreenManager.SetSuspendLayer(base.Layer, isSuspended: true);
		base.Layer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(base.Layer);
		if (_isLastActiveGameStatePaused)
		{
			GameStateManager.Current?.UnregisterActiveStateDisableRequest(this);
			MBCommon.UnPauseGameEngine();
		}
		_cameraPathScript?.Destroy();
		if (_sceneCharacterScripts != null)
		{
			foreach (PopupSceneSpawnPoint sceneCharacterScript in _sceneCharacterScripts)
			{
				sceneCharacterScript.Destroy();
			}
			_sceneCharacterScripts = null;
		}
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _agentRendererSceneController, deleteThisFrame: false);
		_scene.ClearAll();
		_scene.ManualInvalidate();
		_scene = null;
	}

	private string GetContinueKeyText()
	{
		GameTextManager gameTextManager = Module.CurrentModule?.GlobalTextManager;
		if (gameTextManager != null)
		{
			if (Input.IsGamepadActive)
			{
				return gameTextManager.FindText("str_click_to_continue_console").SetTextVariable("CONSOLE_KEY_NAME", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ConversationHotKeyCategory", "ContinueClick"))).ToString();
			}
			return gameTextManager.FindText("str_click_to_continue").ToString();
		}
		return string.Empty;
	}

	public void OnFinalize()
	{
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	public void RegisterContextProvider(ISceneNotificationContextProvider provider)
	{
		_contextProviders.Add(provider);
	}

	public bool RemoveContextProvider(ISceneNotificationContextProvider provider)
	{
		return _contextProviders.Remove(provider);
	}

	private bool IsGivenContextApplicableToCurrentContext(SceneNotificationData.RelevantContextType givenContextType)
	{
		if (LoadingWindow.IsLoadingWindowActive)
		{
			return false;
		}
		if (givenContextType == SceneNotificationData.RelevantContextType.Any)
		{
			return true;
		}
		for (int i = 0; i < _contextProviders.Count; i++)
		{
			ISceneNotificationContextProvider sceneNotificationContextProvider = _contextProviders[i];
			if (sceneNotificationContextProvider != null && !sceneNotificationContextProvider.IsContextAllowed(givenContextType))
			{
				return false;
			}
		}
		return true;
	}
}
