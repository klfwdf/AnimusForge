using System;
using System.Runtime.InteropServices;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet;

public static class Controller
{
	[MonoNativeFunctionWrapper]
	private delegate void ControllerMethodDelegate();

	[MonoNativeFunctionWrapper]
	private delegate void CreateApplicationDomainMethodDelegate(IntPtr gameDllNameAsPointer, IntPtr gameTypeNameAsPointer, int currentEngineAsInteger, int currentPlatformAsInteger);

	[MonoNativeFunctionWrapper]
	private delegate void OverrideManagedDllFolderDelegate(IntPtr overridenFolderAsPointer);

	private static bool _hostedByNative;

	private static Delegate _passControllerMethods;

	private static Delegate _passManagedInitializeMethod;

	private static Delegate _passManagedCallbackMethod;

	private static IntPtr _passManagedInitializeMethodPointer;

	private static IntPtr _passManagedCallbackMethodPointer;

	private static CreateApplicationDomainMethodDelegate _loadOnCurrentApplicationDomainMethod;

	private static Runtime RuntimeLibrary { get; set; } = Runtime.DotNet;

	[MonoPInvokeCallback(typeof(OverrideManagedDllFolderDelegate))]
	public static void OverrideManagedDllFolder(IntPtr overridenFolderAsPointer)
	{
		ManagedDllFolder.OverrideManagedDllFolder(Marshal.PtrToStringAnsi(overridenFolderAsPointer));
	}

	[MonoPInvokeCallback(typeof(CreateApplicationDomainMethodDelegate))]
	public static void LoadOnCurrentApplicationDomain(IntPtr gameDllNameAsPointer, IntPtr gameTypeNameAsPointer, int currentEngineAsInteger, int currentPlatformAsInteger)
	{
		ApplicationPlatform.Initialize((EngineType)currentEngineAsInteger, (Platform)currentPlatformAsInteger, RuntimeLibrary);
		string gameApiDllName = Marshal.PtrToStringAnsi(gameDllNameAsPointer);
		string gameApiTypeName = Marshal.PtrToStringAnsi(gameTypeNameAsPointer);
		Debug.Print("Appending private path to current application domain.");
		AppDomain.CurrentDomain.AppendPrivatePath(ManagedDllFolder.Name);
		Debug.Print("Creating GameApplicationDomainController on current application domain.");
		GameApplicationDomainController gameApplicationDomainController = new GameApplicationDomainController(newApplicationDomain: false);
		if (gameApplicationDomainController == null)
		{
			Console.WriteLine("GameApplicationDomainController is NULL!");
			Console.WriteLine("Press a key to continue...");
			Console.ReadKey();
		}
		if (_hostedByNative)
		{
			Debug.Print("Initializing GameApplicationDomainController as Hosted by Native(Mono or hosted .NET Core).");
			gameApplicationDomainController.LoadAsHostedByNative(_passManagedInitializeMethodPointer, _passManagedCallbackMethodPointer, gameApiDllName, gameApiTypeName, (Platform)currentPlatformAsInteger);
		}
		else
		{
			Debug.Print("Initializing GameApplicationDomainController as Dot Net.");
			gameApplicationDomainController.Load(_passManagedInitializeMethod, _passManagedCallbackMethod, gameApiDllName, gameApiTypeName, (Platform)currentPlatformAsInteger);
		}
	}

	private static void SetEngineMethodsAsHostedByNative(IntPtr passControllerMethods, IntPtr passManagedInitializeMethod, IntPtr passManagedCallbackMethod)
	{
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsHostedByNative");
		Debug.Print("Beginning...");
		_hostedByNative = true;
		_passControllerMethods = (OneMethodPasserDelegate)Marshal.GetDelegateForFunctionPointer(passControllerMethods, typeof(OneMethodPasserDelegate));
		_passManagedInitializeMethodPointer = passManagedInitializeMethod;
		_passManagedCallbackMethodPointer = passManagedCallbackMethod;
		Debug.Print("Starting controller...");
		Start();
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsHostedByNative - Done");
	}

	public static void SetEngineMethodsAsMono(IntPtr passControllerMethods, IntPtr passManagedInitializeMethod, IntPtr passManagedCallbackMethod)
	{
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsMono");
		RuntimeLibrary = Runtime.Mono;
		SetEngineMethodsAsHostedByNative(passControllerMethods, passManagedInitializeMethod, passManagedCallbackMethod);
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsMono - Done");
	}

	public static void SetEngineMethodsAsHostedDotNetCore(IntPtr passControllerMethods, IntPtr passManagedInitializeMethod, IntPtr passManagedCallbackMethod)
	{
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsHostedDotNetCore");
		RuntimeLibrary = Runtime.DotNetCore;
		SetEngineMethodsAsHostedByNative(passControllerMethods, passManagedInitializeMethod, passManagedCallbackMethod);
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsHostedDotNetCore - Done");
	}

	public static void SetEngineMethodsAsDotNet(Delegate passControllerMethods, Delegate passManagedInitializeMethod, Delegate passManagedCallbackMethod)
	{
		Debug.Print("Setting engine methods at Controller::SetEngineMethodsAsDotNet");
		if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"))
		{
			RuntimeLibrary = Runtime.DotNet;
		}
		else if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Core") || RuntimeInformation.FrameworkDescription.StartsWith(".NET 6"))
		{
			RuntimeLibrary = Runtime.DotNetCore;
		}
		_passControllerMethods = passControllerMethods;
		_passManagedInitializeMethod = passManagedInitializeMethod;
		_passManagedCallbackMethod = passManagedCallbackMethod;
		if ((object)_passControllerMethods == null)
		{
			Debug.Print("_passControllerMethods is null");
		}
		if ((object)_passManagedInitializeMethod == null)
		{
			Debug.Print("_passManagedInitializeMethod is null");
		}
		if ((object)_passManagedCallbackMethod == null)
		{
			Debug.Print("_passManagedCallbackMethod is null");
		}
		Start();
	}

	private static void Start()
	{
		_loadOnCurrentApplicationDomainMethod = LoadOnCurrentApplicationDomain;
		PassControllerMethods(_loadOnCurrentApplicationDomainMethod);
	}

	private static void PassControllerMethods(Delegate loadOnCurrentApplicationDomainMethod)
	{
		if ((object)_passControllerMethods != null)
		{
			_passControllerMethods.DynamicInvoke(loadOnCurrentApplicationDomainMethod);
		}
		else
		{
			Debug.Print("Could not find _passControllerMethods");
		}
	}
}
