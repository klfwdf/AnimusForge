using System;
using System.Reflection;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

public class GameApplicationDomainController : MarshalByRefObject
{
	private delegate void InitializerDelegate(Delegate argument);

	private static Delegate _passManagedInitializeMethod;

	private static Delegate _passManagedCallbackMethod;

	private static GameApplicationDomainController _instance;

	private bool _newApplicationDomain;

	public GameApplicationDomainController(bool newApplicationDomain)
	{
		Debug.Print("Constructing GameApplicationDomainController.");
		_instance = this;
		_newApplicationDomain = newApplicationDomain;
	}

	public GameApplicationDomainController()
	{
		Debug.Print("Constructing GameApplicationDomainController.");
		_instance = this;
		_newApplicationDomain = true;
	}

	public void LoadAsHostedByNative(IntPtr passManagedInitializeMethodPointer, IntPtr passManagedCallbackMethodPointer, string gameApiDllName, string gameApiTypeName, Platform currentPlatform)
	{
		Delegate passManagedInitializeMethod = (OneMethodPasserDelegate)Marshal.GetDelegateForFunctionPointer(passManagedInitializeMethodPointer, typeof(OneMethodPasserDelegate));
		Delegate passManagedCallbackMethod = (OneMethodPasserDelegate)Marshal.GetDelegateForFunctionPointer(passManagedCallbackMethodPointer, typeof(OneMethodPasserDelegate));
		Load(passManagedInitializeMethod, passManagedCallbackMethod, gameApiDllName, gameApiTypeName, currentPlatform);
	}

	public void Load(Delegate passManagedInitializeMethod, Delegate passManagedCallbackMethod, string gameApiDllName, string gameApiTypeName, Platform currentPlatform)
	{
		try
		{
			Common.SetInvariantCulture();
			_passManagedInitializeMethod = passManagedInitializeMethod;
			_passManagedCallbackMethod = passManagedCallbackMethod;
			Assembly assembly = null;
			assembly = ((!_newApplicationDomain) ? GetType().Assembly : AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.DotNet.dll"));
			Assembly assembly2 = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + gameApiDllName);
			if (assembly2 == null)
			{
				Console.WriteLine("gameApi is null");
			}
			Type? type = assembly.GetType("TaleWorlds.DotNet.Managed");
			if (type == null)
			{
				Console.WriteLine("engineManagedType is null");
			}
			Type type2 = assembly2.GetType(gameApiTypeName);
			if (type2 == null)
			{
				Console.WriteLine("managedType is null");
			}
			type.GetMethod("PassInitializationMethodPointersForDotNet").Invoke(null, new object[2] { _passManagedInitializeMethod, _passManagedCallbackMethod });
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
}
