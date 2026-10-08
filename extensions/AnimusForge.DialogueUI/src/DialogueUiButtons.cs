using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiButtons
    {
        // Each button receives its own brush; no modification of shared native brushes.
        internal static void Style(ButtonWidget button)
        {
            if (button == null) return;
            var brush = new Brush { Name = "AFDialogue.RoundButton", TransitionDuration = 0.08f };
            brush.Sprite = DialogueUiSprites.Get("afdui_button_normal");
            AddState(brush, "Hovered", "afdui_button_hover", 1f);
            AddState(brush, "Pressed", "afdui_button_pressed", 1f);
            AddState(brush, "Selected", "afdui_button_hover", 1f);
            AddState(brush, "Disabled", "afdui_button_normal", 0.45f);
            button.Brush = brush;
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        internal static void StyleWaxSeal(ButtonWidget button)
        {
            if (button == null) return;
            var brush = new Brush { Name = "AFDialogue.WaxSeal", TransitionDuration = 0.08f };
            brush.Sprite = DialogueUiSprites.Get("afdui_wax_seal");
            AddState(brush, "Hovered", "afdui_wax_seal", 1f);
            AddState(brush, "Pressed", "afdui_wax_seal", 0.78f);
            AddState(brush, "Selected", "afdui_wax_seal", 1f);
            AddState(brush, "Disabled", "afdui_wax_seal", 0.45f);
            button.Brush = brush;
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        internal static void StyleParchmentTab(ButtonWidget button)
        {
            if (button == null) return;
            var brush = new Brush { Name = "AFDialogue.ParchmentTab", TransitionDuration = 0.08f };
            brush.Sprite = DialogueUiSprites.Get("afdui_tab_normal");
            AddState(brush, "Hovered", "afdui_tab_hover", 1f);
            AddState(brush, "Pressed", "afdui_tab_pressed", 1f);
            AddState(brush, "Selected", "afdui_tab_pressed", 1f);
            AddState(brush, "Disabled", "afdui_tab_normal", 0.5f);
            button.Brush = brush;
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        internal static void StylePlate(ButtonWidget button)
        {
            if (button == null) return;
            var brush = new Brush { Name = "AFDialogue.ParchmentPlate", TransitionDuration = 0.08f };
            brush.Sprite = DialogueUiSprites.Get("afdui_button_plate_normal");
            AddState(brush, "Hovered", "afdui_button_plate_hover", 1f);
            AddState(brush, "Pressed", "afdui_button_plate_pressed", 1f);
            AddState(brush, "Selected", "afdui_button_plate_hover", 1f);
            AddState(brush, "Disabled", "afdui_button_plate_normal", 0.45f);
            button.Brush = brush;
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        // Whole resource row is the click target; the brush only tints the row background.
        internal static void StyleRow(ButtonWidget button)
        {
            if (button == null) return;
            var brush = new Brush { Name = "AFDialogue.ResourceRow", TransitionDuration = 0.08f };
            brush.Sprite = DialogueUiSprites.Get("afdui_row_normal");
            AddState(brush, "Hovered", "afdui_row_hover", 1f);
            AddState(brush, "Pressed", "afdui_row_selected", 1f);
            AddState(brush, "Selected", "afdui_row_selected", 1f);
            AddState(brush, "Disabled", "afdui_row_normal", 1f);
            button.Brush = brush;
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        // One sprite, hover/pressed as brightness steps. Used by the scene wheel and session panels.
        internal static void StyleSprite(ButtonWidget button, string sprite)
        {
            if (button == null) return;
            var brush = new Brush { Name = "AFDialogue.Sprite." + sprite, TransitionDuration = 0.08f };
            brush.Sprite = DialogueUiSprites.Get(sprite);
            AddState(brush, "Hovered", sprite, 1f, 1.22f);
            AddState(brush, "Pressed", sprite, 1f, 0.8f);
            AddState(brush, "Selected", sprite, 1f, 1.12f);
            AddState(brush, "Disabled", sprite, 0.45f, 1f);
            button.Brush = brush;
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        internal static void StyleShoutScroll(Widget root)
        {
            if (root == null || !DialogueUiSprites.EnsureShoutScrollLoaded()) return;
            foreach (string id in new[] { "AFDialogueShoutHistory", "AFShoutCancel", "AFShoutCodex", "SceneIllustrationButton", "SceneGalleryButton" })
                StylePlate(root.FindChild(id, true) as ButtonWidget);
            StyleSprite(root.FindChild("AFShoutSubmit", true) as ButtonWidget, "afdui_plaque_nameplate");
            StyleSprite(root.FindChild("AFDialogueShoutSubmit", true) as ButtonWidget, "afdui_plaque_nameplate");
        }

        // Scene prefab buttons are styled by Id prefix after the movie (or a list item) appears.
        internal static void StyleSceneButton(ButtonWidget button)
        {
            string id = button?.Id ?? "";
            if (id.StartsWith("AFCap")) StyleSprite(button, "afdui_capsule_topic");
            else if (id.StartsWith("AFPlaque")) StyleSprite(button, "afdui_plaque_nameplate");
            else if (id.StartsWith("AFPlate")) StylePlate(button);
            else if (id.StartsWith("AFTab")) StyleParchmentTab(button);
            else if (id.StartsWith("AFRow")) StyleRow(button);
            // Invisible hit area (the wheel ring): transparent in every state; the hover wedge is drawn separately.
            else if (id.StartsWith("AFBare")) StyleSprite(button, "afdui_row_normal");
        }

        private static void AddState(Brush brush, string name, string sprite, float alpha, float colorFactor)
        {
            AddState(brush, name, sprite, alpha);
            brush.GetStyle(name).DefaultLayer.ColorFactor = colorFactor;
        }

        private static void AddState(Brush brush, string name, string sprite, float alpha)
        {
            var style = new Style(brush.Layers) { DefaultStyle = brush.DefaultStyle };
            style.FillFrom(brush.DefaultStyle);
            style.Name = name;
            style.DefaultLayer.Sprite = DialogueUiSprites.Get(sprite);
            style.DefaultLayer.AlphaFactor = alpha;
            brush.AddStyle(style);
        }
    }
}
