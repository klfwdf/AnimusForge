using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000016 RID: 22
	public class BannerIconGroup
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000EA RID: 234 RVA: 0x000046F9 File Offset: 0x000028F9
		// (set) Token: 0x060000EB RID: 235 RVA: 0x00004701 File Offset: 0x00002901
		public TextObject Name { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000EC RID: 236 RVA: 0x0000470A File Offset: 0x0000290A
		// (set) Token: 0x060000ED RID: 237 RVA: 0x00004712 File Offset: 0x00002912
		public bool IsPattern { get; private set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000EE RID: 238 RVA: 0x0000471B File Offset: 0x0000291B
		// (set) Token: 0x060000EF RID: 239 RVA: 0x00004723 File Offset: 0x00002923
		public int Id { get; private set; }

		// Token: 0x060000F0 RID: 240 RVA: 0x0000472C File Offset: 0x0000292C
		internal BannerIconGroup()
		{
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00004734 File Offset: 0x00002934
		public void Deserialize(XmlNode xmlNode, MBList<BannerIconGroup> previouslyAddedGroups)
		{
			this._allIcons = new Dictionary<int, BannerIconData>();
			this._availableIcons = new Dictionary<int, BannerIconData>();
			this._allBackgrounds = new Dictionary<int, string>();
			this.AllIcons = new MBReadOnlyDictionary<int, BannerIconData>(this._allIcons);
			this.AvailableIcons = new MBReadOnlyDictionary<int, BannerIconData>(this._availableIcons);
			this.AllBackgrounds = new MBReadOnlyDictionary<int, string>(this._allBackgrounds);
			this.Id = Convert.ToInt32(xmlNode.Attributes["id"].Value);
			this.Name = new TextObject(xmlNode.Attributes["name"].Value, null);
			this.IsPattern = Convert.ToBoolean(xmlNode.Attributes["is_pattern"].Value);
			foreach (object obj in xmlNode.ChildNodes)
			{
				XmlNode xmlNode2 = (XmlNode)obj;
				if (xmlNode2.Name == "Icon")
				{
					int id = Convert.ToInt32(xmlNode2.Attributes["id"].Value);
					string value = xmlNode2.Attributes["material_name"].Value;
					int textureIndex = int.Parse(xmlNode2.Attributes["texture_index"].Value);
					if (!this._allIcons.ContainsKey(id) && !previouslyAddedGroups.Any((BannerIconGroup x) => x.AllIcons.ContainsKey(id)))
					{
						this._allIcons.Add(id, new BannerIconData(value, textureIndex));
						if (xmlNode2.Attributes["is_reserved"] == null || !Convert.ToBoolean(xmlNode2.Attributes["is_reserved"].Value))
						{
							this._availableIcons.Add(id, new BannerIconData(value, textureIndex));
						}
					}
				}
				else if (xmlNode2.Name == "Background")
				{
					int id = Convert.ToInt32(xmlNode2.Attributes["id"].Value);
					string value2 = xmlNode2.Attributes["mesh_name"].Value;
					if (xmlNode2.Attributes["is_base_background"] != null && Convert.ToBoolean(xmlNode2.Attributes["is_base_background"].Value))
					{
						BannerManager.Instance.SetBaseBackgroundId(id);
					}
					if (!this._allBackgrounds.ContainsKey(id) && !previouslyAddedGroups.Any((BannerIconGroup x) => x.AllBackgrounds.ContainsKey(id)))
					{
						this._allBackgrounds.Add(id, value2);
					}
				}
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00004A24 File Offset: 0x00002C24
		public void Merge(BannerIconGroup otherGroup)
		{
			foreach (KeyValuePair<int, BannerIconData> keyValuePair in otherGroup._allIcons)
			{
				if (!this._allIcons.ContainsKey(keyValuePair.Key))
				{
					this._allIcons.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}
			foreach (KeyValuePair<int, string> keyValuePair2 in otherGroup._allBackgrounds)
			{
				if (!this._allBackgrounds.ContainsKey(keyValuePair2.Key))
				{
					this._allBackgrounds.Add(keyValuePair2.Key, keyValuePair2.Value);
				}
			}
			foreach (KeyValuePair<int, BannerIconData> keyValuePair3 in otherGroup._availableIcons)
			{
				if (!this._availableIcons.ContainsKey(keyValuePair3.Key))
				{
					this._availableIcons.Add(keyValuePair3.Key, keyValuePair3.Value);
				}
			}
		}

		// Token: 0x04000116 RID: 278
		public MBReadOnlyDictionary<int, BannerIconData> AllIcons;

		// Token: 0x04000117 RID: 279
		public MBReadOnlyDictionary<int, string> AllBackgrounds;

		// Token: 0x04000118 RID: 280
		public MBReadOnlyDictionary<int, BannerIconData> AvailableIcons;

		// Token: 0x04000119 RID: 281
		private Dictionary<int, BannerIconData> _allIcons;

		// Token: 0x0400011A RID: 282
		private Dictionary<int, string> _allBackgrounds;

		// Token: 0x0400011B RID: 283
		private Dictionary<int, BannerIconData> _availableIcons;
	}
}
