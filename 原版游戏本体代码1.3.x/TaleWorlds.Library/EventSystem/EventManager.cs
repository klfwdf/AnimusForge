using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.EventSystem
{
	// Token: 0x020000B8 RID: 184
	public class EventManager
	{
		// Token: 0x060006DA RID: 1754 RVA: 0x000173CD File Offset: 0x000155CD
		public EventManager()
		{
			this._eventsByType = new DictionaryByType();
		}

		// Token: 0x060006DB RID: 1755 RVA: 0x000173E0 File Offset: 0x000155E0
		public void RegisterEvent<T>(Action<T> eventObjType)
		{
			if (typeof(T).IsSubclassOf(typeof(EventBase)))
			{
				this._eventsByType.Add<T>(eventObjType);
				return;
			}
			Debug.FailedAssert("Events have to derived from EventSystemBase", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\EventSystem\\EventManager.cs", "RegisterEvent", 31);
		}

		// Token: 0x060006DC RID: 1756 RVA: 0x00017420 File Offset: 0x00015620
		public void UnregisterEvent<T>(Action<T> eventObjType)
		{
			if (typeof(T).IsSubclassOf(typeof(EventBase)))
			{
				this._eventsByType.Remove<T>(eventObjType);
				return;
			}
			Debug.FailedAssert("Events have to derived from EventSystemBase", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\EventSystem\\EventManager.cs", "UnregisterEvent", 48);
		}

		// Token: 0x060006DD RID: 1757 RVA: 0x00017460 File Offset: 0x00015660
		public void TriggerEvent<T>(T eventObj)
		{
			this._eventsByType.InvokeActions<T>(eventObj);
		}

		// Token: 0x060006DE RID: 1758 RVA: 0x0001746E File Offset: 0x0001566E
		public void Clear()
		{
			this._eventsByType.Clear();
		}

		// Token: 0x060006DF RID: 1759 RVA: 0x0001747B File Offset: 0x0001567B
		public IDictionary<Type, object> GetCloneOfEventDictionary()
		{
			return this._eventsByType.GetClone();
		}

		// Token: 0x0400021A RID: 538
		private readonly DictionaryByType _eventsByType;
	}
}
