using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000013 RID: 19
	[EngineClass("rglDecal")]
	public sealed class Decal : GameEntityComponent
	{
		// Token: 0x06000096 RID: 150 RVA: 0x00003830 File Offset: 0x00001A30
		internal Decal(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003839 File Offset: 0x00001A39
		public static Decal CreateDecal(string name = null)
		{
			return EngineApplicationInterface.IDecal.CreateDecal(name);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00003846 File Offset: 0x00001A46
		public Decal CreateCopy()
		{
			return EngineApplicationInterface.IDecal.CreateCopy(base.Pointer);
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00003858 File Offset: 0x00001A58
		public void CheckAndRegisterToDecalSet()
		{
			EngineApplicationInterface.IDecal.CheckAndRegisterToDecalSet(base.Pointer);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x0000386A File Offset: 0x00001A6A
		public void SetIsVisible(bool value)
		{
			EngineApplicationInterface.IDecal.SetIsVisible(base.Pointer, value);
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600009B RID: 155 RVA: 0x0000387D File Offset: 0x00001A7D
		public bool IsValid
		{
			get
			{
				return base.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x0600009C RID: 156 RVA: 0x0000388F File Offset: 0x00001A8F
		public uint GetFactor1()
		{
			return EngineApplicationInterface.IDecal.GetFactor1(base.Pointer);
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000038A1 File Offset: 0x00001AA1
		public void OverrideRoadBoundaryP0(Vec2 data)
		{
			EngineApplicationInterface.IDecal.OverrideRoadBoundaryP0(base.Pointer, data);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x000038B5 File Offset: 0x00001AB5
		public void OverrideRoadBoundaryP1(Vec2 data)
		{
			EngineApplicationInterface.IDecal.OverrideRoadBoundaryP1(base.Pointer, data);
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000038C9 File Offset: 0x00001AC9
		public void SetFactor1Linear(uint linearFactorColor1)
		{
			EngineApplicationInterface.IDecal.SetFactor1Linear(base.Pointer, linearFactorColor1);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x000038DC File Offset: 0x00001ADC
		public void SetFactor1(uint factorColor1)
		{
			EngineApplicationInterface.IDecal.SetFactor1(base.Pointer, factorColor1);
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x000038EF File Offset: 0x00001AEF
		public void SetAlpha(float alpha)
		{
			EngineApplicationInterface.IDecal.SetAlpha(base.Pointer, alpha);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00003902 File Offset: 0x00001B02
		public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			EngineApplicationInterface.IDecal.SetVectorArgument(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00003919 File Offset: 0x00001B19
		public void SetVectorArgument2(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			EngineApplicationInterface.IDecal.SetVectorArgument2(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00003930 File Offset: 0x00001B30
		public Material GetMaterial()
		{
			return EngineApplicationInterface.IDecal.GetMaterial(base.Pointer);
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00003942 File Offset: 0x00001B42
		public void SetMaterial(Material material)
		{
			EngineApplicationInterface.IDecal.SetMaterial(base.Pointer, material.Pointer);
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x0000395A File Offset: 0x00001B5A
		public void SetFrame(MatrixFrame Frame)
		{
			this.Frame = Frame;
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x060000A7 RID: 167 RVA: 0x00003964 File Offset: 0x00001B64
		// (set) Token: 0x060000A8 RID: 168 RVA: 0x0000398C File Offset: 0x00001B8C
		public MatrixFrame Frame
		{
			get
			{
				MatrixFrame result = default(MatrixFrame);
				EngineApplicationInterface.IDecal.GetFrame(base.Pointer, ref result);
				return result;
			}
			set
			{
				EngineApplicationInterface.IDecal.SetFrame(base.Pointer, ref value);
			}
		}
	}
}
