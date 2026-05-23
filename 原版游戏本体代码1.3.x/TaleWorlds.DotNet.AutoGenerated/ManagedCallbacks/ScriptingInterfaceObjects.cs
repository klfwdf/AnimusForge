using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ManagedCallbacks
{
	// Token: 0x02000005 RID: 5
	internal static class ScriptingInterfaceObjects
	{
		// Token: 0x06000039 RID: 57 RVA: 0x00002760 File Offset: 0x00000960
		public static Dictionary<string, object> GetObjects()
		{
			return new Dictionary<string, object>
			{
				{
					"TaleWorlds.DotNet.ILibrarySizeChecker",
					new ScriptingInterfaceOfILibrarySizeChecker()
				},
				{
					"TaleWorlds.DotNet.IManaged",
					new ScriptingInterfaceOfIManaged()
				},
				{
					"TaleWorlds.DotNet.INativeArray",
					new ScriptingInterfaceOfINativeArray()
				},
				{
					"TaleWorlds.DotNet.INativeObjectArray",
					new ScriptingInterfaceOfINativeObjectArray()
				},
				{
					"TaleWorlds.DotNet.INativeString",
					new ScriptingInterfaceOfINativeString()
				},
				{
					"TaleWorlds.DotNet.INativeStringHelper",
					new ScriptingInterfaceOfINativeStringHelper()
				},
				{
					"TaleWorlds.DotNet.ITelemetry",
					new ScriptingInterfaceOfITelemetry()
				}
			};
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000027E4 File Offset: 0x000009E4
		public static void SetFunctionPointer(int id, IntPtr pointer)
		{
			switch (id)
			{
			case 0:
				ScriptingInterfaceOfILibrarySizeChecker.call_GetEngineStructMemberOffsetDelegate = (ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructMemberOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructMemberOffsetDelegate));
				return;
			case 1:
				ScriptingInterfaceOfILibrarySizeChecker.call_GetEngineStructSizeDelegate = (ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructSizeDelegate));
				return;
			case 2:
				ScriptingInterfaceOfIManaged.call_DecreaseReferenceCountDelegate = (ScriptingInterfaceOfIManaged.DecreaseReferenceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.DecreaseReferenceCountDelegate));
				return;
			case 3:
				ScriptingInterfaceOfIManaged.call_GetClassTypeDefinitionDelegate = (ScriptingInterfaceOfIManaged.GetClassTypeDefinitionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.GetClassTypeDefinitionDelegate));
				return;
			case 4:
				ScriptingInterfaceOfIManaged.call_GetClassTypeDefinitionCountDelegate = (ScriptingInterfaceOfIManaged.GetClassTypeDefinitionCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.GetClassTypeDefinitionCountDelegate));
				return;
			case 5:
				ScriptingInterfaceOfIManaged.call_IncreaseReferenceCountDelegate = (ScriptingInterfaceOfIManaged.IncreaseReferenceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.IncreaseReferenceCountDelegate));
				return;
			case 6:
				ScriptingInterfaceOfIManaged.call_ReleaseManagedObjectDelegate = (ScriptingInterfaceOfIManaged.ReleaseManagedObjectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.ReleaseManagedObjectDelegate));
				return;
			case 7:
				ScriptingInterfaceOfINativeArray.call_AddElementDelegate = (ScriptingInterfaceOfINativeArray.AddElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.AddElementDelegate));
				return;
			case 8:
				ScriptingInterfaceOfINativeArray.call_AddFloatElementDelegate = (ScriptingInterfaceOfINativeArray.AddFloatElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.AddFloatElementDelegate));
				return;
			case 9:
				ScriptingInterfaceOfINativeArray.call_AddIntegerElementDelegate = (ScriptingInterfaceOfINativeArray.AddIntegerElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.AddIntegerElementDelegate));
				return;
			case 10:
				ScriptingInterfaceOfINativeArray.call_ClearDelegate = (ScriptingInterfaceOfINativeArray.ClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.ClearDelegate));
				return;
			case 11:
				ScriptingInterfaceOfINativeArray.call_CreateDelegate = (ScriptingInterfaceOfINativeArray.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.CreateDelegate));
				return;
			case 12:
				ScriptingInterfaceOfINativeArray.call_GetDataPointerDelegate = (ScriptingInterfaceOfINativeArray.GetDataPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.GetDataPointerDelegate));
				return;
			case 13:
				ScriptingInterfaceOfINativeArray.call_GetDataPointerOffsetDelegate = (ScriptingInterfaceOfINativeArray.GetDataPointerOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.GetDataPointerOffsetDelegate));
				return;
			case 14:
				ScriptingInterfaceOfINativeArray.call_GetDataSizeDelegate = (ScriptingInterfaceOfINativeArray.GetDataSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.GetDataSizeDelegate));
				return;
			case 15:
				ScriptingInterfaceOfINativeObjectArray.call_AddElementDelegate = (ScriptingInterfaceOfINativeObjectArray.AddElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.AddElementDelegate));
				return;
			case 16:
				ScriptingInterfaceOfINativeObjectArray.call_ClearDelegate = (ScriptingInterfaceOfINativeObjectArray.ClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.ClearDelegate));
				return;
			case 17:
				ScriptingInterfaceOfINativeObjectArray.call_CreateDelegate = (ScriptingInterfaceOfINativeObjectArray.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.CreateDelegate));
				return;
			case 18:
				ScriptingInterfaceOfINativeObjectArray.call_GetCountDelegate = (ScriptingInterfaceOfINativeObjectArray.GetCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.GetCountDelegate));
				return;
			case 19:
				ScriptingInterfaceOfINativeObjectArray.call_GetElementAtIndexDelegate = (ScriptingInterfaceOfINativeObjectArray.GetElementAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.GetElementAtIndexDelegate));
				return;
			case 20:
				ScriptingInterfaceOfINativeString.call_CreateDelegate = (ScriptingInterfaceOfINativeString.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeString.CreateDelegate));
				return;
			case 21:
				ScriptingInterfaceOfINativeString.call_GetStringDelegate = (ScriptingInterfaceOfINativeString.GetStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeString.GetStringDelegate));
				return;
			case 22:
				ScriptingInterfaceOfINativeString.call_SetStringDelegate = (ScriptingInterfaceOfINativeString.SetStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeString.SetStringDelegate));
				return;
			case 23:
				ScriptingInterfaceOfINativeStringHelper.call_CreateRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.CreateRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.CreateRglVarStringDelegate));
				return;
			case 24:
				ScriptingInterfaceOfINativeStringHelper.call_DeleteRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.DeleteRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.DeleteRglVarStringDelegate));
				return;
			case 25:
				ScriptingInterfaceOfINativeStringHelper.call_GetThreadLocalCachedRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.GetThreadLocalCachedRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.GetThreadLocalCachedRglVarStringDelegate));
				return;
			case 26:
				ScriptingInterfaceOfINativeStringHelper.call_SetRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.SetRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.SetRglVarStringDelegate));
				return;
			case 27:
				ScriptingInterfaceOfITelemetry.call_BeginTelemetryScopeDelegate = (ScriptingInterfaceOfITelemetry.BeginTelemetryScopeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.BeginTelemetryScopeDelegate));
				return;
			case 28:
				ScriptingInterfaceOfITelemetry.call_EndTelemetryScopeDelegate = (ScriptingInterfaceOfITelemetry.EndTelemetryScopeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.EndTelemetryScopeDelegate));
				return;
			case 29:
				ScriptingInterfaceOfITelemetry.call_GetTelemetryLevelMaskDelegate = (ScriptingInterfaceOfITelemetry.GetTelemetryLevelMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.GetTelemetryLevelMaskDelegate));
				return;
			case 30:
				ScriptingInterfaceOfITelemetry.call_HasTelemetryConnectionDelegate = (ScriptingInterfaceOfITelemetry.HasTelemetryConnectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.HasTelemetryConnectionDelegate));
				return;
			case 31:
				ScriptingInterfaceOfITelemetry.call_StartTelemetryConnectionDelegate = (ScriptingInterfaceOfITelemetry.StartTelemetryConnectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.StartTelemetryConnectionDelegate));
				return;
			case 32:
				ScriptingInterfaceOfITelemetry.call_StopTelemetryConnectionDelegate = (ScriptingInterfaceOfITelemetry.StopTelemetryConnectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.StopTelemetryConnectionDelegate));
				return;
			default:
				return;
			}
		}

		// Token: 0x02000037 RID: 55
		private enum LibraryInterfaceGeneratedEnum
		{
			// Token: 0x0400002B RID: 43
			enm_IMono_LibrarySizeChecker_get_engine_struct_member_offset,
			// Token: 0x0400002C RID: 44
			enm_IMono_LibrarySizeChecker_get_engine_struct_size,
			// Token: 0x0400002D RID: 45
			enm_IMono_Managed_decrease_reference_count,
			// Token: 0x0400002E RID: 46
			enm_IMono_Managed_get_class_type_definition,
			// Token: 0x0400002F RID: 47
			enm_IMono_Managed_get_class_type_definition_count,
			// Token: 0x04000030 RID: 48
			enm_IMono_Managed_increase_reference_count,
			// Token: 0x04000031 RID: 49
			enm_IMono_Managed_release_managed_object,
			// Token: 0x04000032 RID: 50
			enm_IMono_NativeArray_add_element,
			// Token: 0x04000033 RID: 51
			enm_IMono_NativeArray_add_float_element,
			// Token: 0x04000034 RID: 52
			enm_IMono_NativeArray_add_integer_element,
			// Token: 0x04000035 RID: 53
			enm_IMono_NativeArray_clear,
			// Token: 0x04000036 RID: 54
			enm_IMono_NativeArray_create,
			// Token: 0x04000037 RID: 55
			enm_IMono_NativeArray_get_data_pointer,
			// Token: 0x04000038 RID: 56
			enm_IMono_NativeArray_get_data_pointer_offset,
			// Token: 0x04000039 RID: 57
			enm_IMono_NativeArray_get_data_size,
			// Token: 0x0400003A RID: 58
			enm_IMono_NativeObjectArray_add_element,
			// Token: 0x0400003B RID: 59
			enm_IMono_NativeObjectArray_clear,
			// Token: 0x0400003C RID: 60
			enm_IMono_NativeObjectArray_create,
			// Token: 0x0400003D RID: 61
			enm_IMono_NativeObjectArray_get_count,
			// Token: 0x0400003E RID: 62
			enm_IMono_NativeObjectArray_get_element_at_index,
			// Token: 0x0400003F RID: 63
			enm_IMono_NativeString_create,
			// Token: 0x04000040 RID: 64
			enm_IMono_NativeString_get_string,
			// Token: 0x04000041 RID: 65
			enm_IMono_NativeString_set_string,
			// Token: 0x04000042 RID: 66
			enm_IMono_NativeStringHelper_create_rglVarString,
			// Token: 0x04000043 RID: 67
			enm_IMono_NativeStringHelper_delete_rglVarString,
			// Token: 0x04000044 RID: 68
			enm_IMono_NativeStringHelper_get_thread_local_cached_rglVarString,
			// Token: 0x04000045 RID: 69
			enm_IMono_NativeStringHelper_set_rglVarString,
			// Token: 0x04000046 RID: 70
			enm_IMono_Telemetry_begin_telemetry_scope,
			// Token: 0x04000047 RID: 71
			enm_IMono_Telemetry_end_telemetry_scope,
			// Token: 0x04000048 RID: 72
			enm_IMono_Telemetry_get_telemetry_level_mask,
			// Token: 0x04000049 RID: 73
			enm_IMono_Telemetry_has_telemetry_connection,
			// Token: 0x0400004A RID: 74
			enm_IMono_Telemetry_start_telemetry_connection,
			// Token: 0x0400004B RID: 75
			enm_IMono_Telemetry_stop_telemetry_connection
		}
	}
}
