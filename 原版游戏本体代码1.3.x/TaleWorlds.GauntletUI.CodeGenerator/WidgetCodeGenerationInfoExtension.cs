using System;
using System.Collections.Generic;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000011 RID: 17
	public abstract class WidgetCodeGenerationInfoExtension
	{
		// Token: 0x060000EC RID: 236
		public abstract void Initialize();

		// Token: 0x060000ED RID: 237
		public abstract void OnFillSetAttributesMethod(MethodCode methodCode);

		// Token: 0x060000EE RID: 238
		public abstract bool TryGetVariantPropertiesForNewDependency(out UICodeGenerationVariantExtension variantExtension, out Dictionary<string, object> data);

		// Token: 0x060000EF RID: 239
		public abstract void OnFillCreateWidgetMethod(MethodCode methodCode);
	}
}
