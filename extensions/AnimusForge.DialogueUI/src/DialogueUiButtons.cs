using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace AnimusForge.DialogueUI
{
    internal static class DialogueUiButtons
    {
        // Each button receives its own cloned brush with our ornate medieval parchment button sprites.
        internal static void Style(ButtonWidget button, int fontSize = 18)
        {
            if (button == null) return;
            try
            {
                Brush brush = button.Brush?.Clone()
                    ?? button.Context.GetBrush("Popup.Done.Button.NineGrid")?.Clone()
                    ?? button.Context.GetBrush("ButtonBrush2")?.Clone();
                if (brush != null)
                {
                    brush.Name = "AFDialogue.ButtonBrush";
                    brush.TransitionDuration = 0.08f;
                    SetStateSprite(brush, "Default", "afdui_button_normal", 1f);
                    SetStateSprite(brush, "Hovered", "afdui_button_hover", 1f);
                    SetStateSprite(brush, "Pressed", "afdui_button_pressed", 1f);
                    SetStateSprite(brush, "Selected", "afdui_button_hover", 1f);
                    SetStateSprite(brush, "Disabled", "afdui_button_normal", 0.45f);
                    button.Brush = brush;
                }
                foreach (Widget child in button.Children)
                {
                    if (child is TextWidget text && text.Brush != null)
                    {
                        text.Brush = text.Brush.Clone();
                        text.Brush.FontColor = Color.FromUint(0xFF382919);
                        text.Brush.FontSize = fontSize;
                        text.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
                        text.Brush.TextVerticalAlignment = TextVerticalAlignment.Center;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Failed to style button: " + ex.Message);
            }
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        private static void SetStateSprite(Brush brush, string stateName, string spriteName, float alpha)
        {
            Sprite sprite = DialogueUiSprites.Get(spriteName);
            if (sprite == null) return;
            Style style = brush.GetStyle(stateName);
            if (style?.DefaultLayer != null)
            {
                style.DefaultLayer.Sprite = sprite;
                style.DefaultLayer.AlphaFactor = alpha;
            }
            else if (stateName == "Default" && brush.DefaultStyleLayer != null)
            {
                brush.DefaultStyleLayer.Sprite = sprite;
                brush.DefaultStyleLayer.AlphaFactor = alpha;
            }
        }
    }
}
