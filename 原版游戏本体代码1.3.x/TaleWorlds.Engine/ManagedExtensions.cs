using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000058 RID: 88
	public static class ManagedExtensions
	{
		// Token: 0x060008B8 RID: 2232 RVA: 0x00006F70 File Offset: 0x00005170
		private static void OnEditorVariableChanged(DotNetObject managedObject, uint classNameHash, uint fieldNameHash)
		{
		}

		// Token: 0x060008B9 RID: 2233 RVA: 0x00006F74 File Offset: 0x00005174
		[EngineCallback(null, false)]
		internal static void SetObjectFieldString(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, string value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008BA RID: 2234 RVA: 0x00006FAC File Offset: 0x000051AC
		[EngineCallback(null, false)]
		internal static void SetObjectFieldDouble(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, double value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008BB RID: 2235 RVA: 0x00006FF8 File Offset: 0x000051F8
		[EngineCallback(null, false)]
		internal static void SetObjectFieldFloat(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, float value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008BC RID: 2236 RVA: 0x00007044 File Offset: 0x00005244
		[EngineCallback(null, false)]
		internal static void SetObjectFieldBool(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, bool value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008BD RID: 2237 RVA: 0x00007090 File Offset: 0x00005290
		[EngineCallback(null, false)]
		internal static void SetObjectFieldInt(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, int value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008BE RID: 2238 RVA: 0x000070DC File Offset: 0x000052DC
		[EngineCallback(null, false)]
		internal static void SetObjectFieldVec3(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008BF RID: 2239 RVA: 0x00007128 File Offset: 0x00005328
		[EngineCallback(null, false)]
		internal static void SetObjectFieldEntity(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new GameEntity(value) : null);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C0 RID: 2240 RVA: 0x00007184 File Offset: 0x00005384
		[EngineCallback(null, false)]
		internal static void SetObjectFieldTexture(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new Texture(value) : null);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C1 RID: 2241 RVA: 0x000071E0 File Offset: 0x000053E0
		[EngineCallback(null, false)]
		internal static void SetObjectFieldMesh(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new MetaMesh(value) : null);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C2 RID: 2242 RVA: 0x0000723C File Offset: 0x0000543C
		[EngineCallback(null, false)]
		internal static void SetObjectFieldMaterial(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, UIntPtr value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, (value != UIntPtr.Zero) ? new Material(value) : null);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C3 RID: 2243 RVA: 0x00007298 File Offset: 0x00005498
		[EngineCallback(null, false)]
		internal static void SetObjectFieldColor(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, Vec3 value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, new Color
			{
				Red = value.x,
				Green = value.y,
				Blue = value.z,
				Alpha = value.w
			});
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C4 RID: 2244 RVA: 0x00007320 File Offset: 0x00005520
		[EngineCallback(null, false)]
		internal static void SetObjectFieldMatrixFrame(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, MatrixFrame value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			fieldOfClass.SetValue(managedObject, value);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C5 RID: 2245 RVA: 0x0000736C File Offset: 0x0000556C
		[EngineCallback(null, false)]
		internal static void SetObjectFieldEnum(DotNetObject managedObject, uint classNameHash, uint fieldNameHash, string value, int callFieldChangeEventAsInteger)
		{
			bool flag = callFieldChangeEventAsInteger != 0;
			string name = managedObject.GetType().Name;
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			if (fieldOfClass == null)
			{
				return;
			}
			object value2 = Enum.Parse(fieldOfClass.FieldType, value);
			fieldOfClass.SetValue(managedObject, value2);
			if (flag)
			{
				ManagedExtensions.OnEditorVariableChanged(managedObject, classNameHash, fieldNameHash);
			}
		}

		// Token: 0x060008C6 RID: 2246 RVA: 0x000073C0 File Offset: 0x000055C0
		[EngineCallback(null, false)]
		internal static void GetObjectField(DotNetObject managedObject, uint classNameHash, ref ScriptComponentFieldHolder scriptComponentFieldHolder, uint fieldNameHash, RglScriptFieldType type)
		{
			FieldInfo fieldOfClass = Managed.GetFieldOfClass(classNameHash, fieldNameHash);
			switch (type)
			{
			case RglScriptFieldType.RglSftString:
				scriptComponentFieldHolder.s = (string)fieldOfClass.GetValue(managedObject);
				return;
			case RglScriptFieldType.RglSftDouble:
				scriptComponentFieldHolder.d = (double)Convert.ChangeType(fieldOfClass.GetValue(managedObject), typeof(double));
				return;
			case RglScriptFieldType.RglSftFloat:
				scriptComponentFieldHolder.f = (float)Convert.ChangeType(fieldOfClass.GetValue(managedObject), typeof(float));
				return;
			case RglScriptFieldType.RglSftBool:
				scriptComponentFieldHolder.b = (((bool)fieldOfClass.GetValue(managedObject)) ? 1 : 0);
				return;
			case RglScriptFieldType.RglSftInt:
				scriptComponentFieldHolder.i = (int)Convert.ChangeType(fieldOfClass.GetValue(managedObject), typeof(int));
				return;
			case RglScriptFieldType.RglSftVec3:
			{
				Vec3 vec = (Vec3)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.v3 = new Vec3(vec, vec.w);
				return;
			}
			case RglScriptFieldType.RglSftEntity:
			{
				GameEntity gameEntity = (GameEntity)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.entityPointer = ((gameEntity != null) ? ((UIntPtr)Convert.ChangeType(gameEntity.Pointer, typeof(UIntPtr))) : ((UIntPtr)0UL));
				return;
			}
			case RglScriptFieldType.RglSftTexture:
			{
				Texture texture = (Texture)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.texturePointer = ((texture != null) ? ((UIntPtr)Convert.ChangeType(texture.Pointer, typeof(UIntPtr))) : ((UIntPtr)0UL));
				return;
			}
			case RglScriptFieldType.RglSftMesh:
			{
				MetaMesh metaMesh = (MetaMesh)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.meshPointer = ((metaMesh != null) ? ((UIntPtr)Convert.ChangeType(metaMesh.Pointer, typeof(UIntPtr))) : ((UIntPtr)0UL));
				return;
			}
			case RglScriptFieldType.RglSftEnum:
				scriptComponentFieldHolder.enumValue = fieldOfClass.GetValue(managedObject).ToString();
				return;
			case RglScriptFieldType.RglSftMaterial:
			{
				Material material = (Material)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.materialPointer = ((material != null) ? ((UIntPtr)Convert.ChangeType(material.Pointer, typeof(UIntPtr))) : ((UIntPtr)0UL));
				return;
			}
			case RglScriptFieldType.RglSftButton:
				break;
			case RglScriptFieldType.RglSftColor:
			{
				Color color = (Color)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.color.x = color.Red;
				scriptComponentFieldHolder.color.y = color.Green;
				scriptComponentFieldHolder.color.z = color.Blue;
				scriptComponentFieldHolder.color.w = color.Alpha;
				break;
			}
			case RglScriptFieldType.RglSftMatrixFrame:
			{
				MatrixFrame matrixFrame = (MatrixFrame)fieldOfClass.GetValue(managedObject);
				scriptComponentFieldHolder.matrixFrame = matrixFrame;
				return;
			}
			default:
				return;
			}
		}

		// Token: 0x060008C7 RID: 2247 RVA: 0x0000766C File Offset: 0x0000586C
		[EngineCallback(null, false)]
		internal static void CopyObjectFieldsFrom(DotNetObject dst, DotNetObject src, string className, int callFieldChangeEventAsInteger)
		{
			foreach (KeyValuePair<uint, FieldInfo> keyValuePair in Managed.GetEditableFieldsOfClass(Managed.GetStringHashCode(className)))
			{
				FieldInfo value = keyValuePair.Value;
				value.SetValue(dst, value.GetValue(src));
			}
		}

		// Token: 0x060008C8 RID: 2248 RVA: 0x000076D4 File Offset: 0x000058D4
		[EngineCallback(null, false)]
		internal static DotNetObject CreateScriptComponentInstance(string className, UIntPtr entityPtr, ManagedScriptComponent managedScriptComponent)
		{
			ScriptComponentBehavior scriptComponentBehavior = null;
			Func<ScriptComponentBehavior> func = (Func<ScriptComponentBehavior>)Managed.GetConstructorDelegateOfClass(className);
			if (func != null)
			{
				scriptComponentBehavior = func();
				if (scriptComponentBehavior != null)
				{
					scriptComponentBehavior.Construct(entityPtr, managedScriptComponent);
				}
			}
			else
			{
				ConstructorInfo constructorOfClass = Managed.GetConstructorOfClass(className);
				if (constructorOfClass != null)
				{
					scriptComponentBehavior = constructorOfClass.Invoke(new object[0]) as ScriptComponentBehavior;
					if (scriptComponentBehavior != null)
					{
						scriptComponentBehavior.Construct(entityPtr, managedScriptComponent);
					}
				}
				else
				{
					MBDebug.ShowWarning("CreateScriptComponentInstance failed: " + className);
				}
			}
			return scriptComponentBehavior;
		}

		// Token: 0x060008C9 RID: 2249 RVA: 0x00007748 File Offset: 0x00005948
		[EngineCallback(null, false)]
		internal static string GetScriptComponentClassNames()
		{
			List<Type> list = new List<Type>();
			foreach (Type type in Managed.ModuleTypes.Values)
			{
				if (!type.IsAbstract && typeof(ScriptComponentBehavior).IsAssignableFrom(type))
				{
					list.Add(type);
				}
			}
			string text = "";
			for (int i = 0; i < list.Count; i++)
			{
				Type type2 = list[i];
				string str = type2.Name;
				string str2 = "!";
				object[] customAttributesSafe = type2.GetCustomAttributesSafe(typeof(ScriptComponentParams), true);
				if (customAttributesSafe.Length != 0)
				{
					ScriptComponentParams scriptComponentParams = (ScriptComponentParams)customAttributesSafe[0];
					if (scriptComponentParams.NameOverride.Length > 0)
					{
						str = scriptComponentParams.NameOverride;
					}
					if (scriptComponentParams.Tag.Length > 0)
					{
						str2 = scriptComponentParams.Tag;
					}
				}
				text += str;
				text += "-";
				text += type2.BaseType.Name;
				text += "-";
				text += str2;
				if (i + 1 != list.Count)
				{
					text += " ";
				}
			}
			return text;
		}

		// Token: 0x060008CA RID: 2250 RVA: 0x000078A4 File Offset: 0x00005AA4
		[EngineCallback(null, false)]
		internal static bool GetEditorVisibilityOfField(uint classNameHash, uint fieldNamehash)
		{
			object[] customAttributesSafe = Managed.GetFieldOfClass(classNameHash, fieldNamehash).GetCustomAttributesSafe(typeof(EditorVisibleScriptComponentVariable), true);
			return customAttributesSafe.Length == 0 || (customAttributesSafe[0] as EditorVisibleScriptComponentVariable).Visible;
		}

		// Token: 0x060008CB RID: 2251 RVA: 0x000078DC File Offset: 0x00005ADC
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

		// Token: 0x060008CC RID: 2252 RVA: 0x00007AD6 File Offset: 0x00005CD6
		[EngineCallback(null, false)]
		internal static void ForceGarbageCollect()
		{
			Utilities.FlushManagedObjectsMemory();
		}

		// Token: 0x060008CD RID: 2253 RVA: 0x00007AE0 File Offset: 0x00005CE0
		[EngineCallback(null, false)]
		internal static void CollectCommandLineFunctions()
		{
			foreach (string concatName in CommandLineFunctionality.CollectCommandLineFunctions())
			{
				Utilities.AddCommandLineFunction(concatName);
			}
		}
	}
}
