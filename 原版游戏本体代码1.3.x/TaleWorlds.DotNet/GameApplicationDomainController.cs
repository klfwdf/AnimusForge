using System;
using System.Reflection;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000008 RID: 8
	public class GameApplicationDomainController : MarshalByRefObject
	{
		// Token: 0x0600001D RID: 29 RVA: 0x000025D8 File Offset: 0x000007D8
		public GameApplicationDomainController(bool newApplicationDomain)
		{
			Debug.Print("Constructing GameApplicationDomainController.", 0, Debug.DebugColor.White, 17592186044416UL);
			GameApplicationDomainController._instance = this;
			this._newApplicationDomain = newApplicationDomain;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002603 File Offset: 0x00000803
		public GameApplicationDomainController()
		{
			Debug.Print("Constructing GameApplicationDomainController.", 0, Debug.DebugColor.White, 17592186044416UL);
			GameApplicationDomainController._instance = this;
			this._newApplicationDomain = true;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002630 File Offset: 0x00000830
		public void LoadAsHostedByNative(IntPtr passManagedInitializeMethodPointer, IntPtr passManagedCallbackMethodPointer, string gameApiDllName, string gameApiTypeName, Platform currentPlatform)
		{
			Delegate passManagedInitializeMethod = (OneMethodPasserDelegate)Marshal.GetDelegateForFunctionPointer(passManagedInitializeMethodPointer, typeof(OneMethodPasserDelegate));
			Delegate passManagedCallbackMethod = (OneMethodPasserDelegate)Marshal.GetDelegateForFunctionPointer(passManagedCallbackMethodPointer, typeof(OneMethodPasserDelegate));
			this.Load(passManagedInitializeMethod, passManagedCallbackMethod, gameApiDllName, gameApiTypeName, currentPlatform);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002678 File Offset: 0x00000878
		public void Load(Delegate passManagedInitializeMethod, Delegate passManagedCallbackMethod, string gameApiDllName, string gameApiTypeName, Platform currentPlatform)
		{
			try
			{
				Common.SetInvariantCulture();
				GameApplicationDomainController._passManagedInitializeMethod = passManagedInitializeMethod;
				GameApplicationDomainController._passManagedCallbackMethod = passManagedCallbackMethod;
				Assembly assembly;
				if (this._newApplicationDomain)
				{
					assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.DotNet.dll", true);
				}
				else
				{
					assembly = base.GetType().Assembly;
				}
				Assembly assembly2 = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + gameApiDllName, true);
				if (assembly2 == null)
				{
					Console.WriteLine("gameApi is null");
				}
				Type type = assembly.GetType("TaleWorlds.DotNet.Managed");
				if (type == null)
				{
					Console.WriteLine("engineManagedType is null");
				}
				Type type2 = assembly2.GetType(gameApiTypeName);
				if (type2 == null)
				{
					Console.WriteLine("managedType is null");
				}
				type.GetMethod("PassInitializationMethodPointersForDotNet").Invoke(null, new object[]
				{
					GameApplicationDomainController._passManagedInitializeMethod,
					GameApplicationDomainController._passManagedCallbackMethod
				});
				type2.GetMethod("Start").Invoke(null, new object[0]);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.WriteLine(ex.GetType().Name);
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null)
				{
					Console.WriteLine("-");
					Console.WriteLine(ex.InnerException.Message);
					if (ex.InnerException.InnerException != null)
					{
						Console.WriteLine("-");
						Console.WriteLine(ex.InnerException.InnerException.Message);
					}
				}
			}
		}

		// Token: 0x04000013 RID: 19
		private static Delegate _passManagedInitializeMethod;

		// Token: 0x04000014 RID: 20
		private static Delegate _passManagedCallbackMethod;

		// Token: 0x04000015 RID: 21
		private static GameApplicationDomainController _instance;

		// Token: 0x04000016 RID: 22
		private bool _newApplicationDomain;

		// Token: 0x02000039 RID: 57
		// (Invoke) Token: 0x0600015B RID: 347
		private delegate void InitializerDelegate(Delegate argument);
	}
}
