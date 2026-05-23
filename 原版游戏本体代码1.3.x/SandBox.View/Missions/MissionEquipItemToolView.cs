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

namespace SandBox.View.Missions
{
	// Token: 0x02000018 RID: 24
	public class MissionEquipItemToolView : MissionView
	{
		// Token: 0x0600009C RID: 156 RVA: 0x00005DC0 File Offset: 0x00003FC0
		public override void AfterStart()
		{
			this._itemsXmls = new List<XmlDocument>();
			string text = ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/items/";
			foreach (FileInfo fileInfo in new DirectoryInfo(text).GetFiles("*.xml"))
			{
				this._itemsXmls.Add(this.LoadXmlFile(text + fileInfo.Name));
			}
			this._cam = Camera.CreateCamera();
			this.GetItems("Item");
			this.GetItems("CraftedItem");
			foreach (Kingdom kingdom in Game.Current.ObjectManager.GetObjectTypeList<Kingdom>().ToList<Kingdom>())
			{
				if (((IFaction)kingdom).IsKingdomFaction || kingdom.Name.ToString() == "Looters")
				{
					this._allFactions.Add(kingdom.Culture);
				}
			}
			foreach (Clan clan in Game.Current.ObjectManager.GetObjectTypeList<Clan>().ToList<Clan>())
			{
				if (clan.Name.ToString() == "Looters")
				{
					this._allFactions.Add(clan.Culture);
				}
			}
			this._groups.Add(FormationClass.Infantry);
			this._groups.Add(FormationClass.Ranged);
			this._groups.Add(FormationClass.Cavalry);
			this._groups.Add(FormationClass.HorseArcher);
			this._groups.Add(FormationClass.NumberOfDefaultFormations);
			this._groups.Add(FormationClass.HeavyInfantry);
			this._groups.Add(FormationClass.LightCavalry);
			this._groups.Add(FormationClass.HeavyCavalry);
			this._allCharacters = Game.Current.ObjectManager.GetObjectTypeList<CharacterObject>().ToList<CharacterObject>();
			this.SpawnAgent("guard");
			this.SpawnItems();
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00005FD0 File Offset: 0x000041D0
		private void GetItems(string str)
		{
			List<ItemObject> list = Game.Current.ObjectManager.GetObjectTypeList<ItemObject>().ToList<ItemObject>();
			foreach (XmlDocument xmlDocument in this._itemsXmls)
			{
				foreach (object obj in xmlDocument.DocumentElement.SelectNodes(str))
				{
					XmlNode xmlNode = (XmlNode)obj;
					if (xmlNode.Attributes != null && xmlNode.Attributes["id"] != null)
					{
						string innerText = xmlNode.Attributes["id"].InnerText;
						foreach (ItemObject itemObject in list)
						{
							if (itemObject.StringId == innerText)
							{
								this._allItemObjects.Add(itemObject);
							}
						}
					}
				}
			}
		}

		// Token: 0x0600009E RID: 158 RVA: 0x0000610C File Offset: 0x0000430C
		public override void OnMissionTick(float dt)
		{
			this.OnEquipToolDebugTick(dt);
			if (this._init)
			{
				this._init = false;
				this.UpdateCamera();
			}
			if (this._activeIndex == -1)
			{
				if (!base.DebugInput.IsKeyDown(InputKey.LeftControl) && !base.DebugInput.IsKeyDown(InputKey.RightControl) && !base.DebugInput.IsKeyDown(InputKey.LeftShift) && !base.DebugInput.IsKeyDown(InputKey.RightShift) && !base.DebugInput.IsKeyDown(InputKey.LeftAlt) && !base.DebugInput.IsKeyDown(InputKey.RightAlt))
				{
					if (base.DebugInput.IsKeyPressed(InputKey.D1))
					{
						this._activeIndex = 0;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D2))
					{
						this._activeIndex = 1;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D3))
					{
						this._activeIndex = 2;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D4))
					{
						this._activeIndex = 3;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D7))
					{
						this._activeWeaponSlot = 0;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D8))
					{
						this._activeWeaponSlot = 1;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D9))
					{
						this._activeWeaponSlot = 2;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.D0))
					{
						this._activeWeaponSlot = 3;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Numpad2))
					{
						this._factionIndex++;
						if (this._factionIndex >= this._allFactions.Count)
						{
							this._factionIndex = 0;
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Numpad1))
					{
						this._factionIndex--;
						if (this._factionIndex < 0)
						{
							this._factionIndex = this._allFactions.Count - 1;
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Numpad5))
					{
						this._groupIndex++;
						if (this._groupIndex >= this._groups.Count)
						{
							this._groupIndex = 0;
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Numpad4))
					{
						this._groupIndex--;
						if (this._groupIndex < 0)
						{
							this._groupIndex = this._groups.Count - 1;
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Numpad8))
					{
						this._setIndex++;
						if (this._setIndex >= this._mainAgent.Character.BattleEquipments.Count<Equipment>() + this._mainAgent.Character.CivilianEquipments.Count<Equipment>() + 1)
						{
							this._setIndex = 0;
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Numpad7))
					{
						this._setIndex--;
						if (this._setIndex < 0)
						{
							this._setIndex = this._mainAgent.Character.BattleEquipments.Count<Equipment>() + this._mainAgent.Character.CivilianEquipments.Count<Equipment>() - 1;
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollUp) && this._index > 0)
					{
						foreach (MissionEquipItemToolView.ItemData itemData in this._currentItems)
						{
							MatrixFrame frame = itemData.Entity.GetFrame();
							frame.origin += Vec3.Up * this._diff;
							itemData.Entity.SetFrame(ref frame, true);
						}
						this._index--;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown) && this._index < this._currentItems.Count - 1)
					{
						foreach (MissionEquipItemToolView.ItemData itemData2 in this._currentItems)
						{
							MatrixFrame frame2 = itemData2.Entity.GetFrame();
							frame2.origin -= Vec3.Up * this._diff;
							itemData2.Entity.SetFrame(ref frame2, true);
						}
						this._index++;
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.F1))
					{
						if (!this._genderSet)
						{
							this._mainAgent.Character.IsFemale = false;
							this._genderSet = true;
							this.SpawnAgent(this._attributes[0]);
							this.SpawnItems();
						}
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.F2) && !this._genderSet)
					{
						this._mainAgent.Character.IsFemale = true;
						this._genderSet = true;
						this.SpawnAgent(this._attributes[0]);
						this.SpawnItems();
					}
					if (base.DebugInput.IsKeyPressed(InputKey.F))
					{
						if (this._currentItems.Count <= 0)
						{
							goto IL_1307;
						}
						using (List<ItemObject>.Enumerator enumerator2 = this._allItemObjects.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								ItemObject itemObject = enumerator2.Current;
								if (itemObject.StringId == this._currentItems[this._index].Id)
								{
									int num;
									if (this._activeFilter == 5 || this._activeFilter == 9 || this._activeFilter == 6 || this._activeFilter == 7 || this._activeFilter == 8 || this._activeFilter == 10 || this._activeFilter == 11)
									{
										num = this._activeFilter;
									}
									else
									{
										num = this._activeWeaponSlot;
									}
									EquipmentIndex equipmentIndex = (EquipmentIndex)num;
									if ((equipmentIndex == EquipmentIndex.WeaponItemBeginSlot || equipmentIndex == EquipmentIndex.Weapon1 || equipmentIndex == EquipmentIndex.Weapon2 || equipmentIndex == EquipmentIndex.Weapon3 || equipmentIndex == EquipmentIndex.ExtraWeaponSlot) && !this._mainAgent.Equipment[equipmentIndex].IsEmpty)
									{
										this._mainAgent.DropItem(equipmentIndex, WeaponClass.Undefined);
										base.Mission.RemoveSpawnedItemsAndMissiles();
									}
									if (itemObject.Type == ItemObject.ItemTypeEnum.Horse)
									{
										this._horse = itemObject;
									}
									if (itemObject.Type == ItemObject.ItemTypeEnum.HorseHarness)
									{
										this._harness = itemObject;
									}
									if (equipmentIndex == EquipmentIndex.WeaponItemBeginSlot || equipmentIndex == EquipmentIndex.Weapon1 || equipmentIndex == EquipmentIndex.Weapon2 || equipmentIndex == EquipmentIndex.Weapon3 || equipmentIndex == EquipmentIndex.ExtraWeaponSlot)
									{
										ItemObject item = itemObject;
										ItemModifier itemModifier = null;
										IAgentOriginBase origin = this._mainAgent.Origin;
										MissionWeapon missionWeapon = new MissionWeapon(item, itemModifier, (origin != null) ? origin.Banner : null);
										this._mainAgent.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon);
									}
									Equipment equipment = this._mainAgent.SpawnEquipment.Clone(false);
									equipment[equipmentIndex] = new EquipmentElement(itemObject, null, null, false);
									BasicCharacterObject character = this._mainAgent.Character;
									this.SpawnHorse(this._horse, this._harness);
									Mat3 rotation = this._mainAgent.Frame.rotation;
									this._mainAgent.FadeOut(true, false);
									Mission mission = base.Mission;
									AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))).Equipment(equipment).NoHorses(true).Team(base.Mission.DefenderTeam);
									Vec3 vec = new Vec3(500f, 200f, 1f, -1f);
									AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(vec);
									Vec2 asVec = rotation.f.AsVec2;
									this._mainAgent = mission.SpawnAgent(agentBuildData2.InitialDirection(asVec), false);
									foreach (Agent agent in base.Mission.Agents)
									{
										if (agent != this._mainAgent)
										{
											this._mountAgent = agent;
										}
									}
									this.UpdateCamera();
									this.UpdateActiveItems();
									break;
								}
							}
							goto IL_1307;
						}
					}
					if (base.DebugInput.IsKeyPressed(InputKey.BackSpace))
					{
						int num2 = this._activeFilter;
						if (this._activeFilter < 5 || this._activeFilter > 11)
						{
							num2 = this._activeWeaponSlot;
						}
						EquipmentIndex equipmentIndex2 = (EquipmentIndex)num2;
						if ((equipmentIndex2 == EquipmentIndex.WeaponItemBeginSlot || equipmentIndex2 == EquipmentIndex.Weapon1 || equipmentIndex2 == EquipmentIndex.Weapon2 || equipmentIndex2 == EquipmentIndex.Weapon3 || equipmentIndex2 == EquipmentIndex.ExtraWeaponSlot) && !this._mainAgent.Equipment[equipmentIndex2].IsEmpty)
						{
							this._mainAgent.DropItem(equipmentIndex2, WeaponClass.Undefined);
							base.Mission.RemoveSpawnedItemsAndMissiles();
						}
						else if (equipmentIndex2 == EquipmentIndex.ArmorItemEndSlot)
						{
							this._mountAgent.FadeOut(true, false);
							this._horse = null;
							this.SpawnHorse(this._horse, this._harness);
						}
						else if (equipmentIndex2 == EquipmentIndex.HorseHarness)
						{
							this._mountAgent.FadeOut(true, false);
							this._harness = null;
							this.SpawnHorse(this._horse, this._harness);
						}
						else
						{
							Equipment spawnEquipment = this._mainAgent.SpawnEquipment;
							spawnEquipment[equipmentIndex2] = new EquipmentElement(null, null, null, false);
							BasicCharacterObject character2 = this._mainAgent.Character;
							Mat3 rotation2 = this._mainAgent.Frame.rotation;
							this._mainAgent.FadeOut(true, false);
							Mission mission2 = base.Mission;
							AgentBuildData agentBuildData3 = new AgentBuildData(new SimpleAgentOrigin(character2, -1, null, default(UniqueTroopDescriptor))).Equipment(spawnEquipment).NoHorses(true).Team(base.Mission.DefenderTeam);
							Vec3 vec = new Vec3(500f, 200f, 1f, -1f);
							AgentBuildData agentBuildData4 = agentBuildData3.InitialPosition(vec);
							Vec2 asVec = rotation2.f.AsVec2;
							this._mainAgent = mission2.SpawnAgent(agentBuildData4.InitialDirection(asVec), false);
						}
						this.UpdateActiveItems();
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.Q))
					{
						this._activeFilter = 5;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.HeadArmor);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.W))
					{
						this._activeFilter = 9;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.Cape);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.E))
					{
						this._activeFilter = 6;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.BodyArmor);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.R))
					{
						this._activeFilter = 8;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.HandArmor);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.T))
					{
						this._activeFilter = 7;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.LegArmor);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.X))
					{
						this._activeFilter = 12;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.Shield);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.B))
					{
						this._activeFilter = 13;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.Bow);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.C))
					{
						this._activeFilter = 15;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.Crossbow);
					}
					else if (base.DebugInput.IsKeyPressed(InputKey.G))
					{
						this._activeFilter = 10;
						this.Clear(this._filters);
						this._filters[this._activeFilter] = true;
						this.SortFilter(ItemObject.ItemTypeEnum.Horse);
					}
				}
				else
				{
					if (base.DebugInput.IsKeyDown(InputKey.LeftControl) || base.DebugInput.IsKeyDown(InputKey.RightControl))
					{
						if (base.DebugInput.IsKeyPressed(InputKey.S))
						{
							this.SaveToXml();
						}
						if (base.DebugInput.IsKeyPressed(InputKey.O))
						{
							this.CheckForLoad();
						}
						if (base.DebugInput.IsKeyPressed(InputKey.E))
						{
							this.EditNode();
						}
						if (base.DebugInput.IsKeyPressed(InputKey.R))
						{
							this.SpawnAgent(this._attributes[0]);
						}
						if (base.DebugInput.IsKeyPressed(InputKey.Numpad1))
						{
							this._itemCulture--;
							if (this._itemCulture < -1)
							{
								this._itemCulture = this._allFactions.Count - 1;
								goto IL_1307;
							}
							goto IL_1307;
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.Numpad2))
						{
							this._itemCulture++;
							if (this._itemCulture >= this._allFactions.Count)
							{
								this._itemCulture = -1;
								goto IL_1307;
							}
							goto IL_1307;
						}
						else
						{
							if (!base.DebugInput.IsKeyPressed(InputKey.MouseScrollUp) && !base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown))
							{
								goto IL_1307;
							}
							float num3 = 30f;
							bool flag = base.DebugInput.IsKeyDown(InputKey.LeftAlt);
							if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown))
							{
								num3 *= -1f;
							}
							using (List<MissionEquipItemToolView.ItemData>.Enumerator enumerator = this._currentItems.GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									MissionEquipItemToolView.ItemData itemData3 = enumerator.Current;
									MatrixFrame matrixFrame = new MatrixFrame(ref itemData3.Entity.GetFrame().rotation, ref itemData3.Entity.GetFrame().origin);
									if (!flag)
									{
										matrixFrame.rotation.RotateAboutUp(0.017453292f * num3);
									}
									itemData3.Entity.SetFrame(ref matrixFrame, true);
								}
								goto IL_1307;
							}
						}
					}
					if (base.DebugInput.IsKeyDown(InputKey.LeftShift) || base.DebugInput.IsKeyDown(InputKey.RightShift))
					{
						if (base.DebugInput.IsKeyPressed(InputKey.Q))
						{
							this._activeFilter = 1;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.OneHandedWeapon);
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.W))
						{
							this._activeFilter = 2;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.TwoHandedWeapon);
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.E))
						{
							this._activeFilter = 3;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.Polearm);
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.R))
						{
							this._activeFilter = 4;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.Thrown);
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.B))
						{
							this._activeFilter = 14;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.Arrows);
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.C))
						{
							this._activeFilter = 16;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.Bolts);
						}
						else if (base.DebugInput.IsKeyPressed(InputKey.G))
						{
							this._activeFilter = 11;
							this.Clear(this._filters);
							this._filters[this._activeFilter] = true;
							this.SortFilter(ItemObject.ItemTypeEnum.HorseHarness);
						}
					}
				}
			}
			else
			{
				InputKey inputKey = InputKey.D0;
				if (base.DebugInput.IsKeyDown(InputKey.Y) && !this.yGuard)
				{
					this.str += "y";
					this.yGuard = true;
				}
				if (base.DebugInput.IsKeyDown(InputKey.Z) && !this.zGuard)
				{
					this.str += "z";
					this.zGuard = true;
				}
				if (base.DebugInput.IsKeyDown(InputKey.X) && !this.xGuard)
				{
					this.str += "x";
					this.xGuard = true;
				}
				if (base.DebugInput.IsKeyPressed(InputKey.Enter))
				{
					this._attributes[this._activeIndex] = ((this._activeIndex != 0) ? this.str : this.str.ToLower());
					this._activeIndex = -1;
					this.str = "";
					this.underscoreGuard = false;
					this.yGuard = false;
					this.xGuard = false;
					this.zGuard = false;
				}
				if (base.DebugInput.IsKeyPressed(InputKey.Escape))
				{
					this._activeIndex = -1;
					this.str = "";
					this.underscoreGuard = false;
					this.yGuard = false;
					this.xGuard = false;
					this.zGuard = false;
				}
				if (base.DebugInput.IsKeyPressed(InputKey.BackSpace) && this.str.Length > 0)
				{
					this.str = this.str.TrimEnd(new char[] { this.str[this.str.Length - 1] });
					this.underscoreGuard = false;
					this.yGuard = false;
					this.xGuard = false;
					this.zGuard = false;
				}
				if (base.DebugInput.IsKeyPressed(InputKey.Space))
				{
					this.str += " ";
					this.underscoreGuard = false;
					this.yGuard = false;
					this.xGuard = false;
					this.zGuard = false;
				}
				if (base.DebugInput.IsKeyPressed(InputKey.CapsLock))
				{
					this._capsLock = !this._capsLock;
					this.underscoreGuard = false;
					this.yGuard = false;
					this.xGuard = false;
					this.zGuard = false;
				}
				if (base.DebugInput.IsKeyDown(InputKey.RightShift) && base.DebugInput.IsKeyDown(InputKey.Minus) && !this.underscoreGuard)
				{
					this.str += "_";
					this.underscoreGuard = true;
				}
				if (base.DebugInput.IsKeyDown(InputKey.LeftControl) && base.DebugInput.IsKeyPressed(InputKey.V))
				{
					string clipboardText = TaleWorlds.InputSystem.Input.GetClipboardText();
					this.str += clipboardText;
					this.underscoreGuard = false;
					this.yGuard = false;
					this.xGuard = false;
					this.zGuard = false;
					return;
				}
				for (int i = 0; i < 40; i++)
				{
					if (base.DebugInput.IsKeyPressed(inputKey + i))
					{
						string text = (this._capsLock ? (inputKey + i).ToString().ToLower() : (inputKey + i).ToString());
						text = ((text.ToLower() == "d" + i.ToString()) ? text.ToLower().Replace("d", "") : text);
						this.str += text;
						this.underscoreGuard = false;
						this.yGuard = false;
						this.xGuard = false;
						this.zGuard = false;
					}
				}
			}
			IL_1307:
			if (base.DebugInput.IsKeyDown(InputKey.LeftAlt) && !base.DebugInput.IsKeyDown(InputKey.LeftControl) && (base.DebugInput.IsKeyPressed(InputKey.MouseScrollUp) || base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown)))
			{
				float num4 = 60f;
				if (base.DebugInput.IsKeyPressed(InputKey.MouseScrollDown))
				{
					num4 *= -1f;
				}
				MatrixFrame frame3 = this._mainAgent.Frame;
				frame3.rotation.RotateAboutUp(0.017453292f * num4);
				Agent mainAgent = this._mainAgent;
				Vec2 asVec = frame3.origin.AsVec2;
				mainAgent.SetTargetPositionAndDirection(asVec, frame3.rotation.f);
			}
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00007554 File Offset: 0x00005754
		private void OnEquipToolDebugTick(float dt)
		{
			if (this._genderSet)
			{
				float num = 10f + 70f + 15f + 120f;
				for (int i = 0; i < this._currentItems.Count; i++)
				{
					int index = this._index;
					ItemObject.ItemTypeEnum itemType = this._currentItems[i].itemType;
					if (itemType != ItemObject.ItemTypeEnum.BodyArmor && itemType != ItemObject.ItemTypeEnum.HeadArmor && itemType != ItemObject.ItemTypeEnum.LegArmor && itemType != ItemObject.ItemTypeEnum.HandArmor && itemType != ItemObject.ItemTypeEnum.ChestArmor)
					{
						bool flag = itemType == ItemObject.ItemTypeEnum.Cape;
					}
				}
			}
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x000075D4 File Offset: 0x000057D4
		private void SpawnItems()
		{
			float num = -((float)this._allItemObjects.Count * this._diff / 2f);
			this._allItems.Clear();
			foreach (ItemObject itemObject in this._allItemObjects)
			{
				ref MatrixFrame ptr = ref this._mainAgent.Frame;
				Vec3 vec = this._mainAgent.Position + new Vec3(-1f, 1f, 0f, -1f) + Vec3.Up * num;
				MatrixFrame matrixFrame = new MatrixFrame(ref ptr.rotation, ref vec);
				GameEntity gameEntity = GameEntity.CreateEmpty(Mission.Current.Scene, true, true, true);
				MetaMesh copy = MetaMesh.GetCopy(itemObject.MultiMeshName, true, false);
				gameEntity.AddMultiMesh(copy, true);
				gameEntity.SetFrame(ref matrixFrame, true);
				gameEntity.SetVisibilityExcludeParents(false);
				MissionEquipItemToolView.ItemData itemData = new MissionEquipItemToolView.ItemData();
				itemData.Entity = gameEntity;
				itemData.Name = itemObject.Name.ToString();
				itemData.Id = itemObject.StringId;
				itemData.Culture = itemObject.Culture;
				if (itemObject.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale))
				{
					itemData.Gender = MissionEquipItemToolView.GenderEnum.Male;
				}
				else if (itemObject.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))
				{
					itemData.Gender = MissionEquipItemToolView.GenderEnum.Female;
				}
				else
				{
					itemData.Gender = MissionEquipItemToolView.GenderEnum.Unisex;
				}
				itemData.itemType = itemObject.ItemType;
				this._allItems.Add(itemData);
				num += this._diff;
			}
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00007790 File Offset: 0x00005990
		private void SortFilter(ItemObject.ItemTypeEnum type)
		{
			this._currentItems.Clear();
			this._currentArmorValues.Clear();
			using (List<MissionEquipItemToolView.ItemData>.Enumerator enumerator = this._allItems.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					MissionEquipItemToolView.ItemData item = enumerator.Current;
					if (item.itemType == type && (this._itemCulture == -1 || item.Culture == this._allFactions[this._itemCulture]))
					{
						bool flag = false;
						int j;
						for (j = 0; j < this._currentItems.Count; j++)
						{
							string text = this._currentItems[j].Name.ToLower();
							int num = 0;
							while (num < item.Name.Length && num < text.Length)
							{
								if (item.Name.ToLower()[num] < text[num])
								{
									flag = true;
									break;
								}
								if (item.Name.ToLower()[num] > text[num])
								{
									break;
								}
								num++;
							}
							if (flag)
							{
								break;
							}
						}
						if (item.Gender == MissionEquipItemToolView.GenderEnum.Unisex || (this._mainAgent.Character.IsFemale && item.Gender == MissionEquipItemToolView.GenderEnum.Female) || (!this._mainAgent.Character.IsFemale && item.Gender == MissionEquipItemToolView.GenderEnum.Male))
						{
							this._currentItems.Insert(j, item);
							ItemComponent itemComponent = (from i in this._allItemObjects
								where i.StringId == item.Id
								select i).FirstOrDefault<ItemObject>().ItemComponent;
							ArmorComponent armorComponent = ((itemComponent != null && itemComponent is ArmorComponent) ? ((ArmorComponent)itemComponent) : null);
							int item6 = 0;
							int item2 = 0;
							int item3 = 0;
							int item4 = 0;
							if (armorComponent != null)
							{
								item6 = armorComponent.HeadArmor;
								item2 = armorComponent.BodyArmor;
								item3 = armorComponent.LegArmor;
								item4 = armorComponent.ArmArmor;
							}
							Tuple<int, int, int, int> item5 = new Tuple<int, int, int, int>(item6, item2, item3, item4);
							this._currentArmorValues.Insert(j, item5);
						}
					}
					else
					{
						item.Entity.SetVisibilityExcludeParents(false);
					}
				}
			}
			this.PositionCurrentItems();
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00007A04 File Offset: 0x00005C04
		private void SpawnHorse(ItemObject horse, ItemObject harness)
		{
			ItemRosterElement itemRosterElement = default(ItemRosterElement);
			ItemRosterElement itemRosterElement2 = default(ItemRosterElement);
			if (horse != null)
			{
				itemRosterElement = new ItemRosterElement(horse, 1, null);
				if (harness != null)
				{
					itemRosterElement2 = new ItemRosterElement(harness, 1, null);
				}
				else
				{
					itemRosterElement2 = ItemRosterElement.Invalid;
				}
			}
			else
			{
				if (harness == null)
				{
					return;
				}
				itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("mule"), 1, null);
				itemRosterElement2 = new ItemRosterElement(harness, 1, null);
			}
			if (this._mountAgent != null)
			{
				this._mountAgent.FadeOut(true, false);
			}
			this._horse = itemRosterElement.EquipmentElement.Item;
			this._harness = itemRosterElement2.EquipmentElement.Item;
			Mission mission = base.Mission;
			ItemRosterElement rosterElement = itemRosterElement;
			ItemRosterElement harnessRosterElement = itemRosterElement2;
			Vec3 vec = new Vec3(500f, this._mainAgent.Position.y - 3f, 1f, -1f);
			this._mountAgent = mission.SpawnMonster(rosterElement, harnessRosterElement, vec, Vec2.Forward, -1);
			this._mountAgent.Controller = AgentControllerType.None;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00007B04 File Offset: 0x00005D04
		private void SpawnAgent(string id)
		{
			Agent mountAgent = this._mountAgent;
			if (mountAgent != null)
			{
				mountAgent.FadeOut(true, false);
			}
			Agent mainAgent = this._mainAgent;
			if (mainAgent != null)
			{
				mainAgent.FadeOut(true, false);
			}
			CharacterObject @object = Game.Current.ObjectManager.GetObject<CharacterObject>(id);
			List<Equipment> list = @object.BattleEquipments.ToList<Equipment>();
			list.AddRange(@object.CivilianEquipments.ToList<Equipment>());
			list.AddRange(@object.StealthEquipments.ToList<Equipment>());
			Mission mission = base.Mission;
			AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Equipment(list[this._setIndex]).NoHorses(true).Team(base.Mission.DefenderTeam);
			Vec3 vec = new Vec3(500f, 200f, 1f, -1f);
			this._mainAgent = mission.SpawnAgent(agentBuildData.InitialPosition(vec).InitialDirection(Vec2.Forward), false);
			if (list[this._setIndex].Horse.Item != null)
			{
				this.SpawnHorse(list[this._setIndex].Horse.Item, list[this._setIndex].GetEquipmentFromSlot(EquipmentIndex.HorseHarness).Item);
			}
			this._groupIndex = @object.DefaultFormationGroup;
			this._attributes[0] = @object.StringId;
			this._attributes[1] = @object.Name.ToString();
			this._attributes[2] = @object.Level.ToString();
			this._attributes[3] = @object.Occupation.ToString();
			for (int i = 0; i < this._attributes.Length; i++)
			{
				this._spawnAttributes[i] = this._attributes[i];
			}
			this._groupIndex = @object.DefaultFormationGroup;
			for (int j = 0; j < this._allFactions.Count; j++)
			{
				if (this._allFactions[j].StringId == @object.Culture.StringId)
				{
					this._factionIndex = j;
					this._itemCulture = -1;
					break;
				}
			}
			this._spawnSetIndex = this._setIndex;
			this.UpdateActiveItems();
			this.UpdateCamera();
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00007D48 File Offset: 0x00005F48
		private void PositionCurrentItems()
		{
			float num = 0f;
			float z = 0f;
			if (this._activeFilter == 6)
			{
				this._diff = 1.5f;
			}
			else if (this._activeFilter == 10 || this._activeFilter == 11)
			{
				this._diff = 4f;
				num = -1f;
			}
			else if (this._activeFilter == 1 || this._activeFilter == 2 || this._activeFilter == 14 || this._activeFilter == 16)
			{
				this._diff = 1.5f;
			}
			else if (this._activeFilter == 13 || this._activeFilter == 15)
			{
				this._diff = 2.5f;
				z = 1f;
			}
			else
			{
				this._diff = 0.75f;
			}
			float num2 = -((float)(this._currentItems.Count / 2) * this._diff);
			this._textStart = this._mainAgent.Position + new Vec3(-1f, 1f, 0f, -1f) + Vec3.Up * num2;
			foreach (MissionEquipItemToolView.ItemData itemData in this._currentItems)
			{
				Mat3 identity = Mat3.Identity;
				Vec3 vec = this._mainAgent.Position + new Vec3(-1f, 1f + num, z, -1f) + Vec3.Up * num2;
				MatrixFrame matrixFrame = new MatrixFrame(ref identity, ref vec);
				itemData.Entity.SetVisibilityExcludeParents(true);
				itemData.Entity.SetFrame(ref matrixFrame, true);
				num2 += this._diff;
				if (itemData.Entity.GetMetaMesh(0) != null)
				{
					BoundingBox boundingBox = itemData.Entity.GetMetaMesh(0).GetBoundingBox();
					if (!this._bounds.Contains(boundingBox))
					{
						this._bounds.Add(boundingBox);
					}
				}
			}
			this._index = this._currentItems.Count / 2;
			this._pivotDiff = 0f;
			foreach (BoundingBox boundingBox2 in this._bounds)
			{
				this._pivotDiff += boundingBox2.center.z;
			}
			this._pivotDiff /= (float)this._bounds.Count;
			this._bounds.Clear();
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00007FF0 File Offset: 0x000061F0
		private void EditNode()
		{
			this._charactersXml = this.LoadXmlFile(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
			this._charactersXml.DocumentElement.SelectNodes("NPCCharacter");
			foreach (object obj in this._charactersXml.DocumentElement.SelectNodes("NPCCharacter"))
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Attributes["id"] != null && this._spawnAttributes[0] == xmlNode.Attributes["id"].InnerText)
				{
					xmlNode.Attributes["id"].InnerText = this._attributes[0];
					xmlNode.Attributes["name"].InnerText = this._attributes[1];
					if (xmlNode.Attributes["level"] != null)
					{
						xmlNode.Attributes["level"].InnerText = this._attributes[2];
					}
					xmlNode.Attributes["occupation"].InnerText = this._attributes[3];
					xmlNode.Attributes["culture"].InnerText = "Culture." + this._allFactions[this._factionIndex].StringId;
					xmlNode.Attributes["default_group"].InnerText = this._groups[this._groupIndex].GetName();
					this.SlotCheck("Head", 0, xmlNode, null);
					this.SlotCheck("Cape", 1, xmlNode, null);
					this.SlotCheck("Body", 2, xmlNode, null);
					this.SlotCheck("Gloves", 3, xmlNode, null);
					this.SlotCheck("Leg", 4, xmlNode, null);
					this.SlotCheck("Item0", 5, xmlNode, null);
					this.SlotCheck("Item1", 6, xmlNode, null);
					this.SlotCheck("Item2", 7, xmlNode, null);
					this.SlotCheck("Item3", 8, xmlNode, null);
					this.SlotCheck("Horse", -1, xmlNode, this._horse);
					this.SlotCheck("HorseHarness", -1, xmlNode, this._harness);
					this._charactersXml.Save(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
					for (int i = 0; i < this._attributes.Length; i++)
					{
						this._spawnAttributes[i] = this._attributes[i];
					}
				}
			}
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x000082AC File Offset: 0x000064AC
		private void CheckForLoad()
		{
			if (this._spawnAttributes[0] != this._attributes[0] && Game.Current.ObjectManager.GetObject<CharacterObject>(this._attributes[0]) != null)
			{
				this.SpawnAgent(this._attributes[0]);
				return;
			}
			if (this._spawnAttributes[1] != this._attributes[1])
			{
				foreach (CharacterObject characterObject in this._allCharacters)
				{
					if (characterObject.Name.ToString() == this._attributes[1])
					{
						this.SpawnAgent(characterObject.StringId);
						return;
					}
				}
			}
			if (this._setIndex != this._spawnSetIndex)
			{
				this.SpawnAgent(this._mainAgent.Character.StringId);
			}
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000839C File Offset: 0x0000659C
		private void UpdateActiveItems()
		{
			this._activeItems.Clear();
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.NumAllWeaponSlots].Item);
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.Cape].Item);
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.Body].Item);
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.Gloves].Item);
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.Leg].Item);
			MissionWeapon missionWeapon = this._mainAgent.Equipment[EquipmentIndex.WeaponItemBeginSlot];
			if (missionWeapon.WeaponsCount > 0)
			{
				this._activeItems.Add(missionWeapon.Item);
			}
			MissionWeapon missionWeapon2 = this._mainAgent.Equipment[EquipmentIndex.Weapon1];
			if (missionWeapon2.WeaponsCount > 0)
			{
				this._activeItems.Add(missionWeapon2.Item);
			}
			MissionWeapon missionWeapon3 = this._mainAgent.Equipment[EquipmentIndex.Weapon2];
			if (missionWeapon3.WeaponsCount > 0)
			{
				this._activeItems.Add(missionWeapon3.Item);
			}
			MissionWeapon missionWeapon4 = this._mainAgent.Equipment[EquipmentIndex.Weapon3];
			if (missionWeapon4.WeaponsCount > 0)
			{
				this._activeItems.Add(missionWeapon4.Item);
			}
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item);
			this._activeItems.Add(this._mainAgent.SpawnEquipment[EquipmentIndex.HorseHarness].Item);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x00008574 File Offset: 0x00006774
		private void SlotCheck(string slotName, int index, XmlNode parentNode, ItemObject obj = null)
		{
			XmlNodeList xmlNodeList = parentNode.SelectNodes("equipmentSet")[this._setIndex].SelectNodes("equipment");
			bool flag = false;
			foreach (object obj2 in xmlNodeList)
			{
				XmlNode xmlNode = (XmlNode)obj2;
				if (xmlNode.Attributes != null && xmlNode.Attributes["slot"].InnerText == slotName)
				{
					if ((index != -1 && this._activeItems[index] == null) || (index == -1 && obj == null))
					{
						xmlNode.ParentNode.RemoveChild(xmlNode);
						return;
					}
					xmlNode.Attributes["id"].Value = "Item." + ((obj == null) ? this._activeItems[index].StringId : obj.StringId);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if ((index != -1 && this._activeItems[index] == null) || (index == -1 && obj == null))
				{
					return;
				}
				XmlElement xmlElement = this._charactersXml.CreateElement("equipment");
				XmlAttribute xmlAttribute = this._charactersXml.CreateAttribute("slot");
				xmlAttribute.Value = slotName;
				XmlAttribute xmlAttribute2 = this._charactersXml.CreateAttribute("id");
				xmlAttribute2.Value = "Item." + ((obj == null) ? this._activeItems[index].StringId : obj.StringId);
				xmlElement.Attributes.Append(xmlAttribute);
				xmlElement.Attributes.Append(xmlAttribute2);
				parentNode.SelectNodes("equipmentSet")[this._setIndex].AppendChild(xmlElement);
			}
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00008748 File Offset: 0x00006948
		private void UpdateCamera()
		{
			Vec3 v = ((this._mainAgent.MountAgent == null) ? new Vec3(1.3f, 2f, 1f, -1f) : new Vec3(2f, 3f, 2f, -1f));
			MatrixFrame matrixFrame = default(MatrixFrame);
			matrixFrame.rotation.u = -(this._mainAgent.Position - this._cam.Position).NormalizedCopy();
			matrixFrame.rotation.f = Vec3.Up;
			matrixFrame.rotation.s = Vec3.CrossProduct(matrixFrame.rotation.f, matrixFrame.rotation.u);
			matrixFrame.rotation.s.Normalize();
			matrixFrame.rotation.Orthonormalize();
			float aspectRatio = Screen.AspectRatio;
			this._cam.SetFovVertical(1.134464f, aspectRatio, 1E-08f, 1000f);
			matrixFrame.origin = this._mainAgent.Position + v;
			this._cam.Frame = matrixFrame;
			base.MissionScreen.CustomCamera = this._cam;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x00008880 File Offset: 0x00006A80
		private void SaveToXml()
		{
			this._charactersXml = this.LoadXmlFile(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/spnpccharacters.xml");
			XmlNodeList xmlNodeList = this._charactersXml.DocumentElement.SelectNodes("/NPCCharacters");
			bool flag = false;
			string text = "\n  <equipmentSet>\n";
			foreach (object obj in this._charactersXml.DocumentElement.SelectNodes("NPCCharacter"))
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Attributes["id"] != null && this._attributes[0] == xmlNode.Attributes["id"].InnerText)
				{
					flag = true;
					if (this._setIndex > this._spawnSetIndex)
					{
						for (int i = 0; i < 9; i++)
						{
							if (this._activeItems[i] != null)
							{
								string text2 = "";
								if (i == 0)
								{
									text2 = "Head";
								}
								else if (i == 1)
								{
									text2 = "Cape";
								}
								else if (i == 2)
								{
									text2 = "Body";
								}
								else if (i == 3)
								{
									text2 = "Gloves";
								}
								else if (i == 4)
								{
									text2 = "Leg";
								}
								else if (i == 5)
								{
									text2 = "Item0";
								}
								else if (i == 6)
								{
									text2 = "Item1";
								}
								else if (i == 7)
								{
									text2 = "Item2";
								}
								else if (i == 8)
								{
									text2 = "Item3";
								}
								text = string.Concat(new string[]
								{
									text,
									"    <equipment slot=\"",
									text2,
									"\" id=\"Item.",
									this._activeItems[i].StringId,
									"\" />\n"
								});
							}
						}
						if (this._horse != null)
						{
							text = text + "    <equipment slot=\"Horse\" id=\"Item." + this._horse.StringId + "\" />\n";
						}
						if (this._harness != null)
						{
							text = text + "    <equipment slot=\"HorseHarness\" id=\"Item." + this._harness.StringId + "\" />\n";
						}
						text += "  </equipmentSet>\n";
						XmlNode xmlNode2 = xmlNode;
						xmlNode2.InnerXml += text;
						this._charactersXml.Save(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
						Utilities.ConstructMainThreadJob(new MissionEquipItemToolView.MainThreadDelegate(Mission.Current.EndMission), Array.Empty<object>());
						MBGameManager.EndGame();
						for (int j = 0; j < 10; j++)
						{
						}
						break;
					}
					return;
				}
				else if (xmlNode.Attributes["id"] == null || this._attributes[1] == xmlNode.Attributes["name"].InnerText)
				{
				}
			}
			if (!flag)
			{
				XmlElement xmlElement = this._charactersXml.CreateElement("NPCCharacter");
				XmlAttribute xmlAttribute = this._charactersXml.CreateAttribute("id");
				xmlAttribute.Value = this._attributes[0];
				xmlElement.Attributes.Append(xmlAttribute);
				XmlAttribute xmlAttribute2 = this._charactersXml.CreateAttribute("default_group");
				xmlAttribute2.Value = this._groups[this._groupIndex].GetName();
				xmlElement.Attributes.Append(xmlAttribute2);
				XmlAttribute xmlAttribute3 = this._charactersXml.CreateAttribute("level");
				xmlAttribute3.Value = this._attributes[2];
				xmlElement.Attributes.Append(xmlAttribute3);
				XmlAttribute xmlAttribute4 = this._charactersXml.CreateAttribute("name");
				xmlAttribute4.Value = this._attributes[1];
				xmlElement.Attributes.Append(xmlAttribute4);
				XmlAttribute xmlAttribute5 = this._charactersXml.CreateAttribute("occupation");
				xmlAttribute5.Value = this._attributes[3];
				xmlElement.Attributes.Append(xmlAttribute5);
				XmlAttribute xmlAttribute6 = this._charactersXml.CreateAttribute("culture");
				xmlAttribute6.Value = "Culture." + this._allFactions[this._factionIndex].StringId;
				xmlElement.Attributes.Append(xmlAttribute6);
				XmlElement xmlElement2 = this._charactersXml.CreateElement("face");
				XmlElement xmlElement3 = this._charactersXml.CreateElement("face_key_template");
				XmlAttribute xmlAttribute7 = this._charactersXml.CreateAttribute("value");
				xmlAttribute7.Value = "NPCCharacter.villager_vlandia";
				xmlElement3.Attributes.Append(xmlAttribute7);
				xmlElement2.AppendChild(xmlElement3);
				xmlElement.AppendChild(xmlElement2);
				XmlElement xmlElement4 = this._charactersXml.CreateElement("equipmentSet");
				for (int k = 0; k < 9; k++)
				{
					if (this._activeItems[k] != null)
					{
						XmlElement xmlElement5 = this._charactersXml.CreateElement("equipment");
						XmlAttribute xmlAttribute8 = this._charactersXml.CreateAttribute("slot");
						string value = "";
						if (k == 0)
						{
							value = "Head";
						}
						else if (k == 1)
						{
							value = "Cape";
						}
						else if (k == 2)
						{
							value = "Body";
						}
						else if (k == 3)
						{
							value = "Gloves";
						}
						else if (k == 4)
						{
							value = "Leg";
						}
						else if (k == 5)
						{
							value = "Item0";
						}
						else if (k == 6)
						{
							value = "Item1";
						}
						else if (k == 7)
						{
							value = "Item2";
						}
						else if (k == 8)
						{
							value = "Item3";
						}
						xmlAttribute8.Value = value;
						xmlElement5.Attributes.Append(xmlAttribute8);
						XmlAttribute xmlAttribute9 = this._charactersXml.CreateAttribute("id");
						xmlAttribute9.Value = "Item." + this._activeItems[k].StringId;
						xmlElement5.Attributes.Append(xmlAttribute9);
						xmlElement4.AppendChild(xmlElement5);
					}
				}
				xmlElement.AppendChild(xmlElement4);
				if (this._horse != null)
				{
					XmlElement xmlElement6 = this._charactersXml.CreateElement("equipment");
					XmlAttribute xmlAttribute10 = this._charactersXml.CreateAttribute("slot");
					xmlAttribute10.Value = "Horse";
					xmlElement6.Attributes.Append(xmlAttribute10);
					XmlAttribute xmlAttribute11 = this._charactersXml.CreateAttribute("id");
					xmlAttribute11.Value = "Item." + this._horse.StringId;
					xmlElement6.Attributes.Append(xmlAttribute11);
					xmlElement.AppendChild(xmlElement6);
				}
				if (this._harness != null)
				{
					XmlElement xmlElement7 = this._charactersXml.CreateElement("equipment");
					XmlAttribute xmlAttribute12 = this._charactersXml.CreateAttribute("slot");
					xmlAttribute12.Value = "HorseHarness";
					xmlElement7.Attributes.Append(xmlAttribute12);
					XmlAttribute xmlAttribute13 = this._charactersXml.CreateAttribute("id");
					xmlAttribute13.Value = "Item." + this._harness.StringId;
					xmlElement7.Attributes.Append(xmlAttribute13);
					xmlElement.AppendChild(xmlElement7);
				}
				xmlNodeList[xmlNodeList.Count - 1].AppendChild(xmlElement);
				this._charactersXml.Save(ModuleHelper.GetModuleFullPath("SandBoxCore") + "ModuleData/spnpccharacters.xml");
				Utilities.ConstructMainThreadJob(new MissionEquipItemToolView.MainThreadDelegate(Mission.Current.EndMission), Array.Empty<object>());
				MBGameManager.EndGame();
				for (int l = 0; l < 10; l++)
				{
				}
			}
		}

		// Token: 0x060000AB RID: 171 RVA: 0x00009010 File Offset: 0x00007210
		private void Clear(bool[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = false;
			}
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00009030 File Offset: 0x00007230
		private XmlDocument LoadXmlFile(string path)
		{
			Debug.Print("opening " + path, 0, Debug.DebugColor.White, 17592186044416UL);
			XmlDocument xmlDocument = new XmlDocument();
			StreamReader streamReader = new StreamReader(path);
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
			return xmlDocument;
		}

		// Token: 0x04000035 RID: 53
		private string str = "";

		// Token: 0x04000036 RID: 54
		private int _itemCulture;

		// Token: 0x04000037 RID: 55
		private bool[] _filters = new bool[17];

		// Token: 0x04000038 RID: 56
		private bool _genderSet;

		// Token: 0x04000039 RID: 57
		private Agent _mainAgent;

		// Token: 0x0400003A RID: 58
		private List<ItemObject> _allItemObjects = new List<ItemObject>();

		// Token: 0x0400003B RID: 59
		private List<MissionEquipItemToolView.ItemData> _allItems = new List<MissionEquipItemToolView.ItemData>();

		// Token: 0x0400003C RID: 60
		private List<MissionEquipItemToolView.ItemData> _currentItems = new List<MissionEquipItemToolView.ItemData>();

		// Token: 0x0400003D RID: 61
		private List<Tuple<int, int, int, int>> _currentArmorValues = new List<Tuple<int, int, int, int>>();

		// Token: 0x0400003E RID: 62
		private List<CultureObject> _allFactions = new List<CultureObject>();

		// Token: 0x0400003F RID: 63
		private List<CharacterObject> _allCharacters = new List<CharacterObject>();

		// Token: 0x04000040 RID: 64
		private List<FormationClass> _groups = new List<FormationClass>();

		// Token: 0x04000041 RID: 65
		private int _activeIndex = -1;

		// Token: 0x04000042 RID: 66
		private int _factionIndex;

		// Token: 0x04000043 RID: 67
		private int _groupIndex;

		// Token: 0x04000044 RID: 68
		private XmlDocument _charactersXml;

		// Token: 0x04000045 RID: 69
		private List<XmlDocument> _itemsXmls;

		// Token: 0x04000046 RID: 70
		private string[] _attributes = new string[] { "id", "name", "level", "occupation", "culture", "group" };

		// Token: 0x04000047 RID: 71
		private string[] _spawnAttributes = new string[] { "id", "name", "level", "occupation", "culture", "group" };

		// Token: 0x04000048 RID: 72
		private bool underscoreGuard;

		// Token: 0x04000049 RID: 73
		private bool yGuard;

		// Token: 0x0400004A RID: 74
		private bool zGuard;

		// Token: 0x0400004B RID: 75
		private bool xGuard;

		// Token: 0x0400004C RID: 76
		private bool _capsLock;

		// Token: 0x0400004D RID: 77
		private List<ItemObject> _activeItems = new List<ItemObject>();

		// Token: 0x0400004E RID: 78
		private int _setIndex;

		// Token: 0x0400004F RID: 79
		private int _spawnSetIndex;

		// Token: 0x04000050 RID: 80
		private Camera _cam;

		// Token: 0x04000051 RID: 81
		private bool _init = true;

		// Token: 0x04000052 RID: 82
		private int _index;

		// Token: 0x04000053 RID: 83
		private float _diff = 0.75f;

		// Token: 0x04000054 RID: 84
		private int _activeFilter;

		// Token: 0x04000055 RID: 85
		private int _activeWeaponSlot;

		// Token: 0x04000056 RID: 86
		private Vec3 _textStart;

		// Token: 0x04000057 RID: 87
		private List<BoundingBox> _bounds = new List<BoundingBox>();

		// Token: 0x04000058 RID: 88
		private float _pivotDiff;

		// Token: 0x04000059 RID: 89
		private Agent _mountAgent;

		// Token: 0x0400005A RID: 90
		private ItemObject _horse;

		// Token: 0x0400005B RID: 91
		private ItemObject _harness;

		// Token: 0x02000086 RID: 134
		private enum Filter
		{
			// Token: 0x040002A1 RID: 673
			Head = 5,
			// Token: 0x040002A2 RID: 674
			Cape = 9,
			// Token: 0x040002A3 RID: 675
			Body = 6,
			// Token: 0x040002A4 RID: 676
			Hand = 8,
			// Token: 0x040002A5 RID: 677
			Leg = 7,
			// Token: 0x040002A6 RID: 678
			Shield = 12,
			// Token: 0x040002A7 RID: 679
			Bow,
			// Token: 0x040002A8 RID: 680
			Crossbow = 15,
			// Token: 0x040002A9 RID: 681
			Horse = 10,
			// Token: 0x040002AA RID: 682
			Onehanded = 1,
			// Token: 0x040002AB RID: 683
			Twohanded,
			// Token: 0x040002AC RID: 684
			Polearm,
			// Token: 0x040002AD RID: 685
			Thrown,
			// Token: 0x040002AE RID: 686
			Arrow = 14,
			// Token: 0x040002AF RID: 687
			Bolt = 16,
			// Token: 0x040002B0 RID: 688
			Harness = 11
		}

		// Token: 0x02000087 RID: 135
		// (Invoke) Token: 0x06000580 RID: 1408
		private delegate void MainThreadDelegate();

		// Token: 0x02000088 RID: 136
		private class ItemData
		{
			// Token: 0x040002B1 RID: 689
			public GameEntity Entity;

			// Token: 0x040002B2 RID: 690
			public string Name;

			// Token: 0x040002B3 RID: 691
			public string Id;

			// Token: 0x040002B4 RID: 692
			public BasicCultureObject Culture;

			// Token: 0x040002B5 RID: 693
			public ItemObject.ItemTypeEnum itemType;

			// Token: 0x040002B6 RID: 694
			public MissionEquipItemToolView.GenderEnum Gender;
		}

		// Token: 0x02000089 RID: 137
		public enum GenderEnum
		{
			// Token: 0x040002B8 RID: 696
			Male = 1,
			// Token: 0x040002B9 RID: 697
			Unisex,
			// Token: 0x040002BA RID: 698
			Female
		}
	}
}
