using System;
using System.Collections.Generic;
using System.Xml;
using NavalDLC.ComponentInterfaces;
using NavalDLC.CustomBattle.CustomBattleObjects;
using NavalDLC.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CustomBattle;

public class NavalCustomGame : GameType
{
	private List<NavalCustomBattleSceneData> _customNavalBattleScenes;

	private List<NavalCustomBattleSceneData> _customNavalRaidScenes;

	private const TerrainType DefaultTerrain = TerrainType.OpenSea;

	public IEnumerable<NavalCustomBattleSceneData> CustomNavalBattleScenes => _customNavalBattleScenes;

	public IEnumerable<NavalCustomBattleSceneData> CustomNavalRaidScenes => _customNavalRaidScenes;

	public override string GameTypeStringId => "CustomGame";

	public override bool IsCoreOnlyGameMode => true;

	public NavalCustomBattleBannerEffects NavalCustomBattleBannerEffects { get; private set; }

	public static NavalCustomGame Current => Game.Current.GameType as NavalCustomGame;

	public NavalCustomGame()
	{
		_customNavalBattleScenes = new List<NavalCustomBattleSceneData>();
		_customNavalRaidScenes = new List<NavalCustomBattleSceneData>();
	}

	protected override void OnInitialize()
	{
		InitializeScenes();
		Game currentGame = base.CurrentGame;
		IGameStarter gameStarter = new BasicGameStarter();
		InitializeGameModels(gameStarter);
		base.GameManager.InitializeGameStarter(currentGame, gameStarter);
		base.GameManager.OnGameStart(base.CurrentGame, gameStarter);
		MBObjectManager objectManager = currentGame.ObjectManager;
		currentGame.SetBasicModels(gameStarter.Models);
		currentGame.CreateGameManager();
		base.GameManager.BeginGameStart(base.CurrentGame);
		currentGame.InitializeDefaultGameObjects();
		currentGame.LoadBasicFiles();
		LoadCustomGameXmls();
		objectManager.UnregisterNonReadyObjects();
		currentGame.SetDefaultEquipments(new Dictionary<string, Equipment>());
		objectManager.UnregisterNonReadyObjects();
		base.GameManager.OnNewCampaignStart(base.CurrentGame, null);
		base.GameManager.OnAfterCampaignStart(base.CurrentGame);
		base.GameManager.OnGameInitializationFinished(base.CurrentGame);
	}

	private void InitializeGameModels(IGameStarter basicGameStarter)
	{
		basicGameStarter.AddModel(new CustomBattleAgentStatCalculateModel());
		basicGameStarter.AddModel(new NavalCustomBattleAgentStatCalculateModel());
		basicGameStarter.AddModel(new NavalDLCCustomAgentApplyDamageModel());
		basicGameStarter.AddModel(new CustomBattleApplyWeatherEffectsModel());
		basicGameStarter.AddModel(new CustomBattleAutoBlockModel());
		basicGameStarter.AddModel(new CustomBattleMoraleModel());
		basicGameStarter.AddModel(new CustomBattleInitializationModel());
		basicGameStarter.AddModel(new CustomBattleSpawnModel());
		basicGameStarter.AddModel(new DefaultAgentDecideKilledOrUnconsciousModel());
		basicGameStarter.AddModel(new DefaultMissionDifficultyModel());
		basicGameStarter.AddModel(new DefaultRidingModel());
		basicGameStarter.AddModel(new DefaultStrikeMagnitudeModel());
		basicGameStarter.AddModel(new CustomBattleBannerBearersModel());
		basicGameStarter.AddModel(new DefaultFormationArrangementModel());
		basicGameStarter.AddModel(new DefaultDamageParticleModel());
		basicGameStarter.AddModel(new DefaultItemPickupModel());
		basicGameStarter.AddModel(new DefaultItemValueModel());
		basicGameStarter.AddModel(new DefaultSiegeEngineCalculationModel());
		basicGameStarter.AddModel(new NavalDLCCampaignShipParametersModel());
		basicGameStarter.AddModel(new NavalDLCShipPhysicsParametersModel());
		basicGameStarter.AddModel(new NavalDLCClanShipOwnershipModel());
		basicGameStarter.AddModel(new NavalDLCShipDistributionModel());
		basicGameStarter.AddModel(new NavalDLCShipDeploymentModel());
		basicGameStarter.AddModel(new NavalCustomBattleMissionShipParametersModel());
		basicGameStarter.AddModel(new NavalCustomBattleInitializationModel());
	}

	private void InitializeScenes()
	{
		XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("CustomBattleScenes", skipValidation: true);
		LoadCustomBattleScenes(mergedXmlForManaged);
	}

	private void LoadCustomGameXmls()
	{
		NavalCustomBattleBannerEffects = new NavalCustomBattleBannerEffects();
		base.ObjectManager.LoadXML("Items");
		base.ObjectManager.LoadXML("EquipmentRosters");
		base.ObjectManager.LoadXML("NPCCharacters");
		base.ObjectManager.LoadXML("SPCultures");
		base.ObjectManager.LoadXML("ShipUpgradePieces");
		base.ObjectManager.LoadXML("ShipSlots");
		base.ObjectManager.LoadXML("ShipHulls");
		base.ObjectManager.LoadXML("ShipPhysicsReferences");
		base.ObjectManager.LoadXML("MissionShips");
	}

	protected override void BeforeRegisterTypes(MBObjectManager objectManager)
	{
	}

	protected override void OnRegisterTypes(MBObjectManager objectManager)
	{
		objectManager.RegisterType<BasicCharacterObject>("NPCCharacter", "NPCCharacters", 43u);
		objectManager.RegisterType<BasicCultureObject>("Culture", "SPCultures", 17u);
		objectManager.RegisterType<ShipUpgradePiece>("ShipUpgradePiece", "ShipUpgradePieces", 60u);
		objectManager.RegisterType<ShipSlot>("ShipSlot", "ShipSlots", 59u);
		objectManager.RegisterType<ShipHull>("ShipHull", "ShipHulls", 58u);
		objectManager.RegisterType<ShipPhysicsReference>("ShipPhysicsReference", "ShipPhysicsReferences", 64u);
		objectManager.RegisterType<MissionShipObject>("MissionShip", "MissionShips", 57u);
	}

	protected override void DoLoadingForGameType(GameTypeLoadingStates gameTypeLoadingState, out GameTypeLoadingStates nextState)
	{
		nextState = GameTypeLoadingStates.None;
		switch (gameTypeLoadingState)
		{
		case GameTypeLoadingStates.InitializeFirstStep:
			base.CurrentGame.Initialize();
			nextState = GameTypeLoadingStates.WaitSecondStep;
			break;
		case GameTypeLoadingStates.WaitSecondStep:
			nextState = GameTypeLoadingStates.LoadVisualsThirdState;
			break;
		case GameTypeLoadingStates.LoadVisualsThirdState:
			nextState = GameTypeLoadingStates.PostInitializeFourthState;
			break;
		case GameTypeLoadingStates.PostInitializeFourthState:
			break;
		}
	}

	public override void OnDestroy()
	{
	}

	private void LoadCustomBattleScenes(XmlDocument doc)
	{
		if (doc.ChildNodes.Count == 0)
		{
			throw new TWXmlLoadException("Incorrect XML document format. XML document has no nodes.");
		}
		bool num = doc.ChildNodes[0].Name.ToLower().Equals("xml");
		if (num && doc.ChildNodes.Count == 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least one child node");
		}
		XmlNode xmlNode = (num ? doc.ChildNodes[1] : doc.ChildNodes[0]);
		if (xmlNode.Name != "CustomBattleScenes")
		{
			throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be CustomBattleScenes.");
		}
		if (!(xmlNode.Name == "CustomBattleScenes"))
		{
			return;
		}
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment)
			{
				continue;
			}
			bool result = false;
			bool result2 = false;
			string forcedSceneLevel = "";
			string sceneID = null;
			TextObject name = null;
			TerrainType result3 = TerrainType.OpenSea;
			for (int i = 0; i < childNode.Attributes.Count; i++)
			{
				if (childNode.Attributes[i].Name == "id")
				{
					sceneID = childNode.Attributes[i].InnerText;
				}
				else if (childNode.Attributes[i].Name == "name")
				{
					name = new TextObject(childNode.Attributes[i].InnerText);
				}
				else if (childNode.Attributes[i].Name == "is_naval_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result);
				}
				else if (childNode.Attributes[i].Name == "is_naval_raid_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result2);
				}
				else if (childNode.Attributes[i].Name == "terrain")
				{
					if (!Enum.TryParse<TerrainType>(childNode.Attributes[i].InnerText, out result3))
					{
						result3 = TerrainType.OpenSea;
					}
				}
				else if (childNode.Attributes[i].Name == "forced_scene_level")
				{
					forcedSceneLevel = childNode.Attributes[i].InnerText;
				}
			}
			if (result)
			{
				_customNavalBattleScenes.Add(new NavalCustomBattleSceneData(sceneID, name, result3, forcedSceneLevel));
			}
			else if (result2)
			{
				_customNavalRaidScenes.Add(new NavalCustomBattleSceneData(sceneID, name, result3, forcedSceneLevel));
			}
		}
	}

	public override void OnStateChanged(GameState oldState)
	{
	}
}
