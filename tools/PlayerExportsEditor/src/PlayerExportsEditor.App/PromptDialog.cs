namespace PlayerExportsEditor.App;

public sealed class PromptDialog : Form
{
    private readonly TextBox _input = new();

    private PromptDialog(string title, string prompt, string defaultValue)
    {
        Text = title;
        Width = 420;
        Height = 160;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        Controls.Add(layout);

        layout.Controls.Add(new Label { Text = prompt, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.SetColumnSpan(layout.Controls[^1], 2);

        _input.Text = defaultValue;
        _input.Dock = DockStyle.Fill;
        layout.Controls.Add(_input, 0, 1);
        layout.SetColumnSpan(_input, 2);

        var okButton = new Button { Text = "\u786e\u5b9a", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
        var cancelButton = new Button { Text = "\u53d6\u6d88", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill };
        layout.Controls.Add(okButton, 0, 2);
        layout.Controls.Add(cancelButton, 1, 2);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public static string? Show(IWin32Window owner, string title, string prompt, string defaultValue)
    {
        using var dialog = new PromptDialog(title, prompt, defaultValue);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._input.Text.Trim() : null;
    }
}
