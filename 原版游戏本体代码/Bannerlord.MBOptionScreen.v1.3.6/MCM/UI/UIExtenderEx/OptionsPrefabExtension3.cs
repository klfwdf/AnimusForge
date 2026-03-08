using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace MCM.UI.UIExtenderEx
{
	// Token: 0x02000018 RID: 24
	[NullableContext(1)]
	[Nullable(0)]
	[PrefabExtension("Options", "descendant::Widget[@Id='DescriptionsRightPanel']", "Options")]
	internal sealed class OptionsPrefabExtension3 : PrefabExtensionSetAttributePatch
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600008E RID: 142 RVA: 0x000036C3 File Offset: 0x000018C3
		public override List<PrefabExtensionSetAttributePatch.Attribute> Attributes
		{
			get
			{
				return new List<PrefabExtensionSetAttributePatch.Attribute>(1)
				{
					new PrefabExtensionSetAttributePatch.Attribute("SuggestedWidth", "@DescriptionWidth")
				};
			}
		}
	}
}
