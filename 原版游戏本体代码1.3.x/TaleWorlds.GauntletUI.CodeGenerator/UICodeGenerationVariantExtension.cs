using System;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000010 RID: 16
	public abstract class UICodeGenerationVariantExtension
	{
		// Token: 0x060000E4 RID: 228
		public abstract PrefabExtension GetPrefabExtension();

		// Token: 0x060000E5 RID: 229
		public abstract Type GetAttributeType(WidgetAttributeTemplate widgetAttributeTemplate);

		// Token: 0x060000E6 RID: 230
		public abstract void AddExtensionVariables(ClassCode classCode);

		// Token: 0x060000E7 RID: 231
		public abstract void Initialize(WidgetTemplateGenerateContext widgetTemplateGenerateContext);

		// Token: 0x060000E8 RID: 232
		public abstract void AddExtrasToCreatorMethod(MethodCode methodCode);

		// Token: 0x060000E9 RID: 233
		public abstract WidgetCodeGenerationInfoExtension CreateWidgetCodeGenerationInfoExtension(WidgetCodeGenerationInfo widgetCodeGenerationInfo);

		// Token: 0x060000EA RID: 234
		public abstract void DoExtraCodeGeneration(ClassCode classCode);
	}
}
