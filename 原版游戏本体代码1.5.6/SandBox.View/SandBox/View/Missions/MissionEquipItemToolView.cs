using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionEquipItemToolView : MissionView
{
	private enum Filter
	{
		Head = 5,
		Cape = 9,
		Body = 6,
		Hand = 8,
		Leg = 7,
		Shield = 12,
		Bow = 13,
		Crossbow = 15,
		Horse = 10,
		Onehanded = 1,
		Twohanded = 2,
		Polearm = 3,
		Thrown = 4,
		Arrow = 14,
		Bolt = 16,
		Harness = 11
	}

	private delegate void MainThreadDelegate();

	private class ItemData
	{
		public GameEntity Entity;

		public string Name;

		public string Id;

		public BasicCultureObject Culture;

		public ItemObject.ItemTypeEnum itemType;

		public GenderEnum Gender;
	}

	public enum GenderEnum
	{
		Male = 1,
		Unisex,
		Female
	}

	private string str = "";

	private int _itemCulture;

	private bool[] _filters = new bool[17];

	private bool _genderSet;

	private Agent _mainAgent;

	private List<ItemObject> _allItemObjects = new List<ItemObject>();

	private List<ItemData> _allItems = new List<ItemData>();

	private List<ItemData> _currentItems = new List<ItemData>();

	private List<Tuple<int, int, int, int>> _currentArmorValues = new List<Tuple<int, int, int, int>>();

	private List<CultureObject> _allFactions = new List<CultureObject>();

	private List<CharacterObject> _allCharacters = new List<CharacterObject>();

	private List<FormationClass> _groups = new List<FormationClass>();

	private int _activeIndex = -1;

	private int _factionIndex;

	private int _groupIndex;

	private XmlDocument _charactersXml;

	private List<XmlDocument> _itemsXmls;

	private string[] _attributes = new string[6] { "id", "name", "level", "occupation", "culture", "group" };

	private string[] _spawnAttributes = new string[6] { "id", "name", "level", "occupation", "culture", "group" };

	private bool underscoreGuard;

	private bool yGuard;

	private bool zGuard;

	private bool xGuard;

	private bool _capsLock;

	private List<ItemObject> _activeItems = new List<ItemObject>();

	private int _setIndex;

	private int _spawnSetIndex;

	private Camera _cam;

	private bool _init = true;

	private int _index;

	private float _diff = 0.75f;

	private int _activeFilter;

	private int _activeWeaponSlot;

	private Vec3 _textStart;

	private List<BoundingBox> _bounds = new List<BoundingBox>();

	private float _pivotDiff;

	private Agent _mountAgent;

	private ItemObject _horse;

	private ItemObject _harness;

	public override void AfterStart()
	{
		_itemsXmls = new List<XmlDocument>();
		string text = ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/items/";
		FileInfo[] files = new DirectoryInfo(text).GetFiles("*.xml");
		foreach (FileInfo fileInfo in files)
		{
			_itemsXmls.Add(LoadXmlFile(text + fileInfo.Name));
		}
		_cam = Camera.CreateCamera();
		GetItems("Item");
		GetItems("CraftedItem");
		foreach (Kingdom item in Game.Current.ObjectManager.GetObjectTypeList<Kingdom>().ToList())
		{
			if (((IFaction)item).IsKingdomFaction || item.Name.ToString() == "Looters")
			{
				_allFactions.Add(item.Culture);
			}
		}
		foreach (Clan item2 in Game.Current.ObjectManager.GetObjectTypeList<Clan>().ToList())
		{
			if (item2.Name.ToString() == "Looters")
			{
				_allFactions.Add(item2.Culture);
			}
		}
		_groups.Add(FormationClass.Infantry);
		_groups.Add(FormationClass.Ranged);
		_groups.Add(FormationClass.Cavalry);
		_groups.Add(FormationClass.HorseArcher);
		_groups.Add(FormationClass.NumberOfDefaultFormations);
		_groups.Add(FormationClass.HeavyInfantry);
		_groups.Add(FormationClass.LightCavalry);
		_groups.Add(FormationClass.HeavyCavalry);
		_allCharacters = Game.Current.ObjectManager.GetObjectTypeList<CharacterObject>().ToList();
		SpawnAgent("guard");
		SpawnItems();
	}

	private void GetItems(string str)
	{
		List<ItemObject> list = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>().ToList();
		foreach (XmlDocument itemsXml in _itemsXmls)
		{
			foreach (XmlNode item in itemsXml.DocumentElement.SelectNodes(str))
			{
				if (item.Attributes == null || item.Attributes["id"] == null)
				{
					continue;
				}
				string innerText = item.Attributes["id"].InnerText;
				foreach (ItemObject item2 in list)
				{
					if (item2.StringId == innerText)
					{
						_allItemObjects.Add(item2);
					}
				}
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		OnEquipToolDebugTick(dt);
		if (_init)
		{
			_init = false;
			UpdateCamera();
		}
		if (_activeIndex == -1)
		{
			if (!base.DebugInput.IsKeyDown(InputKey.LeftControl) && !base.DebugInput.IsKeyDown(InputKey.RightControl) && !base.DebugInput.IsKeyDown(InputKey.LeftShift) && !base.DebugInput.IsKeyDown(InputKey.RightShift) && !base.DebugInput.IsKeyDown(InputKey.LeftAlt) && !base.DebugInput.IsKeyDown(InputKey.RightAlt))
			{
				if (base.DebugInput.IsKeyPressed(InputKey.D1))
				{
					_activeIndex = 0;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D2))
				{
					_activeIndex = 1;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D3))
				{
					_activeIndex = 2;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D4))
				{
					_activeIndex = 3;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D7))
				{
					_activeWeaponSlot = 0;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D8))
				{
					_activeWeaponSlot = 1;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D9))
				{
					_activeWeaponSlot = 2;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.D0))
				{
					_activeWeaponSlot = 3;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad2))
				{
					_factionIndex++;
					if (_factionIndex >= _allFactions.Count)
					{
						_factionIndex = 0;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad1))
				{
					_factionIndex--;
					if (_factionIndex < 0)
					{
						_factionIndex = _allFactions.Count - 1;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad5))
				{
					_groupIndex++;
					if (_groupIndex >= _groups.Count)
					{
						_groupIndex = 0;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad4))
				{
					_groupIndex--;
					if (_groupIndex < 0)
					{
						_groupIndex = _groups.Count - 1;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad8))
				{
					_setIndex++;
					if (_setIndex >= _mainAgent.Character.BattleEquipments.Count() + _mainAgent.Character.CivilianEquipments.Count() + 1)
					{
						_setIndex = 0;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad7))
				{
					_setIndex--;
					if (_setIndex < 0)
					{
						_setIndex = _mainAgent.Character.BattleEquipments.Count() + _mainAgent.Character.CivilianEquipments.Count() - 1;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollUp) && _index > 0)
				{
					foreach (ItemData currentItem in _currentItems)
					{
						MatrixFrame frame = currentItem.Entity.GetFrame();
						frame.origin += Vec3.Up * _diff;
						currentItem.Entity.SetFrame(ref frame);
					}
					_index--;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown) && _index < _currentItems.Count - 1)
				{
					foreach (ItemData currentItem2 in _currentItems)
					{
						MatrixFrame frame2 = currentItem2.Entity.GetFrame();
						frame2.origin -= Vec3.Up * _diff;
						currentItem2.Entity.SetFrame(ref frame2);
					}
					_index++;
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.F1))
				{
					if (!_genderSet)
					{
						_mainAgent.Character.IsFemale = false;
						_genderSet = true;
						SpawnAgent(_attributes[0]);
						SpawnItems();
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.F2) && !_genderSet)
				{
					_mainAgent.Character.IsFemale = true;
					_genderSet = true;
					SpawnAgent(_attributes[0]);
					SpawnItems();
				}
				if (base.DebugInput.IsKeyPressed(InputKey.F))
				{
					if (_currentItems.Count > 0)
					{
						foreach (ItemObject allItemObject in _allItemObjects)
						{
							if (!(allItemObject.StringId == _currentItems[_index].Id))
							{
								continue;
							}
							int num = 0;
							num = ((_activeFilter != 5 && _activeFilter != 9 && _activeFilter != 6 && _activeFilter != 7 && _activeFilter != 8 && _activeFilter != 10 && _activeFilter != 11) ? _activeWeaponSlot : _activeFilter);
							EquipmentIndex equipmentIndex = (EquipmentIndex)num;
							if ((equipmentIndex == EquipmentIndex.WeaponItemBeginSlot || equipmentIndex == EquipmentIndex.Weapon1 || equipmentIndex == EquipmentIndex.Weapon2 || equipmentIndex == EquipmentIndex.Weapon3 || equipmentIndex == EquipmentIndex.ExtraWeaponSlot) && !_mainAgent.Equipment[equipmentIndex].IsEmpty)
							{
								_mainAgent.DropItem(equipmentIndex);
								base.Mission.RemoveSpawnedItemsAndMissiles();
							}
							if (allItemObject.Type == ItemObject.ItemTypeEnum.Horse)
							{
								_horse = allItemObject;
							}
							if (allItemObject.Type == ItemObject.ItemTypeEnum.HorseHarness)
							{
								_harness = allItemObject;
							}
							if (equipmentIndex == EquipmentIndex.WeaponItemBeginSlot || equipmentIndex == EquipmentIndex.Weapon1 || equipmentIndex == EquipmentIndex.Weapon2 || equipmentIndex == EquipmentIndex.Weapon3 || equipmentIndex == EquipmentIndex.ExtraWeaponSlot)
							{
								MissionWeapon weapon = new MissionWeapon(allItemObject, null, _mainAgent.Origin?.Banner);
								_mainAgent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon);
							}
							Equipment equipment = _mainAgent.SpawnEquipment.Clone();
							equipment[equipmentIndex] = new EquipmentElement(allItemObject);
							BasicCharacterObject character = _mainAgent.Character;
							SpawnHorse(_horse, _harness);
							Mat3 rotation = _mainAgent.Frame.rotation;
							_mainAgent.FadeOut(hideInstantly: true, hideMount: false);
							_mainAgent = base.Mission.SpawnAgent(new AgentBuildData(new SimpleAgentOrigin(character)).Equipment(equipment).NoHorses(noHorses: true).Team(base.Mission.DefenderTeam)
								.InitialPosition(new Vec3(500f, 200f, 1f))
								.InitialDirection(rotation.f.AsVec2));
							foreach (Agent agent in base.Mission.Agents)
							{
								if (agent != _mainAgent)
								{
									_mountAgent = agent;
								}
							}
							UpdateCamera();
							UpdateActiveItems();
							break;
						}
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.BackSpace))
				{
					int num2 = _activeFilter;
					if (_activeFilter < 5 || _activeFilter > 11)
					{
						num2 = _activeWeaponSlot;
					}
					EquipmentIndex equipmentIndex2 = (EquipmentIndex)num2;
					if ((equipmentIndex2 == EquipmentIndex.WeaponItemBeginSlot || equipmentIndex2 == EquipmentIndex.Weapon1 || equipmentIndex2 == EquipmentIndex.Weapon2 || equipmentIndex2 == EquipmentIndex.Weapon3 || equipmentIndex2 == EquipmentIndex.ExtraWeaponSlot) && !_mainAgent.Equipment[equipmentIndex2].IsEmpty)
					{
						_mainAgent.DropItem(equipmentIndex2);
						base.Mission.RemoveSpawnedItemsAndMissiles();
					}
					else
					{
						switch (equipmentIndex2)
						{
						case EquipmentIndex.ArmorItemEndSlot:
							_mountAgent.FadeOut(hideInstantly: true, hideMount: false);
							_horse = null;
							SpawnHorse(_horse, _harness);
							break;
						case EquipmentIndex.HorseHarness:
							_mountAgent.FadeOut(hideInstantly: true, hideMount: false);
							_harness = null;
							SpawnHorse(_horse, _harness);
							break;
						default:
						{
							Equipment spawnEquipment = _mainAgent.SpawnEquipment;
							spawnEquipment[equipmentIndex2] = new EquipmentElement(null);
							BasicCharacterObject character2 = _mainAgent.Character;
							Mat3 rotation2 = _mainAgent.Frame.rotation;
							_mainAgent.FadeOut(hideInstantly: true, hideMount: false);
							_mainAgent = base.Mission.SpawnAgent(new AgentBuildData(new SimpleAgentOrigin(character2)).Equipment(spawnEquipment).NoHorses(noHorses: true).Team(base.Mission.DefenderTeam)
								.InitialPosition(new Vec3(500f, 200f, 1f))
								.InitialDirection(rotation2.f.AsVec2));
							break;
						}
						}
					}
					UpdateActiveItems();
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Q))
				{
					_activeFilter = 5;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.HeadArmor);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.W))
				{
					_activeFilter = 9;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Cape);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.E))
				{
					_activeFilter = 6;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.BodyArmor);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.R))
				{
					_activeFilter = 8;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.HandArmor);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.T))
				{
					_activeFilter = 7;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.LegArmor);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.X))
				{
					_activeFilter = 12;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Shield);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.B))
				{
					_activeFilter = 13;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Bow);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.C))
				{
					_activeFilter = 15;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Crossbow);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.G))
				{
					_activeFilter = 10;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Horse);
				}
			}
			else if (base.DebugInput.IsKeyDown(InputKey.LeftControl) || base.DebugInput.IsKeyDown(InputKey.RightControl))
			{
				if (base.DebugInput.IsKeyPressed(InputKey.S))
				{
					SaveToXml();
				}
				if (base.DebugInput.IsKeyPressed(InputKey.O))
				{
					CheckForLoad();
				}
				if (base.DebugInput.IsKeyPressed(InputKey.E))
				{
					EditNode();
				}
				if (base.DebugInput.IsKeyPressed(InputKey.R))
				{
					SpawnAgent(_attributes[0]);
				}
				if (base.DebugInput.IsKeyPressed(InputKey.Numpad1))
				{
					_itemCulture--;
					if (_itemCulture < -1)
					{
						_itemCulture = _allFactions.Count - 1;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.Numpad2))
				{
					_itemCulture++;
					if (_itemCulture >= _allFactions.Count)
					{
						_itemCulture = -1;
					}
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollUp) || base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown))
				{
					float num3 = 30f;
					bool flag = base.DebugInput.IsKeyDown(InputKey.LeftAlt);
					if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown))
					{
						num3 *= -1f;
					}
					foreach (ItemData currentItem3 in _currentItems)
					{
						MatrixFrame frame3 = currentItem3.Entity.GetFrame();
						ref Mat3 rotation3 = ref frame3.rotation;
						MatrixFrame frame4 = currentItem3.Entity.GetFrame();
						MatrixFrame frame5 = new MatrixFrame(in rotation3, in frame4.origin);
						if (!flag)
						{
							frame5.rotation.RotateAboutUp(System.MathF.PI / 180f * num3);
						}
						currentItem3.Entity.SetFrame(ref frame5);
					}
				}
			}
			else if (base.DebugInput.IsKeyDown(InputKey.LeftShift) || base.DebugInput.IsKeyDown(InputKey.RightShift))
			{
				if (base.DebugInput.IsKeyPressed(InputKey.Q))
				{
					_activeFilter = 1;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.OneHandedWeapon);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.W))
				{
					_activeFilter = 2;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.TwoHandedWeapon);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.E))
				{
					_activeFilter = 3;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Polearm);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.R))
				{
					_activeFilter = 4;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Thrown);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.B))
				{
					_activeFilter = 14;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Arrows);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.C))
				{
					_activeFilter = 16;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.Bolts);
				}
				else if (base.DebugInput.IsKeyPressed(InputKey.G))
				{
					_activeFilter = 11;
					Clear(_filters);
					_filters[_activeFilter] = true;
					SortFilter(ItemObject.ItemTypeEnum.HorseHarness);
				}
			}
		}
		else
		{
			InputKey inputKey = InputKey.D0;
			if (base.DebugInput.IsKeyDown(InputKey.Y) && !yGuard)
			{
				str += "y";
				yGuard = true;
			}
			if (base.DebugInput.IsKeyDown(InputKey.Z) && !zGuard)
			{
				str += "z";
				zGuard = true;
			}
			if (base.DebugInput.IsKeyDown(InputKey.X) && !xGuard)
			{
				str += "x";
				xGuard = true;
			}
			if (base.DebugInput.IsKeyPressed(InputKey.Enter))
			{
				_attributes[_activeIndex] = ((_activeIndex != 0) ? str : str.ToLower());
				_activeIndex = -1;
				str = "";
				underscoreGuard = false;
				yGuard = false;
				xGuard = false;
				zGuard = false;
			}
			if (base.DebugInput.IsKeyPressed(InputKey.Escape))
			{
				_activeIndex = -1;
				str = "";
				underscoreGuard = false;
				yGuard = false;
				xGuard = false;
				zGuard = false;
			}
			if (base.DebugInput.IsKeyPressed(InputKey.BackSpace) && str.Length > 0)
			{
				str = str.TrimEnd(new char[1] { str[str.Length - 1] });
				underscoreGuard = false;
				yGuard = false;
				xGuard = false;
				zGuard = false;
			}
			if (base.DebugInput.IsKeyPressed(InputKey.Space))
			{
				str += " ";
				underscoreGuard = false;
				yGuard = false;
				xGuard = false;
				zGuard = false;
			}
			if (base.DebugInput.IsKeyPressed(InputKey.CapsLock))
			{
				_capsLock = !_capsLock;
				underscoreGuard = false;
				yGuard = false;
				xGuard = false;
				zGuard = false;
			}
			if (base.DebugInput.IsKeyDown(InputKey.RightShift) && base.DebugInput.IsKeyDown(InputKey.Minus) && !underscoreGuard)
			{
				str += "_";
				underscoreGuard = true;
			}
			if (base.DebugInput.IsKeyDown(InputKey.LeftControl) && base.DebugInput.IsKeyPressed(InputKey.V))
			{
				string clipboardText = TaleWorlds.InputSystem.Input.GetClipboardText();
				str += clipboardText;
				underscoreGuard = false;
				yGuard = false;
				xGuard = false;
				zGuard = false;
				return;
			}
			for (int i = 0; i < 40; i++)
			{
				if (base.DebugInput.IsKeyPressed(inputKey + i))
				{
					string text = (_capsLock ? (inputKey + i).ToString().ToLower() : (inputKey + i).ToString());
					text = ((text.ToLower() == "d" + i) ? text.ToLower().Replace("d", "") : text);
					str += text;
					underscoreGuard = false;
					yGuard = false;
					xGuard = false;
					zGuard = false;
				}
			}
		}
		if (base.DebugInput.IsKeyDown(InputKey.LeftAlt) && !base.DebugInput.IsKeyDown(InputKey.LeftControl) && (base.DebugInput.IsKeyPressed(InputKey.MouseScrollUp) || base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown)))
		{
			float num4 = 60f;
			if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown))
			{
				num4 *= -1f;
			}
			MatrixFrame frame6 = _mainAgent.Frame;
			frame6.rotation.RotateAboutUp(System.MathF.PI / 180f * num4);
			_mainAgent.SetTargetPositionAndDirection(frame6.origin.AsVec2, in frame6.rotation.f);
		}
	}

	private void OnEquipToolDebugTick(float dt)
	{
		if (_genderSet)
		{
			_ = 10f + 70f + 15f + 120f;
			for (int i = 0; i < _currentItems.Count; i++)
			{
				_ = _index;
				ItemObject.ItemTypeEnum itemType = _currentItems[i].itemType;
			}
		}
	}

	private void SpawnItems()
	{
		float num = 0f - (float)_allItemObjects.Count * _diff / 2f;
		_allItems.Clear();
		foreach (ItemObject allItemObject in _allItemObjects)
		{
			MatrixFrame frame = _mainAgent.Frame;
			MatrixFrame frame2 = new MatrixFrame(in frame.rotation, _mainAgent.Position + new Vec3(-1f, 1f) + Vec3.Up * num);
			GameEntity gameEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
			MetaMesh copy = MetaMesh.GetCopy(allItemObject.MultiMeshName);
			gameEntity.AddMultiMesh(copy);
			gameEntity.SetFrame(ref frame2);
			gameEntity.SetVisibilityExcludeParents(visible: false);
			ItemData itemData = new ItemData();
			itemData.Entity = gameEntity;
			itemData.Name = allItemObject.Name.ToString();
			itemData.Id = allItemObject.StringId;
			itemData.Culture = allItemObject.Culture;
			if (allItemObject.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale))
			{
				itemData.Gender = GenderEnum.Male;
			}
			else if (allItemObject.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))
			{
				itemData.Gender = GenderEnum.Female;
			}
			else
			{
				itemData.Gender = GenderEnum.Unisex;
			}
			itemData.itemType = allItemObject.ItemType;
			_allItems.Add(itemData);
			num += _diff;
		}
	}

	private void SortFilter(ItemObject.ItemTypeEnum type)
	{
		_currentItems.Clear();
		_currentArmorValues.Clear();
		foreach (ItemData item in _allItems)
		{
			if (item.itemType == type && (_itemCulture == -1 || item.Culture == _allFactions[_itemCulture]))
			{
				int num = 0;
				bool flag = false;
				for (num = 0; num < _currentItems.Count; num++)
				{
					string text = _currentItems[num].Name.ToLower();
					for (int i = 0; i < item.Name.Length && i < text.Length; i++)
					{
						if (item.Name.ToLower()[i] < text[i])
						{
							flag = true;
							break;
						}
						if (item.Name.ToLower()[i] > text[i])
						{
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
				if (item.Gender == GenderEnum.Unisex || (_mainAgent.Character.IsFemale && item.Gender == GenderEnum.Female) || (!_mainAgent.Character.IsFemale && item.Gender == GenderEnum.Male))
				{
					_currentItems.Insert(num, item);
					ItemComponent itemComponent = _allItemObjects.Where((ItemObject itemObject) => itemObject.StringId == item.Id).FirstOrDefault().ItemComponent;
					ArmorComponent armorComponent = ((itemComponent != null && itemComponent is ArmorComponent) ? ((ArmorComponent)itemComponent) : null);
					int item2 = 0;
					int item3 = 0;
					int item4 = 0;
					int item5 = 0;
					if (armorComponent != null)
					{
						item2 = armorComponent.HeadArmor;
						item3 = armorComponent.BodyArmor;
						item4 = armorComponent.LegArmor;
						item5 = armorComponent.ArmArmor;
					}
					Tuple<int, int, int, int> item6 = new Tuple<int, int, int, int>(item2, item3, item4, item5);
					_currentArmorValues.Insert(num, item6);
				}
			}
			else
			{
				item.Entity.SetVisibilityExcludeParents(visible: false);
			}
		}
		PositionCurrentItems();
	}

	private void SpawnHorse(ItemObject horse, ItemObject harness)
	{
		ItemRosterElement itemRosterElement = default(ItemRosterElement);
		ItemRosterElement itemRosterElement2 = default(ItemRosterElement);
		if (horse != null)
		{
			itemRosterElement = new ItemRosterElement(horse, 1);
			itemRosterElement2 = ((harness == null) ? ItemRosterElement.Invalid : new ItemRosterElement(harness, 1));
		}
		else
		{
			if (harness == null)
			{
				return;
			}
			itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("mule"), 1);
			itemRosterElement2 = new ItemRosterElement(harness, 1);
		}
		if (_mountAgent != null)
		{
			_mountAgent.FadeOut(hideInstantly: true, hideMount: false);
		}
		_horse = itemRosterElement.EquipmentElement.Item;
		_harness = itemRosterElement2.EquipmentElement.Item;
		_mountAgent = base.Mission.SpawnMonster(itemRosterElement, itemRosterElement2, new Vec3(500f, _mainAgent.Position.y - 3f, 1f), in Vec2.Forward);
		_mountAgent.Controller = AgentControllerType.None;
	}

	private void SpawnAgent(string id)
	{
		_mountAgent?.FadeOut(hideInstantly: true, hideMount: false);
		_mainAgent?.FadeOut(hideInstantly: true, hideMount: false);
		CharacterObject characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>(id);
		List<Equipment> list = characterObject.BattleEquipments.ToList();
		list.AddRange(characterObject.CivilianEquipments.ToList());
		list.AddRange(characterObject.StealthEquipments.ToList());
		_mainAgent = base.Mission.SpawnAgent(new AgentBuildData(new SimpleAgentOrigin(characterObject)).Equipment(list[_setIndex]).NoHorses(noHorses: true).Team(base.Mission.DefenderTeam)
			.InitialPosition(new Vec3(500f, 200f, 1f))
			.InitialDirection(in Vec2.Forward));
		if (list[_setIndex].Horse.Item != null)
		{
			SpawnHorse(list[_setIndex].Horse.Item, list[_setIndex].GetEquipmentFromSlot(EquipmentIndex.HorseHarness).Item);
		}
		_groupIndex = characterObject.DefaultFormationGroup;
		_attributes[0] = characterObject.StringId;
		_attributes[1] = characterObject.Name.ToString();
		_attributes[2] = characterObject.Level.ToString();
		_attributes[3] = characterObject.Occupation.ToString();
		for (int i = 0; i < _attributes.Length; i++)
		{
			_spawnAttributes[i] = _attributes[i];
		}
		_groupIndex = characterObject.DefaultFormationGroup;
		for (int j = 0; j < _allFactions.Count; j++)
		{
			if (_allFactions[j].StringId == characterObject.Culture.StringId)
			{
				_factionIndex = j;
				_itemCulture = -1;
				break;
			}
		}
		_spawnSetIndex = _setIndex;
		UpdateActiveItems();
		UpdateCamera();
	}

	private void PositionCurrentItems()
	{
		float num = 0f;
		float z = 0f;
		if (_activeFilter == 6)
		{
			_diff = 1.5f;
		}
		else if (_activeFilter == 10 || _activeFilter == 11)
		{
			_diff = 4f;
			num = -1f;
		}
		else if (_activeFilter == 1 || _activeFilter == 2 || _activeFilter == 14 || _activeFilter == 16)
		{
			_diff = 1.5f;
		}
		else if (_activeFilter == 13 || _activeFilter == 15)
		{
			_diff = 2.5f;
			z = 1f;
		}
		else
		{
			_diff = 0.75f;
		}
		float num2 = 0f - (float)(_currentItems.Count / 2) * _diff;
		_textStart = _mainAgent.Position + new Vec3(-1f, 1f) + Vec3.Up * num2;
		foreach (ItemData currentItem in _currentItems)
		{
			MatrixFrame frame = new MatrixFrame(Mat3.Identity, _mainAgent.Position + new Vec3(-1f, 1f + num, z) + Vec3.Up * num2);
			currentItem.Entity.SetVisibilityExcludeParents(visible: true);
			currentItem.Entity.SetFrame(ref frame);
			num2 += _diff;
			if (currentItem.Entity.GetMetaMesh(0) != null)
			{
				BoundingBox boundingBox = currentItem.Entity.GetMetaMesh(0).GetBoundingBox();
				if (!_bounds.Contains(boundingBox))
				{
					_bounds.Add(boundingBox);
				}
			}
		}
		_index = _currentItems.Count / 2;
		_pivotDiff = 0f;
		foreach (BoundingBox bound in _bounds)
		{
			_pivotDiff += bound.center.z;
		}
		_pivotDiff /= _bounds.Count;
		_bounds.Clear();
	}

	private void EditNode()
	{
		_charactersXml = LoadXmlFile(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
		_charactersXml.DocumentElement.SelectNodes("NPCCharacter");
		bool flag = false;
		foreach (XmlNode item in _charactersXml.DocumentElement.SelectNodes("NPCCharacter"))
		{
			if (item.Attributes["id"] != null && _spawnAttributes[0] == item.Attributes["id"].InnerText)
			{
				item.Attributes["id"].InnerText = _attributes[0];
				item.Attributes["name"].InnerText = _attributes[1];
				if (item.Attributes["level"] != null)
				{
					item.Attributes["level"].InnerText = _attributes[2];
				}
				item.Attributes["occupation"].InnerText = _attributes[3];
				item.Attributes["culture"].InnerText = "Culture." + _allFactions[_factionIndex].StringId;
				item.Attributes["default_group"].InnerText = _groups[_groupIndex].GetName();
				SlotCheck("Head", 0, item);
				SlotCheck("Cape", 1, item);
				SlotCheck("Body", 2, item);
				SlotCheck("Gloves", 3, item);
				SlotCheck("Leg", 4, item);
				SlotCheck("Item0", 5, item);
				SlotCheck("Item1", 6, item);
				SlotCheck("Item2", 7, item);
				SlotCheck("Item3", 8, item);
				SlotCheck("Horse", -1, item, _horse);
				SlotCheck("HorseHarness", -1, item, _harness);
				_charactersXml.Save(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
				for (int i = 0; i < _attributes.Length; i++)
				{
					_spawnAttributes[i] = _attributes[i];
				}
				flag = true;
			}
		}
	}

	private void CheckForLoad()
	{
		if (_spawnAttributes[0] != _attributes[0] && Game.Current.ObjectManager.GetObject<CharacterObject>(_attributes[0]) != null)
		{
			SpawnAgent(_attributes[0]);
			return;
		}
		if (_spawnAttributes[1] != _attributes[1])
		{
			foreach (CharacterObject allCharacter in _allCharacters)
			{
				if (allCharacter.Name.ToString() == _attributes[1])
				{
					SpawnAgent(allCharacter.StringId);
					return;
				}
			}
		}
		if (_setIndex != _spawnSetIndex)
		{
			SpawnAgent(_mainAgent.Character.StringId);
		}
	}

	private void UpdateActiveItems()
	{
		_activeItems.Clear();
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.NumAllWeaponSlots].Item);
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.Cape].Item);
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.Body].Item);
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.Gloves].Item);
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.Leg].Item);
		MissionWeapon missionWeapon = _mainAgent.Equipment[EquipmentIndex.WeaponItemBeginSlot];
		if (missionWeapon.WeaponsCount > 0)
		{
			_activeItems.Add(missionWeapon.Item);
		}
		MissionWeapon missionWeapon2 = _mainAgent.Equipment[EquipmentIndex.Weapon1];
		if (missionWeapon2.WeaponsCount > 0)
		{
			_activeItems.Add(missionWeapon2.Item);
		}
		MissionWeapon missionWeapon3 = _mainAgent.Equipment[EquipmentIndex.Weapon2];
		if (missionWeapon3.WeaponsCount > 0)
		{
			_activeItems.Add(missionWeapon3.Item);
		}
		MissionWeapon missionWeapon4 = _mainAgent.Equipment[EquipmentIndex.Weapon3];
		if (missionWeapon4.WeaponsCount > 0)
		{
			_activeItems.Add(missionWeapon4.Item);
		}
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item);
		_activeItems.Add(_mainAgent.SpawnEquipment[EquipmentIndex.HorseHarness].Item);
	}

	private void SlotCheck(string slotName, int index, XmlNode parentNode, ItemObject obj = null)
	{
		XmlNodeList? xmlNodeList = parentNode.SelectNodes("equipmentSet")[_setIndex].SelectNodes("equipment");
		bool flag = false;
		foreach (XmlNode item in xmlNodeList)
		{
			if (item.Attributes != null && item.Attributes["slot"].InnerText == slotName)
			{
				if ((index != -1 && _activeItems[index] == null) || (index == -1 && obj == null))
				{
					item.ParentNode.RemoveChild(item);
					return;
				}
				item.Attributes["id"].Value = "Item." + ((obj == null) ? _activeItems[index].StringId : obj.StringId);
				flag = true;
				break;
			}
		}
		if (!flag && (index == -1 || _activeItems[index] != null) && (index != -1 || obj != null))
		{
			XmlElement xmlElement = _charactersXml.CreateElement("equipment");
			XmlAttribute xmlAttribute = _charactersXml.CreateAttribute("slot");
			xmlAttribute.Value = slotName;
			XmlAttribute xmlAttribute2 = _charactersXml.CreateAttribute("id");
			xmlAttribute2.Value = "Item." + ((obj == null) ? _activeItems[index].StringId : obj.StringId);
			xmlElement.Attributes.Append(xmlAttribute);
			xmlElement.Attributes.Append(xmlAttribute2);
			parentNode.SelectNodes("equipmentSet")[_setIndex].AppendChild(xmlElement);
		}
	}

	private void UpdateCamera()
	{
		Vec3 vec = ((_mainAgent.MountAgent == null) ? new Vec3(1.3f, 2f, 1f) : new Vec3(2f, 3f, 2f));
		MatrixFrame frame = default(MatrixFrame);
		frame.rotation.u = -(_mainAgent.Position - _cam.Position).NormalizedCopy();
		frame.rotation.f = Vec3.Up;
		frame.rotation.s = Vec3.CrossProduct(frame.rotation.f, frame.rotation.u);
		frame.rotation.s.Normalize();
		frame.rotation.Orthonormalize();
		float aspectRatio = Screen.AspectRatio;
		_cam.SetFovVertical(System.MathF.PI * 13f / 36f, aspectRatio, 1E-08f, 1000f);
		frame.origin = _mainAgent.Position + vec;
		_cam.Frame = frame;
		base.MissionScreen.CustomCamera = _cam;
	}

	private void SaveToXml()
	{
		_charactersXml = LoadXmlFile(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/spnpccharacters.xml");
		XmlNodeList xmlNodeList = _charactersXml.DocumentElement.SelectNodes("/NPCCharacters");
		bool flag = false;
		bool flag2 = false;
		string text = "\n  <equipmentSet>\n";
		foreach (XmlNode item in _charactersXml.DocumentElement.SelectNodes("NPCCharacter"))
		{
			if (item.Attributes["id"] != null && _attributes[0] == item.Attributes["id"].InnerText)
			{
				flag2 = true;
				if (_setIndex <= _spawnSetIndex)
				{
					return;
				}
				for (int i = 0; i < 9; i++)
				{
					if (_activeItems[i] != null)
					{
						string text2 = "";
						switch (i)
						{
						case 0:
							text2 = "Head";
							break;
						case 1:
							text2 = "Cape";
							break;
						case 2:
							text2 = "Body";
							break;
						case 3:
							text2 = "Gloves";
							break;
						case 4:
							text2 = "Leg";
							break;
						case 5:
							text2 = "Item0";
							break;
						case 6:
							text2 = "Item1";
							break;
						case 7:
							text2 = "Item2";
							break;
						case 8:
							text2 = "Item3";
							break;
						}
						text = text + "    <equipment slot=\"" + text2 + "\" id=\"Item." + _activeItems[i].StringId + "\" />\n";
					}
				}
				if (_horse != null)
				{
					text = text + "    <equipment slot=\"Horse\" id=\"Item." + _horse.StringId + "\" />\n";
				}
				if (_harness != null)
				{
					text = text + "    <equipment slot=\"HorseHarness\" id=\"Item." + _harness.StringId + "\" />\n";
				}
				text += "  </equipmentSet>\n";
				item.InnerXml += text;
				_charactersXml.Save(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
				Utilities.ConstructMainThreadJob(new MainThreadDelegate(Mission.Current.EndMission));
				MBGameManager.EndGame();
				for (int j = 0; j < 10; j++)
				{
				}
				break;
			}
			if (item.Attributes["id"] != null && _attributes[1] == item.Attributes["name"].InnerText)
			{
				flag = true;
			}
		}
		if (flag2)
		{
			return;
		}
		XmlElement xmlElement = _charactersXml.CreateElement("NPCCharacter");
		XmlAttribute xmlAttribute = _charactersXml.CreateAttribute("id");
		xmlAttribute.Value = _attributes[0];
		xmlElement.Attributes.Append(xmlAttribute);
		XmlAttribute xmlAttribute2 = _charactersXml.CreateAttribute("default_group");
		xmlAttribute2.Value = _groups[_groupIndex].GetName();
		xmlElement.Attributes.Append(xmlAttribute2);
		XmlAttribute xmlAttribute3 = _charactersXml.CreateAttribute("level");
		xmlAttribute3.Value = _attributes[2];
		xmlElement.Attributes.Append(xmlAttribute3);
		XmlAttribute xmlAttribute4 = _charactersXml.CreateAttribute("name");
		xmlAttribute4.Value = _attributes[1];
		xmlElement.Attributes.Append(xmlAttribute4);
		XmlAttribute xmlAttribute5 = _charactersXml.CreateAttribute("occupation");
		xmlAttribute5.Value = _attributes[3];
		xmlElement.Attributes.Append(xmlAttribute5);
		XmlAttribute xmlAttribute6 = _charactersXml.CreateAttribute("culture");
		xmlAttribute6.Value = "Culture." + _allFactions[_factionIndex].StringId;
		xmlElement.Attributes.Append(xmlAttribute6);
		XmlElement xmlElement2 = _charactersXml.CreateElement("face");
		XmlElement xmlElement3 = _charactersXml.CreateElement("face_key_template");
		XmlAttribute xmlAttribute7 = _charactersXml.CreateAttribute("value");
		xmlAttribute7.Value = "NPCCharacter.villager_vlandia";
		xmlElement3.Attributes.Append(xmlAttribute7);
		xmlElement2.AppendChild(xmlElement3);
		xmlElement.AppendChild(xmlElement2);
		XmlElement xmlElement4 = _charactersXml.CreateElement("equipmentSet");
		for (int k = 0; k < 9; k++)
		{
			if (_activeItems[k] != null)
			{
				XmlElement xmlElement5 = _charactersXml.CreateElement("equipment");
				XmlAttribute xmlAttribute8 = _charactersXml.CreateAttribute("slot");
				string value = "";
				switch (k)
				{
				case 0:
					value = "Head";
					break;
				case 1:
					value = "Cape";
					break;
				case 2:
					value = "Body";
					break;
				case 3:
					value = "Gloves";
					break;
				case 4:
					value = "Leg";
					break;
				case 5:
					value = "Item0";
					break;
				case 6:
					value = "Item1";
					break;
				case 7:
					value = "Item2";
					break;
				case 8:
					value = "Item3";
					break;
				}
				xmlAttribute8.Value = value;
				xmlElement5.Attributes.Append(xmlAttribute8);
				XmlAttribute xmlAttribute9 = _charactersXml.CreateAttribute("id");
				xmlAttribute9.Value = "Item." + _activeItems[k].StringId;
				xmlElement5.Attributes.Append(xmlAttribute9);
				xmlElement4.AppendChild(xmlElement5);
			}
		}
		xmlElement.AppendChild(xmlElement4);
		if (_horse != null)
		{
			XmlElement xmlElement6 = _charactersXml.CreateElement("equipment");
			XmlAttribute xmlAttribute10 = _charactersXml.CreateAttribute("slot");
			xmlAttribute10.Value = "Horse";
			xmlElement6.Attributes.Append(xmlAttribute10);
			XmlAttribute xmlAttribute11 = _charactersXml.CreateAttribute("id");
			xmlAttribute11.Value = "Item." + _horse.StringId;
			xmlElement6.Attributes.Append(xmlAttribute11);
			xmlElement.AppendChild(xmlElement6);
		}
		if (_harness != null)
		{
			XmlElement xmlElement7 = _charactersXml.CreateElement("equipment");
			XmlAttribute xmlAttribute12 = _charactersXml.CreateAttribute("slot");
			xmlAttribute12.Value = "HorseHarness";
			xmlElement7.Attributes.Append(xmlAttribute12);
			XmlAttribute xmlAttribute13 = _charactersXml.CreateAttribute("id");
			xmlAttribute13.Value = "Item." + _harness.StringId;
			xmlElement7.Attributes.Append(xmlAttribute13);
			xmlElement.AppendChild(xmlElement7);
		}
		xmlNodeList[xmlNodeList.Count - 1].AppendChild(xmlElement);
		_charactersXml.Save(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
		Utilities.ConstructMainThreadJob(new MainThreadDelegate(Mission.Current.EndMission));
		MBGameManager.EndGame();
		for (int l = 0; l < 10; l++)
		{
		}
	}

	private void Clear(bool[] array)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = false;
		}
	}

	private XmlDocument LoadXmlFile(string path)
	{
		Debug.Print("opening " + path);
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		return xmlDocument;
	}
}
