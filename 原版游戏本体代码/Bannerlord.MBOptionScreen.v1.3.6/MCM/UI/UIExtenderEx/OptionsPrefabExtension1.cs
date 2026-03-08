using System;
using System.Runtime.CompilerServices;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace MCM.UI.UIExtenderEx
{
	// Token: 0x02000016 RID: 22
	[NullableContext(1)]
	[Nullable(0)]
	[PrefabExtension("Options", "descendant::ListPanel[@Id='TabToggleList']/Children/OptionsTabToggle[5]", "Options")]
	internal sealed class OptionsPrefabExtension1 : PrefabExtensionInsertPatch
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000088 RID: 136 RVA: 0x00003667 File Offset: 0x00001867
		public override InsertType Type
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06000089 RID: 137 RVA: 0x0000366A File Offset: 0x0000186A
		public OptionsPrefabExtension1()
		{
			this._xmlDocument.LoadXml("<OptionsTabToggle DataSource=\"{ModOptions}\" PositionYOffset=\"2\" Parameter.ButtonBrush=\"Header.Tab.Center\" Parameter.TabName=\"ModOptionsPage\" />");
		}

		// Token: 0x0600008A RID: 138 RVA: 0x0000368D File Offset: 0x0000188D
		[PrefabExtensionInsertPatch.PrefabExtensionXmlNodeAttribute(false)]
		public XmlNode GetPrefabExtension()
		{
			return this._xmlDocument;
		}

		// Token: 0x0400001E RID: 30
		private readonly XmlDocument _xmlDocument = new XmlDocument();
	}
}
