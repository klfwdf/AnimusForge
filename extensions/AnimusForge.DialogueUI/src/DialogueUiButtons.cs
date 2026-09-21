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

        private static void AddState(Brush brush, string name, string sprite, float alpha)
        {
            var style = new Style(brush.Layers);
            style.FillFrom(brush.DefaultStyle);
            style.Name = name;
            style.DefaultLayer.Sprite = DialogueUiSprites.Get(sprite);
            style.DefaultLayer.AlphaFactor = alpha;
            brush.AddStyle(style);
        }
    }
}
