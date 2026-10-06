using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionAgentTakenDamageVM : ViewModel
{
	private Camera _missionCamera;

	private bool _isEnabled;

	private MBBindingList<MissionAgentTakenDamageItemVM> _takenDamageList;

	[DataSourceProperty]
	public MBBindingList<MissionAgentTakenDamageItemVM> TakenDamageList
	{
		get
		{
			return _takenDamageList;
		}
		set
		{
			if (value != _takenDamageList)
			{
				_takenDamageList = value;
				OnPropertyChangedWithValue(value, "TakenDamageList");
			}
		}
	}

	public MissionAgentTakenDamageVM(Camera missionCamera)
	{
		_missionCamera = missionCamera;
		TakenDamageList = new MBBindingList<MissionAgentTakenDamageItemVM>();
	}

	public void SetIsEnabled(bool isEnabled)
	{
		_isEnabled = isEnabled;
	}

	internal void Tick(float dt)
	{
		if (_isEnabled)
		{
			for (int i = 0; i < TakenDamageList.Count; i++)
			{
				TakenDamageList[i].Update();
			}
		}
	}

	internal void OnMainAgentHit(int damage, float distance)
	{
		if (_isEnabled && damage > 0)
		{
			TakenDamageList.Add(new MissionAgentTakenDamageItemVM(_missionCamera, Agent.Main?.Position ?? default(Vec3), damage, isRanged: false, OnRemoveDamageItem));
		}
	}

	private void OnRemoveDamageItem(MissionAgentTakenDamageItemVM item)
	{
		TakenDamageList.Remove(item);
	}
}
