using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class HandMorphTest : ScriptComponentBehavior
{
	private const bool CreateFaceImmediately = true;

	private readonly ActionIndexCache act_defend_up_fist_active = ActionIndexCache.Create("act_defend_up_fist_active");

	private readonly ActionIndexCache act_visual_test_morph_animation = ActionIndexCache.Create("act_visual_test_morph_animation");

	private MBGameManager _editorGameManager;

	private bool _isFinished;

	private bool _characterSpawned;

	private AgentVisuals _agentVisuals;

	public uint ClothColor1 { get; private set; }

	public uint ClothColor2 { get; private set; }

	protected override void OnInit()
	{
		base.OnInit();
		ClothColor1 = uint.MaxValue;
		ClothColor2 = uint.MaxValue;
		if (_agentVisuals == null && !_characterSpawned)
		{
			SpawnCharacter();
			_characterSpawned = true;
		}
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		_agentVisuals.GetVisuals().SetFrame(ref frame);
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		if (Game.Current == null)
		{
			_editorGameManager = new EditorGameManager();
		}
		ClothColor1 = uint.MaxValue;
		ClothColor2 = uint.MaxValue;
	}

	protected override void OnEditorTick(float dt)
	{
		if (!_isFinished && _editorGameManager != null)
		{
			_isFinished = !_editorGameManager.DoLoadingForGameManager();
		}
		if (Game.Current != null && _agentVisuals == null && !_characterSpawned)
		{
			SpawnCharacter();
			_characterSpawned = true;
		}
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		_agentVisuals.GetVisuals().SetFrame(ref frame);
	}

	public void SpawnCharacter()
	{
		CharacterCode characterCode = CharacterCode.CreateFrom(MBObjectManager.Instance.GetObject<BasicCharacterObject>("facgen_template_test_char_0"));
		InitWithCharacter(characterCode);
	}

	public void Reset()
	{
		_agentVisuals?.Reset();
	}

	public void InitWithCharacter(CharacterCode characterCode)
	{
		Reset();
		MatrixFrame frame = base.GameEntity.GetFrame();
		frame.rotation.s.z = 0f;
		frame.rotation.f.z = 0f;
		frame.rotation.s.Normalize();
		frame.rotation.f.Normalize();
		frame.rotation.u = Vec3.CrossProduct(frame.rotation.s, frame.rotation.f);
		characterCode.BodyProperties = new BodyProperties(new DynamicBodyProperties(20f, 0f, 0f), characterCode.BodyProperties.StaticProperties);
		Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(characterCode.Race);
		_agentVisuals = AgentVisuals.Create(new AgentVisualsData().Equipment(characterCode.CalculateEquipment()).BodyProperties(characterCode.BodyProperties).Race(characterCode.Race)
			.SkeletonType(characterCode.IsFemale ? SkeletonType.Female : SkeletonType.Male)
			.ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, characterCode.IsFemale, "_facegen"))
			.ActionCode(in act_visual_test_morph_animation)
			.Scene(base.GameEntity.Scene)
			.Monster(baseMonsterFromRace)
			.PrepareImmediately(prepareImmediately: true)
			.UseMorphAnims(useMorphAnims: true)
			.ClothColor1(ClothColor1)
			.ClothColor2(ClothColor2)
			.Frame(frame), "HandMorphTest", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
		_agentVisuals.SetAction(in act_defend_up_fist_active, 1f);
		MatrixFrame frame2 = frame;
		_agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(1f, frame2, tickAnimsForChildren: true);
		_agentVisuals.GetVisuals().SetFrame(ref frame2);
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		_agentVisuals.Reset();
	}
}
