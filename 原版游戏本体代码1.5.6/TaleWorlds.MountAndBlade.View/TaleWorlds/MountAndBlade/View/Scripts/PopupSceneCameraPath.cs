using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class PopupSceneCameraPath : ScriptComponentBehavior
{
	public enum InterpolationType
	{
		Linear,
		EaseIn,
		EaseOut,
		EaseInOut
	}

	public struct PathAnimationState
	{
		public Path path;

		public string animationName;

		public float totalDistance;

		public float startTime;

		public float duration;

		public float alpha;

		public float easedAlpha;

		public bool fadeCamera;

		public InterpolationType interpolation;

		public string soundEvent;
	}

	public string LookAtEntity = "";

	public string SkeletonName = "";

	public int BoneIndex;

	public Vec3 AttachmentOffset = new Vec3(0f, 0f, 0f, -1f);

	public string InitialPath = "";

	public string InitialAnimationClip = "";

	public string InitialSound = "event:/mission/siege/siegetower/doorland";

	public float InitialPathStartTime;

	public float InitialPathDuration = 1f;

	public InterpolationType InitialInterpolation;

	public bool InitialFadeOut;

	public string PositivePath = "";

	public string PositiveAnimationClip = "";

	public string PositiveSound = "";

	public float PositivePathStartTime;

	public float PositivePathDuration = 1f;

	public InterpolationType PositiveInterpolation;

	public bool PositiveFadeOut;

	public string NegativePath = "";

	public string NegativeAnimationClip = "";

	public string NegativeSound = "";

	public float NegativePathStartTime;

	public float NegativePathDuration = 1f;

	public InterpolationType NegativeInterpolation;

	public bool NegativeFadeOut;

	private bool _isReady;

	public SimpleButton TestInitial;

	public SimpleButton TestPositive;

	public SimpleButton TestNegative;

	private MatrixFrame _localFrameIdentity = MatrixFrame.Identity;

	private GameEntity _lookAtEntity;

	private int _currentState;

	private float _cameraFadeValue;

	private List<PopupSceneSkeletonAnimationScript> _skeletonAnims = new List<PopupSceneSkeletonAnimationScript>();

	private SoundEvent _activeSoundEvent;

	private readonly PathAnimationState[] _transitionState = new PathAnimationState[3];

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
	}

	public void Initialize()
	{
		if (SkeletonName != "" && (base.GameEntity.Skeleton == null || base.GameEntity.Skeleton.GetName() != SkeletonName))
		{
			base.GameEntity.CreateSimpleSkeleton(SkeletonName);
		}
		else if (SkeletonName == "" && base.GameEntity.Skeleton != null)
		{
			base.GameEntity.RemoveSkeleton();
		}
		if (LookAtEntity != "")
		{
			_lookAtEntity = base.GameEntity.Scene.GetFirstEntityWithName(LookAtEntity);
		}
		_transitionState[0].path = ((InitialPath == "") ? null : base.GameEntity.Scene.GetPathWithName(InitialPath));
		_transitionState[0].animationName = InitialAnimationClip;
		_transitionState[0].startTime = InitialPathStartTime;
		_transitionState[0].duration = InitialPathDuration;
		_transitionState[0].interpolation = InitialInterpolation;
		_transitionState[0].fadeCamera = InitialFadeOut;
		_transitionState[0].soundEvent = InitialSound;
		_transitionState[1].path = ((PositivePath == "") ? null : base.GameEntity.Scene.GetPathWithName(PositivePath));
		_transitionState[1].animationName = PositiveAnimationClip;
		_transitionState[1].startTime = PositivePathStartTime;
		_transitionState[1].duration = PositivePathDuration;
		_transitionState[1].interpolation = PositiveInterpolation;
		_transitionState[1].fadeCamera = PositiveFadeOut;
		_transitionState[1].soundEvent = PositiveSound;
		_transitionState[2].path = ((NegativePath == "") ? null : base.GameEntity.Scene.GetPathWithName(NegativePath));
		_transitionState[2].animationName = NegativeAnimationClip;
		_transitionState[2].startTime = NegativePathStartTime;
		_transitionState[2].duration = NegativePathDuration;
		_transitionState[2].interpolation = NegativeInterpolation;
		_transitionState[2].fadeCamera = NegativeFadeOut;
		_transitionState[2].soundEvent = NegativeSound;
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = base.GameEntity.GlobalPosition;
		SoundManager.SetListenerFrame(identity);
		List<GameEntity> entities = new List<GameEntity>();
		base.Scene.GetAllEntitiesWithScriptComponent<PopupSceneSkeletonAnimationScript>(ref entities);
		entities.ForEach(delegate(GameEntity e)
		{
			_skeletonAnims.Add(e.GetFirstScriptOfType<PopupSceneSkeletonAnimationScript>());
		});
		_skeletonAnims.ForEach(delegate(PopupSceneSkeletonAnimationScript s)
		{
			s.Initialize();
		});
	}

	private void SetState(int state)
	{
		if (base.GameEntity.Skeleton != null && !string.IsNullOrEmpty(_transitionState[state].animationName))
		{
			base.GameEntity.Skeleton.SetAnimationAtChannel(_transitionState[state].animationName, 0);
		}
		_currentState = state;
		_transitionState[state].alpha = 0f;
		if (_transitionState[state].path != null)
		{
			_transitionState[state].totalDistance = _transitionState[state].path.GetTotalLength();
		}
		if (_transitionState[state].soundEvent != "")
		{
			_activeSoundEvent?.Stop();
			_activeSoundEvent = SoundEvent.CreateEventFromString(_transitionState[state].soundEvent, null);
			if (_isReady)
			{
				_activeSoundEvent?.Play();
			}
		}
		UpdateCamera(0f, ref _transitionState[state]);
		_skeletonAnims.ForEach(delegate(PopupSceneSkeletonAnimationScript s)
		{
			s.SetState(state);
		});
	}

	public void SetInitialState()
	{
		SetState(0);
	}

	public void SetPositiveState()
	{
		SetState(1);
	}

	public void SetNegativeState()
	{
		SetState(2);
	}

	public void SetIsReady(bool isReady)
	{
		if (_isReady == isReady)
		{
			return;
		}
		if (isReady)
		{
			SoundEvent activeSoundEvent = _activeSoundEvent;
			if (activeSoundEvent != null && !activeSoundEvent.IsPlaying())
			{
				_activeSoundEvent.Play();
			}
		}
		_isReady = isReady;
	}

	public float GetCameraFade()
	{
		return _cameraFadeValue;
	}

	public void Destroy()
	{
		_activeSoundEvent?.Stop();
		for (int i = 0; i < 3; i++)
		{
			_transitionState[i].path = null;
		}
	}

	private float InQuadBlend(float t)
	{
		return t * t;
	}

	private float OutQuadBlend(float t)
	{
		return t * (2f - t);
	}

	private float InOutQuadBlend(float t)
	{
		if (!(t < 0.5f))
		{
			return -1f + (4f - 2f * t) * t;
		}
		return 2f * t * t;
	}

	private MatrixFrame CreateLookAt(Vec3 position, Vec3 target, Vec3 upVector)
	{
		Vec3 vec = target - position;
		vec.Normalize();
		Vec3 va = Vec3.CrossProduct(vec, upVector);
		va.Normalize();
		Vec3 vec2 = Vec3.CrossProduct(va, vec);
		float x = va.x;
		float y = va.y;
		float z = va.z;
		float _ = 0f;
		float x2 = vec2.x;
		float y2 = vec2.y;
		float z2 = vec2.z;
		float _2 = 0f;
		float _3 = 0f - vec.x;
		float _4 = 0f - vec.y;
		float _5 = 0f - vec.z;
		float _6 = 0f;
		float x3 = position.x;
		float y3 = position.y;
		float z3 = position.z;
		float _7 = 1f;
		return new MatrixFrame(x, y, z, _, x2, y2, z2, _2, _3, _4, _5, _6, x3, y3, z3, _7);
	}

	private float Clamp(float x, float a, float b)
	{
		if (!(x < a))
		{
			if (!(x > b))
			{
				return x;
			}
			return b;
		}
		return a;
	}

	private float SmoothStep(float edge0, float edge1, float x)
	{
		x = Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
		return x * x * (3f - 2f * x);
	}

	private void UpdateCamera(float dt, ref PathAnimationState state)
	{
		GameEntity gameEntity = base.GameEntity.Scene.FindEntityWithTag("camera_instance");
		if (gameEntity == null)
		{
			return;
		}
		state.alpha += dt;
		if (state.alpha > state.startTime + state.duration)
		{
			state.alpha = state.startTime + state.duration;
		}
		float num = SmoothStep(state.startTime, state.startTime + state.duration, state.alpha);
		switch (state.interpolation)
		{
		case InterpolationType.EaseIn:
			num = InQuadBlend(num);
			break;
		case InterpolationType.EaseOut:
			num = OutQuadBlend(num);
			break;
		case InterpolationType.EaseInOut:
			num = InOutQuadBlend(num);
			break;
		}
		state.easedAlpha = num;
		if (state.fadeCamera)
		{
			_cameraFadeValue = num;
		}
		if (base.GameEntity.Skeleton != null && !string.IsNullOrEmpty(state.animationName))
		{
			MatrixFrame m = base.GameEntity.Skeleton.GetBoneEntitialFrame((sbyte)BoneIndex);
			m = _localFrameIdentity.TransformToParent(in m);
			MatrixFrame frame = default(MatrixFrame);
			frame.rotation = m.rotation;
			frame.rotation.u = -m.rotation.s;
			frame.rotation.f = -m.rotation.u;
			frame.rotation.s = m.rotation.f;
			frame.origin = m.origin + AttachmentOffset;
			gameEntity.SetFrame(ref frame);
			SoundManager.SetListenerFrame(frame);
		}
		else if (state.path != null)
		{
			float distance = num * state.totalDistance;
			Vec3 origin = state.path.GetFrameForDistance(distance).origin;
			MatrixFrame frame2 = gameEntity.GetGlobalFrame();
			if (_lookAtEntity != null)
			{
				frame2 = CreateLookAt(origin, _lookAtEntity.GetGlobalFrame().origin, Vec3.Up);
			}
			else
			{
				frame2.origin = origin;
			}
			gameEntity.SetGlobalFrame(in frame2);
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		UpdateCamera(dt, ref _transitionState[_currentState]);
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		OnTick(dt);
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		Initialize();
		if (variableName == "TestInitial")
		{
			SetState(0);
		}
		if (variableName == "TestPositive")
		{
			SetState(1);
		}
		if (variableName == "TestNegative")
		{
			SetState(2);
		}
	}
}
