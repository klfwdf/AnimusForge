using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionViewsContainer
{
	private List<MissionView> _missionViews;

	private List<MissionView> _missionViewsCopy;

	private int _missionViewsCopiedFrame = -1;

	public MissionViewsContainer()
	{
		_missionViews = new List<MissionView>();
		_missionViewsCopy = _missionViews.ToList();
		_missionViewsCopiedFrame = Utilities.EngineFrameNo;
	}

	public void Add(MissionView missionView)
	{
		_missionViews.Add(missionView);
	}

	public void Remove(MissionView missionView)
	{
		_missionViews.Remove(missionView);
		missionView.IsFinalized = true;
	}

	public bool Contains(MissionView missionView)
	{
		return _missionViews.Contains(missionView);
	}

	public void ForEach(Action<MissionView> action)
	{
		foreach (MissionView item in GetMissionViewsCopy())
		{
			if (!item.IsFinalized)
			{
				action(item);
			}
		}
	}

	private List<MissionView> GetMissionViewsCopy()
	{
		int engineFrameNo = Utilities.EngineFrameNo;
		if (_missionViewsCopiedFrame != engineFrameNo)
		{
			_missionViewsCopy = _missionViews.ToList();
			_missionViewsCopiedFrame = engineFrameNo;
		}
		return _missionViewsCopy;
	}
}
