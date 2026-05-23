using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x02000120 RID: 288
	public class IMatchmaking : IDisposable
	{
		// Token: 0x06000AC4 RID: 2756 RVA: 0x00017C68 File Offset: 0x00015E68
		internal IMatchmaking(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000AC5 RID: 2757 RVA: 0x00017C84 File Offset: 0x00015E84
		internal static HandleRef getCPtr(IMatchmaking obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000AC6 RID: 2758 RVA: 0x00017CA4 File Offset: 0x00015EA4
		~IMatchmaking()
		{
			this.Dispose();
		}

		// Token: 0x06000AC7 RID: 2759 RVA: 0x00017CD4 File Offset: 0x00015ED4
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IMatchmaking(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000AC8 RID: 2760 RVA: 0x00017D54 File Offset: 0x00015F54
		public bool SendLobbyMessage(GalaxyID lobbyID, string msg)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(msg);
			return this.SendLobbyMessage(lobbyID, bytes, (uint)bytes.Length);
		}

		// Token: 0x06000AC9 RID: 2761 RVA: 0x00017D78 File Offset: 0x00015F78
		public uint GetLobbyMessage(GalaxyID lobbyID, uint messageID, ref GalaxyID _senderID, out string msg, uint internalBufferLen = 1024U)
		{
			byte[] array = new byte[internalBufferLen];
			GalaxyID galaxyID = new GalaxyID();
			uint lobbyMessage = this.GetLobbyMessage(lobbyID, messageID, ref galaxyID, ref array, (uint)array.Length);
			msg = Encoding.UTF8.GetString(array, 0, (int)lobbyMessage);
			return lobbyMessage;
		}

		// Token: 0x06000ACA RID: 2762 RVA: 0x00017DB5 File Offset: 0x00015FB5
		public virtual void CreateLobby(LobbyType lobbyType, uint maxMembers, bool joinable, LobbyTopologyType lobbyTopologyType, ILobbyCreatedListener lobbyCreatedListener, ILobbyEnteredListener lobbyEnteredListener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_CreateLobby__SWIG_0(this.swigCPtr, (int)lobbyType, maxMembers, joinable, (int)lobbyTopologyType, ILobbyCreatedListener.getCPtr(lobbyCreatedListener), ILobbyEnteredListener.getCPtr(lobbyEnteredListener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ACB RID: 2763 RVA: 0x00017DE5 File Offset: 0x00015FE5
		public virtual void CreateLobby(LobbyType lobbyType, uint maxMembers, bool joinable, LobbyTopologyType lobbyTopologyType, ILobbyCreatedListener lobbyCreatedListener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_CreateLobby__SWIG_1(this.swigCPtr, (int)lobbyType, maxMembers, joinable, (int)lobbyTopologyType, ILobbyCreatedListener.getCPtr(lobbyCreatedListener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ACC RID: 2764 RVA: 0x00017E0E File Offset: 0x0001600E
		public virtual void CreateLobby(LobbyType lobbyType, uint maxMembers, bool joinable, LobbyTopologyType lobbyTopologyType)
		{
			GalaxyInstancePINVOKE.IMatchmaking_CreateLobby__SWIG_2(this.swigCPtr, (int)lobbyType, maxMembers, joinable, (int)lobbyTopologyType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ACD RID: 2765 RVA: 0x00017E30 File Offset: 0x00016030
		public virtual void RequestLobbyList(bool allowFullLobbies, ILobbyListListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_RequestLobbyList__SWIG_0(this.swigCPtr, allowFullLobbies, ILobbyListListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ACE RID: 2766 RVA: 0x00017E54 File Offset: 0x00016054
		public virtual void RequestLobbyList(bool allowFullLobbies)
		{
			GalaxyInstancePINVOKE.IMatchmaking_RequestLobbyList__SWIG_1(this.swigCPtr, allowFullLobbies);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ACF RID: 2767 RVA: 0x00017E72 File Offset: 0x00016072
		public virtual void RequestLobbyList()
		{
			GalaxyInstancePINVOKE.IMatchmaking_RequestLobbyList__SWIG_2(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD0 RID: 2768 RVA: 0x00017E8F File Offset: 0x0001608F
		public virtual void AddRequestLobbyListResultCountFilter(uint limit)
		{
			GalaxyInstancePINVOKE.IMatchmaking_AddRequestLobbyListResultCountFilter(this.swigCPtr, limit);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD1 RID: 2769 RVA: 0x00017EAD File Offset: 0x000160AD
		public virtual void AddRequestLobbyListStringFilter(string keyToMatch, string valueToMatch, LobbyComparisonType comparisonType)
		{
			GalaxyInstancePINVOKE.IMatchmaking_AddRequestLobbyListStringFilter(this.swigCPtr, keyToMatch, valueToMatch, (int)comparisonType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD2 RID: 2770 RVA: 0x00017ECD File Offset: 0x000160CD
		public virtual void AddRequestLobbyListNumericalFilter(string keyToMatch, int valueToMatch, LobbyComparisonType comparisonType)
		{
			GalaxyInstancePINVOKE.IMatchmaking_AddRequestLobbyListNumericalFilter(this.swigCPtr, keyToMatch, valueToMatch, (int)comparisonType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD3 RID: 2771 RVA: 0x00017EED File Offset: 0x000160ED
		public virtual void AddRequestLobbyListNearValueFilter(string keyToMatch, int valueToBeCloseTo)
		{
			GalaxyInstancePINVOKE.IMatchmaking_AddRequestLobbyListNearValueFilter(this.swigCPtr, keyToMatch, valueToBeCloseTo);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD4 RID: 2772 RVA: 0x00017F0C File Offset: 0x0001610C
		public virtual GalaxyID GetLobbyByIndex(uint index)
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyByIndex(this.swigCPtr, index);
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

		// Token: 0x06000AD5 RID: 2773 RVA: 0x00017F51 File Offset: 0x00016151
		public virtual void JoinLobby(GalaxyID lobbyID, ILobbyEnteredListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_JoinLobby__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), ILobbyEnteredListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD6 RID: 2774 RVA: 0x00017F7A File Offset: 0x0001617A
		public virtual void JoinLobby(GalaxyID lobbyID)
		{
			GalaxyInstancePINVOKE.IMatchmaking_JoinLobby__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD7 RID: 2775 RVA: 0x00017F9D File Offset: 0x0001619D
		public virtual void LeaveLobby(GalaxyID lobbyID, ILobbyLeftListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_LeaveLobby__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), ILobbyLeftListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD8 RID: 2776 RVA: 0x00017FC6 File Offset: 0x000161C6
		public virtual void LeaveLobby(GalaxyID lobbyID)
		{
			GalaxyInstancePINVOKE.IMatchmaking_LeaveLobby__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AD9 RID: 2777 RVA: 0x00017FE9 File Offset: 0x000161E9
		public virtual void SetMaxNumLobbyMembers(GalaxyID lobbyID, uint maxNumLobbyMembers, ILobbyDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetMaxNumLobbyMembers__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), maxNumLobbyMembers, ILobbyDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ADA RID: 2778 RVA: 0x00018013 File Offset: 0x00016213
		public virtual void SetMaxNumLobbyMembers(GalaxyID lobbyID, uint maxNumLobbyMembers)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetMaxNumLobbyMembers__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), maxNumLobbyMembers);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ADB RID: 2779 RVA: 0x00018038 File Offset: 0x00016238
		public virtual uint GetMaxNumLobbyMembers(GalaxyID lobbyID)
		{
			uint result = GalaxyInstancePINVOKE.IMatchmaking_GetMaxNumLobbyMembers(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000ADC RID: 2780 RVA: 0x00018068 File Offset: 0x00016268
		public virtual uint GetNumLobbyMembers(GalaxyID lobbyID)
		{
			uint result = GalaxyInstancePINVOKE.IMatchmaking_GetNumLobbyMembers(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000ADD RID: 2781 RVA: 0x00018098 File Offset: 0x00016298
		public virtual GalaxyID GetLobbyMemberByIndex(GalaxyID lobbyID, uint index)
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyMemberByIndex(this.swigCPtr, GalaxyID.getCPtr(lobbyID), index);
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

		// Token: 0x06000ADE RID: 2782 RVA: 0x000180E3 File Offset: 0x000162E3
		public virtual void SetLobbyType(GalaxyID lobbyID, LobbyType lobbyType, ILobbyDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyType__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), (int)lobbyType, ILobbyDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000ADF RID: 2783 RVA: 0x0001810D File Offset: 0x0001630D
		public virtual void SetLobbyType(GalaxyID lobbyID, LobbyType lobbyType)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyType__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), (int)lobbyType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AE0 RID: 2784 RVA: 0x00018134 File Offset: 0x00016334
		public virtual LobbyType GetLobbyType(GalaxyID lobbyID)
		{
			LobbyType result = (LobbyType)GalaxyInstancePINVOKE.IMatchmaking_GetLobbyType(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AE1 RID: 2785 RVA: 0x00018164 File Offset: 0x00016364
		public virtual void SetLobbyJoinable(GalaxyID lobbyID, bool joinable, ILobbyDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyJoinable__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), joinable, ILobbyDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AE2 RID: 2786 RVA: 0x0001818E File Offset: 0x0001638E
		public virtual void SetLobbyJoinable(GalaxyID lobbyID, bool joinable)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyJoinable__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), joinable);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AE3 RID: 2787 RVA: 0x000181B4 File Offset: 0x000163B4
		public virtual bool IsLobbyJoinable(GalaxyID lobbyID)
		{
			bool result = GalaxyInstancePINVOKE.IMatchmaking_IsLobbyJoinable(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AE4 RID: 2788 RVA: 0x000181E4 File Offset: 0x000163E4
		public virtual void RequestLobbyData(GalaxyID lobbyID, ILobbyDataRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_RequestLobbyData__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), ILobbyDataRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AE5 RID: 2789 RVA: 0x0001820D File Offset: 0x0001640D
		public virtual void RequestLobbyData(GalaxyID lobbyID)
		{
			GalaxyInstancePINVOKE.IMatchmaking_RequestLobbyData__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AE6 RID: 2790 RVA: 0x00018230 File Offset: 0x00016430
		public virtual string GetLobbyData(GalaxyID lobbyID, string key)
		{
			string result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyData(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AE7 RID: 2791 RVA: 0x00018264 File Offset: 0x00016464
		public virtual void GetLobbyDataCopy(GalaxyID lobbyID, string key, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IMatchmaking_GetLobbyDataCopy(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, array, bufferLength);
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

		// Token: 0x06000AE8 RID: 2792 RVA: 0x000182C4 File Offset: 0x000164C4
		public virtual void SetLobbyData(GalaxyID lobbyID, string key, string value, ILobbyDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyData__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, value, ILobbyDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AE9 RID: 2793 RVA: 0x000182F0 File Offset: 0x000164F0
		public virtual void SetLobbyData(GalaxyID lobbyID, string key, string value)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyData__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AEA RID: 2794 RVA: 0x00018318 File Offset: 0x00016518
		public virtual uint GetLobbyDataCount(GalaxyID lobbyID)
		{
			uint result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyDataCount(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AEB RID: 2795 RVA: 0x00018348 File Offset: 0x00016548
		public virtual bool GetLobbyDataByIndex(GalaxyID lobbyID, uint index, ref byte[] key, uint keyLength, ref byte[] value, uint valueLength)
		{
			bool result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyDataByIndex(this.swigCPtr, GalaxyID.getCPtr(lobbyID), index, key, keyLength, value, valueLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AEC RID: 2796 RVA: 0x00018382 File Offset: 0x00016582
		public virtual void DeleteLobbyData(GalaxyID lobbyID, string key, ILobbyDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_DeleteLobbyData__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, ILobbyDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AED RID: 2797 RVA: 0x000183AC File Offset: 0x000165AC
		public virtual void DeleteLobbyData(GalaxyID lobbyID, string key)
		{
			GalaxyInstancePINVOKE.IMatchmaking_DeleteLobbyData__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AEE RID: 2798 RVA: 0x000183D0 File Offset: 0x000165D0
		public virtual string GetLobbyMemberData(GalaxyID lobbyID, GalaxyID memberID, string key)
		{
			string result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyMemberData(this.swigCPtr, GalaxyID.getCPtr(lobbyID), GalaxyID.getCPtr(memberID), key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AEF RID: 2799 RVA: 0x00018408 File Offset: 0x00016608
		public virtual void GetLobbyMemberDataCopy(GalaxyID lobbyID, GalaxyID memberID, string key, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IMatchmaking_GetLobbyMemberDataCopy(this.swigCPtr, GalaxyID.getCPtr(lobbyID), GalaxyID.getCPtr(memberID), key, array, bufferLength);
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

		// Token: 0x06000AF0 RID: 2800 RVA: 0x00018470 File Offset: 0x00016670
		public virtual void SetLobbyMemberData(GalaxyID lobbyID, string key, string value, ILobbyMemberDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyMemberData__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, value, ILobbyMemberDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AF1 RID: 2801 RVA: 0x0001849C File Offset: 0x0001669C
		public virtual void SetLobbyMemberData(GalaxyID lobbyID, string key, string value)
		{
			GalaxyInstancePINVOKE.IMatchmaking_SetLobbyMemberData__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AF2 RID: 2802 RVA: 0x000184C4 File Offset: 0x000166C4
		public virtual uint GetLobbyMemberDataCount(GalaxyID lobbyID, GalaxyID memberID)
		{
			uint result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyMemberDataCount(this.swigCPtr, GalaxyID.getCPtr(lobbyID), GalaxyID.getCPtr(memberID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AF3 RID: 2803 RVA: 0x000184FC File Offset: 0x000166FC
		public virtual bool GetLobbyMemberDataByIndex(GalaxyID lobbyID, GalaxyID memberID, uint index, ref byte[] key, uint keyLength, ref byte[] value, uint valueLength)
		{
			bool result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyMemberDataByIndex(this.swigCPtr, GalaxyID.getCPtr(lobbyID), GalaxyID.getCPtr(memberID), index, key, keyLength, value, valueLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AF4 RID: 2804 RVA: 0x0001853D File Offset: 0x0001673D
		public virtual void DeleteLobbyMemberData(GalaxyID lobbyID, string key, ILobbyMemberDataUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IMatchmaking_DeleteLobbyMemberData__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key, ILobbyMemberDataUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AF5 RID: 2805 RVA: 0x00018567 File Offset: 0x00016767
		public virtual void DeleteLobbyMemberData(GalaxyID lobbyID, string key)
		{
			GalaxyInstancePINVOKE.IMatchmaking_DeleteLobbyMemberData__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(lobbyID), key);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000AF6 RID: 2806 RVA: 0x0001858C File Offset: 0x0001678C
		public virtual GalaxyID GetLobbyOwner(GalaxyID lobbyID)
		{
			IntPtr intPtr = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyOwner(this.swigCPtr, GalaxyID.getCPtr(lobbyID));
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

		// Token: 0x06000AF7 RID: 2807 RVA: 0x000185D8 File Offset: 0x000167D8
		public virtual bool SendLobbyMessage(GalaxyID lobbyID, byte[] data, uint dataSize)
		{
			bool result = GalaxyInstancePINVOKE.IMatchmaking_SendLobbyMessage(this.swigCPtr, GalaxyID.getCPtr(lobbyID), data, dataSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000AF8 RID: 2808 RVA: 0x0001860C File Offset: 0x0001680C
		public virtual uint GetLobbyMessage(GalaxyID lobbyID, uint messageID, ref GalaxyID senderID, ref byte[] msg, uint msgLength)
		{
			uint result = GalaxyInstancePINVOKE.IMatchmaking_GetLobbyMessage(this.swigCPtr, GalaxyID.getCPtr(lobbyID), messageID, GalaxyID.getCPtr(senderID), msg, msgLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x040001EF RID: 495
		private HandleRef swigCPtr;

		// Token: 0x040001F0 RID: 496
		protected bool swigCMemOwn;
	}
}
