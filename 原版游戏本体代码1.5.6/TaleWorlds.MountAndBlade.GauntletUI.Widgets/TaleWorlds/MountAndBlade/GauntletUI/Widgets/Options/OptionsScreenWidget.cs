using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options;

public class OptionsScreenWidget : Widget
{
	private Widget _currentOptionWidget;

	private bool _initialized;

	public Widget VideoMemoryUsageWidget { get; set; }

	public RichTextWidget CurrentOptionDescriptionWidget { get; set; }

	public RichTextWidget CurrentOptionNameWidget { get; set; }

	public RichTextWidget CurrentOptionExtraInformationWidget { get; set; }

	public Widget CurrentOptionImageWidget { get; set; }

	public TabToggleWidget PerformanceTabToggle { get; set; }

	public OptionsScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			PerformanceTabToggle.TabControlWidget.OnActiveTabChange += OnActiveTabChange;
			VideoMemoryUsageWidget.IsVisible = false;
			_initialized = true;
		}
	}

	private void OnActiveTabChange()
	{
		VideoMemoryUsageWidget.IsVisible = PerformanceTabToggle.TabControlWidget.ActiveTab.Id == "PerformanceOptionsPage";
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		if (PerformanceTabToggle?.TabControlWidget != null)
		{
			PerformanceTabToggle.TabControlWidget.OnActiveTabChange += OnActiveTabChange;
		}
	}

	public void SetCurrentOption(Widget currentOptionWidget, Sprite newgraphicsSprite)
	{
		if (_currentOptionWidget != currentOptionWidget)
		{
			_currentOptionWidget = currentOptionWidget;
			string text = "";
			string text2 = "";
			string text3 = "";
			if (_currentOptionWidget != null)
			{
				if (_currentOptionWidget is OptionsItemWidget optionsItemWidget)
				{
					text = optionsItemWidget.OptionDescription;
					text2 = optionsItemWidget.OptionTitle;
				}
				else if (_currentOptionWidget is OptionsKeyItemListPanel optionsKeyItemListPanel)
				{
					text = optionsKeyItemListPanel.OptionDescription;
					text2 = optionsKeyItemListPanel.OptionTitle;
					text3 = optionsKeyItemListPanel.OptionExtraInformation;
				}
			}
			if (CurrentOptionDescriptionWidget != null)
			{
				CurrentOptionDescriptionWidget.Text = text;
			}
			if (CurrentOptionNameWidget != null)
			{
				CurrentOptionNameWidget.Text = text2;
			}
			if (CurrentOptionExtraInformationWidget != null)
			{
				CurrentOptionExtraInformationWidget.Text = text3;
			}
		}
		if (CurrentOptionImageWidget != null && CurrentOptionImageWidget.Sprite != newgraphicsSprite)
		{
			CurrentOptionImageWidget.Sprite = newgraphicsSprite;
			if (newgraphicsSprite != null)
			{
				float num = CurrentOptionImageWidget.SuggestedWidth / (float)newgraphicsSprite.Width;
				CurrentOptionImageWidget.SuggestedHeight = (float)newgraphicsSprite.Height * num;
			}
		}
	}
}
