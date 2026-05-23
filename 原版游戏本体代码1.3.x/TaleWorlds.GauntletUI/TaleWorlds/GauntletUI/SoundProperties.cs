using System;
using System.Collections.Generic;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200002F RID: 47
	public class SoundProperties
	{
		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600034C RID: 844 RVA: 0x0000EBD3 File Offset: 0x0000CDD3
		public IEnumerable<KeyValuePair<string, AudioProperty>> RegisteredStateSounds
		{
			get
			{
				foreach (KeyValuePair<string, AudioProperty> keyValuePair in this._stateSounds)
				{
					yield return keyValuePair;
				}
				Dictionary<string, AudioProperty>.Enumerator enumerator = default(Dictionary<string, AudioProperty>.Enumerator);
				yield break;
				yield break;
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600034D RID: 845 RVA: 0x0000EBE3 File Offset: 0x0000CDE3
		public IEnumerable<KeyValuePair<string, AudioProperty>> RegisteredEventSounds
		{
			get
			{
				foreach (KeyValuePair<string, AudioProperty> keyValuePair in this._eventSounds)
				{
					yield return keyValuePair;
				}
				Dictionary<string, AudioProperty>.Enumerator enumerator = default(Dictionary<string, AudioProperty>.Enumerator);
				yield break;
				yield break;
			}
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0000EBF3 File Offset: 0x0000CDF3
		public SoundProperties()
		{
			this._stateSounds = new Dictionary<string, AudioProperty>();
			this._eventSounds = new Dictionary<string, AudioProperty>();
		}

		// Token: 0x0600034F RID: 847 RVA: 0x0000EC11 File Offset: 0x0000CE11
		public void AddStateSound(string state, AudioProperty audioProperty)
		{
			this._stateSounds.Add(state, audioProperty);
		}

		// Token: 0x06000350 RID: 848 RVA: 0x0000EC20 File Offset: 0x0000CE20
		public void AddEventSound(string state, AudioProperty audioProperty)
		{
			if (this._eventSounds.ContainsKey(state))
			{
				this._eventSounds[state] = audioProperty;
				return;
			}
			this._eventSounds.Add(state, audioProperty);
		}

		// Token: 0x06000351 RID: 849 RVA: 0x0000EC4C File Offset: 0x0000CE4C
		public void FillFrom(SoundProperties soundProperties)
		{
			this._stateSounds = new Dictionary<string, AudioProperty>();
			this._eventSounds = new Dictionary<string, AudioProperty>();
			foreach (KeyValuePair<string, AudioProperty> keyValuePair in soundProperties._stateSounds)
			{
				string key = keyValuePair.Key;
				AudioProperty value = keyValuePair.Value;
				AudioProperty audioProperty = new AudioProperty();
				audioProperty.FillFrom(value);
				this._stateSounds.Add(key, audioProperty);
			}
			foreach (KeyValuePair<string, AudioProperty> keyValuePair2 in soundProperties._eventSounds)
			{
				string key2 = keyValuePair2.Key;
				AudioProperty value2 = keyValuePair2.Value;
				AudioProperty audioProperty2 = new AudioProperty();
				audioProperty2.FillFrom(value2);
				this._eventSounds.Add(key2, audioProperty2);
			}
		}

		// Token: 0x06000352 RID: 850 RVA: 0x0000ED48 File Offset: 0x0000CF48
		public AudioProperty GetEventAudioProperty(string eventName)
		{
			if (this._eventSounds.ContainsKey(eventName))
			{
				return this._eventSounds[eventName];
			}
			return null;
		}

		// Token: 0x06000353 RID: 851 RVA: 0x0000ED66 File Offset: 0x0000CF66
		public AudioProperty GetStateAudioProperty(string stateName)
		{
			if (this._stateSounds.ContainsKey(stateName))
			{
				return this._stateSounds[stateName];
			}
			return null;
		}

		// Token: 0x0400019E RID: 414
		private Dictionary<string, AudioProperty> _stateSounds;

		// Token: 0x0400019F RID: 415
		private Dictionary<string, AudioProperty> _eventSounds;
	}
}
