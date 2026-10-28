#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Patches;
using Bannerlord.UIExtenderEx.ResourceManager;
using Bannerlord.UIExtenderEx.Utils;
using HarmonyLib;

namespace Bannerlord.UIExtenderEx;

public class UIExtender
{
	private static readonly Harmony Harmony;

	private static readonly Dictionary<string, UIExtender> Instances;

	private readonly string _moduleName;

	private UIExtenderRuntime? _runtime;

	public static UIExtender Create(string moduleName)
	{
		return new UIExtender(moduleName, _: false);
	}

	public static UIExtender? GetUIExtenderFor(string moduleName)
	{
		if (!Instances.TryGetValue(moduleName, out UIExtender value))
		{
			return null;
		}
		return value;
	}

	internal static UIExtenderRuntime? GetRuntimeFor(string moduleName)
	{
		return Instances[moduleName]._runtime;
	}

	internal static IReadOnlyList<UIExtenderRuntime> GetAllRuntimes()
	{
		return Instances.Select<KeyValuePair<string, UIExtender>, UIExtenderRuntime>((KeyValuePair<string, UIExtender> x) => x.Value._runtime).OfType<UIExtenderRuntime>().ToList();
	}

	static UIExtender()
	{
		Harmony = new Harmony("bannerlord.uiextender.ex");
		Instances = new Dictionary<string, UIExtender>();
		UIConfigPatch.Patch(Harmony);
		ParsePatch.Patch(Harmony);
		ViewModelPatch.Patch(Harmony);
		WidgetPrefabPatch.Patch(Harmony);
		BrushFactoryManager.Patch(Harmony);
		WidgetFactoryManager.Patch(Harmony);
	}

	private UIExtender(string moduleName, bool _)
	{
		_moduleName = moduleName;
	}

	[Obsolete("Use UIExtender.Create(moduleName) if backwards compatibility is not a concern.", false)]
	public UIExtender(string moduleName)
	{
		_moduleName = moduleName;
	}

	[Obsolete("Use explicit call Register(Assembly)", true)]
	public void Register()
	{
		Register(Assembly.GetCallingAssembly());
	}

	public void Register(Assembly assembly)
	{
		Trace.TraceInformation("{0} - Register: {1}", _moduleName, assembly);
		IEnumerable<Type> types = from t in assembly.GetTypes()
			where t.CustomAttributes.Any((CustomAttributeData a) => a.AttributeType.IsSubclassOf(typeof(BaseUIExtenderAttribute)))
			select t;
		Register(types);
	}

	public void Register(IEnumerable<Type> types)
	{
		Trace.TraceInformation("{0} - Register Types", _moduleName);
		if (Instances.ContainsKey(_moduleName))
		{
			MessageUtils.DisplayUserError("Failed to load extension module " + _moduleName + " - already loaded!");
			return;
		}
		Instances[_moduleName] = this;
		_runtime = new UIExtenderRuntime(_moduleName);
		_runtime.Register(types);
	}

	public void Deregister()
	{
		Trace.TraceInformation("{0} - Deregister", _moduleName);
		if (!Instances.ContainsKey(_moduleName))
		{
			MessageUtils.DisplayUserError("Failed to deregister " + _moduleName + " - not loaded!");
		}
		else if (Instances[_moduleName] == this)
		{
			_runtime.Deregister();
			Instances.Remove(_moduleName);
		}
	}

	public void Enable()
	{
		Trace.TraceInformation("{0} - Enabled", _moduleName);
		if (_runtime == null)
		{
			MessageUtils.Fail("Register() method was not called before Enable()!");
		}
		else
		{
			_runtime.Enable();
		}
	}

	public void Disable()
	{
		Trace.TraceInformation("{0} - Disabled", _moduleName);
		if (_runtime == null)
		{
			MessageUtils.Fail("Register() method was not called before Disable()!");
		}
		else
		{
			_runtime.Disable();
		}
	}

	public void Enable(Type type)
	{
		Trace.TraceInformation("{0} - Enable {1}", _moduleName, type);
		if (_runtime == null)
		{
			MessageUtils.Fail("Register() method was not called before Enable(type)!");
		}
		else
		{
			_runtime.Enable(type);
		}
	}

	public void Disable(Type type)
	{
		Trace.TraceInformation("{0} - Enable {1}", _moduleName, type);
		if (_runtime == null)
		{
			MessageUtils.Fail("Register() method was not called before Disable(type)!");
		}
		else
		{
			_runtime.Disable(type);
		}
	}
}
