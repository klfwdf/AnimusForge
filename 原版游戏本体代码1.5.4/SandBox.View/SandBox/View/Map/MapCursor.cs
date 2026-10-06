using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map;

public class MapCursor
{
	private const string GameCursorValidDecalMaterialName = "map_cursor_valid_decal";

	private const string GameCursorInvalidDecalMaterialName = "map_cursor_invalid_decal";

	private const float CursorDecalBaseScale = 0.38f;

	private GameEntity _mapCursorDecalEntity;

	private Decal _mapCursorDecal;

	private MapScreen _mapScreen;

	private Material _gameCursorValidDecalMaterial;

	private Material _gameCursorInvalidDecalMaterial;

	private Vec3 _smoothRotationNormalStart;

	private Vec3 _smoothRotationNormalEnd;

	private Vec3 _smoothRotationNormalCurrent;

	private float _smoothRotationAlpha;

	private int _smallAtlasTextureIndex;

	private float _targetCircleRotationStartTime;

	private bool _gameCursorActive;

	private bool _anotherEntityHiglighted;

	private const float _navigationPositionCheckFrequency = 0.2f;

	private float _navigatablePositionCheckTimer;

	public void Initialize(MapScreen parentMapScreen)
	{
		_targetCircleRotationStartTime = 0f;
		_smallAtlasTextureIndex = 0;
		_mapScreen = parentMapScreen;
		Scene scene = (Campaign.Current.MapSceneWrapper as MapScene).Scene;
		_gameCursorValidDecalMaterial = Material.GetFromResource("map_cursor_valid_decal");
		_gameCursorInvalidDecalMaterial = Material.GetFromResource("map_cursor_invalid_decal");
		_mapCursorDecalEntity = GameEntity.CreateEmpty(scene);
		_mapCursorDecalEntity.Name = "tCursor";
		_mapCursorDecal = Decal.CreateDecal();
		_mapCursorDecal.SetMaterial(_gameCursorValidDecalMaterial);
		_mapCursorDecalEntity.AddComponent(_mapCursorDecal);
		scene.AddDecalInstance(_mapCursorDecal, "editor_set", deletable: true);
		MatrixFrame frame = _mapCursorDecalEntity.GetFrame();
		frame.Scale(new Vec3(0.38f, 0.38f, 0.38f));
		_mapCursorDecal.SetFrame(frame);
	}

	public void BeforeTick(float dt)
	{
		SceneLayer sceneLayer = _mapScreen.SceneLayer;
		Camera camera = _mapScreen.MapCameraView.Camera;
		float cameraDistance = _mapScreen.MapCameraView.CameraDistance;
		Vec3 rayBegin = Vec3.Zero;
		Vec3 rayEnd = Vec3.Zero;
		Vec2 viewportPoint = sceneLayer.SceneView.ScreenPointToViewportPoint(new Vec2(0.5f, 0.5f));
		camera.ViewportPointToWorldRay(ref rayBegin, ref rayEnd, viewportPoint);
		PathFaceRecord currentFace = default(PathFaceRecord);
		_mapScreen.GetCursorIntersectionPoint(ref rayBegin, ref rayEnd, out var _, out var intersectionPoint, ref currentFace, out var isOnland);
		sceneLayer.SceneView.ProjectedMousePositionOnGround(out intersectionPoint, out var groundNormal, mouseVisible: false, BodyFlags.None, checkOccludedSurface: false);
		if (_mapCursorDecalEntity != null)
		{
			_smallAtlasTextureIndex = GetCircleIndex();
			if (_navigatablePositionCheckTimer > 0.2f)
			{
				bool flag = false;
				if (!_anotherEntityHiglighted)
				{
					flag = NavigationHelper.CanPlayerNavigateToPosition(new CampaignVec2(intersectionPoint.AsVec2, isOnland), out var _);
				}
				_mapCursorDecal.SetMaterial((flag || _anotherEntityHiglighted) ? _gameCursorValidDecalMaterial : _gameCursorInvalidDecalMaterial);
				_mapCursorDecal.SetVectorArgument(0.166f, 1f, 0.166f * (float)_smallAtlasTextureIndex, 0f);
				SetAlpha(_anotherEntityHiglighted ? 0.2f : 1f);
			}
			MatrixFrame frame = _mapCursorDecalEntity.GetFrame();
			frame.origin = intersectionPoint;
			bool flag2 = !_smoothRotationNormalStart.IsNonZero;
			Vec3 v = ((cameraDistance > 160f) ? Vec3.Up : groundNormal);
			if (!_smoothRotationNormalEnd.NearlyEquals(in v))
			{
				_smoothRotationNormalStart = (flag2 ? v : _smoothRotationNormalCurrent);
				_smoothRotationNormalEnd = v;
				_smoothRotationNormalStart.Normalize();
				_smoothRotationNormalEnd.Normalize();
				_smoothRotationAlpha = 0f;
			}
			_smoothRotationNormalCurrent = Vec3.Lerp(_smoothRotationNormalStart, _smoothRotationNormalEnd, _smoothRotationAlpha);
			_smoothRotationAlpha += 12f * dt;
			_smoothRotationAlpha = MathF.Clamp(_smoothRotationAlpha, 0f, 1f);
			_smoothRotationNormalCurrent.Normalize();
			frame.rotation.f = camera.Frame.rotation.f;
			frame.rotation.f.z = 0f;
			frame.rotation.f.Normalize();
			frame.rotation.u = _smoothRotationNormalCurrent;
			frame.rotation.u.Normalize();
			frame.rotation.s = Vec3.CrossProduct(frame.rotation.u, frame.rotation.f);
			float value = (cameraDistance + 80f) * (cameraDistance + 80f) / 10000f;
			value = MathF.Clamp(value, 0.2f, 38f);
			frame.Scale(Vec3.One * value);
			_mapCursorDecalEntity.SetGlobalFrame(in frame);
			_anotherEntityHiglighted = false;
		}
		if (_navigatablePositionCheckTimer > 0.2f)
		{
			_navigatablePositionCheckTimer = 0f;
		}
		_navigatablePositionCheckTimer += dt;
	}

	public void SetVisible(bool value)
	{
		if (value)
		{
			if (!_gameCursorActive || _mapScreen.GetMouseVisible())
			{
				_mapScreen.SetMouseVisible(value: false);
				_mapCursorDecalEntity.SetVisibilityExcludeParents(visible: true);
				if (_mapScreen.CurrentVisualOfTooltip != null)
				{
					_mapScreen.RemoveMapTooltip();
				}
				Vec2 resolution = Input.Resolution;
				Input.SetMousePosition((int)(resolution.X / 2f), (int)(resolution.Y / 2f));
				_gameCursorActive = true;
			}
		}
		else
		{
			bool flag = !(GameStateManager.Current.ActiveState is MapState) || (!_mapScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) && !_mapScreen.SceneLayer.Input.IsKeyDown(InputKey.MiddleMouseButton));
			if (_gameCursorActive || _mapScreen.GetMouseVisible() != flag)
			{
				_mapScreen.SetMouseVisible(flag);
				_mapCursorDecalEntity.SetVisibilityExcludeParents(visible: false);
				_gameCursorActive = false;
			}
		}
	}

	protected internal void OnMapTerrainClick()
	{
		_targetCircleRotationStartTime = MBCommon.GetApplicationTime();
	}

	protected internal void OnAnotherEntityHighlighted()
	{
		_anotherEntityHiglighted = true;
	}

	protected internal void SetAlpha(float alpha)
	{
		Color color = Color.FromUint(_mapCursorDecal.GetFactor1());
		Color color2 = new Color(color.Red, color.Green, color.Blue, alpha);
		_mapCursorDecal.SetFactor1(color2.ToUnsignedInteger());
	}

	private int GetCircleIndex()
	{
		int num = (int)((MBCommon.GetApplicationTime() - _targetCircleRotationStartTime) / 0.033f);
		if (num >= 10)
		{
			return 0;
		}
		int num2 = num % 10;
		if (num2 >= 5)
		{
			num2 = 10 - num2 - 1;
		}
		return num2;
	}
}
