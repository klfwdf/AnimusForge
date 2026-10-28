using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace MCM.UI.UIExtenderEx;

[PrefabExtension("Options", "descendant::Widget[@Id='DescriptionsRightPanel']", "Options")]
internal sealed class OptionsPrefabExtension3 : PrefabExtensionSetAttributePatch
{
	public override List<Attribute> Attributes => new List<Attribute>(1)
	{
		new Attribute("SuggestedWidth", "@DescriptionWidth")
	};
}
