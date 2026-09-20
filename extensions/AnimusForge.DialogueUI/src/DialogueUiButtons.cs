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
                        foreach (Style s in text.Brush.Styles)
                        {
                            s.FontColor = Color.FromUint(0xFF382919);
                            if (s.DefaultLayer != null) s.DefaultLayer.Color = Color.FromUint(0xFF382919);
                        }
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
                        foreach (Style s in text.Brush.Styles)
                        {
                            s.FontColor = Color.FromUint(0xFFFFF4DB);
                            if (s.DefaultLayer != null) s.DefaultLayer.Color = Color.FromUint(0xFFFFF4DB);
                        }
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
                // CRITICAL: NEVER call style.RemoveLayer(...) because Brush.Layers still holds the layer name.
                // Doing so causes BrushRenderer.Render to throw NullReferenceException on the missing layer.
                // Instead, hide extra layers cleanly by setting AlphaFactor = 0, Sprite = null, IsHidden = true.
                StyleLayer[] layers = style.GetLayers();
                if (layers != null)
                {
                    for (int i = 0; i < layers.Length; i++)
                    {
                        if (layers[i] != style.DefaultLayer)
                        {
                            layers[i].AlphaFactor = 0f;
                            layers[i].Sprite = null;
                            layers[i].IsHidden = true;
                        }
                    }
                }
                if (style.DefaultLayer != null)
                {
                    style.DefaultLayer.IsHidden = false;
                    style.DefaultLayer.Color = Color.White;
                    style.DefaultLayer.ColorFactor = 1f;
                    style.DefaultLayer.AlphaFactor = 1f;
                    if (style.Name == "Hovered" || style.Name == "Selected")
                    {
                        style.DefaultLayer.Sprite = hover ?? normal;
                    }
                    else if (style.Name == "Pressed")
                    {
                        style.DefaultLayer.Sprite = pressed ?? normal;
                        if (normalSprite == pressedSprite) style.DefaultLayer.AlphaFactor = 0.82f;
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
