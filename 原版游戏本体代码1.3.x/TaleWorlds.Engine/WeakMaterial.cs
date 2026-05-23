using System;

namespace TaleWorlds.Engine
{
	// Token: 0x0200009D RID: 157
	public struct WeakMaterial
	{
		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x06000ECE RID: 3790 RVA: 0x0001127E File Offset: 0x0000F47E
		// (set) Token: 0x06000ECF RID: 3791 RVA: 0x00011286 File Offset: 0x0000F486
		public UIntPtr Pointer { get; private set; }

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x06000ED0 RID: 3792 RVA: 0x0001128F File Offset: 0x0000F48F
		public bool IsValid
		{
			get
			{
				return this.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x06000ED1 RID: 3793 RVA: 0x000112A1 File Offset: 0x0000F4A1
		internal WeakMaterial(UIntPtr pointer)
		{
			this.Pointer = pointer;
		}

		// Token: 0x06000ED2 RID: 3794 RVA: 0x000112AA File Offset: 0x0000F4AA
		public Shader GetShader()
		{
			return EngineApplicationInterface.IMaterial.GetShader(this.Pointer);
		}

		// Token: 0x06000ED3 RID: 3795 RVA: 0x000112BC File Offset: 0x0000F4BC
		public ulong GetShaderFlags()
		{
			return EngineApplicationInterface.IMaterial.GetShaderFlags(this.Pointer);
		}

		// Token: 0x06000ED4 RID: 3796 RVA: 0x000112CE File Offset: 0x0000F4CE
		public void SetShaderFlags(ulong flagEntry)
		{
			EngineApplicationInterface.IMaterial.SetShaderFlags(this.Pointer, flagEntry);
		}

		// Token: 0x06000ED5 RID: 3797 RVA: 0x000112E1 File Offset: 0x0000F4E1
		public void SetMeshVectorArgument(float x, float y, float z, float w)
		{
			EngineApplicationInterface.IMaterial.SetMeshVectorArgument(this.Pointer, x, y, z, w);
		}

		// Token: 0x06000ED6 RID: 3798 RVA: 0x000112F8 File Offset: 0x0000F4F8
		public void SetTexture(Material.MBTextureType textureType, Texture texture)
		{
			EngineApplicationInterface.IMaterial.SetTexture(this.Pointer, (int)textureType, texture.Pointer);
		}

		// Token: 0x06000ED7 RID: 3799 RVA: 0x00011311 File Offset: 0x0000F511
		public void SetTextureAtSlot(int textureSlot, Texture texture)
		{
			EngineApplicationInterface.IMaterial.SetTextureAtSlot(this.Pointer, textureSlot, texture.Pointer);
		}

		// Token: 0x06000ED8 RID: 3800 RVA: 0x0001132A File Offset: 0x0000F52A
		public void SetAreaMapScale(float scale)
		{
			EngineApplicationInterface.IMaterial.SetAreaMapScale(this.Pointer, scale);
		}

		// Token: 0x06000ED9 RID: 3801 RVA: 0x0001133D File Offset: 0x0000F53D
		public void SetEnableSkinning(bool enable)
		{
			EngineApplicationInterface.IMaterial.SetEnableSkinning(this.Pointer, enable);
		}

		// Token: 0x06000EDA RID: 3802 RVA: 0x00011350 File Offset: 0x0000F550
		public bool UsingSkinning()
		{
			return EngineApplicationInterface.IMaterial.UsingSkinning(this.Pointer);
		}

		// Token: 0x06000EDB RID: 3803 RVA: 0x00011362 File Offset: 0x0000F562
		public Texture GetTexture(Material.MBTextureType textureType)
		{
			return EngineApplicationInterface.IMaterial.GetTexture(this.Pointer, (int)textureType);
		}

		// Token: 0x06000EDC RID: 3804 RVA: 0x00011375 File Offset: 0x0000F575
		public Texture GetTextureWithSlot(int textureSlot)
		{
			return EngineApplicationInterface.IMaterial.GetTexture(this.Pointer, textureSlot);
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x06000EDD RID: 3805 RVA: 0x00011388 File Offset: 0x0000F588
		// (set) Token: 0x06000EDE RID: 3806 RVA: 0x0001139A File Offset: 0x0000F59A
		public string Name
		{
			get
			{
				return EngineApplicationInterface.IMaterial.GetName(this.Pointer);
			}
			set
			{
				EngineApplicationInterface.IMaterial.SetName(this.Pointer, value);
			}
		}

		// Token: 0x06000EDF RID: 3807 RVA: 0x000113AD File Offset: 0x0000F5AD
		public void AddMaterialShaderFlag(string flagName, bool showErrors)
		{
			EngineApplicationInterface.IMaterial.AddMaterialShaderFlag(this.Pointer, flagName, showErrors);
		}

		// Token: 0x06000EE0 RID: 3808 RVA: 0x000113C1 File Offset: 0x0000F5C1
		public void RemoveMaterialShaderFlag(string flagName)
		{
			EngineApplicationInterface.IMaterial.RemoveMaterialShaderFlag(this.Pointer, flagName);
		}

		// Token: 0x06000EE1 RID: 3809 RVA: 0x000113D4 File Offset: 0x0000F5D4
		public static bool operator ==(WeakMaterial weakMaterial1, WeakMaterial weakMaterial2)
		{
			return weakMaterial1.Pointer == weakMaterial2.Pointer;
		}

		// Token: 0x06000EE2 RID: 3810 RVA: 0x000113E9 File Offset: 0x0000F5E9
		public static bool operator !=(WeakMaterial weakMaterial1, WeakMaterial weakMaterial2)
		{
			return weakMaterial1.Pointer != weakMaterial2.Pointer;
		}

		// Token: 0x06000EE3 RID: 3811 RVA: 0x000113FE File Offset: 0x0000F5FE
		public override bool Equals(object obj)
		{
			return ((Material)obj).Pointer == this.Pointer;
		}

		// Token: 0x06000EE4 RID: 3812 RVA: 0x00011418 File Offset: 0x0000F618
		public override int GetHashCode()
		{
			return this.Pointer.GetHashCode();
		}

		// Token: 0x04000203 RID: 515
		public static readonly WeakMaterial Invalid = new WeakMaterial(UIntPtr.Zero);
	}
}
