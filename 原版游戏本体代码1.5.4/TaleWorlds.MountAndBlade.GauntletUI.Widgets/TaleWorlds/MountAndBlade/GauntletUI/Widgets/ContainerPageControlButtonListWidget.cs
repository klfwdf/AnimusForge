using System.Collections.Generic;
using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ContainerPageControlButtonListWidget : ContainerPageControlWidget
{
	private List<ButtonWidget> _pageButtonsList = new List<ButtonWidget>();

	private bool _buttonsInitialized;

	private ButtonWidget _pageButtonTemplate;

	private ListPanel _pageButtonItemsListPanel;

	private string _fullButtonBrush;

	private string _emptyButtonBrush;

	[Editor(false)]
	public ButtonWidget PageButtonTemplate
	{
		get
		{
			return _pageButtonTemplate;
		}
		set
		{
			if (value != _pageButtonTemplate)
			{
				_pageButtonTemplate = value;
				OnPropertyChanged(value, "PageButtonTemplate");
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
	public ListPanel PageButtonItemsListPanel
	{
		get
		{
			return _pageButtonItemsListPanel;
		}
		set
		{
			if (value != _pageButtonItemsListPanel)
			{
				_pageButtonItemsListPanel = value;
				OnPropertyChanged(value, "PageButtonItemsListPanel");
			}
		}
	}

	public ContainerPageControlButtonListWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		RefreshButtonList();
	}

	private void RefreshButtonList()
	{
		if (_buttonsInitialized)
		{
			return;
		}
		_pageButtonsList = new List<ButtonWidget>();
		if (HasChild(PageButtonTemplate))
		{
			RemoveChild(PageButtonTemplate);
		}
		RemoveAllChildren();
		if (PageButtonItemsListPanel == null || PageButtonTemplate == null || EmptyButtonBrush == null || FullButtonBrush == null)
		{
			return;
		}
		PageButtonItemsListPanel.RemoveAllChildren();
		if (base.PageCount == 1)
		{
			base.IsVisible = false;
		}
		else
		{
			for (int i = 0; i < base.PageCount; i++)
			{
				ButtonWidget buttonWidget = new ButtonWidget(base.Context);
				PageButtonItemsListPanel.AddChild(buttonWidget);
				buttonWidget.Brush = PageButtonTemplate.ReadOnlyBrush;
				buttonWidget.DoNotAcceptEvents = false;
				buttonWidget.SuggestedHeight = PageButtonTemplate.SuggestedHeight;
				buttonWidget.SuggestedWidth = PageButtonTemplate.SuggestedWidth;
				buttonWidget.MarginLeft = PageButtonTemplate.MarginLeft;
				buttonWidget.MarginRight = PageButtonTemplate.MarginRight;
				buttonWidget.MarginTop = PageButtonTemplate.MarginTop;
				buttonWidget.MarginBottom = PageButtonTemplate.MarginBottom;
				buttonWidget.DoNotPassEventsToChildren = PageButtonTemplate.DoNotPassEventsToChildren;
				buttonWidget.ClickEventHandlers.Add(OnPageSelection);
				_pageButtonsList.Add(buttonWidget);
			}
			UpdatePageButtonBrushes();
		}
		_buttonsInitialized = true;
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();
		_buttonsInitialized = false;
	}

	protected override void OnContainerItemsUpdated()
	{
		base.OnContainerItemsUpdated();
		UpdatePageButtonBrushes();
	}

	private void OnPageSelection(Widget stageButton)
	{
		int index = _pageButtonsList.IndexOf(stageButton as ButtonWidget);
		GoToPage(index);
		UpdatePageButtonBrushes();
	}

	private void UpdatePageButtonBrushes()
	{
		if (_pageButtonsList.Count < base.PageCount)
		{
			return;
		}
		for (int i = 0; i < base.PageCount; i++)
		{
			if (i == _currentPageIndex)
			{
				_pageButtonsList[i].Brush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == FullButtonBrush);
			}
			else
			{
				_pageButtonsList[i].Brush = base.EventManager.Context.Brushes.First((Brush b) => b.Name == EmptyButtonBrush);
			}
		}
	}
}
