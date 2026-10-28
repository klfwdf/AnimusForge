using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerGame : GameType
{
	public override bool IsCoreOnlyGameMode => true;

	public static MultiplayerGame Current => Game.Current.GameType as MultiplayerGame;

	public override bool RequiresTutorial => false;

	protected override void OnInitialize()
	{
		Game currentGame = base.CurrentGame;
		IGameStarter gameStarter = new BasicGameStarter();
		AddGameModels(gameStarter);
		base.GameManager.InitializeGameStarter(currentGame, gameStarter);
		base.GameManager.OnGameStart(base.CurrentGame, gameStarter);
		currentGame.SetBasicModels(gameStarter.Models);
		currentGame.CreateGameManager();
		base.GameManager.BeginGameStart(base.CurrentGame);
		currentGame.InitializeDefaultGameObjects();
		if (!GameNetwork.IsDedicatedServer)
		{
			currentGame.GameTextManager.LoadGameTexts();
		}
		currentGame.LoadBasicFiles();
		base.ObjectManager.LoadXML("Items");
		base.ObjectManager.LoadXML("MPCharacters");
		base.ObjectManager.LoadXML("BasicCultures");
		base.ObjectManager.LoadXML("MPClassDivisions");
		base.ObjectManager.UnregisterNonReadyObjects();
		MultiplayerClassDivisions.Initialize();
		BadgeManager.InitializeWithXML(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/mpbadges.xml");
		base.GameManager.OnNewCampaignStart(base.CurrentGame, null);
		base.GameManager.OnAfterCampaignStart(base.CurrentGame);
		base.GameManager.OnGameInitializationFinished(base.CurrentGame);
		base.CurrentGame.AddGameHandler<ChatBox>();
		if (GameNetwork.IsDedicatedServer)
		{
			base.CurrentGame.AddGameHandler<MultiplayerGameLogger>();
		}
	}

	private void AddGameModels(IGameStarter basicGameStarter)
	{
		basicGameStarter.AddModel(new MultiplayerRidingModel());
		basicGameStarter.AddModel(new MultiplayerStrikeMagnitudeModel());
		basicGameStarter.AddModel(new MultiplayerAgentStatCalculateModel());
		basicGameStarter.AddModel(new MultiplayerAgentApplyDamageModel());
		basicGameStarter.AddModel(new MultiplayerBattleMoraleModel());
		basicGameStarter.AddModel(new MultiplayerBattleInitializationModel());
		basicGameStarter.AddModel(new MultiplayerBattleSpawnModel());
		basicGameStarter.AddModel(new MultiplayerBattleBannerBearersModel());
		basicGameStarter.AddModel(new DefaultFormationArrangementModel());
		basicGameStarter.AddModel(new DefaultAgentDecideKilledOrUnconsciousModel());
		basicGameStarter.AddModel(new DefaultDamageParticleModel());
		basicGameStarter.AddModel(new DefaultItemPickupModel());
		basicGameStarter.AddModel(new DefaultSiegeEngineCalculationModel());
	}

	public static Dictionary<string, Equipment> ReadDefaultEquipments(string defaultEquipmentsPath)
	{
		Dictionary<string, Equipment> dictionary = new Dictionary<string, Equipment>();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(defaultEquipmentsPath);
		foreach (XmlNode childNode in xmlDocument.ChildNodes[0].ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Element)
			{
				string value = childNode.Attributes["name"].Value;
				Equipment equipment = new Equipment(Equipment.EquipmentType.Battle);
				equipment.Deserialize(null, childNode);
				dictionary.Add(value, equipment);
			}
		}
		return dictionary;
	}

	protected override void BeforeRegisterTypes(MBObjectManager objectManager)
	{
	}

	protected override void OnRegisterTypes(MBObjectManager objectManager)
	{
		objectManager.RegisterType<BasicCharacterObject>("NPCCharacter", "MPCharacters", 43u);
		objectManager.RegisterType<BasicCultureObject>("Culture", "BasicCultures", 17u);
		objectManager.RegisterType<MultiplayerClassDivisions.MPHeroClass>("MPClassDivision", "MPClassDivisions", 45u);
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
		BadgeManager.OnFinalize();
		MultiplayerOptions.Release();
		InformationManager.ClearAllMessages();
		MultiplayerClassDivisions.Release();
		AvatarServices.ClearAvatarCaches();
	}

	public override void OnStateChanged(GameState oldState)
	{
	}
}
