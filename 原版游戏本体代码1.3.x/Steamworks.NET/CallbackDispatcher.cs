using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001B0 RID: 432
	public static class CallbackDispatcher
	{
		// Token: 0x06000ABC RID: 2748 RVA: 0x0000E372 File Offset: 0x0000C572
		public static void ExceptionHandler(Exception e)
		{
			Console.WriteLine(e.Message);
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000ABD RID: 2749 RVA: 0x0000E37F File Offset: 0x0000C57F
		public static bool IsInitialized
		{
			get
			{
				return CallbackDispatcher.m_initCount > 0;
			}
		}

		// Token: 0x06000ABE RID: 2750 RVA: 0x0000E38C File Offset: 0x0000C58C
		internal static void Initialize()
		{
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				if (CallbackDispatcher.m_initCount == 0)
				{
					NativeMethods.SteamAPI_ManualDispatch_Init();
					CallbackDispatcher.m_pCallbackMsg = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CallbackMsg_t)));
				}
				CallbackDispatcher.m_initCount++;
			}
		}

		// Token: 0x06000ABF RID: 2751 RVA: 0x0000E3F8 File Offset: 0x0000C5F8
		internal static void Shutdown()
		{
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				CallbackDispatcher.m_initCount--;
				if (CallbackDispatcher.m_initCount == 0)
				{
					CallbackDispatcher.UnregisterAll();
					Marshal.FreeHGlobal(CallbackDispatcher.m_pCallbackMsg);
					CallbackDispatcher.m_pCallbackMsg = IntPtr.Zero;
				}
			}
		}

		// Token: 0x06000AC0 RID: 2752 RVA: 0x0000E460 File Offset: 0x0000C660
		internal static void Register(Callback cb)
		{
			int callbackIdentity = CallbackIdentities.GetCallbackIdentity(cb.GetCallbackType());
			Dictionary<int, List<Callback>> dictionary = (cb.IsGameServer ? CallbackDispatcher.m_registeredGameServerCallbacks : CallbackDispatcher.m_registeredCallbacks);
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				List<Callback> list;
				if (!dictionary.TryGetValue(callbackIdentity, out list))
				{
					list = new List<Callback>();
					dictionary.Add(callbackIdentity, list);
				}
				list.Add(cb);
			}
		}

		// Token: 0x06000AC1 RID: 2753 RVA: 0x0000E4E0 File Offset: 0x0000C6E0
		internal static void Register(SteamAPICall_t asyncCall, CallResult cr)
		{
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				List<CallResult> list;
				if (!CallbackDispatcher.m_registeredCallResults.TryGetValue((ulong)asyncCall, out list))
				{
					list = new List<CallResult>();
					CallbackDispatcher.m_registeredCallResults.Add((ulong)asyncCall, list);
				}
				list.Add(cr);
			}
		}

		// Token: 0x06000AC2 RID: 2754 RVA: 0x0000E54C File Offset: 0x0000C74C
		internal static void Unregister(Callback cb)
		{
			int callbackIdentity = CallbackIdentities.GetCallbackIdentity(cb.GetCallbackType());
			Dictionary<int, List<Callback>> dictionary = (cb.IsGameServer ? CallbackDispatcher.m_registeredGameServerCallbacks : CallbackDispatcher.m_registeredCallbacks);
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				List<Callback> list;
				if (dictionary.TryGetValue(callbackIdentity, out list))
				{
					list.Remove(cb);
					if (list.Count == 0)
					{
						dictionary.Remove(callbackIdentity);
					}
				}
			}
		}

		// Token: 0x06000AC3 RID: 2755 RVA: 0x0000E5CC File Offset: 0x0000C7CC
		internal static void Unregister(SteamAPICall_t asyncCall, CallResult cr)
		{
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				List<CallResult> list;
				if (CallbackDispatcher.m_registeredCallResults.TryGetValue((ulong)asyncCall, out list))
				{
					list.Remove(cr);
					if (list.Count == 0)
					{
						CallbackDispatcher.m_registeredCallResults.Remove((ulong)asyncCall);
					}
				}
			}
		}

		// Token: 0x06000AC4 RID: 2756 RVA: 0x0000E63C File Offset: 0x0000C83C
		private static void UnregisterAll()
		{
			List<Callback> list = new List<Callback>();
			List<CallResult> list2 = new List<CallResult>();
			object sync = CallbackDispatcher.m_sync;
			lock (sync)
			{
				foreach (KeyValuePair<int, List<Callback>> keyValuePair in CallbackDispatcher.m_registeredCallbacks)
				{
					list.AddRange(keyValuePair.Value);
				}
				CallbackDispatcher.m_registeredCallbacks.Clear();
				foreach (KeyValuePair<int, List<Callback>> keyValuePair2 in CallbackDispatcher.m_registeredGameServerCallbacks)
				{
					list.AddRange(keyValuePair2.Value);
				}
				CallbackDispatcher.m_registeredGameServerCallbacks.Clear();
				foreach (KeyValuePair<ulong, List<CallResult>> keyValuePair3 in CallbackDispatcher.m_registeredCallResults)
				{
					list2.AddRange(keyValuePair3.Value);
				}
				CallbackDispatcher.m_registeredCallResults.Clear();
				foreach (Callback callback in list)
				{
					callback.SetUnregistered();
				}
				foreach (CallResult callResult in list2)
				{
					callResult.SetUnregistered();
				}
			}
		}

		// Token: 0x06000AC5 RID: 2757 RVA: 0x0000E838 File Offset: 0x0000CA38
		internal static void RunFrame(bool isGameServer)
		{
			if (!CallbackDispatcher.IsInitialized)
			{
				throw new InvalidOperationException("Callback dispatcher is not initialized.");
			}
			HSteamPipe hSteamPipe = (HSteamPipe)(isGameServer ? NativeMethods.SteamGameServer_GetHSteamPipe() : NativeMethods.SteamAPI_GetHSteamPipe());
			NativeMethods.SteamAPI_ManualDispatch_RunFrame(hSteamPipe);
			Dictionary<int, List<Callback>> dictionary = (isGameServer ? CallbackDispatcher.m_registeredGameServerCallbacks : CallbackDispatcher.m_registeredCallbacks);
			while (NativeMethods.SteamAPI_ManualDispatch_GetNextCallback(hSteamPipe, CallbackDispatcher.m_pCallbackMsg))
			{
				CallbackMsg_t callbackMsg_t = (CallbackMsg_t)Marshal.PtrToStructure(CallbackDispatcher.m_pCallbackMsg, typeof(CallbackMsg_t));
				try
				{
					List<Callback> collection;
					if (callbackMsg_t.m_iCallback == 703)
					{
						SteamAPICallCompleted_t steamAPICallCompleted_t = (SteamAPICallCompleted_t)Marshal.PtrToStructure(callbackMsg_t.m_pubParam, typeof(SteamAPICallCompleted_t));
						IntPtr intPtr = Marshal.AllocHGlobal((int)steamAPICallCompleted_t.m_cubParam);
						bool bFailed;
						if (NativeMethods.SteamAPI_ManualDispatch_GetAPICallResult(hSteamPipe, steamAPICallCompleted_t.m_hAsyncCall, intPtr, (int)steamAPICallCompleted_t.m_cubParam, steamAPICallCompleted_t.m_iCallback, out bFailed))
						{
							object sync = CallbackDispatcher.m_sync;
							lock (sync)
							{
								List<CallResult> list;
								if (CallbackDispatcher.m_registeredCallResults.TryGetValue((ulong)steamAPICallCompleted_t.m_hAsyncCall, out list))
								{
									CallbackDispatcher.m_registeredCallResults.Remove((ulong)steamAPICallCompleted_t.m_hAsyncCall);
									foreach (CallResult callResult in list)
									{
										callResult.OnRunCallResult(intPtr, bFailed, (ulong)steamAPICallCompleted_t.m_hAsyncCall);
										callResult.SetUnregistered();
									}
								}
							}
						}
						Marshal.FreeHGlobal(intPtr);
					}
					else if (dictionary.TryGetValue(callbackMsg_t.m_iCallback, out collection))
					{
						object sync = CallbackDispatcher.m_sync;
						List<Callback> list2;
						lock (sync)
						{
							list2 = new List<Callback>(collection);
						}
						foreach (Callback callback in list2)
						{
							callback.OnRunCallback(callbackMsg_t.m_pubParam);
						}
					}
				}
				catch (Exception e)
				{
					CallbackDispatcher.ExceptionHandler(e);
				}
				finally
				{
					NativeMethods.SteamAPI_ManualDispatch_FreeLastCallback(hSteamPipe);
				}
			}
		}

		// Token: 0x04000A6C RID: 2668
		private static Dictionary<int, List<Callback>> m_registeredCallbacks = new Dictionary<int, List<Callback>>();

		// Token: 0x04000A6D RID: 2669
		private static Dictionary<int, List<Callback>> m_registeredGameServerCallbacks = new Dictionary<int, List<Callback>>();

		// Token: 0x04000A6E RID: 2670
		private static Dictionary<ulong, List<CallResult>> m_registeredCallResults = new Dictionary<ulong, List<CallResult>>();

		// Token: 0x04000A6F RID: 2671
		private static object m_sync = new object();

		// Token: 0x04000A70 RID: 2672
		private static IntPtr m_pCallbackMsg;

		// Token: 0x04000A71 RID: 2673
		private static int m_initCount;
	}
}
