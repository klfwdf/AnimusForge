using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000089 RID: 137
	public sealed class Shader : Resource
	{
		// Token: 0x06000C4E RID: 3150 RVA: 0x0000D954 File Offset: 0x0000BB54
		internal Shader(UIntPtr ptr)
			: base(ptr)
		{
		}

		// Token: 0x06000C4F RID: 3151 RVA: 0x0000D95D File Offset: 0x0000BB5D
		public static Shader GetFromResource(string shaderName)
		{
			return EngineApplicationInterface.IShader.GetFromResource(shaderName);
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x06000C50 RID: 3152 RVA: 0x0000D96A File Offset: 0x0000BB6A
		public string Name
		{
			get
			{
				return EngineApplicationInterface.IShader.GetName(base.Pointer);
			}
		}

		// Token: 0x06000C51 RID: 3153 RVA: 0x0000D97C File Offset: 0x0000BB7C
		public ulong GetMaterialShaderFlagMask(string flagName, bool showErrors = true)
		{
			return EngineApplicationInterface.IShader.GetMaterialShaderFlagMask(base.Pointer, flagName, showErrors);
		}
	}
}
