using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Map.DistanceCache;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map
{
	// Token: 0x0200005E RID: 94
	public class SettlementPositionScript : ScriptComponentBehavior
	{
		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060003A5 RID: 933 RVA: 0x0001CF18 File Offset: 0x0001B118
		private string SettlementsXmlPath
		{
			get
			{
				string text = base.Scene.GetModulePath();
				if (text.Contains("$BASE"))
				{
					text = text.Remove(0, 6);
					text = BasePath.Name + text;
				}
				return text + "ModuleData/settlements.xml";
			}
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x0001CF60 File Offset: 0x0001B160
		protected override void OnInit()
		{
			try
			{
				this.InitializeCachedVariables();
				bool useNavalNavigation = false;
				if (this.GetMapIsNavalDLC() || (!this.GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC")))
				{
					useNavalNavigation = true;
				}
				this.RegisterNavigationCachesOnGameLoad(useNavalNavigation);
			}
			catch (Exception ex)
			{
				Debug.Print("Error when reading distance cache " + ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				Debug.Print("SettlementsDistanceCacheFilePath could not be read!. Campaign starting performance will be affected very badly, cache will be initialized now.", 0, Debug.DebugColor.White, 17592186044416UL);
				Debug.FailedAssert("SettlementsDistanceCacheFilePath could not be read!. Campaign starting performance will be affected very badly, cache will be initialized now.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "OnInit", 536);
			}
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x0001D000 File Offset: 0x0001B200
		private void RegisterNavigationCachesOnGameLoad(bool useNavalNavigation)
		{
			SandBoxNavigationCache cacheToRegister = this.ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType.Default);
			this._mapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Default, cacheToRegister);
			if (useNavalNavigation)
			{
				SandBoxNavigationCache cacheToRegister2 = this.ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType.Naval);
				SandBoxNavigationCache cacheToRegister3 = this.ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType.All);
				this._mapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.Naval, cacheToRegister2);
				this._mapDistanceModel.RegisterDistanceCache(MobileParty.NavigationType.All, cacheToRegister3);
			}
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x0001D050 File Offset: 0x0001B250
		private SandBoxNavigationCache ReadNavigationCacheForNavigationTypeOnGameLoad(MobileParty.NavigationType navigationCapability)
		{
			string text = string.Empty;
			foreach (ModuleInfo moduleInfo in ModuleHelper.GetActiveModules())
			{
				string text2;
				if (moduleInfo.IsActive && this.GetSettlementsDistanceCacheFileForCapability(moduleInfo.Id, navigationCapability, out text2))
				{
					text = text2;
				}
			}
			SandBoxNavigationCache sandBoxNavigationCache;
			if (!string.IsNullOrEmpty(text))
			{
				sandBoxNavigationCache = this.ReadNavigationCacheOnGameLoad(text, navigationCapability);
			}
			else
			{
				Debug.FailedAssert(string.Format("Navigation type with id {0} file is not found, this should not be happening, will generate cache (this will take some time)", navigationCapability), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "ReadNavigationCacheForNavigationTypeOnGameLoad", 576);
				sandBoxNavigationCache = new SandBoxNavigationCache(navigationCapability);
				sandBoxNavigationCache.GenerateCacheData();
			}
			return sandBoxNavigationCache;
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x0001D104 File Offset: 0x0001B304
		private SandBoxNavigationCache ReadNavigationCacheOnGameLoad(string path, MobileParty.NavigationType navigationCapability)
		{
			SandBoxNavigationCache sandBoxNavigationCache = new SandBoxNavigationCache(navigationCapability);
			sandBoxNavigationCache.Deserialize(path);
			return sandBoxNavigationCache;
		}

		// Token: 0x060003AA RID: 938 RVA: 0x0001D113 File Offset: 0x0001B313
		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			this._partyNavigationModelOverriddenClassName = "";
			this._distanceModelOverridenClassName = "";
			this.InitializeCachedVariables();
		}

		// Token: 0x060003AB RID: 939 RVA: 0x0001D138 File Offset: 0x0001B338
		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName == "SavePositions")
			{
				this.SaveSettlementPositions();
			}
			if (variableName == "ComputeAndSaveSettlementDistanceCache")
			{
				this.SaveSettlementDistanceCacheEditor();
			}
			if (variableName == "CheckPositions")
			{
				this.CheckSettlementPositions();
			}
			if (variableName == "_partyNavigationModelOverriddenClassName" || variableName == "_distanceModelOverridenClassName")
			{
				this.InitializeCachedVariables();
			}
		}

		// Token: 0x060003AC RID: 940 RVA: 0x0001D1A5 File Offset: 0x0001B3A5
		protected override void OnSceneSave(string saveFolder)
		{
			base.OnSceneSave(saveFolder);
			this.SaveSettlementPositions();
		}

		// Token: 0x060003AD RID: 941 RVA: 0x0001D1B4 File Offset: 0x0001B3B4
		private void CheckSettlementPositions()
		{
			XmlDocument xmlDocument = this.LoadXmlFile(this.SettlementsXmlPath);
			base.GameEntity.RemoveAllChildren();
			PartyNavigationModel partyNavigationModel = this.GetPartyNavigationModel();
			bool[] regionMapping = SandBoxHelpers.MapSceneHelper.GetRegionMapping(partyNavigationModel);
			base.GameEntity.Scene.SetNavMeshRegionMap(regionMapping);
			List<int> list = partyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default).ToList<int>();
			list.Add(0);
			List<int> list2 = null;
			foreach (object obj in xmlDocument.DocumentElement.SelectNodes("Settlement"))
			{
				string value = ((XmlNode)obj).Attributes["id"].Value;
				GameEntity campaignEntityWithName = base.Scene.GetCampaignEntityWithName(value);
				if (campaignEntityWithName != null)
				{
					Vec3 origin = campaignEntityWithName.GetGlobalFrame().origin;
					Vec3 vec = default(Vec3);
					Vec3 pos = default(Vec3);
					List<GameEntity> list3 = new List<GameEntity>();
					campaignEntityWithName.GetChildrenRecursive(ref list3);
					bool flag = false;
					bool flag2 = false;
					foreach (GameEntity gameEntity in list3)
					{
						if (gameEntity.HasTag("main_map_city_gate"))
						{
							vec = gameEntity.GetGlobalFrame().origin;
							flag = true;
						}
						if (gameEntity.HasTag("main_map_city_port"))
						{
							pos = gameEntity.GetGlobalFrame().origin;
							flag2 = true;
						}
					}
					Vec3 pos2 = origin;
					if (flag)
					{
						pos2 = vec;
					}
					PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
					base.GameEntity.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, pos2.AsVec2, true, true, false);
					int item = 0;
					if (nullFaceRecord.IsValid())
					{
						item = nullFaceRecord.FaceGroupIndex;
					}
					if (list.Contains(item))
					{
						Debug.Print(string.Format("There is gate position problem with settlement {0} at position:  {1}", campaignEntityWithName.Name, pos2.AsVec2), 0, Debug.DebugColor.White, 17592186044416UL);
						MBEditor.ZoomToPosition(pos2);
						break;
					}
					if (flag2)
					{
						if (list2 == null)
						{
							list2 = partyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Naval).ToList<int>();
							list2.Add(0);
						}
						nullFaceRecord = PathFaceRecord.NullFaceRecord;
						base.GameEntity.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, pos.AsVec2, false, true, false);
						item = 0;
						if (nullFaceRecord.IsValid())
						{
							item = nullFaceRecord.FaceGroupIndex;
						}
						if (list2.Contains(item))
						{
							Debug.Print(string.Format("There is port position problem with settlement {0} at position:  {1}", campaignEntityWithName.Name, pos.AsVec2), 0, Debug.DebugColor.White, 17592186044416UL);
							MBEditor.ZoomToPosition(pos);
							break;
						}
					}
				}
			}
		}

		// Token: 0x060003AE RID: 942 RVA: 0x0001D48C File Offset: 0x0001B68C
		private void InitializeCachedVariables()
		{
			this._mapIsNavalDLC = string.Equals("NavalDLC", this.GetMapModuleId(), StringComparison.CurrentCultureIgnoreCase);
			this._mapIsSandBox = string.Equals("Sandbox", this.GetMapModuleId(), StringComparison.CurrentCultureIgnoreCase);
			this._partyNavigationModel = this.GetPartyNavigationModel();
			this._mapDistanceModel = this.GetMapDistanceModel();
		}

		// Token: 0x060003AF RID: 943 RVA: 0x0001D4DF File Offset: 0x0001B6DF
		protected override bool IsOnlyVisual()
		{
			return true;
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x0001D4E2 File Offset: 0x0001B6E2
		private bool GetMapIsNavalDLC()
		{
			return this._mapIsNavalDLC;
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x0001D4EA File Offset: 0x0001B6EA
		private bool GetMapIsSandBox()
		{
			return this._mapIsSandBox;
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x0001D4F2 File Offset: 0x0001B6F2
		private string GetMapModuleId()
		{
			return base.Scene.GetModulePath().Trim().TrimEnd(new char[] { '/' })
				.Split(new char[] { '/' })
				.Last<string>();
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x0001D52C File Offset: 0x0001B72C
		private PartyNavigationModel GetPartyNavigationModel()
		{
			if (Campaign.Current != null)
			{
				return Campaign.Current.Models.PartyNavigationModel;
			}
			if (string.IsNullOrEmpty(this._partyNavigationModelOverriddenClassName))
			{
				if (this.GetMapIsSandBox())
				{
					this._partyNavigationModelOverriddenClassName = "DefaultPartyNavigationModel";
					return SettlementPositionScript.CreateBaseNavigationModel(false);
				}
				if (this.GetMapIsNavalDLC())
				{
					if (!ModuleHelper.IsModuleActive("NavalDLC"))
					{
						throw new ApplicationException("NavalDlc map changes can not be made without NavalDlc module!");
					}
					this._partyNavigationModelOverriddenClassName = "NavalPartyNavigationModel";
					return SettlementPositionScript.CreateBaseNavigationModel(true);
				}
				else
				{
					if (ModuleHelper.IsModuleActive("NavalDLC"))
					{
						this._partyNavigationModelOverriddenClassName = "NavalPartyNavigationModel";
						return SettlementPositionScript.CreateBaseNavigationModel(true);
					}
					this._partyNavigationModelOverriddenClassName = "DefaultPartyNavigationModel";
					return SettlementPositionScript.CreateBaseNavigationModel(false);
				}
			}
			else
			{
				if (SettlementPositionScript.FindClass(this._partyNavigationModelOverriddenClassName) == null)
				{
					Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "GetPartyNavigationModel", 826);
					return SettlementPositionScript.CreateBaseNavigationModel(this.GetMapIsNavalDLC());
				}
				return SettlementPositionScript.CreateCustomNavigationModel(this._partyNavigationModelOverriddenClassName, !this.GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC"));
			}
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x0001D630 File Offset: 0x0001B830
		private MapDistanceModel GetMapDistanceModel()
		{
			if (Campaign.Current != null)
			{
				return Campaign.Current.Models.MapDistanceModel;
			}
			if (string.IsNullOrEmpty(this._distanceModelOverridenClassName))
			{
				if (this.GetMapIsSandBox())
				{
					this._distanceModelOverridenClassName = "DefaultMapDistanceModel";
					return SettlementPositionScript.CreateBaseDistanceModel(false);
				}
				if (this.GetMapIsNavalDLC())
				{
					if (!ModuleHelper.IsModuleActive("NavalDLC"))
					{
						throw new ApplicationException("NavalDlc map changes can not be made without NavalDlc module!");
					}
					this._distanceModelOverridenClassName = "NavalDLCMapDistanceModel";
					return SettlementPositionScript.CreateBaseDistanceModel(true);
				}
				else
				{
					if (ModuleHelper.IsModuleActive("NavalDLC"))
					{
						this._distanceModelOverridenClassName = "NavalDLCMapDistanceModel";
						return SettlementPositionScript.CreateBaseDistanceModel(true);
					}
					this._distanceModelOverridenClassName = "DefaultMapDistanceModel";
					return SettlementPositionScript.CreateBaseDistanceModel(false);
				}
			}
			else
			{
				if (SettlementPositionScript.FindClass(this._distanceModelOverridenClassName) == null)
				{
					Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "GetMapDistanceModel", 882);
					return SettlementPositionScript.CreateBaseDistanceModel(this.GetMapIsNavalDLC());
				}
				return SettlementPositionScript.CreateCustomMapDistanceModel(this._distanceModelOverridenClassName, !this.GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC"));
			}
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x0001D734 File Offset: 0x0001B934
		private static PartyNavigationModel CreateCustomNavigationModel(string name, bool naval)
		{
			if (name == "DefaultPartyNavigationModel")
			{
				return SettlementPositionScript.CreateBaseNavigationModel(false);
			}
			Type type = SettlementPositionScript.FindClass(name);
			if (type == null)
			{
				Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "CreateCustomNavigationModel", 903);
				return SettlementPositionScript.CreateBaseNavigationModel(naval);
			}
			if (type.GetConstructor(new Type[] { typeof(PartyNavigationModel) }) != null)
			{
				return (PartyNavigationModel)Activator.CreateInstance(type, new object[] { SettlementPositionScript.CreateBaseNavigationModel(naval) });
			}
			return (PartyNavigationModel)Activator.CreateInstance(type);
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x0001D7CC File Offset: 0x0001B9CC
		private static MapDistanceModel CreateCustomMapDistanceModel(string name, bool naval)
		{
			if (name == "DefaultMapDistanceModel")
			{
				return SettlementPositionScript.CreateBaseDistanceModel(false);
			}
			Type type = SettlementPositionScript.FindClass(name);
			if (type == null)
			{
				Debug.FailedAssert("Cant find custom navigation model", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "CreateCustomMapDistanceModel", 930);
				return SettlementPositionScript.CreateBaseDistanceModel(naval);
			}
			return (MapDistanceModel)Activator.CreateInstance(type);
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x0001D828 File Offset: 0x0001BA28
		private static Type FindClass(string name)
		{
			Type result = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				foreach (Type type in assemblies[i].GetTypesSafe(null))
				{
					if (type.Name == name)
					{
						result = type;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x0001D8A8 File Offset: 0x0001BAA8
		private static PartyNavigationModel CreateBaseNavigationModel(bool naval)
		{
			if (!naval)
			{
				return new DefaultPartyNavigationModel();
			}
			Type type = SettlementPositionScript.FindClass("NavalPartyNavigationModel");
			if (type == null)
			{
				throw new ArgumentException("Cant find naval navigation model");
			}
			return (PartyNavigationModel)Activator.CreateInstance(type, new object[] { SettlementPositionScript.CreateBaseNavigationModel(false) });
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x0001D8F8 File Offset: 0x0001BAF8
		private static MapDistanceModel CreateBaseDistanceModel(bool naval)
		{
			if (!naval)
			{
				return new DefaultMapDistanceModel();
			}
			Type type = SettlementPositionScript.FindClass("NavalDLCMapDistanceModel");
			if (type == null)
			{
				throw new ArgumentException("Cant find naval navigation model");
			}
			return (MapDistanceModel)Activator.CreateInstance(type);
		}

		// Token: 0x060003BA RID: 954 RVA: 0x0001D938 File Offset: 0x0001BB38
		private static MapDistanceModel CreateBaseDistanceModel()
		{
			return new DefaultMapDistanceModel();
		}

		// Token: 0x060003BB RID: 955 RVA: 0x0001D940 File Offset: 0x0001BB40
		private bool GetSettlementsDistanceCacheFileForCapability(string moduleId, MobileParty.NavigationType navigationType, out string filePath)
		{
			string text = ModuleHelper.GetModuleFullPath(moduleId) + "ModuleData/DistanceCaches";
			string str = navigationType.ToString();
			filePath = text + "/settlements_distance_cache_" + str + ".bin";
			bool flag = File.Exists(filePath);
			if (flag)
			{
				Debug.Print(string.Format("Found distance cache at: {0}, {1}, {2}", moduleId, text, navigationType), 0, Debug.DebugColor.White, 17592186044416UL);
			}
			return flag;
		}

		// Token: 0x060003BC RID: 956 RVA: 0x0001D9AC File Offset: 0x0001BBAC
		private List<SettlementPositionScript.SettlementRecord> LoadSettlementData(XmlDocument settlementDocument)
		{
			List<SettlementPositionScript.SettlementRecord> list = new List<SettlementPositionScript.SettlementRecord>();
			base.GameEntity.RemoveAllChildren();
			foreach (object obj in settlementDocument.DocumentElement.SelectNodes("Settlement"))
			{
				XmlNode xmlNode = (XmlNode)obj;
				string value = xmlNode.Attributes["name"].Value;
				string value2 = xmlNode.Attributes["id"].Value;
				GameEntity campaignEntityWithName = base.Scene.GetCampaignEntityWithName(value2);
				if (!(campaignEntityWithName == null))
				{
					Vec2 asVec = campaignEntityWithName.GetGlobalFrame().origin.AsVec2;
					Vec2 vec = default(Vec2);
					List<GameEntity> list2 = new List<GameEntity>();
					campaignEntityWithName.GetChildrenRecursive(ref list2);
					bool flag = false;
					bool hasPort = false;
					Vec2 portPosition = default(Vec2);
					foreach (GameEntity gameEntity in list2)
					{
						if (gameEntity.HasTag("main_map_city_gate"))
						{
							vec = gameEntity.GetGlobalFrame().origin.AsVec2;
							flag = true;
						}
						if (gameEntity.HasTag("main_map_city_port"))
						{
							portPosition = gameEntity.GetGlobalFrame().origin.AsVec2;
							hasPort = true;
						}
					}
					bool isFortification = false;
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode2 = (XmlNode)obj2;
						if (xmlNode2.Name.Equals("Components"))
						{
							using (IEnumerator enumerator4 = xmlNode2.ChildNodes.GetEnumerator())
							{
								while (enumerator4.MoveNext())
								{
									object obj3 = enumerator4.Current;
									XmlNode xmlNode3 = (XmlNode)obj3;
									if (xmlNode3.Name.Equals("Town"))
									{
										if (xmlNode3.Attributes["is_castle"] != null)
										{
											bool.Parse(xmlNode3.Attributes["is_castle"].Value);
										}
										isFortification = true;
										break;
									}
								}
								break;
							}
						}
					}
					list.Add(new SettlementPositionScript.SettlementRecord(value2, asVec, flag ? vec : asVec, xmlNode, flag, portPosition, hasPort, isFortification));
				}
			}
			return list;
		}

		// Token: 0x060003BD RID: 957 RVA: 0x0001DC84 File Offset: 0x0001BE84
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

		// Token: 0x060003BE RID: 958 RVA: 0x0001DCD0 File Offset: 0x0001BED0
		private void SaveSettlementPositions()
		{
			XmlDocument xmlDocument = this.LoadXmlFile(this.SettlementsXmlPath);
			foreach (SettlementPositionScript.SettlementRecord settlementRecord in this.LoadSettlementData(xmlDocument))
			{
				string value = settlementRecord.Node.Attributes["name"].Value;
				if (settlementRecord.Node.Attributes["posX"] == null)
				{
					XmlAttribute node = xmlDocument.CreateAttribute("posX");
					settlementRecord.Node.Attributes.Append(node);
				}
				settlementRecord.Node.Attributes["posX"].Value = settlementRecord.Position.X.ToString();
				if (settlementRecord.Node.Attributes["posY"] == null)
				{
					XmlAttribute node2 = xmlDocument.CreateAttribute("posY");
					settlementRecord.Node.Attributes.Append(node2);
				}
				settlementRecord.Node.Attributes["posY"].Value = settlementRecord.Position.Y.ToString();
				if (settlementRecord.HasGate)
				{
					if (settlementRecord.Node.Attributes["gate_posX"] == null)
					{
						XmlAttribute node3 = xmlDocument.CreateAttribute("gate_posX");
						settlementRecord.Node.Attributes.Append(node3);
					}
					settlementRecord.Node.Attributes["gate_posX"].Value = settlementRecord.GatePosition.X.ToString();
					if (settlementRecord.Node.Attributes["gate_posY"] == null)
					{
						XmlAttribute node4 = xmlDocument.CreateAttribute("gate_posY");
						settlementRecord.Node.Attributes.Append(node4);
					}
					settlementRecord.Node.Attributes["gate_posY"].Value = settlementRecord.GatePosition.Y.ToString();
				}
				if (settlementRecord.HasPort)
				{
					if (settlementRecord.Node.Attributes["port_posX"] == null)
					{
						XmlAttribute node5 = xmlDocument.CreateAttribute("port_posX");
						settlementRecord.Node.Attributes.Append(node5);
					}
					settlementRecord.Node.Attributes["port_posX"].Value = settlementRecord.PortPosition.X.ToString();
					if (settlementRecord.Node.Attributes["port_posY"] == null)
					{
						XmlAttribute node6 = xmlDocument.CreateAttribute("port_posY");
						settlementRecord.Node.Attributes.Append(node6);
					}
					settlementRecord.Node.Attributes["port_posY"].Value = settlementRecord.PortPosition.Y.ToString();
				}
			}
			xmlDocument.Save(this.SettlementsXmlPath);
		}

		// Token: 0x060003BF RID: 959 RVA: 0x0001DFF0 File Offset: 0x0001C1F0
		private void SaveSettlementDistanceCacheEditor()
		{
			bool[] regionMapping = SandBoxHelpers.MapSceneHelper.GetRegionMapping(this._partyNavigationModel);
			base.Scene.SetNavMeshRegionMap(regionMapping);
			List<MobileParty.NavigationType> list = new List<MobileParty.NavigationType> { MobileParty.NavigationType.Default };
			if (this.GetMapIsNavalDLC() || (!this.GetMapIsSandBox() && ModuleHelper.IsModuleActive("NavalDLC")))
			{
				list.Add(MobileParty.NavigationType.Naval);
				list.Add(MobileParty.NavigationType.All);
			}
			foreach (MobileParty.NavigationType navigationType in list)
			{
				int[] invalidTerrainTypesForNavigationType = this._partyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationType);
				try
				{
					XmlDocument settlementDocument = this.LoadXmlFile(this.SettlementsXmlPath);
					List<SettlementPositionScript.SettlementRecord> settlementRecords = this.LoadSettlementData(settlementDocument);
					foreach (int faceGroupId in invalidTerrainTypesForNavigationType)
					{
						base.Scene.SetAbilityOfFacesWithId(faceGroupId, false);
					}
					SettlementPositionScript.SettlementPositionScriptNavigationCache settlementPositionScriptNavigationCache = new SettlementPositionScript.SettlementPositionScriptNavigationCache(settlementRecords, base.Scene, this._mapDistanceModel, this._partyNavigationModel, navigationType);
					settlementPositionScriptNavigationCache.GenerateCacheData();
					string path;
					this.GetSettlementsDistanceCacheFileForCapability(this.GetMapModuleId(), navigationType, out path);
					settlementPositionScriptNavigationCache.Serialize(path);
				}
				catch
				{
				}
				finally
				{
					foreach (int faceGroupId2 in invalidTerrainTypesForNavigationType)
					{
						base.Scene.SetAbilityOfFacesWithId(faceGroupId2, true);
					}
				}
			}
		}

		// Token: 0x040001E5 RID: 485
		private const string SandBoxModuleId = "Sandbox";

		// Token: 0x040001E6 RID: 486
		private const string NavalDLCModuleId = "NavalDLC";

		// Token: 0x040001E7 RID: 487
		private const string NavalPartyNavigationModelName = "NavalPartyNavigationModel";

		// Token: 0x040001E8 RID: 488
		private const string NavalMapDistanceModelName = "NavalDLCMapDistanceModel";

		// Token: 0x040001E9 RID: 489
		private bool _mapIsSandBox;

		// Token: 0x040001EA RID: 490
		private bool _mapIsNavalDLC;

		// Token: 0x040001EB RID: 491
		[EditableScriptComponentVariable(true, "")]
		private string _partyNavigationModelOverriddenClassName;

		// Token: 0x040001EC RID: 492
		[EditableScriptComponentVariable(true, "")]
		private string _distanceModelOverridenClassName;

		// Token: 0x040001ED RID: 493
		private PartyNavigationModel _partyNavigationModel;

		// Token: 0x040001EE RID: 494
		private MapDistanceModel _mapDistanceModel;

		// Token: 0x040001EF RID: 495
		public SimpleButton CheckPositions;

		// Token: 0x040001F0 RID: 496
		public SimpleButton SavePositions;

		// Token: 0x040001F1 RID: 497
		public SimpleButton ComputeAndSaveSettlementDistanceCache;

		// Token: 0x020000B1 RID: 177
		private sealed class SettlementRecord : ISettlementDataHolder
		{
			// Token: 0x06000608 RID: 1544 RVA: 0x0002946C File Offset: 0x0002766C
			public SettlementRecord(string settlementId, Vec2 position, Vec2 gatePosition, XmlNode node, bool hasGate, Vec2 portPosition, bool hasPort, bool isFortification)
			{
				this.SettlementId = settlementId;
				this.Position = position;
				this.GatePosition = gatePosition;
				this.Node = node;
				this.HasGate = hasGate;
				this.PortPosition = portPosition;
				this.HasPort = hasPort;
				this.IsFortification = isFortification;
			}

			// Token: 0x170000BD RID: 189
			// (get) Token: 0x06000609 RID: 1545 RVA: 0x000294BC File Offset: 0x000276BC
			public string StringId
			{
				get
				{
					return this.SettlementId;
				}
			}

			// Token: 0x170000BE RID: 190
			// (get) Token: 0x0600060A RID: 1546 RVA: 0x000294C4 File Offset: 0x000276C4
			CampaignVec2 ISettlementDataHolder.GatePosition
			{
				get
				{
					return new CampaignVec2(this.GatePosition, true);
				}
			}

			// Token: 0x170000BF RID: 191
			// (get) Token: 0x0600060B RID: 1547 RVA: 0x000294D2 File Offset: 0x000276D2
			CampaignVec2 ISettlementDataHolder.PortPosition
			{
				get
				{
					return new CampaignVec2(this.PortPosition, false);
				}
			}

			// Token: 0x170000C0 RID: 192
			// (get) Token: 0x0600060C RID: 1548 RVA: 0x000294E0 File Offset: 0x000276E0
			bool ISettlementDataHolder.IsFortification
			{
				get
				{
					return this.IsFortification;
				}
			}

			// Token: 0x170000C1 RID: 193
			// (get) Token: 0x0600060D RID: 1549 RVA: 0x000294E8 File Offset: 0x000276E8
			bool ISettlementDataHolder.HasPort
			{
				get
				{
					return this.HasPort;
				}
			}

			// Token: 0x04000382 RID: 898
			public readonly string SettlementId;

			// Token: 0x04000383 RID: 899
			public readonly XmlNode Node;

			// Token: 0x04000384 RID: 900
			public readonly Vec2 Position;

			// Token: 0x04000385 RID: 901
			public readonly Vec2 GatePosition;

			// Token: 0x04000386 RID: 902
			public readonly bool HasGate;

			// Token: 0x04000387 RID: 903
			public readonly Vec2 PortPosition;

			// Token: 0x04000388 RID: 904
			public readonly bool HasPort;

			// Token: 0x04000389 RID: 905
			public readonly bool IsFortification;
		}

		// Token: 0x020000B2 RID: 178
		private sealed class SettlementPositionScriptNavigationCache : NavigationCache<SettlementPositionScript.SettlementRecord>
		{
			// Token: 0x0600060E RID: 1550 RVA: 0x000294F0 File Offset: 0x000276F0
			public SettlementPositionScriptNavigationCache(List<SettlementPositionScript.SettlementRecord> settlementRecords, Scene scene, MapDistanceModel mapDistanceModel, PartyNavigationModel partyNavigationModel, MobileParty.NavigationType navigationType)
				: base(navigationType)
			{
				this.Scene = scene;
				this._settlementRecords = settlementRecords;
				this._excludedFaceIds = partyNavigationModel.GetInvalidTerrainTypesForNavigationType(base._navigationType);
				this._regionSwitchCostTo0 = mapDistanceModel.RegionSwitchCostFromLandToSea;
				this._regionSwitchCostTo1 = mapDistanceModel.RegionSwitchCostFromSeaToLand;
			}

			// Token: 0x0600060F RID: 1551 RVA: 0x0002953E File Offset: 0x0002773E
			protected override NavigationCacheElement<SettlementPositionScript.SettlementRecord> GetCacheElement(SettlementPositionScript.SettlementRecord settlement, bool isPortUsed)
			{
				return new NavigationCacheElement<SettlementPositionScript.SettlementRecord>(settlement, isPortUsed);
			}

			// Token: 0x06000610 RID: 1552 RVA: 0x00029548 File Offset: 0x00027748
			protected override SettlementPositionScript.SettlementRecord GetCacheElement(string settlementId)
			{
				return this._settlementRecords.Single((SettlementPositionScript.SettlementRecord x) => x.SettlementId == settlementId);
			}

			// Token: 0x06000611 RID: 1553 RVA: 0x00029579 File Offset: 0x00027779
			public override void GetSceneXmlCrcValues(out uint sceneXmlCrc, out uint sceneNavigationMeshCrc)
			{
				sceneXmlCrc = this.Scene.GetSceneXMLCRC();
				sceneNavigationMeshCrc = this.Scene.GetNavigationMeshCRC();
			}

			// Token: 0x06000612 RID: 1554 RVA: 0x00029595 File Offset: 0x00027795
			protected override int GetNavMeshFaceCount()
			{
				return this.Scene.GetNavMeshFaceCount();
			}

			// Token: 0x06000613 RID: 1555 RVA: 0x000295A4 File Offset: 0x000277A4
			protected override Vec2 GetNavMeshFaceCenterPosition(int faceIndex)
			{
				Vec3 zero = Vec3.Zero;
				this.Scene.GetNavMeshCenterPosition(faceIndex, ref zero);
				return zero.AsVec2;
			}

			// Token: 0x06000614 RID: 1556 RVA: 0x000295CC File Offset: 0x000277CC
			protected override PathFaceRecord GetFaceRecordAtIndex(int faceIndex)
			{
				return this.Scene.GetNavMeshPathFaceRecord(faceIndex);
			}

			// Token: 0x06000615 RID: 1557 RVA: 0x000295DA File Offset: 0x000277DA
			protected override int[] GetExcludedFaceIds()
			{
				return this._excludedFaceIds;
			}

			// Token: 0x06000616 RID: 1558 RVA: 0x000295E2 File Offset: 0x000277E2
			protected override int GetRegionSwitchCostTo0()
			{
				return this._regionSwitchCostTo0;
			}

			// Token: 0x06000617 RID: 1559 RVA: 0x000295EA File Offset: 0x000277EA
			protected override int GetRegionSwitchCostTo1()
			{
				return this._regionSwitchCostTo1;
			}

			// Token: 0x06000618 RID: 1560 RVA: 0x000295F4 File Offset: 0x000277F4
			protected override IEnumerable<SettlementPositionScript.SettlementRecord> GetClosestSettlementsToPositionInCache(Vec2 checkPosition, List<SettlementPositionScript.SettlementRecord> settlements)
			{
				if (base._navigationType == MobileParty.NavigationType.Naval)
				{
					return from x in settlements
						where x.HasPort
						orderby checkPosition.DistanceSquared(x.PortPosition)
						select x;
				}
				if (base._navigationType == MobileParty.NavigationType.Default)
				{
					return from x in settlements
						orderby checkPosition.DistanceSquared(x.GatePosition)
						select x;
				}
				return settlements.OrderBy(delegate(SettlementPositionScript.SettlementRecord x)
				{
					if (!x.HasPort)
					{
						return checkPosition.DistanceSquared(x.GatePosition);
					}
					return MathF.Min(checkPosition.DistanceSquared(x.GatePosition), checkPosition.DistanceSquared(x.PortPosition));
				});
			}

			// Token: 0x06000619 RID: 1561 RVA: 0x0002967C File Offset: 0x0002787C
			protected override float GetRealPathDistanceFromPositionToSettlement(Vec2 checkPosition, PathFaceRecord currentFaceRecord, float maxDistanceToLookForPathDetection, SettlementPositionScript.SettlementRecord currentSettlementToLook, out bool isPort)
			{
				float result = float.MaxValue;
				isPort = false;
				PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
				switch (base._navigationType)
				{
				case MobileParty.NavigationType.Default:
				{
					this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, currentSettlementToLook.GatePosition, true, false, true);
					float num;
					if (this.Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, nullFaceRecord.FaceIndex, checkPosition, currentSettlementToLook.GatePosition, 0.3f, maxDistanceToLookForPathDetection, out num, this._excludedFaceIds, this._regionSwitchCostTo0, this._regionSwitchCostTo1))
					{
						result = num;
					}
					break;
				}
				case MobileParty.NavigationType.Naval:
				{
					this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, currentSettlementToLook.PortPosition, false, false, true);
					float num2;
					if (this.Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, nullFaceRecord.FaceIndex, checkPosition, currentSettlementToLook.PortPosition, 0.3f, maxDistanceToLookForPathDetection, out num2, this._excludedFaceIds, this._regionSwitchCostTo0, this._regionSwitchCostTo1))
					{
						result = num2;
						isPort = true;
					}
					break;
				}
				case MobileParty.NavigationType.All:
				{
					this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, currentSettlementToLook.GatePosition, true, false, true);
					float num3;
					if (this.Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, nullFaceRecord.FaceIndex, checkPosition, currentSettlementToLook.GatePosition, 0.3f, maxDistanceToLookForPathDetection, out num3, this._excludedFaceIds, this._regionSwitchCostTo0, this._regionSwitchCostTo1))
					{
						result = num3;
					}
					if (currentSettlementToLook.HasPort)
					{
						this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, currentSettlementToLook.PortPosition, false, false, true);
						float num4;
						if (this.Scene.GetPathDistanceBetweenAIFaces(currentFaceRecord.FaceIndex, nullFaceRecord.FaceIndex, checkPosition, currentSettlementToLook.PortPosition, 0.3f, maxDistanceToLookForPathDetection, out num4, this._excludedFaceIds, this._regionSwitchCostTo0, this._regionSwitchCostTo1) && num4 < num3)
						{
							result = num4;
							isPort = true;
						}
					}
					break;
				}
				}
				return result;
			}

			// Token: 0x0600061A RID: 1562 RVA: 0x00029834 File Offset: 0x00027A34
			protected override float GetRealDistanceAndLandRatioBetweenSettlements(NavigationCacheElement<SettlementPositionScript.SettlementRecord> settlement1, NavigationCacheElement<SettlementPositionScript.SettlementRecord> settlement2, out float landRatio)
			{
				Vec2 vec = (settlement1.IsPortUsed ? settlement1.PortPosition.ToVec2() : settlement1.GatePosition.ToVec2());
				Vec2 vec2 = (settlement2.IsPortUsed ? settlement2.PortPosition.ToVec2() : settlement2.GatePosition.ToVec2());
				PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
				this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, vec, !settlement1.IsPortUsed, false, true);
				PathFaceRecord nullFaceRecord2 = PathFaceRecord.NullFaceRecord;
				this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord2, vec2, !settlement2.IsPortUsed, false, true);
				landRatio = 1f;
				if (base._navigationType == MobileParty.NavigationType.Naval)
				{
					landRatio = 0f;
				}
				else if (base._navigationType == MobileParty.NavigationType.All)
				{
					NavigationPath path = new NavigationPath();
					this.Scene.GetPathBetweenAIFaces(nullFaceRecord.FaceIndex, nullFaceRecord2.FaceIndex, vec, vec2, 0.3f, path, this._excludedFaceIds, 1f, this._regionSwitchCostTo0, this._regionSwitchCostTo1);
					landRatio = base.GetLandRatioOfPath(path, vec);
				}
				float result;
				this.Scene.GetPathDistanceBetweenAIFaces(nullFaceRecord.FaceIndex, nullFaceRecord2.FaceIndex, vec, vec2, 0.3f, float.PositiveInfinity, out result, this._excludedFaceIds, this._regionSwitchCostTo0, this._regionSwitchCostTo1);
				return result;
			}

			// Token: 0x0600061B RID: 1563 RVA: 0x0002997C File Offset: 0x00027B7C
			protected override void GetFaceRecordForPoint(Vec2 position, out bool isOnRegion1)
			{
				isOnRegion1 = true;
				PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
				this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, position, isOnRegion1, false, true);
				if (!nullFaceRecord.IsValid())
				{
					isOnRegion1 = false;
					this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, position, isOnRegion1, false, true);
				}
				if (!nullFaceRecord.IsValid())
				{
					Debug.Print(string.Format("{0} has no region data.", position), 0, Debug.DebugColor.Red, 17592186044416UL);
				}
			}

			// Token: 0x0600061C RID: 1564 RVA: 0x000299EC File Offset: 0x00027BEC
			protected override bool CheckBeingNeighbor(List<SettlementPositionScript.SettlementRecord> settlementsToConsider, SettlementPositionScript.SettlementRecord settlement1, SettlementPositionScript.SettlementRecord settlement2, bool useGate1, bool useGate2, out float distance)
			{
				Vec2 vec = (useGate1 ? settlement1.GatePosition : settlement1.PortPosition);
				Vec2 vec2 = (useGate2 ? settlement2.GatePosition : settlement2.PortPosition);
				PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
				this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord, vec, useGate1, false, true);
				PathFaceRecord nullFaceRecord2 = PathFaceRecord.NullFaceRecord;
				this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord2, vec2, useGate2, false, true);
				if (!nullFaceRecord.IsValid() || !nullFaceRecord2.IsValid())
				{
					Debug.FailedAssert("Settlement navFace index should not be -1, check here", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\SettlementPositionScript.cs", "CheckBeingNeighbor", 392);
				}
				NavigationPath navigationPath = new NavigationPath();
				float num = (((float)(this._regionSwitchCostTo0 + this._regionSwitchCostTo1) > 0f) ? 2f : 0f);
				if (num > 0f)
				{
					this.Scene.GetPathBetweenAIFaces(nullFaceRecord.FaceIndex, nullFaceRecord2.FaceIndex, vec, vec2, 0.3f, navigationPath, this._excludedFaceIds, num, this._regionSwitchCostTo0, this._regionSwitchCostTo1);
				}
				else
				{
					this.Scene.GetPathBetweenAIFaces(nullFaceRecord.FaceIndex, nullFaceRecord2.FaceIndex, vec, vec2, 0.3f, navigationPath, this._excludedFaceIds, 0f);
				}
				bool flag = navigationPath.Size > 0 || nullFaceRecord.FaceIndex == nullFaceRecord2.FaceIndex;
				bool flag2 = useGate1;
				if (!this.Scene.GetPathDistanceBetweenAIFaces(nullFaceRecord.FaceIndex, nullFaceRecord2.FaceIndex, vec, vec2, 0.3f, 1784684f, out distance, this.GetExcludedFaceIds(), this._regionSwitchCostTo0, this._regionSwitchCostTo1))
				{
					distance = 1784684f;
				}
				int num2 = 0;
				while (num2 < navigationPath.Size && flag)
				{
					Vec2 v = navigationPath[num2] - ((num2 == 0) ? vec : navigationPath[num2 - 1]);
					float num3 = v.Length / 1f;
					v.Normalize();
					int num4 = 0;
					while ((float)num4 < num3)
					{
						Vec2 vec3 = ((num2 == 0) ? vec : navigationPath[num2 - 1]) + v * 1f * (float)num4;
						if (vec3 != vec && vec3 != vec2)
						{
							PathFaceRecord nullFaceRecord3 = PathFaceRecord.NullFaceRecord;
							this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord3, vec3, flag2, false, true);
							if (nullFaceRecord3.FaceIndex == -1)
							{
								flag2 = !flag2;
								this.Scene.GetNavMeshFaceIndex(ref nullFaceRecord3, vec3, flag2, false, true);
							}
							bool flag3;
							float realPathDistanceFromPositionToSettlement = this.GetRealPathDistanceFromPositionToSettlement(vec3, nullFaceRecord3, distance, settlement1, out flag3);
							float realPathDistanceFromPositionToSettlement2 = this.GetRealPathDistanceFromPositionToSettlement(vec3, nullFaceRecord3, distance, settlement2, out flag3);
							float num5 = ((realPathDistanceFromPositionToSettlement < realPathDistanceFromPositionToSettlement2) ? realPathDistanceFromPositionToSettlement : realPathDistanceFromPositionToSettlement2);
							if (nullFaceRecord3.FaceIndex != -1)
							{
								SettlementPositionScript.SettlementRecord closestSettlementToPosition = base.GetClosestSettlementToPosition(vec3, nullFaceRecord3, this._excludedFaceIds, settlementsToConsider, this._regionSwitchCostTo0, this._regionSwitchCostTo1, num5 * 0.8f, out flag3);
								if (closestSettlementToPosition != null && closestSettlementToPosition != settlement1 && closestSettlementToPosition != settlement2)
								{
									flag = false;
									break;
								}
							}
						}
						num4++;
					}
					num2++;
				}
				return flag;
			}

			// Token: 0x0600061D RID: 1565 RVA: 0x00029CE3 File Offset: 0x00027EE3
			protected override List<SettlementPositionScript.SettlementRecord> GetAllRegisteredSettlements()
			{
				return this._settlementRecords;
			}

			// Token: 0x0400038A RID: 906
			private readonly Scene Scene;

			// Token: 0x0400038B RID: 907
			private readonly List<SettlementPositionScript.SettlementRecord> _settlementRecords;

			// Token: 0x0400038C RID: 908
			private readonly int[] _excludedFaceIds;

			// Token: 0x0400038D RID: 909
			private readonly int _regionSwitchCostTo0;

			// Token: 0x0400038E RID: 910
			private readonly int _regionSwitchCostTo1;
		}
	}
}
