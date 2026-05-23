using System.Collections.Generic;

namespace TaleWorlds.GauntletUI;

public class SoundProperties
{
	private Dictionary<string, AudioProperty> _stateSounds;

	private Dictionary<string, AudioProperty> _eventSounds;

	public IEnumerable<KeyValuePair<string, AudioProperty>> RegisteredStateSounds
	{
		get
		{
			foreach (KeyValuePair<string, AudioProperty> stateSound in _stateSounds)
			{
				yield return stateSound;
			}
		}
	}

	public IEnumerable<KeyValuePair<string, AudioProperty>> RegisteredEventSounds
	{
		get
		{
			foreach (KeyValuePair<string, AudioProperty> eventSound in _eventSounds)
			{
				yield return eventSound;
			}
		}
	}

	public SoundProperties()
	{
		_stateSounds = new Dictionary<string, AudioProperty>();
		_eventSounds = new Dictionary<string, AudioProperty>();
	}

	public void AddStateSound(string state, AudioProperty audioProperty)
	{
		_stateSounds.Add(state, audioProperty);
	}

	public void AddEventSound(string state, AudioProperty audioProperty)
	{
		if (_eventSounds.ContainsKey(state))
		{
			_eventSounds[state] = audioProperty;
		}
		else
		{
			_eventSounds.Add(state, audioProperty);
		}
	}

	public void FillFrom(SoundProperties soundProperties)
	{
		_stateSounds = new Dictionary<string, AudioProperty>();
		_eventSounds = new Dictionary<string, AudioProperty>();
		foreach (KeyValuePair<string, AudioProperty> stateSound in soundProperties._stateSounds)
		{
			string key = stateSound.Key;
			AudioProperty value = stateSound.Value;
			AudioProperty audioProperty = new AudioProperty();
			audioProperty.FillFrom(value);
			_stateSounds.Add(key, audioProperty);
		}
		foreach (KeyValuePair<string, AudioProperty> eventSound in soundProperties._eventSounds)
		{
			string key2 = eventSound.Key;
			AudioProperty value2 = eventSound.Value;
			AudioProperty audioProperty2 = new AudioProperty();
			audioProperty2.FillFrom(value2);
			_eventSounds.Add(key2, audioProperty2);
		}
	}

	public AudioProperty GetEventAudioProperty(string eventName)
	{
		if (_eventSounds.ContainsKey(eventName))
		{
			return _eventSounds[eventName];
		}
		return null;
	}

	public AudioProperty GetStateAudioProperty(string stateName)
	{
		if (_stateSounds.ContainsKey(stateName))
		{
			return _stateSounds[stateName];
		}
		return null;
	}
}
