using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class PersonaEditorForm : Form
{
    private readonly TextBox _personality = new();
    private readonly TextBox _background = new();
    private readonly TextBox _traits = new();
    private readonly TextBox _motivations = new();
    private readonly TextBox _values = new();
    private readonly TextBox _fears = new();
    private readonly TextBox _speechStyle = new();
    private readonly TextBox _voiceId = new();

    public PersonaEditorForm(string entityId, string displayName, NpcPersonaProfile? profile)
    {
        Text = PersonaEditorText.Format("Title", string.IsNullOrWhiteSpace(displayName) ? entityId : displayName);
        Width = 980;
        Height = 700;
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout(entityId, displayName);
        _personality.Text = profile?.Personality ?? "";
        _background.Text = profile?.Background ?? "";
        _traits.Text = string.Join(Environment.NewLine, profile?.Traits?.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id)).Select(x => (x.Id ?? "").Trim() + ":" + Math.Max(1, Math.Min(3, x.Strength))) ?? Array.Empty<string>());
        _motivations.Text = string.Join(Environment.NewLine, profile?.Motivations ?? new List<string>());
        _values.Text = string.Join(Environment.NewLine, profile?.Values ?? new List<string>());
        _fears.Text = string.Join(Environment.NewLine, profile?.Fears ?? new List<string>());
        _speechStyle.Text = profile?.SpeechStyle ?? "";
        _voiceId.Text = profile?.VoiceId ?? "";
    }

    public NpcPersonaProfile Profile => new()
    {
        Personality = _personality.Text.Trim(),
        Background = _background.Text.Trim(),
        Traits = ParseTraits(_traits.Text),
        Motivations = SplitLines(_motivations.Text),
        Values = SplitLines(_values.Text),
        Fears = SplitLines(_fears.Text),
        SpeechStyle = _speechStyle.Text.Trim(),
        SchemaVersion = 2,
        VoiceId = _voiceId.Text.Trim()
    };

    private void BuildLayout(string entityId, string displayName)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 10, ColumnCount = 2, Padding = new Padding(10) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(Label(PersonaEditorText.Get("Object")), 0, 0);
        root.Controls.Add(Label(entityId + (string.IsNullOrWhiteSpace(displayName) ? "" : " / " + displayName)), 1, 0);
        root.Controls.Add(Label(PersonaEditorText.Get("Voice")), 0, 1);
        _voiceId.Dock = DockStyle.Fill;
        root.Controls.Add(_voiceId, 1, 1);

        root.Controls.Add(Label(PersonaEditorText.Get("Traits")), 0, 2);
        ConfigureLongText(_traits);
        root.Controls.Add(_traits, 1, 2);

        root.Controls.Add(Label(PersonaEditorText.Get("Personality")), 0, 3);
        ConfigureLongText(_personality);
        root.Controls.Add(_personality, 1, 3);

        root.Controls.Add(Label(PersonaEditorText.Get("Background")), 0, 4);
        ConfigureLongText(_background);
        root.Controls.Add(_background, 1, 4);

        root.Controls.Add(Label(PersonaEditorText.Get("Motivations")), 0, 5);
        ConfigureLongText(_motivations);
        root.Controls.Add(_motivations, 1, 5);

        root.Controls.Add(Label(PersonaEditorText.Get("Values")), 0, 6);
        ConfigureLongText(_values);
        root.Controls.Add(_values, 1, 6);

        root.Controls.Add(Label(PersonaEditorText.Get("Fears")), 0, 7);
        ConfigureLongText(_fears);
        root.Controls.Add(_fears, 1, 7);

        root.Controls.Add(Label(PersonaEditorText.Get("SpeechStyle")), 0, 8);
        ConfigureLongText(_speechStyle);
        root.Controls.Add(_speechStyle, 1, 8);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = PersonaEditorText.Get("Save"), Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = PersonaEditorText.Get("Cancel"), Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 9);
        root.SetColumnSpan(buttons, 2);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void ConfigureLongText(TextBox box)
    {
        box.Dock = DockStyle.Fill;
        box.Multiline = true;
        box.AcceptsReturn = true;
        box.ScrollBars = ScrollBars.Vertical;
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }

    private static List<string> SplitLines(string text)
        => (text ?? "").Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    private static List<NpcPersonaTrait> ParseTraits(string text)
    {
        var result = new List<NpcPersonaTrait>();
        foreach (var raw in SplitLines((text ?? "").Replace(',', '\n')))
        {
            var parts = raw.Split(':', 2);
            var id = parts[0].Trim();
            if (id.Length == 0) continue;
            var strength = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var parsed) ? parsed : 1;
            result.Add(new NpcPersonaTrait { Id = id, Strength = Math.Max(1, Math.Min(3, strength)) });
            if (result.Count >= 5) break;
        }
        return result;
    }
}
