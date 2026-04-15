namespace TaskbarMediaControls;

public sealed class SettingsForm : Form {
    private readonly AppSettings _workingCopy;

    private readonly CheckBox _showPreviousCheck = new() { Text = "Show Previous icon", AutoSize = true };
    private readonly CheckBox _showPlayPauseCheck = new() { Text = "Show Play/Pause icon", AutoSize = true };
    private readonly CheckBox _showNextCheck = new() { Text = "Show Next icon", AutoSize = true };
    private readonly CheckBox _showHoverInfoCheck = new() { Text = "Show track info on hover", AutoSize = true };
    private readonly CheckBox _launchOnStartupCheck = new() { Text = "Launch on Startup", AutoSize = true };

    private readonly ComboBox _prevSingleAction = CreateActionCombo();
    private readonly ComboBox _prevDoubleAction = CreateActionCombo();
    private readonly ComboBox _playSingleAction = CreateActionCombo();
    private readonly ComboBox _playDoubleAction = CreateActionCombo();
    private readonly ComboBox _nextSingleAction = CreateActionCombo();
    private readonly ComboBox _nextDoubleAction = CreateActionCombo();

    private readonly TextBox _fallbackExePath = new() { Width = 300 };

    public AppSettings UpdatedSettings { get; private set; }
    public bool LaunchOnStartupEnabled { get; private set; }

    public SettingsForm(AppSettings settings, bool launchOnStartupEnabled) {
        _workingCopy = SettingsModelLogic.Clone(settings);
        UpdatedSettings = SettingsModelLogic.Clone(settings);
        LaunchOnStartupEnabled = launchOnStartupEnabled;

        Text = "TaskbarMediaControls Settings";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 660;
        Height = 470;
        MinimumSize = new Size(600, 420);

        var scrollContainer = new Panel {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8)
        };
        Controls.Add(scrollContainer);

        var root = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 5,
            Width = 620,
            AutoSize = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        scrollContainer.Controls.Add(root);

        root.Controls.Add(CreateVisibilityGroup(), 0, 0);
        root.Controls.Add(CreateActionsGroup(), 0, 1);
        root.Controls.Add(CreateHoverGroup(), 0, 2);
        root.Controls.Add(CreateFallbackGroup(), 0, 3);
        root.Controls.Add(CreateButtonsRow(), 0, 4);

        BindFromSettings(_workingCopy);
    }

    private Control CreateVisibilityGroup() {
        var box = new GroupBox { Text = "Tray Icon Visibility", Dock = DockStyle.Top, Height = 90 };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        panel.Controls.Add(_showPreviousCheck);
        panel.Controls.Add(_showPlayPauseCheck);
        panel.Controls.Add(_showNextCheck);
        box.Controls.Add(panel);
        return box;
    }

    private Control CreateActionsGroup() {
        var box = new GroupBox { Text = "Click Actions (Per Icon)", Dock = DockStyle.Top, Height = 200 };
        var grid = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(8)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        grid.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 0);
        grid.Controls.Add(new Label { Text = "Single Click", AutoSize = true }, 1, 0);
        grid.Controls.Add(new Label { Text = "Double Click", AutoSize = true }, 2, 0);

        grid.Controls.Add(new Label { Text = "Previous Icon", AutoSize = true }, 0, 1);
        grid.Controls.Add(_prevSingleAction, 1, 1);
        grid.Controls.Add(_prevDoubleAction, 2, 1);

        grid.Controls.Add(new Label { Text = "Play/Pause Icon", AutoSize = true }, 0, 2);
        grid.Controls.Add(_playSingleAction, 1, 2);
        grid.Controls.Add(_playDoubleAction, 2, 2);

        grid.Controls.Add(new Label { Text = "Next Icon", AutoSize = true }, 0, 3);
        grid.Controls.Add(_nextSingleAction, 1, 3);
        grid.Controls.Add(_nextDoubleAction, 2, 3);

        box.Controls.Add(grid);
        return box;
    }

    private Control CreateHoverGroup() {
        var box = new GroupBox { Text = "Hover", Dock = DockStyle.Top, Height = 70 };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        panel.Controls.Add(_showHoverInfoCheck);
        panel.Controls.Add(_launchOnStartupCheck);
        box.Controls.Add(panel);
        return box;
    }

    private Control CreateFallbackGroup() {
        var box = new GroupBox { Text = "Fallback Application", Dock = DockStyle.Top, Height = 90 };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        panel.Controls.Add(new Label { Text = "Executable path:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        panel.Controls.Add(_fallbackExePath);
        var browse = new Button { Text = "Browse...", AutoSize = true };
        browse.Click += (_, _) => BrowseExecutable();
        panel.Controls.Add(browse);
        box.Controls.Add(panel);
        return box;
    }

    private Control CreateButtonsRow() {
        var panel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        var saveButton = new Button { Text = "Save", Width = 90 };
        saveButton.Click += (_, _) => SaveAndClose();
        var cancelButton = new Button { Text = "Cancel", Width = 90 };
        cancelButton.Click += (_, _) => {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        var resetButton = new Button { Text = "Reset to Defaults", Width = 140 };
        resetButton.Click += (_, _) => ResetToDefaults();

        panel.Controls.Add(saveButton);
        panel.Controls.Add(cancelButton);
        panel.Controls.Add(resetButton);
        return panel;
    }

    private static ComboBox CreateActionCombo() {
        var combo = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180
        };
        combo.DataSource = Enum.GetValues(typeof(ClickAction));
        return combo;
    }

    private void BrowseExecutable() {
        using var dialog = new OpenFileDialog {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK) {
            _fallbackExePath.Text = dialog.FileName;
        }
    }

    private void BindFromSettings(AppSettings settings) {
        _showPreviousCheck.Checked = settings.PreviousIcon.Visible;
        _showPlayPauseCheck.Checked = settings.PlayPauseIcon.Visible;
        _showNextCheck.Checked = settings.NextIcon.Visible;
        _showHoverInfoCheck.Checked = settings.ShowHoverTrackInfo;
        _launchOnStartupCheck.Checked = LaunchOnStartupEnabled;

        _prevSingleAction.SelectedItem = settings.PreviousIcon.SingleClick;
        _prevDoubleAction.SelectedItem = settings.PreviousIcon.DoubleClick;
        _playSingleAction.SelectedItem = settings.PlayPauseIcon.SingleClick;
        _playDoubleAction.SelectedItem = settings.PlayPauseIcon.DoubleClick;
        _nextSingleAction.SelectedItem = settings.NextIcon.SingleClick;
        _nextDoubleAction.SelectedItem = settings.NextIcon.DoubleClick;

        _fallbackExePath.Text = settings.FallbackExecutablePath;
    }

    private void SaveAndClose() {
        var path = _fallbackExePath.Text.Trim();
        if (!TrayFeatureLogic.IsFallbackPathValid(path)) {
            MessageBox.Show(this, "Fallback executable path does not exist.", "Invalid path", MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _workingCopy.PreviousIcon.Visible = _showPreviousCheck.Checked;
        _workingCopy.PlayPauseIcon.Visible = _showPlayPauseCheck.Checked;
        _workingCopy.NextIcon.Visible = _showNextCheck.Checked;
        _workingCopy.ShowHoverTrackInfo = _showHoverInfoCheck.Checked;
        LaunchOnStartupEnabled = _launchOnStartupCheck.Checked;

        _workingCopy.PreviousIcon.SingleClick = (ClickAction)_prevSingleAction.SelectedItem!;
        _workingCopy.PreviousIcon.DoubleClick = (ClickAction)_prevDoubleAction.SelectedItem!;
        _workingCopy.PlayPauseIcon.SingleClick = (ClickAction)_playSingleAction.SelectedItem!;
        _workingCopy.PlayPauseIcon.DoubleClick = (ClickAction)_playDoubleAction.SelectedItem!;
        _workingCopy.NextIcon.SingleClick = (ClickAction)_nextSingleAction.SelectedItem!;
        _workingCopy.NextIcon.DoubleClick = (ClickAction)_nextDoubleAction.SelectedItem!;

        _workingCopy.FallbackExecutablePath = path;

        UpdatedSettings = SettingsModelLogic.Clone(_workingCopy);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResetToDefaults() {
        var defaults = new AppSettings();

        _showPreviousCheck.Checked = defaults.PreviousIcon.Visible;
        _showPlayPauseCheck.Checked = defaults.PlayPauseIcon.Visible;
        _showNextCheck.Checked = defaults.NextIcon.Visible;
        _showHoverInfoCheck.Checked = defaults.ShowHoverTrackInfo;
        _launchOnStartupCheck.Checked = false;

        _prevSingleAction.SelectedItem = defaults.PreviousIcon.SingleClick;
        _prevDoubleAction.SelectedItem = defaults.PreviousIcon.DoubleClick;
        _playSingleAction.SelectedItem = defaults.PlayPauseIcon.SingleClick;
        _playDoubleAction.SelectedItem = defaults.PlayPauseIcon.DoubleClick;
        _nextSingleAction.SelectedItem = defaults.NextIcon.SingleClick;
        _nextDoubleAction.SelectedItem = defaults.NextIcon.DoubleClick;

        _fallbackExePath.Text = defaults.FallbackExecutablePath;
    }
}
