using System;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class ItemCatalogController : MissionLogic
{
	public delegate void BeforeCatalogTickDelegate(int currentItemIndex);

	private Campaign _campaign;

	private Game _game;

	private Agent _playerAgent;

	private int curItemIndex = 1;

	private Timer timer;

	public MBReadOnlyList<ItemObject> AllItems { get; private set; }

	public event BeforeCatalogTickDelegate BeforeCatalogTick;

	public event Action AfterCatalogTick;

	public ItemCatalogController()
	{
		_campaign = Campaign.Current;
		_game = Game.Current;
		timer = new Timer(base.Mission.CurrentTime, 1f);
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.Mission.SetMissionMode(MissionMode.Battle, atStart: true);
		AllItems = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>();
		if (!_campaign.IsSinglePlayerReferencesInitialized)
		{
			_campaign.InitializeSinglePlayerReferences();
		}
		CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
		MobileParty.MainParty.MemberRoster.AddToCounts(playerCharacter, 1);
		if (!base.Mission.Teams.IsEmpty())
		{
			throw new MBIllegalValueException("Number of teams is not 0.");
		}
		base.Mission.Teams.Add(BattleSideEnum.Defender, 4284776512u);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, 4281877080u);
		base.Mission.PlayerTeam = base.Mission.AttackerTeam;
		EquipmentElement value = playerCharacter.Equipment[0];
		EquipmentElement value2 = playerCharacter.Equipment[1];
		EquipmentElement value3 = playerCharacter.Equipment[2];
		EquipmentElement value4 = playerCharacter.Equipment[3];
		EquipmentElement value5 = playerCharacter.Equipment[4];
		playerCharacter.Equipment[0] = value;
		playerCharacter.Equipment[1] = value2;
		playerCharacter.Equipment[2] = value3;
		playerCharacter.Equipment[3] = value4;
		playerCharacter.Equipment[4] = value5;
		ItemObject item = AllItems[0];
		Equipment equipment = new Equipment();
		equipment.AddEquipmentToSlotWithoutAgent(GetEquipmentIndexOfItem(item), new EquipmentElement(AllItems[0]));
		AgentBuildData agentBuildData = new AgentBuildData(playerCharacter);
		agentBuildData.Equipment(equipment);
		_playerAgent = base.Mission.SpawnAgent(agentBuildData.Team(base.Mission.AttackerTeam).InitialPosition(new Vec3(15f, 12f, 1f)).InitialDirection(in Vec2.Forward)
			.Controller(AgentControllerType.Player));
		_playerAgent.WieldInitialWeapons();
		_playerAgent.Health = 10000f;
	}

	private EquipmentIndex GetEquipmentIndexOfItem(ItemObject item)
	{
		if (item.ItemFlags.HasAnyFlag(ItemFlags.DropOnWeaponChange | ItemFlags.DropOnAnyAction))
		{
			return EquipmentIndex.ExtraWeaponSlot;
		}
		switch (item.ItemType)
		{
		case ItemObject.ItemTypeEnum.Horse:
			return EquipmentIndex.ArmorItemEndSlot;
		case ItemObject.ItemTypeEnum.OneHandedWeapon:
		case ItemObject.ItemTypeEnum.TwoHandedWeapon:
		case ItemObject.ItemTypeEnum.Polearm:
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.SlingStones:
		case ItemObject.ItemTypeEnum.Shield:
		case ItemObject.ItemTypeEnum.Bow:
		case ItemObject.ItemTypeEnum.Crossbow:
		case ItemObject.ItemTypeEnum.Sling:
		case ItemObject.ItemTypeEnum.Thrown:
		case ItemObject.ItemTypeEnum.Pistol:
		case ItemObject.ItemTypeEnum.Musket:
		case ItemObject.ItemTypeEnum.Bullets:
			return EquipmentIndex.WeaponItemBeginSlot;
		case ItemObject.ItemTypeEnum.HeadArmor:
			return EquipmentIndex.NumAllWeaponSlots;
		case ItemObject.ItemTypeEnum.BodyArmor:
			return EquipmentIndex.Body;
		case ItemObject.ItemTypeEnum.LegArmor:
			return EquipmentIndex.Leg;
		case ItemObject.ItemTypeEnum.HandArmor:
			return EquipmentIndex.Gloves;
		case ItemObject.ItemTypeEnum.Animal:
			return EquipmentIndex.ArmorItemEndSlot;
		case ItemObject.ItemTypeEnum.HorseHarness:
			return EquipmentIndex.HorseHarness;
		case ItemObject.ItemTypeEnum.Cape:
			return EquipmentIndex.Cape;
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\ItemCatalogController.cs", "GetEquipmentIndexOfItem", 138);
			return EquipmentIndex.None;
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (timer.Check(base.Mission.CurrentTime))
		{
			if (!Directory.Exists("ItemCatalog"))
			{
				Directory.CreateDirectory("ItemCatalog");
			}
			this.BeforeCatalogTick?.Invoke(curItemIndex);
			timer.Reset(base.Mission.CurrentTime);
			MatrixFrame frame = new MatrixFrame
			{
				origin = new Vec3(10000f, 10000f, 10000f),
				rotation = Mat3.Identity
			};
			_playerAgent.AgentVisuals.SetFrame(ref frame);
			_playerAgent.TeleportToPosition(frame.origin);
			Blow b = new Blow(_playerAgent.Index);
			b.DamageType = DamageTypes.Blunt;
			b.BaseMagnitude = 1E+09f;
			b.GlobalPosition = _playerAgent.Position;
			_playerAgent.Die(b, Agent.KillInfo.Backstabbed);
			_playerAgent = null;
			for (int num = base.Mission.Agents.Count - 1; num >= 0; num--)
			{
				Agent agent = base.Mission.Agents[num];
				Blow blow = new Blow(agent.Index);
				blow.DamageType = DamageTypes.Blunt;
				blow.BaseMagnitude = 1E+09f;
				blow.GlobalPosition = agent.Position;
				Blow b2 = blow;
				agent.TeleportToPosition(frame.origin);
				agent.Die(b2, Agent.KillInfo.Backstabbed);
			}
			ItemObject item = AllItems[curItemIndex];
			Equipment equipment = new Equipment();
			equipment.AddEquipmentToSlotWithoutAgent(GetEquipmentIndexOfItem(item), new EquipmentElement(item));
			AgentBuildData agentBuildData = new AgentBuildData(_game.PlayerTroop);
			agentBuildData.Equipment(equipment);
			_playerAgent = base.Mission.SpawnAgent(agentBuildData.Team(base.Mission.AttackerTeam).InitialPosition(new Vec3(15f, 12f, 1f)).InitialDirection(in Vec2.Forward)
				.Controller(AgentControllerType.Player));
			_playerAgent.WieldInitialWeapons();
			_playerAgent.Health = 10000f;
			this.AfterCatalogTick?.Invoke();
			curItemIndex++;
		}
	}
}
