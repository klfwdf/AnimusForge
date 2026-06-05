using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class PersonaEditorForm : Form
{
    private readonly TextBox _personality = new();
    private readonly TextBox _background = new();
    private readonly TextBox _voiceId = new();

    public PersonaEditorForm(string entityId, string displayName, NpcPersonaProfile? profile)
    {
        Text = "\u4eba\u7269\u8d44\u6599\u7f16\u8f91 - " + (string.IsNullOrWhiteSpace(displayName) ? entityId : displayName);
        Width = 980;
        Height = 700;
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout(entityId, displayName);
        _personality.Text = profile?.Personality ?? "";
        _background.Text = profile?.Background ?? "";
        _voiceId.Text = profile?.VoiceId ?? "";
    }

    public NpcPersonaProfile Profile => new()
    {
        Personality = _personality.Text.Trim(),
        Background = _background.Text.Trim(),
        VoiceId = _voiceId.Text.Trim()
    };

    private void BuildLayout(string entityId, string displayName)
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 2, Padding = new Padding(10) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(Label("\u5bf9\u8c61"), 0, 0);
        root.Controls.Add(Label(entityId + (string.IsNullOrWhiteSpace(displayName) ? "" : " / " + displayName)), 1, 0);
        root.Controls.Add(Label("VoiceId"), 0, 1);
        _voiceId.Dock = DockStyle.Fill;
        root.Controls.Add(_voiceId, 1, 1);

        root.Controls.Add(Label("\u4e2a\u6027"), 0, 2);
        ConfigureLongText(_personality);
        root.Controls.Add(_personality, 1, 2);

        root.Controls.Add(Label("\u80cc\u666f"), 0, 3);
        ConfigureLongText(_background);
        root.Controls.Add(_background, 1, 3);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 4);
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
}
