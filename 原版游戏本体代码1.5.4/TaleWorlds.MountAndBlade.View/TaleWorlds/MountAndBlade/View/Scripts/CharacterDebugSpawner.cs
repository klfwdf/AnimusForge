using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class CharacterDebugSpawner : ScriptComponentBehavior
{
	private readonly ActionIndexCache[] _actionIndices = new ActionIndexCache[97]
	{
		ActionIndexCache.Create("act_start_conversation"),
		ActionIndexCache.Create("act_stand_conversation"),
		ActionIndexCache.Create("act_start_angry_conversation"),
		ActionIndexCache.Create("act_stand_angry_conversation"),
		ActionIndexCache.Create("act_start_sad_conversation"),
		ActionIndexCache.Create("act_stand_sad_conversation"),
		ActionIndexCache.Create("act_start_happy_conversation"),
		ActionIndexCache.Create("act_stand_happy_conversation"),
		ActionIndexCache.Create("act_start_busy_conversation"),
		ActionIndexCache.Create("act_stand_busy_conversation"),
		ActionIndexCache.Create("act_explaining_conversation"),
		ActionIndexCache.Create("act_introduction_conversation"),
		ActionIndexCache.Create("act_wondering_conversation"),
		ActionIndexCache.Create("act_unknown_conversation"),
		ActionIndexCache.Create("act_friendly_conversation"),
		ActionIndexCache.Create("act_offer_conversation"),
		ActionIndexCache.Create("act_negative_conversation"),
		ActionIndexCache.Create("act_affermative_conversation"),
		ActionIndexCache.Create("act_secret_conversation"),
		ActionIndexCache.Create("act_remember_conversation"),
		ActionIndexCache.Create("act_laugh_conversation"),
		ActionIndexCache.Create("act_threat_conversation"),
		ActionIndexCache.Create("act_scared_conversation"),
		ActionIndexCache.Create("act_flirty_conversation"),
		ActionIndexCache.Create("act_thanks_conversation"),
		ActionIndexCache.Create("act_farewell_conversation"),
		ActionIndexCache.Create("act_troop_cavalry_sword"),
		ActionIndexCache.Create("act_inventory_idle_start"),
		ActionIndexCache.Create("act_inventory_idle"),
		ActionIndexCache.Create("act_character_developer_idle"),
		ActionIndexCache.Create("act_inventory_cloth_equip"),
		ActionIndexCache.Create("act_inventory_glove_equip"),
		ActionIndexCache.Create("act_jump"),
		ActionIndexCache.Create("act_jump_loop"),
		ActionIndexCache.Create("act_jump_end"),
		ActionIndexCache.Create("act_jump_end_hard"),
		ActionIndexCache.Create("act_jump_left_stance"),
		ActionIndexCache.Create("act_jump_loop_left_stance"),
		ActionIndexCache.Create("act_jump_end_left_stance"),
		ActionIndexCache.Create("act_jump_end_hard_left_stance"),
		ActionIndexCache.Create("act_jump_forward"),
		ActionIndexCache.Create("act_jump_forward_loop"),
		ActionIndexCache.Create("act_jump_forward_end"),
		ActionIndexCache.Create("act_jump_forward_end_hard"),
		ActionIndexCache.Create("act_jump_forward_left_stance"),
		ActionIndexCache.Create("act_jump_forward_loop_left_stance"),
		ActionIndexCache.Create("act_jump_forward_end_left_stance"),
		ActionIndexCache.Create("act_jump_forward_end_hard_left_stance"),
		ActionIndexCache.Create("act_jump_backward"),
		ActionIndexCache.Create("act_jump_backward_loop"),
		ActionIndexCache.Create("act_jump_backward_end"),
		ActionIndexCache.Create("act_jump_backward_end_hard"),
		ActionIndexCache.Create("act_jump_backward_left_stance"),
		ActionIndexCache.Create("act_jump_backward_loop_left_stance"),
		ActionIndexCache.Create("act_jump_backward_end_left_stance"),
		ActionIndexCache.Create("act_jump_backward_end_hard_left_stance"),
		ActionIndexCache.Create("act_jump_forward_right"),
		ActionIndexCache.Create("act_jump_forward_right_left_stance"),
		ActionIndexCache.Create("act_jump_forward_left"),
		ActionIndexCache.Create("act_jump_forward_left_left_stance"),
		ActionIndexCache.Create("act_jump_right"),
		ActionIndexCache.Create("act_jump_right_loop"),
		ActionIndexCache.Create("act_jump_right_end"),
		ActionIndexCache.Create("act_jump_right_end_hard"),
		ActionIndexCache.Create("act_jump_left"),
		ActionIndexCache.Create("act_jump_left_loop"),
		ActionIndexCache.Create("act_jump_left_end"),
		ActionIndexCache.Create("act_jump_left_end_hard"),
		ActionIndexCache.Create("act_jump_loop_long"),
		ActionIndexCache.Create("act_jump_loop_long_left_stance"),
		ActionIndexCache.Create("act_throne_sit_down_from_front"),
		ActionIndexCache.Create("act_throne_stand_up_to_front"),
		ActionIndexCache.Create("act_throne_sit_idle"),
		ActionIndexCache.Create("act_sit_down_from_front"),
		ActionIndexCache.Create("act_sit_down_from_right"),
		ActionIndexCache.Create("act_sit_down_from_left"),
		ActionIndexCache.Create("act_sit_down_on_floor_1"),
		ActionIndexCache.Create("act_sit_down_on_floor_2"),
		ActionIndexCache.Create("act_sit_down_on_floor_3"),
		ActionIndexCache.Create("act_stand_up_to_front"),
		ActionIndexCache.Create("act_stand_up_to_right"),
		ActionIndexCache.Create("act_stand_up_to_left"),
		ActionIndexCache.Create("act_stand_up_floor_1"),
		ActionIndexCache.Create("act_stand_up_floor_2"),
		ActionIndexCache.Create("act_stand_up_floor_3"),
		ActionIndexCache.Create("act_sit_1"),
		ActionIndexCache.Create("act_sit_2"),
		ActionIndexCache.Create("act_sit_3"),
		ActionIndexCache.Create("act_sit_4"),
		ActionIndexCache.Create("act_sit_5"),
		ActionIndexCache.Create("act_sit_6"),
		ActionIndexCache.Create("act_sit_7"),
		ActionIndexCache.Create("act_sit_8"),
		ActionIndexCache.Create("act_sit_idle_on_floor_1"),
		ActionIndexCache.Create("act_sit_idle_on_floor_2"),
		ActionIndexCache.Create("act_sit_idle_on_floor_3"),
		ActionIndexCache.Create("act_sit_conversation")
	};

	public readonly ActionIndexCache PoseAction = ActionIndexCache.act_walk_idle_unarmed;

	public string LordName = "main_hero";

	public bool IsWeaponWielded;

	private Vec2 MovementDirection;

	private float MovementSpeed;

	private float PhaseDiff;

	private float Time;

	private float ActionSetTimer;

	private float ActionChangeInterval;

	private float MovementDirectionChange;

	private static MBGameManager _editorGameManager = null;

	private static int _editorGameManagerRefCount = 0;

	private static bool isFinished = false;

	private static int gameTickFrameNo = -1;

	private bool CreateFaceImmediately = true;

	private AgentVisuals _agentVisuals;

	public uint ClothColor1 { get; private set; }

	public uint ClothColor2 { get; private set; }

	protected override void OnInit()
	{
		base.OnInit();
		ClothColor1 = uint.MaxValue;
		ClothColor2 = uint.MaxValue;
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		if (_editorGameManager == null)
		{
			_editorGameManager = new EditorGameManager();
		}
		_editorGameManagerRefCount++;
	}

	protected override void OnEditorTick(float dt)
	{
		if (!isFinished && gameTickFrameNo != Utilities.EngineFrameNo)
		{
			gameTickFrameNo = Utilities.EngineFrameNo;
			isFinished = !_editorGameManager.DoLoadingForGameManager();
		}
		if (Game.Current != null && _agentVisuals == null)
		{
			MovementDirection.x = MBRandom.RandomFloatNormal;
			MovementDirection.y = MBRandom.RandomFloatNormal;
			MovementDirection.Normalize();
			MovementSpeed = MBRandom.RandomFloat * 9f + 1f;
			PhaseDiff = MBRandom.RandomFloat;
			MovementDirectionChange = MBRandom.RandomFloatNormal * System.MathF.PI;
			Time = 0f;
			ActionSetTimer = 0f;
			ActionChangeInterval = MBRandom.RandomFloat * 0.5f + 0.5f;
			SpawnCharacter();
		}
		MatrixFrame frame = _agentVisuals.GetVisuals().GetGlobalFrame();
		_agentVisuals.GetVisuals().SetFrame(ref frame);
		Vec3 vec = new Vec3(MovementDirection);
		vec.RotateAboutZ(MovementDirectionChange * dt);
		MovementDirection.x = vec.x;
		MovementDirection.y = vec.y;
		float num = MovementSpeed * (TaleWorlds.Library.MathF.Sin(PhaseDiff + Time) * 0.5f) + 2f;
		Vec2 agentLocalSpeed = MovementDirection * num;
		_agentVisuals.SetAgentLocalSpeed(agentLocalSpeed);
		Time += dt;
		if (Time - ActionSetTimer > ActionChangeInterval)
		{
			ActionSetTimer = Time;
			_agentVisuals.SetAction(in _actionIndices[MBRandom.RandomInt(_actionIndices.Length)]);
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		Reset();
		_editorGameManagerRefCount--;
		if (_editorGameManagerRefCount == 0)
		{
			_editorGameManager = null;
			isFinished = false;
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		switch (variableName)
		{
		case "LordName":
		{
			BasicCharacterObject basicCharacterObject = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
			AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
			copyAgentVisualsData.BodyProperties(basicCharacterObject.GetBodyProperties(null)).SkeletonType(basicCharacterObject.IsFemale ? SkeletonType.Female : SkeletonType.Male).ActionSet(MBGlobals.GetActionSetWithSuffix(copyAgentVisualsData.MonsterData, basicCharacterObject.IsFemale, "_poses"))
				.Equipment(basicCharacterObject.Equipment)
				.UseMorphAnims(useMorphAnims: true);
			_agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
			break;
		}
		case "PoseAction":
			_agentVisuals?.SetAction(in PoseAction);
			break;
		case "IsWeaponWielded":
		{
			BasicCharacterObject character = Game.Current.ObjectManager.GetObject<BasicCharacterObject>(LordName);
			WieldWeapon(CharacterCode.CreateFrom(character));
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
			InitWithCharacter(characterCode);
		}
	}

	public void Reset()
	{
		_agentVisuals?.Reset();
	}

	public void InitWithCharacter(CharacterCode characterCode)
	{
		GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene, isModifiableFromEditor: false);
		gameEntity.Name = "TableauCharacterAgentVisualsEntity";
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterCode.Race);
		_agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(characterCode.CalculateEquipment()).BodyProperties(characterCode.BodyProperties).Race(characterCode.Race)
			.Frame(gameEntity.GetGlobalFrame())
			.SkeletonType(characterCode.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.Entity(gameEntity)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterCode.IsFemale, "_facegen"))
			.ActionCode(in ActionIndexCache.act_inventory_idle_start)
			.Scene(base.GameEntity.Scene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(CreateFaceImmediately)
			.Banner(characterCode.Banner)
			.ClothColor1(ClothColor1)
			.ClothColor2(ClothColor2), "CharacterDebugSpawner", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		_agentVisuals.SetAction(in PoseAction, MBRandom.RandomFloat);
		base.GameEntity.AddChild(gameEntity.WeakEntity);
		WieldWeapon(characterCode);
		_agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(0.1f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: true);
	}

	public void WieldWeapon(CharacterCode characterCode)
	{
		if (!IsWeaponWielded)
		{
			return;
		}
		int num = -1;
		int num2 = -1;
		Equipment equipment = characterCode.CalculateEquipment();
		for (int i = 0; i < 4; i++)
		{
			if (equipment[i].Item?.PrimaryWeapon != null)
			{
				if (num2 == -1 && equipment[i].Item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
				{
					num2 = i;
				}
				if (num == -1 && equipment[i].Item.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.WeaponMask))
				{
					num = i;
				}
			}
		}
		if (num != -1 || num2 != -1)
		{
			AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
			copyAgentVisualsData.RightWieldedItemIndex(num).LeftWieldedItemIndex(num2).ActionCode(in PoseAction);
			_agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
		}
	}
}
