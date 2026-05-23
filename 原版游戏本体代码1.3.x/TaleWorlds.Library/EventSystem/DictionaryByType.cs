using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.EventSystem
{
	// Token: 0x020000B9 RID: 185
	public class DictionaryByType
	{
		// Token: 0x060006E0 RID: 1760 RVA: 0x00017488 File Offset: 0x00015688
		public void Add<T>(Action<T> value)
		{
			object obj;
			if (!this._eventsByType.TryGetValue(typeof(T), out obj))
			{
				obj = new List<Action<T>>();
				this._eventsByType[typeof(T)] = obj;
			}
			((List<Action<T>>)obj).Add(value);
		}

		// Token: 0x060006E1 RID: 1761 RVA: 0x000174D8 File Offset: 0x000156D8
		public void Remove<T>(Action<T> value)
		{
			object obj;
			if (this._eventsByType.TryGetValue(typeof(T), out obj))
			{
				List<Action<T>> list = (List<Action<T>>)obj;
				list.Remove(value);
				this._eventsByType[typeof(T)] = list;
				return;
			}
			Debug.FailedAssert("Event: " + typeof(T).Name + " were not registered in the first place", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\EventSystem\\EventManager.cs", "Remove", 106);
		}

		// Token: 0x060006E2 RID: 1762 RVA: 0x00017554 File Offset: 0x00015754
		public void InvokeActions<T>(T item)
		{
			object obj;
			if (this._eventsByType.TryGetValue(typeof(T), out obj))
			{
				foreach (Action<T> action in ((List<Action<T>>)obj))
				{
					action(item);
				}
			}
		}

		// Token: 0x060006E3 RID: 1763 RVA: 0x000175C0 File Offset: 0x000157C0
		public List<Action<T>> Get<T>()
		{
			return (List<Action<T>>)this._eventsByType[typeof(T)];
		}

		// Token: 0x060006E4 RID: 1764 RVA: 0x000175DC File Offset: 0x000157DC
		public bool TryGet<T>(out List<Action<T>> value)
		{
			object obj;
			if (this._eventsByType.TryGetValue(typeof(T), out obj))
			{
				value = (List<Action<T>>)obj;
				return true;
			}
			value = null;
			return false;
		}

		// Token: 0x060006E5 RID: 1765 RVA: 0x00017610 File Offset: 0x00015810
		public IDictionary<Type, object> GetClone()
		{
			return new Dictionary<Type, object>(this._eventsByType);
		}

		// Token: 0x060006E6 RID: 1766 RVA: 0x0001761D File Offset: 0x0001581D
		public void Clear()
		{
			this._eventsByType.Clear();
		}

		// Token: 0x0400021B RID: 539
		private readonly IDictionary<Type, object> _eventsByType = new Dictionary<Type, object>();
	}
}
