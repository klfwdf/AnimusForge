using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed class AnimusForgeConversationHistoryLogVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action<string> _onOpenEncyclopediaLink;

	private MBBindingList<AnimusForgeConversationHistoryLogItemVM> _items;

	private string _titleText;

	private string _subtitleText;

	[DataSourceProperty]
	public MBBindingList<AnimusForgeConversationHistoryLogItemVM> Items
	{
		get => _items;
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, nameof(Items));
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get => _titleText;
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, nameof(TitleText));
			}
		}
	}

	[DataSourceProperty]
	public string SubtitleText
	{
		get => _subtitleText;
		set
		{
			if (value != _subtitleText)
			{
				_subtitleText = value;
				OnPropertyChangedWithValue(value, nameof(SubtitleText));
			}
		}
	}

	public AnimusForgeConversationHistoryLogVM(string targetName, IReadOnlyList<AnimusForgeDialogueHistoryEntry> entries, Hero conversationTargetHero, CharacterObject conversationTargetCharacter, Action onClose, Action<string> onOpenEncyclopediaLink)
	{
		_onClose = onClose;
		_onOpenEncyclopediaLink = onOpenEncyclopediaLink;
		TitleText = "AnimusForge 对话历史";
		SubtitleText = string.IsNullOrWhiteSpace(targetName) ? "当前对话对象" : targetName.Trim();
		Items = new MBBindingList<AnimusForgeConversationHistoryLogItemVM>();
		// One short-lived entity catalog serves all history rows, avoiding a campaign-wide entity scan per row.
		EncyclopediaEntityLinkFormatter.DisplaySession linkDisplaySession = EncyclopediaEntityLinkFormatter.CreateDisplaySession();
		if (entries != null && entries.Count > 0)
		{
			foreach (AnimusForgeDialogueHistoryEntry entry in entries)
			{
				if (entry == null)
				{
					continue;
				}
				Items.Add(new AnimusForgeConversationHistoryLogItemVM(entry.GameDate, entry.Speaker, entry.Text, entry.Kind, linkDisplaySession, conversationTargetHero, conversationTargetCharacter, _onOpenEncyclopediaLink));
			}
		}
		if (Items.Count == 0)
		{
			Items.Add(new AnimusForgeConversationHistoryLogItemVM("", "AnimusForge", "当前对象还没有可显示的 AnimusForge 对话历史。", "system", linkDisplaySession, conversationTargetHero, conversationTargetCharacter, _onOpenEncyclopediaLink));
		}
	}

	public void CloseEx()
	{
		_onClose?.Invoke();
	}
}
