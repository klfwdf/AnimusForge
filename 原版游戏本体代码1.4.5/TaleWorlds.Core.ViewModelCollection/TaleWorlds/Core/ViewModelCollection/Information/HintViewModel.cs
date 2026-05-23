using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class HintViewModel : ViewModel
{
	public TextObject HintText;

	private readonly string _uniqueName;

	public HintViewModel()
	{
		HintText = TextObject.GetEmpty();
	}

	public HintViewModel(TextObject hintText, string uniqueName = null)
	{
		HintText = hintText;
		_uniqueName = uniqueName;
	}

	public void ExecuteBeginHint()
	{
		if (!TextObject.IsNullOrEmpty(HintText))
		{
			MBInformationManager.ShowHint(HintText.ToString());
		}
	}

	public void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}
}
