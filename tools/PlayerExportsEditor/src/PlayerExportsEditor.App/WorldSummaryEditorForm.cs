using System.Text.Json.Nodes;
using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class WorldSummaryEditorForm : Form
{
    private readonly TextBox _summary = new();

    public WorldSummaryEditorForm(string rawJson)
    {
        Text = "\u4e16\u754c\u5f00\u5c40\u6982\u8981";
        Width = 940;
        Height = 620;
        MinimumSize = new Size(760, 500);
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout();
        LoadJson(rawJson);
    }

    public string Json => BuildJson();

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(Label("\u6982\u8981"), 0, 0);
        _summary.Dock = DockStyle.Fill;
        _summary.Multiline = true;
        _summary.AcceptsReturn = true;
        _summary.ScrollBars = ScrollBars.Vertical;
        root.Controls.Add(_summary, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void LoadJson(string rawJson)
    {
        var root = JsonNode.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson) as JsonObject ?? new JsonObject();
        _summary.Text = root["Summary"]?.GetValue<string>() ?? "";
    }

    private string BuildJson()
    {
        var root = new JsonObject { ["Summary"] = _summary.Text.Trim() };
        return root.ToJsonString(JsonFileStore.JsonOptions);
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }
}
