using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattleObjects;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomGame : GameType
{
	private List<CustomBattleSceneData> _customBattleScenes;

	private const TerrainType DefaultTerrain = TerrainType.Plain;

	private const ForestDensity DefaultForestDensity = ForestDensity.None;

	public IEnumerable<CustomBattleSceneData> CustomBattleScenes => _customBattleScenes;

	public override bool IsCoreOnlyGameMode => true;

	public CustomBattleBannerEffects CustomBattleBannerEffects { get; private set; }

	public static CustomGame Current => Game.Current.GameType as CustomGame;

	public CustomGame()
	{
		_customBattleScenes = new List<CustomBattleSceneData>();
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
		basicGameStarter.AddModel(new CustomAgentApplyDamageModel());
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
	}

	private void InitializeScenes()
	{
		XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("CustomBattleScenes", skipValidation: true);
		LoadCustomBattleScenes(mergedXmlForManaged);
	}

	private void LoadCustomGameXmls()
	{
		CustomBattleBannerEffects = new CustomBattleBannerEffects();
		base.ObjectManager.LoadXML("Items");
		base.ObjectManager.LoadXML("EquipmentRosters");
		base.ObjectManager.LoadXML("NPCCharacters");
		base.ObjectManager.LoadXML("SPCultures");
	}

	protected override void BeforeRegisterTypes(MBObjectManager objectManager)
	{
	}

	protected override void OnRegisterTypes(MBObjectManager objectManager)
	{
		objectManager.RegisterType<BasicCharacterObject>("NPCCharacter", "NPCCharacters", 43u);
		objectManager.RegisterType<BasicCultureObject>("Culture", "SPCultures", 17u);
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
			string sceneID = null;
			TextObject name = null;
			TerrainType result = TerrainType.Plain;
			ForestDensity result2 = ForestDensity.None;
			bool result3 = false;
			bool result4 = false;
			bool result5 = false;
			bool result6 = false;
			bool result7 = false;
			string forcedSceneLevel = "";
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
				else if (childNode.Attributes[i].Name == "terrain")
				{
					if (!Enum.TryParse<TerrainType>(childNode.Attributes[i].InnerText, out result))
					{
						result = TerrainType.Plain;
					}
				}
				else if (childNode.Attributes[i].Name == "forest_density")
				{
					char[] array = childNode.Attributes[i].InnerText.ToLower().ToCharArray();
					array[0] = char.ToUpper(array[0]);
					if (!Enum.TryParse<ForestDensity>(new string(array), out result2))
					{
						result2 = ForestDensity.None;
					}
				}
				else if (childNode.Attributes[i].Name == "is_siege_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result3);
				}
				else if (childNode.Attributes[i].Name == "is_village_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result4);
				}
				else if (childNode.Attributes[i].Name == "is_lords_hall_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result5);
				}
				else if (childNode.Attributes[i].Name == "is_naval_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result6);
				}
				else if (childNode.Attributes[i].Name == "is_naval_raid_map")
				{
					bool.TryParse(childNode.Attributes[i].InnerText, out result7);
				}
				else if (childNode.Attributes[i].Name == "forced_scene_level")
				{
					forcedSceneLevel = childNode.Attributes[i].InnerText;
				}
			}
			if (result6 || result7)
			{
				continue;
			}
			XmlNodeList childNodes = childNode.ChildNodes;
			List<TerrainType> list = new List<TerrainType>();
			foreach (XmlNode item in childNodes)
			{
				if (item.NodeType == XmlNodeType.Comment || !(item.Name == "flags"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in item.ChildNodes)
				{
					if (childNode2.NodeType != XmlNodeType.Comment && childNode2.Attributes["name"].InnerText == "TerrainType" && Enum.TryParse<TerrainType>(childNode2.Attributes["value"].InnerText, out var result8) && !list.Contains(result8))
					{
						list.Add(result8);
					}
				}
			}
			_customBattleScenes.Add(new CustomBattleSceneData(sceneID, name, result, list, result2, result3, result4, result5, forcedSceneLevel));
		}
	}

	public override void OnStateChanged(GameState oldState)
	{
	}
}
