using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Galaxy.Api
{
	// Token: 0x0200014F RID: 335
	public class IStats : IDisposable
	{
		// Token: 0x06000C1A RID: 3098 RVA: 0x00018DFC File Offset: 0x00016FFC
		internal IStats(IntPtr cPtr, bool cMemoryOwn)
		{
			this.swigCMemOwn = cMemoryOwn;
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000C1B RID: 3099 RVA: 0x00018E18 File Offset: 0x00017018
		internal static HandleRef getCPtr(IStats obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000C1C RID: 3100 RVA: 0x00018E38 File Offset: 0x00017038
		~IStats()
		{
			this.Dispose();
		}

		// Token: 0x06000C1D RID: 3101 RVA: 0x00018E68 File Offset: 0x00017068
		public virtual void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IStats(this.swigCPtr);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000C1E RID: 3102 RVA: 0x00018EE8 File Offset: 0x000170E8
		public virtual void RequestUserStatsAndAchievements(GalaxyID userID, IUserStatsAndAchievementsRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_RequestUserStatsAndAchievements__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), IUserStatsAndAchievementsRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C1F RID: 3103 RVA: 0x00018F11 File Offset: 0x00017111
		public virtual void RequestUserStatsAndAchievements(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IStats_RequestUserStatsAndAchievements__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C20 RID: 3104 RVA: 0x00018F34 File Offset: 0x00017134
		public virtual void RequestUserStatsAndAchievements()
		{
			GalaxyInstancePINVOKE.IStats_RequestUserStatsAndAchievements__SWIG_2(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C21 RID: 3105 RVA: 0x00018F54 File Offset: 0x00017154
		public virtual int GetStatInt(string name, GalaxyID userID)
		{
			int result = GalaxyInstancePINVOKE.IStats_GetStatInt__SWIG_0(this.swigCPtr, name, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C22 RID: 3106 RVA: 0x00018F88 File Offset: 0x00017188
		public virtual int GetStatInt(string name)
		{
			int result = GalaxyInstancePINVOKE.IStats_GetStatInt__SWIG_1(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C23 RID: 3107 RVA: 0x00018FB4 File Offset: 0x000171B4
		public virtual float GetStatFloat(string name, GalaxyID userID)
		{
			float result = GalaxyInstancePINVOKE.IStats_GetStatFloat__SWIG_0(this.swigCPtr, name, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C24 RID: 3108 RVA: 0x00018FE8 File Offset: 0x000171E8
		public virtual float GetStatFloat(string name)
		{
			float result = GalaxyInstancePINVOKE.IStats_GetStatFloat__SWIG_1(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C25 RID: 3109 RVA: 0x00019013 File Offset: 0x00017213
		public virtual void SetStatInt(string name, int value)
		{
			GalaxyInstancePINVOKE.IStats_SetStatInt(this.swigCPtr, name, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C26 RID: 3110 RVA: 0x00019032 File Offset: 0x00017232
		public virtual void SetStatFloat(string name, float value)
		{
			GalaxyInstancePINVOKE.IStats_SetStatFloat(this.swigCPtr, name, value);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C27 RID: 3111 RVA: 0x00019051 File Offset: 0x00017251
		public virtual void UpdateAvgRateStat(string name, float countThisSession, double sessionLength)
		{
			GalaxyInstancePINVOKE.IStats_UpdateAvgRateStat(this.swigCPtr, name, countThisSession, sessionLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C28 RID: 3112 RVA: 0x00019071 File Offset: 0x00017271
		public virtual void GetAchievement(string name, ref bool unlocked, ref uint unlockTime, GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IStats_GetAchievement__SWIG_0(this.swigCPtr, name, ref unlocked, ref unlockTime, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C29 RID: 3113 RVA: 0x00019098 File Offset: 0x00017298
		public virtual void GetAchievement(string name, ref bool unlocked, ref uint unlockTime)
		{
			GalaxyInstancePINVOKE.IStats_GetAchievement__SWIG_1(this.swigCPtr, name, ref unlocked, ref unlockTime);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C2A RID: 3114 RVA: 0x000190B8 File Offset: 0x000172B8
		public virtual void SetAchievement(string name)
		{
			GalaxyInstancePINVOKE.IStats_SetAchievement(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C2B RID: 3115 RVA: 0x000190D6 File Offset: 0x000172D6
		public virtual void ClearAchievement(string name)
		{
			GalaxyInstancePINVOKE.IStats_ClearAchievement(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C2C RID: 3116 RVA: 0x000190F4 File Offset: 0x000172F4
		public virtual void StoreStatsAndAchievements(IStatsAndAchievementsStoreListener listener)
		{
			GalaxyInstancePINVOKE.IStats_StoreStatsAndAchievements__SWIG_0(this.swigCPtr, IStatsAndAchievementsStoreListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C2D RID: 3117 RVA: 0x00019117 File Offset: 0x00017317
		public virtual void StoreStatsAndAchievements()
		{
			GalaxyInstancePINVOKE.IStats_StoreStatsAndAchievements__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C2E RID: 3118 RVA: 0x00019134 File Offset: 0x00017334
		public virtual void ResetStatsAndAchievements(IStatsAndAchievementsStoreListener listener)
		{
			GalaxyInstancePINVOKE.IStats_ResetStatsAndAchievements__SWIG_0(this.swigCPtr, IStatsAndAchievementsStoreListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C2F RID: 3119 RVA: 0x00019157 File Offset: 0x00017357
		public virtual void ResetStatsAndAchievements()
		{
			GalaxyInstancePINVOKE.IStats_ResetStatsAndAchievements__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C30 RID: 3120 RVA: 0x00019174 File Offset: 0x00017374
		public virtual string GetAchievementDisplayName(string name)
		{
			string result = GalaxyInstancePINVOKE.IStats_GetAchievementDisplayName(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C31 RID: 3121 RVA: 0x000191A0 File Offset: 0x000173A0
		public virtual void GetAchievementDisplayNameCopy(string name, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IStats_GetAchievementDisplayNameCopy(this.swigCPtr, name, array, bufferLength);
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

		// Token: 0x06000C32 RID: 3122 RVA: 0x000191F8 File Offset: 0x000173F8
		public virtual string GetAchievementDescription(string name)
		{
			string result = GalaxyInstancePINVOKE.IStats_GetAchievementDescription(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C33 RID: 3123 RVA: 0x00019224 File Offset: 0x00017424
		public virtual void GetAchievementDescriptionCopy(string name, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IStats_GetAchievementDescriptionCopy(this.swigCPtr, name, array, bufferLength);
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

		// Token: 0x06000C34 RID: 3124 RVA: 0x0001927C File Offset: 0x0001747C
		public virtual bool IsAchievementVisible(string name)
		{
			bool result = GalaxyInstancePINVOKE.IStats_IsAchievementVisible(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C35 RID: 3125 RVA: 0x000192A8 File Offset: 0x000174A8
		public virtual bool IsAchievementVisibleWhileLocked(string name)
		{
			bool result = GalaxyInstancePINVOKE.IStats_IsAchievementVisibleWhileLocked(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C36 RID: 3126 RVA: 0x000192D3 File Offset: 0x000174D3
		public virtual void RequestLeaderboards(ILeaderboardsRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboards__SWIG_0(this.swigCPtr, ILeaderboardsRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C37 RID: 3127 RVA: 0x000192F6 File Offset: 0x000174F6
		public virtual void RequestLeaderboards()
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboards__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C38 RID: 3128 RVA: 0x00019314 File Offset: 0x00017514
		public virtual string GetLeaderboardDisplayName(string name)
		{
			string result = GalaxyInstancePINVOKE.IStats_GetLeaderboardDisplayName(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C39 RID: 3129 RVA: 0x00019340 File Offset: 0x00017540
		public virtual void GetLeaderboardDisplayNameCopy(string name, out string buffer, uint bufferLength)
		{
			byte[] array = new byte[bufferLength];
			try
			{
				GalaxyInstancePINVOKE.IStats_GetLeaderboardDisplayNameCopy(this.swigCPtr, name, array, bufferLength);
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

		// Token: 0x06000C3A RID: 3130 RVA: 0x00019398 File Offset: 0x00017598
		public virtual LeaderboardSortMethod GetLeaderboardSortMethod(string name)
		{
			LeaderboardSortMethod result = (LeaderboardSortMethod)GalaxyInstancePINVOKE.IStats_GetLeaderboardSortMethod(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C3B RID: 3131 RVA: 0x000193C4 File Offset: 0x000175C4
		public virtual LeaderboardDisplayType GetLeaderboardDisplayType(string name)
		{
			LeaderboardDisplayType result = (LeaderboardDisplayType)GalaxyInstancePINVOKE.IStats_GetLeaderboardDisplayType(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C3C RID: 3132 RVA: 0x000193EF File Offset: 0x000175EF
		public virtual void RequestLeaderboardEntriesGlobal(string name, uint rangeStart, uint rangeEnd, ILeaderboardEntriesRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesGlobal__SWIG_0(this.swigCPtr, name, rangeStart, rangeEnd, ILeaderboardEntriesRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C3D RID: 3133 RVA: 0x00019416 File Offset: 0x00017616
		public virtual void RequestLeaderboardEntriesGlobal(string name, uint rangeStart, uint rangeEnd)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesGlobal__SWIG_1(this.swigCPtr, name, rangeStart, rangeEnd);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C3E RID: 3134 RVA: 0x00019436 File Offset: 0x00017636
		public virtual void RequestLeaderboardEntriesAroundUser(string name, uint countBefore, uint countAfter, GalaxyID userID, ILeaderboardEntriesRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesAroundUser__SWIG_0(this.swigCPtr, name, countBefore, countAfter, GalaxyID.getCPtr(userID), ILeaderboardEntriesRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C3F RID: 3135 RVA: 0x00019464 File Offset: 0x00017664
		public virtual void RequestLeaderboardEntriesAroundUser(string name, uint countBefore, uint countAfter, GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesAroundUser__SWIG_1(this.swigCPtr, name, countBefore, countAfter, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C40 RID: 3136 RVA: 0x0001948B File Offset: 0x0001768B
		public virtual void RequestLeaderboardEntriesAroundUser(string name, uint countBefore, uint countAfter)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesAroundUser__SWIG_2(this.swigCPtr, name, countBefore, countAfter);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C41 RID: 3137 RVA: 0x000194AC File Offset: 0x000176AC
		public virtual void RequestLeaderboardEntriesForUsers(string name, ref GalaxyID[] userArray, ILeaderboardEntriesRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesForUsers__SWIG_0(this.swigCPtr, name, Array.ConvertAll<GalaxyID, ulong>(userArray, (GalaxyID id) => id.ToUint64()), (uint)userArray.LongLength, ILeaderboardEntriesRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C42 RID: 3138 RVA: 0x00019508 File Offset: 0x00017708
		public virtual void RequestLeaderboardEntriesForUsers(string name, ref GalaxyID[] userArray)
		{
			GalaxyInstancePINVOKE.IStats_RequestLeaderboardEntriesForUsers__SWIG_1(this.swigCPtr, name, Array.ConvertAll<GalaxyID, ulong>(userArray, (GalaxyID id) => id.ToUint64()), (uint)userArray.LongLength);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C43 RID: 3139 RVA: 0x0001955D File Offset: 0x0001775D
		public virtual void GetRequestedLeaderboardEntry(uint index, ref uint rank, ref int score, ref GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IStats_GetRequestedLeaderboardEntry(this.swigCPtr, index, ref rank, ref score, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C44 RID: 3140 RVA: 0x00019585 File Offset: 0x00017785
		public virtual void GetRequestedLeaderboardEntryWithDetails(uint index, ref uint rank, ref int score, byte[] details, uint detailsSize, ref uint outDetailsSize, ref GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IStats_GetRequestedLeaderboardEntryWithDetails(this.swigCPtr, index, ref rank, ref score, details, detailsSize, ref outDetailsSize, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C45 RID: 3141 RVA: 0x000195B3 File Offset: 0x000177B3
		public virtual void SetLeaderboardScore(string name, int score, bool forceUpdate, ILeaderboardScoreUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IStats_SetLeaderboardScore__SWIG_0(this.swigCPtr, name, score, forceUpdate, ILeaderboardScoreUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C46 RID: 3142 RVA: 0x000195DA File Offset: 0x000177DA
		public virtual void SetLeaderboardScore(string name, int score, bool forceUpdate)
		{
			GalaxyInstancePINVOKE.IStats_SetLeaderboardScore__SWIG_1(this.swigCPtr, name, score, forceUpdate);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C47 RID: 3143 RVA: 0x000195FA File Offset: 0x000177FA
		public virtual void SetLeaderboardScore(string name, int score)
		{
			GalaxyInstancePINVOKE.IStats_SetLeaderboardScore__SWIG_2(this.swigCPtr, name, score);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C48 RID: 3144 RVA: 0x00019619 File Offset: 0x00017819
		public virtual void SetLeaderboardScoreWithDetails(string name, int score, byte[] details, uint detailsSize, bool forceUpdate, ILeaderboardScoreUpdateListener listener)
		{
			GalaxyInstancePINVOKE.IStats_SetLeaderboardScoreWithDetails__SWIG_0(this.swigCPtr, name, score, details, detailsSize, forceUpdate, ILeaderboardScoreUpdateListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C49 RID: 3145 RVA: 0x00019644 File Offset: 0x00017844
		public virtual void SetLeaderboardScoreWithDetails(string name, int score, byte[] details, uint detailsSize, bool forceUpdate)
		{
			GalaxyInstancePINVOKE.IStats_SetLeaderboardScoreWithDetails__SWIG_1(this.swigCPtr, name, score, details, detailsSize, forceUpdate);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C4A RID: 3146 RVA: 0x00019668 File Offset: 0x00017868
		public virtual void SetLeaderboardScoreWithDetails(string name, int score, byte[] details, uint detailsSize)
		{
			GalaxyInstancePINVOKE.IStats_SetLeaderboardScoreWithDetails__SWIG_2(this.swigCPtr, name, score, details, detailsSize);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C4B RID: 3147 RVA: 0x0001968C File Offset: 0x0001788C
		public virtual uint GetLeaderboardEntryCount(string name)
		{
			uint result = GalaxyInstancePINVOKE.IStats_GetLeaderboardEntryCount(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C4C RID: 3148 RVA: 0x000196B7 File Offset: 0x000178B7
		public virtual void FindLeaderboard(string name, ILeaderboardRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_FindLeaderboard__SWIG_0(this.swigCPtr, name, ILeaderboardRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C4D RID: 3149 RVA: 0x000196DB File Offset: 0x000178DB
		public virtual void FindLeaderboard(string name)
		{
			GalaxyInstancePINVOKE.IStats_FindLeaderboard__SWIG_1(this.swigCPtr, name);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C4E RID: 3150 RVA: 0x000196F9 File Offset: 0x000178F9
		public virtual void FindOrCreateLeaderboard(string name, string displayName, LeaderboardSortMethod sortMethod, LeaderboardDisplayType displayType, ILeaderboardRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_FindOrCreateLeaderboard__SWIG_0(this.swigCPtr, name, displayName, (int)sortMethod, (int)displayType, ILeaderboardRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C4F RID: 3151 RVA: 0x00019722 File Offset: 0x00017922
		public virtual void FindOrCreateLeaderboard(string name, string displayName, LeaderboardSortMethod sortMethod, LeaderboardDisplayType displayType)
		{
			GalaxyInstancePINVOKE.IStats_FindOrCreateLeaderboard__SWIG_1(this.swigCPtr, name, displayName, (int)sortMethod, (int)displayType);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C50 RID: 3152 RVA: 0x00019744 File Offset: 0x00017944
		public virtual void RequestUserTimePlayed(GalaxyID userID, IUserTimePlayedRetrieveListener listener)
		{
			GalaxyInstancePINVOKE.IStats_RequestUserTimePlayed__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID), IUserTimePlayedRetrieveListener.getCPtr(listener));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C51 RID: 3153 RVA: 0x0001976D File Offset: 0x0001796D
		public virtual void RequestUserTimePlayed(GalaxyID userID)
		{
			GalaxyInstancePINVOKE.IStats_RequestUserTimePlayed__SWIG_1(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C52 RID: 3154 RVA: 0x00019790 File Offset: 0x00017990
		public virtual void RequestUserTimePlayed()
		{
			GalaxyInstancePINVOKE.IStats_RequestUserTimePlayed__SWIG_2(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
		}

		// Token: 0x06000C53 RID: 3155 RVA: 0x000197B0 File Offset: 0x000179B0
		public virtual uint GetUserTimePlayed(GalaxyID userID)
		{
			uint result = GalaxyInstancePINVOKE.IStats_GetUserTimePlayed__SWIG_0(this.swigCPtr, GalaxyID.getCPtr(userID));
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x06000C54 RID: 3156 RVA: 0x000197E0 File Offset: 0x000179E0
		public virtual uint GetUserTimePlayed()
		{
			uint result = GalaxyInstancePINVOKE.IStats_GetUserTimePlayed__SWIG_1(this.swigCPtr);
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			return result;
		}

		// Token: 0x0400025D RID: 605
		private HandleRef swigCPtr;

		// Token: 0x0400025E RID: 606
		protected bool swigCMemOwn;
	}
}
