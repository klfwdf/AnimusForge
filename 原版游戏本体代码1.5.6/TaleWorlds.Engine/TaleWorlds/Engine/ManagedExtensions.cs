using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class ManagedExtensions
{
	private static void OnEditorVariableChanged(DotNetObject managedObject, uint classNameHash, uint fieldNameHash)
	{
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldString(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, string value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldDouble(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, double value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldFloat(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, float value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldBool(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, bool value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldInt(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, int value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldVec3(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldEntity(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new GameEntity(value) : null);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldTexture(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new Texture(value) : null);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldMesh(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new MetaMesh(value) : null);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldMaterial(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new Material(value) : null);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldColor(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, new Color
			{
				Red = value.x,
				Green = value.y,
				Blue = value.z,
				Alpha = value.w
			});
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldMatrixFrame(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, MatrixFrame value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void SetObjectFieldEnum(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, string value, int callFieldChangeEventAsInteger)
	{
		bool flag = callFieldChangeEventAsInteger != 0;
		_ = managedObject.GetType().Name;
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (!(fieldOfClass == null))
		{
			object value2 = Enum.Parse(fieldOfClass.FieldType, value);
			fieldOfClass.SetValue(managedObject, value2);
			if (flag)
			{
				OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}
	}

	[EngineCallback(null, false)]
	internal static void GetObjectField(DotNetObject managedObject, uint classNameHash, ref ScriptComponentFieldHolder scriptComponentFieldHolder, uint fieldNameHash, RglScriptFieldType type)
	{
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		switch (type)
		{
		case RglScriptFieldType.RglSftString:
			scriptComponentFieldHolder.s = (string)fieldOfClass.GetValue(managedObject);
			break;
		case RglScriptFieldType.RglSftDouble:
			scriptComponentFieldHolder.d = (double)Convert.ChangeType(fieldOfClass.GetValue(managedObject), typeof(double));
			break;
		case RglScriptFieldType.RglSftFloat:
			scriptComponentFieldHolder.f = (float)Convert.ChangeType(fieldOfClass.GetValue(managedObject), typeof(float));
			break;
		case RglScriptFieldType.RglSftBool:
		{
			bool flag = (bool)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.b = (flag ? 1 : 0);
			break;
		}
		case RglScriptFieldType.RglSftInt:
			scriptComponentFieldHolder.i = (int)Convert.ChangeType(fieldOfClass.GetValue(managedObject), typeof(int));
			break;
		case RglScriptFieldType.RglSftVec3:
		{
			Vec3 c = (Vec3)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.v3 = new Vec3(c, c.w);
			break;
		}
		case RglScriptFieldType.RglSftMatrixFrame:
		{
			MatrixFrame matrixFrame = (MatrixFrame)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.matrixFrame = matrixFrame;
			break;
		}
		case RglScriptFieldType.RglSftEntity:
		{
			GameEntity gameEntity = (GameEntity)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.entityPointer = ((gameEntity != null) ? ((UIntPtr)Convert.ChangeType(gameEntity.Pointer, typeof(UIntPtr))) : ((UIntPtr)0uL));
			break;
		}
		case RglScriptFieldType.RglSftTexture:
		{
			Texture texture = (Texture)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.texturePointer = ((texture != null) ? ((UIntPtr)Convert.ChangeType(texture.Pointer, typeof(UIntPtr))) : ((UIntPtr)0uL));
			break;
		}
		case RglScriptFieldType.RglSftMesh:
		{
			MetaMesh metaMesh = (MetaMesh)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.meshPointer = ((metaMesh != null) ? ((UIntPtr)Convert.ChangeType(metaMesh.Pointer, typeof(UIntPtr))) : ((UIntPtr)0uL));
			break;
		}
		case RglScriptFieldType.RglSftEnum:
			scriptComponentFieldHolder.enumValue = fieldOfClass.GetValue(managedObject).ToString();
			break;
		case RglScriptFieldType.RglSftMaterial:
		{
			Material material = (Material)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.materialPointer = ((material != null) ? ((UIntPtr)Convert.ChangeType(material.Pointer, typeof(UIntPtr))) : ((UIntPtr)0uL));
			break;
		}
		case RglScriptFieldType.RglSftColor:
		{
			Color color = (Color)fieldOfClass.GetValue(managedObject);
			scriptComponentFieldHolder.color.x = color.Red;
			scriptComponentFieldHolder.color.y = color.Green;
			scriptComponentFieldHolder.color.z = color.Blue;
			scriptComponentFieldHolder.color.w = color.Alpha;
			break;
		}
		case RglScriptFieldType.RglSftButton:
			break;
		}
	}

	[EngineCallback(null, false)]
	internal static void CopyObjectFieldsFrom(DotNetObject dst, DotNetObject src, string className, int callFieldChangeEventAsInteger)
	{
		foreach (KeyValuePair<uint, FieldInfo> item in Managed.GetEditableFieldsOfClass(Managed.GetStringHashCode(className)))
		{
			FieldInfo value = item.Value;
			value.SetValue(dst, value.GetValue(src));
		}
	}

	[EngineCallback(null, false)]
	internal static DotNetObject CreateScriptComponentInstance(string className, UIntPtr entityPtr, ManagedScriptComponent managedScriptComponent)
	{
		ScriptComponentBehavior scriptComponentBehavior = null;
		Func<ScriptComponentBehavior> func = (Func<ScriptComponentBehavior>)Managed.GetConstructorDelegateOfClass(className);
		if (func != null)
		{
			scriptComponentBehavior = func();
			scriptComponentBehavior?.Construct(entityPtr, managedScriptComponent);
		}
		else
		{
			ConstructorInfo constructorOfClass = Managed.GetConstructorOfClass(className);
			if (constructorOfClass != null)
			{
				scriptComponentBehavior = constructorOfClass.Invoke(new object[0]) as ScriptComponentBehavior;
				scriptComponentBehavior?.Construct(entityPtr, managedScriptComponent);
			}
			else
			{
				MBDebug.ShowWarning("CreateScriptComponentInstance failed: " + className);
			}
		}
		return scriptComponentBehavior;
	}

	[EngineCallback(null, false)]
	internal static string GetScriptComponentClassNames()
	{
		List<Type> list = new List<Type>();
		foreach (Type value in Managed.ModuleTypes.Values)
		{
			if (!value.IsAbstract && typeof(ScriptComponentBehavior).IsAssignableFrom(value))
			{
				list.Add(value);
			}
		}
		string text = "";
		for (int i = 0; i < list.Count; i++)
		{
			Type type = list[i];
			string text2 = type.Name;
			string text3 = "!";
			object[] customAttributesSafe = type.GetCustomAttributesSafe(typeof(ScriptComponentParams), inherit: true);
			if (customAttributesSafe.Length != 0)
			{
				ScriptComponentParams scriptComponentParams = (ScriptComponentParams)customAttributesSafe[0];
				if (scriptComponentParams.NameOverride.Length > 0)
				{
					text2 = scriptComponentParams.NameOverride;
				}
				if (scriptComponentParams.Tag.Length > 0)
				{
					text3 = scriptComponentParams.Tag;
				}
			}
			text += text2;
			text += "-";
			text += type.BaseType.Name;
			text += "-";
			text += text3;
			if (i + 1 != list.Count)
			{
				text += " ";
			}
		}
		return text;
	}

	[EngineCallback(null, false)]
	internal static bool GetEditorVisibilityOfField(uint classNameHash, uint fieldNamehash)
	{
		object[] customAttributesSafe = Managed.GetFieldOfClass(classNameHash, fieldNamehash).GetCustomAttributesSafe(typeof(EditorVisibleScriptComponentVariable), inherit: true);
		if (customAttributesSafe.Length != 0)
		{
			return (customAttributesSafe[0] as EditorVisibleScriptComponentVariable).Visible;
		}
		return true;
	}

	[EngineCallback(null, false)]
	internal static RglScriptFieldType GetTypeOfField(uint classNameHash, uint fieldNameHash)
	{
		FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
		if (fieldOfClass == null)
		{
			return RglScriptFieldType.RglSftInvalid;
		}
		Type fieldType = fieldOfClass.FieldType;
		if (fieldOfClass.FieldType == typeof(string))
		{
			return RglScriptFieldType.RglSftString;
		}
		if (fieldOfClass.FieldType == typeof(double))
		{
			return RglScriptFieldType.RglSftDouble;
		}
		if (fieldOfClass.FieldType.IsEnum)
		{
			return RglScriptFieldType.RglSftEnum;
		}
		if (fieldOfClass.FieldType == typeof(float))
		{
			return RglScriptFieldType.RglSftFloat;
		}
		if (fieldOfClass.FieldType == typeof(bool))
		{
			return RglScriptFieldType.RglSftBool;
		}
		if (fieldType == typeof(byte) || fieldType == typeof(sbyte) || fieldType == typeof(short) || fieldType == typeof(ushort) || fieldType == typeof(int) || fieldType == typeof(uint) || fieldType == typeof(long) || fieldType == typeof(ulong))
		{
			return RglScriptFieldType.RglSftInt;
		}
		if (fieldOfClass.FieldType == typeof(Vec3))
		{
			return RglScriptFieldType.RglSftVec3;
		}
		if (fieldOfClass.FieldType == typeof(GameEntity))
		{
			return RglScriptFieldType.RglSftEntity;
		}
		if (fieldOfClass.FieldType == typeof(Texture))
		{
			return RglScriptFieldType.RglSftTexture;
		}
		if (fieldOfClass.FieldType == typeof(MetaMesh))
		{
			return RglScriptFieldType.RglSftMesh;
		}
		if (fieldOfClass.FieldType == typeof(Material))
		{
			return RglScriptFieldType.RglSftMaterial;
		}
		if (fieldOfClass.FieldType == typeof(SimpleButton))
		{
			return RglScriptFieldType.RglSftButton;
		}
		if (fieldOfClass.FieldType == typeof(MatrixFrame))
		{
			return RglScriptFieldType.RglSftMatrixFrame;
		}
		if (fieldOfClass.FieldType == typeof(Color))
		{
			return RglScriptFieldType.RglSftColor;
		}
		return RglScriptFieldType.RglSftInvalid;
	}

	[EngineCallback(null, false)]
	internal static void ForceGarbageCollect()
	{
		Utilities.FlushManagedObjectsMemory();
	}

	[EngineCallback(null, false)]
	internal static void CollectCommandLineFunctions()
	{
		foreach (string item in CommandLineFunctionality.CollectCommandLineFunctions())
		{
			Utilities.AddCommandLineFunction(item);
		}
	}
}
