using System;
using System.Diagnostics.Tracing;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Win32
{
	// Token: 0x02000018 RID: 24
	[SecurityCritical]
	[SuppressUnmanagedCodeSecurity]
	internal static class UnsafeNativeMethods
	{
		// Token: 0x06000150 RID: 336
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int GetTimeZoneInformation(out Win32Native.TimeZoneInformation lpTimeZoneInformation);

		// Token: 0x06000151 RID: 337
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern int GetDynamicTimeZoneInformation(out Win32Native.DynamicTimeZoneInformation lpDynamicTimeZoneInformation);

		// Token: 0x06000152 RID: 338
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetFileMUIPath(int flags, [MarshalAs(UnmanagedType.LPWStr)] string filePath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder language, ref int languageLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder fileMuiPath, ref int fileMuiPathLength, ref long enumerator);

		// Token: 0x06000153 RID: 339
		[DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "LoadStringW", ExactSpelling = true, SetLastError = true)]
		internal static extern int LoadString(SafeLibraryHandle handle, int id, StringBuilder buffer, int bufferLength);

		// Token: 0x06000154 RID: 340
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern SafeLibraryHandle LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

		// Token: 0x06000155 RID: 341
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool FreeLibrary(IntPtr hModule);

		// Token: 0x06000156 RID: 342
		[SecurityCritical]
		[DllImport("combase.dll")]
		internal static extern int RoGetActivationFactory([MarshalAs(UnmanagedType.HString)] string activatableClassId, [In] ref Guid iid, [MarshalAs(UnmanagedType.IInspectable)] out object factory);

		// Token: 0x02000ABE RID: 2750
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		internal static class ManifestEtw
		{
			// Token: 0x06006992 RID: 27026
			[SecurityCritical]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal unsafe static extern uint EventRegister([In] ref Guid providerId, [In] UnsafeNativeMethods.ManifestEtw.EtwEnableCallback enableCallback, [In] void* callbackContext, [In] [Out] ref long registrationHandle);

			// Token: 0x06006993 RID: 27027
			[SecurityCritical]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal static extern uint EventUnregister([In] long registrationHandle);

			// Token: 0x06006994 RID: 27028
			[SecurityCritical]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal unsafe static extern int EventWrite([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] int userDataCount, [In] EventProvider.EventData* userData);

			// Token: 0x06006995 RID: 27029
			[SecurityCritical]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal static extern int EventWriteString([In] long registrationHandle, [In] byte level, [In] long keyword, [In] string msg);

			// Token: 0x06006996 RID: 27030 RVA: 0x0016B9B8 File Offset: 0x00169BB8
			internal unsafe static int EventWriteTransferWrapper(long registrationHandle, ref EventDescriptor eventDescriptor, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
			{
				int num = UnsafeNativeMethods.ManifestEtw.EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, relatedActivityId, userDataCount, userData);
				if (num == 87 && relatedActivityId == null)
				{
					Guid empty = Guid.Empty;
					num = UnsafeNativeMethods.ManifestEtw.EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, &empty, userDataCount, userData);
				}
				return num;
			}

			// Token: 0x06006997 RID: 27031
			[SuppressUnmanagedCodeSecurity]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			private unsafe static extern int EventWriteTransfer([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] Guid* activityId, [In] Guid* relatedActivityId, [In] int userDataCount, [In] EventProvider.EventData* userData);

			// Token: 0x06006998 RID: 27032
			[SuppressUnmanagedCodeSecurity]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal static extern int EventActivityIdControl([In] UnsafeNativeMethods.ManifestEtw.ActivityControl ControlCode, [In] [Out] ref Guid ActivityId);

			// Token: 0x06006999 RID: 27033
			[SuppressUnmanagedCodeSecurity]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal unsafe static extern int EventSetInformation([In] long registrationHandle, [In] UnsafeNativeMethods.ManifestEtw.EVENT_INFO_CLASS informationClass, [In] void* eventInformation, [In] int informationLength);

			// Token: 0x0600699A RID: 27034
			[SuppressUnmanagedCodeSecurity]
			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
			internal unsafe static extern int EnumerateTraceGuidsEx(UnsafeNativeMethods.ManifestEtw.TRACE_QUERY_INFO_CLASS TraceQueryInfoClass, void* InBuffer, int InBufferSize, void* OutBuffer, int OutBufferSize, ref int ReturnLength);

			// Token: 0x040030B8 RID: 12472
			internal const int ERROR_ARITHMETIC_OVERFLOW = 534;

			// Token: 0x040030B9 RID: 12473
			internal const int ERROR_NOT_ENOUGH_MEMORY = 8;

			// Token: 0x040030BA RID: 12474
			internal const int ERROR_MORE_DATA = 234;

			// Token: 0x040030BB RID: 12475
			internal const int ERROR_NOT_SUPPORTED = 50;

			// Token: 0x040030BC RID: 12476
			internal const int ERROR_INVALID_PARAMETER = 87;

			// Token: 0x040030BD RID: 12477
			internal const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;

			// Token: 0x040030BE RID: 12478
			internal const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;

			// Token: 0x040030BF RID: 12479
			internal const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;

			// Token: 0x02000CF2 RID: 3314
			// (Invoke) Token: 0x060071CC RID: 29132
			[SecurityCritical]
			internal unsafe delegate void EtwEnableCallback([In] ref Guid sourceId, [In] int isEnabled, [In] byte level, [In] long matchAnyKeywords, [In] long matchAllKeywords, [In] UnsafeNativeMethods.ManifestEtw.EVENT_FILTER_DESCRIPTOR* filterData, [In] void* callbackContext);

			// Token: 0x02000CF3 RID: 3315
			internal struct EVENT_FILTER_DESCRIPTOR
			{
				// Token: 0x040038F0 RID: 14576
				public long Ptr;

				// Token: 0x040038F1 RID: 14577
				public int Size;

				// Token: 0x040038F2 RID: 14578
				public int Type;
			}

			// Token: 0x02000CF4 RID: 3316
			internal enum ActivityControl : uint
			{
				// Token: 0x040038F4 RID: 14580
				EVENT_ACTIVITY_CTRL_GET_ID = 1U,
				// Token: 0x040038F5 RID: 14581
				EVENT_ACTIVITY_CTRL_SET_ID,
				// Token: 0x040038F6 RID: 14582
				EVENT_ACTIVITY_CTRL_CREATE_ID,
				// Token: 0x040038F7 RID: 14583
				EVENT_ACTIVITY_CTRL_GET_SET_ID,
				// Token: 0x040038F8 RID: 14584
				EVENT_ACTIVITY_CTRL_CREATE_SET_ID
			}

			// Token: 0x02000CF5 RID: 3317
			internal enum EVENT_INFO_CLASS
			{
				// Token: 0x040038FA RID: 14586
				BinaryTrackInfo,
				// Token: 0x040038FB RID: 14587
				SetEnableAllKeywords,
				// Token: 0x040038FC RID: 14588
				SetTraits
			}

			// Token: 0x02000CF6 RID: 3318
			internal enum TRACE_QUERY_INFO_CLASS
			{
				// Token: 0x040038FE RID: 14590
				TraceGuidQueryList,
				// Token: 0x040038FF RID: 14591
				TraceGuidQueryInfo,
				// Token: 0x04003900 RID: 14592
				TraceGuidQueryProcess,
				// Token: 0x04003901 RID: 14593
				TraceStackTracingInfo,
				// Token: 0x04003902 RID: 14594
				MaxTraceSetInfoClass
			}

			// Token: 0x02000CF7 RID: 3319
			internal struct TRACE_GUID_INFO
			{
				// Token: 0x04003903 RID: 14595
				public int InstanceCount;

				// Token: 0x04003904 RID: 14596
				public int Reserved;
			}

			// Token: 0x02000CF8 RID: 3320
			internal struct TRACE_PROVIDER_INSTANCE_INFO
			{
				// Token: 0x04003905 RID: 14597
				public int NextOffset;

				// Token: 0x04003906 RID: 14598
				public int EnableCount;

				// Token: 0x04003907 RID: 14599
				public int Pid;

				// Token: 0x04003908 RID: 14600
				public int Flags;
			}

			// Token: 0x02000CF9 RID: 3321
			internal struct TRACE_ENABLE_INFO
			{
				// Token: 0x04003909 RID: 14601
				public int IsEnabled;

				// Token: 0x0400390A RID: 14602
				public byte Level;

				// Token: 0x0400390B RID: 14603
				public byte Reserved1;

				// Token: 0x0400390C RID: 14604
				public ushort LoggerId;

				// Token: 0x0400390D RID: 14605
				public int EnableProperty;

				// Token: 0x0400390E RID: 14606
				public int Reserved2;

				// Token: 0x0400390F RID: 14607
				public long MatchAnyKeyword;

				// Token: 0x04003910 RID: 14608
				public long MatchAllKeyword;
			}
		}
	}
}
