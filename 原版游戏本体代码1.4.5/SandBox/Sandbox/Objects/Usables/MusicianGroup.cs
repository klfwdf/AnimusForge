using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.AI;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Objects.Usables;

public class MusicianGroup : UsableMachine
{
	public const int GapBetweenTracks = 8;

	public const bool DisableAmbientMusic = true;

	private const int TempoMidValue = 120;

	private const int TempoSpeedUpLimit = 130;

	private const int TempoSlowDownLimit = 100;

	private List<PlayMusicPoint> _musicianPoints;

	private SoundEvent _trackEvent;

	private BasicMissionTimer _gapTimer;

	private List<SettlementMusicData> _playList;

	private int _currentTrackIndex = -1;

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		return null;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return null;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new UsablePlaceAI(this);
	}

	public void SetPlayList(List<SettlementMusicData> playList)
	{
		_playList = playList.ToList();
	}

	protected override void OnInit()
	{
		base.OnInit();
		_playList = new List<SettlementMusicData>();
		_musicianPoints = base.StandingPoints.OfType<PlayMusicPoint>().ToList();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		CheckNewTrackStart();
		CheckTrackEnd();
	}

	private void CheckNewTrackStart()
	{
		if (_playList.Count > 0 && _trackEvent == null && (_gapTimer == null || _gapTimer.ElapsedTime > 8f) && _musicianPoints.Any((PlayMusicPoint x) => x.HasUser))
		{
			_currentTrackIndex++;
			if (_currentTrackIndex == _playList.Count)
			{
				_currentTrackIndex = 0;
			}
			SetupInstruments();
			StartTrack();
			_gapTimer = null;
		}
	}

	private void CheckTrackEnd()
	{
		if (_trackEvent != null)
		{
			if (_trackEvent.IsPlaying() && !_musicianPoints.Any((PlayMusicPoint x) => x.HasUser))
			{
				_trackEvent.Stop();
			}
			if (_trackEvent != null && !_trackEvent.IsPlaying())
			{
				_trackEvent.Release();
				_trackEvent = null;
				StopMusicians();
				_gapTimer = new BasicMissionTimer();
			}
		}
	}

	private void StopMusicians()
	{
		foreach (PlayMusicPoint musicianPoint in _musicianPoints)
		{
			if (musicianPoint.HasUser)
			{
				musicianPoint.EndLoop();
			}
		}
	}

	private void SetupInstruments()
	{
		List<PlayMusicPoint> list = _musicianPoints.ToList();
		list.Shuffle();
		SettlementMusicData settlementMusicData = _playList[_currentTrackIndex];
		foreach (InstrumentData instrumentData in settlementMusicData.Instruments)
		{
			PlayMusicPoint playMusicPoint = list.FirstOrDefault((PlayMusicPoint x) => x.GameEntity.Parent.Tags.Contains(instrumentData.Tag) || string.IsNullOrEmpty(instrumentData.Tag));
			if (playMusicPoint != null)
			{
				Tuple<InstrumentData, float> instrument = new Tuple<InstrumentData, float>(instrumentData, (float)settlementMusicData.Tempo / 120f);
				playMusicPoint.ChangeInstrument(instrument);
				list.Remove(playMusicPoint);
			}
		}
		Tuple<InstrumentData, float> instrumentEmptyData = GetInstrumentEmptyData(settlementMusicData.Tempo);
		foreach (PlayMusicPoint item in list)
		{
			item.ChangeInstrument(instrumentEmptyData);
		}
	}

	private Tuple<InstrumentData, float> GetInstrumentEmptyData(int tempo)
	{
		if (tempo > 130)
		{
			return new Tuple<InstrumentData, float>(MBObjectManager.Instance.GetObject<InstrumentData>("cheerful"), 1f);
		}
		if (tempo > 100)
		{
			return new Tuple<InstrumentData, float>(MBObjectManager.Instance.GetObject<InstrumentData>("active"), 1f);
		}
		return new Tuple<InstrumentData, float>(MBObjectManager.Instance.GetObject<InstrumentData>("calm"), 1f);
	}

	private void StartTrack()
	{
		int eventIdFromString = SoundEvent.GetEventIdFromString(_playList[_currentTrackIndex].MusicPath);
		_trackEvent = SoundEvent.CreateEvent(eventIdFromString, Mission.Current.Scene);
		_trackEvent.SetPosition(base.GameEntity.GetGlobalFrame().origin);
		_trackEvent.Play();
		foreach (PlayMusicPoint musicianPoint in _musicianPoints)
		{
			musicianPoint.StartLoop(_trackEvent);
		}
	}
}
