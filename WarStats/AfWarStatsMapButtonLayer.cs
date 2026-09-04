using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AFWarStatsTerminal.UI;

public sealed class AfWarStatsMapButtonLayer : GlobalLayer
{
    private readonly AfWarStatsMapButtonVM _dataSource;

    private GauntletLayer _gauntletLayer;

    private GauntletMovieIdentifier _movie;

    private bool _isFinalized;

    public AfWarStatsMapButtonLayer()
    {
        _dataSource = new AfWarStatsMapButtonVM();
        _gauntletLayer = new GauntletLayer("AFWarStatsMapButton", 203, false);
        Layer = _gauntletLayer;
        _movie = _gauntletLayer.LoadMovie("AFWarStatsMapButton", _dataSource);
    }

    protected override void OnTick(float dt)
    {
        base.OnTick(dt);
        ScreenBase topScreen = ScreenManager.TopScreen;
        bool isMapScreen = Campaign.Current != null && IsCampaignMapScreen(topScreen);
        bool isTerminalOpen = AnimusForge.AnimusForgeTerminalPopup.ActivePopup != null;
        bool isVisible = isMapScreen && !isTerminalOpen && !AfWarStatsPopup.IsOpen && AnimusForge.AnimusForgeTerminalSettings.IsMapIconEnabled;
        _dataSource.IsVisible = isVisible;

        if (isVisible)
        {
            _gauntletLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.MouseButtons);
        }
        else
        {
            _gauntletLayer.InputRestrictions.ResetInputRestrictions();
        }
    }

    public static bool IsCampaignMapScreen(ScreenBase screen)
    {
        string screenTypeName = screen?.GetType().Name;
        return !string.IsNullOrEmpty(screenTypeName)
            && screenTypeName.EndsWith("MapScreen", StringComparison.Ordinal);
    }

    public void FinalizeLayer()
    {
        if (_isFinalized)
        {
            return;
        }

        _isFinalized = true;
        if (_gauntletLayer != null && _movie != null)
        {
            _gauntletLayer.ReleaseMovie(_movie);
        }

        _dataSource.OnFinalize();
        _movie = null;
        _gauntletLayer = null;
        Layer = null;
    }
}
