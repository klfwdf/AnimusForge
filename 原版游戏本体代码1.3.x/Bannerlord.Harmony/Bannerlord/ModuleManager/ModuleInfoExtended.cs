using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200001A RID: 26
	[NullableContext(1)]
	[Nullable(0)]
	internal class ModuleInfoExtended : IEquatable<ModuleInfoExtended>
	{
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000136 RID: 310 RVA: 0x0000650D File Offset: 0x0000470D
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleInfoExtended);
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000137 RID: 311 RVA: 0x00006519 File Offset: 0x00004719
		// (set) Token: 0x06000138 RID: 312 RVA: 0x00006521 File Offset: 0x00004721
		public string Id { get; set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000139 RID: 313 RVA: 0x0000652A File Offset: 0x0000472A
		// (set) Token: 0x0600013A RID: 314 RVA: 0x00006532 File Offset: 0x00004732
		public string Name { get; set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x0600013B RID: 315 RVA: 0x0000653B File Offset: 0x0000473B
		// (set) Token: 0x0600013C RID: 316 RVA: 0x00006543 File Offset: 0x00004743
		public bool IsOfficial { get; set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600013D RID: 317 RVA: 0x0000654C File Offset: 0x0000474C
		// (set) Token: 0x0600013E RID: 318 RVA: 0x00006554 File Offset: 0x00004754
		public ApplicationVersion Version { get; set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600013F RID: 319 RVA: 0x0000655D File Offset: 0x0000475D
		// (set) Token: 0x06000140 RID: 320 RVA: 0x00006565 File Offset: 0x00004765
		public bool IsSingleplayerModule { get; set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000141 RID: 321 RVA: 0x0000656E File Offset: 0x0000476E
		// (set) Token: 0x06000142 RID: 322 RVA: 0x00006576 File Offset: 0x00004776
		public bool IsMultiplayerModule { get; set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000143 RID: 323 RVA: 0x0000657F File Offset: 0x0000477F
		// (set) Token: 0x06000144 RID: 324 RVA: 0x00006587 File Offset: 0x00004787
		public bool IsServerModule { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000145 RID: 325 RVA: 0x00006590 File Offset: 0x00004790
		// (set) Token: 0x06000146 RID: 326 RVA: 0x00006598 File Offset: 0x00004798
		public IReadOnlyList<SubModuleInfoExtended> SubModules { get; set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000147 RID: 327 RVA: 0x000065A1 File Offset: 0x000047A1
		// (set) Token: 0x06000148 RID: 328 RVA: 0x000065A9 File Offset: 0x000047A9
		public IReadOnlyList<DependentModule> DependentModules { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000149 RID: 329 RVA: 0x000065B2 File Offset: 0x000047B2
		// (set) Token: 0x0600014A RID: 330 RVA: 0x000065BA File Offset: 0x000047BA
		public IReadOnlyList<DependentModule> ModulesToLoadAfterThis { get; set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x0600014B RID: 331 RVA: 0x000065C3 File Offset: 0x000047C3
		// (set) Token: 0x0600014C RID: 332 RVA: 0x000065CB File Offset: 0x000047CB
		public IReadOnlyList<DependentModule> IncompatibleModules { get; set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x0600014D RID: 333 RVA: 0x000065D4 File Offset: 0x000047D4
		// (set) Token: 0x0600014E RID: 334 RVA: 0x000065DC File Offset: 0x000047DC
		public string Url { get; set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600014F RID: 335 RVA: 0x000065E5 File Offset: 0x000047E5
		// (set) Token: 0x06000150 RID: 336 RVA: 0x000065ED File Offset: 0x000047ED
		public string UpdateInfo { get; set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000151 RID: 337 RVA: 0x000065F6 File Offset: 0x000047F6
		// (set) Token: 0x06000152 RID: 338 RVA: 0x000065FE File Offset: 0x000047FE
		public IReadOnlyList<DependentModuleMetadata> DependentModuleMetadatas { get; set; }

		// Token: 0x06000153 RID: 339 RVA: 0x00006608 File Offset: 0x00004808
		[NullableContext(2)]
		public static ModuleInfoExtended FromXml(XmlDocument xmlDocument)
		{
			bool flag = xmlDocument == null;
			ModuleInfoExtended result;
			if (flag)
			{
				result = null;
			}
			else
			{
				XmlNode moduleNode = xmlDocument.SelectSingleNode("Module");
				string text;
				if (moduleNode == null)
				{
					text = null;
				}
				else
				{
					XmlNode xmlNode = moduleNode.SelectSingleNode("Id");
					if (xmlNode == null)
					{
						text = null;
					}
					else
					{
						XmlAttributeCollection attributes = xmlNode.Attributes;
						if (attributes == null)
						{
							text = null;
						}
						else
						{
							XmlAttribute xmlAttribute = attributes["value"];
							text = ((xmlAttribute != null) ? xmlAttribute.InnerText : null);
						}
					}
				}
				string id = text ?? string.Empty;
				string text2;
				if (moduleNode == null)
				{
					text2 = null;
				}
				else
				{
					XmlNode xmlNode2 = moduleNode.SelectSingleNode("Name");
					if (xmlNode2 == null)
					{
						text2 = null;
					}
					else
					{
						XmlAttributeCollection attributes2 = xmlNode2.Attributes;
						if (attributes2 == null)
						{
							text2 = null;
						}
						else
						{
							XmlAttribute xmlAttribute2 = attributes2["value"];
							text2 = ((xmlAttribute2 != null) ? xmlAttribute2.InnerText : null);
						}
					}
				}
				string name = text2 ?? string.Empty;
				string versionAsString;
				if (moduleNode == null)
				{
					versionAsString = null;
				}
				else
				{
					XmlNode xmlNode3 = moduleNode.SelectSingleNode("Version");
					if (xmlNode3 == null)
					{
						versionAsString = null;
					}
					else
					{
						XmlAttributeCollection attributes3 = xmlNode3.Attributes;
						if (attributes3 == null)
						{
							versionAsString = null;
						}
						else
						{
							XmlAttribute xmlAttribute3 = attributes3["value"];
							versionAsString = ((xmlAttribute3 != null) ? xmlAttribute3.InnerText : null);
						}
					}
				}
				ApplicationVersion version;
				ApplicationVersion.TryParse(versionAsString, out version);
				string text3;
				if (moduleNode == null)
				{
					text3 = null;
				}
				else
				{
					XmlNode xmlNode4 = moduleNode.SelectSingleNode("ModuleType");
					if (xmlNode4 == null)
					{
						text3 = null;
					}
					else
					{
						XmlAttributeCollection attributes4 = xmlNode4.Attributes;
						text3 = ((attributes4 != null) ? attributes4["value"].InnerText : null);
					}
				}
				string moduleType = text3;
				if (moduleNode != null)
				{
					XmlNode xmlNode5 = moduleNode.SelectSingleNode("Official");
					bool? flag2;
					if (xmlNode5 == null)
					{
						flag2 = null;
					}
					else
					{
						XmlAttributeCollection attributes5 = xmlNode5.Attributes;
						if (attributes5 == null)
						{
							flag2 = null;
						}
						else
						{
							XmlAttribute xmlAttribute4 = attributes5["value"];
							flag2 = ((xmlAttribute4 != null) ? new bool?(xmlAttribute4.InnerText.Equals("true")) : null);
						}
					}
					bool? flag3 = flag2;
					if (flag3.GetValueOrDefault())
					{
						goto IL_1A7;
					}
				}
				bool flag4;
				if (!(moduleType == "Official"))
				{
					flag4 = moduleType == "OfficialOptional";
					goto IL_1A8;
				}
				IL_1A7:
				flag4 = true;
				IL_1A8:
				bool isOfficial = flag4;
				string text4;
				if (moduleNode == null)
				{
					text4 = null;
				}
				else
				{
					XmlNode xmlNode6 = moduleNode.SelectSingleNode("ModuleCategory");
					if (xmlNode6 == null)
					{
						text4 = null;
					}
					else
					{
						XmlAttributeCollection attributes6 = xmlNode6.Attributes;
						if (attributes6 == null)
						{
							text4 = null;
						}
						else
						{
							XmlAttribute xmlAttribute5 = attributes6["value"];
							text4 = ((xmlAttribute5 != null) ? xmlAttribute5.InnerText : null);
						}
					}
				}
				string moduleCategory = text4;
				if (moduleNode != null)
				{
					XmlNode xmlNode7 = moduleNode.SelectSingleNode("SingleplayerModule");
					bool? flag5;
					if (xmlNode7 == null)
					{
						flag5 = null;
					}
					else
					{
						XmlAttributeCollection attributes7 = xmlNode7.Attributes;
						if (attributes7 == null)
						{
							flag5 = null;
						}
						else
						{
							XmlAttribute xmlAttribute6 = attributes7["value"];
							flag5 = ((xmlAttribute6 != null) ? new bool?(xmlAttribute6.InnerText.Equals("true")) : null);
						}
					}
					bool? flag3 = flag5;
					if (flag3.GetValueOrDefault())
					{
						goto IL_26E;
					}
				}
				bool flag6;
				if (!(moduleCategory == "Singleplayer"))
				{
					flag6 = moduleCategory == "SingleplayerOptional";
					goto IL_26F;
				}
				IL_26E:
				flag6 = true;
				IL_26F:
				bool isSingleplayerModule = flag6;
				if (moduleNode != null)
				{
					XmlNode xmlNode8 = moduleNode.SelectSingleNode("MultiplayerModule");
					bool? flag7;
					if (xmlNode8 == null)
					{
						flag7 = null;
					}
					else
					{
						XmlAttributeCollection attributes8 = xmlNode8.Attributes;
						if (attributes8 == null)
						{
							flag7 = null;
						}
						else
						{
							XmlAttribute xmlAttribute7 = attributes8["value"];
							flag7 = ((xmlAttribute7 != null) ? new bool?(xmlAttribute7.InnerText.Equals("true")) : null);
						}
					}
					bool? flag3 = flag7;
					if (flag3.GetValueOrDefault())
					{
						goto IL_2F9;
					}
				}
				bool flag8;
				if (!(moduleCategory == "Multiplayer"))
				{
					flag8 = moduleCategory == "MultiplayerOptional";
					goto IL_2FA;
				}
				IL_2F9:
				flag8 = true;
				IL_2FA:
				bool isMultiplayerModule = flag8;
				bool isServerModule = moduleCategory == "Server" || moduleCategory == "ServerOptional";
				XmlNode dependentModulesNode = ((moduleNode != null) ? moduleNode.SelectSingleNode("DependedModules") : null);
				XmlNodeList dependentModulesList = ((dependentModulesNode != null) ? dependentModulesNode.SelectNodes("DependedModule") : null);
				List<DependentModule> dependentModules = new List<DependentModule>((dependentModulesList != null) ? dependentModulesList.Count : 0);
				int i = 0;
				for (;;)
				{
					int num = i;
					int? num2 = ((dependentModulesList != null) ? new int?(dependentModulesList.Count) : null);
					if (!((num < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					XmlAttribute xmlAttribute8;
					if (dependentModulesList == null)
					{
						xmlAttribute8 = null;
					}
					else
					{
						XmlNode xmlNode9 = dependentModulesList[i];
						if (xmlNode9 == null)
						{
							xmlAttribute8 = null;
						}
						else
						{
							XmlAttributeCollection attributes9 = xmlNode9.Attributes;
							xmlAttribute8 = ((attributes9 != null) ? attributes9["Id"] : null);
						}
					}
					XmlAttribute idAttr = xmlAttribute8;
					bool flag9 = idAttr != null;
					if (flag9)
					{
						XmlNode xmlNode10 = dependentModulesList[i];
						string versionAsString2;
						if (xmlNode10 == null)
						{
							versionAsString2 = null;
						}
						else
						{
							XmlAttributeCollection attributes10 = xmlNode10.Attributes;
							if (attributes10 == null)
							{
								versionAsString2 = null;
							}
							else
							{
								XmlAttribute xmlAttribute9 = attributes10["DependentVersion"];
								versionAsString2 = ((xmlAttribute9 != null) ? xmlAttribute9.InnerText : null);
							}
						}
						ApplicationVersion dVersion;
						ApplicationVersion.TryParse(versionAsString2, out dVersion);
						XmlNode xmlNode11 = dependentModulesList[i];
						XmlAttribute xmlAttribute10;
						if (xmlNode11 == null)
						{
							xmlAttribute10 = null;
						}
						else
						{
							XmlAttributeCollection attributes11 = xmlNode11.Attributes;
							xmlAttribute10 = ((attributes11 != null) ? attributes11["Optional"] : null);
						}
						XmlAttribute optional = xmlAttribute10;
						bool isOptional = optional != null && optional.InnerText.Equals("true");
						dependentModules.Add(new DependentModule(idAttr.InnerText, dVersion, isOptional));
					}
					i++;
				}
				XmlNode modulesToLoadAfterThisNode = ((moduleNode != null) ? moduleNode.SelectSingleNode("ModulesToLoadAfterThis") : null);
				XmlNodeList modulesToLoadAfterThisList = ((modulesToLoadAfterThisNode != null) ? modulesToLoadAfterThisNode.SelectNodes("Module") : null);
				List<DependentModule> modulesToLoadAfterThis = new List<DependentModule>((modulesToLoadAfterThisList != null) ? modulesToLoadAfterThisList.Count : 0);
				int j = 0;
				for (;;)
				{
					int num3 = j;
					int? num2 = ((modulesToLoadAfterThisList != null) ? new int?(modulesToLoadAfterThisList.Count) : null);
					if (!((num3 < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					XmlAttribute xmlAttribute11;
					if (modulesToLoadAfterThisList == null)
					{
						xmlAttribute11 = null;
					}
					else
					{
						XmlNode xmlNode12 = modulesToLoadAfterThisList[j];
						if (xmlNode12 == null)
						{
							xmlAttribute11 = null;
						}
						else
						{
							XmlAttributeCollection attributes12 = xmlNode12.Attributes;
							xmlAttribute11 = ((attributes12 != null) ? attributes12["Id"] : null);
						}
					}
					XmlAttribute idAttr2 = xmlAttribute11;
					bool flag10 = idAttr2 != null;
					if (flag10)
					{
						modulesToLoadAfterThis.Add(new DependentModule
						{
							Id = idAttr2.InnerText,
							IsOptional = true
						});
					}
					j++;
				}
				XmlNode incompatibleModulesNode = ((moduleNode != null) ? moduleNode.SelectSingleNode("IncompatibleModules") : null);
				XmlNodeList incompatibleModulesList = ((incompatibleModulesNode != null) ? incompatibleModulesNode.SelectNodes("Module") : null);
				List<DependentModule> incompatibleModules = new List<DependentModule>((incompatibleModulesList != null) ? incompatibleModulesList.Count : 0);
				int k = 0;
				for (;;)
				{
					int num4 = k;
					int? num2 = ((incompatibleModulesList != null) ? new int?(incompatibleModulesList.Count) : null);
					if (!((num4 < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					XmlAttribute xmlAttribute12;
					if (incompatibleModulesList == null)
					{
						xmlAttribute12 = null;
					}
					else
					{
						XmlNode xmlNode13 = incompatibleModulesList[k];
						if (xmlNode13 == null)
						{
							xmlAttribute12 = null;
						}
						else
						{
							XmlAttributeCollection attributes13 = xmlNode13.Attributes;
							xmlAttribute12 = ((attributes13 != null) ? attributes13["Id"] : null);
						}
					}
					XmlAttribute idAttr3 = xmlAttribute12;
					bool flag11 = idAttr3 != null;
					if (flag11)
					{
						incompatibleModules.Add(new DependentModule
						{
							Id = idAttr3.InnerText,
							IsOptional = true
						});
					}
					k++;
				}
				XmlNode subModulesNode = ((moduleNode != null) ? moduleNode.SelectSingleNode("SubModules") : null);
				XmlNodeList subModuleList = ((subModulesNode != null) ? subModulesNode.SelectNodes("SubModule") : null);
				List<SubModuleInfoExtended> subModules = new List<SubModuleInfoExtended>((subModuleList != null) ? subModuleList.Count : 0);
				int l = 0;
				for (;;)
				{
					int num5 = l;
					int? num2 = ((subModuleList != null) ? new int?(subModuleList.Count) : null);
					if (!((num5 < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					SubModuleInfoExtended subModule = SubModuleInfoExtended.FromXml((subModuleList != null) ? subModuleList[l] : null);
					bool flag12 = subModule != null;
					if (flag12)
					{
						subModules.Add(subModule);
					}
					l++;
				}
				string text5;
				if (moduleNode == null)
				{
					text5 = null;
				}
				else
				{
					XmlNode xmlNode14 = moduleNode.SelectSingleNode("Url");
					if (xmlNode14 == null)
					{
						text5 = null;
					}
					else
					{
						XmlAttributeCollection attributes14 = xmlNode14.Attributes;
						if (attributes14 == null)
						{
							text5 = null;
						}
						else
						{
							XmlAttribute xmlAttribute13 = attributes14["value"];
							text5 = ((xmlAttribute13 != null) ? xmlAttribute13.InnerText : null);
						}
					}
				}
				string url = text5 ?? string.Empty;
				string text6;
				if (moduleNode == null)
				{
					text6 = null;
				}
				else
				{
					XmlNode xmlNode15 = moduleNode.SelectSingleNode("UpdateInfo");
					if (xmlNode15 == null)
					{
						text6 = null;
					}
					else
					{
						XmlAttributeCollection attributes15 = xmlNode15.Attributes;
						if (attributes15 == null)
						{
							text6 = null;
						}
						else
						{
							XmlAttribute xmlAttribute14 = attributes15["value"];
							text6 = ((xmlAttribute14 != null) ? xmlAttribute14.InnerText : null);
						}
					}
				}
				string updateInfo = text6 ?? string.Empty;
				XmlNode dependentModuleMetadatasNode = ((moduleNode != null) ? moduleNode.SelectSingleNode("DependedModuleMetadatas") : null);
				XmlNodeList dependentModuleMetadatasList = ((dependentModuleMetadatasNode != null) ? dependentModuleMetadatasNode.SelectNodes("DependedModuleMetadata") : null);
				XmlNode loadAfterModules = ((moduleNode != null) ? moduleNode.SelectSingleNode("LoadAfterModules") : null);
				XmlNodeList loadAfterModuleList = ((loadAfterModules != null) ? loadAfterModules.SelectNodes("LoadAfterModule") : null);
				XmlNode optionalDependentModules = ((moduleNode != null) ? moduleNode.SelectSingleNode("OptionalDependModules") : null);
				IEnumerable<XmlNode> enumerable;
				if (dependentModulesNode == null)
				{
					enumerable = null;
				}
				else
				{
					XmlNodeList xmlNodeList = dependentModulesNode.SelectNodes("OptionalDependModule");
					enumerable = ((xmlNodeList != null) ? xmlNodeList.Cast<XmlNode>() : null);
				}
				IEnumerable<XmlNode> first = enumerable ?? Enumerable.Empty<XmlNode>();
				IEnumerable<XmlNode> enumerable2;
				if (optionalDependentModules == null)
				{
					enumerable2 = null;
				}
				else
				{
					XmlNodeList xmlNodeList2 = optionalDependentModules.SelectNodes("OptionalDependModule");
					enumerable2 = ((xmlNodeList2 != null) ? xmlNodeList2.Cast<XmlNode>() : null);
				}
				IEnumerable<XmlNode> first2 = first.Concat(enumerable2 ?? Enumerable.Empty<XmlNode>());
				IEnumerable<XmlNode> enumerable3;
				if (optionalDependentModules == null)
				{
					enumerable3 = null;
				}
				else
				{
					XmlNodeList xmlNodeList3 = optionalDependentModules.SelectNodes("DependModule");
					enumerable3 = ((xmlNodeList3 != null) ? xmlNodeList3.Cast<XmlNode>() : null);
				}
				List<XmlNode> optionalDependentModuleList = first2.Concat(enumerable3 ?? Enumerable.Empty<XmlNode>()).ToList<XmlNode>();
				int capacity;
				if (dependentModuleMetadatasList == null)
				{
					int? num6 = ((loadAfterModuleList != null) ? new int?(loadAfterModuleList.Count) : null);
					capacity = ((num6 != null) ? new int?(num6.GetValueOrDefault()) : null) ?? optionalDependentModuleList.Count;
				}
				else
				{
					capacity = dependentModuleMetadatasList.Count;
				}
				List<DependentModuleMetadata> dependentModuleMetadatas = new List<DependentModuleMetadata>(capacity);
				int m = 0;
				for (;;)
				{
					int num7 = m;
					int? num2 = ((dependentModuleMetadatasList != null) ? new int?(dependentModuleMetadatasList.Count) : null);
					if (!((num7 < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					XmlAttribute xmlAttribute15;
					if (dependentModuleMetadatasList == null)
					{
						xmlAttribute15 = null;
					}
					else
					{
						XmlNode xmlNode16 = dependentModuleMetadatasList[m];
						if (xmlNode16 == null)
						{
							xmlAttribute15 = null;
						}
						else
						{
							XmlAttributeCollection attributes16 = xmlNode16.Attributes;
							xmlAttribute15 = ((attributes16 != null) ? attributes16["id"] : null);
						}
					}
					XmlAttribute idAttr4 = xmlAttribute15;
					bool flag13 = idAttr4 != null;
					if (flag13)
					{
						XmlNode xmlNode17 = dependentModuleMetadatasList[m];
						XmlAttribute xmlAttribute16;
						if (xmlNode17 == null)
						{
							xmlAttribute16 = null;
						}
						else
						{
							XmlAttributeCollection attributes17 = xmlNode17.Attributes;
							xmlAttribute16 = ((attributes17 != null) ? attributes17["order"] : null);
						}
						XmlAttribute orderAttr = xmlAttribute16;
						LoadTypeParse loadType;
						LoadType order = (LoadType)((orderAttr != null && Enum.TryParse<LoadTypeParse>(orderAttr.InnerText, out loadType)) ? loadType : ((LoadTypeParse)0));
						XmlNode xmlNode18 = dependentModuleMetadatasList[m];
						bool? flag14;
						if (xmlNode18 == null)
						{
							flag14 = null;
						}
						else
						{
							XmlAttributeCollection attributes18 = xmlNode18.Attributes;
							if (attributes18 == null)
							{
								flag14 = null;
							}
							else
							{
								XmlAttribute xmlAttribute17 = attributes18["optional"];
								flag14 = ((xmlAttribute17 != null) ? new bool?(xmlAttribute17.InnerText.Equals("true")) : null);
							}
						}
						bool? flag3 = flag14;
						bool optional2 = flag3.GetValueOrDefault();
						XmlNode xmlNode19 = dependentModuleMetadatasList[m];
						bool? flag15;
						if (xmlNode19 == null)
						{
							flag15 = null;
						}
						else
						{
							XmlAttributeCollection attributes19 = xmlNode19.Attributes;
							if (attributes19 == null)
							{
								flag15 = null;
							}
							else
							{
								XmlAttribute xmlAttribute18 = attributes19["incompatible"];
								flag15 = ((xmlAttribute18 != null) ? new bool?(xmlAttribute18.InnerText.Equals("true")) : null);
							}
						}
						flag3 = flag15;
						bool incompatible = flag3.GetValueOrDefault();
						XmlNode xmlNode20 = dependentModuleMetadatasList[m];
						string versionAsString3;
						if (xmlNode20 == null)
						{
							versionAsString3 = null;
						}
						else
						{
							XmlAttributeCollection attributes20 = xmlNode20.Attributes;
							if (attributes20 == null)
							{
								versionAsString3 = null;
							}
							else
							{
								XmlAttribute xmlAttribute19 = attributes20["version"];
								versionAsString3 = ((xmlAttribute19 != null) ? xmlAttribute19.InnerText : null);
							}
						}
						ApplicationVersion v;
						ApplicationVersion dVersion2 = (ApplicationVersion.TryParse(versionAsString3, out v) ? v : ApplicationVersion.Empty);
						XmlNode xmlNode21 = dependentModuleMetadatasList[m];
						string text7;
						if (xmlNode21 == null)
						{
							text7 = null;
						}
						else
						{
							XmlAttributeCollection attributes21 = xmlNode21.Attributes;
							if (attributes21 == null)
							{
								text7 = null;
							}
							else
							{
								XmlAttribute xmlAttribute20 = attributes21["version"];
								text7 = ((xmlAttribute20 != null) ? xmlAttribute20.InnerText : null);
							}
						}
						ApplicationVersionRange vr;
						ApplicationVersionRange dVersionRange = (ApplicationVersionRange.TryParse(text7 ?? string.Empty, out vr) ? vr : ApplicationVersionRange.Empty);
						dependentModuleMetadatas.Add(new DependentModuleMetadata
						{
							Id = idAttr4.InnerText,
							LoadType = order,
							IsOptional = optional2,
							IsIncompatible = incompatible,
							Version = dVersion2,
							VersionRange = dVersionRange
						});
					}
					m++;
				}
				int n = 0;
				for (;;)
				{
					int num8 = n;
					int? num2 = ((loadAfterModuleList != null) ? new int?(loadAfterModuleList.Count) : null);
					if (!((num8 < num2.GetValueOrDefault()) & (num2 != null)))
					{
						break;
					}
					XmlAttribute xmlAttribute21;
					if (loadAfterModuleList == null)
					{
						xmlAttribute21 = null;
					}
					else
					{
						XmlNode xmlNode22 = loadAfterModuleList[n];
						if (xmlNode22 == null)
						{
							xmlAttribute21 = null;
						}
						else
						{
							XmlAttributeCollection attributes22 = xmlNode22.Attributes;
							xmlAttribute21 = ((attributes22 != null) ? attributes22["Id"] : null);
						}
					}
					XmlAttribute idAttr5 = xmlAttribute21;
					bool flag16 = idAttr5 != null;
					if (flag16)
					{
						dependentModuleMetadatas.Add(new DependentModuleMetadata
						{
							Id = idAttr5.InnerText,
							LoadType = LoadType.LoadAfterThis,
							IsOptional = false,
							IsIncompatible = false,
							Version = ApplicationVersion.Empty,
							VersionRange = ApplicationVersionRange.Empty
						});
					}
					n++;
				}
				for (int i2 = 0; i2 < optionalDependentModuleList.Count; i2++)
				{
					XmlAttributeCollection attributes23 = optionalDependentModuleList[i2].Attributes;
					XmlAttribute idAttr6 = ((attributes23 != null) ? attributes23["Id"] : null);
					bool flag17 = idAttr6 != null;
					if (flag17)
					{
						dependentModuleMetadatas.Add(new DependentModuleMetadata
						{
							Id = idAttr6.InnerText,
							LoadType = LoadType.None,
							IsOptional = true,
							IsIncompatible = false,
							Version = ApplicationVersion.Empty,
							VersionRange = ApplicationVersionRange.Empty
						});
					}
				}
				XmlNode requiredGameVersion = ((moduleNode != null) ? moduleNode.SelectSingleNode("RequiredGameVersion") : null);
				string text8;
				if (requiredGameVersion == null)
				{
					text8 = null;
				}
				else
				{
					XmlAttributeCollection attributes24 = requiredGameVersion.Attributes;
					if (attributes24 == null)
					{
						text8 = null;
					}
					else
					{
						XmlAttribute xmlAttribute22 = attributes24["value"];
						text8 = ((xmlAttribute22 != null) ? xmlAttribute22.InnerText : null);
					}
				}
				string requiredGameVersionVal = text8 ?? string.Empty;
				bool flag18;
				if (requiredGameVersion == null)
				{
					flag18 = false;
				}
				else
				{
					XmlAttributeCollection attributes25 = requiredGameVersion.Attributes;
					bool? flag19;
					if (attributes25 == null)
					{
						flag19 = null;
					}
					else
					{
						XmlAttribute xmlAttribute23 = attributes25["optional"];
						flag19 = ((xmlAttribute23 != null) ? new bool?(xmlAttribute23.InnerText.Equals("true")) : null);
					}
					bool? flag3 = flag19;
					flag18 = flag3.GetValueOrDefault();
				}
				bool requiredGameVersionOptional = flag18;
				ApplicationVersion gameVersion;
				bool flag20 = !string.IsNullOrWhiteSpace(requiredGameVersionVal) && ApplicationVersion.TryParse(requiredGameVersionVal, out gameVersion);
				if (flag20)
				{
					string[] officialModuleIds = ModuleInfoExtended.OfficialModuleIds;
					for (int num9 = 0; num9 < officialModuleIds.Length; num9++)
					{
						string moduleId = officialModuleIds[num9];
						bool isNative = moduleId.Equals(ModuleInfoExtended.NativeModuleId);
						DependentModuleMetadata module = dependentModuleMetadatas.Find((DependentModuleMetadata dmm) => moduleId.Equals(dmm.Id, StringComparison.Ordinal));
						bool flag21 = module != null;
						if (flag21)
						{
							dependentModuleMetadatas.Remove(module);
						}
						dependentModuleMetadatas.Add(new DependentModuleMetadata
						{
							Id = moduleId,
							LoadType = LoadType.LoadBeforeThis,
							IsOptional = (requiredGameVersionOptional && !isNative),
							IsIncompatible = false,
							Version = gameVersion,
							VersionRange = ApplicationVersionRange.Empty
						});
					}
				}
				result = new ModuleInfoExtended
				{
					Id = id,
					Name = name,
					IsOfficial = isOfficial,
					Version = version,
					IsSingleplayerModule = isSingleplayerModule,
					IsMultiplayerModule = isMultiplayerModule,
					IsServerModule = isServerModule,
					SubModules = subModules,
					DependentModules = dependentModules,
					ModulesToLoadAfterThis = modulesToLoadAfterThis,
					IncompatibleModules = incompatibleModules,
					Url = url,
					UpdateInfo = ((!string.IsNullOrEmpty(updateInfo)) ? updateInfo : string.Empty),
					DependentModuleMetadatas = dependentModuleMetadatas
				};
			}
			return result;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x000074E8 File Offset: 0x000056E8
		public ModuleInfoExtended()
		{
			this.Id = string.Empty;
			this.Name = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.SubModules = Array.Empty<SubModuleInfoExtended>();
			this.DependentModules = Array.Empty<DependentModule>();
			this.ModulesToLoadAfterThis = Array.Empty<DependentModule>();
			this.IncompatibleModules = Array.Empty<DependentModule>();
			this.Url = string.Empty;
			this.UpdateInfo = string.Empty;
			this.DependentModuleMetadatas = Array.Empty<DependentModuleMetadata>();
			base..ctor();
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000756C File Offset: 0x0000576C
		public ModuleInfoExtended(string id, string name, bool isOfficial, ApplicationVersion version, bool isSingleplayerModule, bool isMultiplayerModule, IReadOnlyList<SubModuleInfoExtended> subModules, IReadOnlyList<DependentModule> dependentModules, IReadOnlyList<DependentModule> modulesToLoadAfterThis, IReadOnlyList<DependentModule> incompatibleModules, IReadOnlyList<DependentModuleMetadata> dependentModuleMetadatas, string url)
		{
			this.Id = string.Empty;
			this.Name = string.Empty;
			this.Version = ApplicationVersion.Empty;
			this.SubModules = Array.Empty<SubModuleInfoExtended>();
			this.DependentModules = Array.Empty<DependentModule>();
			this.ModulesToLoadAfterThis = Array.Empty<DependentModule>();
			this.IncompatibleModules = Array.Empty<DependentModule>();
			this.Url = string.Empty;
			this.UpdateInfo = string.Empty;
			this.DependentModuleMetadatas = Array.Empty<DependentModuleMetadata>();
			base..ctor();
			this.Id = id;
			this.Name = name;
			this.IsOfficial = isOfficial;
			this.Version = version;
			this.IsSingleplayerModule = isSingleplayerModule;
			this.IsMultiplayerModule = isMultiplayerModule;
			this.SubModules = subModules;
			this.DependentModules = dependentModules;
			this.ModulesToLoadAfterThis = modulesToLoadAfterThis;
			this.IncompatibleModules = incompatibleModules;
			this.DependentModuleMetadatas = dependentModuleMetadatas;
			this.Url = url;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00007658 File Offset: 0x00005858
		public bool IsNative()
		{
			return ModuleInfoExtended.NativeModuleId.Equals(this.Id, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06000157 RID: 343 RVA: 0x0000766C File Offset: 0x0000586C
		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
			defaultInterpolatedStringHandler.AppendFormatted(this.Id);
			defaultInterpolatedStringHandler.AppendLiteral(" - ");
			defaultInterpolatedStringHandler.AppendFormatted<ApplicationVersion>(this.Version);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000076B4 File Offset: 0x000058B4
		[NullableContext(2)]
		public virtual bool Equals(ModuleInfoExtended other)
		{
			bool flag = other == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = this == other;
				result = flag2 || this.Id == other.Id;
			}
			return result;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x000076EF File Offset: 0x000058EF
		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		// Token: 0x0600015A RID: 346 RVA: 0x000076FC File Offset: 0x000058FC
		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("Id = ");
			builder.Append(this.Id);
			builder.Append(", Name = ");
			builder.Append(this.Name);
			builder.Append(", IsOfficial = ");
			builder.Append(this.IsOfficial.ToString());
			builder.Append(", Version = ");
			builder.Append(this.Version);
			builder.Append(", IsSingleplayerModule = ");
			builder.Append(this.IsSingleplayerModule.ToString());
			builder.Append(", IsMultiplayerModule = ");
			builder.Append(this.IsMultiplayerModule.ToString());
			builder.Append(", IsServerModule = ");
			builder.Append(this.IsServerModule.ToString());
			builder.Append(", SubModules = ");
			builder.Append(this.SubModules);
			builder.Append(", DependentModules = ");
			builder.Append(this.DependentModules);
			builder.Append(", ModulesToLoadAfterThis = ");
			builder.Append(this.ModulesToLoadAfterThis);
			builder.Append(", IncompatibleModules = ");
			builder.Append(this.IncompatibleModules);
			builder.Append(", Url = ");
			builder.Append(this.Url);
			builder.Append(", UpdateInfo = ");
			builder.Append(this.UpdateInfo);
			builder.Append(", DependentModuleMetadatas = ");
			builder.Append(this.DependentModuleMetadatas);
			return true;
		}

		// Token: 0x0600015B RID: 347 RVA: 0x000078A5 File Offset: 0x00005AA5
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleInfoExtended left, ModuleInfoExtended right)
		{
			return !(left == right);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000078B1 File Offset: 0x00005AB1
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleInfoExtended left, ModuleInfoExtended right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000078C7 File Offset: 0x00005AC7
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleInfoExtended);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x000078E0 File Offset: 0x00005AE0
		[CompilerGenerated]
		protected ModuleInfoExtended(ModuleInfoExtended original)
		{
			this.Id = original.<Id>k__BackingField;
			this.Name = original.<Name>k__BackingField;
			this.IsOfficial = original.<IsOfficial>k__BackingField;
			this.Version = original.<Version>k__BackingField;
			this.IsSingleplayerModule = original.<IsSingleplayerModule>k__BackingField;
			this.IsMultiplayerModule = original.<IsMultiplayerModule>k__BackingField;
			this.IsServerModule = original.<IsServerModule>k__BackingField;
			this.SubModules = original.<SubModules>k__BackingField;
			this.DependentModules = original.<DependentModules>k__BackingField;
			this.ModulesToLoadAfterThis = original.<ModulesToLoadAfterThis>k__BackingField;
			this.IncompatibleModules = original.<IncompatibleModules>k__BackingField;
			this.Url = original.<Url>k__BackingField;
			this.UpdateInfo = original.<UpdateInfo>k__BackingField;
			this.DependentModuleMetadatas = original.<DependentModuleMetadatas>k__BackingField;
		}

		// Token: 0x0400004C RID: 76
		private static readonly string NativeModuleId = "Native";

		// Token: 0x0400004D RID: 77
		private static readonly string[] OfficialModuleIds = new string[]
		{
			ModuleInfoExtended.NativeModuleId,
			"SandBox",
			"SandBoxCore",
			"StoryMode",
			"CustomBattle",
			"BirthAndDeath",
			"Multiplayer"
		};
	}
}
