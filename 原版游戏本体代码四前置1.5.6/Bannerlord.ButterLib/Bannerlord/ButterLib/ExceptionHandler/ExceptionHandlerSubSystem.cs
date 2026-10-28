using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using Bannerlord.BLSE;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ButterLib.Options;
using Bannerlord.ButterLib.SubSystems;
using Bannerlord.ButterLib.SubSystems.Settings;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using JetBrains.Annotations;

namespace Bannerlord.ButterLib.ExceptionHandler;

[BLSEInterceptor]
internal sealed class ExceptionHandlerSubSystem : ISubSystem, ISubSystemSettings<ExceptionHandlerSubSystem>
{
	private delegate void DetachWatchdogDelegate();

	private static readonly DetachWatchdogDelegate? DetachWatchdog = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<DetachWatchdogDelegate>("TaleWorlds.Engine.Utilities:DetachWatchdog");

	internal readonly Harmony Harmony = new Harmony("Bannerlord.ButterLib.ExceptionHandler.BEW");

	private bool _disableWhenDebuggerIsAttached = true;

	private bool _wasInitialized;

	private static bool _isSubscribedToUnhandledException;

	private static bool _wasButrLoaderInterceptorCalled;

	public static ExceptionHandlerSubSystem? Instance { get; private set; }

	public string Id => "ExceptionHandler";

	public string Name => "{=ZypQtNNcVN}Exception Handler";

	public string Description => "{=UreeIeLQYS}Captures game crashes and creates reports out of them.";

	public bool IsEnabled { get; private set; }

	public bool CanBeDisabled => true;

	public bool CanBeSwitchedAtRuntime => true;

	public bool DisableWhenDebuggerIsAttached
	{
		get
		{
			return _disableWhenDebuggerIsAttached;
		}
		private set
		{
			if (_disableWhenDebuggerIsAttached == value)
			{
				return;
			}
			_disableWhenDebuggerIsAttached = value;
			if (BEWPatch.IsDebuggerAttached())
			{
				if (_disableWhenDebuggerIsAttached)
				{
					Disable();
				}
				else
				{
					SubscribeToUnhandledException();
				}
			}
			else
			{
				SubscribeToUnhandledException();
			}
		}
	}

	public IReadOnlyCollection<SubSystemSettingsDeclaration<ExceptionHandlerSubSystem>> Declarations { get; } = new global::_003C_003Ez__ReadOnlySingleElementList<SubSystemSettingsDeclaration<ExceptionHandlerSubSystem>>(new SubSystemSettingsPropertyBool<ExceptionHandlerSubSystem>("{=B7bfrDNzIk} Disable when Debugger is Attached", "{=r3ktQzFMRz} Stops the Exception Handler when a debugger is attached.", (ExceptionHandlerSubSystem x) => x.DisableWhenDebuggerIsAttached));

	public ExceptionHandlerSubSystem()
	{
		Instance = this;
	}

	public void Enable()
	{
		if (!_wasInitialized)
		{
			_wasInitialized = true;
			if (!(SettingsProvider.PopulateSubSystemSettings(this) ?? true))
			{
				return;
			}
		}
		if (!IsEnabled)
		{
			IsEnabled = true;
			if (!BEWPatch.IsDebuggerAttached())
			{
				SubscribeToUnhandledException();
			}
			else if (_disableWhenDebuggerIsAttached)
			{
				return;
			}
			if (!_wasButrLoaderInterceptorCalled)
			{
				BEWPatch.Enable(Harmony);
				DetachWatchdog?.Invoke();
			}
		}
	}

	public void Disable()
	{
		if (IsEnabled)
		{
			IsEnabled = false;
			UnsubscribeToUnhandledException();
			if (ModuleInfoHelper.GetLoadedModules().Any((ModuleInfoExtendedHelper m) => string.Equals(m.Id, "BetterExceptionWindow", StringComparison.InvariantCultureIgnoreCase)))
			{
				BEWPatch.Disable(Harmony);
			}
		}
	}

	private static void SubscribeToUnhandledException()
	{
		if (!_isSubscribedToUnhandledException && !_wasButrLoaderInterceptorCalled)
		{
			_isSubscribedToUnhandledException = true;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}
	}

	private static void UnsubscribeToUnhandledException()
	{
		if (_isSubscribedToUnhandledException && !_wasButrLoaderInterceptorCalled)
		{
			_isSubscribedToUnhandledException = false;
			AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
		}
	}

	[HandleProcessCorruptedStateExceptions]
	[SecurityCritical]
	private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception exception)
		{
			ExceptionReporter.Show(exception);
		}
	}

	[UsedImplicitly]
	private static void OnInitializeSubModulesPrefix()
	{
		_wasButrLoaderInterceptorCalled = true;
	}
}
