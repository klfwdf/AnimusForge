using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialObjectiveStickParentWidget : TextWidget
{
	public class StickAnimStage
	{
		public enum AnimTypes
		{
			Movement,
			FadeInLocal,
			FadeOutLocal,
			FadeInGlobal,
			FadeOutGlobal,
			Stay
		}

		private float _totalTime;

		public bool IsCompleted { get; private set; }

		public float AnimTime { get; private set; }

		public Vec2 Direction { get; private set; }

		public AnimTypes AnimType { get; private set; }

		public Widget WidgetToManipulate { get; private set; }

		private StickAnimStage()
		{
		}

		internal static StickAnimStage CreateMovementStage(float movementTime, Vec2 direction, Widget widgetToManipulate)
		{
			return new StickAnimStage
			{
				AnimTime = movementTime,
				Direction = direction,
				AnimType = AnimTypes.Movement,
				WidgetToManipulate = widgetToManipulate
			};
		}

		internal static StickAnimStage CreateFadeInStage(float fadeInTime, Widget widgetToManipulate, bool isGlobal)
		{
			return new StickAnimStage
			{
				AnimTime = fadeInTime,
				AnimType = ((!isGlobal) ? AnimTypes.FadeInLocal : AnimTypes.FadeInGlobal),
				WidgetToManipulate = widgetToManipulate
			};
		}

		internal static StickAnimStage CreateStayStage(float stayTime)
		{
			return new StickAnimStage
			{
				AnimTime = stayTime,
				AnimType = AnimTypes.Stay,
				WidgetToManipulate = null
			};
		}

		public void Tick(float dt)
		{
			float num = MathF.Clamp(_totalTime / AnimTime, 0f, 1f);
			switch (AnimType)
			{
			case AnimTypes.Movement:
				WidgetToManipulate.PositionXOffset = ((Direction.X != 0f) ? MathF.Lerp(0f, Direction.X, num) : 0f);
				WidgetToManipulate.PositionYOffset = ((Direction.Y != 0f) ? MathF.Lerp(0f, Direction.Y, num) : 0f);
				IsCompleted = _totalTime > AnimTime;
				break;
			case AnimTypes.FadeInLocal:
				WidgetToManipulate.AlphaFactor = num;
				IsCompleted = WidgetToManipulate.AlphaFactor > 0.98f;
				break;
			case AnimTypes.FadeOutLocal:
				WidgetToManipulate.AlphaFactor = 1f - num;
				IsCompleted = WidgetToManipulate.AlphaFactor < 0.02f;
				break;
			case AnimTypes.FadeInGlobal:
				WidgetToManipulate.SetGlobalAlphaRecursively(num);
				IsCompleted = WidgetToManipulate.AlphaFactor > 0.98f;
				break;
			case AnimTypes.FadeOutGlobal:
				WidgetToManipulate.SetGlobalAlphaRecursively(1f - num);
				IsCompleted = WidgetToManipulate.AlphaFactor < 0.02f;
				break;
			case AnimTypes.Stay:
				IsCompleted = _totalTime > AnimTime;
				break;
			}
			_totalTime += dt;
		}
	}

	private const float LongStayTime = 1f;

	private const float ShortStayTime = 0.1f;

	private const float FadeInTime = 0.15f;

	private const float FadeOutTime = 0.15f;

	private const float SingleMovementDirection = 20f;

	private const float MovementTime = 0.15f;

	private const float ParentActiveAlpha = 0.5f;

	private Queue<List<StickAnimStage>> _animQueue = new Queue<List<StickAnimStage>>();

	private int _movementType;

	public Widget StickMiddle { get; set; }

	[Editor(false)]
	public int MovementType
	{
		get
		{
			return _movementType;
		}
		set
		{
			if (value != _movementType)
			{
				_movementType = value;
				OnPropertyChanged(value, "MovementType");
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_animQueue.Count > 0)
		{
			base.ParentWidget.ParentWidget.AlphaFactor = 0.5f;
			_animQueue.Peek().ForEach(delegate(StickAnimStage a)
			{
				a.Tick(dt);
			});
			if (_animQueue.Peek().All((StickAnimStage a) => a.IsCompleted))
			{
				_animQueue.Dequeue();
			}
		}
		else
		{
			UpdateAnimQueue();
		}
	}

	public TutorialObjectiveStickParentWidget(UIContext context)
		: base(context)
	{
	}

	private void ResetAnim()
	{
		base.PositionXOffset = 0f;
		base.PositionYOffset = 0f;
		this.SetGlobalAlphaRecursively(0f);
	}

	private void UpdateAnimQueue()
	{
		ResetAnim();
		switch (MovementType)
		{
		case 1:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(-20f, 0f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(1f) });
			break;
		case 2:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(20f, 0f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(1f) });
			break;
		case 3:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(0f, -20f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(1f) });
			break;
		case 4:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(0f, 20f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(1f) });
			break;
		case 5:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(-20f, 0f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(2f) });
			break;
		case 6:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(20f, 0f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(2f) });
			break;
		case 7:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(0f, -20f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(2f) });
			break;
		case 8:
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateFadeInStage(0.15f, this, isGlobal: true),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage>
			{
				StickAnimStage.CreateMovementStage(0.15f, new Vec2(0f, 20f), StickMiddle),
				StickAnimStage.CreateFadeInStage(0.15f, StickMiddle, isGlobal: false)
			});
			_animQueue.Enqueue(new List<StickAnimStage> { StickAnimStage.CreateStayStage(2f) });
			break;
		}
	}
}
