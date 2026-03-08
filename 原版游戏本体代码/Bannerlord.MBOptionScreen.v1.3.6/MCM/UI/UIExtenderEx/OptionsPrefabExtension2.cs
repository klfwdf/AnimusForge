using System;
using System.Runtime.CompilerServices;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace MCM.UI.UIExtenderEx
{
	// Token: 0x02000017 RID: 23
	[NullableContext(1)]
	[Nullable(0)]
	[PrefabExtension("Options", "descendant::TabControl[@Id='TabControl']/Children/*[5]", "Options")]
	internal sealed class OptionsPrefabExtension2 : PrefabExtensionInsertPatch
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600008B RID: 139 RVA: 0x00003695 File Offset: 0x00001895
		public override InsertType Type
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00003698 File Offset: 0x00001898
		public OptionsPrefabExtension2()
		{
			this._xmlDocument.LoadXml("<ModOptionsPageView Id=\"ModOptionsPage\" DataSource=\"{ModOptions}\" />");
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000036BB File Offset: 0x000018BB
		[PrefabExtensionInsertPatch.PrefabExtensionXmlDocumentAttribute(false)]
		public XmlDocument GetPrefabExtension()
		{
			return this._xmlDocument;
		}

		// Token: 0x0400001F RID: 31
		private readonly XmlDocument _xmlDocument = new XmlDocument();
	}
}
