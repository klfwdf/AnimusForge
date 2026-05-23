using System;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000007 RID: 7
	public static class GalaxyInstance
	{
		// Token: 0x0600001A RID: 26 RVA: 0x00002428 File Offset: 0x00000628
		public static IError GetError()
		{
			IntPtr error = GalaxyInstancePINVOKE.GetError();
			IError result = ((!(error == IntPtr.Zero)) ? new IError(error, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x0000246C File Offset: 0x0000066C
		public static IListenerRegistrar ListenerRegistrar()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.ListenerRegistrar();
			IListenerRegistrar result = ((!(intPtr == IntPtr.Zero)) ? new IListenerRegistrar(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000024B0 File Offset: 0x000006B0
		public static IListenerRegistrar GameServerListenerRegistrar()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerListenerRegistrar();
			IListenerRegistrar result = ((!(intPtr == IntPtr.Zero)) ? new IListenerRegistrar(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000024F2 File Offset: 0x000006F2
		public static void Init(InitParams initpParams)
		{
			GalaxyInstancePINVOKE.Init(InitParams.getCPtr(initpParams));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000250F File Offset: 0x0000070F
		public static void InitGameServer(InitParams initpParams)
		{
			GalaxyInstancePINVOKE.InitGameServer(InitParams.getCPtr(initpParams));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000252C File Offset: 0x0000072C
		public static void Shutdown(bool unloadModule)
		{
			GalaxyInstancePINVOKE.Shutdown(unloadModule);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002544 File Offset: 0x00000744
		public static IUser User()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.User();
			IUser result = ((!(intPtr == IntPtr.Zero)) ? new IUser(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002588 File Offset: 0x00000788
		public static IFriends Friends()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Friends();
			IFriends result = ((!(intPtr == IntPtr.Zero)) ? new IFriends(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000025CC File Offset: 0x000007CC
		public static IChat Chat()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Chat();
			IChat result = ((!(intPtr == IntPtr.Zero)) ? new IChat(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002610 File Offset: 0x00000810
		public static IMatchmaking Matchmaking()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Matchmaking();
			IMatchmaking result = ((!(intPtr == IntPtr.Zero)) ? new IMatchmaking(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002654 File Offset: 0x00000854
		public static INetworking Networking()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Networking();
			INetworking result = ((!(intPtr == IntPtr.Zero)) ? new INetworking(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002698 File Offset: 0x00000898
		public static IStats Stats()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Stats();
			IStats result = ((!(intPtr == IntPtr.Zero)) ? new IStats(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000026DC File Offset: 0x000008DC
		public static IUtils Utils()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Utils();
			IUtils result = ((!(intPtr == IntPtr.Zero)) ? new IUtils(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002720 File Offset: 0x00000920
		public static IApps Apps()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Apps();
			IApps result = ((!(intPtr == IntPtr.Zero)) ? new IApps(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002764 File Offset: 0x00000964
		public static IStorage Storage()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Storage();
			IStorage result = ((!(intPtr == IntPtr.Zero)) ? new IStorage(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000027A8 File Offset: 0x000009A8
		public static ICustomNetworking CustomNetworking()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.CustomNetworking();
			ICustomNetworking result = ((!(intPtr == IntPtr.Zero)) ? new ICustomNetworking(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000027EC File Offset: 0x000009EC
		public static ILogger Logger()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Logger();
			ILogger result = ((!(intPtr == IntPtr.Zero)) ? new ILogger(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002830 File Offset: 0x00000A30
		public static ITelemetry Telemetry()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.Telemetry();
			ITelemetry result = ((!(intPtr == IntPtr.Zero)) ? new ITelemetry(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002872 File Offset: 0x00000A72
		public static void ProcessData()
		{
			GalaxyInstancePINVOKE.ProcessData();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002889 File Offset: 0x00000A89
		public static void ShutdownGameServer()
		{
			GalaxyInstancePINVOKE.ShutdownGameServer();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000028A0 File Offset: 0x00000AA0
		public static IUser GameServerUser()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerUser();
			IUser result = ((!(intPtr == IntPtr.Zero)) ? new IUser(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000028E4 File Offset: 0x00000AE4
		public static IMatchmaking GameServerMatchmaking()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerMatchmaking();
			IMatchmaking result = ((!(intPtr == IntPtr.Zero)) ? new IMatchmaking(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002928 File Offset: 0x00000B28
		public static INetworking GameServerNetworking()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerNetworking();
			INetworking result = ((!(intPtr == IntPtr.Zero)) ? new INetworking(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x0000296C File Offset: 0x00000B6C
		public static IUtils GameServerUtils()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerUtils();
			IUtils result = ((!(intPtr == IntPtr.Zero)) ? new IUtils(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000029B0 File Offset: 0x00000BB0
		public static ITelemetry GameServerTelemetry()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerTelemetry();
			ITelemetry result = ((!(intPtr == IntPtr.Zero)) ? new ITelemetry(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000029F4 File Offset: 0x00000BF4
		public static ILogger GameServerLogger()
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.GameServerLogger();
			ILogger result = ((!(intPtr == IntPtr.Zero)) ? new ILogger(intPtr, false) : null);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002A36 File Offset: 0x00000C36
		public static void ProcessGameServerData()
		{
			GalaxyInstancePINVOKE.ProcessGameServerData();
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x04000015 RID: 21
		private static GalaxyInstance.CustomExceptionHelper exceptionHelper = new GalaxyInstance.CustomExceptionHelper();

		// Token: 0x02000008 RID: 8
		public class Error : ApplicationException
		{
			// Token: 0x06000035 RID: 53 RVA: 0x00002A4D File Offset: 0x00000C4D
			public Error(string message)
				: base(message)
			{
			}
		}

		// Token: 0x02000009 RID: 9
		public class UnauthorizedAccessError : GalaxyInstance.Error
		{
			// Token: 0x06000036 RID: 54 RVA: 0x00002A56 File Offset: 0x00000C56
			public UnauthorizedAccessError(string message)
				: base(message)
			{
			}
		}

		// Token: 0x0200000A RID: 10
		public class InvalidArgumentError : GalaxyInstance.Error
		{
			// Token: 0x06000037 RID: 55 RVA: 0x00002A5F File Offset: 0x00000C5F
			public InvalidArgumentError(string message)
				: base(message)
			{
			}
		}

		// Token: 0x0200000B RID: 11
		public class InvalidStateError : GalaxyInstance.Error
		{
			// Token: 0x06000038 RID: 56 RVA: 0x00002A68 File Offset: 0x00000C68
			public InvalidStateError(string message)
				: base(message)
			{
			}
		}

		// Token: 0x0200000C RID: 12
		public class RuntimeError : GalaxyInstance.Error
		{
			// Token: 0x06000039 RID: 57 RVA: 0x00002A71 File Offset: 0x00000C71
			public RuntimeError(string message)
				: base(message)
			{
			}
		}

		// Token: 0x0200000D RID: 13
		private class CustomExceptionHelper
		{
			// Token: 0x0600003A RID: 58 RVA: 0x00002A7A File Offset: 0x00000C7A
			static CustomExceptionHelper()
			{
				GalaxyInstance.CustomExceptionHelper.CustomExceptionRegisterCallback(GalaxyInstance.CustomExceptionHelper.customDelegate);
			}

			// Token: 0x0600003C RID: 60
			[DllImport("GalaxyCSharpGlue")]
			public static extern void CustomExceptionRegisterCallback(GalaxyInstance.CustomExceptionHelper.CustomExceptionDelegate customCallback);

			// Token: 0x0600003D RID: 61 RVA: 0x00002AA0 File Offset: 0x00000CA0
			[MonoPInvokeCallback(typeof(GalaxyInstance.CustomExceptionHelper.CustomExceptionDelegate))]
			private static void SetPendingCustomException(IError.Type type, string message)
			{
				switch (type)
				{
				case IError.Type.UNAUTHORIZED_ACCESS:
					GalaxyInstancePINVOKE.SWIGPendingException.Set(new GalaxyInstance.UnauthorizedAccessError(message));
					break;
				case IError.Type.INVALID_ARGUMENT:
					GalaxyInstancePINVOKE.SWIGPendingException.Set(new GalaxyInstance.InvalidArgumentError(message));
					break;
				case IError.Type.INVALID_STATE:
					GalaxyInstancePINVOKE.SWIGPendingException.Set(new GalaxyInstance.InvalidStateError(message));
					break;
				case IError.Type.RUNTIME_ERROR:
					GalaxyInstancePINVOKE.SWIGPendingException.Set(new GalaxyInstance.RuntimeError(message));
					break;
				default:
					GalaxyInstancePINVOKE.SWIGPendingException.Set(new ApplicationException(message));
					break;
				}
			}

			// Token: 0x04000016 RID: 22
			private static GalaxyInstance.CustomExceptionHelper.CustomExceptionDelegate customDelegate = new GalaxyInstance.CustomExceptionHelper.CustomExceptionDelegate(GalaxyInstance.CustomExceptionHelper.SetPendingCustomException);

			// Token: 0x0200000E RID: 14
			// (Invoke) Token: 0x0600003F RID: 63
			public delegate void CustomExceptionDelegate(IError.Type type, string message);
		}
	}
}
