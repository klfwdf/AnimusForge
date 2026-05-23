using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000055 RID: 85
	[EngineClass("rglLight")]
	public sealed class Light : GameEntityComponent
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600089A RID: 2202 RVA: 0x00006CF3 File Offset: 0x00004EF3
		public bool IsValid
		{
			get
			{
				return base.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x0600089B RID: 2203 RVA: 0x00006D05 File Offset: 0x00004F05
		internal Light(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x0600089C RID: 2204 RVA: 0x00006D0E File Offset: 0x00004F0E
		public static Light CreatePointLight(float lightRadius)
		{
			return EngineApplicationInterface.ILight.CreatePointLight(lightRadius);
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x0600089D RID: 2205 RVA: 0x00006D1C File Offset: 0x00004F1C
		// (set) Token: 0x0600089E RID: 2206 RVA: 0x00006D3C File Offset: 0x00004F3C
		public MatrixFrame Frame
		{
			get
			{
				MatrixFrame result;
				EngineApplicationInterface.ILight.GetFrame(base.Pointer, out result);
				return result;
			}
			set
			{
				EngineApplicationInterface.ILight.SetFrame(base.Pointer, ref value);
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x0600089F RID: 2207 RVA: 0x00006D50 File Offset: 0x00004F50
		// (set) Token: 0x060008A0 RID: 2208 RVA: 0x00006D62 File Offset: 0x00004F62
		public Vec3 LightColor
		{
			get
			{
				return EngineApplicationInterface.ILight.GetLightColor(base.Pointer);
			}
			set
			{
				EngineApplicationInterface.ILight.SetLightColor(base.Pointer, value);
			}
		}

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060008A1 RID: 2209 RVA: 0x00006D75 File Offset: 0x00004F75
		// (set) Token: 0x060008A2 RID: 2210 RVA: 0x00006D87 File Offset: 0x00004F87
		public float Intensity
		{
			get
			{
				return EngineApplicationInterface.ILight.GetIntensity(base.Pointer);
			}
			set
			{
				EngineApplicationInterface.ILight.SetIntensity(base.Pointer, value);
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060008A3 RID: 2211 RVA: 0x00006D9A File Offset: 0x00004F9A
		// (set) Token: 0x060008A4 RID: 2212 RVA: 0x00006DAC File Offset: 0x00004FAC
		public float Radius
		{
			get
			{
				return EngineApplicationInterface.ILight.GetRadius(base.Pointer);
			}
			set
			{
				EngineApplicationInterface.ILight.SetRadius(base.Pointer, value);
			}
		}

		// Token: 0x060008A5 RID: 2213 RVA: 0x00006DBF File Offset: 0x00004FBF
		public void SetShadowType(Light.ShadowType type)
		{
			EngineApplicationInterface.ILight.SetShadows(base.Pointer, (int)type);
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060008A6 RID: 2214 RVA: 0x00006DD2 File Offset: 0x00004FD2
		// (set) Token: 0x060008A7 RID: 2215 RVA: 0x00006DE4 File Offset: 0x00004FE4
		public bool ShadowEnabled
		{
			get
			{
				return EngineApplicationInterface.ILight.IsShadowEnabled(base.Pointer);
			}
			set
			{
				EngineApplicationInterface.ILight.EnableShadow(base.Pointer, value);
			}
		}

		// Token: 0x060008A8 RID: 2216 RVA: 0x00006DF7 File Offset: 0x00004FF7
		public void SetLightFlicker(float magnitude, float interval)
		{
			EngineApplicationInterface.ILight.SetLightFlicker(base.Pointer, magnitude, interval);
		}

		// Token: 0x060008A9 RID: 2217 RVA: 0x00006E0B File Offset: 0x0000500B
		public void SetVolumetricProperties(bool volumetricLightEnabled, float volumeParameters)
		{
			EngineApplicationInterface.ILight.SetVolumetricProperties(base.Pointer, volumetricLightEnabled, volumeParameters);
		}

		// Token: 0x060008AA RID: 2218 RVA: 0x00006E1F File Offset: 0x0000501F
		public void Dispose()
		{
			if (this.IsValid)
			{
				this.Release();
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x060008AB RID: 2219 RVA: 0x00006E35 File Offset: 0x00005035
		public void SetVisibility(bool value)
		{
			EngineApplicationInterface.ILight.SetVisibility(base.Pointer, value);
		}

		// Token: 0x060008AC RID: 2220 RVA: 0x00006E48 File Offset: 0x00005048
		private void Release()
		{
			EngineApplicationInterface.ILight.Release(base.Pointer);
		}

		// Token: 0x060008AD RID: 2221 RVA: 0x00006E5C File Offset: 0x0000505C
		~Light()
		{
			this.Dispose();
		}

		// Token: 0x020000C2 RID: 194
		public enum ShadowType
		{
			// Token: 0x040003E5 RID: 997
			NoShadow,
			// Token: 0x040003E6 RID: 998
			StaticShadow,
			// Token: 0x040003E7 RID: 999
			DynamicShadow,
			// Token: 0x040003E8 RID: 1000
			Count
		}
	}
}
