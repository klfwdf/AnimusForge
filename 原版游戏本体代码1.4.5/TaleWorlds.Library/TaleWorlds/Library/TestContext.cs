using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TaleWorlds.Library;

public class TestContext
{
	private AsyncRunner _asyncRunner;

	private AwaitableAsyncRunner _awaitableAsyncRunner;

	private Thread _asyncThread;

	private Task _asyncTask;

	public void RunTestAux(string commandLine)
	{
		TestCommonBase.BaseInstance.IsTestEnabled = true;
		Debug.SetTestModeEnabled(testModeEnabled: true);
		string[] array = commandLine.Split(new char[1] { ' ' });
		if (array.Length < 2)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == "/runTest")
			{
				num = i + 1;
			}
		}
		if (num >= array.Length || num == -1)
		{
			return;
		}
		string text = array[num];
		if (text == "OpenSceneOnStartup")
		{
			TestCommonBase.BaseInstance.SceneNameToOpenOnStartup = array[2];
		}
		for (int j = 3; j < array.Length; j++)
		{
			int.TryParse(array[j], out var result);
			TestCommonBase.BaseInstance.TestRandomSeed = result;
		}
		Debug.Print("commandLine: " + commandLine);
		Debug.Print("p: " + array.ToString());
		Debug.Print("Looking for test: " + text, 0, Debug.DebugColor.Yellow);
		ConstructorInfo asyncRunnerConstructor = GetAsyncRunnerConstructor(text);
		object obj = null;
		if (asyncRunnerConstructor != null)
		{
			obj = asyncRunnerConstructor.Invoke(new object[0]);
		}
		_asyncRunner = obj as AsyncRunner;
		_awaitableAsyncRunner = obj as AwaitableAsyncRunner;
		if (_asyncRunner != null)
		{
			_asyncThread = new Thread((ThreadStart)delegate
			{
				_asyncRunner.Run();
			});
			_asyncThread.Name = "ManagedAsyncThread";
			_asyncThread.Start();
		}
		if (_awaitableAsyncRunner != null)
		{
			_asyncTask = _awaitableAsyncRunner.RunAsync();
		}
	}

	private ConstructorInfo GetAsyncRunnerConstructor(string asyncRunner)
	{
		Assembly[] asyncRunnerAssemblies = GetAsyncRunnerAssemblies();
		for (int i = 0; i < asyncRunnerAssemblies.Length; i++)
		{
			Type[] types = asyncRunnerAssemblies[i].GetTypes();
			foreach (Type type in types)
			{
				if (type.Name == asyncRunner && (typeof(AsyncRunner).IsAssignableFrom(type) || typeof(AwaitableAsyncRunner).IsAssignableFrom(type)))
				{
					ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new Type[0], null);
					if (constructor != null)
					{
						return constructor;
					}
				}
			}
		}
		return null;
	}

	private Assembly[] GetAsyncRunnerAssemblies()
	{
		List<Assembly> list = new List<Assembly>();
		Assembly assembly = typeof(AsyncRunner).Assembly;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly2 in assemblies)
		{
			AssemblyName[] referencedAssemblies = assembly2.GetReferencedAssemblies();
			for (int j = 0; j < referencedAssemblies.Length; j++)
			{
				if (referencedAssemblies[j].ToString() == assembly.GetName().ToString())
				{
					list.Add(assembly2);
					break;
				}
			}
		}
		return list.ToArray();
	}

	public void OnApplicationTick(float dt)
	{
		if (_asyncTask != null && _asyncTask.Status == TaskStatus.Faulted)
		{
			string text = "ERROR: Mono exception occurred at async Test Run\n";
			if (_asyncTask.Exception.InnerException != null)
			{
				text += _asyncTask.Exception.InnerException.Message;
				text += "\n";
				text += _asyncTask.Exception.InnerException.StackTrace;
			}
			_asyncTask = null;
			Debug.Print(text, 5);
			Debug.FailedAssert(text, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\TestContext.cs", "OnApplicationTick", 177);
			Debug.DoDelayedexit(5);
		}
	}

	public void TickTest(float dt)
	{
		if (_asyncThread != null && _asyncThread.IsAlive && _asyncRunner != null)
		{
			_asyncRunner.SyncTick();
		}
		if (_awaitableAsyncRunner != null)
		{
			_awaitableAsyncRunner.OnTick(dt);
		}
	}

	public void FinalizeContext()
	{
		if (_asyncThread != null)
		{
			_asyncThread.Join();
		}
		_asyncThread = null;
		_asyncRunner = null;
		_awaitableAsyncRunner = null;
		_asyncTask = null;
	}
}
