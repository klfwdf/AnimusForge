using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;

namespace MCM.UI.Adapter.MCMv5.Properties
{
	// Token: 0x0200000B RID: 11
	[NullableContext(1)]
	[Nullable(0)]
	public sealed class MCMv5AttributeSettingsPropertyDiscoverer : IAttributeSettingsPropertyDiscoverer, ISettingsPropertyDiscoverer
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000019 RID: 25 RVA: 0x000027F7 File Offset: 0x000009F7
		public IEnumerable<string> DiscoveryTypes { get; } = new string[] { "mcm_v5_attributes" };

		// Token: 0x0600001A RID: 26 RVA: 0x000027FF File Offset: 0x000009FF
		public IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings)
		{
			MCMv5AttributeSettingsPropertyDiscoverer.<GetProperties>d__3 <GetProperties>d__ = new MCMv5AttributeSettingsPropertyDiscoverer.<GetProperties>d__3(-2);
			<GetProperties>d__.<>4__this = this;
			<GetProperties>d__.<>3__settings = settings;
			return <GetProperties>d__;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002816 File Offset: 0x00000A16
		private static IEnumerable<ISettingsPropertyDefinition> GetPropertiesInternal([Nullable(2)] object @object)
		{
			MCMv5AttributeSettingsPropertyDiscoverer.<GetPropertiesInternal>d__4 <GetPropertiesInternal>d__ = new MCMv5AttributeSettingsPropertyDiscoverer.<GetPropertiesInternal>d__4(-2);
			<GetPropertiesInternal>d__.<>3__object = @object;
			return <GetPropertiesInternal>d__;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002826 File Offset: 0x00000A26
		private static IEnumerable<IPropertyDefinitionBase> GetPropertyDefinitionWrappers(IReadOnlyCollection<object> properties)
		{
			MCMv5AttributeSettingsPropertyDiscoverer.<GetPropertyDefinitionWrappers>d__5 <GetPropertyDefinitionWrappers>d__ = new MCMv5AttributeSettingsPropertyDiscoverer.<GetPropertyDefinitionWrappers>d__5(-2);
			<GetPropertyDefinitionWrappers>d__.<>3__properties = properties;
			return <GetPropertyDefinitionWrappers>d__;
		}
	}
}
