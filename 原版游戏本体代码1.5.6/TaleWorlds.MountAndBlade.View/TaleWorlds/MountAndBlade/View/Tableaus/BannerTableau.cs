using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Tableaus;

public class BannerTableau
{
	private static int _tableauIndex;

	private bool _isFinalized;

	private bool _isEnabled;

	private bool _isNineGrid;

	private bool _isDirty;

	private Banner _banner;

	private int _latestWidth = -1;

	private int _latestHeight = -1;

	private int _tableauSizeX;

	private int _tableauSizeY;

	private float RenderScale = 1f;

	private float _customRenderScale = 1f;

	private Scene _scene;

	private Camera _defaultCamera;

	private Camera _nineGridCamera;

	private MetaMesh _currentMultiMesh;

	private GameEntity _currentMeshEntity;

	private int _meshIndexToUpdate;

	public Texture Texture { get; private set; }

	internal Camera CurrentCamera
	{
		get
		{
			if (!_isNineGrid)
			{
				return _defaultCamera;
			}
			return _nineGridCamera;
		}
	}

	private TableauView View
	{
		get
		{
			if (Texture != null)
			{
				return Texture.TableauView;
			}
			return null;
		}
	}

	public BannerTableau()
	{
		SetEnabled(enabled: true);
		FirstTimeInit();
	}

	public void OnTick(float dt)
	{
		if (_isEnabled && !_isFinalized)
		{
			Refresh();
			View?.SetDoNotRenderThisFrame(value: false);
		}
	}

	private void FirstTimeInit()
	{
		_scene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
		_scene.DisableStaticShadows(value: true);
		_scene.SetName("BannerTableau.Scene");
		_scene.SetDefaultLighting();
		_defaultCamera = BannerTextureCreator.CreateDefaultBannerCamera();
		_nineGridCamera = BannerTextureCreator.CreateNineGridBannerCamera();
		_isDirty = true;
	}

	private void Refresh()
	{
		if (!_isDirty)
		{
			return;
		}
		if (_currentMeshEntity != null)
		{
			_scene.RemoveEntity(_currentMeshEntity, 111);
		}
		if (_banner != null)
		{
			MatrixFrame placementFrame = MatrixFrame.Identity;
			if (Banner.IsValidBannerCode(_banner.BannerCode))
			{
				_currentMultiMesh = _banner.ConvertToMultiMesh();
				_currentMeshEntity = _scene.AddItemEntity(ref placementFrame, _currentMultiMesh);
				_currentMeshEntity.ManualInvalidate();
				_currentMultiMesh.ManualInvalidate();
			}
			else
			{
				Debug.FailedAssert("Banner code is not valid: " + _banner.BannerCode, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\BannerTableau.cs", "Refresh", 109);
			}
			_isDirty = false;
		}
	}

	private void SetEnabled(bool enabled)
	{
		_isEnabled = enabled;
		View?.SetEnable(_isEnabled);
	}

	public void SetTargetSize(int width, int height)
	{
		_latestWidth = width;
		_latestHeight = height;
		if (width <= 0 || height <= 0)
		{
			_tableauSizeX = 10;
			_tableauSizeY = 10;
		}
		else
		{
			RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
			_tableauSizeX = (int)((float)width * _customRenderScale * RenderScale);
			_tableauSizeY = (int)((float)height * _customRenderScale * RenderScale);
		}
		View?.SetEnable(value: false);
		View?.AddClearTask(clearOnlySceneview: true);
		Texture?.Release();
		Texture = TableauView.AddTableau($"BannerTableau_{_tableauIndex++}", BannerTableauContinuousRenderFunction, _scene, _tableauSizeX, _tableauSizeY);
		Texture.TableauView.SetSceneUsesContour(value: false);
	}

	public void SetBannerCode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			_banner = null;
		}
		else
		{
			_banner = new Banner(value);
		}
		_isDirty = true;
	}

	public void OnFinalize()
	{
		if (!_isFinalized)
		{
			_scene?.ClearDecals();
			_scene?.ClearAll();
			_scene?.ManualInvalidate();
			_scene = null;
			View?.SetEnable(value: false);
			Texture?.Release();
			Texture = null;
			_defaultCamera?.ReleaseCamera();
			_defaultCamera = null;
			_nineGridCamera?.ReleaseCamera();
			_nineGridCamera = null;
		}
		_isFinalized = true;
	}

	public void SetCustomRenderScale(float value)
	{
		if (!_customRenderScale.ApproximatelyEqualsTo(value))
		{
			_customRenderScale = value;
			if (_latestWidth != -1 && _latestHeight != -1)
			{
				SetTargetSize(_latestWidth, _latestHeight);
			}
		}
	}

	internal void BannerTableauContinuousRenderFunction(Texture sender, EventArgs e)
	{
		Scene scene = (Scene)sender.UserData;
		TableauView tableauView = sender.TableauView;
		if (scene == null)
		{
			tableauView.SetContinuousRendering(value: false);
			tableauView.SetDeleteAfterRendering(value: true);
			return;
		}
		scene.EnsurePostfxSystem();
		scene.SetDofMode(mode: false);
		scene.SetMotionBlurMode(mode: false);
		scene.SetBloom(mode: false);
		scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.31f);
		tableauView.SetRenderWithPostfx(value: false);
		tableauView.SetScene(scene);
		tableauView.SetCamera(CurrentCamera);
		tableauView.SetSceneUsesSkybox(value: false);
		tableauView.SetDeleteAfterRendering(value: false);
		tableauView.SetContinuousRendering(value: true);
		tableauView.SetDoNotRenderThisFrame(value: true);
		tableauView.SetClearColor(0u);
	}

	public void SetIsNineGrid(bool value)
	{
		_isNineGrid = value;
		_isDirty = true;
	}

	public void SetMeshIndexToUpdate(int value)
	{
		_meshIndexToUpdate = value;
	}

	public void SetUpdatePositionValueManual(Vec2 value)
	{
		if (_currentMultiMesh.MeshCount >= 1 && _meshIndexToUpdate >= 0 && _meshIndexToUpdate < _currentMultiMesh.MeshCount)
		{
			Mesh meshAtIndex = _currentMultiMesh.GetMeshAtIndex(_meshIndexToUpdate);
			MatrixFrame localFrame = meshAtIndex.GetLocalFrame();
			localFrame.origin.x = 0f;
			localFrame.origin.y = 0f;
			localFrame.origin.x += value.X / 1528f;
			localFrame.origin.y -= value.Y / 1528f;
			meshAtIndex.SetLocalFrame(localFrame);
		}
	}

	public void SetUpdateSizeValueManual(Vec2 value)
	{
		if (_currentMultiMesh.MeshCount >= 1 && _meshIndexToUpdate >= 0 && _meshIndexToUpdate < _currentMultiMesh.MeshCount)
		{
			Mesh meshAtIndex = _currentMultiMesh.GetMeshAtIndex(_meshIndexToUpdate);
			MatrixFrame localFrame = meshAtIndex.GetLocalFrame();
			float x = value.X / 1528f / meshAtIndex.GetBoundingBoxWidth();
			float y = value.Y / 1528f / meshAtIndex.GetBoundingBoxHeight();
			Vec3 eulerAngles = localFrame.rotation.GetEulerAngles();
			localFrame.rotation = Mat3.Identity;
			localFrame.rotation.ApplyEulerAngles(in eulerAngles);
			localFrame.rotation.ApplyScaleLocal(new Vec3(x, y, 1f));
			meshAtIndex.SetLocalFrame(localFrame);
		}
	}

	public void SetUpdateRotationValueManual((float, bool) value)
	{
		if (_currentMultiMesh.MeshCount >= 1 && _meshIndexToUpdate >= 0 && _meshIndexToUpdate < _currentMultiMesh.MeshCount)
		{
			Mesh meshAtIndex = _currentMultiMesh.GetMeshAtIndex(_meshIndexToUpdate);
			MatrixFrame localFrame = meshAtIndex.GetLocalFrame();
			float a = value.Item1 * 2f * System.MathF.PI;
			Vec3 scaleAmountXYZ = localFrame.rotation.GetScaleVector();
			localFrame.rotation = Mat3.Identity;
			localFrame.rotation.RotateAboutUp(a);
			localFrame.rotation.ApplyScaleLocal(in scaleAmountXYZ);
			if (value.Item2)
			{
				localFrame.rotation.RotateAboutForward(System.MathF.PI);
			}
			meshAtIndex.SetLocalFrame(localFrame);
		}
	}
}
