using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x020000E7 RID: 231
	public class IFriends : IDisposable
	{
		// Token: 0x06000943 RID: 2371 RVA: 0x00016CDA File Offset: 0x00014EDA
		internal IFriends(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000944 RID: 2372 RVA: 0x00016CF6 File Offset: 0x00014EF6
		internal static HandleRef getCPtr(IFriends obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000945 RID: 2373 RVA: 0x00016D14 File Offset: 0x00014F14
		~IFriends()
		{
			this.Dispose();
		}

		// Token: 0x06000946 RID: 2374 RVA: 0x00016D44 File Offset: 0x00014F44
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IFriends(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000947 RID: 2375 RVA: 0x00016DC4 File Offset: 0x00014FC4
		public virtual uint GetDefaultAvatarCriteria()
		{
			uint result = GalaxyInstancePINVOKE.IFriends_GetDefaultAvatarCriteria(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000948 RID: 2376 RVA: 0x00016DEE File Offset: 0x00014FEE
		public virtual void SetDefaultAvatarCriteria(uint defaultAvatarCriteria)
		{
			GalaxyInstancePINVOKE.IFriends_SetDefaultAvatarCriteria(this.swigCPtr, defaultAvatarCriteria);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000949 RID: 2377 RVA: 0x00016E0C File Offset: 0x0001500C
		public virtual void RequestUserInformation(GalaxyID userID, uint avatarCriteria, IUserInformationRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_RequestUserInformation__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), avatarCriteria, IUserInformationRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600094A RID: 2378 RVA: 0x00016E36 File Offset: 0x00015036
		public virtual void RequestUserInformation(GalaxyID userID, uint avatarCriteria)
		{
			GalaxyInstancePINVOKE.IFriends_RequestUserInformation__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID), avatarCriteria);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600094B RID: 2379 RVA: 0x00016E5A File Offset: 0x0001505A
		public virtual void RequestUserInformation(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IFriends_RequestUserInformation__SWIG_2(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600094C RID: 2380 RVA: 0x00016E80 File Offset: 0x00015080
		public virtual bool IsUserInformationAvailable(GalaxyID userID)
		{
			bool result = GalaxyInstancePINVOKE.IFriends_IsUserInformationAvailable(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600094D RID: 2381 RVA: 0x00016EB0 File Offset: 0x000150B0
		public virtual string GetPersonaName()
		{
			string result = GalaxyInstancePINVOKE.IFriends_GetPersonaName(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600094E RID: 2382 RVA: 0x00016EDC File Offset: 0x000150DC
		public virtual void GetPersonaNameCopy(out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IFriends_GetPersonaNameCopy(this.swigCPtr, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x0600094F RID: 2383 RVA: 0x00016F34 File Offset: 0x00015134
		public virtual PersonaState GetPersonaState()
		{
			PersonaState result = (PersonaState)GalaxyInstancePINVOKE.IFriends_GetPersonaState(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000950 RID: 2384 RVA: 0x00016F60 File Offset: 0x00015160
		public virtual string GetFriendPersonaName(GalaxyID userID)
		{
			string result = GalaxyInstancePINVOKE.IFriends_GetFriendPersonaName(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000951 RID: 2385 RVA: 0x00016F90 File Offset: 0x00015190
		public virtual void GetFriendPersonaNameCopy(GalaxyID userID, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IFriends_GetFriendPersonaNameCopy(this.swigCPtr, GalaxyID.getCPtr(userID), array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000952 RID: 2386 RVA: 0x00016FEC File Offset: 0x000151EC
		public virtual PersonaState GetFriendPersonaState(GalaxyID userID)
		{
			PersonaState result = (PersonaState)GalaxyInstancePINVOKE.IFriends_GetFriendPersonaState(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000953 RID: 2387 RVA: 0x0001701C File Offset: 0x0001521C
		public virtual string GetFriendAvatarUrl(GalaxyID userID, AvatarType avatarType)
		{
			string result = GalaxyInstancePINVOKE.IFriends_GetFriendAvatarUrl(this.swigCPtr, GalaxyID.getCPtr(userID), (int)avatarType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000954 RID: 2388 RVA: 0x00017050 File Offset: 0x00015250
		public virtual void GetFriendAvatarUrlCopy(GalaxyID userID, AvatarType avatarType, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IFriends_GetFriendAvatarUrlCopy(this.swigCPtr, GalaxyID.getCPtr(userID), (int)avatarType, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000955 RID: 2389 RVA: 0x000170B0 File Offset: 0x000152B0
		public virtual uint GetFriendAvatarImageID(GalaxyID userID, AvatarType avatarType)
		{
			uint result = GalaxyInstancePINVOKE.IFriends_GetFriendAvatarImageID(this.swigCPtr, GalaxyID.getCPtr(userID), (int)avatarType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000956 RID: 2390 RVA: 0x000170E1 File Offset: 0x000152E1
		public virtual void GetFriendAvatarImageRGBA(GalaxyID userID, AvatarType avatarType, byte[] buffer, uint bufferLength)
		{
			GalaxyInstancePINVOKE.IFriends_GetFriendAvatarImageRGBA(this.swigCPtr, GalaxyID.getCPtr(userID), (int)avatarType, buffer, bufferLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000957 RID: 2391 RVA: 0x00017108 File Offset: 0x00015308
		public virtual bool IsFriendAvatarImageRGBAAvailable(GalaxyID userID, AvatarType avatarType)
		{
			bool result = GalaxyInstancePINVOKE.IFriends_IsFriendAvatarImageRGBAAvailable(this.swigCPtr, GalaxyID.getCPtr(userID), (int)avatarType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000958 RID: 2392 RVA: 0x00017139 File Offset: 0x00015339
		public virtual void RequestFriendList(IFriendListListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_RequestFriendList__SWIG_0(this.swigCPtr, IFriendListListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000959 RID: 2393 RVA: 0x0001715C File Offset: 0x0001535C
		public virtual void RequestFriendList()
		{
			GalaxyInstancePINVOKE.IFriends_RequestFriendList__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600095A RID: 2394 RVA: 0x0001717C File Offset: 0x0001537C
		public virtual bool IsFriend(GalaxyID userID)
		{
			bool result = GalaxyInstancePINVOKE.IFriends_IsFriend(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600095B RID: 2395 RVA: 0x000171AC File Offset: 0x000153AC
		public virtual uint GetFriendCount()
		{
			uint result = GalaxyInstancePINVOKE.IFriends_GetFriendCount(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0600095C RID: 2396 RVA: 0x000171D8 File Offset: 0x000153D8
		public virtual GalaxyID GetFriendByIndex(uint index)
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IFriends_GetFriendByIndex(this.swigCPtr, index);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			GalaxyID result = null;
			if (intPtr != IntPtr.Zero)
			{
				result = new GalaxyID(intPtr, true);
			}
			return result;
		}

		// Token: 0x0600095D RID: 2397 RVA: 0x0001721D File Offset: 0x0001541D
		public virtual void SendFriendInvitation(GalaxyID userID, IFriendInvitationSendListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_SendFriendInvitation__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), IFriendInvitationSendListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600095E RID: 2398 RVA: 0x00017246 File Offset: 0x00015446
		public virtual void SendFriendInvitation(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IFriends_SendFriendInvitation__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600095F RID: 2399 RVA: 0x00017269 File Offset: 0x00015469
		public virtual void RequestFriendInvitationList(IFriendInvitationListRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_RequestFriendInvitationList__SWIG_0(this.swigCPtr, IFriendInvitationListRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000960 RID: 2400 RVA: 0x0001728C File Offset: 0x0001548C
		public virtual void RequestFriendInvitationList()
		{
			GalaxyInstancePINVOKE.IFriends_RequestFriendInvitationList__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000961 RID: 2401 RVA: 0x000172A9 File Offset: 0x000154A9
		public virtual void RequestSentFriendInvitationList(ISentFriendInvitationListRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_RequestSentFriendInvitationList__SWIG_0(this.swigCPtr, ISentFriendInvitationListRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000962 RID: 2402 RVA: 0x000172CC File Offset: 0x000154CC
		public virtual void RequestSentFriendInvitationList()
		{
			GalaxyInstancePINVOKE.IFriends_RequestSentFriendInvitationList__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000963 RID: 2403 RVA: 0x000172EC File Offset: 0x000154EC
		public virtual uint GetFriendInvitationCount()
		{
			uint result = GalaxyInstancePINVOKE.IFriends_GetFriendInvitationCount(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x00017316 File Offset: 0x00015516
		public virtual void GetFriendInvitationByIndex(uint index, ref GalaxyID userID, ref uint sendTime)
		{
			GalaxyInstancePINVOKE.IFriends_GetFriendInvitationByIndex(this.swigCPtr, index, GalaxyID.getCPtr(userID), ref sendTime);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000965 RID: 2405 RVA: 0x0001733C File Offset: 0x0001553C
		public virtual void RespondToFriendInvitation(GalaxyID userID, bool accept, IFriendInvitationRespondToListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_RespondToFriendInvitation__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), accept, IFriendInvitationRespondToListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000966 RID: 2406 RVA: 0x00017366 File Offset: 0x00015566
		public virtual void RespondToFriendInvitation(GalaxyID userID, bool accept)
		{
			GalaxyInstancePINVOKE.IFriends_RespondToFriendInvitation__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID), accept);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000967 RID: 2407 RVA: 0x0001738A File Offset: 0x0001558A
		public virtual void DeleteFriend(GalaxyID userID, IFriendDeleteListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_DeleteFriend__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), IFriendDeleteListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000968 RID: 2408 RVA: 0x000173B3 File Offset: 0x000155B3
		public virtual void DeleteFriend(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IFriends_DeleteFriend__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000969 RID: 2409 RVA: 0x000173D6 File Offset: 0x000155D6
		public virtual void SetRichPresence(string key, string value, IRichPresenceChangeListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_SetRichPresence__SWIG_0(this.swigCPtr, key, value, IRichPresenceChangeListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x000173FB File Offset: 0x000155FB
		public virtual void SetRichPresence(string key, string value)
		{
			GalaxyInstancePINVOKE.IFriends_SetRichPresence__SWIG_1(this.swigCPtr, key, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600096B RID: 2411 RVA: 0x0001741A File Offset: 0x0001561A
		public virtual void DeleteRichPresence(string key, IRichPresenceChangeListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_DeleteRichPresence__SWIG_0(this.swigCPtr, key, IRichPresenceChangeListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600096C RID: 2412 RVA: 0x0001743E File Offset: 0x0001563E
		public virtual void DeleteRichPresence(string key)
		{
			GalaxyInstancePINVOKE.IFriends_DeleteRichPresence__SWIG_1(this.swigCPtr, key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x0001745C File Offset: 0x0001565C
		public virtual void ClearRichPresence(IRichPresenceChangeListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_ClearRichPresence__SWIG_0(this.swigCPtr, IRichPresenceChangeListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600096E RID: 2414 RVA: 0x0001747F File Offset: 0x0001567F
		public virtual void ClearRichPresence()
		{
			GalaxyInstancePINVOKE.IFriends_ClearRichPresence__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600096F RID: 2415 RVA: 0x0001749C File Offset: 0x0001569C
		public virtual void RequestRichPresence(GalaxyID userID, IRichPresenceRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_RequestRichPresence__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), IRichPresenceRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000970 RID: 2416 RVA: 0x000174C5 File Offset: 0x000156C5
		public virtual void RequestRichPresence(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IFriends_RequestRichPresence__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000971 RID: 2417 RVA: 0x000174E8 File Offset: 0x000156E8
		public virtual void RequestRichPresence()
		{
			GalaxyInstancePINVOKE.IFriends_RequestRichPresence__SWIG_2(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000972 RID: 2418 RVA: 0x00017508 File Offset: 0x00015708
		public virtual string GetRichPresence(string key, GalaxyID userID)
		{
			string result = GalaxyInstancePINVOKE.IFriends_GetRichPresence__SWIG_0(this.swigCPtr, key, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000973 RID: 2419 RVA: 0x0001753C File Offset: 0x0001573C
		public virtual string GetRichPresence(string key)
		{
			string result = GalaxyInstancePINVOKE.IFriends_GetRichPresence__SWIG_1(this.swigCPtr, key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000974 RID: 2420 RVA: 0x00017568 File Offset: 0x00015768
		public virtual void GetRichPresenceCopy(string key, out string buffer, uint bufferLength, GalaxyID userID)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IFriends_GetRichPresenceCopy__SWIG_0(this.swigCPtr, key, array, bufferLength, GalaxyID.getCPtr(userID));
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000975 RID: 2421 RVA: 0x000175C8 File Offset: 0x000157C8
		public virtual void GetRichPresenceCopy(string key, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IFriends_GetRichPresenceCopy__SWIG_1(this.swigCPtr, key, array, bufferLength);
				if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
				{
					throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
				}
			}
			finally
			{
				buffer = Encoding.UTF8.GetString(array);
			}
		}

		// Token: 0x06000976 RID: 2422 RVA: 0x00017620 File Offset: 0x00015820
		public virtual uint GetRichPresenceCount(GalaxyID userID)
		{
			uint result = GalaxyInstancePINVOKE.IFriends_GetRichPresenceCount__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000977 RID: 2423 RVA: 0x00017650 File Offset: 0x00015850
		public virtual uint GetRichPresenceCount()
		{
			uint result = GalaxyInstancePINVOKE.IFriends_GetRichPresenceCount__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000978 RID: 2424 RVA: 0x0001767A File Offset: 0x0001587A
		public virtual void GetRichPresenceByIndex(uint index, ref byte[] key, uint keyLength, ref byte[] value, uint valueLength, GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IFriends_GetRichPresenceByIndex__SWIG_0(this.swigCPtr, index, key, keyLength, value, valueLength, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000979 RID: 2425 RVA: 0x000176A7 File Offset: 0x000158A7
		public virtual void GetRichPresenceByIndex(uint index, ref byte[] key, uint keyLength, ref byte[] value, uint valueLength)
		{
			GalaxyInstancePINVOKE.IFriends_GetRichPresenceByIndex__SWIG_1(this.swigCPtr, index, key, keyLength, value, valueLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600097A RID: 2426 RVA: 0x000176CD File Offset: 0x000158CD
		public virtual void ShowOverlayInviteDialog(string connectionString)
		{
			GalaxyInstancePINVOKE.IFriends_ShowOverlayInviteDialog(this.swigCPtr, connectionString);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600097B RID: 2427 RVA: 0x000176EB File Offset: 0x000158EB
		public virtual void SendInvitation(GalaxyID userID, string connectionString, ISendInvitationListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_SendInvitation__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), connectionString, ISendInvitationListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600097C RID: 2428 RVA: 0x00017715 File Offset: 0x00015915
		public virtual void SendInvitation(GalaxyID userID, string connectionString)
		{
			GalaxyInstancePINVOKE.IFriends_SendInvitation__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID), connectionString);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600097D RID: 2429 RVA: 0x00017739 File Offset: 0x00015939
		public virtual void FindUser(string userSpecifier, IUserFindListener listener)
		{
			GalaxyInstancePINVOKE.IFriends_FindUser__SWIG_0(this.swigCPtr, userSpecifier, IUserFindListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x0001775D File Offset: 0x0001595D
		public virtual void FindUser(string userSpecifier)
		{
			GalaxyInstancePINVOKE.IFriends_FindUser__SWIG_1(this.swigCPtr, userSpecifier);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x0600097F RID: 2431 RVA: 0x0001777C File Offset: 0x0001597C
		public virtual bool IsUserInTheSameGame(GalaxyID userID)
		{
			bool result = GalaxyInstancePINVOKE.IFriends_IsUserInTheSameGame(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400016F RID: 367
		private HandleRef swigCPtr;

		// Token: 0x04000170 RID: 368
		protected bool swigCMemOwn;
	}
}
