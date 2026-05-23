using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;

public class ConversationScreenButtonWidget : ButtonWidget
{
	private List<ConversationOptionListPanel> _newlyAddedItems = new List<ConversationOptionListPanel>();

	private ListPanel _answerList;

	private ButtonWidget _continueButton;

	private bool _isPersuasionActive;

	[Editor(false)]
	public ListPanel AnswerList
	{
		get
		{
			return _answerList;
		}
		set
		{
			if (value != _answerList)
			{
				if (value != null)
				{
					value.ItemAddEventHandlers.Add(OnNewOptionAdded);
					value.ItemRemoveEventHandlers.Add(OnOptionRemoved);
				}
				if (_answerList != null)
				{
					value.ItemAddEventHandlers.Remove(OnNewOptionAdded);
					value.ItemRemoveEventHandlers.Remove(OnOptionRemoved);
				}
				_answerList = value;
				OnPropertyChanged(value, "AnswerList");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ContinueButton
	{
		get
		{
			return _continueButton;
		}
		set
		{
			if (value != _continueButton)
			{
				_continueButton = value;
				OnPropertyChanged(value, "ContinueButton");
			}
		}
	}

	[Editor(false)]
	public bool IsPersuasionActive
	{
		get
		{
			return _isPersuasionActive;
		}
		set
		{
			if (value != _isPersuasionActive)
			{
				_isPersuasionActive = value;
				OnPropertyChanged(value, "IsPersuasionActive");
			}
		}
	}

	public ConversationScreenButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (AnswerList != null && ContinueButton != null)
		{
			ContinueButton.IsVisible = AnswerList.ChildCount == 0;
			ContinueButton.IsEnabled = AnswerList.ChildCount == 0;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		foreach (ConversationOptionListPanel newlyAddedItem in _newlyAddedItems)
		{
			newlyAddedItem.OptionButtonWidget.ClickEventHandlers.Add(OnOptionSelection);
		}
		_newlyAddedItems.Clear();
		ListPanel answerList = AnswerList;
		if (answerList != null && answerList.ChildCount > 0 && AnswerList.GetChild(AnswerList.ChildCount - 1) != null)
		{
			AnswerList.GetChild(AnswerList.ChildCount - 1).MarginBottom = 5f;
		}
	}

	private void OnOptionSelection(Widget obj)
	{
	}

	private void OnOptionRemoved(Widget obj, Widget child)
	{
		if (obj is ConversationOptionListPanel conversationOptionListPanel)
		{
			conversationOptionListPanel.OptionButtonWidget.ClickEventHandlers.Remove(OnOptionSelection);
		}
	}

	private void OnNewOptionAdded(Widget parent, Widget child)
	{
		_newlyAddedItems.Add(child as ConversationOptionListPanel);
	}
}
