using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionAgentTakenDamageItemVM : ViewModel
{
	private Action<MissionAgentTakenDamageItemVM> _onRemove;

	private Vec3 _affectorAgentPosition;

	private Camera _missionCamera;

	private int _damage;

	private bool _isBehind;

	private bool _isRanged;

	private Vec2 _screenPosOfAffectorAgent;

	[DataSourceProperty]
	public int Damage
	{
		get
		{
			return _damage;
		}
		set
		{
			if (value != _damage)
			{
				_damage = value;
				OnPropertyChangedWithValue(value, "Damage");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRanged
	{
		get
		{
			return _isRanged;
		}
		set
		{
			if (value != _isRanged)
			{
				_isRanged = value;
				OnPropertyChangedWithValue(value, "IsRanged");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (value != _isBehind)
			{
				_isBehind = value;
				OnPropertyChangedWithValue(value, "IsBehind");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 ScreenPosOfAffectorAgent
	{
		get
		{
			return _screenPosOfAffectorAgent;
		}
		set
		{
			if (value != _screenPosOfAffectorAgent)
			{
				_screenPosOfAffectorAgent = value;
				OnPropertyChangedWithValue(value, "ScreenPosOfAffectorAgent");
			}
		}
	}

	public MissionAgentTakenDamageItemVM(Camera missionCamera, Vec3 affectorAgentPos, int damage, bool isRanged, Action<MissionAgentTakenDamageItemVM> onRemove)
	{
		_affectorAgentPosition = affectorAgentPos;
		Damage = damage;
		IsRanged = isRanged;
		_missionCamera = missionCamera;
		_onRemove = onRemove;
	}

	internal void Update()
	{
		if (IsRanged)
		{
			float screenX = 0f;
			float screenY = 0f;
			float w = 0f;
			MBWindowManager.WorldToScreen(_missionCamera, _affectorAgentPosition, ref screenX, ref screenY, ref w);
			ScreenPosOfAffectorAgent = new Vec2(screenX, screenY);
			IsBehind = w < 0f;
		}
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}
