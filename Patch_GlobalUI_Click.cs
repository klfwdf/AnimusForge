using System;
using HarmonyLib;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge;

[HarmonyPatch(typeof(ButtonWidget), "HandleClick")]
public static class Patch_GlobalUI_Click
{
	public static void Prefix(ButtonWidget __instance)
	{
		if (!TraceHelper.IsEnabled)
		{
			return;
		}
		try
		{
			Logger.LogTrace("GodMode", "\ud83d\udc49 [UI点击] ID: " + __instance.Id);
		}
		catch
		{
		}
	}

	public static void Postfix(ButtonWidget __instance)
	{
		try
		{
			KingdomCustomTabIsolation.OnButtonClicked(__instance);
		}
		catch (System.Exception ex)
		{
			if (TraceHelper.IsEnabled)
			{
				Logger.LogTrace("KingdomTabIsolation", "Kingdom tab click isolation failed: " + ex.Message);
			}
		}
	}
}

/// <summary>
/// Keeps AnimusForge's kingdom agenda mutually exclusive with tabs injected by
/// other mods. External tabs do not call the vanilla KingdomManagementVM tab
/// commands, so Harmony patches on those commands alone cannot clear agenda UI.
/// </summary>
internal static class KingdomCustomTabIsolation
{
	private const string AgendaTabButtonId = "AgendaTabButton";
	private const string AgendaPanelRootId = "AgendaPanelRoot";
	private const string KingdomTabControlListPanelTypeName = "KingdomTabControlListPanel";
	private const int MaxAncestorDepth = 16;

	private static WeakReference _lastKnownTabStrip;

	internal static void ResetForNewKingdomScreen()
	{
		_lastKnownTabStrip = null;
	}

	/// <summary>
	/// Called after ButtonWidget.HandleClick. At this point Command.Click handlers
	/// have already selected the target tab, so hiding stale content cannot be
	/// undone by the target tab's own click handler.
	/// </summary>
	internal static void OnButtonClicked(ButtonWidget button)
	{
		if (button == null)
		{
			return;
		}

		Widget tabStrip = FindKingdomTabStrip(button);
		if (tabStrip == null)
		{
			return;
		}

		_lastKnownTabStrip = new WeakReference(tabStrip);
		if (string.Equals(button.Id, AgendaTabButtonId, StringComparison.Ordinal))
		{
			HideForeignContentPanels(tabStrip);
			return;
		}

		// A button inside the real kingdom tab strip is either vanilla or another
		// mod's tab. In both cases the agenda must stop owning the content area.
		KingdomAgendaTabState.ClearForCustomTabClick();
	}

	/// <summary>
	/// Covers programmatic agenda selection (for example, a policy reminder) once
	/// the screen's tab strip has been observed through a normal user click.
	/// </summary>
	internal static void HideForeignPanelsForAgendaSelection()
	{
		if (_lastKnownTabStrip?.Target is Widget tabStrip)
		{
			HideForeignContentPanels(tabStrip);
		}
	}

	private static Widget FindKingdomTabStrip(ButtonWidget button)
	{
		Widget current = button.ParentWidget;
		for (int depth = 0; current != null && depth < MaxAncestorDepth; depth++, current = current.ParentWidget)
		{
			string typeName = current.GetType().Name ?? string.Empty;
			if (string.Equals(typeName, KingdomTabControlListPanelTypeName, StringComparison.Ordinal))
			{
				return current;
			}

			// Some UI replacement mods preserve the original hierarchy but replace
			// the concrete list-panel type. Restrict the fallback to direct children
			// so buttons inside an agenda page are never mistaken for tab buttons.
			if (HasDirectChild(current, AgendaTabButtonId) && HasAnyNativeTabButton(current))
			{
				return current;
			}
		}

		return null;
	}

	private static bool HasAnyNativeTabButton(Widget widget)
	{
		return HasDirectChild(widget, "ClanTabButton")
			|| HasDirectChild(widget, "FiefsTabButton")
			|| HasDirectChild(widget, "PoliciesTabButton")
			|| HasDirectChild(widget, "ArmiesTabButton")
			|| HasDirectChild(widget, "DiplomacyTabButton");
	}

	private static bool HasDirectChild(Widget parent, string id)
	{
		if (parent == null || string.IsNullOrEmpty(id))
		{
			return false;
		}

		for (int index = 0; index < parent.ChildCount; index++)
		{
			Widget child = parent.GetChild(index);
			if (child != null && string.Equals(child.Id, id, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static void HideForeignContentPanels(Widget tabStrip)
	{
		Widget agendaPanel = FindAgendaPanel(tabStrip);
		Widget contentParent = agendaPanel?.ParentWidget;
		if (agendaPanel == null || contentParent == null)
		{
			return;
		}

		int hiddenCount = 0;
		for (int index = 0; index < contentParent.ChildCount; index++)
		{
			Widget candidate = contentParent.GetChild(index);
			if (ReferenceEquals(candidate, agendaPanel) || !IsContentPanel(candidate))
			{
				continue;
			}

			candidate.IsVisible = false;
			hiddenCount++;
		}

		if (hiddenCount > 0 && TraceHelper.IsEnabled)
		{
			Logger.LogTrace("KingdomTabIsolation", "Agenda selected; hid " + hiddenCount + " stale kingdom content panel(s).");
		}
	}

	private static Widget FindAgendaPanel(Widget tabStrip)
	{
		Widget current = tabStrip;
		for (int depth = 0; current != null && depth < MaxAncestorDepth; depth++, current = current.ParentWidget)
		{
			Widget agendaPanel = current.FindChild(AgendaPanelRootId, includeAllChildren: true);
			if (agendaPanel != null)
			{
				return agendaPanel;
			}
		}

		return null;
	}

	private static bool IsContentPanel(Widget widget)
	{
		// The kingdom tab panes share this content rectangle. This intentionally
		// excludes the header, footer, decision overlays, and arbitrary controls.
		return widget != null
			&& widget.IsVisible
			&& widget.WidthSizePolicy == SizePolicy.StretchToParent
			&& widget.HeightSizePolicy == SizePolicy.StretchToParent
			&& widget.MarginTop >= 120f
			&& widget.MarginBottom >= 30f;
	}
}
