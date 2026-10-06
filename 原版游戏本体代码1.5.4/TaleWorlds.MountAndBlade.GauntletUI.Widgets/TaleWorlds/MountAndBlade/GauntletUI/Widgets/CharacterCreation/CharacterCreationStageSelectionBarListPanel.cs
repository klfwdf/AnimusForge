using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterCreation;

public class CharacterCreationStageSelectionBarListPanel : ListPanel
{
	private List<ButtonWidget> _stageButtonsList = new List<ButtonWidget>();

	private bool _buttonsInitialized;

	private ButtonWidget _stageButtonTemplate;

	private int _currentStageIndex = -1;

	private int _totalStagesCount = -1;

	private int _openedStageIndex = -1;

	private string _fullButtonBrush;

	private string _emptyButtonBrush;

	private string _fullBrightButtonBrush;

	private Widget _barFillWidget;

	private Widget _barCanvasWidget;

	[Editor(false)]
	public ButtonWidget StageButtonTemplate
	{
		get
		{
			return _stageButtonTemplate;
		}
		set
		{
			if (_stageButtonTemplate != value)
			{
				_stageButtonTemplate = value;
				OnPropertyChanged(value, "StageButtonTemplate");
				if (value != null)
				{
					RemoveChild(value);
				}
			}
		}
	}

	[Editor(false)]
	public Widget BarFillWidget
	{
		get
		{
			return _barFillWidget;
		}
		set
		{
			if (_barFillWidget != value)
			{
				_barFillWidget = value;
				OnPropertyChanged(value, "BarFillWidget");
			}
		}
	}

	[Editor(false)]
	public Widget BarCanvasWidget
	{
		get
		{
			return _barCanvasWidget;
		}
		set
		{
			if (_barCanvasWidget != value)
			{
				_barCanvasWidget = value;
				OnPropertyChanged(value, "BarCanvasWidget");
			}
		}
	}

	[Editor(false)]
	public int CurrentStageIndex
	{
		get
		{
			return _currentStageIndex;
		}
		set
		{
			if (_currentStageIndex != value)
			{
				_currentStageIndex = value;
				OnPropertyChanged(value, "CurrentStageIndex");
				_buttonsInitialized = false;
			}
		}
	}

	[Editor(false)]
	public int TotalStagesCount
	{
		get
		{
			return _totalStagesCount;
		}
		set
		{
			if (_totalStagesCount != value)
			{
				_totalStagesCount = value;
				OnPropertyChanged(value, "TotalStagesCount");
			}
		}
	}

	[Editor(false)]
	public int OpenedStageIndex
	{
		get
		{
			return _openedStageIndex;
		}
		set
		{
			if (_openedStageIndex != value)
			{
				_openedStageIndex = value;
				OnPropertyChanged(value, "OpenedStageIndex");
			}
		}
	}

	[Editor(false)]
	public string FullButtonBrush
	{
		get
		{
			return _fullButtonBrush;
		}
		set
		{
			if (_fullButtonBrush != value)
			{
				_fullButtonBrush = value;
				OnPropertyChanged(value, "FullButtonBrush");
			}
		}
	}

	[Editor(false)]
	public string EmptyButtonBrush
	{
		get
		{
			return _emptyButtonBrush;
		}
		set
		{
			if (_emptyButtonBrush != value)
			{
				_emptyButtonBrush = value;
				OnPropertyChanged(value, "EmptyButtonBrush");
			}
		}
	}

	[Editor(false)]
	public string FullBrightButtonBrush
	{
		get
		{
			return _fullBrightButtonBrush;
		}
		set
		{
			if (_fullBrightButtonBrush != value)
			{
				_fullBrightButtonBrush = value;
				OnPropertyChanged(value, "FullBrightButtonBrush");
			}
		}
	}

	public CharacterCreationStageSelectionBarListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		RefreshButtonList();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (BarFillWidget != null && TotalStagesCount != 0 && _buttonsInitialized && CurrentStageIndex != -1)
		{
			BarFillWidget.ScaledSuggestedWidth = BarCanvasWidget.Size.X - _stageButtonsList[_stageButtonsList.Count - 1 - CurrentStageIndex].LocalPosition.X;
		}
	}

	private void RefreshButtonList()
	{
		if (_buttonsInitialized)
		{
			return;
		}
		_stageButtonsList = new List<ButtonWidget>();
		if (HasChild(StageButtonTemplate))
		{
			RemoveChild(StageButtonTemplate);
		}
		RemoveAllChildren();
		if (StageButtonTemplate == null || EmptyButtonBrush == null || FullButtonBrush == null || FullBrightButtonBrush == null)
		{
			return;
		}
		if (TotalStagesCount == 0)
		{
			BarCanvasWidget.IsVisible = false;
			base.IsVisible = false;
		}
		else
		{
			for (int i = 0; i < TotalStagesCount; i++)
			{
				ButtonWidget buttonWidget = new ButtonWidget(base.Context);
				AddChild(buttonWidget);
				buttonWidget.Brush = StageButtonTemplate.ReadOnlyBrush;
				bool flag = false;
				if (i == CurrentStageIndex)
				{
					buttonWidget.Brush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == FullBrightButtonBrush);
				}
				else if (i <= OpenedStageIndex || (OpenedStageIndex == -1 && i < CurrentStageIndex))
				{
					buttonWidget.Brush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == FullButtonBrush);
					flag = true;
				}
				else
				{
					buttonWidget.Brush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == EmptyButtonBrush);
				}
				buttonWidget.DoNotAcceptEvents = !flag;
				buttonWidget.SuggestedHeight = StageButtonTemplate.SuggestedHeight;
				buttonWidget.SuggestedWidth = StageButtonTemplate.SuggestedWidth;
				buttonWidget.DoNotPassEventsToChildren = StageButtonTemplate.DoNotPassEventsToChildren;
				buttonWidget.ClickEventHandlers.Add(OnStageSelection);
				_stageButtonsList.Add(buttonWidget);
			}
		}
		_buttonsInitialized = true;
	}

	private void OnStageSelection(Widget stageButton)
	{
		int num = _stageButtonsList.IndexOf(stageButton as ButtonWidget);
		EventFired("OnStageSelection", num);
	}
}
