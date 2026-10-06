using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

public class BenchmarkScreen : ScreenBase
{
	private SceneView _sceneView;

	private Scene _scene;

	private Camera _camera;

	private MatrixFrame _cameraFrame;

	private Timer _cameraTimer;

	private const string _parentEntityName = "LocationEntityParent";

	private const string _sceneName = "benchmark";

	private const string _xmlPath = "../../../Tools/TestAutomation/Attachments/benchmark_scene_performance.xml";

	private List<GameEntity> _cameraLocationEntities;

	private int _currentEntityIndex = -1;

	private PerformanceAnalyzer _analyzer = new PerformanceAnalyzer();

	protected override void OnActivate()
	{
		base.OnActivate();
		_scene = Scene.CreateNewScene();
		_scene.SetName("BenchmarkScreen");
		_scene.Read("benchmark");
		_cameraFrame = _scene.ReadAndCalculateInitialCamera();
		_scene.SetUseConstantTime(value: true);
		_sceneView = SceneView.CreateSceneView();
		_sceneView.SetScene(_scene);
		_sceneView.SetSceneUsesShadows(value: true);
		_camera = Camera.CreateCamera();
		UpdateCamera();
		_cameraTimer = new Timer(MBCommon.GetApplicationTime() - 5f, 5f);
		GameEntity gameEntity = _scene.FindEntityWithName("LocationEntityParent");
		_cameraLocationEntities = gameEntity.GetChildren().ToList();
	}

	public void UpdateCamera()
	{
		_camera.Frame = _cameraFrame;
		_sceneView.SetCamera(_camera);
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_scene = null;
		_analyzer = null;
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_cameraTimer.Check(MBCommon.GetApplicationTime()))
		{
			_currentEntityIndex++;
			if (_currentEntityIndex >= _cameraLocationEntities.Count)
			{
				_analyzer.FinalizeAndWrite("../../../Tools/TestAutomation/Attachments/benchmark_scene_performance.xml");
				ScreenManager.PopScreen();
				return;
			}
			GameEntity gameEntity = _cameraLocationEntities[_currentEntityIndex];
			_cameraFrame = gameEntity.GetGlobalFrame();
			UpdateCamera();
			_analyzer.Start(gameEntity.Name);
			_cameraTimer.Reset(MBCommon.GetApplicationTime());
		}
		_analyzer.Tick(dt);
	}
}
