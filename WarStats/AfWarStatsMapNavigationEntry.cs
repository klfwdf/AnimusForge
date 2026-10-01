using System;
using System.Reflection;
using AFWarStatsTerminal.Localization;
using HarmonyLib;
using SandBox.View.Map;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace AFWarStatsTerminal.UI;

// Adds the terminal entry to the native bottom-left map navigation bar.
// Icon and button background come from brush layers named after ItemId (GUI/Brushes/AFTerminalMapBarBrushes.xml).
internal static class AfWarStatsMapNavigationEntry
{
    internal const string ItemId = "af_terminal";

    private const string HarmonyId = "AnimusForge.terminal.mapbar.navigation";

    private const string InsertBeforeItemId = "kingdom";

    private static bool _patched;

    // Weak so a finished map screen's view models are not kept alive across campaigns.
    private static WeakReference<MapNavigationVM> _currentNavigation;

    private static WeakReference<MapNavigationHandler> _currentHandler;

    internal static void EnsurePatched()
    {
        if (_patched)
        {
            return;
        }
        _patched = true;
        try
        {
            Harmony harmony = new Harmony(HarmonyId);
            HarmonyMethod postfix = new HarmonyMethod(typeof(AfWarStatsMapNavigationEntry), nameof(MapNavigationConstructorPostfix));
            int patched = 0;
            foreach (ConstructorInfo constructor in AccessTools.GetDeclaredConstructors(typeof(MapNavigationVM)))
            {
                if (constructor.IsStatic)
                {
                    continue;
                }
                harmony.Patch(constructor, postfix: postfix);
                patched++;
            }
            AnimusForge.Logger.Log("Terminal", "[INFO] map bar terminal entry patched constructors=" + patched);
        }
        catch (Exception ex)
        {
            _patched = false;
            AnimusForge.Logger.Log("Terminal", "[WARN] map bar terminal entry patch failed: " + ex.Message);
        }
    }

    private static void MapNavigationConstructorPostfix(MapNavigationVM __instance, object[] __args)
    {
        try
        {
            // Constructor shape differs between 1.3 and 1.4; only the handler argument is needed.
            MapNavigationHandler handler = null;
            if (__args != null)
            {
                foreach (object arg in __args)
                {
                    if (arg is MapNavigationHandler navigationHandler)
                    {
                        handler = navigationHandler;
                        break;
                    }
                }
            }
            _currentNavigation = new WeakReference<MapNavigationVM>(__instance);
            _currentHandler = new WeakReference<MapNavigationHandler>(handler);
            SyncVisibility();
        }
        catch (Exception ex)
        {
            AnimusForge.Logger.Log("Terminal", "[WARN] map bar terminal entry insert failed: " + ex.Message);
        }
    }

    // Adds or removes the bar item so the terminal setting toggle applies without reopening the map.
    internal static void SyncVisibility()
    {
        MapNavigationVM navigation = null;
        if (_currentNavigation == null || !_currentNavigation.TryGetTarget(out navigation) || navigation?.NavigationItems == null)
        {
            return;
        }
        MBBindingList<MapNavigationItemVM> items = navigation.NavigationItems;
        int existingIndex = -1;
        int insertIndex = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            string id = items[i]?.ItemId;
            if (id == ItemId)
            {
                existingIndex = i;
            }
            else if (id == InsertBeforeItemId && insertIndex == items.Count)
            {
                insertIndex = i;
            }
        }
        bool shouldShow = AnimusForge.AnimusForgeTerminalSettings.IsMapIconEnabled;
        if (shouldShow && existingIndex < 0)
        {
            // Kingdom's background sprite is the bar's curved end cap, so the entry goes right before it.
            MapNavigationHandler handler = null;
            _currentHandler?.TryGetTarget(out handler);
            // Must run before the item VM binds so the icon widget copies the custom sprite.
            AfTerminalMapBarIconSprite.EnsureApplied();
            items.Insert(insertIndex, new MapNavigationItemVM(new TerminalNavigationElement(handler)));
        }
        else if (!shouldShow && existingIndex >= 0)
        {
            MapNavigationItemVM item = items[existingIndex];
            items.RemoveAt(existingIndex);
            item.OnFinalize();
        }
    }

    private sealed class TerminalNavigationElement : INavigationElement
    {
        private readonly MapNavigationHandler _handler;

        public TerminalNavigationElement(MapNavigationHandler handler)
        {
            _handler = handler;
        }

        public string StringId => ItemId;

        public bool IsActive => AnimusForge.AnimusForgeTerminalPopup.ActivePopup != null;

        public bool IsLockingNavigation => false;

        public bool HasAlert => false;

        public NavigationPermissionItem Permission => GetPermission();

        public TextObject Tooltip => AfWarStatsTexts.TerminalNavigationTooltip;

        public TextObject AlertTooltip => TextObject.GetEmpty();

        private NavigationPermissionItem GetPermission()
        {
            // Same visibility rules as the old floating button: campaign map only, terminal not already open.
            if (IsActive || Campaign.Current == null || !(ScreenManager.TopScreen is MapScreen))
            {
                return new NavigationPermissionItem(isAuthorized: false, null);
            }
            // IsNavigationBarEnabled dereferences the handler unconditionally.
            if (_handler != null && !MapNavigationHelper.IsNavigationBarEnabled(_handler))
            {
                return new NavigationPermissionItem(isAuthorized: false, null);
            }
            return new NavigationPermissionItem(isAuthorized: true, null);
        }

        public void OpenView()
        {
            if (!Permission.IsAuthorized)
            {
                return;
            }
            AnimusForge.AnimusForgeTerminalBehavior behavior = AnimusForge.AnimusForgeTerminalBehavior.Instance;
            if (behavior == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(AfWarStatsTexts.OpenFailed));
                return;
            }
            behavior.OpenTerminalToWarStats();
        }

        public void OpenView(params object[] parameters)
        {
            OpenView();
        }

        public void GoToLink()
        {
        }
    }
}
