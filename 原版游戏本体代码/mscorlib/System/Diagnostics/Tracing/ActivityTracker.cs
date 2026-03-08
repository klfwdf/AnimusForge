using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200041B RID: 1051
	internal class ActivityTracker
	{
		// Token: 0x0600343A RID: 13370 RVA: 0x000C6E20 File Offset: 0x000C5020
		public void OnStart(string providerName, string activityName, int task, ref Guid activityId, ref Guid relatedActivityId, EventActivityOptions options)
		{
			if (this.m_current == null)
			{
				if (this.m_checkedForEnable)
				{
					return;
				}
				this.m_checkedForEnable = true;
				if (TplEtwProvider.Log.IsEnabled(EventLevel.Informational, (EventKeywords)128L))
				{
					this.Enable();
				}
				if (this.m_current == null)
				{
					return;
				}
			}
			ActivityTracker.ActivityInfo value = this.m_current.Value;
			string text = this.NormalizeActivityName(providerName, activityName, task);
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.Debug)
			{
				log.DebugFacilityMessage("OnStartEnter", text);
				log.DebugFacilityMessage("OnStartEnterActivityState", ActivityTracker.ActivityInfo.LiveActivities(value));
			}
			if (value != null)
			{
				if (value.m_level >= 100)
				{
					activityId = Guid.Empty;
					relatedActivityId = Guid.Empty;
					if (log.Debug)
					{
						log.DebugFacilityMessage("OnStartRET", "Fail");
					}
					return;
				}
				if ((options & EventActivityOptions.Recursive) == EventActivityOptions.None)
				{
					ActivityTracker.ActivityInfo activityInfo = this.FindActiveActivity(text, value);
					if (activityInfo != null)
					{
						this.OnStop(providerName, activityName, task, ref activityId);
						value = this.m_current.Value;
					}
				}
			}
			long uniqueId;
			if (value == null)
			{
				uniqueId = Interlocked.Increment(ref ActivityTracker.m_nextId);
			}
			else
			{
				uniqueId = Interlocked.Increment(ref value.m_lastChildID);
			}
			relatedActivityId = EventSource.CurrentThreadActivityId;
			ActivityTracker.ActivityInfo activityInfo2 = new ActivityTracker.ActivityInfo(text, uniqueId, value, relatedActivityId, options);
			this.m_current.Value = activityInfo2;
			activityId = activityInfo2.ActivityId;
			if (log.Debug)
			{
				log.DebugFacilityMessage("OnStartRetActivityState", ActivityTracker.ActivityInfo.LiveActivities(activityInfo2));
				log.DebugFacilityMessage1("OnStartRet", activityId.ToString(), relatedActivityId.ToString());
			}
		}

		// Token: 0x0600343B RID: 13371 RVA: 0x000C6FA8 File Offset: 0x000C51A8
		public void OnStop(string providerName, string activityName, int task, ref Guid activityId)
		{
			if (this.m_current == null)
			{
				return;
			}
			string text = this.NormalizeActivityName(providerName, activityName, task);
			TplEtwProvider log = TplEtwProvider.Log;
			if (log.Debug)
			{
				log.DebugFacilityMessage("OnStopEnter", text);
				log.DebugFacilityMessage("OnStopEnterActivityState", ActivityTracker.ActivityInfo.LiveActivities(this.m_current.Value));
			}
			ActivityTracker.ActivityInfo activityInfo;
			for (;;)
			{
				ActivityTracker.ActivityInfo value = this.m_current.Value;
				activityInfo = null;
				ActivityTracker.ActivityInfo activityInfo2 = this.FindActiveActivity(text, value);
				if (activityInfo2 == null)
				{
					break;
				}
				activityId = activityInfo2.ActivityId;
				ActivityTracker.ActivityInfo activityInfo3 = value;
				while (activityInfo3 != activityInfo2 && activityInfo3 != null)
				{
					if (activityInfo3.m_stopped != 0)
					{
						activityInfo3 = activityInfo3.m_creator;
					}
					else
					{
						if (activityInfo3.CanBeOrphan())
						{
							if (activityInfo == null)
							{
								activityInfo = activityInfo3;
							}
						}
						else
						{
							activityInfo3.m_stopped = 1;
						}
						activityInfo3 = activityInfo3.m_creator;
					}
				}
				if (Interlocked.CompareExchange(ref activityInfo2.m_stopped, 1, 0) == 0)
				{
					goto Block_9;
				}
			}
			activityId = Guid.Empty;
			if (log.Debug)
			{
				log.DebugFacilityMessage("OnStopRET", "Fail");
			}
			return;
			Block_9:
			if (activityInfo == null)
			{
				ActivityTracker.ActivityInfo activityInfo2;
				activityInfo = activityInfo2.m_creator;
			}
			this.m_current.Value = activityInfo;
			if (log.Debug)
			{
				log.DebugFacilityMessage("OnStopRetActivityState", ActivityTracker.ActivityInfo.LiveActivities(activityInfo));
				log.DebugFacilityMessage("OnStopRet", activityId.ToString());
			}
		}

		// Token: 0x0600343C RID: 13372 RVA: 0x000C70EC File Offset: 0x000C52EC
		[SecuritySafeCritical]
		public void Enable()
		{
			if (this.m_current == null)
			{
				this.m_current = new AsyncLocal<ActivityTracker.ActivityInfo>(new Action<AsyncLocalValueChangedArgs<ActivityTracker.ActivityInfo>>(this.ActivityChanging));
			}
		}

		// Token: 0x170007B9 RID: 1977
		// (get) Token: 0x0600343D RID: 13373 RVA: 0x000C710D File Offset: 0x000C530D
		public static ActivityTracker Instance
		{
			get
			{
				return ActivityTracker.s_activityTrackerInstance;
			}
		}

		// Token: 0x170007BA RID: 1978
		// (get) Token: 0x0600343E RID: 13374 RVA: 0x000C7114 File Offset: 0x000C5314
		private Guid CurrentActivityId
		{
			get
			{
				return this.m_current.Value.ActivityId;
			}
		}

		// Token: 0x0600343F RID: 13375 RVA: 0x000C7128 File Offset: 0x000C5328
		private ActivityTracker.ActivityInfo FindActiveActivity(string name, ActivityTracker.ActivityInfo startLocation)
		{
			for (ActivityTracker.ActivityInfo activityInfo = startLocation; activityInfo != null; activityInfo = activityInfo.m_creator)
			{
				if (name == activityInfo.m_name && activityInfo.m_stopped == 0)
				{
					return activityInfo;
				}
			}
			return null;
		}

		// Token: 0x06003440 RID: 13376 RVA: 0x000C715C File Offset: 0x000C535C
		private string NormalizeActivityName(string providerName, string activityName, int task)
		{
			if (activityName.EndsWith("Start"))
			{
				activityName = activityName.Substring(0, activityName.Length - "Start".Length);
			}
			else if (activityName.EndsWith("Stop"))
			{
				activityName = activityName.Substring(0, activityName.Length - "Stop".Length);
			}
			else if (task != 0)
			{
				activityName = "task" + task.ToString();
			}
			return providerName + activityName;
		}

		// Token: 0x06003441 RID: 13377 RVA: 0x000C71D8 File Offset: 0x000C53D8
		private void ActivityChanging(AsyncLocalValueChangedArgs<ActivityTracker.ActivityInfo> args)
		{
			ActivityTracker.ActivityInfo activityInfo = args.CurrentValue;
			ActivityTracker.ActivityInfo previousValue = args.PreviousValue;
			if (previousValue != null && previousValue.m_creator == activityInfo && (activityInfo == null || previousValue.m_activityIdToRestore != activityInfo.ActivityId))
			{
				EventSource.SetCurrentThreadActivityId(previousValue.m_activityIdToRestore);
				return;
			}
			while (activityInfo != null)
			{
				if (activityInfo.m_stopped == 0)
				{
					EventSource.SetCurrentThreadActivityId(activityInfo.ActivityId);
					return;
				}
				activityInfo = activityInfo.m_creator;
			}
		}

		// Token: 0x04001728 RID: 5928
		private AsyncLocal<ActivityTracker.ActivityInfo> m_current;

		// Token: 0x04001729 RID: 5929
		private bool m_checkedForEnable;

		// Token: 0x0400172A RID: 5930
		private static ActivityTracker s_activityTrackerInstance = new ActivityTracker();

		// Token: 0x0400172B RID: 5931
		private static long m_nextId = 0L;

		// Token: 0x0400172C RID: 5932
		private const ushort MAX_ACTIVITY_DEPTH = 100;

		// Token: 0x02000B85 RID: 2949
		private class ActivityInfo
		{
			// Token: 0x06006C62 RID: 27746 RVA: 0x00177554 File Offset: 0x00175754
			public ActivityInfo(string name, long uniqueId, ActivityTracker.ActivityInfo creator, Guid activityIDToRestore, EventActivityOptions options)
			{
				this.m_name = name;
				this.m_eventOptions = options;
				this.m_creator = creator;
				this.m_uniqueId = uniqueId;
				this.m_level = ((creator != null) ? (creator.m_level + 1) : 0);
				this.m_activityIdToRestore = activityIDToRestore;
				this.CreateActivityPathGuid(out this.m_guid, out this.m_activityPathGuidOffset);
			}

			// Token: 0x1700125B RID: 4699
			// (get) Token: 0x06006C63 RID: 27747 RVA: 0x001775B2 File Offset: 0x001757B2
			public Guid ActivityId
			{
				get
				{
					return this.m_guid;
				}
			}

			// Token: 0x06006C64 RID: 27748 RVA: 0x001775BC File Offset: 0x001757BC
			public static string Path(ActivityTracker.ActivityInfo activityInfo)
			{
				if (activityInfo == null)
				{
					return "";
				}
				return ActivityTracker.ActivityInfo.Path(activityInfo.m_creator) + "/" + activityInfo.m_uniqueId.ToString();
			}

			// Token: 0x06006C65 RID: 27749 RVA: 0x001775F8 File Offset: 0x001757F8
			public override string ToString()
			{
				string text = "";
				if (this.m_stopped != 0)
				{
					text = ",DEAD";
				}
				return string.Concat(new string[]
				{
					this.m_name,
					"(",
					ActivityTracker.ActivityInfo.Path(this),
					text,
					")"
				});
			}

			// Token: 0x06006C66 RID: 27750 RVA: 0x0017764A File Offset: 0x0017584A
			public static string LiveActivities(ActivityTracker.ActivityInfo list)
			{
				if (list == null)
				{
					return "";
				}
				return list.ToString() + ";" + ActivityTracker.ActivityInfo.LiveActivities(list.m_creator);
			}

			// Token: 0x06006C67 RID: 27751 RVA: 0x00177670 File Offset: 0x00175870
			public bool CanBeOrphan()
			{
				return (this.m_eventOptions & EventActivityOptions.Detachable) != EventActivityOptions.None;
			}

			// Token: 0x06006C68 RID: 27752 RVA: 0x00177680 File Offset: 0x00175880
			[SecuritySafeCritical]
			private unsafe void CreateActivityPathGuid(out Guid idRet, out int activityPathGuidOffset)
			{
				fixed (Guid* ptr = &idRet)
				{
					Guid* outPtr = ptr;
					int whereToAddId = 0;
					if (this.m_creator != null)
					{
						whereToAddId = this.m_creator.m_activityPathGuidOffset;
						idRet = this.m_creator.m_guid;
					}
					else
					{
						int domainID = Thread.GetDomainID();
						whereToAddId = ActivityTracker.ActivityInfo.AddIdToGuid(outPtr, whereToAddId, (uint)domainID, false);
					}
					activityPathGuidOffset = ActivityTracker.ActivityInfo.AddIdToGuid(outPtr, whereToAddId, (uint)this.m_uniqueId, false);
					if (12 < activityPathGuidOffset)
					{
						this.CreateOverflowGuid(outPtr);
					}
				}
			}

			// Token: 0x06006C69 RID: 27753 RVA: 0x001776F0 File Offset: 0x001758F0
			[SecurityCritical]
			private unsafe void CreateOverflowGuid(Guid* outPtr)
			{
				for (ActivityTracker.ActivityInfo creator = this.m_creator; creator != null; creator = creator.m_creator)
				{
					if (creator.m_activityPathGuidOffset <= 10)
					{
						uint id = (uint)Interlocked.Increment(ref creator.m_lastChildID);
						*outPtr = creator.m_guid;
						int num = ActivityTracker.ActivityInfo.AddIdToGuid(outPtr, creator.m_activityPathGuidOffset, id, true);
						if (num <= 12)
						{
							break;
						}
					}
				}
			}

			// Token: 0x06006C6A RID: 27754 RVA: 0x00177748 File Offset: 0x00175948
			[SecurityCritical]
			private unsafe static int AddIdToGuid(Guid* outPtr, int whereToAddId, uint id, bool overflow = false)
			{
				byte* ptr = (byte*)outPtr;
				byte* ptr2 = ptr + 12;
				ptr += whereToAddId;
				if (ptr2 == ptr)
				{
					return 13;
				}
				if (0U < id && id <= 10U && !overflow)
				{
					ActivityTracker.ActivityInfo.WriteNibble(ref ptr, ptr2, id);
				}
				else
				{
					uint num = 4U;
					if (id <= 255U)
					{
						num = 1U;
					}
					else if (id <= 65535U)
					{
						num = 2U;
					}
					else if (id <= 16777215U)
					{
						num = 3U;
					}
					if (overflow)
					{
						if (ptr2 == ptr + 2)
						{
							return 13;
						}
						ActivityTracker.ActivityInfo.WriteNibble(ref ptr, ptr2, 11U);
					}
					ActivityTracker.ActivityInfo.WriteNibble(ref ptr, ptr2, 12U + (num - 1U));
					if (ptr < ptr2 && *ptr != 0)
					{
						if (id < 4096U)
						{
							*ptr = (byte)(192U + (id >> 8));
							id &= 255U;
						}
						ptr++;
					}
					while (0U < num)
					{
						if (ptr2 == ptr)
						{
							ptr++;
							break;
						}
						*(ptr++) = (byte)id;
						id >>= 8;
						num -= 1U;
					}
				}
				*(int*)(outPtr + (IntPtr)3 * 4 / (IntPtr)sizeof(Guid)) = (int)(*(uint*)outPtr + *(uint*)(outPtr + 4 / sizeof(Guid)) + *(uint*)(outPtr + (IntPtr)2 * 4 / (IntPtr)sizeof(Guid)) + 1503500717U);
				return (int)((long)((byte*)ptr - (byte*)outPtr));
			}

			// Token: 0x06006C6B RID: 27755 RVA: 0x00177838 File Offset: 0x00175A38
			[SecurityCritical]
			private unsafe static void WriteNibble(ref byte* ptr, byte* endPtr, uint value)
			{
				if (*ptr != 0)
				{
					byte* ptr2 = ptr;
					ptr = ptr2 + 1;
					byte* ptr3 = ptr2;
					*ptr3 |= (byte)value;
					return;
				}
				*ptr = (byte)(value << 4);
			}

			// Token: 0x040034E4 RID: 13540
			internal readonly string m_name;

			// Token: 0x040034E5 RID: 13541
			private readonly long m_uniqueId;

			// Token: 0x040034E6 RID: 13542
			internal readonly Guid m_guid;

			// Token: 0x040034E7 RID: 13543
			internal readonly int m_activityPathGuidOffset;

			// Token: 0x040034E8 RID: 13544
			internal readonly int m_level;

			// Token: 0x040034E9 RID: 13545
			internal readonly EventActivityOptions m_eventOptions;

			// Token: 0x040034EA RID: 13546
			internal long m_lastChildID;

			// Token: 0x040034EB RID: 13547
			internal int m_stopped;

			// Token: 0x040034EC RID: 13548
			internal readonly ActivityTracker.ActivityInfo m_creator;

			// Token: 0x040034ED RID: 13549
			internal readonly Guid m_activityIdToRestore;

			// Token: 0x02000D04 RID: 3332
			private enum NumberListCodes : byte
			{
				// Token: 0x04003935 RID: 14645
				End,
				// Token: 0x04003936 RID: 14646
				LastImmediateValue = 10,
				// Token: 0x04003937 RID: 14647
				PrefixCode,
				// Token: 0x04003938 RID: 14648
				MultiByte1
			}
		}
	}
}
