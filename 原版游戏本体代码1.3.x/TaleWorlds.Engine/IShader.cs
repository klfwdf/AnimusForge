using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200001A RID: 26
	[ApplicationInterfaceBase]
	internal interface IShader
	{
		// Token: 0x0600012A RID: 298
		[EngineMethod("get_from_resource", false, null, false)]
		Shader GetFromResource(string shaderName);

		// Token: 0x0600012B RID: 299
		[EngineMethod("get_name", false, null, false)]
		string GetName(UIntPtr shaderPointer);

		// Token: 0x0600012C RID: 300
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr shaderPointer);

		// Token: 0x0600012D RID: 301
		[EngineMethod("get_material_shader_flag_mask", false, null, false)]
		ulong GetMaterialShaderFlagMask(UIntPtr shaderPointer, string flagName, bool showError);
	}
}
