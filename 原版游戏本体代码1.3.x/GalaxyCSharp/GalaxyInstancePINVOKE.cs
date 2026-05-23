using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x0200000F RID: 15
	internal class GalaxyInstancePINVOKE
	{
		// Token: 0x06000044 RID: 68
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IError___")]
		public static extern void delete_IError(HandleRef jarg1);

		// Token: 0x06000045 RID: 69
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IError_GetName___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IError_GetName(HandleRef jarg1);

		// Token: 0x06000046 RID: 70
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IError_GetMsg___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IError_GetMsg(HandleRef jarg1);

		// Token: 0x06000047 RID: 71
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IError_GetErrorType___")]
		public static extern int IError_GetErrorType(HandleRef jarg1);

		// Token: 0x06000048 RID: 72
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUnauthorizedAccessError___")]
		public static extern void delete_IUnauthorizedAccessError(HandleRef jarg1);

		// Token: 0x06000049 RID: 73
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IInvalidArgumentError___")]
		public static extern void delete_IInvalidArgumentError(HandleRef jarg1);

		// Token: 0x0600004A RID: 74
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IInvalidStateError___")]
		public static extern void delete_IInvalidStateError(HandleRef jarg1);

		// Token: 0x0600004B RID: 75
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IRuntimeError___")]
		public static extern void delete_IRuntimeError(HandleRef jarg1);

		// Token: 0x0600004C RID: 76
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GetError___")]
		public static extern IntPtr GetError();

		// Token: 0x0600004D RID: 77
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IGalaxyListener___")]
		public static extern void delete_IGalaxyListener(HandleRef jarg1);

		// Token: 0x0600004E RID: 78
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IGalaxyListener___")]
		public static extern IntPtr new_IGalaxyListener();

		// Token: 0x0600004F RID: 79
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IListenerRegistrar___")]
		public static extern void delete_IListenerRegistrar(HandleRef jarg1);

		// Token: 0x06000050 RID: 80
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IListenerRegistrar_Register___")]
		public static extern void IListenerRegistrar_Register(HandleRef jarg1, int jarg2, HandleRef jarg3);

		// Token: 0x06000051 RID: 81
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IListenerRegistrar_Unregister___")]
		public static extern void IListenerRegistrar_Unregister(HandleRef jarg1, int jarg2, HandleRef jarg3);

		// Token: 0x06000052 RID: 82
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ListenerRegistrar___")]
		public static extern IntPtr ListenerRegistrar();

		// Token: 0x06000053 RID: 83
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerListenerRegistrar___")]
		public static extern IntPtr GameServerListenerRegistrar();

		// Token: 0x06000054 RID: 84
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOverlayVisibilityChange_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerOverlayVisibilityChange_GetListenerType();

		// Token: 0x06000055 RID: 85
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerOverlayVisibilityChange___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerOverlayVisibilityChange();

		// Token: 0x06000056 RID: 86
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerOverlayVisibilityChange___")]
		public static extern void delete_GalaxyTypeAwareListenerOverlayVisibilityChange(HandleRef jarg1);

		// Token: 0x06000057 RID: 87
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOverlayInitializationStateChange_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerOverlayInitializationStateChange_GetListenerType();

		// Token: 0x06000058 RID: 88
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerOverlayInitializationStateChange___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerOverlayInitializationStateChange();

		// Token: 0x06000059 RID: 89
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerOverlayInitializationStateChange___")]
		public static extern void delete_GalaxyTypeAwareListenerOverlayInitializationStateChange(HandleRef jarg1);

		// Token: 0x0600005A RID: 90
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerGogServicesConnectionState_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerGogServicesConnectionState_GetListenerType();

		// Token: 0x0600005B RID: 91
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerGogServicesConnectionState___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerGogServicesConnectionState();

		// Token: 0x0600005C RID: 92
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerGogServicesConnectionState___")]
		public static extern void delete_GalaxyTypeAwareListenerGogServicesConnectionState(HandleRef jarg1);

		// Token: 0x0600005D RID: 93
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOperationalStateChange_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerOperationalStateChange_GetListenerType();

		// Token: 0x0600005E RID: 94
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerOperationalStateChange___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerOperationalStateChange();

		// Token: 0x0600005F RID: 95
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerOperationalStateChange___")]
		public static extern void delete_GalaxyTypeAwareListenerOperationalStateChange(HandleRef jarg1);

		// Token: 0x06000060 RID: 96
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerAuth_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerAuth_GetListenerType();

		// Token: 0x06000061 RID: 97
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerAuth___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerAuth();

		// Token: 0x06000062 RID: 98
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerAuth___")]
		public static extern void delete_GalaxyTypeAwareListenerAuth(HandleRef jarg1);

		// Token: 0x06000063 RID: 99
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOtherSessionStart_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerOtherSessionStart_GetListenerType();

		// Token: 0x06000064 RID: 100
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerOtherSessionStart___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerOtherSessionStart();

		// Token: 0x06000065 RID: 101
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerOtherSessionStart___")]
		public static extern void delete_GalaxyTypeAwareListenerOtherSessionStart(HandleRef jarg1);

		// Token: 0x06000066 RID: 102
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserData_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerUserData_GetListenerType();

		// Token: 0x06000067 RID: 103
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerUserData___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerUserData();

		// Token: 0x06000068 RID: 104
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerUserData___")]
		public static extern void delete_GalaxyTypeAwareListenerUserData(HandleRef jarg1);

		// Token: 0x06000069 RID: 105
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSpecificUserData_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerSpecificUserData_GetListenerType();

		// Token: 0x0600006A RID: 106
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerSpecificUserData___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerSpecificUserData();

		// Token: 0x0600006B RID: 107
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerSpecificUserData___")]
		public static extern void delete_GalaxyTypeAwareListenerSpecificUserData(HandleRef jarg1);

		// Token: 0x0600006C RID: 108
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerEncryptedAppTicket_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerEncryptedAppTicket_GetListenerType();

		// Token: 0x0600006D RID: 109
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerEncryptedAppTicket___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerEncryptedAppTicket();

		// Token: 0x0600006E RID: 110
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerEncryptedAppTicket___")]
		public static extern void delete_GalaxyTypeAwareListenerEncryptedAppTicket(HandleRef jarg1);

		// Token: 0x0600006F RID: 111
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerAccessToken_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerAccessToken_GetListenerType();

		// Token: 0x06000070 RID: 112
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerAccessToken___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerAccessToken();

		// Token: 0x06000071 RID: 113
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerAccessToken___")]
		public static extern void delete_GalaxyTypeAwareListenerAccessToken(HandleRef jarg1);

		// Token: 0x06000072 RID: 114
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyList_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyList_GetListenerType();

		// Token: 0x06000073 RID: 115
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyList___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyList();

		// Token: 0x06000074 RID: 116
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyList___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyList(HandleRef jarg1);

		// Token: 0x06000075 RID: 117
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyCreated_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyCreated_GetListenerType();

		// Token: 0x06000076 RID: 118
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyCreated___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyCreated();

		// Token: 0x06000077 RID: 119
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyCreated___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyCreated(HandleRef jarg1);

		// Token: 0x06000078 RID: 120
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyEntered_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyEntered_GetListenerType();

		// Token: 0x06000079 RID: 121
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyEntered___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyEntered();

		// Token: 0x0600007A RID: 122
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyEntered___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyEntered(HandleRef jarg1);

		// Token: 0x0600007B RID: 123
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyLeft_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyLeft_GetListenerType();

		// Token: 0x0600007C RID: 124
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyLeft___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyLeft();

		// Token: 0x0600007D RID: 125
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyLeft___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyLeft(HandleRef jarg1);

		// Token: 0x0600007E RID: 126
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyData_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyData_GetListenerType();

		// Token: 0x0600007F RID: 127
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyData___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyData();

		// Token: 0x06000080 RID: 128
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyData___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyData(HandleRef jarg1);

		// Token: 0x06000081 RID: 129
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyDataUpdate_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyDataUpdate_GetListenerType();

		// Token: 0x06000082 RID: 130
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyDataUpdate___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyDataUpdate();

		// Token: 0x06000083 RID: 131
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyDataUpdate___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyDataUpdate(HandleRef jarg1);

		// Token: 0x06000084 RID: 132
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyMemberDataUpdate_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyMemberDataUpdate_GetListenerType();

		// Token: 0x06000085 RID: 133
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyMemberDataUpdate___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyMemberDataUpdate();

		// Token: 0x06000086 RID: 134
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyMemberDataUpdate___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyMemberDataUpdate(HandleRef jarg1);

		// Token: 0x06000087 RID: 135
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyDataRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyDataRetrieve_GetListenerType();

		// Token: 0x06000088 RID: 136
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyDataRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyDataRetrieve();

		// Token: 0x06000089 RID: 137
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyDataRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyDataRetrieve(HandleRef jarg1);

		// Token: 0x0600008A RID: 138
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyMemberState_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyMemberState_GetListenerType();

		// Token: 0x0600008B RID: 139
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyMemberState___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyMemberState();

		// Token: 0x0600008C RID: 140
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyMemberState___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyMemberState(HandleRef jarg1);

		// Token: 0x0600008D RID: 141
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyOwnerChange_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyOwnerChange_GetListenerType();

		// Token: 0x0600008E RID: 142
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyOwnerChange___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyOwnerChange();

		// Token: 0x0600008F RID: 143
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyOwnerChange___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyOwnerChange(HandleRef jarg1);

		// Token: 0x06000090 RID: 144
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyMessage_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLobbyMessage_GetListenerType();

		// Token: 0x06000091 RID: 145
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLobbyMessage___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLobbyMessage();

		// Token: 0x06000092 RID: 146
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLobbyMessage___")]
		public static extern void delete_GalaxyTypeAwareListenerLobbyMessage(HandleRef jarg1);

		// Token: 0x06000093 RID: 147
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve_GetListenerType();

		// Token: 0x06000094 RID: 148
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve();

		// Token: 0x06000095 RID: 149
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve(HandleRef jarg1);

		// Token: 0x06000096 RID: 150
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerStatsAndAchievementsStore_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerStatsAndAchievementsStore_GetListenerType();

		// Token: 0x06000097 RID: 151
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerStatsAndAchievementsStore___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerStatsAndAchievementsStore();

		// Token: 0x06000098 RID: 152
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerStatsAndAchievementsStore___")]
		public static extern void delete_GalaxyTypeAwareListenerStatsAndAchievementsStore(HandleRef jarg1);

		// Token: 0x06000099 RID: 153
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerAchievementChange_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerAchievementChange_GetListenerType();

		// Token: 0x0600009A RID: 154
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerAchievementChange___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerAchievementChange();

		// Token: 0x0600009B RID: 155
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerAchievementChange___")]
		public static extern void delete_GalaxyTypeAwareListenerAchievementChange(HandleRef jarg1);

		// Token: 0x0600009C RID: 156
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardsRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLeaderboardsRetrieve_GetListenerType();

		// Token: 0x0600009D RID: 157
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLeaderboardsRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLeaderboardsRetrieve();

		// Token: 0x0600009E RID: 158
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLeaderboardsRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerLeaderboardsRetrieve(HandleRef jarg1);

		// Token: 0x0600009F RID: 159
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLeaderboardEntriesRetrieve_GetListenerType();

		// Token: 0x060000A0 RID: 160
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve();

		// Token: 0x060000A1 RID: 161
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve(HandleRef jarg1);

		// Token: 0x060000A2 RID: 162
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardScoreUpdate_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLeaderboardScoreUpdate_GetListenerType();

		// Token: 0x060000A3 RID: 163
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLeaderboardScoreUpdate___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLeaderboardScoreUpdate();

		// Token: 0x060000A4 RID: 164
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLeaderboardScoreUpdate___")]
		public static extern void delete_GalaxyTypeAwareListenerLeaderboardScoreUpdate(HandleRef jarg1);

		// Token: 0x060000A5 RID: 165
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerLeaderboardRetrieve_GetListenerType();

		// Token: 0x060000A6 RID: 166
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerLeaderboardRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerLeaderboardRetrieve();

		// Token: 0x060000A7 RID: 167
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerLeaderboardRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerLeaderboardRetrieve(HandleRef jarg1);

		// Token: 0x060000A8 RID: 168
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserTimePlayedRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerUserTimePlayedRetrieve_GetListenerType();

		// Token: 0x060000A9 RID: 169
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerUserTimePlayedRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerUserTimePlayedRetrieve();

		// Token: 0x060000AA RID: 170
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerUserTimePlayedRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerUserTimePlayedRetrieve(HandleRef jarg1);

		// Token: 0x060000AB RID: 171
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFileShare_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFileShare_GetListenerType();

		// Token: 0x060000AC RID: 172
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFileShare___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFileShare();

		// Token: 0x060000AD RID: 173
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFileShare___")]
		public static extern void delete_GalaxyTypeAwareListenerFileShare(HandleRef jarg1);

		// Token: 0x060000AE RID: 174
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSharedFileDownload_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerSharedFileDownload_GetListenerType();

		// Token: 0x060000AF RID: 175
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerSharedFileDownload___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerSharedFileDownload();

		// Token: 0x060000B0 RID: 176
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerSharedFileDownload___")]
		public static extern void delete_GalaxyTypeAwareListenerSharedFileDownload(HandleRef jarg1);

		// Token: 0x060000B1 RID: 177
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerConnectionOpen_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerConnectionOpen_GetListenerType();

		// Token: 0x060000B2 RID: 178
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerConnectionOpen___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerConnectionOpen();

		// Token: 0x060000B3 RID: 179
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerConnectionOpen___")]
		public static extern void delete_GalaxyTypeAwareListenerConnectionOpen(HandleRef jarg1);

		// Token: 0x060000B4 RID: 180
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerConnectionClose_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerConnectionClose_GetListenerType();

		// Token: 0x060000B5 RID: 181
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerConnectionClose___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerConnectionClose();

		// Token: 0x060000B6 RID: 182
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerConnectionClose___")]
		public static extern void delete_GalaxyTypeAwareListenerConnectionClose(HandleRef jarg1);

		// Token: 0x060000B7 RID: 183
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerConnectionData_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerConnectionData_GetListenerType();

		// Token: 0x060000B8 RID: 184
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerConnectionData___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerConnectionData();

		// Token: 0x060000B9 RID: 185
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerConnectionData___")]
		public static extern void delete_GalaxyTypeAwareListenerConnectionData(HandleRef jarg1);

		// Token: 0x060000BA RID: 186
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerNetworking_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerNetworking_GetListenerType();

		// Token: 0x060000BB RID: 187
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerNetworking___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerNetworking();

		// Token: 0x060000BC RID: 188
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerNetworking___")]
		public static extern void delete_GalaxyTypeAwareListenerNetworking(HandleRef jarg1);

		// Token: 0x060000BD RID: 189
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerNatTypeDetection_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerNatTypeDetection_GetListenerType();

		// Token: 0x060000BE RID: 190
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerNatTypeDetection___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerNatTypeDetection();

		// Token: 0x060000BF RID: 191
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerNatTypeDetection___")]
		public static extern void delete_GalaxyTypeAwareListenerNatTypeDetection(HandleRef jarg1);

		// Token: 0x060000C0 RID: 192
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerPersonaDataChanged_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerPersonaDataChanged_GetListenerType();

		// Token: 0x060000C1 RID: 193
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerPersonaDataChanged___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerPersonaDataChanged();

		// Token: 0x060000C2 RID: 194
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerPersonaDataChanged___")]
		public static extern void delete_GalaxyTypeAwareListenerPersonaDataChanged(HandleRef jarg1);

		// Token: 0x060000C3 RID: 195
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserInformationRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerUserInformationRetrieve_GetListenerType();

		// Token: 0x060000C4 RID: 196
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerUserInformationRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerUserInformationRetrieve();

		// Token: 0x060000C5 RID: 197
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerUserInformationRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerUserInformationRetrieve(HandleRef jarg1);

		// Token: 0x060000C6 RID: 198
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendList_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendList_GetListenerType();

		// Token: 0x060000C7 RID: 199
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendList___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendList();

		// Token: 0x060000C8 RID: 200
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendList___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendList(HandleRef jarg1);

		// Token: 0x060000C9 RID: 201
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitationSend_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendInvitationSend_GetListenerType();

		// Token: 0x060000CA RID: 202
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendInvitationSend___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendInvitationSend();

		// Token: 0x060000CB RID: 203
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendInvitationSend___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendInvitationSend(HandleRef jarg1);

		// Token: 0x060000CC RID: 204
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitationListRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendInvitationListRetrieve_GetListenerType();

		// Token: 0x060000CD RID: 205
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendInvitationListRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendInvitationListRetrieve();

		// Token: 0x060000CE RID: 206
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendInvitationListRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendInvitationListRetrieve(HandleRef jarg1);

		// Token: 0x060000CF RID: 207
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerSentFriendInvitationListRetrieve_GetListenerType();

		// Token: 0x060000D0 RID: 208
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve();

		// Token: 0x060000D1 RID: 209
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve(HandleRef jarg1);

		// Token: 0x060000D2 RID: 210
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitation_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendInvitation_GetListenerType();

		// Token: 0x060000D3 RID: 211
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendInvitation___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendInvitation();

		// Token: 0x060000D4 RID: 212
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendInvitation___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendInvitation(HandleRef jarg1);

		// Token: 0x060000D5 RID: 213
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitationRespondTo_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendInvitationRespondTo_GetListenerType();

		// Token: 0x060000D6 RID: 214
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendInvitationRespondTo___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendInvitationRespondTo();

		// Token: 0x060000D7 RID: 215
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendInvitationRespondTo___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendInvitationRespondTo(HandleRef jarg1);

		// Token: 0x060000D8 RID: 216
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendAdd_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendAdd_GetListenerType();

		// Token: 0x060000D9 RID: 217
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendAdd___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendAdd();

		// Token: 0x060000DA RID: 218
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendAdd___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendAdd(HandleRef jarg1);

		// Token: 0x060000DB RID: 219
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendDelete_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerFriendDelete_GetListenerType();

		// Token: 0x060000DC RID: 220
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerFriendDelete___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerFriendDelete();

		// Token: 0x060000DD RID: 221
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerFriendDelete___")]
		public static extern void delete_GalaxyTypeAwareListenerFriendDelete(HandleRef jarg1);

		// Token: 0x060000DE RID: 222
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerRichPresenceChange_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerRichPresenceChange_GetListenerType();

		// Token: 0x060000DF RID: 223
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerRichPresenceChange___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerRichPresenceChange();

		// Token: 0x060000E0 RID: 224
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerRichPresenceChange___")]
		public static extern void delete_GalaxyTypeAwareListenerRichPresenceChange(HandleRef jarg1);

		// Token: 0x060000E1 RID: 225
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerRichPresence_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerRichPresence_GetListenerType();

		// Token: 0x060000E2 RID: 226
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerRichPresence___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerRichPresence();

		// Token: 0x060000E3 RID: 227
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerRichPresence___")]
		public static extern void delete_GalaxyTypeAwareListenerRichPresence(HandleRef jarg1);

		// Token: 0x060000E4 RID: 228
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerRichPresenceRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerRichPresenceRetrieve_GetListenerType();

		// Token: 0x060000E5 RID: 229
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerRichPresenceRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerRichPresenceRetrieve();

		// Token: 0x060000E6 RID: 230
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerRichPresenceRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerRichPresenceRetrieve(HandleRef jarg1);

		// Token: 0x060000E7 RID: 231
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerGameJoinRequested_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerGameJoinRequested_GetListenerType();

		// Token: 0x060000E8 RID: 232
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerGameJoinRequested___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerGameJoinRequested();

		// Token: 0x060000E9 RID: 233
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerGameJoinRequested___")]
		public static extern void delete_GalaxyTypeAwareListenerGameJoinRequested(HandleRef jarg1);

		// Token: 0x060000EA RID: 234
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerGameInvitationReceived_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerGameInvitationReceived_GetListenerType();

		// Token: 0x060000EB RID: 235
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerGameInvitationReceived___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerGameInvitationReceived();

		// Token: 0x060000EC RID: 236
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerGameInvitationReceived___")]
		public static extern void delete_GalaxyTypeAwareListenerGameInvitationReceived(HandleRef jarg1);

		// Token: 0x060000ED RID: 237
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSendInvitation_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerSendInvitation_GetListenerType();

		// Token: 0x060000EE RID: 238
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerSendInvitation___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerSendInvitation();

		// Token: 0x060000EF RID: 239
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerSendInvitation___")]
		public static extern void delete_GalaxyTypeAwareListenerSendInvitation(HandleRef jarg1);

		// Token: 0x060000F0 RID: 240
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerNotification_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerNotification_GetListenerType();

		// Token: 0x060000F1 RID: 241
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerNotification___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerNotification();

		// Token: 0x060000F2 RID: 242
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerNotification___")]
		public static extern void delete_GalaxyTypeAwareListenerNotification(HandleRef jarg1);

		// Token: 0x060000F3 RID: 243
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserFind_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerUserFind_GetListenerType();

		// Token: 0x060000F4 RID: 244
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerUserFind___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerUserFind();

		// Token: 0x060000F5 RID: 245
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerUserFind___")]
		public static extern void delete_GalaxyTypeAwareListenerUserFind(HandleRef jarg1);

		// Token: 0x060000F6 RID: 246
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomWithUserRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerChatRoomWithUserRetrieve_GetListenerType();

		// Token: 0x060000F7 RID: 247
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerChatRoomWithUserRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerChatRoomWithUserRetrieve();

		// Token: 0x060000F8 RID: 248
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerChatRoomWithUserRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerChatRoomWithUserRetrieve(HandleRef jarg1);

		// Token: 0x060000F9 RID: 249
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomMessagesRetrieve_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerChatRoomMessagesRetrieve_GetListenerType();

		// Token: 0x060000FA RID: 250
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerChatRoomMessagesRetrieve___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerChatRoomMessagesRetrieve();

		// Token: 0x060000FB RID: 251
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerChatRoomMessagesRetrieve___")]
		public static extern void delete_GalaxyTypeAwareListenerChatRoomMessagesRetrieve(HandleRef jarg1);

		// Token: 0x060000FC RID: 252
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomMessageSend_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerChatRoomMessageSend_GetListenerType();

		// Token: 0x060000FD RID: 253
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerChatRoomMessageSend___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerChatRoomMessageSend();

		// Token: 0x060000FE RID: 254
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerChatRoomMessageSend___")]
		public static extern void delete_GalaxyTypeAwareListenerChatRoomMessageSend(HandleRef jarg1);

		// Token: 0x060000FF RID: 255
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomMessages_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerChatRoomMessages_GetListenerType();

		// Token: 0x06000100 RID: 256
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerChatRoomMessages___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerChatRoomMessages();

		// Token: 0x06000101 RID: 257
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerChatRoomMessages___")]
		public static extern void delete_GalaxyTypeAwareListenerChatRoomMessages(HandleRef jarg1);

		// Token: 0x06000102 RID: 258
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerTelemetryEventSend_GetListenerType___")]
		public static extern int GalaxyTypeAwareListenerTelemetryEventSend_GetListenerType();

		// Token: 0x06000103 RID: 259
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyTypeAwareListenerTelemetryEventSend___")]
		public static extern IntPtr new_GalaxyTypeAwareListenerTelemetryEventSend();

		// Token: 0x06000104 RID: 260
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyTypeAwareListenerTelemetryEventSend___")]
		public static extern void delete_GalaxyTypeAwareListenerTelemetryEventSend(HandleRef jarg1);

		// Token: 0x06000105 RID: 261
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IApps___")]
		public static extern void delete_IApps(HandleRef jarg1);

		// Token: 0x06000106 RID: 262
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IApps_IsDlcInstalled___")]
		public static extern bool IApps_IsDlcInstalled(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000107 RID: 263
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IApps_GetCurrentGameLanguage__SWIG_0___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IApps_GetCurrentGameLanguage__SWIG_0(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000108 RID: 264
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IApps_GetCurrentGameLanguage__SWIG_1___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IApps_GetCurrentGameLanguage__SWIG_1(HandleRef jarg1);

		// Token: 0x06000109 RID: 265
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IApps_GetCurrentGameLanguageCopy__SWIG_0___")]
		public static extern void IApps_GetCurrentGameLanguageCopy__SWIG_0(HandleRef jarg1, byte[] jarg2, uint jarg3, ulong jarg4);

		// Token: 0x0600010A RID: 266
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IApps_GetCurrentGameLanguageCopy__SWIG_1___")]
		public static extern void IApps_GetCurrentGameLanguageCopy__SWIG_1(HandleRef jarg1, byte[] jarg2, uint jarg3);

		// Token: 0x0600010B RID: 267
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOverlayVisibilityChangeListener_OnOverlayVisibilityChanged___")]
		public static extern void IOverlayVisibilityChangeListener_OnOverlayVisibilityChanged(HandleRef jarg1, bool jarg2);

		// Token: 0x0600010C RID: 268
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IOverlayVisibilityChangeListener___")]
		public static extern IntPtr new_IOverlayVisibilityChangeListener();

		// Token: 0x0600010D RID: 269
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IOverlayVisibilityChangeListener___")]
		public static extern void delete_IOverlayVisibilityChangeListener(HandleRef jarg1);

		// Token: 0x0600010E RID: 270
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOverlayVisibilityChangeListener_director_connect___")]
		public static extern void IOverlayVisibilityChangeListener_director_connect(HandleRef jarg1, IOverlayVisibilityChangeListener.SwigDelegateIOverlayVisibilityChangeListener_0 delegate0);

		// Token: 0x0600010F RID: 271
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOverlayInitializationStateChangeListener_OnOverlayStateChanged___")]
		public static extern void IOverlayInitializationStateChangeListener_OnOverlayStateChanged(HandleRef jarg1, int jarg2);

		// Token: 0x06000110 RID: 272
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IOverlayInitializationStateChangeListener___")]
		public static extern IntPtr new_IOverlayInitializationStateChangeListener();

		// Token: 0x06000111 RID: 273
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IOverlayInitializationStateChangeListener___")]
		public static extern void delete_IOverlayInitializationStateChangeListener(HandleRef jarg1);

		// Token: 0x06000112 RID: 274
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOverlayInitializationStateChangeListener_director_connect___")]
		public static extern void IOverlayInitializationStateChangeListener_director_connect(HandleRef jarg1, IOverlayInitializationStateChangeListener.SwigDelegateIOverlayInitializationStateChangeListener_0 delegate0);

		// Token: 0x06000113 RID: 275
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INotificationListener_OnNotificationReceived___")]
		public static extern void INotificationListener_OnNotificationReceived(HandleRef jarg1, ulong jarg2, uint jarg3, uint jarg4);

		// Token: 0x06000114 RID: 276
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_INotificationListener___")]
		public static extern IntPtr new_INotificationListener();

		// Token: 0x06000115 RID: 277
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_INotificationListener___")]
		public static extern void delete_INotificationListener(HandleRef jarg1);

		// Token: 0x06000116 RID: 278
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INotificationListener_director_connect___")]
		public static extern void INotificationListener_director_connect(HandleRef jarg1, INotificationListener.SwigDelegateINotificationListener_0 delegate0);

		// Token: 0x06000117 RID: 279
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGogServicesConnectionStateListener_OnConnectionStateChange___")]
		public static extern void IGogServicesConnectionStateListener_OnConnectionStateChange(HandleRef jarg1, int jarg2);

		// Token: 0x06000118 RID: 280
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IGogServicesConnectionStateListener___")]
		public static extern IntPtr new_IGogServicesConnectionStateListener();

		// Token: 0x06000119 RID: 281
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IGogServicesConnectionStateListener___")]
		public static extern void delete_IGogServicesConnectionStateListener(HandleRef jarg1);

		// Token: 0x0600011A RID: 282
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGogServicesConnectionStateListener_director_connect___")]
		public static extern void IGogServicesConnectionStateListener_director_connect(HandleRef jarg1, IGogServicesConnectionStateListener.SwigDelegateIGogServicesConnectionStateListener_0 delegate0);

		// Token: 0x0600011B RID: 283
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUtils___")]
		public static extern void delete_IUtils(HandleRef jarg1);

		// Token: 0x0600011C RID: 284
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_GetImageSize___")]
		public static extern void IUtils_GetImageSize(HandleRef jarg1, uint jarg2, ref int jarg3, ref int jarg4);

		// Token: 0x0600011D RID: 285
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_GetImageRGBA___")]
		public static extern void IUtils_GetImageRGBA(HandleRef jarg1, uint jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x0600011E RID: 286
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_RegisterForNotification___")]
		public static extern void IUtils_RegisterForNotification(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600011F RID: 287
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_GetNotification___")]
		public static extern uint IUtils_GetNotification(HandleRef jarg1, ulong jarg2, ref bool jarg3, byte[] jarg4, uint jarg5, byte[] jarg6, uint jarg7);

		// Token: 0x06000120 RID: 288
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_ShowOverlayWithWebPage___")]
		public static extern void IUtils_ShowOverlayWithWebPage(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000121 RID: 289
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_IsOverlayVisible___")]
		public static extern bool IUtils_IsOverlayVisible(HandleRef jarg1);

		// Token: 0x06000122 RID: 290
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_GetOverlayState___")]
		public static extern int IUtils_GetOverlayState(HandleRef jarg1);

		// Token: 0x06000123 RID: 291
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_DisableOverlayPopups___")]
		public static extern void IUtils_DisableOverlayPopups(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000124 RID: 292
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUtils_GetGogServicesConnectionState___")]
		public static extern int IUtils_GetGogServicesConnectionState(HandleRef jarg1);

		// Token: 0x06000125 RID: 293
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAuthListener_OnAuthSuccess___")]
		public static extern void IAuthListener_OnAuthSuccess(HandleRef jarg1);

		// Token: 0x06000126 RID: 294
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAuthListener_OnAuthFailure___")]
		public static extern void IAuthListener_OnAuthFailure(HandleRef jarg1, int jarg2);

		// Token: 0x06000127 RID: 295
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAuthListener_OnAuthLost___")]
		public static extern void IAuthListener_OnAuthLost(HandleRef jarg1);

		// Token: 0x06000128 RID: 296
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IAuthListener___")]
		public static extern IntPtr new_IAuthListener();

		// Token: 0x06000129 RID: 297
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IAuthListener___")]
		public static extern void delete_IAuthListener(HandleRef jarg1);

		// Token: 0x0600012A RID: 298
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAuthListener_director_connect___")]
		public static extern void IAuthListener_director_connect(HandleRef jarg1, IAuthListener.SwigDelegateIAuthListener_0 delegate0, IAuthListener.SwigDelegateIAuthListener_1 delegate1, IAuthListener.SwigDelegateIAuthListener_2 delegate2);

		// Token: 0x0600012B RID: 299
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOtherSessionStartListener_OnOtherSessionStarted___")]
		public static extern void IOtherSessionStartListener_OnOtherSessionStarted(HandleRef jarg1);

		// Token: 0x0600012C RID: 300
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IOtherSessionStartListener___")]
		public static extern IntPtr new_IOtherSessionStartListener();

		// Token: 0x0600012D RID: 301
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IOtherSessionStartListener___")]
		public static extern void delete_IOtherSessionStartListener(HandleRef jarg1);

		// Token: 0x0600012E RID: 302
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOtherSessionStartListener_director_connect___")]
		public static extern void IOtherSessionStartListener_director_connect(HandleRef jarg1, IOtherSessionStartListener.SwigDelegateIOtherSessionStartListener_0 delegate0);

		// Token: 0x0600012F RID: 303
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOperationalStateChangeListener_OnOperationalStateChanged___")]
		public static extern void IOperationalStateChangeListener_OnOperationalStateChanged(HandleRef jarg1, uint jarg2);

		// Token: 0x06000130 RID: 304
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IOperationalStateChangeListener___")]
		public static extern IntPtr new_IOperationalStateChangeListener();

		// Token: 0x06000131 RID: 305
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IOperationalStateChangeListener___")]
		public static extern void delete_IOperationalStateChangeListener(HandleRef jarg1);

		// Token: 0x06000132 RID: 306
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOperationalStateChangeListener_director_connect___")]
		public static extern void IOperationalStateChangeListener_director_connect(HandleRef jarg1, IOperationalStateChangeListener.SwigDelegateIOperationalStateChangeListener_0 delegate0);

		// Token: 0x06000133 RID: 307
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserDataListener_OnUserDataUpdated___")]
		public static extern void IUserDataListener_OnUserDataUpdated(HandleRef jarg1);

		// Token: 0x06000134 RID: 308
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IUserDataListener___")]
		public static extern IntPtr new_IUserDataListener();

		// Token: 0x06000135 RID: 309
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUserDataListener___")]
		public static extern void delete_IUserDataListener(HandleRef jarg1);

		// Token: 0x06000136 RID: 310
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserDataListener_director_connect___")]
		public static extern void IUserDataListener_director_connect(HandleRef jarg1, IUserDataListener.SwigDelegateIUserDataListener_0 delegate0);

		// Token: 0x06000137 RID: 311
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISpecificUserDataListener_OnSpecificUserDataUpdated___")]
		public static extern void ISpecificUserDataListener_OnSpecificUserDataUpdated(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000138 RID: 312
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ISpecificUserDataListener___")]
		public static extern IntPtr new_ISpecificUserDataListener();

		// Token: 0x06000139 RID: 313
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ISpecificUserDataListener___")]
		public static extern void delete_ISpecificUserDataListener(HandleRef jarg1);

		// Token: 0x0600013A RID: 314
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISpecificUserDataListener_director_connect___")]
		public static extern void ISpecificUserDataListener_director_connect(HandleRef jarg1, ISpecificUserDataListener.SwigDelegateISpecificUserDataListener_0 delegate0);

		// Token: 0x0600013B RID: 315
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IEncryptedAppTicketListener_OnEncryptedAppTicketRetrieveSuccess___")]
		public static extern void IEncryptedAppTicketListener_OnEncryptedAppTicketRetrieveSuccess(HandleRef jarg1);

		// Token: 0x0600013C RID: 316
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IEncryptedAppTicketListener_OnEncryptedAppTicketRetrieveFailure___")]
		public static extern void IEncryptedAppTicketListener_OnEncryptedAppTicketRetrieveFailure(HandleRef jarg1, int jarg2);

		// Token: 0x0600013D RID: 317
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IEncryptedAppTicketListener___")]
		public static extern IntPtr new_IEncryptedAppTicketListener();

		// Token: 0x0600013E RID: 318
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IEncryptedAppTicketListener___")]
		public static extern void delete_IEncryptedAppTicketListener(HandleRef jarg1);

		// Token: 0x0600013F RID: 319
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IEncryptedAppTicketListener_director_connect___")]
		public static extern void IEncryptedAppTicketListener_director_connect(HandleRef jarg1, IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_0 delegate0, IEncryptedAppTicketListener.SwigDelegateIEncryptedAppTicketListener_1 delegate1);

		// Token: 0x06000140 RID: 320
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAccessTokenListener_OnAccessTokenChanged___")]
		public static extern void IAccessTokenListener_OnAccessTokenChanged(HandleRef jarg1);

		// Token: 0x06000141 RID: 321
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IAccessTokenListener___")]
		public static extern IntPtr new_IAccessTokenListener();

		// Token: 0x06000142 RID: 322
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IAccessTokenListener___")]
		public static extern void delete_IAccessTokenListener(HandleRef jarg1);

		// Token: 0x06000143 RID: 323
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAccessTokenListener_director_connect___")]
		public static extern void IAccessTokenListener_director_connect(HandleRef jarg1, IAccessTokenListener.SwigDelegateIAccessTokenListener_0 delegate0);

		// Token: 0x06000144 RID: 324
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUser___")]
		public static extern void delete_IUser(HandleRef jarg1);

		// Token: 0x06000145 RID: 325
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignedIn___")]
		public static extern bool IUser_SignedIn(HandleRef jarg1);

		// Token: 0x06000146 RID: 326
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetGalaxyID___")]
		public static extern IntPtr IUser_GetGalaxyID(HandleRef jarg1);

		// Token: 0x06000147 RID: 327
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInCredentials__SWIG_0___")]
		public static extern void IUser_SignInCredentials__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x06000148 RID: 328
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInCredentials__SWIG_1___")]
		public static extern void IUser_SignInCredentials__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000149 RID: 329
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInToken__SWIG_0___")]
		public static extern void IUser_SignInToken__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x0600014A RID: 330
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInToken__SWIG_1___")]
		public static extern void IUser_SignInToken__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600014B RID: 331
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInSteam__SWIG_0___")]
		public static extern void IUser_SignInSteam__SWIG_0(HandleRef jarg1, byte[] jarg2, uint jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, HandleRef jarg5);

		// Token: 0x0600014C RID: 332
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInSteam__SWIG_1___")]
		public static extern void IUser_SignInSteam__SWIG_1(HandleRef jarg1, byte[] jarg2, uint jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4);

		// Token: 0x0600014D RID: 333
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInEpic__SWIG_0___")]
		public static extern void IUser_SignInEpic__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x0600014E RID: 334
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInEpic__SWIG_1___")]
		public static extern void IUser_SignInEpic__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x0600014F RID: 335
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInGalaxy__SWIG_0___")]
		public static extern void IUser_SignInGalaxy__SWIG_0(HandleRef jarg1, bool jarg2, HandleRef jarg3);

		// Token: 0x06000150 RID: 336
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInGalaxy__SWIG_1___")]
		public static extern void IUser_SignInGalaxy__SWIG_1(HandleRef jarg1, bool jarg2);

		// Token: 0x06000151 RID: 337
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInGalaxy__SWIG_2___")]
		public static extern void IUser_SignInGalaxy__SWIG_2(HandleRef jarg1);

		// Token: 0x06000152 RID: 338
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInUWP__SWIG_0___")]
		public static extern void IUser_SignInUWP__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000153 RID: 339
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInUWP__SWIG_1___")]
		public static extern void IUser_SignInUWP__SWIG_1(HandleRef jarg1);

		// Token: 0x06000154 RID: 340
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInPS4__SWIG_0___")]
		public static extern void IUser_SignInPS4__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000155 RID: 341
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInPS4__SWIG_1___")]
		public static extern void IUser_SignInPS4__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000156 RID: 342
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInXB1__SWIG_0___")]
		public static extern void IUser_SignInXB1__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000157 RID: 343
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInXB1__SWIG_1___")]
		public static extern void IUser_SignInXB1__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000158 RID: 344
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInXBLive__SWIG_0___")]
		public static extern void IUser_SignInXBLive__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg5, HandleRef jarg6);

		// Token: 0x06000159 RID: 345
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInXBLive__SWIG_1___")]
		public static extern void IUser_SignInXBLive__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg5);

		// Token: 0x0600015A RID: 346
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInAnonymous__SWIG_0___")]
		public static extern void IUser_SignInAnonymous__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600015B RID: 347
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInAnonymous__SWIG_1___")]
		public static extern void IUser_SignInAnonymous__SWIG_1(HandleRef jarg1);

		// Token: 0x0600015C RID: 348
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInAnonymousTelemetry__SWIG_0___")]
		public static extern void IUser_SignInAnonymousTelemetry__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600015D RID: 349
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInAnonymousTelemetry__SWIG_1___")]
		public static extern void IUser_SignInAnonymousTelemetry__SWIG_1(HandleRef jarg1);

		// Token: 0x0600015E RID: 350
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInServerKey__SWIG_0___")]
		public static extern void IUser_SignInServerKey__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x0600015F RID: 351
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignInServerKey__SWIG_1___")]
		public static extern void IUser_SignInServerKey__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000160 RID: 352
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SignOut___")]
		public static extern void IUser_SignOut(HandleRef jarg1);

		// Token: 0x06000161 RID: 353
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_RequestUserData__SWIG_0___")]
		public static extern void IUser_RequestUserData__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x06000162 RID: 354
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_RequestUserData__SWIG_1___")]
		public static extern void IUser_RequestUserData__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000163 RID: 355
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_RequestUserData__SWIG_2___")]
		public static extern void IUser_RequestUserData__SWIG_2(HandleRef jarg1);

		// Token: 0x06000164 RID: 356
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_IsUserDataAvailable__SWIG_0___")]
		public static extern bool IUser_IsUserDataAvailable__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000165 RID: 357
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_IsUserDataAvailable__SWIG_1___")]
		public static extern bool IUser_IsUserDataAvailable__SWIG_1(HandleRef jarg1);

		// Token: 0x06000166 RID: 358
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserData__SWIG_0___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IUser_GetUserData__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000167 RID: 359
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserData__SWIG_1___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IUser_GetUserData__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000168 RID: 360
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserDataCopy__SWIG_0___")]
		public static extern void IUser_GetUserDataCopy__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4, HandleRef jarg5);

		// Token: 0x06000169 RID: 361
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserDataCopy__SWIG_1___")]
		public static extern void IUser_GetUserDataCopy__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x0600016A RID: 362
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SetUserData__SWIG_0___")]
		public static extern void IUser_SetUserData__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x0600016B RID: 363
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_SetUserData__SWIG_1___")]
		public static extern void IUser_SetUserData__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x0600016C RID: 364
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserDataCount__SWIG_0___")]
		public static extern uint IUser_GetUserDataCount__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600016D RID: 365
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserDataCount__SWIG_1___")]
		public static extern uint IUser_GetUserDataCount__SWIG_1(HandleRef jarg1);

		// Token: 0x0600016E RID: 366
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserDataByIndex__SWIG_0___")]
		public static extern bool IUser_GetUserDataByIndex__SWIG_0(HandleRef jarg1, uint jarg2, byte[] jarg3, uint jarg4, byte[] jarg5, uint jarg6, HandleRef jarg7);

		// Token: 0x0600016F RID: 367
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetUserDataByIndex__SWIG_1___")]
		public static extern bool IUser_GetUserDataByIndex__SWIG_1(HandleRef jarg1, uint jarg2, byte[] jarg3, uint jarg4, byte[] jarg5, uint jarg6);

		// Token: 0x06000170 RID: 368
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_DeleteUserData__SWIG_0___")]
		public static extern void IUser_DeleteUserData__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000171 RID: 369
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_DeleteUserData__SWIG_1___")]
		public static extern void IUser_DeleteUserData__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000172 RID: 370
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_IsLoggedOn___")]
		public static extern bool IUser_IsLoggedOn(HandleRef jarg1);

		// Token: 0x06000173 RID: 371
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_RequestEncryptedAppTicket__SWIG_0___")]
		public static extern void IUser_RequestEncryptedAppTicket__SWIG_0(HandleRef jarg1, byte[] jarg2, uint jarg3, HandleRef jarg4);

		// Token: 0x06000174 RID: 372
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_RequestEncryptedAppTicket__SWIG_1___")]
		public static extern void IUser_RequestEncryptedAppTicket__SWIG_1(HandleRef jarg1, byte[] jarg2, uint jarg3);

		// Token: 0x06000175 RID: 373
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetEncryptedAppTicket___")]
		public static extern void IUser_GetEncryptedAppTicket(HandleRef jarg1, byte[] jarg2, uint jarg3, ref uint jarg4);

		// Token: 0x06000176 RID: 374
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetSessionID___")]
		public static extern ulong IUser_GetSessionID(HandleRef jarg1);

		// Token: 0x06000177 RID: 375
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetAccessToken___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IUser_GetAccessToken(HandleRef jarg1);

		// Token: 0x06000178 RID: 376
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_GetAccessTokenCopy___")]
		public static extern void IUser_GetAccessTokenCopy(HandleRef jarg1, byte[] jarg2, uint jarg3);

		// Token: 0x06000179 RID: 377
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_ReportInvalidAccessToken__SWIG_0___")]
		public static extern bool IUser_ReportInvalidAccessToken__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x0600017A RID: 378
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUser_ReportInvalidAccessToken__SWIG_1___")]
		public static extern bool IUser_ReportInvalidAccessToken__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600017B RID: 379
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILogger___")]
		public static extern void delete_ILogger(HandleRef jarg1);

		// Token: 0x0600017C RID: 380
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILogger_Trace___")]
		public static extern void ILogger_Trace(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600017D RID: 381
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILogger_Debug___")]
		public static extern void ILogger_Debug(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600017E RID: 382
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILogger_Info___")]
		public static extern void ILogger_Info(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600017F RID: 383
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILogger_Warning___")]
		public static extern void ILogger_Warning(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000180 RID: 384
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILogger_Error___")]
		public static extern void ILogger_Error(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000181 RID: 385
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILogger_Fatal___")]
		public static extern void ILogger_Fatal(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000182 RID: 386
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IPersonaDataChangedListener_OnPersonaDataChanged___")]
		public static extern void IPersonaDataChangedListener_OnPersonaDataChanged(HandleRef jarg1, HandleRef jarg2, uint jarg3);

		// Token: 0x06000183 RID: 387
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IPersonaDataChangedListener___")]
		public static extern IntPtr new_IPersonaDataChangedListener();

		// Token: 0x06000184 RID: 388
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IPersonaDataChangedListener___")]
		public static extern void delete_IPersonaDataChangedListener(HandleRef jarg1);

		// Token: 0x06000185 RID: 389
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IPersonaDataChangedListener_director_connect___")]
		public static extern void IPersonaDataChangedListener_director_connect(HandleRef jarg1, IPersonaDataChangedListener.SwigDelegateIPersonaDataChangedListener_0 delegate0);

		// Token: 0x06000186 RID: 390
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserInformationRetrieveListener_OnUserInformationRetrieveSuccess___")]
		public static extern void IUserInformationRetrieveListener_OnUserInformationRetrieveSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000187 RID: 391
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserInformationRetrieveListener_OnUserInformationRetrieveFailure___")]
		public static extern void IUserInformationRetrieveListener_OnUserInformationRetrieveFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x06000188 RID: 392
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IUserInformationRetrieveListener___")]
		public static extern IntPtr new_IUserInformationRetrieveListener();

		// Token: 0x06000189 RID: 393
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUserInformationRetrieveListener___")]
		public static extern void delete_IUserInformationRetrieveListener(HandleRef jarg1);

		// Token: 0x0600018A RID: 394
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserInformationRetrieveListener_director_connect___")]
		public static extern void IUserInformationRetrieveListener_director_connect(HandleRef jarg1, IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_0 delegate0, IUserInformationRetrieveListener.SwigDelegateIUserInformationRetrieveListener_1 delegate1);

		// Token: 0x0600018B RID: 395
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendListListener_OnFriendListRetrieveSuccess___")]
		public static extern void IFriendListListener_OnFriendListRetrieveSuccess(HandleRef jarg1);

		// Token: 0x0600018C RID: 396
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendListListener_OnFriendListRetrieveFailure___")]
		public static extern void IFriendListListener_OnFriendListRetrieveFailure(HandleRef jarg1, int jarg2);

		// Token: 0x0600018D RID: 397
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendListListener___")]
		public static extern IntPtr new_IFriendListListener();

		// Token: 0x0600018E RID: 398
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendListListener___")]
		public static extern void delete_IFriendListListener(HandleRef jarg1);

		// Token: 0x0600018F RID: 399
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendListListener_director_connect___")]
		public static extern void IFriendListListener_director_connect(HandleRef jarg1, IFriendListListener.SwigDelegateIFriendListListener_0 delegate0, IFriendListListener.SwigDelegateIFriendListListener_1 delegate1);

		// Token: 0x06000190 RID: 400
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationSendListener_OnFriendInvitationSendSuccess___")]
		public static extern void IFriendInvitationSendListener_OnFriendInvitationSendSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000191 RID: 401
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationSendListener_OnFriendInvitationSendFailure___")]
		public static extern void IFriendInvitationSendListener_OnFriendInvitationSendFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x06000192 RID: 402
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendInvitationSendListener___")]
		public static extern IntPtr new_IFriendInvitationSendListener();

		// Token: 0x06000193 RID: 403
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendInvitationSendListener___")]
		public static extern void delete_IFriendInvitationSendListener(HandleRef jarg1);

		// Token: 0x06000194 RID: 404
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationSendListener_director_connect___")]
		public static extern void IFriendInvitationSendListener_director_connect(HandleRef jarg1, IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_0 delegate0, IFriendInvitationSendListener.SwigDelegateIFriendInvitationSendListener_1 delegate1);

		// Token: 0x06000195 RID: 405
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListRetrieveListener_OnFriendInvitationListRetrieveSuccess___")]
		public static extern void IFriendInvitationListRetrieveListener_OnFriendInvitationListRetrieveSuccess(HandleRef jarg1);

		// Token: 0x06000196 RID: 406
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListRetrieveListener_OnFriendInvitationListRetrieveFailure___")]
		public static extern void IFriendInvitationListRetrieveListener_OnFriendInvitationListRetrieveFailure(HandleRef jarg1, int jarg2);

		// Token: 0x06000197 RID: 407
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendInvitationListRetrieveListener___")]
		public static extern IntPtr new_IFriendInvitationListRetrieveListener();

		// Token: 0x06000198 RID: 408
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendInvitationListRetrieveListener___")]
		public static extern void delete_IFriendInvitationListRetrieveListener(HandleRef jarg1);

		// Token: 0x06000199 RID: 409
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListRetrieveListener_director_connect___")]
		public static extern void IFriendInvitationListRetrieveListener_director_connect(HandleRef jarg1, IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_0 delegate0, IFriendInvitationListRetrieveListener.SwigDelegateIFriendInvitationListRetrieveListener_1 delegate1);

		// Token: 0x0600019A RID: 410
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISentFriendInvitationListRetrieveListener_OnSentFriendInvitationListRetrieveSuccess___")]
		public static extern void ISentFriendInvitationListRetrieveListener_OnSentFriendInvitationListRetrieveSuccess(HandleRef jarg1);

		// Token: 0x0600019B RID: 411
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISentFriendInvitationListRetrieveListener_OnSentFriendInvitationListRetrieveFailure___")]
		public static extern void ISentFriendInvitationListRetrieveListener_OnSentFriendInvitationListRetrieveFailure(HandleRef jarg1, int jarg2);

		// Token: 0x0600019C RID: 412
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ISentFriendInvitationListRetrieveListener___")]
		public static extern IntPtr new_ISentFriendInvitationListRetrieveListener();

		// Token: 0x0600019D RID: 413
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ISentFriendInvitationListRetrieveListener___")]
		public static extern void delete_ISentFriendInvitationListRetrieveListener(HandleRef jarg1);

		// Token: 0x0600019E RID: 414
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISentFriendInvitationListRetrieveListener_director_connect___")]
		public static extern void ISentFriendInvitationListRetrieveListener_director_connect(HandleRef jarg1, ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_0 delegate0, ISentFriendInvitationListRetrieveListener.SwigDelegateISentFriendInvitationListRetrieveListener_1 delegate1);

		// Token: 0x0600019F RID: 415
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListener_OnFriendInvitationReceived___")]
		public static extern void IFriendInvitationListener_OnFriendInvitationReceived(HandleRef jarg1, HandleRef jarg2, uint jarg3);

		// Token: 0x060001A0 RID: 416
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendInvitationListener___")]
		public static extern IntPtr new_IFriendInvitationListener();

		// Token: 0x060001A1 RID: 417
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendInvitationListener___")]
		public static extern void delete_IFriendInvitationListener(HandleRef jarg1);

		// Token: 0x060001A2 RID: 418
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListener_director_connect___")]
		public static extern void IFriendInvitationListener_director_connect(HandleRef jarg1, IFriendInvitationListener.SwigDelegateIFriendInvitationListener_0 delegate0);

		// Token: 0x060001A3 RID: 419
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationRespondToListener_OnFriendInvitationRespondToSuccess___")]
		public static extern void IFriendInvitationRespondToListener_OnFriendInvitationRespondToSuccess(HandleRef jarg1, HandleRef jarg2, bool jarg3);

		// Token: 0x060001A4 RID: 420
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationRespondToListener_OnFriendInvitationRespondToFailure___")]
		public static extern void IFriendInvitationRespondToListener_OnFriendInvitationRespondToFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001A5 RID: 421
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendInvitationRespondToListener___")]
		public static extern IntPtr new_IFriendInvitationRespondToListener();

		// Token: 0x060001A6 RID: 422
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendInvitationRespondToListener___")]
		public static extern void delete_IFriendInvitationRespondToListener(HandleRef jarg1);

		// Token: 0x060001A7 RID: 423
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationRespondToListener_director_connect___")]
		public static extern void IFriendInvitationRespondToListener_director_connect(HandleRef jarg1, IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_0 delegate0, IFriendInvitationRespondToListener.SwigDelegateIFriendInvitationRespondToListener_1 delegate1);

		// Token: 0x060001A8 RID: 424
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendAddListener_OnFriendAdded___")]
		public static extern void IFriendAddListener_OnFriendAdded(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001A9 RID: 425
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendAddListener___")]
		public static extern IntPtr new_IFriendAddListener();

		// Token: 0x060001AA RID: 426
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendAddListener___")]
		public static extern void delete_IFriendAddListener(HandleRef jarg1);

		// Token: 0x060001AB RID: 427
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendAddListener_director_connect___")]
		public static extern void IFriendAddListener_director_connect(HandleRef jarg1, IFriendAddListener.SwigDelegateIFriendAddListener_0 delegate0);

		// Token: 0x060001AC RID: 428
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendDeleteListener_OnFriendDeleteSuccess___")]
		public static extern void IFriendDeleteListener_OnFriendDeleteSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001AD RID: 429
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendDeleteListener_OnFriendDeleteFailure___")]
		public static extern void IFriendDeleteListener_OnFriendDeleteFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001AE RID: 430
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFriendDeleteListener___")]
		public static extern IntPtr new_IFriendDeleteListener();

		// Token: 0x060001AF RID: 431
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriendDeleteListener___")]
		public static extern void delete_IFriendDeleteListener(HandleRef jarg1);

		// Token: 0x060001B0 RID: 432
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendDeleteListener_director_connect___")]
		public static extern void IFriendDeleteListener_director_connect(HandleRef jarg1, IFriendDeleteListener.SwigDelegateIFriendDeleteListener_0 delegate0, IFriendDeleteListener.SwigDelegateIFriendDeleteListener_1 delegate1);

		// Token: 0x060001B1 RID: 433
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceChangeListener_OnRichPresenceChangeSuccess___")]
		public static extern void IRichPresenceChangeListener_OnRichPresenceChangeSuccess(HandleRef jarg1);

		// Token: 0x060001B2 RID: 434
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceChangeListener_OnRichPresenceChangeFailure___")]
		public static extern void IRichPresenceChangeListener_OnRichPresenceChangeFailure(HandleRef jarg1, int jarg2);

		// Token: 0x060001B3 RID: 435
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IRichPresenceChangeListener___")]
		public static extern IntPtr new_IRichPresenceChangeListener();

		// Token: 0x060001B4 RID: 436
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IRichPresenceChangeListener___")]
		public static extern void delete_IRichPresenceChangeListener(HandleRef jarg1);

		// Token: 0x060001B5 RID: 437
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceChangeListener_director_connect___")]
		public static extern void IRichPresenceChangeListener_director_connect(HandleRef jarg1, IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_0 delegate0, IRichPresenceChangeListener.SwigDelegateIRichPresenceChangeListener_1 delegate1);

		// Token: 0x060001B6 RID: 438
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceListener_OnRichPresenceUpdated___")]
		public static extern void IRichPresenceListener_OnRichPresenceUpdated(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001B7 RID: 439
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IRichPresenceListener___")]
		public static extern IntPtr new_IRichPresenceListener();

		// Token: 0x060001B8 RID: 440
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IRichPresenceListener___")]
		public static extern void delete_IRichPresenceListener(HandleRef jarg1);

		// Token: 0x060001B9 RID: 441
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceListener_director_connect___")]
		public static extern void IRichPresenceListener_director_connect(HandleRef jarg1, IRichPresenceListener.SwigDelegateIRichPresenceListener_0 delegate0);

		// Token: 0x060001BA RID: 442
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceRetrieveListener_OnRichPresenceRetrieveSuccess___")]
		public static extern void IRichPresenceRetrieveListener_OnRichPresenceRetrieveSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001BB RID: 443
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceRetrieveListener_OnRichPresenceRetrieveFailure___")]
		public static extern void IRichPresenceRetrieveListener_OnRichPresenceRetrieveFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001BC RID: 444
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IRichPresenceRetrieveListener___")]
		public static extern IntPtr new_IRichPresenceRetrieveListener();

		// Token: 0x060001BD RID: 445
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IRichPresenceRetrieveListener___")]
		public static extern void delete_IRichPresenceRetrieveListener(HandleRef jarg1);

		// Token: 0x060001BE RID: 446
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceRetrieveListener_director_connect___")]
		public static extern void IRichPresenceRetrieveListener_director_connect(HandleRef jarg1, IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_0 delegate0, IRichPresenceRetrieveListener.SwigDelegateIRichPresenceRetrieveListener_1 delegate1);

		// Token: 0x060001BF RID: 447
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGameJoinRequestedListener_OnGameJoinRequested___")]
		public static extern void IGameJoinRequestedListener_OnGameJoinRequested(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x060001C0 RID: 448
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IGameJoinRequestedListener___")]
		public static extern IntPtr new_IGameJoinRequestedListener();

		// Token: 0x060001C1 RID: 449
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IGameJoinRequestedListener___")]
		public static extern void delete_IGameJoinRequestedListener(HandleRef jarg1);

		// Token: 0x060001C2 RID: 450
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGameJoinRequestedListener_director_connect___")]
		public static extern void IGameJoinRequestedListener_director_connect(HandleRef jarg1, IGameJoinRequestedListener.SwigDelegateIGameJoinRequestedListener_0 delegate0);

		// Token: 0x060001C3 RID: 451
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGameInvitationReceivedListener_OnGameInvitationReceived___")]
		public static extern void IGameInvitationReceivedListener_OnGameInvitationReceived(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x060001C4 RID: 452
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IGameInvitationReceivedListener___")]
		public static extern IntPtr new_IGameInvitationReceivedListener();

		// Token: 0x060001C5 RID: 453
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IGameInvitationReceivedListener___")]
		public static extern void delete_IGameInvitationReceivedListener(HandleRef jarg1);

		// Token: 0x060001C6 RID: 454
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGameInvitationReceivedListener_director_connect___")]
		public static extern void IGameInvitationReceivedListener_director_connect(HandleRef jarg1, IGameInvitationReceivedListener.SwigDelegateIGameInvitationReceivedListener_0 delegate0);

		// Token: 0x060001C7 RID: 455
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISendInvitationListener_OnInvitationSendSuccess___")]
		public static extern void ISendInvitationListener_OnInvitationSendSuccess(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x060001C8 RID: 456
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISendInvitationListener_OnInvitationSendFailure___")]
		public static extern void ISendInvitationListener_OnInvitationSendFailure(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, int jarg4);

		// Token: 0x060001C9 RID: 457
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ISendInvitationListener___")]
		public static extern IntPtr new_ISendInvitationListener();

		// Token: 0x060001CA RID: 458
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ISendInvitationListener___")]
		public static extern void delete_ISendInvitationListener(HandleRef jarg1);

		// Token: 0x060001CB RID: 459
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISendInvitationListener_director_connect___")]
		public static extern void ISendInvitationListener_director_connect(HandleRef jarg1, ISendInvitationListener.SwigDelegateISendInvitationListener_0 delegate0, ISendInvitationListener.SwigDelegateISendInvitationListener_1 delegate1);

		// Token: 0x060001CC RID: 460
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserFindListener_OnUserFindSuccess___")]
		public static extern void IUserFindListener_OnUserFindSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x060001CD RID: 461
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserFindListener_OnUserFindFailure___")]
		public static extern void IUserFindListener_OnUserFindFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x060001CE RID: 462
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IUserFindListener___")]
		public static extern IntPtr new_IUserFindListener();

		// Token: 0x060001CF RID: 463
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUserFindListener___")]
		public static extern void delete_IUserFindListener(HandleRef jarg1);

		// Token: 0x060001D0 RID: 464
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserFindListener_director_connect___")]
		public static extern void IUserFindListener_director_connect(HandleRef jarg1, IUserFindListener.SwigDelegateIUserFindListener_0 delegate0, IUserFindListener.SwigDelegateIUserFindListener_1 delegate1);

		// Token: 0x060001D1 RID: 465
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFriends___")]
		public static extern void delete_IFriends(HandleRef jarg1);

		// Token: 0x060001D2 RID: 466
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetDefaultAvatarCriteria___")]
		public static extern uint IFriends_GetDefaultAvatarCriteria(HandleRef jarg1);

		// Token: 0x060001D3 RID: 467
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SetDefaultAvatarCriteria___")]
		public static extern void IFriends_SetDefaultAvatarCriteria(HandleRef jarg1, uint jarg2);

		// Token: 0x060001D4 RID: 468
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestUserInformation__SWIG_0___")]
		public static extern void IFriends_RequestUserInformation__SWIG_0(HandleRef jarg1, HandleRef jarg2, uint jarg3, HandleRef jarg4);

		// Token: 0x060001D5 RID: 469
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestUserInformation__SWIG_1___")]
		public static extern void IFriends_RequestUserInformation__SWIG_1(HandleRef jarg1, HandleRef jarg2, uint jarg3);

		// Token: 0x060001D6 RID: 470
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestUserInformation__SWIG_2___")]
		public static extern void IFriends_RequestUserInformation__SWIG_2(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001D7 RID: 471
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_IsUserInformationAvailable___")]
		public static extern bool IFriends_IsUserInformationAvailable(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001D8 RID: 472
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetPersonaName___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IFriends_GetPersonaName(HandleRef jarg1);

		// Token: 0x060001D9 RID: 473
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetPersonaNameCopy___")]
		public static extern void IFriends_GetPersonaNameCopy(HandleRef jarg1, byte[] jarg2, uint jarg3);

		// Token: 0x060001DA RID: 474
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetPersonaState___")]
		public static extern int IFriends_GetPersonaState(HandleRef jarg1);

		// Token: 0x060001DB RID: 475
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendPersonaName___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IFriends_GetFriendPersonaName(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001DC RID: 476
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendPersonaNameCopy___")]
		public static extern void IFriends_GetFriendPersonaNameCopy(HandleRef jarg1, HandleRef jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x060001DD RID: 477
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendPersonaState___")]
		public static extern int IFriends_GetFriendPersonaState(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001DE RID: 478
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendAvatarUrl___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IFriends_GetFriendAvatarUrl(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001DF RID: 479
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendAvatarUrlCopy___")]
		public static extern void IFriends_GetFriendAvatarUrlCopy(HandleRef jarg1, HandleRef jarg2, int jarg3, byte[] jarg4, uint jarg5);

		// Token: 0x060001E0 RID: 480
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendAvatarImageID___")]
		public static extern uint IFriends_GetFriendAvatarImageID(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001E1 RID: 481
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendAvatarImageRGBA___")]
		public static extern void IFriends_GetFriendAvatarImageRGBA(HandleRef jarg1, HandleRef jarg2, int jarg3, byte[] jarg4, uint jarg5);

		// Token: 0x060001E2 RID: 482
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_IsFriendAvatarImageRGBAAvailable___")]
		public static extern bool IFriends_IsFriendAvatarImageRGBAAvailable(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060001E3 RID: 483
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestFriendList__SWIG_0___")]
		public static extern void IFriends_RequestFriendList__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001E4 RID: 484
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestFriendList__SWIG_1___")]
		public static extern void IFriends_RequestFriendList__SWIG_1(HandleRef jarg1);

		// Token: 0x060001E5 RID: 485
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_IsFriend___")]
		public static extern bool IFriends_IsFriend(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001E6 RID: 486
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendCount___")]
		public static extern uint IFriends_GetFriendCount(HandleRef jarg1);

		// Token: 0x060001E7 RID: 487
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendByIndex___")]
		public static extern IntPtr IFriends_GetFriendByIndex(HandleRef jarg1, uint jarg2);

		// Token: 0x060001E8 RID: 488
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SendFriendInvitation__SWIG_0___")]
		public static extern void IFriends_SendFriendInvitation__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060001E9 RID: 489
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SendFriendInvitation__SWIG_1___")]
		public static extern void IFriends_SendFriendInvitation__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001EA RID: 490
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestFriendInvitationList__SWIG_0___")]
		public static extern void IFriends_RequestFriendInvitationList__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001EB RID: 491
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestFriendInvitationList__SWIG_1___")]
		public static extern void IFriends_RequestFriendInvitationList__SWIG_1(HandleRef jarg1);

		// Token: 0x060001EC RID: 492
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestSentFriendInvitationList__SWIG_0___")]
		public static extern void IFriends_RequestSentFriendInvitationList__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001ED RID: 493
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestSentFriendInvitationList__SWIG_1___")]
		public static extern void IFriends_RequestSentFriendInvitationList__SWIG_1(HandleRef jarg1);

		// Token: 0x060001EE RID: 494
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendInvitationCount___")]
		public static extern uint IFriends_GetFriendInvitationCount(HandleRef jarg1);

		// Token: 0x060001EF RID: 495
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetFriendInvitationByIndex___")]
		public static extern void IFriends_GetFriendInvitationByIndex(HandleRef jarg1, uint jarg2, HandleRef jarg3, ref uint jarg4);

		// Token: 0x060001F0 RID: 496
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RespondToFriendInvitation__SWIG_0___")]
		public static extern void IFriends_RespondToFriendInvitation__SWIG_0(HandleRef jarg1, HandleRef jarg2, bool jarg3, HandleRef jarg4);

		// Token: 0x060001F1 RID: 497
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RespondToFriendInvitation__SWIG_1___")]
		public static extern void IFriends_RespondToFriendInvitation__SWIG_1(HandleRef jarg1, HandleRef jarg2, bool jarg3);

		// Token: 0x060001F2 RID: 498
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_DeleteFriend__SWIG_0___")]
		public static extern void IFriends_DeleteFriend__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060001F3 RID: 499
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_DeleteFriend__SWIG_1___")]
		public static extern void IFriends_DeleteFriend__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001F4 RID: 500
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SetRichPresence__SWIG_0___")]
		public static extern void IFriends_SetRichPresence__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x060001F5 RID: 501
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SetRichPresence__SWIG_1___")]
		public static extern void IFriends_SetRichPresence__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x060001F6 RID: 502
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_DeleteRichPresence__SWIG_0___")]
		public static extern void IFriends_DeleteRichPresence__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x060001F7 RID: 503
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_DeleteRichPresence__SWIG_1___")]
		public static extern void IFriends_DeleteRichPresence__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x060001F8 RID: 504
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_ClearRichPresence__SWIG_0___")]
		public static extern void IFriends_ClearRichPresence__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001F9 RID: 505
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_ClearRichPresence__SWIG_1___")]
		public static extern void IFriends_ClearRichPresence__SWIG_1(HandleRef jarg1);

		// Token: 0x060001FA RID: 506
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestRichPresence__SWIG_0___")]
		public static extern void IFriends_RequestRichPresence__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060001FB RID: 507
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestRichPresence__SWIG_1___")]
		public static extern void IFriends_RequestRichPresence__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060001FC RID: 508
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_RequestRichPresence__SWIG_2___")]
		public static extern void IFriends_RequestRichPresence__SWIG_2(HandleRef jarg1);

		// Token: 0x060001FD RID: 509
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresence__SWIG_0___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IFriends_GetRichPresence__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x060001FE RID: 510
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresence__SWIG_1___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IFriends_GetRichPresence__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x060001FF RID: 511
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresenceCopy__SWIG_0___")]
		public static extern void IFriends_GetRichPresenceCopy__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4, HandleRef jarg5);

		// Token: 0x06000200 RID: 512
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresenceCopy__SWIG_1___")]
		public static extern void IFriends_GetRichPresenceCopy__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000201 RID: 513
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresenceCount__SWIG_0___")]
		public static extern uint IFriends_GetRichPresenceCount__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000202 RID: 514
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresenceCount__SWIG_1___")]
		public static extern uint IFriends_GetRichPresenceCount__SWIG_1(HandleRef jarg1);

		// Token: 0x06000203 RID: 515
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresenceByIndex__SWIG_0___")]
		public static extern void IFriends_GetRichPresenceByIndex__SWIG_0(HandleRef jarg1, uint jarg2, byte[] jarg3, uint jarg4, byte[] jarg5, uint jarg6, HandleRef jarg7);

		// Token: 0x06000204 RID: 516
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_GetRichPresenceByIndex__SWIG_1___")]
		public static extern void IFriends_GetRichPresenceByIndex__SWIG_1(HandleRef jarg1, uint jarg2, byte[] jarg3, uint jarg4, byte[] jarg5, uint jarg6);

		// Token: 0x06000205 RID: 517
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_ShowOverlayInviteDialog___")]
		public static extern void IFriends_ShowOverlayInviteDialog(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000206 RID: 518
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SendInvitation__SWIG_0___")]
		public static extern void IFriends_SendInvitation__SWIG_0(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x06000207 RID: 519
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_SendInvitation__SWIG_1___")]
		public static extern void IFriends_SendInvitation__SWIG_1(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000208 RID: 520
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_FindUser__SWIG_0___")]
		public static extern void IFriends_FindUser__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000209 RID: 521
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_FindUser__SWIG_1___")]
		public static extern void IFriends_FindUser__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600020A RID: 522
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriends_IsUserInTheSameGame___")]
		public static extern bool IFriends_IsUserInTheSameGame(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600020B RID: 523
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserStatsAndAchievementsRetrieveListener_OnUserStatsAndAchievementsRetrieveSuccess___")]
		public static extern void IUserStatsAndAchievementsRetrieveListener_OnUserStatsAndAchievementsRetrieveSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600020C RID: 524
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserStatsAndAchievementsRetrieveListener_OnUserStatsAndAchievementsRetrieveFailure___")]
		public static extern void IUserStatsAndAchievementsRetrieveListener_OnUserStatsAndAchievementsRetrieveFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x0600020D RID: 525
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IUserStatsAndAchievementsRetrieveListener___")]
		public static extern IntPtr new_IUserStatsAndAchievementsRetrieveListener();

		// Token: 0x0600020E RID: 526
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUserStatsAndAchievementsRetrieveListener___")]
		public static extern void delete_IUserStatsAndAchievementsRetrieveListener(HandleRef jarg1);

		// Token: 0x0600020F RID: 527
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserStatsAndAchievementsRetrieveListener_director_connect___")]
		public static extern void IUserStatsAndAchievementsRetrieveListener_director_connect(HandleRef jarg1, IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_0 delegate0, IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_1 delegate1);

		// Token: 0x06000210 RID: 528
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStatsAndAchievementsStoreListener_OnUserStatsAndAchievementsStoreSuccess___")]
		public static extern void IStatsAndAchievementsStoreListener_OnUserStatsAndAchievementsStoreSuccess(HandleRef jarg1);

		// Token: 0x06000211 RID: 529
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStatsAndAchievementsStoreListener_OnUserStatsAndAchievementsStoreFailure___")]
		public static extern void IStatsAndAchievementsStoreListener_OnUserStatsAndAchievementsStoreFailure(HandleRef jarg1, int jarg2);

		// Token: 0x06000212 RID: 530
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IStatsAndAchievementsStoreListener___")]
		public static extern IntPtr new_IStatsAndAchievementsStoreListener();

		// Token: 0x06000213 RID: 531
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IStatsAndAchievementsStoreListener___")]
		public static extern void delete_IStatsAndAchievementsStoreListener(HandleRef jarg1);

		// Token: 0x06000214 RID: 532
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStatsAndAchievementsStoreListener_director_connect___")]
		public static extern void IStatsAndAchievementsStoreListener_director_connect(HandleRef jarg1, IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_0 delegate0, IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_1 delegate1);

		// Token: 0x06000215 RID: 533
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAchievementChangeListener_OnAchievementUnlocked___")]
		public static extern void IAchievementChangeListener_OnAchievementUnlocked(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000216 RID: 534
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IAchievementChangeListener___")]
		public static extern IntPtr new_IAchievementChangeListener();

		// Token: 0x06000217 RID: 535
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IAchievementChangeListener___")]
		public static extern void delete_IAchievementChangeListener(HandleRef jarg1);

		// Token: 0x06000218 RID: 536
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAchievementChangeListener_director_connect___")]
		public static extern void IAchievementChangeListener_director_connect(HandleRef jarg1, IAchievementChangeListener.SwigDelegateIAchievementChangeListener_0 delegate0);

		// Token: 0x06000219 RID: 537
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardsRetrieveListener_OnLeaderboardsRetrieveSuccess___")]
		public static extern void ILeaderboardsRetrieveListener_OnLeaderboardsRetrieveSuccess(HandleRef jarg1);

		// Token: 0x0600021A RID: 538
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardsRetrieveListener_OnLeaderboardsRetrieveFailure___")]
		public static extern void ILeaderboardsRetrieveListener_OnLeaderboardsRetrieveFailure(HandleRef jarg1, int jarg2);

		// Token: 0x0600021B RID: 539
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILeaderboardsRetrieveListener___")]
		public static extern IntPtr new_ILeaderboardsRetrieveListener();

		// Token: 0x0600021C RID: 540
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILeaderboardsRetrieveListener___")]
		public static extern void delete_ILeaderboardsRetrieveListener(HandleRef jarg1);

		// Token: 0x0600021D RID: 541
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardsRetrieveListener_director_connect___")]
		public static extern void ILeaderboardsRetrieveListener_director_connect(HandleRef jarg1, ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_0 delegate0, ILeaderboardsRetrieveListener.SwigDelegateILeaderboardsRetrieveListener_1 delegate1);

		// Token: 0x0600021E RID: 542
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardEntriesRetrieveListener_OnLeaderboardEntriesRetrieveSuccess___")]
		public static extern void ILeaderboardEntriesRetrieveListener_OnLeaderboardEntriesRetrieveSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3);

		// Token: 0x0600021F RID: 543
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardEntriesRetrieveListener_OnLeaderboardEntriesRetrieveFailure___")]
		public static extern void ILeaderboardEntriesRetrieveListener_OnLeaderboardEntriesRetrieveFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x06000220 RID: 544
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILeaderboardEntriesRetrieveListener___")]
		public static extern IntPtr new_ILeaderboardEntriesRetrieveListener();

		// Token: 0x06000221 RID: 545
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILeaderboardEntriesRetrieveListener___")]
		public static extern void delete_ILeaderboardEntriesRetrieveListener(HandleRef jarg1);

		// Token: 0x06000222 RID: 546
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardEntriesRetrieveListener_director_connect___")]
		public static extern void ILeaderboardEntriesRetrieveListener_director_connect(HandleRef jarg1, ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_0 delegate0, ILeaderboardEntriesRetrieveListener.SwigDelegateILeaderboardEntriesRetrieveListener_1 delegate1);

		// Token: 0x06000223 RID: 547
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardScoreUpdateListener_OnLeaderboardScoreUpdateSuccess___")]
		public static extern void ILeaderboardScoreUpdateListener_OnLeaderboardScoreUpdateSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, uint jarg4, uint jarg5);

		// Token: 0x06000224 RID: 548
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardScoreUpdateListener_OnLeaderboardScoreUpdateFailure___")]
		public static extern void ILeaderboardScoreUpdateListener_OnLeaderboardScoreUpdateFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, int jarg4);

		// Token: 0x06000225 RID: 549
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILeaderboardScoreUpdateListener___")]
		public static extern IntPtr new_ILeaderboardScoreUpdateListener();

		// Token: 0x06000226 RID: 550
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILeaderboardScoreUpdateListener___")]
		public static extern void delete_ILeaderboardScoreUpdateListener(HandleRef jarg1);

		// Token: 0x06000227 RID: 551
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardScoreUpdateListener_director_connect___")]
		public static extern void ILeaderboardScoreUpdateListener_director_connect(HandleRef jarg1, ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_0 delegate0, ILeaderboardScoreUpdateListener.SwigDelegateILeaderboardScoreUpdateListener_1 delegate1);

		// Token: 0x06000228 RID: 552
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardRetrieveListener_OnLeaderboardRetrieveSuccess___")]
		public static extern void ILeaderboardRetrieveListener_OnLeaderboardRetrieveSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000229 RID: 553
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardRetrieveListener_OnLeaderboardRetrieveFailure___")]
		public static extern void ILeaderboardRetrieveListener_OnLeaderboardRetrieveFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x0600022A RID: 554
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILeaderboardRetrieveListener___")]
		public static extern IntPtr new_ILeaderboardRetrieveListener();

		// Token: 0x0600022B RID: 555
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILeaderboardRetrieveListener___")]
		public static extern void delete_ILeaderboardRetrieveListener(HandleRef jarg1);

		// Token: 0x0600022C RID: 556
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardRetrieveListener_director_connect___")]
		public static extern void ILeaderboardRetrieveListener_director_connect(HandleRef jarg1, ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_0 delegate0, ILeaderboardRetrieveListener.SwigDelegateILeaderboardRetrieveListener_1 delegate1);

		// Token: 0x0600022D RID: 557
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserTimePlayedRetrieveListener_OnUserTimePlayedRetrieveSuccess___")]
		public static extern void IUserTimePlayedRetrieveListener_OnUserTimePlayedRetrieveSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600022E RID: 558
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserTimePlayedRetrieveListener_OnUserTimePlayedRetrieveFailure___")]
		public static extern void IUserTimePlayedRetrieveListener_OnUserTimePlayedRetrieveFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x0600022F RID: 559
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IUserTimePlayedRetrieveListener___")]
		public static extern IntPtr new_IUserTimePlayedRetrieveListener();

		// Token: 0x06000230 RID: 560
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IUserTimePlayedRetrieveListener___")]
		public static extern void delete_IUserTimePlayedRetrieveListener(HandleRef jarg1);

		// Token: 0x06000231 RID: 561
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserTimePlayedRetrieveListener_director_connect___")]
		public static extern void IUserTimePlayedRetrieveListener_director_connect(HandleRef jarg1, IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_0 delegate0, IUserTimePlayedRetrieveListener.SwigDelegateIUserTimePlayedRetrieveListener_1 delegate1);

		// Token: 0x06000232 RID: 562
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IStats___")]
		public static extern void delete_IStats(HandleRef jarg1);

		// Token: 0x06000233 RID: 563
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestUserStatsAndAchievements__SWIG_0___")]
		public static extern void IStats_RequestUserStatsAndAchievements__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x06000234 RID: 564
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestUserStatsAndAchievements__SWIG_1___")]
		public static extern void IStats_RequestUserStatsAndAchievements__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000235 RID: 565
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestUserStatsAndAchievements__SWIG_2___")]
		public static extern void IStats_RequestUserStatsAndAchievements__SWIG_2(HandleRef jarg1);

		// Token: 0x06000236 RID: 566
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetStatInt__SWIG_0___")]
		public static extern int IStats_GetStatInt__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000237 RID: 567
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetStatInt__SWIG_1___")]
		public static extern int IStats_GetStatInt__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000238 RID: 568
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetStatFloat__SWIG_0___")]
		public static extern float IStats_GetStatFloat__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000239 RID: 569
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetStatFloat__SWIG_1___")]
		public static extern float IStats_GetStatFloat__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600023A RID: 570
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetStatInt___")]
		public static extern void IStats_SetStatInt(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x0600023B RID: 571
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetStatFloat___")]
		public static extern void IStats_SetStatFloat(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, float jarg3);

		// Token: 0x0600023C RID: 572
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_UpdateAvgRateStat___")]
		public static extern void IStats_UpdateAvgRateStat(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, float jarg3, double jarg4);

		// Token: 0x0600023D RID: 573
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetAchievement__SWIG_0___")]
		public static extern void IStats_GetAchievement__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, ref bool jarg3, ref uint jarg4, HandleRef jarg5);

		// Token: 0x0600023E RID: 574
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetAchievement__SWIG_1___")]
		public static extern void IStats_GetAchievement__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, ref bool jarg3, ref uint jarg4);

		// Token: 0x0600023F RID: 575
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetAchievement___")]
		public static extern void IStats_SetAchievement(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000240 RID: 576
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_ClearAchievement___")]
		public static extern void IStats_ClearAchievement(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000241 RID: 577
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_StoreStatsAndAchievements__SWIG_0___")]
		public static extern void IStats_StoreStatsAndAchievements__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000242 RID: 578
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_StoreStatsAndAchievements__SWIG_1___")]
		public static extern void IStats_StoreStatsAndAchievements__SWIG_1(HandleRef jarg1);

		// Token: 0x06000243 RID: 579
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_ResetStatsAndAchievements__SWIG_0___")]
		public static extern void IStats_ResetStatsAndAchievements__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000244 RID: 580
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_ResetStatsAndAchievements__SWIG_1___")]
		public static extern void IStats_ResetStatsAndAchievements__SWIG_1(HandleRef jarg1);

		// Token: 0x06000245 RID: 581
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetAchievementDisplayName___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IStats_GetAchievementDisplayName(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000246 RID: 582
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetAchievementDisplayNameCopy___")]
		public static extern void IStats_GetAchievementDisplayNameCopy(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000247 RID: 583
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetAchievementDescription___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IStats_GetAchievementDescription(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000248 RID: 584
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetAchievementDescriptionCopy___")]
		public static extern void IStats_GetAchievementDescriptionCopy(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000249 RID: 585
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_IsAchievementVisible___")]
		public static extern bool IStats_IsAchievementVisible(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600024A RID: 586
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_IsAchievementVisibleWhileLocked___")]
		public static extern bool IStats_IsAchievementVisibleWhileLocked(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600024B RID: 587
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboards__SWIG_0___")]
		public static extern void IStats_RequestLeaderboards__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600024C RID: 588
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboards__SWIG_1___")]
		public static extern void IStats_RequestLeaderboards__SWIG_1(HandleRef jarg1);

		// Token: 0x0600024D RID: 589
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetLeaderboardDisplayName___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IStats_GetLeaderboardDisplayName(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600024E RID: 590
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetLeaderboardDisplayNameCopy___")]
		public static extern void IStats_GetLeaderboardDisplayNameCopy(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x0600024F RID: 591
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetLeaderboardSortMethod___")]
		public static extern int IStats_GetLeaderboardSortMethod(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000250 RID: 592
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetLeaderboardDisplayType___")]
		public static extern int IStats_GetLeaderboardDisplayType(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000251 RID: 593
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesGlobal__SWIG_0___")]
		public static extern void IStats_RequestLeaderboardEntriesGlobal__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3, uint jarg4, HandleRef jarg5);

		// Token: 0x06000252 RID: 594
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesGlobal__SWIG_1___")]
		public static extern void IStats_RequestLeaderboardEntriesGlobal__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3, uint jarg4);

		// Token: 0x06000253 RID: 595
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesAroundUser__SWIG_0___")]
		public static extern void IStats_RequestLeaderboardEntriesAroundUser__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3, uint jarg4, HandleRef jarg5, HandleRef jarg6);

		// Token: 0x06000254 RID: 596
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesAroundUser__SWIG_1___")]
		public static extern void IStats_RequestLeaderboardEntriesAroundUser__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3, uint jarg4, HandleRef jarg5);

		// Token: 0x06000255 RID: 597
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesAroundUser__SWIG_2___")]
		public static extern void IStats_RequestLeaderboardEntriesAroundUser__SWIG_2(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3, uint jarg4);

		// Token: 0x06000256 RID: 598
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesForUsers__SWIG_0___")]
		public static extern void IStats_RequestLeaderboardEntriesForUsers__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, ulong[] array, uint jarg3, HandleRef jarg5);

		// Token: 0x06000257 RID: 599
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestLeaderboardEntriesForUsers__SWIG_1___")]
		public static extern void IStats_RequestLeaderboardEntriesForUsers__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, ulong[] array, uint jarg3);

		// Token: 0x06000258 RID: 600
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetRequestedLeaderboardEntry___")]
		public static extern void IStats_GetRequestedLeaderboardEntry(HandleRef jarg1, uint jarg2, ref uint jarg3, ref int jarg4, HandleRef jarg5);

		// Token: 0x06000259 RID: 601
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetRequestedLeaderboardEntryWithDetails___")]
		public static extern void IStats_GetRequestedLeaderboardEntryWithDetails(HandleRef jarg1, uint jarg2, ref uint jarg3, ref int jarg4, byte[] jarg5, uint jarg6, ref uint jarg7, HandleRef jarg8);

		// Token: 0x0600025A RID: 602
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetLeaderboardScore__SWIG_0___")]
		public static extern void IStats_SetLeaderboardScore__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, bool jarg4, HandleRef jarg5);

		// Token: 0x0600025B RID: 603
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetLeaderboardScore__SWIG_1___")]
		public static extern void IStats_SetLeaderboardScore__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, bool jarg4);

		// Token: 0x0600025C RID: 604
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetLeaderboardScore__SWIG_2___")]
		public static extern void IStats_SetLeaderboardScore__SWIG_2(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x0600025D RID: 605
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetLeaderboardScoreWithDetails__SWIG_0___")]
		public static extern void IStats_SetLeaderboardScoreWithDetails__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, byte[] jarg4, uint jarg5, bool jarg6, HandleRef jarg7);

		// Token: 0x0600025E RID: 606
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetLeaderboardScoreWithDetails__SWIG_1___")]
		public static extern void IStats_SetLeaderboardScoreWithDetails__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, byte[] jarg4, uint jarg5, bool jarg6);

		// Token: 0x0600025F RID: 607
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_SetLeaderboardScoreWithDetails__SWIG_2___")]
		public static extern void IStats_SetLeaderboardScoreWithDetails__SWIG_2(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, byte[] jarg4, uint jarg5);

		// Token: 0x06000260 RID: 608
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetLeaderboardEntryCount___")]
		public static extern uint IStats_GetLeaderboardEntryCount(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000261 RID: 609
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_FindLeaderboard__SWIG_0___")]
		public static extern void IStats_FindLeaderboard__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000262 RID: 610
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_FindLeaderboard__SWIG_1___")]
		public static extern void IStats_FindLeaderboard__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000263 RID: 611
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_FindOrCreateLeaderboard__SWIG_0___")]
		public static extern void IStats_FindOrCreateLeaderboard__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, int jarg4, int jarg5, HandleRef jarg6);

		// Token: 0x06000264 RID: 612
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_FindOrCreateLeaderboard__SWIG_1___")]
		public static extern void IStats_FindOrCreateLeaderboard__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, int jarg4, int jarg5);

		// Token: 0x06000265 RID: 613
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestUserTimePlayed__SWIG_0___")]
		public static extern void IStats_RequestUserTimePlayed__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x06000266 RID: 614
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestUserTimePlayed__SWIG_1___")]
		public static extern void IStats_RequestUserTimePlayed__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000267 RID: 615
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_RequestUserTimePlayed__SWIG_2___")]
		public static extern void IStats_RequestUserTimePlayed__SWIG_2(HandleRef jarg1);

		// Token: 0x06000268 RID: 616
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetUserTimePlayed__SWIG_0___")]
		public static extern uint IStats_GetUserTimePlayed__SWIG_0(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000269 RID: 617
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStats_GetUserTimePlayed__SWIG_1___")]
		public static extern uint IStats_GetUserTimePlayed__SWIG_1(HandleRef jarg1);

		// Token: 0x0600026A RID: 618
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFileShareListener_OnFileShareSuccess___")]
		public static extern void IFileShareListener_OnFileShareSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, ulong jarg3);

		// Token: 0x0600026B RID: 619
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFileShareListener_OnFileShareFailure___")]
		public static extern void IFileShareListener_OnFileShareFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x0600026C RID: 620
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IFileShareListener___")]
		public static extern IntPtr new_IFileShareListener();

		// Token: 0x0600026D RID: 621
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IFileShareListener___")]
		public static extern void delete_IFileShareListener(HandleRef jarg1);

		// Token: 0x0600026E RID: 622
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFileShareListener_director_connect___")]
		public static extern void IFileShareListener_director_connect(HandleRef jarg1, IFileShareListener.SwigDelegateIFileShareListener_0 delegate0, IFileShareListener.SwigDelegateIFileShareListener_1 delegate1);

		// Token: 0x0600026F RID: 623
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISharedFileDownloadListener_OnSharedFileDownloadSuccess___")]
		public static extern void ISharedFileDownloadListener_OnSharedFileDownloadSuccess(HandleRef jarg1, ulong jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000270 RID: 624
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISharedFileDownloadListener_OnSharedFileDownloadFailure___")]
		public static extern void ISharedFileDownloadListener_OnSharedFileDownloadFailure(HandleRef jarg1, ulong jarg2, int jarg3);

		// Token: 0x06000271 RID: 625
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ISharedFileDownloadListener___")]
		public static extern IntPtr new_ISharedFileDownloadListener();

		// Token: 0x06000272 RID: 626
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ISharedFileDownloadListener___")]
		public static extern void delete_ISharedFileDownloadListener(HandleRef jarg1);

		// Token: 0x06000273 RID: 627
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISharedFileDownloadListener_director_connect___")]
		public static extern void ISharedFileDownloadListener_director_connect(HandleRef jarg1, ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_0 delegate0, ISharedFileDownloadListener.SwigDelegateISharedFileDownloadListener_1 delegate1);

		// Token: 0x06000274 RID: 628
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IStorage___")]
		public static extern void delete_IStorage(HandleRef jarg1);

		// Token: 0x06000275 RID: 629
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_FileWrite___")]
		public static extern void IStorage_FileWrite(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000276 RID: 630
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_FileRead___")]
		public static extern uint IStorage_FileRead(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000277 RID: 631
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_FileDelete___")]
		public static extern void IStorage_FileDelete(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000278 RID: 632
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_FileExists___")]
		public static extern bool IStorage_FileExists(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000279 RID: 633
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetFileSize___")]
		public static extern uint IStorage_GetFileSize(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600027A RID: 634
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetFileTimestamp___")]
		public static extern uint IStorage_GetFileTimestamp(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600027B RID: 635
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetFileCount___")]
		public static extern uint IStorage_GetFileCount(HandleRef jarg1);

		// Token: 0x0600027C RID: 636
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetFileNameByIndex___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IStorage_GetFileNameByIndex(HandleRef jarg1, uint jarg2);

		// Token: 0x0600027D RID: 637
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetFileNameCopyByIndex___")]
		public static extern void IStorage_GetFileNameCopyByIndex(HandleRef jarg1, uint jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x0600027E RID: 638
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_FileShare__SWIG_0___")]
		public static extern void IStorage_FileShare__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x0600027F RID: 639
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_FileShare__SWIG_1___")]
		public static extern void IStorage_FileShare__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000280 RID: 640
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_DownloadSharedFile__SWIG_0___")]
		public static extern void IStorage_DownloadSharedFile__SWIG_0(HandleRef jarg1, ulong jarg2, HandleRef jarg3);

		// Token: 0x06000281 RID: 641
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_DownloadSharedFile__SWIG_1___")]
		public static extern void IStorage_DownloadSharedFile__SWIG_1(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000282 RID: 642
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetSharedFileName___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IStorage_GetSharedFileName(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000283 RID: 643
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetSharedFileNameCopy___")]
		public static extern void IStorage_GetSharedFileNameCopy(HandleRef jarg1, ulong jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000284 RID: 644
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetSharedFileSize___")]
		public static extern uint IStorage_GetSharedFileSize(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000285 RID: 645
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetSharedFileOwner___")]
		public static extern IntPtr IStorage_GetSharedFileOwner(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000286 RID: 646
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_SharedFileRead__SWIG_0___")]
		public static extern uint IStorage_SharedFileRead__SWIG_0(HandleRef jarg1, ulong jarg2, byte[] jarg3, uint jarg4, uint jarg5);

		// Token: 0x06000287 RID: 647
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_SharedFileRead__SWIG_1___")]
		public static extern uint IStorage_SharedFileRead__SWIG_1(HandleRef jarg1, ulong jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000288 RID: 648
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_SharedFileClose___")]
		public static extern void IStorage_SharedFileClose(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000289 RID: 649
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetDownloadedSharedFileCount___")]
		public static extern uint IStorage_GetDownloadedSharedFileCount(HandleRef jarg1);

		// Token: 0x0600028A RID: 650
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStorage_GetDownloadedSharedFileByIndex___")]
		public static extern ulong IStorage_GetDownloadedSharedFileByIndex(HandleRef jarg1, uint jarg2);

		// Token: 0x0600028B RID: 651
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionOpenListener_OnConnectionOpenSuccess___")]
		public static extern void IConnectionOpenListener_OnConnectionOpenSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, ulong jarg3);

		// Token: 0x0600028C RID: 652
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionOpenListener_OnConnectionOpenFailure___")]
		public static extern void IConnectionOpenListener_OnConnectionOpenFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x0600028D RID: 653
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IConnectionOpenListener___")]
		public static extern IntPtr new_IConnectionOpenListener();

		// Token: 0x0600028E RID: 654
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IConnectionOpenListener___")]
		public static extern void delete_IConnectionOpenListener(HandleRef jarg1);

		// Token: 0x0600028F RID: 655
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionOpenListener_director_connect___")]
		public static extern void IConnectionOpenListener_director_connect(HandleRef jarg1, IConnectionOpenListener.SwigDelegateIConnectionOpenListener_0 delegate0, IConnectionOpenListener.SwigDelegateIConnectionOpenListener_1 delegate1);

		// Token: 0x06000290 RID: 656
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionCloseListener_OnConnectionClosed___")]
		public static extern void IConnectionCloseListener_OnConnectionClosed(HandleRef jarg1, ulong jarg2, int jarg3);

		// Token: 0x06000291 RID: 657
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IConnectionCloseListener___")]
		public static extern IntPtr new_IConnectionCloseListener();

		// Token: 0x06000292 RID: 658
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IConnectionCloseListener___")]
		public static extern void delete_IConnectionCloseListener(HandleRef jarg1);

		// Token: 0x06000293 RID: 659
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionCloseListener_director_connect___")]
		public static extern void IConnectionCloseListener_director_connect(HandleRef jarg1, IConnectionCloseListener.SwigDelegateIConnectionCloseListener_0 delegate0);

		// Token: 0x06000294 RID: 660
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionDataListener_OnConnectionDataReceived___")]
		public static extern void IConnectionDataListener_OnConnectionDataReceived(HandleRef jarg1, ulong jarg2, uint jarg3);

		// Token: 0x06000295 RID: 661
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IConnectionDataListener___")]
		public static extern IntPtr new_IConnectionDataListener();

		// Token: 0x06000296 RID: 662
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IConnectionDataListener___")]
		public static extern void delete_IConnectionDataListener(HandleRef jarg1);

		// Token: 0x06000297 RID: 663
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionDataListener_director_connect___")]
		public static extern void IConnectionDataListener_director_connect(HandleRef jarg1, IConnectionDataListener.SwigDelegateIConnectionDataListener_0 delegate0);

		// Token: 0x06000298 RID: 664
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ICustomNetworking___")]
		public static extern void delete_ICustomNetworking(HandleRef jarg1);

		// Token: 0x06000299 RID: 665
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_OpenConnection__SWIG_0___")]
		public static extern void ICustomNetworking_OpenConnection__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x0600029A RID: 666
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_OpenConnection__SWIG_1___")]
		public static extern void ICustomNetworking_OpenConnection__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600029B RID: 667
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_CloseConnection__SWIG_0___")]
		public static extern void ICustomNetworking_CloseConnection__SWIG_0(HandleRef jarg1, ulong jarg2, HandleRef jarg3);

		// Token: 0x0600029C RID: 668
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_CloseConnection__SWIG_1___")]
		public static extern void ICustomNetworking_CloseConnection__SWIG_1(HandleRef jarg1, ulong jarg2);

		// Token: 0x0600029D RID: 669
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_SendData___")]
		public static extern void ICustomNetworking_SendData(HandleRef jarg1, ulong jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x0600029E RID: 670
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_GetAvailableDataSize___")]
		public static extern uint ICustomNetworking_GetAvailableDataSize(HandleRef jarg1, ulong jarg2);

		// Token: 0x0600029F RID: 671
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_PeekData___")]
		public static extern void ICustomNetworking_PeekData(HandleRef jarg1, ulong jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x060002A0 RID: 672
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_ReadData___")]
		public static extern void ICustomNetworking_ReadData(HandleRef jarg1, ulong jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x060002A1 RID: 673
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ICustomNetworking_PopData___")]
		public static extern void ICustomNetworking_PopData(HandleRef jarg1, ulong jarg2, uint jarg3);

		// Token: 0x060002A2 RID: 674
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworkingListener_OnP2PPacketAvailable___")]
		public static extern void INetworkingListener_OnP2PPacketAvailable(HandleRef jarg1, uint jarg2, byte jarg3);

		// Token: 0x060002A3 RID: 675
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_INetworkingListener___")]
		public static extern IntPtr new_INetworkingListener();

		// Token: 0x060002A4 RID: 676
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_INetworkingListener___")]
		public static extern void delete_INetworkingListener(HandleRef jarg1);

		// Token: 0x060002A5 RID: 677
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworkingListener_director_connect___")]
		public static extern void INetworkingListener_director_connect(HandleRef jarg1, INetworkingListener.SwigDelegateINetworkingListener_0 delegate0);

		// Token: 0x060002A6 RID: 678
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INatTypeDetectionListener_OnNatTypeDetectionSuccess___")]
		public static extern void INatTypeDetectionListener_OnNatTypeDetectionSuccess(HandleRef jarg1, int jarg2);

		// Token: 0x060002A7 RID: 679
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INatTypeDetectionListener_OnNatTypeDetectionFailure___")]
		public static extern void INatTypeDetectionListener_OnNatTypeDetectionFailure(HandleRef jarg1);

		// Token: 0x060002A8 RID: 680
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_INatTypeDetectionListener___")]
		public static extern IntPtr new_INatTypeDetectionListener();

		// Token: 0x060002A9 RID: 681
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_INatTypeDetectionListener___")]
		public static extern void delete_INatTypeDetectionListener(HandleRef jarg1);

		// Token: 0x060002AA RID: 682
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INatTypeDetectionListener_director_connect___")]
		public static extern void INatTypeDetectionListener_director_connect(HandleRef jarg1, INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_0 delegate0, INatTypeDetectionListener.SwigDelegateINatTypeDetectionListener_1 delegate1);

		// Token: 0x060002AB RID: 683
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_INetworking___")]
		public static extern void delete_INetworking(HandleRef jarg1);

		// Token: 0x060002AC RID: 684
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_SendP2PPacket__SWIG_0___")]
		public static extern bool INetworking_SendP2PPacket__SWIG_0(HandleRef jarg1, HandleRef jarg2, byte[] jarg3, uint jarg4, int jarg5, byte jarg6);

		// Token: 0x060002AD RID: 685
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_SendP2PPacket__SWIG_1___")]
		public static extern bool INetworking_SendP2PPacket__SWIG_1(HandleRef jarg1, HandleRef jarg2, byte[] jarg3, uint jarg4, int jarg5);

		// Token: 0x060002AE RID: 686
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_PeekP2PPacket__SWIG_0___")]
		public static extern bool INetworking_PeekP2PPacket__SWIG_0(HandleRef jarg1, byte[] jarg2, uint jarg3, ref uint jarg4, HandleRef jarg5, byte jarg6);

		// Token: 0x060002AF RID: 687
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_PeekP2PPacket__SWIG_1___")]
		public static extern bool INetworking_PeekP2PPacket__SWIG_1(HandleRef jarg1, byte[] jarg2, uint jarg3, ref uint jarg4, HandleRef jarg5);

		// Token: 0x060002B0 RID: 688
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_IsP2PPacketAvailable__SWIG_0___")]
		public static extern bool INetworking_IsP2PPacketAvailable__SWIG_0(HandleRef jarg1, ref uint jarg2, byte jarg3);

		// Token: 0x060002B1 RID: 689
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_IsP2PPacketAvailable__SWIG_1___")]
		public static extern bool INetworking_IsP2PPacketAvailable__SWIG_1(HandleRef jarg1, ref uint jarg2);

		// Token: 0x060002B2 RID: 690
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_ReadP2PPacket__SWIG_0___")]
		public static extern bool INetworking_ReadP2PPacket__SWIG_0(HandleRef jarg1, byte[] jarg2, uint jarg3, ref uint jarg4, HandleRef jarg5, byte jarg6);

		// Token: 0x060002B3 RID: 691
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_ReadP2PPacket__SWIG_1___")]
		public static extern bool INetworking_ReadP2PPacket__SWIG_1(HandleRef jarg1, byte[] jarg2, uint jarg3, ref uint jarg4, HandleRef jarg5);

		// Token: 0x060002B4 RID: 692
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_PopP2PPacket__SWIG_0___")]
		public static extern void INetworking_PopP2PPacket__SWIG_0(HandleRef jarg1, byte jarg2);

		// Token: 0x060002B5 RID: 693
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_PopP2PPacket__SWIG_1___")]
		public static extern void INetworking_PopP2PPacket__SWIG_1(HandleRef jarg1);

		// Token: 0x060002B6 RID: 694
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_GetPingWith___")]
		public static extern int INetworking_GetPingWith(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002B7 RID: 695
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_RequestNatTypeDetection___")]
		public static extern void INetworking_RequestNatTypeDetection(HandleRef jarg1);

		// Token: 0x060002B8 RID: 696
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_GetNatType___")]
		public static extern int INetworking_GetNatType(HandleRef jarg1);

		// Token: 0x060002B9 RID: 697
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworking_GetConnectionType___")]
		public static extern int INetworking_GetConnectionType(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002BA RID: 698
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyListListener_OnLobbyList___")]
		public static extern void ILobbyListListener_OnLobbyList(HandleRef jarg1, uint jarg2, int jarg3);

		// Token: 0x060002BB RID: 699
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyListListener___")]
		public static extern IntPtr new_ILobbyListListener();

		// Token: 0x060002BC RID: 700
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyListListener___")]
		public static extern void delete_ILobbyListListener(HandleRef jarg1);

		// Token: 0x060002BD RID: 701
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyListListener_director_connect___")]
		public static extern void ILobbyListListener_director_connect(HandleRef jarg1, ILobbyListListener.SwigDelegateILobbyListListener_0 delegate0);

		// Token: 0x060002BE RID: 702
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyCreatedListener_OnLobbyCreated___")]
		public static extern void ILobbyCreatedListener_OnLobbyCreated(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060002BF RID: 703
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyCreatedListener___")]
		public static extern IntPtr new_ILobbyCreatedListener();

		// Token: 0x060002C0 RID: 704
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyCreatedListener___")]
		public static extern void delete_ILobbyCreatedListener(HandleRef jarg1);

		// Token: 0x060002C1 RID: 705
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyCreatedListener_director_connect___")]
		public static extern void ILobbyCreatedListener_director_connect(HandleRef jarg1, ILobbyCreatedListener.SwigDelegateILobbyCreatedListener_0 delegate0);

		// Token: 0x060002C2 RID: 706
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyEnteredListener_OnLobbyEntered___")]
		public static extern void ILobbyEnteredListener_OnLobbyEntered(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060002C3 RID: 707
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyEnteredListener___")]
		public static extern IntPtr new_ILobbyEnteredListener();

		// Token: 0x060002C4 RID: 708
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyEnteredListener___")]
		public static extern void delete_ILobbyEnteredListener(HandleRef jarg1);

		// Token: 0x060002C5 RID: 709
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyEnteredListener_director_connect___")]
		public static extern void ILobbyEnteredListener_director_connect(HandleRef jarg1, ILobbyEnteredListener.SwigDelegateILobbyEnteredListener_0 delegate0);

		// Token: 0x060002C6 RID: 710
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyLeftListener_OnLobbyLeft___")]
		public static extern void ILobbyLeftListener_OnLobbyLeft(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060002C7 RID: 711
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyLeftListener___")]
		public static extern IntPtr new_ILobbyLeftListener();

		// Token: 0x060002C8 RID: 712
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyLeftListener___")]
		public static extern void delete_ILobbyLeftListener(HandleRef jarg1);

		// Token: 0x060002C9 RID: 713
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyLeftListener_director_connect___")]
		public static extern void ILobbyLeftListener_director_connect(HandleRef jarg1, ILobbyLeftListener.SwigDelegateILobbyLeftListener_0 delegate0);

		// Token: 0x060002CA RID: 714
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataListener_OnLobbyDataUpdated___")]
		public static extern void ILobbyDataListener_OnLobbyDataUpdated(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060002CB RID: 715
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyDataListener___")]
		public static extern IntPtr new_ILobbyDataListener();

		// Token: 0x060002CC RID: 716
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyDataListener___")]
		public static extern void delete_ILobbyDataListener(HandleRef jarg1);

		// Token: 0x060002CD RID: 717
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataListener_director_connect___")]
		public static extern void ILobbyDataListener_director_connect(HandleRef jarg1, ILobbyDataListener.SwigDelegateILobbyDataListener_0 delegate0);

		// Token: 0x060002CE RID: 718
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataUpdateListener_OnLobbyDataUpdateSuccess___")]
		public static extern void ILobbyDataUpdateListener_OnLobbyDataUpdateSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002CF RID: 719
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataUpdateListener_OnLobbyDataUpdateFailure___")]
		public static extern void ILobbyDataUpdateListener_OnLobbyDataUpdateFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060002D0 RID: 720
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyDataUpdateListener___")]
		public static extern IntPtr new_ILobbyDataUpdateListener();

		// Token: 0x060002D1 RID: 721
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyDataUpdateListener___")]
		public static extern void delete_ILobbyDataUpdateListener(HandleRef jarg1);

		// Token: 0x060002D2 RID: 722
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataUpdateListener_director_connect___")]
		public static extern void ILobbyDataUpdateListener_director_connect(HandleRef jarg1, ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_0 delegate0, ILobbyDataUpdateListener.SwigDelegateILobbyDataUpdateListener_1 delegate1);

		// Token: 0x060002D3 RID: 723
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberDataUpdateListener_OnLobbyMemberDataUpdateSuccess___")]
		public static extern void ILobbyMemberDataUpdateListener_OnLobbyMemberDataUpdateSuccess(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060002D4 RID: 724
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberDataUpdateListener_OnLobbyMemberDataUpdateFailure___")]
		public static extern void ILobbyMemberDataUpdateListener_OnLobbyMemberDataUpdateFailure(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3, int jarg4);

		// Token: 0x060002D5 RID: 725
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyMemberDataUpdateListener___")]
		public static extern IntPtr new_ILobbyMemberDataUpdateListener();

		// Token: 0x060002D6 RID: 726
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyMemberDataUpdateListener___")]
		public static extern void delete_ILobbyMemberDataUpdateListener(HandleRef jarg1);

		// Token: 0x060002D7 RID: 727
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberDataUpdateListener_director_connect___")]
		public static extern void ILobbyMemberDataUpdateListener_director_connect(HandleRef jarg1, ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_0 delegate0, ILobbyMemberDataUpdateListener.SwigDelegateILobbyMemberDataUpdateListener_1 delegate1);

		// Token: 0x060002D8 RID: 728
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataRetrieveListener_OnLobbyDataRetrieveSuccess___")]
		public static extern void ILobbyDataRetrieveListener_OnLobbyDataRetrieveSuccess(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002D9 RID: 729
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataRetrieveListener_OnLobbyDataRetrieveFailure___")]
		public static extern void ILobbyDataRetrieveListener_OnLobbyDataRetrieveFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x060002DA RID: 730
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyDataRetrieveListener___")]
		public static extern IntPtr new_ILobbyDataRetrieveListener();

		// Token: 0x060002DB RID: 731
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyDataRetrieveListener___")]
		public static extern void delete_ILobbyDataRetrieveListener(HandleRef jarg1);

		// Token: 0x060002DC RID: 732
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataRetrieveListener_director_connect___")]
		public static extern void ILobbyDataRetrieveListener_director_connect(HandleRef jarg1, ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_0 delegate0, ILobbyDataRetrieveListener.SwigDelegateILobbyDataRetrieveListener_1 delegate1);

		// Token: 0x060002DD RID: 733
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberStateListener_OnLobbyMemberStateChanged___")]
		public static extern void ILobbyMemberStateListener_OnLobbyMemberStateChanged(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3, int jarg4);

		// Token: 0x060002DE RID: 734
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyMemberStateListener___")]
		public static extern IntPtr new_ILobbyMemberStateListener();

		// Token: 0x060002DF RID: 735
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyMemberStateListener___")]
		public static extern void delete_ILobbyMemberStateListener(HandleRef jarg1);

		// Token: 0x060002E0 RID: 736
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberStateListener_director_connect___")]
		public static extern void ILobbyMemberStateListener_director_connect(HandleRef jarg1, ILobbyMemberStateListener.SwigDelegateILobbyMemberStateListener_0 delegate0);

		// Token: 0x060002E1 RID: 737
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyOwnerChangeListener_OnLobbyOwnerChanged___")]
		public static extern void ILobbyOwnerChangeListener_OnLobbyOwnerChanged(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060002E2 RID: 738
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyOwnerChangeListener___")]
		public static extern IntPtr new_ILobbyOwnerChangeListener();

		// Token: 0x060002E3 RID: 739
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyOwnerChangeListener___")]
		public static extern void delete_ILobbyOwnerChangeListener(HandleRef jarg1);

		// Token: 0x060002E4 RID: 740
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyOwnerChangeListener_director_connect___")]
		public static extern void ILobbyOwnerChangeListener_director_connect(HandleRef jarg1, ILobbyOwnerChangeListener.SwigDelegateILobbyOwnerChangeListener_0 delegate0);

		// Token: 0x060002E5 RID: 741
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMessageListener_OnLobbyMessageReceived___")]
		public static extern void ILobbyMessageListener_OnLobbyMessageReceived(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3, uint jarg4, uint jarg5);

		// Token: 0x060002E6 RID: 742
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ILobbyMessageListener___")]
		public static extern IntPtr new_ILobbyMessageListener();

		// Token: 0x060002E7 RID: 743
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ILobbyMessageListener___")]
		public static extern void delete_ILobbyMessageListener(HandleRef jarg1);

		// Token: 0x060002E8 RID: 744
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMessageListener_director_connect___")]
		public static extern void ILobbyMessageListener_director_connect(HandleRef jarg1, ILobbyMessageListener.SwigDelegateILobbyMessageListener_0 delegate0);

		// Token: 0x060002E9 RID: 745
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IMatchmaking___")]
		public static extern void delete_IMatchmaking(HandleRef jarg1);

		// Token: 0x060002EA RID: 746
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_CreateLobby__SWIG_0___")]
		public static extern void IMatchmaking_CreateLobby__SWIG_0(HandleRef jarg1, int jarg2, uint jarg3, bool jarg4, int jarg5, HandleRef jarg6, HandleRef jarg7);

		// Token: 0x060002EB RID: 747
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_CreateLobby__SWIG_1___")]
		public static extern void IMatchmaking_CreateLobby__SWIG_1(HandleRef jarg1, int jarg2, uint jarg3, bool jarg4, int jarg5, HandleRef jarg6);

		// Token: 0x060002EC RID: 748
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_CreateLobby__SWIG_2___")]
		public static extern void IMatchmaking_CreateLobby__SWIG_2(HandleRef jarg1, int jarg2, uint jarg3, bool jarg4, int jarg5);

		// Token: 0x060002ED RID: 749
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_RequestLobbyList__SWIG_0___")]
		public static extern void IMatchmaking_RequestLobbyList__SWIG_0(HandleRef jarg1, bool jarg2, HandleRef jarg3);

		// Token: 0x060002EE RID: 750
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_RequestLobbyList__SWIG_1___")]
		public static extern void IMatchmaking_RequestLobbyList__SWIG_1(HandleRef jarg1, bool jarg2);

		// Token: 0x060002EF RID: 751
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_RequestLobbyList__SWIG_2___")]
		public static extern void IMatchmaking_RequestLobbyList__SWIG_2(HandleRef jarg1);

		// Token: 0x060002F0 RID: 752
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_AddRequestLobbyListResultCountFilter___")]
		public static extern void IMatchmaking_AddRequestLobbyListResultCountFilter(HandleRef jarg1, uint jarg2);

		// Token: 0x060002F1 RID: 753
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_AddRequestLobbyListStringFilter___")]
		public static extern void IMatchmaking_AddRequestLobbyListStringFilter(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, int jarg4);

		// Token: 0x060002F2 RID: 754
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_AddRequestLobbyListNumericalFilter___")]
		public static extern void IMatchmaking_AddRequestLobbyListNumericalFilter(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3, int jarg4);

		// Token: 0x060002F3 RID: 755
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_AddRequestLobbyListNearValueFilter___")]
		public static extern void IMatchmaking_AddRequestLobbyListNearValueFilter(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x060002F4 RID: 756
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyByIndex___")]
		public static extern IntPtr IMatchmaking_GetLobbyByIndex(HandleRef jarg1, uint jarg2);

		// Token: 0x060002F5 RID: 757
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_JoinLobby__SWIG_0___")]
		public static extern void IMatchmaking_JoinLobby__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060002F6 RID: 758
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_JoinLobby__SWIG_1___")]
		public static extern void IMatchmaking_JoinLobby__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002F7 RID: 759
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_LeaveLobby__SWIG_0___")]
		public static extern void IMatchmaking_LeaveLobby__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x060002F8 RID: 760
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_LeaveLobby__SWIG_1___")]
		public static extern void IMatchmaking_LeaveLobby__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002F9 RID: 761
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetMaxNumLobbyMembers__SWIG_0___")]
		public static extern void IMatchmaking_SetMaxNumLobbyMembers__SWIG_0(HandleRef jarg1, HandleRef jarg2, uint jarg3, HandleRef jarg4);

		// Token: 0x060002FA RID: 762
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetMaxNumLobbyMembers__SWIG_1___")]
		public static extern void IMatchmaking_SetMaxNumLobbyMembers__SWIG_1(HandleRef jarg1, HandleRef jarg2, uint jarg3);

		// Token: 0x060002FB RID: 763
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetMaxNumLobbyMembers___")]
		public static extern uint IMatchmaking_GetMaxNumLobbyMembers(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002FC RID: 764
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetNumLobbyMembers___")]
		public static extern uint IMatchmaking_GetNumLobbyMembers(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x060002FD RID: 765
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyMemberByIndex___")]
		public static extern IntPtr IMatchmaking_GetLobbyMemberByIndex(HandleRef jarg1, HandleRef jarg2, uint jarg3);

		// Token: 0x060002FE RID: 766
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyType__SWIG_0___")]
		public static extern void IMatchmaking_SetLobbyType__SWIG_0(HandleRef jarg1, HandleRef jarg2, int jarg3, HandleRef jarg4);

		// Token: 0x060002FF RID: 767
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyType__SWIG_1___")]
		public static extern void IMatchmaking_SetLobbyType__SWIG_1(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x06000300 RID: 768
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyType___")]
		public static extern int IMatchmaking_GetLobbyType(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000301 RID: 769
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyJoinable__SWIG_0___")]
		public static extern void IMatchmaking_SetLobbyJoinable__SWIG_0(HandleRef jarg1, HandleRef jarg2, bool jarg3, HandleRef jarg4);

		// Token: 0x06000302 RID: 770
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyJoinable__SWIG_1___")]
		public static extern void IMatchmaking_SetLobbyJoinable__SWIG_1(HandleRef jarg1, HandleRef jarg2, bool jarg3);

		// Token: 0x06000303 RID: 771
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_IsLobbyJoinable___")]
		public static extern bool IMatchmaking_IsLobbyJoinable(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000304 RID: 772
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_RequestLobbyData__SWIG_0___")]
		public static extern void IMatchmaking_RequestLobbyData__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x06000305 RID: 773
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_RequestLobbyData__SWIG_1___")]
		public static extern void IMatchmaking_RequestLobbyData__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000306 RID: 774
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyData___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IMatchmaking_GetLobbyData(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000307 RID: 775
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyDataCopy___")]
		public static extern void IMatchmaking_GetLobbyDataCopy(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, byte[] jarg4, uint jarg5);

		// Token: 0x06000308 RID: 776
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyData__SWIG_0___")]
		public static extern void IMatchmaking_SetLobbyData__SWIG_0(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, HandleRef jarg5);

		// Token: 0x06000309 RID: 777
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyData__SWIG_1___")]
		public static extern void IMatchmaking_SetLobbyData__SWIG_1(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4);

		// Token: 0x0600030A RID: 778
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyDataCount___")]
		public static extern uint IMatchmaking_GetLobbyDataCount(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600030B RID: 779
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyDataByIndex___")]
		public static extern bool IMatchmaking_GetLobbyDataByIndex(HandleRef jarg1, HandleRef jarg2, uint jarg3, byte[] jarg4, uint jarg5, byte[] jarg6, uint jarg7);

		// Token: 0x0600030C RID: 780
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_DeleteLobbyData__SWIG_0___")]
		public static extern void IMatchmaking_DeleteLobbyData__SWIG_0(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x0600030D RID: 781
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_DeleteLobbyData__SWIG_1___")]
		public static extern void IMatchmaking_DeleteLobbyData__SWIG_1(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x0600030E RID: 782
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyMemberData___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string IMatchmaking_GetLobbyMemberData(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4);

		// Token: 0x0600030F RID: 783
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyMemberDataCopy___")]
		public static extern void IMatchmaking_GetLobbyMemberDataCopy(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, byte[] jarg5, uint jarg6);

		// Token: 0x06000310 RID: 784
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyMemberData__SWIG_0___")]
		public static extern void IMatchmaking_SetLobbyMemberData__SWIG_0(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, HandleRef jarg5);

		// Token: 0x06000311 RID: 785
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SetLobbyMemberData__SWIG_1___")]
		public static extern void IMatchmaking_SetLobbyMemberData__SWIG_1(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4);

		// Token: 0x06000312 RID: 786
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyMemberDataCount___")]
		public static extern uint IMatchmaking_GetLobbyMemberDataCount(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x06000313 RID: 787
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyMemberDataByIndex___")]
		public static extern bool IMatchmaking_GetLobbyMemberDataByIndex(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3, uint jarg4, byte[] jarg5, uint jarg6, byte[] jarg7, uint jarg8);

		// Token: 0x06000314 RID: 788
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_DeleteLobbyMemberData__SWIG_0___")]
		public static extern void IMatchmaking_DeleteLobbyMemberData__SWIG_0(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x06000315 RID: 789
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_DeleteLobbyMemberData__SWIG_1___")]
		public static extern void IMatchmaking_DeleteLobbyMemberData__SWIG_1(HandleRef jarg1, HandleRef jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000316 RID: 790
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyOwner___")]
		public static extern IntPtr IMatchmaking_GetLobbyOwner(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000317 RID: 791
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_SendLobbyMessage___")]
		public static extern bool IMatchmaking_SendLobbyMessage(HandleRef jarg1, HandleRef jarg2, byte[] jarg3, uint jarg4);

		// Token: 0x06000318 RID: 792
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IMatchmaking_GetLobbyMessage___")]
		public static extern uint IMatchmaking_GetLobbyMessage(HandleRef jarg1, HandleRef jarg2, uint jarg3, HandleRef jarg4, byte[] jarg5, uint jarg6);

		// Token: 0x06000319 RID: 793
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomWithUserRetrieveListener_OnChatRoomWithUserRetrieveSuccess___")]
		public static extern void IChatRoomWithUserRetrieveListener_OnChatRoomWithUserRetrieveSuccess(HandleRef jarg1, HandleRef jarg2, ulong jarg3);

		// Token: 0x0600031A RID: 794
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomWithUserRetrieveListener_OnChatRoomWithUserRetrieveFailure___")]
		public static extern void IChatRoomWithUserRetrieveListener_OnChatRoomWithUserRetrieveFailure(HandleRef jarg1, HandleRef jarg2, int jarg3);

		// Token: 0x0600031B RID: 795
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IChatRoomWithUserRetrieveListener___")]
		public static extern IntPtr new_IChatRoomWithUserRetrieveListener();

		// Token: 0x0600031C RID: 796
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IChatRoomWithUserRetrieveListener___")]
		public static extern void delete_IChatRoomWithUserRetrieveListener(HandleRef jarg1);

		// Token: 0x0600031D RID: 797
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomWithUserRetrieveListener_director_connect___")]
		public static extern void IChatRoomWithUserRetrieveListener_director_connect(HandleRef jarg1, IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_0 delegate0, IChatRoomWithUserRetrieveListener.SwigDelegateIChatRoomWithUserRetrieveListener_1 delegate1);

		// Token: 0x0600031E RID: 798
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessageSendListener_OnChatRoomMessageSendSuccess___")]
		public static extern void IChatRoomMessageSendListener_OnChatRoomMessageSendSuccess(HandleRef jarg1, ulong jarg2, uint jarg3, ulong jarg4, uint jarg5);

		// Token: 0x0600031F RID: 799
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessageSendListener_OnChatRoomMessageSendFailure___")]
		public static extern void IChatRoomMessageSendListener_OnChatRoomMessageSendFailure(HandleRef jarg1, ulong jarg2, uint jarg3, int jarg4);

		// Token: 0x06000320 RID: 800
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IChatRoomMessageSendListener___")]
		public static extern IntPtr new_IChatRoomMessageSendListener();

		// Token: 0x06000321 RID: 801
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IChatRoomMessageSendListener___")]
		public static extern void delete_IChatRoomMessageSendListener(HandleRef jarg1);

		// Token: 0x06000322 RID: 802
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessageSendListener_director_connect___")]
		public static extern void IChatRoomMessageSendListener_director_connect(HandleRef jarg1, IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_0 delegate0, IChatRoomMessageSendListener.SwigDelegateIChatRoomMessageSendListener_1 delegate1);

		// Token: 0x06000323 RID: 803
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesListener_OnChatRoomMessagesReceived___")]
		public static extern void IChatRoomMessagesListener_OnChatRoomMessagesReceived(HandleRef jarg1, ulong jarg2, uint jarg3, uint jarg4);

		// Token: 0x06000324 RID: 804
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IChatRoomMessagesListener___")]
		public static extern IntPtr new_IChatRoomMessagesListener();

		// Token: 0x06000325 RID: 805
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IChatRoomMessagesListener___")]
		public static extern void delete_IChatRoomMessagesListener(HandleRef jarg1);

		// Token: 0x06000326 RID: 806
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesListener_director_connect___")]
		public static extern void IChatRoomMessagesListener_director_connect(HandleRef jarg1, IChatRoomMessagesListener.SwigDelegateIChatRoomMessagesListener_0 delegate0);

		// Token: 0x06000327 RID: 807
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesRetrieveListener_OnChatRoomMessagesRetrieveSuccess___")]
		public static extern void IChatRoomMessagesRetrieveListener_OnChatRoomMessagesRetrieveSuccess(HandleRef jarg1, ulong jarg2, uint jarg3, uint jarg4);

		// Token: 0x06000328 RID: 808
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesRetrieveListener_OnChatRoomMessagesRetrieveFailure___")]
		public static extern void IChatRoomMessagesRetrieveListener_OnChatRoomMessagesRetrieveFailure(HandleRef jarg1, ulong jarg2, int jarg3);

		// Token: 0x06000329 RID: 809
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_IChatRoomMessagesRetrieveListener___")]
		public static extern IntPtr new_IChatRoomMessagesRetrieveListener();

		// Token: 0x0600032A RID: 810
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IChatRoomMessagesRetrieveListener___")]
		public static extern void delete_IChatRoomMessagesRetrieveListener(HandleRef jarg1);

		// Token: 0x0600032B RID: 811
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesRetrieveListener_director_connect___")]
		public static extern void IChatRoomMessagesRetrieveListener_director_connect(HandleRef jarg1, IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_0 delegate0, IChatRoomMessagesRetrieveListener.SwigDelegateIChatRoomMessagesRetrieveListener_1 delegate1);

		// Token: 0x0600032C RID: 812
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_IChat___")]
		public static extern void delete_IChat(HandleRef jarg1);

		// Token: 0x0600032D RID: 813
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_RequestChatRoomWithUser__SWIG_0___")]
		public static extern void IChat_RequestChatRoomWithUser__SWIG_0(HandleRef jarg1, HandleRef jarg2, HandleRef jarg3);

		// Token: 0x0600032E RID: 814
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_RequestChatRoomWithUser__SWIG_1___")]
		public static extern void IChat_RequestChatRoomWithUser__SWIG_1(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x0600032F RID: 815
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_RequestChatRoomMessages__SWIG_0___")]
		public static extern void IChat_RequestChatRoomMessages__SWIG_0(HandleRef jarg1, ulong jarg2, uint jarg3, ulong jarg4, HandleRef jarg5);

		// Token: 0x06000330 RID: 816
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_RequestChatRoomMessages__SWIG_1___")]
		public static extern void IChat_RequestChatRoomMessages__SWIG_1(HandleRef jarg1, ulong jarg2, uint jarg3, ulong jarg4);

		// Token: 0x06000331 RID: 817
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_RequestChatRoomMessages__SWIG_2___")]
		public static extern void IChat_RequestChatRoomMessages__SWIG_2(HandleRef jarg1, ulong jarg2, uint jarg3);

		// Token: 0x06000332 RID: 818
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_SendChatRoomMessage__SWIG_0___")]
		public static extern uint IChat_SendChatRoomMessage__SWIG_0(HandleRef jarg1, ulong jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, HandleRef jarg4);

		// Token: 0x06000333 RID: 819
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_SendChatRoomMessage__SWIG_1___")]
		public static extern uint IChat_SendChatRoomMessage__SWIG_1(HandleRef jarg1, ulong jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000334 RID: 820
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_GetChatRoomMessageByIndex___")]
		public static extern uint IChat_GetChatRoomMessageByIndex(HandleRef jarg1, uint jarg2, ref ulong jarg3, ref int jarg4, HandleRef jarg5, ref uint jarg6, byte[] jarg7, uint jarg8);

		// Token: 0x06000335 RID: 821
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_GetChatRoomMemberCount___")]
		public static extern uint IChat_GetChatRoomMemberCount(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000336 RID: 822
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_GetChatRoomMemberUserIDByIndex___")]
		public static extern IntPtr IChat_GetChatRoomMemberUserIDByIndex(HandleRef jarg1, ulong jarg2, uint jarg3);

		// Token: 0x06000337 RID: 823
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_GetChatRoomUnreadMessageCount___")]
		public static extern uint IChat_GetChatRoomUnreadMessageCount(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000338 RID: 824
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChat_MarkChatRoomAsRead___")]
		public static extern void IChat_MarkChatRoomAsRead(HandleRef jarg1, ulong jarg2);

		// Token: 0x06000339 RID: 825
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetryEventSendListener_OnTelemetryEventSendSuccess___")]
		public static extern void ITelemetryEventSendListener_OnTelemetryEventSendSuccess(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3);

		// Token: 0x0600033A RID: 826
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetryEventSendListener_OnTelemetryEventSendFailure___")]
		public static extern void ITelemetryEventSendListener_OnTelemetryEventSendFailure(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, uint jarg3, int jarg4);

		// Token: 0x0600033B RID: 827
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_ITelemetryEventSendListener___")]
		public static extern IntPtr new_ITelemetryEventSendListener();

		// Token: 0x0600033C RID: 828
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ITelemetryEventSendListener___")]
		public static extern void delete_ITelemetryEventSendListener(HandleRef jarg1);

		// Token: 0x0600033D RID: 829
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetryEventSendListener_director_connect___")]
		public static extern void ITelemetryEventSendListener_director_connect(HandleRef jarg1, ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_0 delegate0, ITelemetryEventSendListener.SwigDelegateITelemetryEventSendListener_1 delegate1);

		// Token: 0x0600033E RID: 830
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_ITelemetry___")]
		public static extern void delete_ITelemetry(HandleRef jarg1);

		// Token: 0x0600033F RID: 831
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_AddStringParam___")]
		public static extern void ITelemetry_AddStringParam(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x06000340 RID: 832
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_AddIntParam___")]
		public static extern void ITelemetry_AddIntParam(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, int jarg3);

		// Token: 0x06000341 RID: 833
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_AddFloatParam___")]
		public static extern void ITelemetry_AddFloatParam(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, double jarg3);

		// Token: 0x06000342 RID: 834
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_AddBoolParam___")]
		public static extern void ITelemetry_AddBoolParam(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, bool jarg3);

		// Token: 0x06000343 RID: 835
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_AddObjectParam___")]
		public static extern void ITelemetry_AddObjectParam(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000344 RID: 836
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_AddArrayParam___")]
		public static extern void ITelemetry_AddArrayParam(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000345 RID: 837
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_CloseParam___")]
		public static extern void ITelemetry_CloseParam(HandleRef jarg1);

		// Token: 0x06000346 RID: 838
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_ClearParams___")]
		public static extern void ITelemetry_ClearParams(HandleRef jarg1);

		// Token: 0x06000347 RID: 839
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_SetSamplingClass___")]
		public static extern void ITelemetry_SetSamplingClass(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x06000348 RID: 840
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_SendTelemetryEvent__SWIG_0___")]
		public static extern uint ITelemetry_SendTelemetryEvent__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x06000349 RID: 841
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_SendTelemetryEvent__SWIG_1___")]
		public static extern uint ITelemetry_SendTelemetryEvent__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600034A RID: 842
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_SendAnonymousTelemetryEvent__SWIG_0___")]
		public static extern uint ITelemetry_SendAnonymousTelemetryEvent__SWIG_0(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, HandleRef jarg3);

		// Token: 0x0600034B RID: 843
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_SendAnonymousTelemetryEvent__SWIG_1___")]
		public static extern uint ITelemetry_SendAnonymousTelemetryEvent__SWIG_1(HandleRef jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x0600034C RID: 844
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_GetVisitID___")]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)]
		public static extern string ITelemetry_GetVisitID(HandleRef jarg1);

		// Token: 0x0600034D RID: 845
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_GetVisitIDCopy___")]
		public static extern void ITelemetry_GetVisitIDCopy(HandleRef jarg1, byte[] jarg2, uint jarg3);

		// Token: 0x0600034E RID: 846
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetry_ResetVisitID___")]
		public static extern void ITelemetry_ResetVisitID(HandleRef jarg1);

		// Token: 0x0600034F RID: 847
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_UNASSIGNED_VALUE_get___")]
		public static extern ulong GalaxyID_UNASSIGNED_VALUE_get();

		// Token: 0x06000350 RID: 848
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_FromRealID___")]
		public static extern IntPtr GalaxyID_FromRealID(int jarg1, ulong jarg2);

		// Token: 0x06000351 RID: 849
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyID__SWIG_0___")]
		public static extern IntPtr new_GalaxyID__SWIG_0();

		// Token: 0x06000352 RID: 850
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyID__SWIG_1___")]
		public static extern IntPtr new_GalaxyID__SWIG_1(ulong jarg1);

		// Token: 0x06000353 RID: 851
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_GalaxyID__SWIG_2___")]
		public static extern IntPtr new_GalaxyID__SWIG_2(HandleRef jarg1);

		// Token: 0x06000354 RID: 852
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_operator_assign___")]
		public static extern IntPtr GalaxyID_operator_assign(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000355 RID: 853
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_operator_less___")]
		public static extern bool GalaxyID_operator_less(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000356 RID: 854
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_operator_equals___")]
		public static extern bool GalaxyID_operator_equals(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000357 RID: 855
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_operator_not_equals___")]
		public static extern bool GalaxyID_operator_not_equals(HandleRef jarg1, HandleRef jarg2);

		// Token: 0x06000358 RID: 856
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_IsValid___")]
		public static extern bool GalaxyID_IsValid(HandleRef jarg1);

		// Token: 0x06000359 RID: 857
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_ToUint64___")]
		public static extern ulong GalaxyID_ToUint64(HandleRef jarg1);

		// Token: 0x0600035A RID: 858
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_GetRealID___")]
		public static extern ulong GalaxyID_GetRealID(HandleRef jarg1);

		// Token: 0x0600035B RID: 859
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyID_GetIDType___")]
		public static extern int GalaxyID_GetIDType(HandleRef jarg1);

		// Token: 0x0600035C RID: 860
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GalaxyID___")]
		public static extern void delete_GalaxyID(HandleRef jarg1);

		// Token: 0x0600035D RID: 861
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalOverlayVisibilityChangeListener___")]
		public static extern void delete_GlobalOverlayVisibilityChangeListener(HandleRef jarg1);

		// Token: 0x0600035E RID: 862
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalOverlayInitializationStateChangeListener___")]
		public static extern void delete_GlobalOverlayInitializationStateChangeListener(HandleRef jarg1);

		// Token: 0x0600035F RID: 863
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalGogServicesConnectionStateListener___")]
		public static extern void delete_GlobalGogServicesConnectionStateListener(HandleRef jarg1);

		// Token: 0x06000360 RID: 864
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalGogServicesConnectionStateListener___")]
		public static extern void delete_GameServerGlobalGogServicesConnectionStateListener(HandleRef jarg1);

		// Token: 0x06000361 RID: 865
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalAuthListener___")]
		public static extern void delete_GlobalAuthListener(HandleRef jarg1);

		// Token: 0x06000362 RID: 866
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalAuthListener___")]
		public static extern void delete_GameServerGlobalAuthListener(HandleRef jarg1);

		// Token: 0x06000363 RID: 867
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalOtherSessionStartListener___")]
		public static extern void delete_GlobalOtherSessionStartListener(HandleRef jarg1);

		// Token: 0x06000364 RID: 868
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalOperationalStateChangeListener___")]
		public static extern void delete_GlobalOperationalStateChangeListener(HandleRef jarg1);

		// Token: 0x06000365 RID: 869
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalOperationalStateChangeListener___")]
		public static extern void delete_GameServerGlobalOperationalStateChangeListener(HandleRef jarg1);

		// Token: 0x06000366 RID: 870
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalUserDataListener___")]
		public static extern void delete_GlobalUserDataListener(HandleRef jarg1);

		// Token: 0x06000367 RID: 871
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalSpecificUserDataListener___")]
		public static extern void delete_GlobalSpecificUserDataListener(HandleRef jarg1);

		// Token: 0x06000368 RID: 872
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalSpecificUserDataListener___")]
		public static extern void delete_GameServerGlobalSpecificUserDataListener(HandleRef jarg1);

		// Token: 0x06000369 RID: 873
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalEncryptedAppTicketListener___")]
		public static extern void delete_GlobalEncryptedAppTicketListener(HandleRef jarg1);

		// Token: 0x0600036A RID: 874
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalEncryptedAppTicketListener___")]
		public static extern void delete_GameServerGlobalEncryptedAppTicketListener(HandleRef jarg1);

		// Token: 0x0600036B RID: 875
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalAccessTokenListener___")]
		public static extern void delete_GlobalAccessTokenListener(HandleRef jarg1);

		// Token: 0x0600036C RID: 876
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalAccessTokenListener___")]
		public static extern void delete_GameServerGlobalAccessTokenListener(HandleRef jarg1);

		// Token: 0x0600036D RID: 877
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyListListener___")]
		public static extern void delete_GlobalLobbyListListener(HandleRef jarg1);

		// Token: 0x0600036E RID: 878
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyCreatedListener___")]
		public static extern void delete_GlobalLobbyCreatedListener(HandleRef jarg1);

		// Token: 0x0600036F RID: 879
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyCreatedListener___")]
		public static extern void delete_GameServerGlobalLobbyCreatedListener(HandleRef jarg1);

		// Token: 0x06000370 RID: 880
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyEnteredListener___")]
		public static extern void delete_GlobalLobbyEnteredListener(HandleRef jarg1);

		// Token: 0x06000371 RID: 881
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyEnteredListener___")]
		public static extern void delete_GameServerGlobalLobbyEnteredListener(HandleRef jarg1);

		// Token: 0x06000372 RID: 882
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyLeftListener___")]
		public static extern void delete_GlobalLobbyLeftListener(HandleRef jarg1);

		// Token: 0x06000373 RID: 883
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyLeftListener___")]
		public static extern void delete_GameServerGlobalLobbyLeftListener(HandleRef jarg1);

		// Token: 0x06000374 RID: 884
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyDataListener___")]
		public static extern void delete_GlobalLobbyDataListener(HandleRef jarg1);

		// Token: 0x06000375 RID: 885
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyDataListener___")]
		public static extern void delete_GameServerGlobalLobbyDataListener(HandleRef jarg1);

		// Token: 0x06000376 RID: 886
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyDataUpdateListener___")]
		public static extern void delete_GlobalLobbyDataUpdateListener(HandleRef jarg1);

		// Token: 0x06000377 RID: 887
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyDataUpdateListener___")]
		public static extern void delete_GameServerGlobalLobbyDataUpdateListener(HandleRef jarg1);

		// Token: 0x06000378 RID: 888
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyMemberDataUpdateListener___")]
		public static extern void delete_GlobalLobbyMemberDataUpdateListener(HandleRef jarg1);

		// Token: 0x06000379 RID: 889
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyMemberDataUpdateListener___")]
		public static extern void delete_GameServerGlobalLobbyMemberDataUpdateListener(HandleRef jarg1);

		// Token: 0x0600037A RID: 890
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyDataRetrieveListener___")]
		public static extern void delete_GlobalLobbyDataRetrieveListener(HandleRef jarg1);

		// Token: 0x0600037B RID: 891
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyDataRetrieveListener___")]
		public static extern void delete_GameServerGlobalLobbyDataRetrieveListener(HandleRef jarg1);

		// Token: 0x0600037C RID: 892
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyMemberStateListener___")]
		public static extern void delete_GlobalLobbyMemberStateListener(HandleRef jarg1);

		// Token: 0x0600037D RID: 893
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyMemberStateListener___")]
		public static extern void delete_GameServerGlobalLobbyMemberStateListener(HandleRef jarg1);

		// Token: 0x0600037E RID: 894
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyOwnerChangeListener___")]
		public static extern void delete_GlobalLobbyOwnerChangeListener(HandleRef jarg1);

		// Token: 0x0600037F RID: 895
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLobbyMessageListener___")]
		public static extern void delete_GlobalLobbyMessageListener(HandleRef jarg1);

		// Token: 0x06000380 RID: 896
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalLobbyMessageListener___")]
		public static extern void delete_GameServerGlobalLobbyMessageListener(HandleRef jarg1);

		// Token: 0x06000381 RID: 897
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalUserStatsAndAchievementsRetrieveListener___")]
		public static extern void delete_GlobalUserStatsAndAchievementsRetrieveListener(HandleRef jarg1);

		// Token: 0x06000382 RID: 898
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalStatsAndAchievementsStoreListener___")]
		public static extern void delete_GlobalStatsAndAchievementsStoreListener(HandleRef jarg1);

		// Token: 0x06000383 RID: 899
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalAchievementChangeListener___")]
		public static extern void delete_GlobalAchievementChangeListener(HandleRef jarg1);

		// Token: 0x06000384 RID: 900
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLeaderboardsRetrieveListener___")]
		public static extern void delete_GlobalLeaderboardsRetrieveListener(HandleRef jarg1);

		// Token: 0x06000385 RID: 901
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLeaderboardEntriesRetrieveListener___")]
		public static extern void delete_GlobalLeaderboardEntriesRetrieveListener(HandleRef jarg1);

		// Token: 0x06000386 RID: 902
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLeaderboardScoreUpdateListener___")]
		public static extern void delete_GlobalLeaderboardScoreUpdateListener(HandleRef jarg1);

		// Token: 0x06000387 RID: 903
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalLeaderboardRetrieveListener___")]
		public static extern void delete_GlobalLeaderboardRetrieveListener(HandleRef jarg1);

		// Token: 0x06000388 RID: 904
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalUserTimePlayedRetrieveListener___")]
		public static extern void delete_GlobalUserTimePlayedRetrieveListener(HandleRef jarg1);

		// Token: 0x06000389 RID: 905
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFileShareListener___")]
		public static extern void delete_GlobalFileShareListener(HandleRef jarg1);

		// Token: 0x0600038A RID: 906
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalSharedFileDownloadListener___")]
		public static extern void delete_GlobalSharedFileDownloadListener(HandleRef jarg1);

		// Token: 0x0600038B RID: 907
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalConnectionOpenListener___")]
		public static extern void delete_GlobalConnectionOpenListener(HandleRef jarg1);

		// Token: 0x0600038C RID: 908
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalConnectionCloseListener___")]
		public static extern void delete_GlobalConnectionCloseListener(HandleRef jarg1);

		// Token: 0x0600038D RID: 909
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalConnectionDataListener___")]
		public static extern void delete_GlobalConnectionDataListener(HandleRef jarg1);

		// Token: 0x0600038E RID: 910
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalNetworkingListener___")]
		public static extern void delete_GlobalNetworkingListener(HandleRef jarg1);

		// Token: 0x0600038F RID: 911
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalNetworkingListener___")]
		public static extern void delete_GameServerGlobalNetworkingListener(HandleRef jarg1);

		// Token: 0x06000390 RID: 912
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalNatTypeDetectionListener___")]
		public static extern void delete_GlobalNatTypeDetectionListener(HandleRef jarg1);

		// Token: 0x06000391 RID: 913
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalPersonaDataChangedListener___")]
		public static extern void delete_GlobalPersonaDataChangedListener(HandleRef jarg1);

		// Token: 0x06000392 RID: 914
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendListListener___")]
		public static extern void delete_GlobalFriendListListener(HandleRef jarg1);

		// Token: 0x06000393 RID: 915
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendInvitationSendListener___")]
		public static extern void delete_GlobalFriendInvitationSendListener(HandleRef jarg1);

		// Token: 0x06000394 RID: 916
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendInvitationListRetrieveListener___")]
		public static extern void delete_GlobalFriendInvitationListRetrieveListener(HandleRef jarg1);

		// Token: 0x06000395 RID: 917
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalSentFriendInvitationListRetrieveListener___")]
		public static extern void delete_GlobalSentFriendInvitationListRetrieveListener(HandleRef jarg1);

		// Token: 0x06000396 RID: 918
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendInvitationListener___")]
		public static extern void delete_GlobalFriendInvitationListener(HandleRef jarg1);

		// Token: 0x06000397 RID: 919
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendInvitationRespondToListener___")]
		public static extern void delete_GlobalFriendInvitationRespondToListener(HandleRef jarg1);

		// Token: 0x06000398 RID: 920
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendAddListener___")]
		public static extern void delete_GlobalFriendAddListener(HandleRef jarg1);

		// Token: 0x06000399 RID: 921
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalFriendDeleteListener___")]
		public static extern void delete_GlobalFriendDeleteListener(HandleRef jarg1);

		// Token: 0x0600039A RID: 922
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalRichPresenceChangeListener___")]
		public static extern void delete_GlobalRichPresenceChangeListener(HandleRef jarg1);

		// Token: 0x0600039B RID: 923
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalRichPresenceListener___")]
		public static extern void delete_GlobalRichPresenceListener(HandleRef jarg1);

		// Token: 0x0600039C RID: 924
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalGameJoinRequestedListener___")]
		public static extern void delete_GlobalGameJoinRequestedListener(HandleRef jarg1);

		// Token: 0x0600039D RID: 925
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalGameInvitationReceivedListener___")]
		public static extern void delete_GlobalGameInvitationReceivedListener(HandleRef jarg1);

		// Token: 0x0600039E RID: 926
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalSendInvitationListener___")]
		public static extern void delete_GlobalSendInvitationListener(HandleRef jarg1);

		// Token: 0x0600039F RID: 927
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalUserFindListener___")]
		public static extern void delete_GlobalUserFindListener(HandleRef jarg1);

		// Token: 0x060003A0 RID: 928
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalNotificationListener___")]
		public static extern void delete_GlobalNotificationListener(HandleRef jarg1);

		// Token: 0x060003A1 RID: 929
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalChatRoomWithUserRetrieveListener___")]
		public static extern void delete_GlobalChatRoomWithUserRetrieveListener(HandleRef jarg1);

		// Token: 0x060003A2 RID: 930
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalChatRoomMessagesRetrieveListener___")]
		public static extern void delete_GlobalChatRoomMessagesRetrieveListener(HandleRef jarg1);

		// Token: 0x060003A3 RID: 931
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalChatRoomMessageSendListener___")]
		public static extern void delete_GlobalChatRoomMessageSendListener(HandleRef jarg1);

		// Token: 0x060003A4 RID: 932
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalChatRoomMessagesListener___")]
		public static extern void delete_GlobalChatRoomMessagesListener(HandleRef jarg1);

		// Token: 0x060003A5 RID: 933
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalTelemetryEventSendListener___")]
		public static extern void delete_GlobalTelemetryEventSendListener(HandleRef jarg1);

		// Token: 0x060003A6 RID: 934
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalTelemetryEventSendListener___")]
		public static extern void delete_GameServerGlobalTelemetryEventSendListener(HandleRef jarg1);

		// Token: 0x060003A7 RID: 935
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalUserInformationRetrieveListener___")]
		public static extern void delete_GlobalUserInformationRetrieveListener(HandleRef jarg1);

		// Token: 0x060003A8 RID: 936
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalUserInformationRetrieveListener___")]
		public static extern void delete_GameServerGlobalUserInformationRetrieveListener(HandleRef jarg1);

		// Token: 0x060003A9 RID: 937
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GlobalRichPresenceRetrieveListener___")]
		public static extern void delete_GlobalRichPresenceRetrieveListener(HandleRef jarg1);

		// Token: 0x060003AA RID: 938
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_GameServerGlobalRichPresenceRetrieveListener___")]
		public static extern void delete_GameServerGlobalRichPresenceRetrieveListener(HandleRef jarg1);

		// Token: 0x060003AB RID: 939
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_InitParams__SWIG_0___")]
		public static extern IntPtr new_InitParams__SWIG_0([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, bool jarg5, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg6);

		// Token: 0x060003AC RID: 940
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_InitParams__SWIG_1___")]
		public static extern IntPtr new_InitParams__SWIG_1([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4, bool jarg5);

		// Token: 0x060003AD RID: 941
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_InitParams__SWIG_2___")]
		public static extern IntPtr new_InitParams__SWIG_2([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg4);

		// Token: 0x060003AE RID: 942
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_InitParams__SWIG_3___")]
		public static extern IntPtr new_InitParams__SWIG_3([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg3);

		// Token: 0x060003AF RID: 943
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_new_InitParams__SWIG_4___")]
		public static extern IntPtr new_InitParams__SWIG_4([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg1, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string jarg2);

		// Token: 0x060003B0 RID: 944
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_clientID_set___")]
		public static extern void InitParams_clientID_set(HandleRef jarg1, string jarg2);

		// Token: 0x060003B1 RID: 945
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_clientID_get___")]
		public static extern string InitParams_clientID_get(HandleRef jarg1);

		// Token: 0x060003B2 RID: 946
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_clientSecret_set___")]
		public static extern void InitParams_clientSecret_set(HandleRef jarg1, string jarg2);

		// Token: 0x060003B3 RID: 947
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_clientSecret_get___")]
		public static extern string InitParams_clientSecret_get(HandleRef jarg1);

		// Token: 0x060003B4 RID: 948
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_configFilePath_set___")]
		public static extern void InitParams_configFilePath_set(HandleRef jarg1, string jarg2);

		// Token: 0x060003B5 RID: 949
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_configFilePath_get___")]
		public static extern string InitParams_configFilePath_get(HandleRef jarg1);

		// Token: 0x060003B6 RID: 950
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_storagePath_set___")]
		public static extern void InitParams_storagePath_set(HandleRef jarg1, string jarg2);

		// Token: 0x060003B7 RID: 951
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_storagePath_get___")]
		public static extern string InitParams_storagePath_get(HandleRef jarg1);

		// Token: 0x060003B8 RID: 952
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_galaxyModulePath_set___")]
		public static extern void InitParams_galaxyModulePath_set(HandleRef jarg1, string jarg2);

		// Token: 0x060003B9 RID: 953
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_galaxyModulePath_get___")]
		public static extern string InitParams_galaxyModulePath_get(HandleRef jarg1);

		// Token: 0x060003BA RID: 954
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_loadModule_set___")]
		public static extern void InitParams_loadModule_set(HandleRef jarg1, bool jarg2);

		// Token: 0x060003BB RID: 955
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitParams_loadModule_get___")]
		public static extern bool InitParams_loadModule_get(HandleRef jarg1);

		// Token: 0x060003BC RID: 956
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_delete_InitParams___")]
		public static extern void delete_InitParams(HandleRef jarg1);

		// Token: 0x060003BD RID: 957
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Init___")]
		public static extern void Init(HandleRef jarg1);

		// Token: 0x060003BE RID: 958
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_InitGameServer___")]
		public static extern void InitGameServer(HandleRef jarg1);

		// Token: 0x060003BF RID: 959
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Shutdown___")]
		public static extern void Shutdown(bool jarg1);

		// Token: 0x060003C0 RID: 960
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_User___")]
		public static extern IntPtr User();

		// Token: 0x060003C1 RID: 961
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Friends___")]
		public static extern IntPtr Friends();

		// Token: 0x060003C2 RID: 962
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Chat___")]
		public static extern IntPtr Chat();

		// Token: 0x060003C3 RID: 963
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Matchmaking___")]
		public static extern IntPtr Matchmaking();

		// Token: 0x060003C4 RID: 964
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Networking___")]
		public static extern IntPtr Networking();

		// Token: 0x060003C5 RID: 965
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Stats___")]
		public static extern IntPtr Stats();

		// Token: 0x060003C6 RID: 966
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Utils___")]
		public static extern IntPtr Utils();

		// Token: 0x060003C7 RID: 967
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Apps___")]
		public static extern IntPtr Apps();

		// Token: 0x060003C8 RID: 968
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Storage___")]
		public static extern IntPtr Storage();

		// Token: 0x060003C9 RID: 969
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_CustomNetworking___")]
		public static extern IntPtr CustomNetworking();

		// Token: 0x060003CA RID: 970
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Logger___")]
		public static extern IntPtr Logger();

		// Token: 0x060003CB RID: 971
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_Telemetry___")]
		public static extern IntPtr Telemetry();

		// Token: 0x060003CC RID: 972
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ProcessData___")]
		public static extern void ProcessData();

		// Token: 0x060003CD RID: 973
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ShutdownGameServer___")]
		public static extern void ShutdownGameServer();

		// Token: 0x060003CE RID: 974
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerUser___")]
		public static extern IntPtr GameServerUser();

		// Token: 0x060003CF RID: 975
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerMatchmaking___")]
		public static extern IntPtr GameServerMatchmaking();

		// Token: 0x060003D0 RID: 976
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerNetworking___")]
		public static extern IntPtr GameServerNetworking();

		// Token: 0x060003D1 RID: 977
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerUtils___")]
		public static extern IntPtr GameServerUtils();

		// Token: 0x060003D2 RID: 978
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerTelemetry___")]
		public static extern IntPtr GameServerTelemetry();

		// Token: 0x060003D3 RID: 979
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerLogger___")]
		public static extern IntPtr GameServerLogger();

		// Token: 0x060003D4 RID: 980
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ProcessGameServerData___")]
		public static extern void ProcessGameServerData();

		// Token: 0x060003D5 RID: 981
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUnauthorizedAccessError_SWIGUpcast___")]
		public static extern IntPtr IUnauthorizedAccessError_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003D6 RID: 982
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IInvalidArgumentError_SWIGUpcast___")]
		public static extern IntPtr IInvalidArgumentError_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003D7 RID: 983
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IInvalidStateError_SWIGUpcast___")]
		public static extern IntPtr IInvalidStateError_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003D8 RID: 984
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRuntimeError_SWIGUpcast___")]
		public static extern IntPtr IRuntimeError_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003D9 RID: 985
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOverlayVisibilityChange_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerOverlayVisibilityChange_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003DA RID: 986
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOverlayInitializationStateChange_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerOverlayInitializationStateChange_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003DB RID: 987
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerGogServicesConnectionState_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerGogServicesConnectionState_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003DC RID: 988
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOperationalStateChange_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerOperationalStateChange_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003DD RID: 989
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerAuth_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerAuth_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003DE RID: 990
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerOtherSessionStart_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerOtherSessionStart_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003DF RID: 991
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserData_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerUserData_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E0 RID: 992
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSpecificUserData_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerSpecificUserData_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E1 RID: 993
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerEncryptedAppTicket_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerEncryptedAppTicket_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E2 RID: 994
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerAccessToken_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerAccessToken_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E3 RID: 995
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyList_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyList_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E4 RID: 996
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyCreated_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyCreated_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E5 RID: 997
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyEntered_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyEntered_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E6 RID: 998
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyLeft_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyLeft_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E7 RID: 999
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyData_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyData_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E8 RID: 1000
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyDataUpdate_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyDataUpdate_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003E9 RID: 1001
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyMemberDataUpdate_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyMemberDataUpdate_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003EA RID: 1002
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyDataRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyDataRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003EB RID: 1003
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyMemberState_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyMemberState_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003EC RID: 1004
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyOwnerChange_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyOwnerChange_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003ED RID: 1005
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLobbyMessage_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLobbyMessage_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003EE RID: 1006
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003EF RID: 1007
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerStatsAndAchievementsStore_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerStatsAndAchievementsStore_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F0 RID: 1008
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerAchievementChange_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerAchievementChange_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F1 RID: 1009
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardsRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLeaderboardsRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F2 RID: 1010
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardEntriesRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLeaderboardEntriesRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F3 RID: 1011
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardScoreUpdate_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLeaderboardScoreUpdate_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F4 RID: 1012
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerLeaderboardRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerLeaderboardRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F5 RID: 1013
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserTimePlayedRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerUserTimePlayedRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F6 RID: 1014
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFileShare_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFileShare_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F7 RID: 1015
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSharedFileDownload_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerSharedFileDownload_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F8 RID: 1016
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerConnectionOpen_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerConnectionOpen_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003F9 RID: 1017
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerConnectionClose_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerConnectionClose_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003FA RID: 1018
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerConnectionData_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerConnectionData_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003FB RID: 1019
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerNetworking_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerNetworking_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003FC RID: 1020
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerNatTypeDetection_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerNatTypeDetection_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003FD RID: 1021
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerPersonaDataChanged_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerPersonaDataChanged_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003FE RID: 1022
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserInformationRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerUserInformationRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x060003FF RID: 1023
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendList_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendList_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000400 RID: 1024
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitationSend_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendInvitationSend_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000401 RID: 1025
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitationListRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendInvitationListRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000402 RID: 1026
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSentFriendInvitationListRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerSentFriendInvitationListRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000403 RID: 1027
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitation_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendInvitation_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000404 RID: 1028
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendInvitationRespondTo_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendInvitationRespondTo_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000405 RID: 1029
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendAdd_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendAdd_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000406 RID: 1030
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerFriendDelete_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerFriendDelete_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000407 RID: 1031
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerRichPresenceChange_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerRichPresenceChange_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000408 RID: 1032
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerRichPresence_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerRichPresence_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000409 RID: 1033
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerRichPresenceRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerRichPresenceRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600040A RID: 1034
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerGameJoinRequested_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerGameJoinRequested_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600040B RID: 1035
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerGameInvitationReceived_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerGameInvitationReceived_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600040C RID: 1036
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerSendInvitation_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerSendInvitation_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600040D RID: 1037
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerNotification_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerNotification_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600040E RID: 1038
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerUserFind_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerUserFind_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600040F RID: 1039
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomWithUserRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerChatRoomWithUserRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000410 RID: 1040
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomMessagesRetrieve_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerChatRoomMessagesRetrieve_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000411 RID: 1041
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomMessageSend_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerChatRoomMessageSend_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000412 RID: 1042
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerChatRoomMessages_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerChatRoomMessages_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000413 RID: 1043
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GalaxyTypeAwareListenerTelemetryEventSend_SWIGUpcast___")]
		public static extern IntPtr GalaxyTypeAwareListenerTelemetryEventSend_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000414 RID: 1044
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOverlayVisibilityChangeListener_SWIGUpcast___")]
		public static extern IntPtr IOverlayVisibilityChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000415 RID: 1045
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOverlayInitializationStateChangeListener_SWIGUpcast___")]
		public static extern IntPtr IOverlayInitializationStateChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000416 RID: 1046
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INotificationListener_SWIGUpcast___")]
		public static extern IntPtr INotificationListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000417 RID: 1047
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGogServicesConnectionStateListener_SWIGUpcast___")]
		public static extern IntPtr IGogServicesConnectionStateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000418 RID: 1048
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAuthListener_SWIGUpcast___")]
		public static extern IntPtr IAuthListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000419 RID: 1049
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOtherSessionStartListener_SWIGUpcast___")]
		public static extern IntPtr IOtherSessionStartListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600041A RID: 1050
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IOperationalStateChangeListener_SWIGUpcast___")]
		public static extern IntPtr IOperationalStateChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600041B RID: 1051
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserDataListener_SWIGUpcast___")]
		public static extern IntPtr IUserDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600041C RID: 1052
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISpecificUserDataListener_SWIGUpcast___")]
		public static extern IntPtr ISpecificUserDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600041D RID: 1053
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IEncryptedAppTicketListener_SWIGUpcast___")]
		public static extern IntPtr IEncryptedAppTicketListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600041E RID: 1054
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAccessTokenListener_SWIGUpcast___")]
		public static extern IntPtr IAccessTokenListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600041F RID: 1055
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IPersonaDataChangedListener_SWIGUpcast___")]
		public static extern IntPtr IPersonaDataChangedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000420 RID: 1056
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserInformationRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IUserInformationRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000421 RID: 1057
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendListListener_SWIGUpcast___")]
		public static extern IntPtr IFriendListListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000422 RID: 1058
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationSendListener_SWIGUpcast___")]
		public static extern IntPtr IFriendInvitationSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000423 RID: 1059
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IFriendInvitationListRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000424 RID: 1060
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISentFriendInvitationListRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr ISentFriendInvitationListRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000425 RID: 1061
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationListener_SWIGUpcast___")]
		public static extern IntPtr IFriendInvitationListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000426 RID: 1062
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendInvitationRespondToListener_SWIGUpcast___")]
		public static extern IntPtr IFriendInvitationRespondToListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000427 RID: 1063
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendAddListener_SWIGUpcast___")]
		public static extern IntPtr IFriendAddListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000428 RID: 1064
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFriendDeleteListener_SWIGUpcast___")]
		public static extern IntPtr IFriendDeleteListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000429 RID: 1065
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceChangeListener_SWIGUpcast___")]
		public static extern IntPtr IRichPresenceChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600042A RID: 1066
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceListener_SWIGUpcast___")]
		public static extern IntPtr IRichPresenceListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600042B RID: 1067
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IRichPresenceRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IRichPresenceRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600042C RID: 1068
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGameJoinRequestedListener_SWIGUpcast___")]
		public static extern IntPtr IGameJoinRequestedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600042D RID: 1069
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IGameInvitationReceivedListener_SWIGUpcast___")]
		public static extern IntPtr IGameInvitationReceivedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600042E RID: 1070
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISendInvitationListener_SWIGUpcast___")]
		public static extern IntPtr ISendInvitationListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600042F RID: 1071
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserFindListener_SWIGUpcast___")]
		public static extern IntPtr IUserFindListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000430 RID: 1072
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserStatsAndAchievementsRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IUserStatsAndAchievementsRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000431 RID: 1073
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IStatsAndAchievementsStoreListener_SWIGUpcast___")]
		public static extern IntPtr IStatsAndAchievementsStoreListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000432 RID: 1074
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IAchievementChangeListener_SWIGUpcast___")]
		public static extern IntPtr IAchievementChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000433 RID: 1075
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardsRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr ILeaderboardsRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000434 RID: 1076
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardEntriesRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr ILeaderboardEntriesRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000435 RID: 1077
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardScoreUpdateListener_SWIGUpcast___")]
		public static extern IntPtr ILeaderboardScoreUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000436 RID: 1078
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILeaderboardRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr ILeaderboardRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000437 RID: 1079
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IUserTimePlayedRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IUserTimePlayedRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000438 RID: 1080
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IFileShareListener_SWIGUpcast___")]
		public static extern IntPtr IFileShareListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000439 RID: 1081
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ISharedFileDownloadListener_SWIGUpcast___")]
		public static extern IntPtr ISharedFileDownloadListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600043A RID: 1082
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionOpenListener_SWIGUpcast___")]
		public static extern IntPtr IConnectionOpenListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600043B RID: 1083
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionCloseListener_SWIGUpcast___")]
		public static extern IntPtr IConnectionCloseListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600043C RID: 1084
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IConnectionDataListener_SWIGUpcast___")]
		public static extern IntPtr IConnectionDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600043D RID: 1085
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INetworkingListener_SWIGUpcast___")]
		public static extern IntPtr INetworkingListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600043E RID: 1086
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_INatTypeDetectionListener_SWIGUpcast___")]
		public static extern IntPtr INatTypeDetectionListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600043F RID: 1087
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyListListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyListListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000440 RID: 1088
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyCreatedListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyCreatedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000441 RID: 1089
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyEnteredListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyEnteredListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000442 RID: 1090
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyLeftListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyLeftListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000443 RID: 1091
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000444 RID: 1092
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataUpdateListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyDataUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000445 RID: 1093
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberDataUpdateListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyMemberDataUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000446 RID: 1094
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyDataRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyDataRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000447 RID: 1095
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMemberStateListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyMemberStateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000448 RID: 1096
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyOwnerChangeListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyOwnerChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000449 RID: 1097
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ILobbyMessageListener_SWIGUpcast___")]
		public static extern IntPtr ILobbyMessageListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600044A RID: 1098
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomWithUserRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IChatRoomWithUserRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600044B RID: 1099
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessageSendListener_SWIGUpcast___")]
		public static extern IntPtr IChatRoomMessageSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600044C RID: 1100
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesListener_SWIGUpcast___")]
		public static extern IntPtr IChatRoomMessagesListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600044D RID: 1101
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_IChatRoomMessagesRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr IChatRoomMessagesRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600044E RID: 1102
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_ITelemetryEventSendListener_SWIGUpcast___")]
		public static extern IntPtr ITelemetryEventSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600044F RID: 1103
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalOverlayVisibilityChangeListener_SWIGUpcast___")]
		public static extern IntPtr GlobalOverlayVisibilityChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000450 RID: 1104
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalOverlayInitializationStateChangeListener_SWIGUpcast___")]
		public static extern IntPtr GlobalOverlayInitializationStateChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000451 RID: 1105
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalGogServicesConnectionStateListener_SWIGUpcast___")]
		public static extern IntPtr GlobalGogServicesConnectionStateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000452 RID: 1106
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalGogServicesConnectionStateListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalGogServicesConnectionStateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000453 RID: 1107
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalAuthListener_SWIGUpcast___")]
		public static extern IntPtr GlobalAuthListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000454 RID: 1108
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalAuthListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalAuthListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000455 RID: 1109
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalOtherSessionStartListener_SWIGUpcast___")]
		public static extern IntPtr GlobalOtherSessionStartListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000456 RID: 1110
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalOperationalStateChangeListener_SWIGUpcast___")]
		public static extern IntPtr GlobalOperationalStateChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000457 RID: 1111
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalOperationalStateChangeListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalOperationalStateChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000458 RID: 1112
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalUserDataListener_SWIGUpcast___")]
		public static extern IntPtr GlobalUserDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000459 RID: 1113
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalSpecificUserDataListener_SWIGUpcast___")]
		public static extern IntPtr GlobalSpecificUserDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600045A RID: 1114
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalSpecificUserDataListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalSpecificUserDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600045B RID: 1115
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalEncryptedAppTicketListener_SWIGUpcast___")]
		public static extern IntPtr GlobalEncryptedAppTicketListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600045C RID: 1116
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalEncryptedAppTicketListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalEncryptedAppTicketListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600045D RID: 1117
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalAccessTokenListener_SWIGUpcast___")]
		public static extern IntPtr GlobalAccessTokenListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600045E RID: 1118
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalAccessTokenListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalAccessTokenListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600045F RID: 1119
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyListListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyListListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000460 RID: 1120
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyCreatedListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyCreatedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000461 RID: 1121
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyCreatedListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyCreatedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000462 RID: 1122
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyEnteredListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyEnteredListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000463 RID: 1123
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyEnteredListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyEnteredListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000464 RID: 1124
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyLeftListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyLeftListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000465 RID: 1125
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyLeftListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyLeftListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000466 RID: 1126
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyDataListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000467 RID: 1127
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyDataListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000468 RID: 1128
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyDataUpdateListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyDataUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000469 RID: 1129
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyDataUpdateListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyDataUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600046A RID: 1130
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyMemberDataUpdateListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyMemberDataUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600046B RID: 1131
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyMemberDataUpdateListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyMemberDataUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600046C RID: 1132
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyDataRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyDataRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600046D RID: 1133
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyDataRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyDataRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600046E RID: 1134
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyMemberStateListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyMemberStateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600046F RID: 1135
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyMemberStateListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyMemberStateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000470 RID: 1136
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyOwnerChangeListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyOwnerChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000471 RID: 1137
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLobbyMessageListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLobbyMessageListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000472 RID: 1138
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalLobbyMessageListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalLobbyMessageListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000473 RID: 1139
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalUserStatsAndAchievementsRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalUserStatsAndAchievementsRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000474 RID: 1140
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalStatsAndAchievementsStoreListener_SWIGUpcast___")]
		public static extern IntPtr GlobalStatsAndAchievementsStoreListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000475 RID: 1141
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalAchievementChangeListener_SWIGUpcast___")]
		public static extern IntPtr GlobalAchievementChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000476 RID: 1142
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLeaderboardsRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLeaderboardsRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000477 RID: 1143
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLeaderboardEntriesRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLeaderboardEntriesRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000478 RID: 1144
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLeaderboardScoreUpdateListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLeaderboardScoreUpdateListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000479 RID: 1145
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalLeaderboardRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalLeaderboardRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600047A RID: 1146
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalUserTimePlayedRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalUserTimePlayedRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600047B RID: 1147
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFileShareListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFileShareListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600047C RID: 1148
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalSharedFileDownloadListener_SWIGUpcast___")]
		public static extern IntPtr GlobalSharedFileDownloadListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600047D RID: 1149
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalConnectionOpenListener_SWIGUpcast___")]
		public static extern IntPtr GlobalConnectionOpenListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600047E RID: 1150
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalConnectionCloseListener_SWIGUpcast___")]
		public static extern IntPtr GlobalConnectionCloseListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600047F RID: 1151
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalConnectionDataListener_SWIGUpcast___")]
		public static extern IntPtr GlobalConnectionDataListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000480 RID: 1152
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalNetworkingListener_SWIGUpcast___")]
		public static extern IntPtr GlobalNetworkingListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000481 RID: 1153
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalNetworkingListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalNetworkingListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000482 RID: 1154
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalNatTypeDetectionListener_SWIGUpcast___")]
		public static extern IntPtr GlobalNatTypeDetectionListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000483 RID: 1155
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalPersonaDataChangedListener_SWIGUpcast___")]
		public static extern IntPtr GlobalPersonaDataChangedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000484 RID: 1156
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendListListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendListListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000485 RID: 1157
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendInvitationSendListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendInvitationSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000486 RID: 1158
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendInvitationListRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendInvitationListRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000487 RID: 1159
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalSentFriendInvitationListRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalSentFriendInvitationListRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000488 RID: 1160
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendInvitationListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendInvitationListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000489 RID: 1161
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendInvitationRespondToListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendInvitationRespondToListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600048A RID: 1162
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendAddListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendAddListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600048B RID: 1163
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalFriendDeleteListener_SWIGUpcast___")]
		public static extern IntPtr GlobalFriendDeleteListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600048C RID: 1164
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalRichPresenceChangeListener_SWIGUpcast___")]
		public static extern IntPtr GlobalRichPresenceChangeListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600048D RID: 1165
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalRichPresenceListener_SWIGUpcast___")]
		public static extern IntPtr GlobalRichPresenceListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600048E RID: 1166
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalGameJoinRequestedListener_SWIGUpcast___")]
		public static extern IntPtr GlobalGameJoinRequestedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600048F RID: 1167
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalGameInvitationReceivedListener_SWIGUpcast___")]
		public static extern IntPtr GlobalGameInvitationReceivedListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000490 RID: 1168
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalSendInvitationListener_SWIGUpcast___")]
		public static extern IntPtr GlobalSendInvitationListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000491 RID: 1169
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalUserFindListener_SWIGUpcast___")]
		public static extern IntPtr GlobalUserFindListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000492 RID: 1170
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalNotificationListener_SWIGUpcast___")]
		public static extern IntPtr GlobalNotificationListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000493 RID: 1171
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalChatRoomWithUserRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalChatRoomWithUserRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000494 RID: 1172
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalChatRoomMessagesRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalChatRoomMessagesRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000495 RID: 1173
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalChatRoomMessageSendListener_SWIGUpcast___")]
		public static extern IntPtr GlobalChatRoomMessageSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000496 RID: 1174
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalChatRoomMessagesListener_SWIGUpcast___")]
		public static extern IntPtr GlobalChatRoomMessagesListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000497 RID: 1175
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalTelemetryEventSendListener_SWIGUpcast___")]
		public static extern IntPtr GlobalTelemetryEventSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000498 RID: 1176
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalTelemetryEventSendListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalTelemetryEventSendListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x06000499 RID: 1177
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalUserInformationRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalUserInformationRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600049A RID: 1178
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalUserInformationRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalUserInformationRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600049B RID: 1179
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GlobalRichPresenceRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GlobalRichPresenceRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x0600049C RID: 1180
		[DllImport("GalaxyCSharpGlue", EntryPoint = "CSharp_GalaxyfApi_GameServerGlobalRichPresenceRetrieveListener_SWIGUpcast___")]
		public static extern IntPtr GameServerGlobalRichPresenceRetrieveListener_SWIGUpcast(IntPtr jarg1);

		// Token: 0x04000017 RID: 23
		protected static GalaxyInstancePINVOKE.SWIGExceptionHelper swigExceptionHelper = new GalaxyInstancePINVOKE.SWIGExceptionHelper();

		// Token: 0x04000018 RID: 24
		protected static GalaxyInstancePINVOKE.SWIGStringHelper swigStringHelper = new GalaxyInstancePINVOKE.SWIGStringHelper();

		// Token: 0x02000010 RID: 16
		protected class SWIGExceptionHelper
		{
			// Token: 0x0600049D RID: 1181 RVA: 0x00002B38 File Offset: 0x00000D38
			static SWIGExceptionHelper()
			{
				GalaxyInstancePINVOKE.SWIGExceptionHelper.SWIGRegisterExceptionCallbacks_GalaxyInstance(GalaxyInstancePINVOKE.SWIGExceptionHelper.applicationDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.arithmeticDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.divideByZeroDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.indexOutOfRangeDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.invalidCastDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.invalidOperationDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ioDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.nullReferenceDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.outOfMemoryDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.overflowDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.systemDelegate);
				GalaxyInstancePINVOKE.SWIGExceptionHelper.SWIGRegisterExceptionCallbacksArgument_GalaxyInstance(GalaxyInstancePINVOKE.SWIGExceptionHelper.argumentDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.argumentNullDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.argumentOutOfRangeDelegate);
			}

			// Token: 0x0600049F RID: 1183
			[DllImport("GalaxyCSharpGlue")]
			public static extern void SWIGRegisterExceptionCallbacks_GalaxyInstance(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate applicationDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate arithmeticDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate divideByZeroDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate indexOutOfRangeDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate invalidCastDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate invalidOperationDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate ioDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate nullReferenceDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate outOfMemoryDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate overflowDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate systemExceptionDelegate);

			// Token: 0x060004A0 RID: 1184
			[DllImport("GalaxyCSharpGlue", EntryPoint = "SWIGRegisterExceptionArgumentCallbacks_GalaxyInstance")]
			public static extern void SWIGRegisterExceptionCallbacksArgument_GalaxyInstance(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate argumentDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate argumentNullDelegate, GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate argumentOutOfRangeDelegate);

			// Token: 0x060004A1 RID: 1185 RVA: 0x00002C8B File Offset: 0x00000E8B
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingApplicationException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new ApplicationException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A2 RID: 1186 RVA: 0x00002C9D File Offset: 0x00000E9D
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingArithmeticException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new ArithmeticException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A3 RID: 1187 RVA: 0x00002CAF File Offset: 0x00000EAF
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingDivideByZeroException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new DivideByZeroException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A4 RID: 1188 RVA: 0x00002CC1 File Offset: 0x00000EC1
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingIndexOutOfRangeException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new IndexOutOfRangeException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A5 RID: 1189 RVA: 0x00002CD3 File Offset: 0x00000ED3
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingInvalidCastException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new InvalidCastException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A6 RID: 1190 RVA: 0x00002CE5 File Offset: 0x00000EE5
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingInvalidOperationException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new InvalidOperationException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A7 RID: 1191 RVA: 0x00002CF7 File Offset: 0x00000EF7
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingIOException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new IOException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A8 RID: 1192 RVA: 0x00002D09 File Offset: 0x00000F09
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingNullReferenceException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new NullReferenceException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004A9 RID: 1193 RVA: 0x00002D1B File Offset: 0x00000F1B
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingOutOfMemoryException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new OutOfMemoryException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004AA RID: 1194 RVA: 0x00002D2D File Offset: 0x00000F2D
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingOverflowException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new OverflowException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004AB RID: 1195 RVA: 0x00002D3F File Offset: 0x00000F3F
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate))]
			private static void SetPendingSystemException(string message)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new SystemException(message, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004AC RID: 1196 RVA: 0x00002D51 File Offset: 0x00000F51
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate))]
			private static void SetPendingArgumentException(string message, string paramName)
			{
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new ArgumentException(message, paramName, GalaxyInstancePINVOKE.SWIGPendingException.Retrieve()));
			}

			// Token: 0x060004AD RID: 1197 RVA: 0x00002D64 File Offset: 0x00000F64
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate))]
			private static void SetPendingArgumentNullException(string message, string paramName)
			{
				Exception ex = GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				if (ex != null)
				{
					message = message + " Inner Exception: " + ex.Message;
				}
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new ArgumentNullException(paramName, message));
			}

			// Token: 0x060004AE RID: 1198 RVA: 0x00002D9C File Offset: 0x00000F9C
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate))]
			private static void SetPendingArgumentOutOfRangeException(string message, string paramName)
			{
				Exception ex = GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				if (ex != null)
				{
					message = message + " Inner Exception: " + ex.Message;
				}
				GalaxyInstancePINVOKE.SWIGPendingException.Set(new ArgumentOutOfRangeException(paramName, message));
			}

			// Token: 0x04000019 RID: 25
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate applicationDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingApplicationException);

			// Token: 0x0400001A RID: 26
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate arithmeticDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingArithmeticException);

			// Token: 0x0400001B RID: 27
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate divideByZeroDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingDivideByZeroException);

			// Token: 0x0400001C RID: 28
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate indexOutOfRangeDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingIndexOutOfRangeException);

			// Token: 0x0400001D RID: 29
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate invalidCastDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingInvalidCastException);

			// Token: 0x0400001E RID: 30
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate invalidOperationDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingInvalidOperationException);

			// Token: 0x0400001F RID: 31
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate ioDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingIOException);

			// Token: 0x04000020 RID: 32
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate nullReferenceDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingNullReferenceException);

			// Token: 0x04000021 RID: 33
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate outOfMemoryDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingOutOfMemoryException);

			// Token: 0x04000022 RID: 34
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate overflowDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingOverflowException);

			// Token: 0x04000023 RID: 35
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate systemDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingSystemException);

			// Token: 0x04000024 RID: 36
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate argumentDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingArgumentException);

			// Token: 0x04000025 RID: 37
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate argumentNullDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingArgumentNullException);

			// Token: 0x04000026 RID: 38
			private static GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate argumentOutOfRangeDelegate = new GalaxyInstancePINVOKE.SWIGExceptionHelper.ExceptionArgumentDelegate(GalaxyInstancePINVOKE.SWIGExceptionHelper.SetPendingArgumentOutOfRangeException);

			// Token: 0x02000011 RID: 17
			// (Invoke) Token: 0x060004B0 RID: 1200
			public delegate void ExceptionDelegate(string message);

			// Token: 0x02000012 RID: 18
			// (Invoke) Token: 0x060004B4 RID: 1204
			public delegate void ExceptionArgumentDelegate(string message, string paramName);
		}

		// Token: 0x02000013 RID: 19
		public class SWIGPendingException
		{
			// Token: 0x17000001 RID: 1
			// (get) Token: 0x060004B8 RID: 1208 RVA: 0x00002DDC File Offset: 0x00000FDC
			public static bool Pending
			{
				get
				{
					bool result = false;
					if (GalaxyInstancePINVOKE.SWIGPendingException.numExceptionsPending > 0 && GalaxyInstancePINVOKE.SWIGPendingException.pendingException != null)
					{
						result = true;
					}
					return result;
				}
			}

			// Token: 0x060004B9 RID: 1209 RVA: 0x00002E04 File Offset: 0x00001004
			public static void Set(Exception e)
			{
				if (GalaxyInstancePINVOKE.SWIGPendingException.pendingException != null)
				{
					throw new ApplicationException("FATAL: An earlier pending exception from unmanaged code was missed and thus not thrown (" + GalaxyInstancePINVOKE.SWIGPendingException.pendingException.ToString() + ")", e);
				}
				GalaxyInstancePINVOKE.SWIGPendingException.pendingException = e;
				object typeFromHandle = typeof(GalaxyInstancePINVOKE);
				lock (typeFromHandle)
				{
					GalaxyInstancePINVOKE.SWIGPendingException.numExceptionsPending++;
				}
			}

			// Token: 0x060004BA RID: 1210 RVA: 0x00002E7C File Offset: 0x0000107C
			public static Exception Retrieve()
			{
				Exception result = null;
				if (GalaxyInstancePINVOKE.SWIGPendingException.numExceptionsPending > 0 && GalaxyInstancePINVOKE.SWIGPendingException.pendingException != null)
				{
					result = GalaxyInstancePINVOKE.SWIGPendingException.pendingException;
					GalaxyInstancePINVOKE.SWIGPendingException.pendingException = null;
					object typeFromHandle = typeof(GalaxyInstancePINVOKE);
					lock (typeFromHandle)
					{
						GalaxyInstancePINVOKE.SWIGPendingException.numExceptionsPending--;
					}
				}
				return result;
			}

			// Token: 0x04000027 RID: 39
			[ThreadStatic]
			private static Exception pendingException;

			// Token: 0x04000028 RID: 40
			private static int numExceptionsPending;
		}

		// Token: 0x02000014 RID: 20
		protected class SWIGStringHelper
		{
			// Token: 0x060004BC RID: 1212 RVA: 0x00002EEA File Offset: 0x000010EA
			static SWIGStringHelper()
			{
				GalaxyInstancePINVOKE.SWIGStringHelper.SWIGRegisterStringCallback_GalaxyInstance(GalaxyInstancePINVOKE.SWIGStringHelper.stringDelegate);
			}

			// Token: 0x060004BE RID: 1214
			[DllImport("GalaxyCSharpGlue")]
			public static extern void SWIGRegisterStringCallback_GalaxyInstance(GalaxyInstancePINVOKE.SWIGStringHelper.SWIGStringDelegate stringDelegate);

			// Token: 0x060004BF RID: 1215 RVA: 0x00002F0F File Offset: 0x0000110F
			[MonoPInvokeCallback(typeof(GalaxyInstancePINVOKE.SWIGStringHelper.SWIGStringDelegate))]
			private static string CreateString(string cString)
			{
				return cString;
			}

			// Token: 0x04000029 RID: 41
			private static GalaxyInstancePINVOKE.SWIGStringHelper.SWIGStringDelegate stringDelegate = new GalaxyInstancePINVOKE.SWIGStringHelper.SWIGStringDelegate(GalaxyInstancePINVOKE.SWIGStringHelper.CreateString);

			// Token: 0x02000015 RID: 21
			// (Invoke) Token: 0x060004C1 RID: 1217
			public delegate string SWIGStringDelegate(string message);
		}

		// Token: 0x02000016 RID: 22
		public class UTF8Marshaler : ICustomMarshaler
		{
			// Token: 0x060004C5 RID: 1221 RVA: 0x00002F1C File Offset: 0x0000111C
			public IntPtr MarshalManagedToNative(object managedObj)
			{
				if (managedObj == null)
				{
					return IntPtr.Zero;
				}
				if (!(managedObj is string))
				{
					throw new MarshalDirectiveException("UTF8Marshaler must be used on a string.");
				}
				byte[] bytes = Encoding.UTF8.GetBytes((string)managedObj);
				IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length + 1);
				Marshal.Copy(bytes, 0, intPtr, bytes.Length);
				Marshal.WriteByte(intPtr, bytes.Length, 0);
				return intPtr;
			}

			// Token: 0x060004C6 RID: 1222 RVA: 0x00002F80 File Offset: 0x00001180
			public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
			{
				byte* ptr = (byte*)(void*)pNativeData;
				while (*ptr != 0)
				{
					ptr++;
				}
				int num = (int)((long)(ptr - (void*)pNativeData));
				byte[] array = new byte[num];
				Marshal.Copy(pNativeData, array, 0, num);
				return Encoding.UTF8.GetString(array);
			}

			// Token: 0x060004C7 RID: 1223 RVA: 0x00002FCC File Offset: 0x000011CC
			public void CleanUpNativeData(IntPtr pNativeData)
			{
				Marshal.FreeHGlobal(pNativeData);
			}

			// Token: 0x060004C8 RID: 1224 RVA: 0x00002FD4 File Offset: 0x000011D4
			public void CleanUpManagedData(object managedObj)
			{
			}

			// Token: 0x060004C9 RID: 1225 RVA: 0x00002FD6 File Offset: 0x000011D6
			public int GetNativeDataSize()
			{
				return -1;
			}

			// Token: 0x060004CA RID: 1226 RVA: 0x00002FD9 File Offset: 0x000011D9
			public static ICustomMarshaler GetInstance(string cookie)
			{
				if (GalaxyInstancePINVOKE.UTF8Marshaler.static_instance == null)
				{
					return GalaxyInstancePINVOKE.UTF8Marshaler.static_instance = new GalaxyInstancePINVOKE.UTF8Marshaler();
				}
				return GalaxyInstancePINVOKE.UTF8Marshaler.static_instance;
			}

			// Token: 0x0400002A RID: 42
			private static GalaxyInstancePINVOKE.UTF8Marshaler static_instance;
		}
	}
}
