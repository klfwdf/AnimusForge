using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class FullScreenNoticeVM : ViewModel
{
	private bool _isNoticeActive;

	private string _noticeTitleText;

	private string _noticeContentText;

	private string _confirmText;

	private InputKeyItemVM _doneInputKey;

	[DataSourceProperty]
	public bool IsNoticeActive
	{
		get
		{
			return _isNoticeActive;
		}
		set
		{
			if (value != _isNoticeActive)
			{
				_isNoticeActive = value;
				OnPropertyChangedWithValue(value, "IsNoticeActive");
			}
		}
	}

	[DataSourceProperty]
	public string NoticeTitleText
	{
		get
		{
			return _noticeTitleText;
		}
		set
		{
			if (value != _noticeTitleText)
			{
				_noticeTitleText = value;
				OnPropertyChangedWithValue(value, "NoticeTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string NoticeContentText
	{
		get
		{
			return _noticeContentText;
		}
		set
		{
			if (value != _noticeContentText)
			{
				_noticeContentText = value;
				OnPropertyChangedWithValue(value, "NoticeContentText");
			}
		}
	}

	[DataSourceProperty]
	public string ConfirmText
	{
		get
		{
			return _confirmText;
		}
		set
		{
			if (value != _confirmText)
			{
				_confirmText = value;
				OnPropertyChangedWithValue(value, "ConfirmText");
			}
		}
	}

	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	public FullScreenNoticeVM()
	{
		IsNoticeActive = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		NoticeTitleText = new TextObject("{=LW8QCm22}Notice").ToString();
		ConfirmText = new TextObject("{=5Unqsx3N}Confirm").ToString();
		TextObject variable = new TextObject("{=Uma907CV}We have released the War Sails Expansion Pack for the game which includes ships, naval battles, and much more. You can use the link on the main menu to go to the store page for more information and obtaining the expansion pack. The addition of the product page link also requires us to update our ESRB rating information as follows:");
		string variable2 = new TextObject("{=VIdtkghu}The ESRB rating information has been updated to include the interactive element:{newline}In-Game Purchases").SetTextVariable("newline", "\n").ToString();
		NoticeContentText = new TextObject("{=!}{LEFT}{newline}{newline}{RIGHT}").SetTextVariable("newline", "\n").SetTextVariable("LEFT", variable).SetTextVariable("RIGHT", variable2)
			.ToString();
	}

	public void ExecuteCloseNotice()
	{
		IsNoticeActive = false;
		BannerlordConfig.IAPNoticeConfirmed = true;
		BannerlordConfig.Save();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
