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
        internal static void Style(ButtonWidget button, int fontSize = 16)
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
                    CleanAndSetStyles(brush, "afdui_button_normal", "afdui_button_hover", "afdui_button_pressed");
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

        internal static void StyleWaxSeal(ButtonWidget button, int fontSize = 15)
        {
            if (button == null) return;
            try
            {
                Brush brush = button.Brush?.Clone()
                    ?? button.Context.GetBrush("Popup.Done.Button.NineGrid")?.Clone()
                    ?? button.Context.GetBrush("ButtonBrush2")?.Clone();
                if (brush != null)
                {
                    brush.Name = "AFDialogue.WaxSealBrush";
                    brush.TransitionDuration = 0.08f;
                    CleanAndSetStyles(brush, "afdui_wax_seal", "afdui_wax_seal", "afdui_wax_seal");
                    button.Brush = brush;
                }
                foreach (Widget child in button.Children)
                {
                    if (child is TextWidget text && text.Brush != null)
                    {
                        text.Brush = text.Brush.Clone();
                        text.Brush.FontColor = Color.FromUint(0xFFFFF4DB);
                        text.Brush.FontSize = fontSize;
                        text.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
                        text.Brush.TextVerticalAlignment = TextVerticalAlignment.Center;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogueUiRuntime.Log("Failed to style wax seal: " + ex.Message);
            }
            button.DoNotPassEventsToChildren = true;
            button.UpdateChildrenStates = true;
        }

        private static void CleanAndSetStyles(Brush brush, string normalSprite, string hoverSprite, string pressedSprite)
        {
            Sprite normal = DialogueUiSprites.Get(normalSprite);
            Sprite hover = DialogueUiSprites.Get(hoverSprite);
            Sprite pressed = DialogueUiSprites.Get(pressedSprite);

            foreach (Style style in brush.Styles)
            {
                // Remove all extra layers to completely prevent vanilla green/metallic layers from showing on hover
                StyleLayer[] layers = style.GetLayers();
                if (layers != null)
                {
                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (layers[i] != style.DefaultLayer && !string.IsNullOrEmpty(layers[i].Name))
                        {
                            style.RemoveLayer(layers[i].Name);
                        }
                    }
                }
                if (style.DefaultLayer != null)
                {
                    style.DefaultLayer.Color = Color.White;
                    style.DefaultLayer.ColorFactor = 1f;
                    style.DefaultLayer.AlphaFactor = 1f;
                    if (style.Name == "Hovered" || style.Name == "Selected")
                    {
                        style.DefaultLayer.Sprite = hover;
                    }
                    else if (style.Name == "Pressed")
                    {
                        style.DefaultLayer.Sprite = pressed;
                    }
                    else
                    {
                        style.DefaultLayer.Sprite = normal;
                        if (style.Name == "Disabled") style.DefaultLayer.AlphaFactor = 0.45f;
                    }
                }
            }
        }
    }
}
