using TaleWorlds.Core;
using TaleWorlds.Library;
using AFWarStatsTerminal.Localization;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsMapButtonVM : ViewModel
{
    private bool _isVisible;

    [DataSourceProperty]
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value, nameof(IsVisible));
    }

    public void ExecuteOpen()
    {
        Debug.Print("[AnimusForge] Map WarStats terminal button clicked. Visible=" + IsVisible + ".");
        if (!IsVisible)
        {
            return;
        }

        if (AnimusForge.AnimusForgeTerminalPopup.ActivePopup != null)
        {
            AnimusForge.AnimusForgeTerminalPopup.CloseActive();
            return;
        }

        if (AnimusForge.AnimusForgeTerminalBehavior.Instance != null)
        {
            AnimusForge.AnimusForgeTerminalBehavior.Instance.OpenTerminalToWarStats();
        }
        else if (!AfWarStatsPopup.Show())
        {
            InformationManager.DisplayMessage(new InformationMessage(AfWarStatsTexts.OpenFailed));
        }
    }
}
