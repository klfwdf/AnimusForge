using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200005C RID: 92
	[NullableContext(1)]
	[Nullable(0)]
	internal class ModuleInfoExtended : IEquatable<ModuleInfoExtended>
	{
		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x0600033E RID: 830 RVA: 0x0000B4E7 File Offset: 0x000096E7
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(ModuleInfoExtended);
			}
		}

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x0600033F RID: 831 RVA: 0x0000B4F3 File Offset: 0x000096F3
		// (set) Token: 0x06000340 RID: 832 RVA: 0x0000B4FB File Offset: 0x000096FB
		public string Id { get; set; }

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000341 RID: 833 RVA: 0x0000B504 File Offset: 0x00009704
		// (set) Token: 0x06000342 RID: 834 RVA: 0x0000B50C File Offset: 0x0000970C
		public string Name { get; set; }

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000343 RID: 835 RVA: 0x0000B515 File Offset: 0x00009715
		// (set) Token: 0x06000344 RID: 836 RVA: 0x0000B51D File Offset: 0x0000971D
		public bool IsOfficial { get; set; }

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000345 RID: 837 RVA: 0x0000B526 File Offset: 0x00009726
		// (set) Token: 0x06000346 RID: 838 RVA: 0x0000B52E File Offset: 0x0000972E
		public ApplicationVersion Version { get; set; }

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000347 RID: 839 RVA: 0x0000B537 File Offset: 0x00009737
		// (set) Token: 0x06000348 RID: 840 RVA: 0x0000B53F File Offset: 0x0000973F
		public bool IsSingleplayerModule { get; set; }

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000349 RID: 841 RVA: 0x0000B548 File Offset: 0x00009748
		// (set) Token: 0x0600034A RID: 842 RVA: 0x0000B550 File Offset: 0x00009750
		public bool IsMultiplayerModule { get; set; }

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600034B RID: 843 RVA: 0x0000B559 File Offset: 0x00009759
		// (set) Token: 0x0600034C RID: 844 RVA: 0x0000B561 File Offset: 0x00009761
		public bool IsServerModule { get; set; }

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600034D RID: 845 RVA: 0x0000B56A File Offset: 0x0000976A
		// (set) Token: 0x0600034E RID: 846 RVA: 0x0000B572 File Offset: 0x00009772
		public IReadOnlyList<SubModuleInfoExtended> SubModules { get; set; }

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x0600034F RID: 847 RVA: 0x0000B57B File Offset: 0x0000977B
		// (set) Token: 0x06000350 RID: 848 RVA: 0x0000B583 File Offset: 0x00009783
		public IReadOnlyList<DependentModule> DependentModules { get; set; }

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x06000351 RID: 849 RVA: 0x0000B58C File Offset: 0x0000978C
		// (set) Token: 0x06000352 RID: 850 RVA: 0x0000B594 File Offset: 0x00009794
		public IReadOnlyList<DependentModule> ModulesToLoadAfterThis { get; set; }

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x06000353 RID: 851 RVA: 0x0000B59D File Offset: 0x0000979D
		// (set) Token: 0x06000354 RID: 852 RVA: 0x0000B5A5 File Offset: 0x000097A5
		public IReadOnlyList<DependentModule> IncompatibleModules { get; set; }

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x06000355 RID: 853 RVA: 0x0000B5AE File Offset: 0x000097AE
		// (set) Token: 0x06000356 RID: 854 RVA: 0x0000B5B6 File Offset: 0x000097B6
		public string Url { get; set; }

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x06000357 RID: 855 RVA: 0x0000B5BF File Offset: 0x000097BF
		// (set) Token: 0x06000358 RID: 856 RVA: 0x0000B5C7 File Offset: 0x000097C7
		public string UpdateInfo { get; set; }

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x06000359 RID: 857 RVA: 0x0000B5D0 File Offset: 0x000097D0
		// (set) Token: 0x0600035A RID: 858 RVA: 0x0000B5D8 File Offset: 0x000097D8
		public IReadOnlyList<DependentModuleMetadata> DependentModuleMetadatas { get; set; }

		// Token: 0x0600035B RID: 859 RVA: 0x0000B5E4 File Offset: 0x000097E4
		[NullableContext(2)]
		public static ModuleInfoExtended FromXml(XmlDocument xmlDocument)
		{
			if (xmlDocument == null)
			{
				return null;
			}
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
				bool? flag;
				if (xmlNode5 == null)
				{
					flag = null;
				}
				else
				{
					XmlAttributeCollection attributes5 = xmlNode5.Attributes;
					if (attributes5 == null)
					{
						flag = null;
					}
					else
					{
						XmlAttribute xmlAttribute4 = attributes5["value"];
						flag = ((xmlAttribute4 != null) ? new bool?(xmlAttribute4.InnerText.Equals("true")) : null);
					}
				}
				bool? flag2 = flag;
				if (flag2.GetValueOrDefault())
				{
					goto IL_198;
				}
			}
			bool flag3;
			if (!(moduleType == "Official"))
			{
				flag3 = moduleType == "OfficialOptional";
				goto IL_199;
			}
			IL_198:
			flag3 = true;
			IL_199:
			bool isOfficial = flag3;
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
				bool? flag4;
				if (xmlNode7 == null)
				{
					flag4 = null;
				}
				else
				{
					XmlAttributeCollection attributes7 = xmlNode7.Attributes;
					if (attributes7 == null)
					{
						flag4 = null;
					}
					else
					{
						XmlAttribute xmlAttribute6 = attributes7["value"];
						flag4 = ((xmlAttribute6 != null) ? new bool?(xmlAttribute6.InnerText.Equals("true")) : null);
					}
				}
				bool? flag2 = flag4;
				if (flag2.GetValueOrDefault())
				{
					goto IL_25F;
				}
			}
			bool flag5;
			if (!(moduleCategory == "Singleplayer"))
			{
				flag5 = moduleCategory == "SingleplayerOptional";
				goto IL_260;
			}
			IL_25F:
			flag5 = true;
			IL_260:
			bool isSingleplayerModule = flag5;
			if (moduleNode != null)
			{
				XmlNode xmlNode8 = moduleNode.SelectSingleNode("MultiplayerModule");
				bool? flag6;
				if (xmlNode8 == null)
				{
					flag6 = null;
				}
				else
				{
					XmlAttributeCollection attributes8 = xmlNode8.Attributes;
					if (attributes8 == null)
					{
						flag6 = null;
					}
					else
					{
						XmlAttribute xmlAttribute7 = attributes8["value"];
						flag6 = ((xmlAttribute7 != null) ? new bool?(xmlAttribute7.InnerText.Equals("true")) : null);
					}
				}
				bool? flag2 = flag6;
				if (flag2.GetValueOrDefault())
				{
					goto IL_2EA;
				}
			}
			bool flag7;
			if (!(moduleCategory == "Multiplayer"))
			{
				flag7 = moduleCategory == "MultiplayerOptional";
				goto IL_2EB;
			}
			IL_2EA:
			flag7 = true;
			IL_2EB:
			bool isMultiplayerModule = flag7;
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
				if (idAttr != null)
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
				if (idAttr2 != null)
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
				if (idAttr3 != null)
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
				if (subModule != null)
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
						XmlAttribute xmlAttribute14 = attributes15["provider"];
						text6 = ((xmlAttribute14 != null) ? xmlAttribute14.InnerText : null);
					}
				}
			}
			string updateInfoProvider = text6 ?? string.Empty;
			string text7;
			if (moduleNode == null)
			{
				text7 = null;
			}
			else
			{
				XmlNode xmlNode16 = moduleNode.SelectSingleNode("UpdateInfo");
				if (xmlNode16 == null)
				{
					text7 = null;
				}
				else
				{
					XmlAttributeCollection attributes16 = xmlNode16.Attributes;
					if (attributes16 == null)
					{
						text7 = null;
					}
					else
					{
						XmlAttribute xmlAttribute15 = attributes16["value"];
						text7 = ((xmlAttribute15 != null) ? xmlAttribute15.InnerText : null);
					}
				}
			}
			string updateInfoValue = text7 ?? string.Empty;
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
				XmlAttribute xmlAttribute16;
				if (dependentModuleMetadatasList == null)
				{
					xmlAttribute16 = null;
				}
				else
				{
					XmlNode xmlNode17 = dependentModuleMetadatasList[m];
					if (xmlNode17 == null)
					{
						xmlAttribute16 = null;
					}
					else
					{
						XmlAttributeCollection attributes17 = xmlNode17.Attributes;
						xmlAttribute16 = ((attributes17 != null) ? attributes17["id"] : null);
					}
				}
				XmlAttribute idAttr4 = xmlAttribute16;
				if (idAttr4 != null)
				{
					XmlNode xmlNode18 = dependentModuleMetadatasList[m];
					XmlAttribute xmlAttribute17;
					if (xmlNode18 == null)
					{
						xmlAttribute17 = null;
					}
					else
					{
						XmlAttributeCollection attributes18 = xmlNode18.Attributes;
						xmlAttribute17 = ((attributes18 != null) ? attributes18["order"] : null);
					}
					XmlAttribute orderAttr = xmlAttribute17;
					LoadTypeParse loadType;
					LoadType order = (LoadType)((orderAttr != null && Enum.TryParse<LoadTypeParse>(orderAttr.InnerText, out loadType)) ? loadType : ((LoadTypeParse)0));
					XmlNode xmlNode19 = dependentModuleMetadatasList[m];
					bool? flag8;
					if (xmlNode19 == null)
					{
						flag8 = null;
					}
					else
					{
						XmlAttributeCollection attributes19 = xmlNode19.Attributes;
						if (attributes19 == null)
						{
							flag8 = null;
						}
						else
						{
							XmlAttribute xmlAttribute18 = attributes19["optional"];
							flag8 = ((xmlAttribute18 != null) ? new bool?(xmlAttribute18.InnerText.Equals("true")) : null);
						}
					}
					bool? flag2 = flag8;
					bool optional2 = flag2.GetValueOrDefault();
					XmlNode xmlNode20 = dependentModuleMetadatasList[m];
					bool? flag9;
					if (xmlNode20 == null)
					{
						flag9 = null;
					}
					else
					{
						XmlAttributeCollection attributes20 = xmlNode20.Attributes;
						if (attributes20 == null)
						{
							flag9 = null;
						}
						else
						{
							XmlAttribute xmlAttribute19 = attributes20["incompatible"];
							flag9 = ((xmlAttribute19 != null) ? new bool?(xmlAttribute19.InnerText.Equals("true")) : null);
						}
					}
					flag2 = flag9;
					bool incompatible = flag2.GetValueOrDefault();
					XmlNode xmlNode21 = dependentModuleMetadatasList[m];
					string versionAsString3;
					if (xmlNode21 == null)
					{
						versionAsString3 = null;
					}
					else
					{
						XmlAttributeCollection attributes21 = xmlNode21.Attributes;
						if (attributes21 == null)
						{
							versionAsString3 = null;
						}
						else
						{
							XmlAttribute xmlAttribute20 = attributes21["version"];
							versionAsString3 = ((xmlAttribute20 != null) ? xmlAttribute20.InnerText : null);
						}
					}
					ApplicationVersion v;
					ApplicationVersion dVersion2 = (ApplicationVersion.TryParse(versionAsString3, out v) ? v : ApplicationVersion.Empty);
					XmlNode xmlNode22 = dependentModuleMetadatasList[m];
					string text8;
					if (xmlNode22 == null)
					{
						text8 = null;
					}
					else
					{
						XmlAttributeCollection attributes22 = xmlNode22.Attributes;
						if (attributes22 == null)
						{
							text8 = null;
						}
						else
						{
							XmlAttribute xmlAttribute21 = attributes22["version"];
							text8 = ((xmlAttribute21 != null) ? xmlAttribute21.InnerText : null);
						}
					}
					ApplicationVersionRange vr;
					ApplicationVersionRange dVersionRange = (ApplicationVersionRange.TryParse(text8 ?? string.Empty, out vr) ? vr : ApplicationVersionRange.Empty);
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
				XmlAttribute xmlAttribute22;
				if (loadAfterModuleList == null)
				{
					xmlAttribute22 = null;
				}
				else
				{
					XmlNode xmlNode23 = loadAfterModuleList[n];
					if (xmlNode23 == null)
					{
						xmlAttribute22 = null;
					}
					else
					{
						XmlAttributeCollection attributes23 = xmlNode23.Attributes;
						xmlAttribute22 = ((attributes23 != null) ? attributes23["Id"] : null);
					}
				}
				XmlAttribute idAttr5 = xmlAttribute22;
				if (idAttr5 != null)
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
				XmlAttributeCollection attributes24 = optionalDependentModuleList[i2].Attributes;
				XmlAttribute idAttr6 = ((attributes24 != null) ? attributes24["Id"] : null);
				if (idAttr6 != null)
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
			string text9;
			if (requiredGameVersion == null)
			{
				text9 = null;
			}
			else
			{
				XmlAttributeCollection attributes25 = requiredGameVersion.Attributes;
				if (attributes25 == null)
				{
					text9 = null;
				}
				else
				{
					XmlAttribute xmlAttribute23 = attributes25["value"];
					text9 = ((xmlAttribute23 != null) ? xmlAttribute23.InnerText : null);
				}
			}
			string requiredGameVersionVal = text9 ?? string.Empty;
			bool flag10;
			if (requiredGameVersion == null)
			{
				flag10 = false;
			}
			else
			{
				XmlAttributeCollection attributes26 = requiredGameVersion.Attributes;
				bool? flag11;
				if (attributes26 == null)
				{
					flag11 = null;
				}
				else
				{
					XmlAttribute xmlAttribute24 = attributes26["optional"];
					flag11 = ((xmlAttribute24 != null) ? new bool?(xmlAttribute24.InnerText.Equals("true")) : null);
				}
				bool? flag2 = flag11;
				flag10 = flag2.GetValueOrDefault();
			}
			bool requiredGameVersionOptional = flag10;
			ApplicationVersion gameVersion;
			if (!string.IsNullOrWhiteSpace(requiredGameVersionVal) && ApplicationVersion.TryParse(requiredGameVersionVal, out gameVersion))
			{
				string[] officialModuleIds = ModuleInfoExtended.OfficialModuleIds;
				for (int num9 = 0; num9 < officialModuleIds.Length; num9++)
				{
					string moduleId = officialModuleIds[num9];
					bool isNative = moduleId.Equals(ModuleInfoExtended.NativeModuleId);
					DependentModuleMetadata module = dependentModuleMetadatas.Find((DependentModuleMetadata dmm) => dmm.Id.Equals(moduleId, StringComparison.Ordinal));
					if (module != null)
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
			return new ModuleInfoExtended
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
				UpdateInfo = ((!string.IsNullOrEmpty(updateInfoProvider) && !string.IsNullOrEmpty(updateInfoValue)) ? (updateInfoProvider + ":" + updateInfoValue) : string.Empty),
				DependentModuleMetadatas = dependentModuleMetadatas
			};
		}

		// Token: 0x0600035C RID: 860 RVA: 0x0000C450 File Offset: 0x0000A650
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

		// Token: 0x0600035D RID: 861 RVA: 0x0000C4D4 File Offset: 0x0000A6D4
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

		// Token: 0x0600035E RID: 862 RVA: 0x0000C5B2 File Offset: 0x0000A7B2
		public bool IsNative()
		{
			return this.Id.Equals(ModuleInfoExtended.NativeModuleId, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0600035F RID: 863 RVA: 0x0000C5C5 File Offset: 0x0000A7C5
		public override string ToString()
		{
			return string.Format("{0} - {1}", this.Id, this.Version);
		}

		// Token: 0x06000360 RID: 864 RVA: 0x0000C5DD File Offset: 0x0000A7DD
		[NullableContext(2)]
		public virtual bool Equals(ModuleInfoExtended other)
		{
			return other != null && (this == other || this.Id == other.Id);
		}

		// Token: 0x06000361 RID: 865 RVA: 0x0000C5FB File Offset: 0x0000A7FB
		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
		}

		// Token: 0x06000362 RID: 866 RVA: 0x0000C608 File Offset: 0x0000A808
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

		// Token: 0x06000363 RID: 867 RVA: 0x0000C7B1 File Offset: 0x0000A9B1
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator !=(ModuleInfoExtended left, ModuleInfoExtended right)
		{
			return !(left == right);
		}

		// Token: 0x06000364 RID: 868 RVA: 0x0000C7BD File Offset: 0x0000A9BD
		[NullableContext(2)]
		[CompilerGenerated]
		public static bool operator ==(ModuleInfoExtended left, ModuleInfoExtended right)
		{
			return left == right || (left != null && left.Equals(right));
		}

		// Token: 0x06000365 RID: 869 RVA: 0x0000C7D1 File Offset: 0x0000A9D1
		[NullableContext(2)]
		[CompilerGenerated]
		public override bool Equals(object obj)
		{
			return this.Equals(obj as ModuleInfoExtended);
		}

		// Token: 0x06000367 RID: 871 RVA: 0x0000C7E8 File Offset: 0x0000A9E8
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

		// Token: 0x0400011C RID: 284
		private static readonly string NativeModuleId = "Native";

		// Token: 0x0400011D RID: 285
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
