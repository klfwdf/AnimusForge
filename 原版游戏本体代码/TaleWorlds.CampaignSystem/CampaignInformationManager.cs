using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000058 RID: 88
	public class CampaignInformationManager
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060008C5 RID: 2245 RVA: 0x00026A14 File Offset: 0x00024C14
		// (remove) Token: 0x060008C6 RID: 2246 RVA: 0x00026A48 File Offset: 0x00024C48
		public static event Func<TextObject, int, BasicCharacterObject, Equipment, MBInformationManager.NotificationPriority, string, MBInformationManager.DialogNotificationHandle> OnDisplayDialog;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x060008C7 RID: 2247 RVA: 0x00026A7C File Offset: 0x00024C7C
		// (remove) Token: 0x060008C8 RID: 2248 RVA: 0x00026AB0 File Offset: 0x00024CB0
		public static event Func<MBInformationManager.DialogNotificationHandle, MBInformationManager.NotificationStatus> OnGetStatusOfDialogNotification;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x060008C9 RID: 2249 RVA: 0x00026AE4 File Offset: 0x00024CE4
		// (remove) Token: 0x060008CA RID: 2250 RVA: 0x00026B18 File Offset: 0x00024D18
		public static event Action<MBInformationManager.DialogNotificationHandle, bool> OnClearDialogNotification;

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x060008CB RID: 2251 RVA: 0x00026B4C File Offset: 0x00024D4C
		// (remove) Token: 0x060008CC RID: 2252 RVA: 0x00026B80 File Offset: 0x00024D80
		public static event Func<bool> IsAnyDialogNotificationActiveOrQueued;

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x060008CD RID: 2253 RVA: 0x00026BB4 File Offset: 0x00024DB4
		// (remove) Token: 0x060008CE RID: 2254 RVA: 0x00026BE8 File Offset: 0x00024DE8
		public static event Action<bool> OnClearAllDialogNotifications;

		// Token: 0x060008CF RID: 2255 RVA: 0x00026C1B File Offset: 0x00024E1B
		public CampaignInformationManager()
		{
			this._mapNotices = new List<InformationData>();
		}

		// Token: 0x060008D0 RID: 2256 RVA: 0x00026C30 File Offset: 0x00024E30
		private void MapNoticeRemoved(InformationData obj)
		{
			int num = -1;
			for (int i = 0; i < this._mapNotices.Count; i++)
			{
				if (obj == this._mapNotices[i])
				{
					num = i;
				}
			}
			if (num >= 0)
			{
				this._mapNotices.RemoveAt(num);
			}
		}

		// Token: 0x060008D1 RID: 2257 RVA: 0x00026C78 File Offset: 0x00024E78
		internal void NewLogEntryAdded(LogEntry log)
		{
			IChatNotification chatNotification;
			if (this._isSessionLaunched && (chatNotification = log as IChatNotification) != null && chatNotification.IsVisibleNotification)
			{
				InformationManager.DisplayMessage(new InformationMessage
				{
					Information = chatNotification.GetNotificationText().ToString(),
					Color = Color.FromUint(Campaign.Current.Models.DiplomacyModel.GetNotificationColor(chatNotification.NotificationType))
				});
			}
		}

		// Token: 0x060008D2 RID: 2258 RVA: 0x00026CDF File Offset: 0x00024EDF
		private void AddInformationData(InformationData informationData)
		{
			List<InformationData> mapNotices = this._mapNotices;
			if (mapNotices != null)
			{
				mapNotices.Add(informationData);
			}
			MBInformationManager.AddNotice(informationData);
		}

		// Token: 0x060008D3 RID: 2259 RVA: 0x00026CF9 File Offset: 0x00024EF9
		internal void RegisterEvents()
		{
			this._isSessionLaunched = true;
			MBInformationManager.OnRemoveMapNotice += this.MapNoticeRemoved;
		}

		// Token: 0x060008D4 RID: 2260 RVA: 0x00026D13 File Offset: 0x00024F13
		internal void DeRegisterEvents()
		{
			this._isSessionLaunched = false;
			MBInformationManager.OnRemoveMapNotice -= this.MapNoticeRemoved;
		}

		// Token: 0x060008D5 RID: 2261 RVA: 0x00026D30 File Offset: 0x00024F30
		public void OnGameLoaded()
		{
			this._mapNotices.RemoveAll((InformationData t) => t == null || !t.IsValid());
			foreach (InformationData data in this._mapNotices)
			{
				MBInformationManager.AddNotice(data);
			}
		}

		// Token: 0x060008D6 RID: 2262 RVA: 0x00026DAC File Offset: 0x00024FAC
		public void NewMapNoticeAdded(InformationData informationData)
		{
			this.AddInformationData(informationData);
		}

		// Token: 0x060008D7 RID: 2263 RVA: 0x00026DB8 File Offset: 0x00024FB8
		public bool InformationDataExists<T>(Func<T, bool> predicate) where T : InformationData
		{
			using (List<InformationData>.Enumerator enumerator = this._mapNotices.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					T arg;
					if ((arg = enumerator.Current as T) != null && (predicate == null || predicate(arg)))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060008D8 RID: 2264 RVA: 0x00026E2C File Offset: 0x0002502C
		public static MBInformationManager.DialogNotificationHandle AddDialogLine(TextObject text, CharacterObject speakerCharacter, Equipment equipment = null, int extraTimeInMs = 0, MBInformationManager.NotificationPriority priority = MBInformationManager.NotificationPriority.Medium)
		{
			Debug.Print(text.ToString(), 0, Debug.DebugColor.White, 4503599627370496UL);
			Func<TextObject, int, BasicCharacterObject, Equipment, MBInformationManager.NotificationPriority, string, MBInformationManager.DialogNotificationHandle> onDisplayDialog = CampaignInformationManager.OnDisplayDialog;
			return ((onDisplayDialog != null) ? onDisplayDialog(text, extraTimeInMs, speakerCharacter, equipment, priority, CampaignInformationManager.GetSoundPath(text, speakerCharacter)) : null) ?? null;
		}

		// Token: 0x060008D9 RID: 2265 RVA: 0x00026E68 File Offset: 0x00025068
		public static MBInformationManager.NotificationStatus GetStatusOfDialogNotification(MBInformationManager.DialogNotificationHandle handle)
		{
			Func<MBInformationManager.DialogNotificationHandle, MBInformationManager.NotificationStatus> onGetStatusOfDialogNotification = CampaignInformationManager.OnGetStatusOfDialogNotification;
			if (onGetStatusOfDialogNotification == null)
			{
				return MBInformationManager.NotificationStatus.Inactive;
			}
			return onGetStatusOfDialogNotification(handle);
		}

		// Token: 0x060008DA RID: 2266 RVA: 0x00026E7B File Offset: 0x0002507B
		public static void ClearDialogNotification(MBInformationManager.DialogNotificationHandle handle, bool fadeOut = true)
		{
			Action<MBInformationManager.DialogNotificationHandle, bool> onClearDialogNotification = CampaignInformationManager.OnClearDialogNotification;
			if (onClearDialogNotification == null)
			{
				return;
			}
			onClearDialogNotification(handle, fadeOut);
		}

		// Token: 0x060008DB RID: 2267 RVA: 0x00026E8E File Offset: 0x0002508E
		public static bool GetIsAnyDialogNotificationActiveOrQueued()
		{
			Func<bool> isAnyDialogNotificationActiveOrQueued = CampaignInformationManager.IsAnyDialogNotificationActiveOrQueued;
			return isAnyDialogNotificationActiveOrQueued != null && isAnyDialogNotificationActiveOrQueued();
		}

		// Token: 0x060008DC RID: 2268 RVA: 0x00026EA0 File Offset: 0x000250A0
		public static void ClearAllDialogNotifications(bool fadeOut)
		{
			Action<bool> onClearAllDialogNotifications = CampaignInformationManager.OnClearAllDialogNotifications;
			if (onClearAllDialogNotifications == null)
			{
				return;
			}
			onClearAllDialogNotifications(fadeOut);
		}

		// Token: 0x060008DD RID: 2269 RVA: 0x00026EB4 File Offset: 0x000250B4
		private static string GetSoundPath(TextObject line, CharacterObject characterObject)
		{
			VoiceObject voiceObject;
			string text;
			if (characterObject != null && MBTextManager.TryGetVoiceObject(line, out voiceObject, out text))
			{
				return Campaign.Current.Models.VoiceOverModel.GetSoundPathForCharacter(characterObject, voiceObject);
			}
			Debug.FailedAssert("Sound path for voice line not found! Character: " + ((characterObject != null) ? characterObject.ToString() : null) + ", Line: " + line.ToString(), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignInformationManager.cs", "GetSoundPath", 180);
			return null;
		}

		// Token: 0x060008DE RID: 2270 RVA: 0x00026F1D File Offset: 0x0002511D
		internal static void AutoGeneratedStaticCollectObjectsCampaignInformationManager(object o, List<object> collectedObjects)
		{
			((CampaignInformationManager)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x060008DF RID: 2271 RVA: 0x00026F2B File Offset: 0x0002512B
		protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			collectedObjects.Add(this._mapNotices);
		}

		// Token: 0x060008E0 RID: 2272 RVA: 0x00026F39 File Offset: 0x00025139
		internal static object AutoGeneratedGetMemberValue_mapNotices(object o)
		{
			return ((CampaignInformationManager)o)._mapNotices;
		}

		// Token: 0x040002C0 RID: 704
		[SaveableField(10)]
		private List<InformationData> _mapNotices;

		// Token: 0x040002C1 RID: 705
		[CachedData]
		private bool _isSessionLaunched;

		// Token: 0x02000510 RID: 1296
		public enum NoticeType
		{
			// Token: 0x0400158B RID: 5515
			None,
			// Token: 0x0400158C RID: 5516
			WarAnnouncement,
			// Token: 0x0400158D RID: 5517
			PeaceAnnouncement,
			// Token: 0x0400158E RID: 5518
			ChangeSettlementOwner,
			// Token: 0x0400158F RID: 5519
			FortificationIsCaptured,
			// Token: 0x04001590 RID: 5520
			HeroChangedFaction,
			// Token: 0x04001591 RID: 5521
			BarterAnnouncement
		}
	}
}
