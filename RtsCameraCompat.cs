using System;
using System.Reflection;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class RtsCameraCompat
{
	private const float ReflectionProbeIntervalSeconds = 0.25f;
	private const float InputGraceSeconds = 1.0f;

	private static bool _lastF10Down;
	private static bool _f10ToggleActive;
	private static float _lastF10ActivityMissionTime = -999f;
	private static float _nextReflectionProbeMissionTime = -999f;
	private static bool _lastReflectionActive;
	private static bool _lastReportedActive;
	private static bool _lastRtsAssemblyLoaded;

	internal static bool IsLikelyExternalCameraControlActive(Mission mission, float missionTime)
	{
		if (mission == null)
		{
			ResetInputState();
			return false;
		}
		if (!IsRtsCameraAssemblyLoaded())
		{
			ResetInputState();
			return false;
		}
		bool f10Down = IsF10Down();
		if (f10Down && !_lastF10Down)
		{
			_f10ToggleActive = !_f10ToggleActive;
			_lastF10ActivityMissionTime = missionTime;
			PerfProbe.MarkEvent("RTSCameraCompat.F10Toggle");
		}
		else if (f10Down)
		{
			_lastF10ActivityMissionTime = missionTime;
		}
		_lastF10Down = f10Down;
		bool reflectionActive = _lastReflectionActive;
		if (missionTime >= _nextReflectionProbeMissionTime)
		{
			_nextReflectionProbeMissionTime = missionTime + ReflectionProbeIntervalSeconds;
			reflectionActive = ProbeRtsCameraState();
			_lastReflectionActive = reflectionActive;
			if (reflectionActive)
			{
				_lastF10ActivityMissionTime = missionTime;
			}
		}
		bool active = reflectionActive || f10Down || _f10ToggleActive || missionTime - _lastF10ActivityMissionTime <= InputGraceSeconds;
		if (active && !_lastReportedActive)
		{
			Logger.Log("RTSCameraCompat", "RTS camera/order control detected; AnimusForge meeting lock will yield player controller while it is active.");
		}
		_lastReportedActive = active;
		return active;
	}

	private static void ResetInputState()
	{
		_lastF10Down = false;
		_f10ToggleActive = false;
		_lastReflectionActive = false;
		_lastReportedActive = false;
		_lastF10ActivityMissionTime = -999f;
	}

	private static bool IsF10Down()
	{
		try
		{
			return Input.IsKeyDown(InputKey.F10);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRtsCameraAssemblyLoaded()
	{
		if (_lastRtsAssemblyLoaded)
		{
			return true;
		}
		try
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				string name = assembly?.GetName()?.Name ?? "";
				if (name.IndexOf("RTSCamera", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					_lastRtsAssemblyLoaded = true;
					return true;
				}
			}
		}
		catch
		{
		}
		_lastRtsAssemblyLoaded = false;
		return false;
	}

	private static bool ProbeRtsCameraState()
	{
		if (!_lastRtsAssemblyLoaded)
		{
			return false;
		}
		try
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				string assemblyName = assembly?.GetName()?.Name ?? "";
				if (assemblyName.IndexOf("RTSCamera", StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types;
				}
				catch
				{
					continue;
				}
				if (types == null)
				{
					continue;
				}
				for (int i = 0; i < types.Length; i++)
				{
					Type type = types[i];
					if (type == null)
					{
						continue;
					}
					string typeName = type.FullName ?? type.Name ?? "";
					if (typeName.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) < 0
						&& typeName.IndexOf("Command", StringComparison.OrdinalIgnoreCase) < 0
						&& typeName.IndexOf("Order", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					if (ProbeTypeState(type))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool ProbeTypeState(Type type)
	{
		const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
		try
		{
			FieldInfo[] fields = type.GetFields(staticFlags);
			for (int i = 0; i < fields.Length; i++)
			{
				FieldInfo field = fields[i];
				if (field == null)
				{
					continue;
				}
				if (field.FieldType == typeof(bool) && IsActiveStateName(field.Name) && SafeGetBoolField(field, null))
				{
					return true;
				}
				if (MayContainRtsState(field.FieldType) && ProbeObjectState(SafeGetFieldValue(field, null), 1))
				{
					return true;
				}
			}
			PropertyInfo[] properties = type.GetProperties(staticFlags);
			for (int i = 0; i < properties.Length; i++)
			{
				PropertyInfo property = properties[i];
				if (property == null || property.GetIndexParameters().Length != 0)
				{
					continue;
				}
				if (property.PropertyType == typeof(bool) && IsActiveStateName(property.Name) && SafeGetBoolProperty(property, null))
				{
					return true;
				}
				if (MayContainRtsState(property.PropertyType) && ProbeObjectState(SafeGetPropertyValue(property, null), 1))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool ProbeObjectState(object instance, int depth)
	{
		if (instance == null || depth > 2)
		{
			return false;
		}
		Type type = instance.GetType();
		const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		try
		{
			FieldInfo[] fields = type.GetFields(instanceFlags);
			for (int i = 0; i < fields.Length; i++)
			{
				FieldInfo field = fields[i];
				if (field == null)
				{
					continue;
				}
				if (field.FieldType == typeof(bool) && IsActiveStateName(field.Name) && SafeGetBoolField(field, instance))
				{
					return true;
				}
				if (MayContainRtsState(field.FieldType) && ProbeObjectState(SafeGetFieldValue(field, instance), depth + 1))
				{
					return true;
				}
			}
			PropertyInfo[] properties = type.GetProperties(instanceFlags);
			for (int i = 0; i < properties.Length; i++)
			{
				PropertyInfo property = properties[i];
				if (property == null || property.GetIndexParameters().Length != 0)
				{
					continue;
				}
				if (property.PropertyType == typeof(bool) && IsActiveStateName(property.Name) && SafeGetBoolProperty(property, instance))
				{
					return true;
				}
				if (MayContainRtsState(property.PropertyType) && ProbeObjectState(SafeGetPropertyValue(property, instance), depth + 1))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsActiveStateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		return name.IndexOf("IsFreeCamera", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("_isFreeCamera", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("IsOrderMenuOpen", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("IsOrderShown", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("_isOrderShown", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("_isOrderViewOpened", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("IsSpectatorCamera", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("IsElevatedCameraApplied", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("IsKeepingElevatedCamera", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool MayContainRtsState(Type type)
	{
		if (type == null || type == typeof(string) || type.IsValueType)
		{
			return false;
		}
		string name = type.FullName ?? type.Name ?? "";
		return name.IndexOf("RTSCamera", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("Command", StringComparison.OrdinalIgnoreCase) >= 0
			|| name.IndexOf("Order", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool SafeGetBoolField(FieldInfo field, object instance)
	{
		try
		{
			return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(instance);
		}
		catch
		{
			return false;
		}
	}

	private static bool SafeGetBoolProperty(PropertyInfo property, object instance)
	{
		try
		{
			return property != null && property.PropertyType == typeof(bool) && (bool)property.GetValue(instance, null);
		}
		catch
		{
			return false;
		}
	}

	private static object SafeGetFieldValue(FieldInfo field, object instance)
	{
		try
		{
			return field?.GetValue(instance);
		}
		catch
		{
			return null;
		}
	}

	private static object SafeGetPropertyValue(PropertyInfo property, object instance)
	{
		try
		{
			return property?.GetValue(instance, null);
		}
		catch
		{
			return null;
		}
	}
}
