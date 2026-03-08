using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x02000008 RID: 8
	public class TwoDimensionEnginePlatform : ITwoDimensionPlatform
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600003C RID: 60 RVA: 0x00002B24 File Offset: 0x00000D24
		float ITwoDimensionPlatform.Width
		{
			get
			{
				return Screen.RealScreenResolutionWidth * ScreenManager.UsableArea.X;
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600003D RID: 61 RVA: 0x00002B44 File Offset: 0x00000D44
		float ITwoDimensionPlatform.Height
		{
			get
			{
				return Screen.RealScreenResolutionHeight * ScreenManager.UsableArea.Y;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00002B64 File Offset: 0x00000D64
		float ITwoDimensionPlatform.ReferenceWidth
		{
			get
			{
				return 1920f;
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600003F RID: 63 RVA: 0x00002B6B File Offset: 0x00000D6B
		float ITwoDimensionPlatform.ReferenceHeight
		{
			get
			{
				return 1080f;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000040 RID: 64 RVA: 0x00002B72 File Offset: 0x00000D72
		float ITwoDimensionPlatform.ApplicationTime
		{
			get
			{
				return Time.ApplicationTime;
			}
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002B79 File Offset: 0x00000D79
		public TwoDimensionEnginePlatform(TwoDimensionView view)
		{
			this._view = view;
			this._textMaterials = new Dictionary<Texture, Material>();
			this._soundEvents = new Dictionary<string, SoundEvent>();
			((ITwoDimensionPlatform)this).ResetScissors();
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002BA4 File Offset: 0x00000DA4
		private WeakMaterial GetOrCreateMaterial(Texture mainTexture, Texture overlayTexture, bool useCustomMesh, bool useOverlayTextureAlphaAsMask)
		{
			WeakMaterial orCreateMaterial = this._view.GetOrCreateMaterial(mainTexture, overlayTexture);
			orCreateMaterial.SetTexture(Material.MBTextureType.DiffuseMap, mainTexture);
			if (overlayTexture != null)
			{
				orCreateMaterial.AddMaterialShaderFlag("use_overlay_texture", true);
				if (useOverlayTextureAlphaAsMask)
				{
					orCreateMaterial.AddMaterialShaderFlag("use_overlay_texture_alpha_as_mask", true);
				}
				orCreateMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, overlayTexture);
			}
			if (useCustomMesh)
			{
				orCreateMaterial.AddMaterialShaderFlag("use_custom_mesh", true);
			}
			return orCreateMaterial;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00002C0C File Offset: 0x00000E0C
		private Material GetOrCreateTextMaterial(Texture texture)
		{
			Material result;
			if (this._textMaterials.TryGetValue(texture, out result))
			{
				return result;
			}
			Material material = Material.GetFromResource("two_dimension_text_material").CreateCopy();
			material.SetTexture(Material.MBTextureType.DiffuseMap, texture);
			this._textMaterials.Add(texture, material);
			return material;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00002C54 File Offset: 0x00000E54
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.DrawImage(SimpleMaterial material, in ImageDrawObject imageDrawObject, int layer)
		{
			Texture texture = material.Texture;
			if (texture == null)
			{
				return;
			}
			Texture texture2 = ((EngineTexture)texture.PlatformTexture).Texture;
			if (texture2 == null)
			{
				return;
			}
			if (texture2.IsReleased)
			{
				Debug.FailedAssert("Trying to render a released texture", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\TwoDimensionEnginePlatform.cs", "DrawImage", 100);
				return;
			}
			WeakMaterial weakMaterial = WeakMaterial.Invalid;
			Rectangle2D rectangle = imageDrawObject.Rectangle;
			MatrixFrame cachedVisualMatrixFrame = rectangle.GetCachedVisualMatrixFrame();
			Vec2 zero = Vec2.Zero;
			Vec2 zero2 = Vec2.Zero;
			if (material.OverlayEnabled)
			{
				Texture texture3 = ((EngineTexture)material.OverlayTexture.PlatformTexture).Texture;
				if (texture3.IsReleased)
				{
					Debug.FailedAssert("Trying to render a released texture", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\TwoDimensionEnginePlatform.cs", "DrawImage", 117);
					return;
				}
				weakMaterial = this.GetOrCreateMaterial(texture2, texture3, true, material.UseOverlayAlphaAsMask);
				rectangle = imageDrawObject.Rectangle;
				Vector2 visualScale = rectangle.GetVisualScale();
				float num = 1f / Mathf.Abs(visualScale.X);
				float num2 = 1f / Mathf.Abs(visualScale.Y);
				zero.x = material.OverlayTextureWidth * num;
				zero.y = material.OverlayTextureHeight * num2;
				zero2.x = material.OverlayXOffset * num;
				zero2.y = material.OverlayYOffset * num2;
			}
			if (weakMaterial == WeakMaterial.Invalid)
			{
				weakMaterial = this.GetOrCreateMaterial(texture2, null, true, false);
			}
			uint color = material.Color.ToUnsignedInteger();
			float colorFactor = material.ColorFactor;
			float alphaFactor = material.AlphaFactor;
			float hueFactor = material.HueFactor;
			float saturationFactor = material.SaturationFactor;
			float valueFactor = material.ValueFactor;
			Vec2 zero3 = Vec2.Zero;
			float clipCircleRadius = 0f;
			float clipCircleSmoothingRadius = 0f;
			if (material.CircularMaskingEnabled)
			{
				zero3 = new Vec2(material.CircularMaskingCenter.X, material.CircularMaskingCenter.Y);
				clipCircleRadius = material.CircularMaskingRadius;
				clipCircleSmoothingRadius = material.CircularMaskingSmoothingRadius;
			}
			TwoDimensionMeshDrawData meshDrawData = default(TwoDimensionMeshDrawData);
			meshDrawData.MatrixFrame = cachedVisualMatrixFrame;
			meshDrawData.ClipRectInfo = new Vec3(this._activeScissor.X, this._activeScissor.Y, this._activeScissor.X2, this._activeScissor.Y2);
			meshDrawData.Uvs = imageDrawObject.Uvs;
			meshDrawData.SpriteSize = new Vec2((float)material.Texture.Width, (float)material.Texture.Height);
			meshDrawData.ScreenSize = Screen.RealScreenResolution;
			meshDrawData.ScreenScale = new Vec2(imageDrawObject.Scale, imageDrawObject.Scale);
			SpriteNinePatchParameters ninePatchParameters = material.NinePatchParameters;
			if (ninePatchParameters.IsValid)
			{
				meshDrawData.NinePatchBorders = new Vec3((float)ninePatchParameters.LeftWidth, (float)ninePatchParameters.TopHeight, (float)ninePatchParameters.RightWidth, (float)ninePatchParameters.BottomHeight);
			}
			meshDrawData.Layer = layer;
			meshDrawData.ClipCircleCenter = zero3;
			meshDrawData.ClipCircleRadius = clipCircleRadius;
			meshDrawData.ClipCircleSmoothingRadius = clipCircleSmoothingRadius;
			meshDrawData.Color = color;
			meshDrawData.ColorFactor = colorFactor;
			meshDrawData.AlphaFactor = alphaFactor;
			meshDrawData.HueFactor = hueFactor;
			meshDrawData.SaturationFactor = saturationFactor;
			meshDrawData.ValueFactor = valueFactor;
			meshDrawData.OverlayScale = zero;
			meshDrawData.OverlayOffset = zero2;
			if (!MBDebug.DisableAllUI)
			{
				this._view.CreateMeshFromDescription(weakMaterial, meshDrawData);
			}
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00002F8C File Offset: 0x0000118C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void ITwoDimensionPlatform.DrawText(TextMaterial material, in TextDrawObject textDrawObject, int layer)
		{
			uint color = material.Color.ToUnsignedInteger();
			Texture texture = material.Texture;
			if (texture != null)
			{
				Texture texture2 = ((EngineTexture)texture.PlatformTexture).Texture;
				if (texture2 != null)
				{
					Material orCreateTextMaterial = this.GetOrCreateTextMaterial(texture2);
					TwoDimensionTextMeshDrawData meshDrawData = default(TwoDimensionTextMeshDrawData);
					Rectangle2D rectangle = textDrawObject.Rectangle;
					meshDrawData.MatrixFrame = rectangle.GetCachedVisualMatrixFrame();
					meshDrawData.ClipRectInfo = new Vec3(this._activeScissor.X, this._activeScissor.Y, this._activeScissor.X2, this._activeScissor.Y2);
					meshDrawData.ScreenWidth = Screen.RealScreenResolutionWidth;
					meshDrawData.ScreenHeight = Screen.RealScreenResolutionHeight;
					meshDrawData.Color = color;
					meshDrawData.ScaleFactor = 1.5f / material.ScaleFactor;
					meshDrawData.SmoothingConstant = material.SmoothingConstant;
					meshDrawData.GlowColor = material.GlowColor.ToUnsignedInteger();
					meshDrawData.OutlineColor = material.OutlineColor.ToVec3();
					meshDrawData.OutlineAmount = material.OutlineAmount;
					meshDrawData.GlowRadius = material.GlowRadius;
					meshDrawData.Blur = material.Blur;
					meshDrawData.ShadowOffset = material.ShadowOffset;
					meshDrawData.ShadowAngle = material.ShadowAngle;
					meshDrawData.ColorFactor = material.ColorFactor;
					meshDrawData.AlphaFactor = material.AlphaFactor;
					meshDrawData.HueFactor = material.HueFactor;
					meshDrawData.SaturationFactor = material.SaturationFactor;
					meshDrawData.ValueFactor = material.ValueFactor;
					meshDrawData.Layer = layer;
					meshDrawData.HashCode1 = textDrawObject.HashCode1;
					meshDrawData.HashCode2 = textDrawObject.HashCode2;
					if (!MBDebug.DisableAllUI && !this._view.CreateTextMeshFromCache(orCreateTextMaterial, meshDrawData))
					{
						this._view.CreateTextMeshFromDescription(textDrawObject.Text_Vertices, textDrawObject.Text_TextureCoordinates, textDrawObject.Text_Indices, textDrawObject.Text_Indices.Length, orCreateTextMaterial, meshDrawData);
					}
				}
			}
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003180 File Offset: 0x00001380
		void ITwoDimensionPlatform.OnFrameBegin()
		{
			this.Reset();
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00003188 File Offset: 0x00001388
		void ITwoDimensionPlatform.OnFrameEnd()
		{
			this.Reset();
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003190 File Offset: 0x00001390
		void ITwoDimensionPlatform.Clear()
		{
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003192 File Offset: 0x00001392
		void ITwoDimensionPlatform.SetScissor(ScissorTestInfo scissorTestInfo)
		{
			this._activeScissor = scissorTestInfo;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x0000319B File Offset: 0x0000139B
		void ITwoDimensionPlatform.ResetScissors()
		{
			this._activeScissor = new ScissorTestInfo(0f, 0f, Screen.RealScreenResolutionWidth, Screen.RealScreenResolutionHeight);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x000031BC File Offset: 0x000013BC
		void ITwoDimensionPlatform.PlaySound(string soundName)
		{
			SoundEvent.PlaySound2D("event:/ui/" + soundName);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000031D0 File Offset: 0x000013D0
		void ITwoDimensionPlatform.CreateSoundEvent(string soundName)
		{
			if (!this._soundEvents.ContainsKey(soundName))
			{
				SoundEvent value = SoundEvent.CreateEventFromString("event:/ui/" + soundName, null);
				this._soundEvents.Add(soundName, value);
			}
		}

		// Token: 0x0600004D RID: 77 RVA: 0x0000320C File Offset: 0x0000140C
		void ITwoDimensionPlatform.PlaySoundEvent(string soundName)
		{
			SoundEvent soundEvent;
			if (this._soundEvents.TryGetValue(soundName, out soundEvent))
			{
				soundEvent.Play();
			}
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00003230 File Offset: 0x00001430
		void ITwoDimensionPlatform.StopAndRemoveSoundEvent(string soundName)
		{
			SoundEvent soundEvent;
			if (this._soundEvents.TryGetValue(soundName, out soundEvent))
			{
				soundEvent.Stop();
				this._soundEvents.Remove(soundName);
			}
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003260 File Offset: 0x00001460
		void ITwoDimensionPlatform.OpenOnScreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
		{
			Input.IsOnScreenKeyboardActive = ScreenManager.OnPlatformScreenKeyboardRequested(initialText, descriptionText, maxLength, keyboardTypeEnum);
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00003271 File Offset: 0x00001471
		void ITwoDimensionPlatform.BeginDebugPanel(string panelTitle)
		{
			Imgui.BeginMainThreadScope();
			Imgui.Begin(panelTitle);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x0000327E File Offset: 0x0000147E
		void ITwoDimensionPlatform.EndDebugPanel()
		{
			Imgui.End();
			Imgui.EndMainThreadScope();
		}

		// Token: 0x06000052 RID: 82 RVA: 0x0000328A File Offset: 0x0000148A
		void ITwoDimensionPlatform.DrawDebugText(string text)
		{
			Imgui.Text(text);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003292 File Offset: 0x00001492
		bool ITwoDimensionPlatform.DrawDebugTreeNode(string text)
		{
			return Imgui.TreeNode(text);
		}

		// Token: 0x06000054 RID: 84 RVA: 0x0000329A File Offset: 0x0000149A
		void ITwoDimensionPlatform.PopDebugTreeNode()
		{
			Imgui.TreePop();
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000032A1 File Offset: 0x000014A1
		void ITwoDimensionPlatform.DrawCheckbox(string label, ref bool isChecked)
		{
			Imgui.Checkbox(label, ref isChecked);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x000032AB File Offset: 0x000014AB
		bool ITwoDimensionPlatform.IsDebugItemHovered()
		{
			return Imgui.IsItemHovered();
		}

		// Token: 0x06000057 RID: 87 RVA: 0x000032B2 File Offset: 0x000014B2
		bool ITwoDimensionPlatform.IsDebugModeEnabled()
		{
			return UIConfig.DebugModeEnabled;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000032B9 File Offset: 0x000014B9
		private void Reset()
		{
		}

		// Token: 0x0400000B RID: 11
		private TwoDimensionView _view;

		// Token: 0x0400000C RID: 12
		private ScissorTestInfo _activeScissor;

		// Token: 0x0400000D RID: 13
		private Dictionary<Texture, Material> _textMaterials;

		// Token: 0x0400000E RID: 14
		private Dictionary<string, SoundEvent> _soundEvents;
	}
}
