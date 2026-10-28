using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ManagedCallbacks;

internal static class ScriptingInterfaceObjects
{
	private enum LibraryInterfaceGeneratedEnum
	{
		enm_IMono_LibrarySizeChecker_get_engine_struct_member_offset,
		enm_IMono_LibrarySizeChecker_get_engine_struct_size,
		enm_IMono_Managed_decrease_reference_count,
		enm_IMono_Managed_get_class_type_definition,
		enm_IMono_Managed_get_class_type_definition_count,
		enm_IMono_Managed_increase_reference_count,
		enm_IMono_Managed_release_managed_object,
		enm_IMono_NativeArray_add_element,
		enm_IMono_NativeArray_add_float_element,
		enm_IMono_NativeArray_add_integer_element,
		enm_IMono_NativeArray_clear,
		enm_IMono_NativeArray_create,
		enm_IMono_NativeArray_get_data_pointer,
		enm_IMono_NativeArray_get_data_pointer_offset,
		enm_IMono_NativeArray_get_data_size,
		enm_IMono_NativeObjectArray_add_element,
		enm_IMono_NativeObjectArray_clear,
		enm_IMono_NativeObjectArray_create,
		enm_IMono_NativeObjectArray_get_count,
		enm_IMono_NativeObjectArray_get_element_at_index,
		enm_IMono_NativeString_create,
		enm_IMono_NativeString_get_string,
		enm_IMono_NativeString_set_string,
		enm_IMono_NativeStringHelper_create_rglVarString,
		enm_IMono_NativeStringHelper_delete_rglVarString,
		enm_IMono_NativeStringHelper_get_thread_local_cached_rglVarString,
		enm_IMono_NativeStringHelper_set_rglVarString,
		enm_IMono_Telemetry_begin_telemetry_scope,
		enm_IMono_Telemetry_end_telemetry_scope,
		enm_IMono_Telemetry_get_telemetry_level_mask,
		enm_IMono_Telemetry_has_telemetry_connection,
		enm_IMono_Telemetry_start_telemetry_connection,
		enm_IMono_Telemetry_stop_telemetry_connection
	}

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

	public static void SetFunctionPointer(int id, IntPtr pointer)
	{
		switch ((LibraryInterfaceGeneratedEnum)id)
		{
		case LibraryInterfaceGeneratedEnum.enm_IMono_LibrarySizeChecker_get_engine_struct_member_offset:
			ScriptingInterfaceOfILibrarySizeChecker.call_GetEngineStructMemberOffsetDelegate = (ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructMemberOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructMemberOffsetDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_LibrarySizeChecker_get_engine_struct_size:
			ScriptingInterfaceOfILibrarySizeChecker.call_GetEngineStructSizeDelegate = (ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfILibrarySizeChecker.GetEngineStructSizeDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Managed_decrease_reference_count:
			ScriptingInterfaceOfIManaged.call_DecreaseReferenceCountDelegate = (ScriptingInterfaceOfIManaged.DecreaseReferenceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.DecreaseReferenceCountDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Managed_get_class_type_definition:
			ScriptingInterfaceOfIManaged.call_GetClassTypeDefinitionDelegate = (ScriptingInterfaceOfIManaged.GetClassTypeDefinitionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.GetClassTypeDefinitionDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Managed_get_class_type_definition_count:
			ScriptingInterfaceOfIManaged.call_GetClassTypeDefinitionCountDelegate = (ScriptingInterfaceOfIManaged.GetClassTypeDefinitionCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.GetClassTypeDefinitionCountDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Managed_increase_reference_count:
			ScriptingInterfaceOfIManaged.call_IncreaseReferenceCountDelegate = (ScriptingInterfaceOfIManaged.IncreaseReferenceCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.IncreaseReferenceCountDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Managed_release_managed_object:
			ScriptingInterfaceOfIManaged.call_ReleaseManagedObjectDelegate = (ScriptingInterfaceOfIManaged.ReleaseManagedObjectDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfIManaged.ReleaseManagedObjectDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_add_element:
			ScriptingInterfaceOfINativeArray.call_AddElementDelegate = (ScriptingInterfaceOfINativeArray.AddElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.AddElementDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_add_float_element:
			ScriptingInterfaceOfINativeArray.call_AddFloatElementDelegate = (ScriptingInterfaceOfINativeArray.AddFloatElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.AddFloatElementDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_add_integer_element:
			ScriptingInterfaceOfINativeArray.call_AddIntegerElementDelegate = (ScriptingInterfaceOfINativeArray.AddIntegerElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.AddIntegerElementDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_clear:
			ScriptingInterfaceOfINativeArray.call_ClearDelegate = (ScriptingInterfaceOfINativeArray.ClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.ClearDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_create:
			ScriptingInterfaceOfINativeArray.call_CreateDelegate = (ScriptingInterfaceOfINativeArray.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.CreateDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_get_data_pointer:
			ScriptingInterfaceOfINativeArray.call_GetDataPointerDelegate = (ScriptingInterfaceOfINativeArray.GetDataPointerDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.GetDataPointerDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_get_data_pointer_offset:
			ScriptingInterfaceOfINativeArray.call_GetDataPointerOffsetDelegate = (ScriptingInterfaceOfINativeArray.GetDataPointerOffsetDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.GetDataPointerOffsetDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeArray_get_data_size:
			ScriptingInterfaceOfINativeArray.call_GetDataSizeDelegate = (ScriptingInterfaceOfINativeArray.GetDataSizeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeArray.GetDataSizeDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeObjectArray_add_element:
			ScriptingInterfaceOfINativeObjectArray.call_AddElementDelegate = (ScriptingInterfaceOfINativeObjectArray.AddElementDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.AddElementDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeObjectArray_clear:
			ScriptingInterfaceOfINativeObjectArray.call_ClearDelegate = (ScriptingInterfaceOfINativeObjectArray.ClearDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.ClearDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeObjectArray_create:
			ScriptingInterfaceOfINativeObjectArray.call_CreateDelegate = (ScriptingInterfaceOfINativeObjectArray.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.CreateDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeObjectArray_get_count:
			ScriptingInterfaceOfINativeObjectArray.call_GetCountDelegate = (ScriptingInterfaceOfINativeObjectArray.GetCountDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.GetCountDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeObjectArray_get_element_at_index:
			ScriptingInterfaceOfINativeObjectArray.call_GetElementAtIndexDelegate = (ScriptingInterfaceOfINativeObjectArray.GetElementAtIndexDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeObjectArray.GetElementAtIndexDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeString_create:
			ScriptingInterfaceOfINativeString.call_CreateDelegate = (ScriptingInterfaceOfINativeString.CreateDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeString.CreateDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeString_get_string:
			ScriptingInterfaceOfINativeString.call_GetStringDelegate = (ScriptingInterfaceOfINativeString.GetStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeString.GetStringDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeString_set_string:
			ScriptingInterfaceOfINativeString.call_SetStringDelegate = (ScriptingInterfaceOfINativeString.SetStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeString.SetStringDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeStringHelper_create_rglVarString:
			ScriptingInterfaceOfINativeStringHelper.call_CreateRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.CreateRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.CreateRglVarStringDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeStringHelper_delete_rglVarString:
			ScriptingInterfaceOfINativeStringHelper.call_DeleteRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.DeleteRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.DeleteRglVarStringDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeStringHelper_get_thread_local_cached_rglVarString:
			ScriptingInterfaceOfINativeStringHelper.call_GetThreadLocalCachedRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.GetThreadLocalCachedRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.GetThreadLocalCachedRglVarStringDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_NativeStringHelper_set_rglVarString:
			ScriptingInterfaceOfINativeStringHelper.call_SetRglVarStringDelegate = (ScriptingInterfaceOfINativeStringHelper.SetRglVarStringDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfINativeStringHelper.SetRglVarStringDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Telemetry_begin_telemetry_scope:
			ScriptingInterfaceOfITelemetry.call_BeginTelemetryScopeDelegate = (ScriptingInterfaceOfITelemetry.BeginTelemetryScopeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.BeginTelemetryScopeDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Telemetry_end_telemetry_scope:
			ScriptingInterfaceOfITelemetry.call_EndTelemetryScopeDelegate = (ScriptingInterfaceOfITelemetry.EndTelemetryScopeDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.EndTelemetryScopeDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Telemetry_get_telemetry_level_mask:
			ScriptingInterfaceOfITelemetry.call_GetTelemetryLevelMaskDelegate = (ScriptingInterfaceOfITelemetry.GetTelemetryLevelMaskDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.GetTelemetryLevelMaskDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Telemetry_has_telemetry_connection:
			ScriptingInterfaceOfITelemetry.call_HasTelemetryConnectionDelegate = (ScriptingInterfaceOfITelemetry.HasTelemetryConnectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.HasTelemetryConnectionDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Telemetry_start_telemetry_connection:
			ScriptingInterfaceOfITelemetry.call_StartTelemetryConnectionDelegate = (ScriptingInterfaceOfITelemetry.StartTelemetryConnectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.StartTelemetryConnectionDelegate));
			break;
		case LibraryInterfaceGeneratedEnum.enm_IMono_Telemetry_stop_telemetry_connection:
			ScriptingInterfaceOfITelemetry.call_StopTelemetryConnectionDelegate = (ScriptingInterfaceOfITelemetry.StopTelemetryConnectionDelegate)Marshal.GetDelegateForFunctionPointer(pointer, typeof(ScriptingInterfaceOfITelemetry.StopTelemetryConnectionDelegate));
			break;
		}
	}
}
