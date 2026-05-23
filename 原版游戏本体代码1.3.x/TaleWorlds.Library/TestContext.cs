using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TaleWorlds.Library
{
	// Token: 0x02000094 RID: 148
	public class TestContext
	{
		// Token: 0x06000547 RID: 1351 RVA: 0x00012B04 File Offset: 0x00010D04
		public void RunTestAux(string commandLine)
		{
			TestCommonBase.BaseInstance.IsTestEnabled = true;
			Debug.SetTestModeEnabled(true);
			string[] array = commandLine.Split(new char[] { ' ' });
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
				int testRandomSeed;
				int.TryParse(array[j], out testRandomSeed);
				TestCommonBase.BaseInstance.TestRandomSeed = testRandomSeed;
			}
			Debug.Print("commandLine" + commandLine, 0, Debug.DebugColor.White, 17592186044416UL);
			Debug.Print("p" + array.ToString(), 0, Debug.DebugColor.White, 17592186044416UL);
			Debug.Print("Looking for test " + text, 0, Debug.DebugColor.Yellow, 17592186044416UL);
			ConstructorInfo asyncRunnerConstructor = this.GetAsyncRunnerConstructor(text);
			object obj = null;
			if (asyncRunnerConstructor != null)
			{
				obj = asyncRunnerConstructor.Invoke(new object[0]);
			}
			this._asyncRunner = obj as AsyncRunner;
			this._awaitableAsyncRunner = obj as AwaitableAsyncRunner;
			if (this._asyncRunner != null)
			{
				this._asyncThread = new Thread(delegate()
				{
					this._asyncRunner.Run();
				});
				this._asyncThread.Name = "ManagedAsyncThread";
				this._asyncThread.Start();
			}
			if (this._awaitableAsyncRunner != null)
			{
				this._asyncTask = this._awaitableAsyncRunner.RunAsync();
			}
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x00012CA0 File Offset: 0x00010EA0
		private ConstructorInfo GetAsyncRunnerConstructor(string asyncRunner)
		{
			Assembly[] asyncRunnerAssemblies = this.GetAsyncRunnerAssemblies();
			for (int i = 0; i < asyncRunnerAssemblies.Length; i++)
			{
				foreach (Type type in asyncRunnerAssemblies[i].GetTypes())
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

		// Token: 0x06000549 RID: 1353 RVA: 0x00012D40 File Offset: 0x00010F40
		private Assembly[] GetAsyncRunnerAssemblies()
		{
			List<Assembly> list = new List<Assembly>();
			Assembly assembly = typeof(AsyncRunner).Assembly;
			foreach (Assembly assembly2 in AppDomain.CurrentDomain.GetAssemblies())
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

		// Token: 0x0600054A RID: 1354 RVA: 0x00012DCC File Offset: 0x00010FCC
		public void OnApplicationTick(float dt)
		{
			if (this._asyncTask != null && this._asyncTask.Status == TaskStatus.Faulted)
			{
				string text = "ERROR: Mono exception occurred at async Test Run\n";
				if (this._asyncTask.Exception.InnerException != null)
				{
					text += this._asyncTask.Exception.InnerException.Message;
					text += "\n";
					text += this._asyncTask.Exception.InnerException.StackTrace;
				}
				this._asyncTask = null;
				Debug.Print(text, 5, Debug.DebugColor.White, 17592186044416UL);
				Debug.FailedAssert(text, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\TestContext.cs", "OnApplicationTick", 180);
				Debug.DoDelayedexit(5);
			}
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x00012E85 File Offset: 0x00011085
		public void TickTest(float dt)
		{
			if (this._asyncThread != null && this._asyncThread.IsAlive && this._asyncRunner != null)
			{
				this._asyncRunner.SyncTick();
			}
			if (this._awaitableAsyncRunner != null)
			{
				this._awaitableAsyncRunner.OnTick(dt);
			}
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x00012EC3 File Offset: 0x000110C3
		public void FinalizeContext()
		{
			if (this._asyncThread != null)
			{
				this._asyncThread.Join();
			}
			this._asyncThread = null;
			this._asyncRunner = null;
			this._awaitableAsyncRunner = null;
			this._asyncTask = null;
		}

		// Token: 0x040001A3 RID: 419
		private AsyncRunner _asyncRunner;

		// Token: 0x040001A4 RID: 420
		private AwaitableAsyncRunner _awaitableAsyncRunner;

		// Token: 0x040001A5 RID: 421
		private Thread _asyncThread;

		// Token: 0x040001A6 RID: 422
		private Task _asyncTask;
	}
}
