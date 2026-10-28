using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class CharacterSpawner : ScriptComponentBehavior
{
	public bool Enabled;

	public string PoseAction = "act_walk_idle_unarmed";

	public string LordName = "main_hero_for_perf";

	public string ActionSetSuffix = "_facegen";

	public string PoseActionForHorse = "horse_stand_3";

	public string BodyPropertiesString = "<BodyProperties version=\"4\" age=\"23.16\" weight=\"0.3333\" build=\"0\" key=\"00000C07000000010011111211151111000701000010000000111011000101000000500202111110000000000000000000000000000000000000000000A00000\" />";

	public bool IsWeaponWielded;

	public bool HasMount;

	public bool WieldOffHand = true;

	public float AnimationProgress;

	public float HorseAnimationProgress;

	private MBGameManager _editorGameManager;

	private bool isFinished;

	private bool CreateFaceImmediately = true;

	private AgentVisuals _agentVisuals;

	private GameEntity _agentEntity;

	private GameEntity _horseEntity;

	public bool Active;

	private MatrixFrame _spawnFrame;

	public uint ClothColor1 { get; private set; }

	public uint ClothColor2 { get; private set; }

	protected override void OnInit()
	{
		base.OnInit();
		ClothColor1 = uint.MaxValue;
		ClothColor2 = uint.MaxValue;
	}

	protected void Init()
	{
		Active = false;
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		if (Game.Current == null)
		{
			_editorGameManager = new EditorGameManager();
			isFinished = !_editorGameManager.DoLoadingForGameManager();
		}
	}

	protected override void OnEditorTick(float dt)
	{
		if (!Enabled)
		{
			return;
		}
		if (!isFinished && _editorGameManager != null)
		{
			isFinished = !_editorGameManager.DoLoadingForGameManager();
		}
		if (Game.Current != null && _agentVisuals == null)
		{
			SpawnCharacter();
			base.GameEntity.SetVisibilityExcludeParents(visible: false);
			if (_agentEntity != null)
			{
				_agentEntity.SetVisibilityExcludeParents(visible: false);
			}
			if (_horseEntity != null)
			{
				_horseEntity.SetVisibilityExcludeParents(visible: false);
			}
		}
		if (_agentVisuals != null)
		{
			Skeleton skeleton = _agentVisuals.GetVisuals().GetSkeleton();
			if (skeleton != null)
			{
				skeleton.Freeze(p: false);
				skeleton.TickAnimationsAndForceUpdate(0.001f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: false);
			}
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		if (_agentVisuals != null)
		{
			_agentVisuals.Reset();
			_agentVisuals.GetVisuals().ManualInvalidate();
			_agentVisuals = null;
		}
	}

	public void SetCreateFaceImmediately(bool value)
	{
		CreateFaceImmediately = value;
	}

	private void Disable()
	{
		if (_agentEntity != null && _agentEntity.Parent == base.GameEntity)
		{
			base.GameEntity.RemoveChild(_agentEntity.WeakEntity, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 34);
		}
		if (_agentVisuals != null)
		{
			_agentVisuals.Reset();
			_agentVisuals.GetVisuals().ManualInvalidate();
			_agentVisuals = null;
		}
		if (_horseEntity != null && _horseEntity.Parent == base.GameEntity)
		{
			_horseEntity.Scene.RemoveEntity(_horseEntity, 96);
		}
		Active = false;
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "Enabled")
		{
			if (Enabled)
			{
				Init();
			}
			else
			{
				Disable();
			}
		}
		if (!Enabled)
		{
			return;
		}
		switch (variableName)
		{
		case "LordName":
		case "ActionSetSuffix":
			if (_agentVisuals != null)
			{
				BasicCharacterObject basicCharacterObject5 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
				if (basicCharacterObject5 != null)
				{
					InitWithCharacter(CharacterCode.CreateFrom(basicCharacterObject5), useBodyProperties: true);
				}
			}
			break;
		case "PoseActionForHorse":
		{
			BasicCharacterObject basicCharacterObject4 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
			if (basicCharacterObject4 != null)
			{
				InitWithCharacter(CharacterCode.CreateFrom(basicCharacterObject4), useBodyProperties: true);
			}
			break;
		}
		case "PoseAction":
			if (_agentVisuals != null)
			{
				BasicCharacterObject basicCharacterObject2 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
				if (basicCharacterObject2 != null)
				{
					InitWithCharacter(CharacterCode.CreateFrom(basicCharacterObject2), useBodyProperties: true);
				}
			}
			break;
		case "IsWeaponWielded":
		{
			BasicCharacterObject character2 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
			WieldWeapon(CharacterCode.CreateFrom(character2));
			break;
		}
		case "AnimationProgress":
		{
			Skeleton skeleton = _agentVisuals.GetVisuals().GetSkeleton();
			skeleton.Freeze(p: false);
			skeleton.TickAnimationsAndForceUpdate(0.001f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: false);
			skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(AnimationProgress, 0f, 1f));
			skeleton.SetUptoDate(value: false);
			skeleton.Freeze(p: true);
			break;
		}
		case "HorseAnimationProgress":
			if (_horseEntity != null)
			{
				_horseEntity.Skeleton.Freeze(p: false);
				_horseEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, _horseEntity.GetGlobalFrame(), tickAnimsForChildren: false);
				_horseEntity.Skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(HorseAnimationProgress, 0f, 1f));
				_horseEntity.Skeleton.SetUptoDate(value: false);
				_horseEntity.Skeleton.Freeze(p: true);
			}
			break;
		case "HasMount":
			if (HasMount)
			{
				BasicCharacterObject character = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
				SpawnMount(CharacterCode.CreateFrom(character));
			}
			else if (_horseEntity != null)
			{
				_horseEntity.Scene.RemoveEntity(_horseEntity, 97);
			}
			break;
		case "Active":
			base.GameEntity.SetVisibilityExcludeParents(Active);
			if (_agentEntity != null)
			{
				_agentEntity.SetVisibilityExcludeParents(Active);
			}
			if (_horseEntity != null)
			{
				_horseEntity.SetVisibilityExcludeParents(Active);
			}
			break;
		case "FaceKeyString":
		{
			BasicCharacterObject basicCharacterObject3 = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
			if (basicCharacterObject3 != null)
			{
				InitWithCharacter(CharacterCode.CreateFrom(basicCharacterObject3), useBodyProperties: true);
			}
			break;
		}
		case "WieldOffHand":
		{
			BasicCharacterObject basicCharacterObject = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
			if (basicCharacterObject != null)
			{
				InitWithCharacter(CharacterCode.CreateFrom(basicCharacterObject), useBodyProperties: true);
			}
			break;
		}
		}
	}

	public void SetClothColors(uint color1, uint color2)
	{
		ClothColor1 = color1;
		ClothColor2 = color2;
	}

	public void SpawnCharacter()
	{
		BasicCharacterObject basicCharacterObject = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
		if (basicCharacterObject != null)
		{
			CharacterCode characterCode = CharacterCode.CreateFrom(basicCharacterObject);
			InitWithCharacter(characterCode, useBodyProperties: true);
		}
	}

	public void InitWithCharacter(CharacterCode characterCode, bool useBodyProperties = false)
	{
		base.GameEntity.BreakPrefab();
		if (_agentEntity != null && _agentEntity.Parent == base.GameEntity)
		{
			base.GameEntity.RemoveChild(_agentEntity.WeakEntity, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 35);
		}
		_agentVisuals?.Reset();
		_agentVisuals?.GetVisuals()?.ManualInvalidate();
		if (_horseEntity != null && _horseEntity.Parent == base.GameEntity)
		{
			_horseEntity.Scene.RemoveEntity(_horseEntity, 98);
		}
		_agentEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene, isModifiableFromEditor: false);
		_agentEntity.Name = "TableauCharacterAgentVisualsEntity";
		_spawnFrame = _agentEntity.GetFrame();
		BodyProperties bodyProperties = characterCode.BodyProperties;
		if (useBodyProperties)
		{
			BodyProperties.FromString(BodyPropertiesString, out bodyProperties);
		}
		if (characterCode.Color1 != uint.MaxValue)
		{
			ClothColor1 = characterCode.Color1;
		}
		if (characterCode.Color2 != uint.MaxValue)
		{
			ClothColor2 = characterCode.Color2;
		}
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterCode.Race);
		_agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(characterCode.CalculateEquipment()).BodyProperties(bodyProperties).Race(characterCode.Race)
			.Frame(_spawnFrame)
			.Scale(1f)
			.SkeletonType(characterCode.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Entity(_agentEntity)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterCode.IsFemale, ActionSetSuffix))
			.ActionCode(in ActionIndexCache.act_inventory_idle_start)
			.Scene(base.GameEntity.Scene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(CreateFaceImmediately)
			.Banner(characterCode.Banner)
			.ClothColor1(ClothColor1)
			.ClothColor2(ClothColor2)
			.UseMorphAnims(useMorphAnims: true), "TableauCharacterAgentVisuals", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		_agentVisuals.SetAction(ActionIndexCache.Create(PoseAction), MBMath.ClampFloat(AnimationProgress, 0f, 1f));
		base.GameEntity.AddChild(_agentEntity.WeakEntity);
		WieldWeapon(characterCode);
		if (characterCode.FormationClass == FormationClass.Ranged)
		{
			Equipment equipment = _agentVisuals.GetEquipment();
			for (int i = 0; i < 4; i++)
			{
				if (equipment[i].Item?.PrimaryWeapon != null)
				{
					MBDebug.Print("Ranged primary weapon: " + equipment[i].Item?.StringId + "\n");
				}
			}
		}
		MatrixFrame frame = MatrixFrame.Identity;
		_agentVisuals.GetVisuals().SetFrame(ref frame);
		if (HasMount)
		{
			SpawnMount(characterCode);
		}
		base.GameEntity.SetVisibilityExcludeParents(visible: true);
		_agentEntity.SetVisibilityExcludeParents(visible: true);
		if (_horseEntity != null)
		{
			_horseEntity.SetVisibilityExcludeParents(visible: true);
		}
		_agentEntity.CheckResources(addToQueue: true, checkFaceResources: true);
		Skeleton skeleton = _agentVisuals.GetVisuals().GetSkeleton();
		skeleton.Freeze(p: false);
		skeleton.TickAnimationsAndForceUpdate(0.001f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: false);
		skeleton.SetUptoDate(value: false);
		skeleton.Freeze(p: true);
		foreach (WeakGameEntity child in _agentEntity.WeakEntity.GetChildren())
		{
			child.SetBoundingboxDirty();
		}
		skeleton.Freeze(p: false);
		skeleton.TickAnimationsAndForceUpdate(0.001f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: false);
		skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(AnimationProgress, 0f, 1f));
		skeleton.SetUptoDate(value: false);
		skeleton.Freeze(p: true);
		_agentEntity.SetBoundingboxDirty();
		foreach (WeakGameEntity child2 in _agentEntity.WeakEntity.GetChildren())
		{
			child2.SetBoundingboxDirty();
		}
		skeleton.ManualInvalidate();
		if (_horseEntity != null)
		{
			_horseEntity.Skeleton.Freeze(p: false);
			_horseEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, _horseEntity.GetGlobalFrame(), tickAnimsForChildren: false);
			_horseEntity.Skeleton.SetUptoDate(value: false);
			_horseEntity.Skeleton.Freeze(p: true);
			_horseEntity.SetBoundingboxDirty();
			_horseEntity.SetBoundingboxDirty();
			foreach (WeakGameEntity child3 in _horseEntity.WeakEntity.GetChildren())
			{
				child3.SetBoundingboxDirty();
			}
		}
		if (_horseEntity != null)
		{
			_horseEntity.Skeleton.Freeze(p: false);
			_horseEntity.Skeleton.TickAnimationsAndForceUpdate(0.001f, _horseEntity.GetGlobalFrame(), tickAnimsForChildren: false);
			_horseEntity.Skeleton.SetAnimationParameterAtChannel(0, MBMath.ClampFloat(HorseAnimationProgress, 0f, 1f));
			_horseEntity.Skeleton.SetUptoDate(value: false);
			_horseEntity.Skeleton.Freeze(p: true);
			_horseEntity.SetBoundingboxDirty();
			foreach (WeakGameEntity child4 in _horseEntity.WeakEntity.GetChildren())
			{
				child4.SetBoundingboxDirty();
			}
		}
		base.GameEntity.SetBoundingboxDirty();
		if (!base.GameEntity.Scene.IsEditorScene())
		{
			if (_agentEntity != null)
			{
				_agentEntity.ManualInvalidate();
			}
			if (_horseEntity != null)
			{
				_horseEntity.ManualInvalidate();
			}
		}
	}

	private void WieldWeapon(CharacterCode characterCode)
	{
		if (!IsWeaponWielded)
		{
			return;
		}
		WeaponFlags weaponFlags = (WeaponFlags)0uL;
		switch (characterCode.FormationClass)
		{
		case FormationClass.Infantry:
		case FormationClass.Cavalry:
		case FormationClass.NumberOfDefaultFormations:
		case FormationClass.HeavyInfantry:
		case FormationClass.LightCavalry:
		case FormationClass.NumberOfRegularFormations:
		case FormationClass.Bodyguard:
			weaponFlags = WeaponFlags.MeleeWeapon;
			break;
		case FormationClass.Ranged:
		case FormationClass.HorseArcher:
			weaponFlags = WeaponFlags.RangedWeapon;
			break;
		}
		int num = -1;
		int num2 = -1;
		WeaponComponentData weaponComponentData = null;
		Equipment equipment = characterCode.CalculateEquipment();
		for (int i = 0; i < 4; i++)
		{
			if (equipment[i].Item?.PrimaryWeapon != null)
			{
				if (num2 == -1 && equipment[i].Item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand) && WieldOffHand)
				{
					num2 = i;
				}
				if (num == -1 && equipment[i].Item.PrimaryWeapon.WeaponFlags.HasAnyFlag(weaponFlags))
				{
					num = i;
					weaponComponentData = equipment[i].Item.PrimaryWeapon;
				}
			}
		}
		if (WieldOffHand && weaponFlags == WeaponFlags.RangedWeapon && weaponComponentData != null)
		{
			for (int j = 0; j < 4; j++)
			{
				WeaponComponentData weaponComponentData2 = equipment[j].Item?.PrimaryWeapon;
				if (weaponComponentData2 != null && weaponComponentData2.IsAmmo && weaponComponentData2.WeaponClass == weaponComponentData.AmmoClass)
				{
					num2 = j;
				}
			}
		}
		if (num != -1 || num2 != -1)
		{
			AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
			copyAgentVisualsData.RightWieldedItemIndex(num).LeftWieldedItemIndex(num2).ActionCode(ActionIndexCache.Create(PoseAction))
				.Frame(_spawnFrame);
			_agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
		}
	}

	private void SpawnMount(CharacterCode characterCode)
	{
		Equipment equipment = characterCode.CalculateEquipment();
		ItemObject item = equipment[EquipmentIndex.ArmorItemEndSlot].Item;
		if (item == null)
		{
			HasMount = false;
			return;
		}
		_horseEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene, isModifiableFromEditor: false);
		_horseEntity.Name = "MountEntity";
		Monster monster = item.HorseComponent.Monster;
		MBActionSet actionSet = MBActionSet.GetActionSet(monster.ActionSetCode);
		_horseEntity.CreateAgentSkeleton(actionSet.GetSkeletonName(), isHumanoid: false, actionSet, monster.MonsterUsage, monster);
		_horseEntity.CopyComponentsToSkeleton();
		_horseEntity.Skeleton.SetAgentActionChannel(0, ActionIndexCache.Create(PoseActionForHorse), MBMath.ClampFloat(HorseAnimationProgress, 0f, 1f));
		base.GameEntity.AddChild(_horseEntity.WeakEntity);
		MountVisualCreator.AddMountMeshToEntity(_horseEntity, equipment[10].Item, equipment[11].Item, MountCreationKey.GetRandomMountKeyString(equipment[10].Item, MBRandom.RandomInt()));
		_horseEntity.SetVisibilityExcludeParents(visible: true);
		_horseEntity.Skeleton.TickAnimations(0.01f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: true);
	}
}
