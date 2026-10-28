using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

public class VisualTestsScreen : ScreenBase
{
	public enum CameraPointTestType
	{
		Final,
		Albedo,
		Normal,
		Specular,
		AO,
		OnlyAmbient,
		OnlyDirect
	}

	public class CameraPoint
	{
		public MatrixFrame CamFrame;

		public string CameraName;

		public List<CameraPointTestType> TestTypes;

		public CameraPoint()
		{
			TestTypes = new List<CameraPointTestType>();
			CamFrame = MatrixFrame.Identity;
			CameraName = "";
		}
	}

	private Scene _scene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private Camera _camera;

	private SceneLayer _sceneLayer;

	private List<CameraPoint> CamPoints;

	private DateTime testTime;

	private string _validWriteDirectory = Utilities.GetVisualTestsValidatePath();

	private string _validReadDirectory = Utilities.GetBasePath() + "ValidVisuals/";

	private string _pathDirectory = Utilities.GetVisualTestsTestFilesPath();

	private string _failDirectory = TestCommonBase.GetAttachmentsFolderPath();

	private string _reportFile = "report.txt";

	private int CurCameraIndex;

	private int TestSubIndex;

	private bool isValidTest_ = true;

	private NativeOptions.ConfigQuality preset_;

	public static bool isSceneSuccess = true;

	private string date;

	private string scene_name;

	private int frameCounter = -200;

	private List<string> testTypesToCheck_ = new List<string>();

	private int CamPointCount => CamPoints.Count;

	public bool StartedRendering()
	{
		return _sceneLayer.SceneView.ReadyToRender();
	}

	public string GetSubTestName(CameraPointTestType type)
	{
		return type switch
		{
			CameraPointTestType.Albedo => "_albedo", 
			CameraPointTestType.Normal => "_normal", 
			CameraPointTestType.Specular => "_specular", 
			CameraPointTestType.AO => "_ao", 
			CameraPointTestType.OnlyAmbient => "_onlyambient", 
			CameraPointTestType.OnlyDirect => "_onlydirect", 
			CameraPointTestType.Final => "_final", 
			_ => "", 
		};
	}

	public Utilities.EngineRenderDisplayMode GetRenderMode(CameraPointTestType type)
	{
		return type switch
		{
			CameraPointTestType.Albedo => Utilities.EngineRenderDisplayMode.ShowAlbedo, 
			CameraPointTestType.Normal => Utilities.EngineRenderDisplayMode.ShowNormals, 
			CameraPointTestType.Specular => Utilities.EngineRenderDisplayMode.ShowSpecular, 
			CameraPointTestType.AO => Utilities.EngineRenderDisplayMode.ShowOcclusion, 
			CameraPointTestType.OnlyAmbient => Utilities.EngineRenderDisplayMode.ShowDisableSunLighting, 
			CameraPointTestType.OnlyDirect => Utilities.EngineRenderDisplayMode.ShowDisableAmbientLighting, 
			_ => Utilities.EngineRenderDisplayMode.ShowNone, 
		};
	}

	public VisualTestsScreen(bool isValidTest, NativeOptions.ConfigQuality preset, string sceneName, DateTime testTime, List<string> testTypesToCheck)
	{
		isValidTest_ = isValidTest;
		preset_ = preset;
		scene_name = sceneName;
		this.testTime = testTime;
		isSceneSuccess = true;
		_failDirectory = _failDirectory + "/" + sceneName + "_" + preset;
		testTypesToCheck_ = testTypesToCheck;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_sceneLayer = new SceneLayer();
		AddLayer(_sceneLayer);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (!isValidTest_)
		{
			date = testTime.ToString("dd-MM-yyyy hh-mmtt");
			_pathDirectory = _pathDirectory + date + "/";
			Directory.CreateDirectory(_pathDirectory);
			_reportFile = _pathDirectory + "report.txt";
		}
		CreateScene();
		_scene.Tick(0f);
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
		MessageManager.EraseMessageLines();
		if (!_sceneLayer.ReadyToRender())
		{
			return;
		}
		SetTestCamera();
		if (Utilities.GetNumberOfShaderCompilationsInProgress() > 0)
		{
			return;
		}
		float dt2 = ((_scene.GetName() == "visualtestmorph") ? 0.01f : 0f);
		_scene.Tick(dt2);
		int num = 5;
		frameCounter++;
		if (frameCounter >= num)
		{
			TakeScreenshotAndAnalyze();
			if (CurCameraIndex >= CamPointCount)
			{
				ScreenManager.PopScreen();
			}
			else
			{
				frameCounter = 0;
			}
		}
	}

	private void CreateScene()
	{
		_scene = Scene.CreateNewScene();
		_scene.SetName("VisualTestScreen");
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_scene);
		_scene.Read(scene_name);
		_scene.SetUseConstantTime(value: true);
		_scene.SetOcclusionMode(mode: true);
		_scene.OptimizeScene(optimizeFlora: true, optimizeOro: true);
		_sceneLayer.SetScene(_scene);
		_sceneLayer.SceneView.SetSceneUsesShadows(value: true);
		_sceneLayer.SceneView.SetForceShaderCompilation(value: true);
		_sceneLayer.SceneView.SetClearGbuffer(value: true);
		_camera = Camera.CreateCamera();
		GetCameraPoints();
		MessageManager.EraseMessageLines();
	}

	private bool ShouldCheckTestModeWithTag(string mode, GameEntity entity)
	{
		if (testTypesToCheck_.Count > 0)
		{
			if (testTypesToCheck_.Contains(mode))
			{
				return entity.HasTag(mode);
			}
			return false;
		}
		return entity.HasTag(mode);
	}

	private bool ShouldCheckTestMode(string mode)
	{
		return testTypesToCheck_.Contains(mode);
	}

	private void GetCameraPoints()
	{
		CamPoints = new List<CameraPoint>();
		foreach (GameEntity item in (from o in _scene.FindEntitiesWithTag("test_camera")
			orderby o.Name
			select o).ToList())
		{
			if (item.HasTag("exclude_" + (int)preset_))
			{
				continue;
			}
			CameraPoint cameraPoint = new CameraPoint();
			cameraPoint.CamFrame = item.GetFrame();
			cameraPoint.CameraName = item.Name;
			HashSet<CameraPointTestType> hashSet = new HashSet<CameraPointTestType>();
			if (ShouldCheckTestModeWithTag("gbuffer", item))
			{
				hashSet.Add(CameraPointTestType.Albedo);
				hashSet.Add(CameraPointTestType.Normal);
				hashSet.Add(CameraPointTestType.Specular);
				hashSet.Add(CameraPointTestType.AO);
			}
			if (ShouldCheckTestMode("albedo"))
			{
				hashSet.Add(CameraPointTestType.Albedo);
			}
			if (ShouldCheckTestMode("normal"))
			{
				hashSet.Add(CameraPointTestType.Normal);
			}
			if (ShouldCheckTestMode("specular"))
			{
				hashSet.Add(CameraPointTestType.Specular);
			}
			if (ShouldCheckTestMode("ao"))
			{
				hashSet.Add(CameraPointTestType.AO);
			}
			if (ShouldCheckTestModeWithTag("only_ambient", item))
			{
				hashSet.Add(CameraPointTestType.OnlyAmbient);
			}
			foreach (CameraPointTestType item2 in hashSet)
			{
				cameraPoint.TestTypes.Add(item2);
			}
			cameraPoint.TestTypes.Add(CameraPointTestType.Final);
			CamPoints.Add(cameraPoint);
		}
	}

	private void SetTestCamera()
	{
		CameraPoint cameraPoint = CamPoints[CurCameraIndex];
		MatrixFrame camFrame = cameraPoint.CamFrame;
		_camera.Frame = camFrame;
		float aspectRatio = Screen.AspectRatio;
		_camera.SetFovVertical(System.MathF.PI / 3f, aspectRatio, 0.1f, 500f);
		_sceneLayer.SetCamera(_camera);
		CameraPointTestType type = cameraPoint.TestTypes[TestSubIndex];
		Utilities.SetRenderMode(GetRenderMode(type));
	}

	protected override void OnFinalize()
	{
		MBDebug.Print("On finalized called for scene: " + scene_name);
		base.OnFinalize();
		_sceneLayer.ClearAll();
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
		_scene = null;
	}

	public void Reset()
	{
	}

	private void TakeScreenshotAndAnalyze()
	{
		CameraPoint cameraPoint = CamPoints[CurCameraIndex];
		CameraPointTestType type = cameraPoint.TestTypes[TestSubIndex];
		GetRenderMode(type);
		bool flag = true;
		string text = "";
		text = ((!isValidTest_) ? (_validReadDirectory + scene_name + "_" + cameraPoint.CameraName + "_" + GetSubTestName(type) + "_preset_" + NativeOptions.GetGFXPresetName(preset_) + ".bmp") : (_validReadDirectory + scene_name + "_" + cameraPoint.CameraName + "_" + GetSubTestName(type) + "_preset_" + NativeOptions.GetGFXPresetName(preset_) + ".bmp"));
		string text2 = scene_name + "_" + cameraPoint.CameraName + "_" + GetSubTestName(type) + "_preset_" + NativeOptions.GetGFXPresetName(preset_) + ".bmp";
		string text3 = _pathDirectory + text2;
		MBDebug.Print(text);
		MBDebug.Print(text3);
		if (isValidTest_)
		{
			Utilities.TakeScreenshot(text);
		}
		else
		{
			Utilities.TakeScreenshot(text3);
		}
		NativeOptions.GetGFXPresetName(preset_);
		if (!isValidTest_)
		{
			if (File.Exists(text))
			{
				if (!AnalyzeImageDifferences(text, text3))
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
		}
		if (!flag)
		{
			if (!Directory.Exists(_failDirectory))
			{
				Directory.CreateDirectory(TestCommonBase.GetAttachmentsFolderPath());
			}
			if (!Directory.Exists(_failDirectory))
			{
				Directory.CreateDirectory(_failDirectory);
			}
			string text4 = _failDirectory + "/" + cameraPoint.CameraName + GetSubTestName(type);
			if (!Directory.Exists(text4))
			{
				Directory.CreateDirectory(text4);
			}
			string text5 = text4 + "/branch_result.bmp";
			string text6 = text4 + "/valid.bmp";
			if (File.Exists(text5))
			{
				File.Delete(text5);
			}
			if (File.Exists(text6))
			{
				File.Delete(text6);
			}
			File.Copy(text3, text5);
			if (File.Exists(text))
			{
				if (File.Exists(text6))
				{
					File.Delete(text6);
				}
				File.Copy(text, text6);
			}
			isSceneSuccess = false;
		}
		TestSubIndex++;
		if (cameraPoint.TestTypes.Count == TestSubIndex)
		{
			CurCameraIndex++;
			TestSubIndex = 0;
		}
	}

	private bool AnalyzeImageDifferences(string path1, string path2)
	{
		byte[] array = File.ReadAllBytes(path1);
		byte[] array2 = File.ReadAllBytes(path2);
		if (array.Length != array2.Length)
		{
			return false;
		}
		float num = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = (int)array[i];
			float num3 = (int)array2[i];
			float num4 = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Abs(num2 - num3), 0f);
			num += num4;
		}
		num /= (float)array.Length;
		return num < 0.5f;
	}
}
