using System.Text.Json.Nodes;
using PlayerExportsEditor.Core;

namespace PlayerExportsEditor.App;

public sealed class VoiceMappingEditorForm : Form
{
    private static readonly string[] GroupKeys =
    {
        "male_young",
        "male_middle",
        "male_old",
        "female_young",
        "female_middle",
        "female_old"
    };

    private readonly Dictionary<string, TextBox> _groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextBox _fallback = new();

    public VoiceMappingEditorForm(string rawJson)
    {
        Text = "\u58f0\u97f3\u6620\u5c04\u7f16\u8f91\u5668";
        Width = 980;
        Height = 720;
        MinimumSize = new Size(820, 600);
        StartPosition = FormStartPosition.CenterParent;
        BuildLayout();
        LoadJson(rawJson);
    }

    public string Json => BuildJson();

    private void BuildLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(Label("\u6bcf\u884c\u4e00\u4e2a\u58f0\u97f3 ID\u3002\u4fdd\u5b58\u65f6\u4f1a\u81ea\u52a8\u79fb\u9664\u540c\u7ec4\u91cd\u590d\u9879\u3002"), 0, 0);

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 4 };
        for (var i = 0; i < 3; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
        }

        for (var i = 0; i < 4; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(i % 2 == 0 ? SizeType.Absolute : SizeType.Percent, i % 2 == 0 ? 110 : 50));
        }

        for (var i = 0; i < GroupKeys.Length; i++)
        {
            var key = GroupKeys[i];
            var row = i / 2;
            var col = (i % 2) * 2;
            var box = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                WordWrap = false
            };
            _groups[key] = box;
            grid.Controls.Add(Label(key), col, row);
            grid.Controls.Add(box, col + 1, row);
        }

        root.Controls.Add(grid, 0, 1);

        var fallbackPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        fallbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        fallbackPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fallbackPanel.Controls.Add(Label("fallback"), 0, 0);
        _fallback.Dock = DockStyle.Fill;
        fallbackPanel.Controls.Add(_fallback, 1, 0);
        root.Controls.Add(fallbackPanel, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
        var ok = new Button { Text = "\u4fdd\u5b58", Width = 100, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "\u53d6\u6d88", Width = 100, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 3);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void LoadJson(string rawJson)
    {
        var root = JsonNode.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson) as JsonObject ?? new JsonObject();
        foreach (var key in GroupKeys)
        {
            if (!_groups.TryGetValue(key, out var box))
            {
                continue;
            }

            var values = new List<string>();
            if (root[key] is JsonArray array)
            {
                foreach (var node in array)
                {
                    var value = node?.GetValue<string>()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value);
                    }
                }
            }

            box.Text = string.Join(Environment.NewLine, values);
        }

        _fallback.Text = root["fallback"]?.GetValue<string>()?.Trim() ?? "";
    }

    private string BuildJson()
    {
        var root = new JsonObject();
        foreach (var key in GroupKeys)
        {
            var array = new JsonArray();
            foreach (var voice in Lines(_groups[key].Text))
            {
                array.Add(voice);
            }

            root[key] = array;
        }

        root["fallback"] = _fallback.Text.Trim();
        return root.ToJsonString(JsonFileStore.JsonOptions);
    }

    private static List<string> Lines(string text)
    {
        return (text ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Label Label(string text)
    {
        return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    }
}
