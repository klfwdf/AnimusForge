using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class TutorialArea : MissionObject
{
	public enum TrainingType
	{
		Bow,
		Melee,
		Mounted,
		AdvancedMelee
	}

	private struct TutorialEntity
	{
		public string Tag;

		public List<Tuple<GameEntity, MatrixFrame>> EntityList;

		public List<DestructableComponent> DestructableComponents;

		public List<GameEntity> WeaponList;

		public List<ItemObject> WeaponNames;

		public TutorialEntity(string tag, List<Tuple<GameEntity, MatrixFrame>> entityList, List<DestructableComponent> destructableComponents, List<GameEntity> weapon, List<ItemObject> weaponNames)
		{
			Tag = tag;
			EntityList = entityList;
			DestructableComponents = destructableComponents;
			WeaponList = weapon;
			WeaponNames = weaponNames;
		}
	}

	[EditableScriptComponentVariable(true, "")]
	private TrainingType _typeOfTraining;

	[EditableScriptComponentVariable(true, "")]
	private string _tagPrefix = "A_";

	private readonly List<TutorialEntity> _tagWeapon = new List<TutorialEntity>();

	private readonly List<VolumeBox> _volumeBoxes = new List<VolumeBox>();

	private readonly List<GameEntity> _boundaries = new List<GameEntity>();

	private bool _boundariesHidden;

	private readonly List<GameEntity> _highlightedEntities = new List<GameEntity>();

	private readonly List<ItemObject> _allowedWeaponsHelper = new List<ItemObject>();

	private readonly MBList<TrainingIcon> _trainingIcons = new MBList<TrainingIcon>();

	public MBReadOnlyList<TrainingIcon> TrainingIconsReadOnly => _trainingIcons;

	public TrainingType TypeOfTraining
	{
		get
		{
			return _typeOfTraining;
		}
		private set
		{
			_typeOfTraining = value;
		}
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		GatherWeapons();
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (MBEditor.IsEntitySelected(base.GameEntity))
		{
			uint value = 4294901760u;
			{
				foreach (TutorialEntity item in _tagWeapon)
				{
					foreach (Tuple<GameEntity, MatrixFrame> entity in item.EntityList)
					{
						entity.Item1.SetContourColor(value);
						_highlightedEntities.Add(entity.Item1);
					}
				}
				return;
			}
		}
		foreach (GameEntity highlightedEntity in _highlightedEntities)
		{
			highlightedEntity.SetContourColor(null);
		}
		_highlightedEntities.Clear();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		List<GameEntity> entities = new List<GameEntity>();
		base.GameEntity.Scene.GetEntities(ref entities);
		foreach (GameEntity item in entities)
		{
			string[] tags = item.Tags;
			for (int i = 0; i < tags.Length; i++)
			{
				if (tags[i].StartsWith(_tagPrefix) && item.HasScriptOfType<WeaponSpawner>())
				{
					item.GetFirstScriptOfType<WeaponSpawner>().SpawnWeapon();
					break;
				}
			}
		}
		GatherWeapons();
	}

	public override void AfterMissionStart()
	{
		DeactivateAllWeapons(resetDestructibles: true);
		MarkTrainingIcons(mark: false);
	}

	private void GatherWeapons()
	{
		List<GameEntity> entities = new List<GameEntity>();
		base.GameEntity.Scene.GetEntities(ref entities);
		foreach (GameEntity item in entities)
		{
			string[] tags = item.Tags;
			foreach (string text in tags)
			{
				TrainingIcon firstScriptOfType = item.GetFirstScriptOfType<TrainingIcon>();
				if (firstScriptOfType != null)
				{
					if (firstScriptOfType.GetTrainingSubTypeTag().StartsWith(_tagPrefix))
					{
						_trainingIcons.Add(firstScriptOfType);
					}
				}
				else if (text == _tagPrefix + "boundary")
				{
					AddBoundary(item);
				}
				else if (text.StartsWith(_tagPrefix))
				{
					AddTaggedWeapon(item, text);
				}
			}
		}
	}

	public void MarkTrainingIcons(bool mark)
	{
		foreach (TrainingIcon trainingIcon in _trainingIcons)
		{
			trainingIcon.SetMarked(mark);
		}
	}

	public TrainingIcon GetActiveTrainingIcon()
	{
		foreach (TrainingIcon trainingIcon in _trainingIcons)
		{
			if (trainingIcon.GetIsActivated())
			{
				return trainingIcon;
			}
		}
		return null;
	}

	private void AddBoundary(GameEntity boundary)
	{
		_boundaries.Add(boundary);
	}

	private void AddTaggedWeapon(GameEntity weapon, string tag)
	{
		if (weapon.HasScriptOfType<VolumeBox>())
		{
			_volumeBoxes.Add(weapon.GetFirstScriptOfType<VolumeBox>());
			return;
		}
		bool flag = false;
		foreach (TutorialEntity item in _tagWeapon)
		{
			if (item.Tag == tag)
			{
				item.EntityList.Add(Tuple.Create(weapon, weapon.GetGlobalFrame()));
				if (weapon.HasScriptOfType<DestructableComponent>())
				{
					item.DestructableComponents.Add(weapon.GetFirstScriptOfType<DestructableComponent>());
				}
				else if (weapon.HasScriptOfType<SpawnedItemEntity>())
				{
					item.WeaponList.Add(weapon);
					item.WeaponNames.Add(MBObjectManager.Instance.GetObject<ItemObject>(weapon.GetFirstScriptOfType<SpawnedItemEntity>().WeaponCopy.Item.StringId));
				}
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			_tagWeapon.Add(new TutorialEntity(tag, new List<Tuple<GameEntity, MatrixFrame>> { Tuple.Create(weapon, weapon.GetGlobalFrame()) }, new List<DestructableComponent>(), new List<GameEntity>(), new List<ItemObject>()));
			if (weapon.HasScriptOfType<DestructableComponent>())
			{
				_tagWeapon[_tagWeapon.Count - 1].DestructableComponents.Add(weapon.GetFirstScriptOfType<DestructableComponent>());
			}
			else if (weapon.HasScriptOfType<SpawnedItemEntity>())
			{
				_tagWeapon[_tagWeapon.Count - 1].WeaponList.Add(weapon);
				_tagWeapon[_tagWeapon.Count - 1].WeaponNames.Add(MBObjectManager.Instance.GetObject<ItemObject>(weapon.GetFirstScriptOfType<SpawnedItemEntity>().WeaponCopy.Item.StringId));
			}
		}
	}

	public int GetIndexFromTag(string tag)
	{
		for (int i = 0; i < _tagWeapon.Count; i++)
		{
			if (_tagWeapon[i].Tag == tag)
			{
				return i;
			}
		}
		return -1;
	}

	public List<string> GetSubTrainingTags()
	{
		List<string> list = new List<string>();
		foreach (TutorialEntity item in _tagWeapon)
		{
			list.Add(item.Tag);
		}
		return list;
	}

	public void ActivateTaggedWeapons(int index)
	{
		if (index >= _tagWeapon.Count)
		{
			return;
		}
		DeactivateAllWeapons(resetDestructibles: false);
		foreach (Tuple<GameEntity, MatrixFrame> entity in _tagWeapon[index].EntityList)
		{
			entity.Item1.SetVisibilityExcludeParents(visible: true);
		}
	}

	public void EquipWeaponsToPlayer(int index)
	{
		foreach (GameEntity weapon in _tagWeapon[index].WeaponList)
		{
			Agent.Main.OnItemPickup(weapon.GetFirstScriptOfType<SpawnedItemEntity>(), EquipmentIndex.None, out var _);
		}
	}

	public void DeactivateAllWeapons(bool resetDestructibles)
	{
		foreach (TutorialEntity item in _tagWeapon)
		{
			if (resetDestructibles)
			{
				foreach (DestructableComponent destructableComponent in item.DestructableComponents)
				{
					destructableComponent.Reset();
					destructableComponent.HitPoint = 1000000f;
					destructableComponent.GameEntity.GetFirstScriptOfType<Markable>()?.DisableMarkerActivation();
				}
			}
			foreach (Tuple<GameEntity, MatrixFrame> entity in item.EntityList)
			{
				if (!entity.Item1.HasScriptOfType<DestructableComponent>())
				{
					if (entity.Item1.HasScriptOfType<SpawnedItemEntity>())
					{
						entity.Item1.GetFirstScriptOfType<SpawnedItemEntity>().StopPhysicsAndSetFrameForClient(entity.Item2, null);
						entity.Item1.GetFirstScriptOfType<SpawnedItemEntity>().HasLifeTime = false;
					}
					entity.Item1.SetGlobalFrame(entity.Item2);
				}
				entity.Item1.SetVisibilityExcludeParents(visible: false);
			}
		}
		HideBoundaries();
	}

	public void ActivateBoundaries()
	{
		if (!_boundariesHidden)
		{
			return;
		}
		foreach (GameEntity boundary in _boundaries)
		{
			boundary.SetVisibilityExcludeParents(visible: true);
		}
		_boundariesHidden = false;
	}

	public void HideBoundaries()
	{
		if (_boundariesHidden)
		{
			return;
		}
		foreach (GameEntity boundary in _boundaries)
		{
			boundary.SetVisibilityExcludeParents(visible: false);
		}
		_boundariesHidden = true;
	}

	public int GetBreakablesCount(int index)
	{
		return _tagWeapon[index].DestructableComponents.Count;
	}

	public void MakeDestructible(int index)
	{
		for (int i = 0; i < _tagWeapon[index].DestructableComponents.Count; i++)
		{
			_tagWeapon[index].DestructableComponents[i].HitPoint = _tagWeapon[index].DestructableComponents[i].MaxHitPoint;
		}
	}

	public void MarkAllTargets(int index, bool mark)
	{
		foreach (DestructableComponent destructableComponent in _tagWeapon[index].DestructableComponents)
		{
			if (mark)
			{
				destructableComponent.GameEntity.GetFirstScriptOfType<Markable>()?.ActivateMarkerFor(3f, 10f);
			}
			else
			{
				destructableComponent.GameEntity.GetFirstScriptOfType<Markable>()?.DisableMarkerActivation();
			}
		}
	}

	public void ResetMarkingTargetTimers(int index)
	{
		foreach (DestructableComponent destructableComponent in _tagWeapon[index].DestructableComponents)
		{
			destructableComponent.GameEntity.GetFirstScriptOfType<Markable>()?.ResetPassiveDurationTimer();
		}
	}

	public void MakeInDestructible(int index)
	{
		for (int i = 0; i < _tagWeapon[index].DestructableComponents.Count; i++)
		{
			_tagWeapon[index].DestructableComponents[i].HitPoint = 1000000f;
		}
	}

	public bool AllBreakablesAreBroken(int index)
	{
		for (int i = 0; i < _tagWeapon[index].DestructableComponents.Count; i++)
		{
			if (!_tagWeapon[index].DestructableComponents[i].IsDestroyed)
			{
				return false;
			}
		}
		return true;
	}

	public int GetBrokenBreakableCount(int index)
	{
		int num = 0;
		for (int i = 0; i < _tagWeapon[index].DestructableComponents.Count; i++)
		{
			if (_tagWeapon[index].DestructableComponents[i].IsDestroyed)
			{
				num++;
			}
		}
		return num;
	}

	public int GetUnbrokenBreakableCount(int index)
	{
		int num = 0;
		for (int i = 0; i < _tagWeapon[index].DestructableComponents.Count; i++)
		{
			if (!_tagWeapon[index].DestructableComponents[i].IsDestroyed)
			{
				num++;
			}
		}
		return num;
	}

	public void ResetBreakables(int index, bool makeIndestructible = true)
	{
		for (int i = 0; i < _tagWeapon[index].DestructableComponents.Count; i++)
		{
			if (makeIndestructible)
			{
				_tagWeapon[index].DestructableComponents[i].HitPoint = 1000000f;
			}
			_tagWeapon[index].DestructableComponents[i].Reset();
		}
	}

	public bool HasMainAgentPickedAll(int index)
	{
		foreach (GameEntity weapon in _tagWeapon[index].WeaponList)
		{
			if (weapon.HasScriptOfType<SpawnedItemEntity>())
			{
				return false;
			}
		}
		return true;
	}

	public void CheckMainAgentEquipment(int index)
	{
		_allowedWeaponsHelper.Clear();
		_allowedWeaponsHelper.AddRange(_tagWeapon[index].WeaponNames);
		EquipmentIndex i;
		for (i = EquipmentIndex.WeaponItemBeginSlot; i <= EquipmentIndex.Weapon3; i++)
		{
			if (!Mission.Current.MainAgent.Equipment[i].IsEmpty)
			{
				if (_allowedWeaponsHelper.Exists((ItemObject x) => x == Mission.Current.MainAgent.Equipment[i].Item))
				{
					_allowedWeaponsHelper.Remove(Mission.Current.MainAgent.Equipment[i].Item);
					continue;
				}
				Mission.Current.MainAgent.DropItem(i);
				MBInformationManager.AddQuickInformation(new TextObject("{=3PP01vFv}Keep away from that weapon."));
			}
		}
	}

	public void CheckWeapons(int index)
	{
		foreach (GameEntity weapon in _tagWeapon[index].WeaponList)
		{
			if (weapon.HasScriptOfType<SpawnedItemEntity>())
			{
				weapon.GetFirstScriptOfType<SpawnedItemEntity>().HasLifeTime = false;
			}
		}
	}

	public bool IsPositionInsideTutorialArea(Vec3 position, out string[] volumeBoxTags)
	{
		foreach (VolumeBox volumeBox in _volumeBoxes)
		{
			if (volumeBox.IsPointIn(position))
			{
				volumeBoxTags = volumeBox.GameEntity.Tags;
				return true;
			}
		}
		volumeBoxTags = null;
		return false;
	}
}
