using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens.Scripts;

public class MultiThreadedStressTestsScreen : ScreenBase
{
	public static class MultiThreadedTestFunctions
	{
		public static void MeshMerger(InputLayout layout)
		{
			Mesh randomMeshWithVdecl = Mesh.GetRandomMeshWithVdecl((int)layout);
			randomMeshWithVdecl = randomMeshWithVdecl.CreateCopy();
			UIntPtr uIntPtr = randomMeshWithVdecl.LockEditDataWrite();
			Mesh randomMeshWithVdecl2 = Mesh.GetRandomMeshWithVdecl((int)layout);
			randomMeshWithVdecl2 = randomMeshWithVdecl2.CreateCopy();
			Mesh randomMeshWithVdecl3 = Mesh.GetRandomMeshWithVdecl((int)layout);
			Mesh randomMeshWithVdecl4 = Mesh.GetRandomMeshWithVdecl((int)layout);
			randomMeshWithVdecl.AddMesh(randomMeshWithVdecl3, MatrixFrame.Identity);
			randomMeshWithVdecl2.AddMesh(randomMeshWithVdecl4, MatrixFrame.Identity);
			randomMeshWithVdecl.AddMesh(randomMeshWithVdecl2, MatrixFrame.Identity);
			int patchNode = randomMeshWithVdecl.AddFaceCorner(new Vec3(0f, 0f, 1f), new Vec3(0f, 0f, 1f), new Vec2(0f, 1f), 268435455u, uIntPtr);
			int patchNode2 = randomMeshWithVdecl.AddFaceCorner(new Vec3(0f, 1f), new Vec3(0f, 0f, 1f), new Vec2(1f, 0f), 268435455u, uIntPtr);
			int patchNode3 = randomMeshWithVdecl.AddFaceCorner(new Vec3(0f, 1f, 1f), new Vec3(0f, 0f, 1f), new Vec2(1f, 1f), 268435455u, uIntPtr);
			randomMeshWithVdecl.AddFace(patchNode, patchNode2, patchNode3, uIntPtr);
			randomMeshWithVdecl.UnlockEditDataWrite(uIntPtr);
		}

		public static void SceneHandler(SceneView view)
		{
			int num = 0;
			while (num < 500)
			{
				view.SetSceneUsesShadows(value: false);
				view.SetRenderWithPostfx(value: false);
				Thread.Sleep(5000);
				view.SetSceneUsesShadows(value: true);
				view.SetRenderWithPostfx(value: true);
				Thread.Sleep(5000);
				view.SetSceneUsesContour(value: true);
				Thread.Sleep(5000);
			}
		}
	}

	private List<Thread> _workerThreads;

	private Scene _scene;

	private SceneView _sceneView;

	protected override void OnActivate()
	{
		base.OnActivate();
		_scene = Scene.CreateNewScene();
		_scene.Read("mp_ruins_2");
		_sceneView = SceneView.CreateSceneView();
		_sceneView.SetScene(_scene);
		_sceneView.SetSceneUsesShadows(value: true);
		Camera camera = Camera.CreateCamera();
		camera.Frame = _scene.ReadAndCalculateInitialCamera();
		_sceneView.SetCamera(camera);
		_workerThreads = new List<Thread>();
		Thread thread = new Thread((ThreadStart)delegate
		{
			MultiThreadedTestFunctions.MeshMerger(InputLayout.Input_layout_regular);
		});
		thread.Name = "StressTester|Mesh Merger Thread";
		_workerThreads.Add(thread);
		Thread thread2 = new Thread((ThreadStart)delegate
		{
			MultiThreadedTestFunctions.MeshMerger(InputLayout.Input_layout_normal_map);
		});
		thread2.Name = "StressTester|Mesh Merger Thread";
		_workerThreads.Add(thread2);
		Thread thread3 = new Thread((ThreadStart)delegate
		{
			MultiThreadedTestFunctions.MeshMerger(InputLayout.Input_layout_skinning);
		});
		thread3.Name = "StressTester|Mesh Merger Thread";
		_workerThreads.Add(thread3);
		for (int num = 0; num < _workerThreads.Count; num++)
		{
			_workerThreads[num].Start();
		}
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_sceneView = null;
		_scene = null;
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		bool flag = true;
		for (int i = 0; i < _workerThreads.Count; i++)
		{
			if (_workerThreads[i].IsAlive)
			{
				flag = false;
			}
		}
		if (flag)
		{
			ScreenManager.PopScreen();
		}
	}
}
