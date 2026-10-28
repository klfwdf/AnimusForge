using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI;

public class TwoDimensionEnginePlatform : ITwoDimensionPlatform
{
	private TwoDimensionView _view;

	private ScissorTestInfo _activeScissor;

	private Dictionary<Texture, Material> _textMaterials;

	private Dictionary<string, SoundEvent> _soundEvents;

	float ITwoDimensionPlatform.Width => Screen.RealScreenResolutionWidth * ScreenManager.UsableArea.X;

	float ITwoDimensionPlatform.Height => Screen.RealScreenResolutionHeight * ScreenManager.UsableArea.Y;

	float ITwoDimensionPlatform.ReferenceWidth => 1920f;

	float ITwoDimensionPlatform.ReferenceHeight => 1080f;

	float ITwoDimensionPlatform.ApplicationTime => Time.ApplicationTime;

	public TwoDimensionEnginePlatform(TwoDimensionView view)
	{
		_view = view;
		_textMaterials = new Dictionary<Texture, Material>();
		_soundEvents = new Dictionary<string, SoundEvent>();
		((ITwoDimensionPlatform)this).ResetScissors();
	}

	private WeakMaterial GetOrCreateMaterial(Texture mainTexture, Texture overlayTexture, bool useCustomMesh, bool useOverlayTextureAlphaAsMask)
	{
		WeakMaterial orCreateMaterial = _view.GetOrCreateMaterial(mainTexture, overlayTexture);
		orCreateMaterial.SetTexture(Material.MBTextureType.DiffuseMap, mainTexture);
		if (overlayTexture != null)
		{
			orCreateMaterial.AddMaterialShaderFlag("use_overlay_texture", showErrors: true);
			if (useOverlayTextureAlphaAsMask)
			{
				orCreateMaterial.AddMaterialShaderFlag("use_overlay_texture_alpha_as_mask", showErrors: true);
			}
			orCreateMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, overlayTexture);
		}
		if (useCustomMesh)
		{
			orCreateMaterial.AddMaterialShaderFlag("use_custom_mesh", showErrors: true);
		}
		return orCreateMaterial;
	}

	private Material GetOrCreateTextMaterial(Texture texture)
	{
		if (_textMaterials.TryGetValue(texture, out var value))
		{
			return value;
		}
		Material material = Material.GetFromResource("two_dimension_text_material").CreateCopy();
		material.SetTexture(Material.MBTextureType.DiffuseMap, texture);
		_textMaterials.Add(texture, material);
		return material;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void ITwoDimensionPlatform.DrawImage(SimpleMaterial material, in ImageDrawObject imageDrawObject, int layer)
	{
		TaleWorlds.TwoDimension.Texture texture = material.Texture;
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
		MatrixFrame cachedVisualMatrixFrame = imageDrawObject.Rectangle.GetCachedVisualMatrixFrame();
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
			weakMaterial = GetOrCreateMaterial(texture2, texture3, useCustomMesh: true, material.UseOverlayAlphaAsMask);
			Vector2 visualScale = imageDrawObject.Rectangle.GetVisualScale();
			float num = 1f / Mathf.Abs(visualScale.X);
			float num2 = 1f / Mathf.Abs(visualScale.Y);
			zero.x = material.OverlayTextureWidth * num;
			zero.y = material.OverlayTextureHeight * num2;
			zero2.x = material.OverlayXOffset * num;
			zero2.y = material.OverlayYOffset * num2;
		}
		if (weakMaterial == WeakMaterial.Invalid)
		{
			weakMaterial = GetOrCreateMaterial(texture2, null, useCustomMesh: true, useOverlayTextureAlphaAsMask: false);
		}
		uint color = material.Color.ToUnsignedInteger();
		float colorFactor = material.ColorFactor;
		float alphaFactor = material.AlphaFactor;
		float hueFactor = material.HueFactor;
		float saturationFactor = material.SaturationFactor;
		float valueFactor = material.ValueFactor;
		Vec2 clipCircleCenter = Vec2.Zero;
		float clipCircleRadius = 0f;
		float clipCircleSmoothingRadius = 0f;
		if (material.CircularMaskingEnabled)
		{
			clipCircleCenter = new Vec2(material.CircularMaskingCenter.X, material.CircularMaskingCenter.Y);
			clipCircleRadius = material.CircularMaskingRadius;
			clipCircleSmoothingRadius = material.CircularMaskingSmoothingRadius;
		}
		TwoDimensionMeshDrawData meshDrawData = new TwoDimensionMeshDrawData
		{
			MatrixFrame = cachedVisualMatrixFrame,
			ClipRectInfo = new Vec3(_activeScissor.X, _activeScissor.Y, _activeScissor.X2, _activeScissor.Y2),
			Uvs = imageDrawObject.Uvs,
			SpriteSize = new Vec2(material.Texture.Width, material.Texture.Height),
			ScreenSize = Screen.RealScreenResolution,
			ScreenScale = new Vec2(imageDrawObject.Scale, imageDrawObject.Scale)
		};
		SpriteNinePatchParameters ninePatchParameters = material.NinePatchParameters;
		if (ninePatchParameters.IsValid)
		{
			meshDrawData.NinePatchBorders = new Vec3(ninePatchParameters.LeftWidth, ninePatchParameters.TopHeight, ninePatchParameters.RightWidth, ninePatchParameters.BottomHeight);
		}
		meshDrawData.Layer = layer;
		meshDrawData.ClipCircleCenter = clipCircleCenter;
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
			_view.CreateMeshFromDescription(weakMaterial, meshDrawData);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void ITwoDimensionPlatform.DrawText(TextMaterial material, in TextDrawObject textDrawObject, int layer)
	{
		uint color = material.Color.ToUnsignedInteger();
		TaleWorlds.TwoDimension.Texture texture = material.Texture;
		if (texture == null)
		{
			return;
		}
		Texture texture2 = ((EngineTexture)texture.PlatformTexture).Texture;
		if (texture2 != null)
		{
			Material orCreateTextMaterial = GetOrCreateTextMaterial(texture2);
			TwoDimensionTextMeshDrawData meshDrawData = new TwoDimensionTextMeshDrawData
			{
				MatrixFrame = textDrawObject.Rectangle.GetCachedVisualMatrixFrame(),
				ClipRectInfo = new Vec3(_activeScissor.X, _activeScissor.Y, _activeScissor.X2, _activeScissor.Y2),
				ScreenWidth = Screen.RealScreenResolutionWidth,
				ScreenHeight = Screen.RealScreenResolutionHeight,
				Color = color,
				ScaleFactor = 1.5f / material.ScaleFactor,
				SmoothingConstant = material.SmoothingConstant,
				GlowColor = material.GlowColor.ToUnsignedInteger(),
				OutlineColor = material.OutlineColor.ToVec3(),
				OutlineAmount = material.OutlineAmount,
				GlowRadius = material.GlowRadius,
				Blur = material.Blur,
				ShadowOffset = material.ShadowOffset,
				ShadowAngle = material.ShadowAngle,
				ColorFactor = material.ColorFactor,
				AlphaFactor = material.AlphaFactor,
				HueFactor = material.HueFactor,
				SaturationFactor = material.SaturationFactor,
				ValueFactor = material.ValueFactor,
				Layer = layer,
				HashCode1 = textDrawObject.HashCode1,
				HashCode2 = textDrawObject.HashCode2
			};
			if (!MBDebug.DisableAllUI && !_view.CreateTextMeshFromCache(orCreateTextMaterial, meshDrawData))
			{
				_view.CreateTextMeshFromDescription(textDrawObject.Text_Vertices, textDrawObject.Text_TextureCoordinates, textDrawObject.Text_Indices, textDrawObject.Text_Indices.Length, orCreateTextMaterial, meshDrawData);
			}
		}
	}

	void ITwoDimensionPlatform.OnFrameBegin()
	{
		Reset();
	}

	void ITwoDimensionPlatform.OnFrameEnd()
	{
		Reset();
	}

	void ITwoDimensionPlatform.Clear()
	{
	}

	void ITwoDimensionPlatform.SetScissor(ScissorTestInfo scissorTestInfo)
	{
		_activeScissor = scissorTestInfo;
	}

	void ITwoDimensionPlatform.ResetScissors()
	{
		_activeScissor = new ScissorTestInfo(0f, 0f, Screen.RealScreenResolutionWidth, Screen.RealScreenResolutionHeight);
	}

	void ITwoDimensionPlatform.PlaySound(string soundName)
	{
		SoundEvent.PlaySound2D("event:/ui/" + soundName);
	}

	void ITwoDimensionPlatform.CreateSoundEvent(string soundName)
	{
		if (!_soundEvents.ContainsKey(soundName))
		{
			SoundEvent value = SoundEvent.CreateEventFromString("event:/ui/" + soundName, null);
			_soundEvents.Add(soundName, value);
		}
	}

	void ITwoDimensionPlatform.PlaySoundEvent(string soundName)
	{
		if (_soundEvents.TryGetValue(soundName, out var value))
		{
			value.Play();
		}
	}

	void ITwoDimensionPlatform.StopAndRemoveSoundEvent(string soundName)
	{
		if (_soundEvents.TryGetValue(soundName, out var value))
		{
			value.Stop();
			_soundEvents.Remove(soundName);
		}
	}

	void ITwoDimensionPlatform.OpenOnScreenKeyboard(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
	{
		Input.IsOnScreenKeyboardActive = ScreenManager.OnPlatformScreenKeyboardRequested(initialText, descriptionText, maxLength, keyboardTypeEnum);
	}

	void ITwoDimensionPlatform.BeginDebugPanel(string panelTitle)
	{
		Imgui.BeginMainThreadScope();
		Imgui.Begin(panelTitle);
	}

	void ITwoDimensionPlatform.EndDebugPanel()
	{
		Imgui.End();
		Imgui.EndMainThreadScope();
	}

	void ITwoDimensionPlatform.DrawDebugText(string text)
	{
		Imgui.Text(text);
	}

	bool ITwoDimensionPlatform.DrawDebugTreeNode(string text)
	{
		return Imgui.TreeNode(text);
	}

	void ITwoDimensionPlatform.PopDebugTreeNode()
	{
		Imgui.TreePop();
	}

	void ITwoDimensionPlatform.DrawCheckbox(string label, ref bool isChecked)
	{
		Imgui.Checkbox(label, ref isChecked);
	}

	bool ITwoDimensionPlatform.IsDebugItemHovered()
	{
		return Imgui.IsItemHovered();
	}

	bool ITwoDimensionPlatform.IsDebugModeEnabled()
	{
		return UIConfig.DebugModeEnabled;
	}

	private void Reset()
	{
	}
}
