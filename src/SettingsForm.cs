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
    private readonly ComboBox _fallbackWhenMediaActiveAction = CreateFallbackWhenMediaActiveCombo();
    private readonly ComboBox _fallbackPlayerType = CreateFallbackPlayerTypeCombo();

    private readonly TextBox _fallbackExePath = new() { Width = 300 };

    public AppSettings UpdatedSettings { get; private set; }
    public bool LaunchOnStartupEnabled { get; private set; }

    public SettingsForm(AppSettings settings, bool launchOnStartupEnabled) {
        _workingCopy = SettingsModelLogic.Clone(settings);
        UpdatedSettings = SettingsModelLogic.Clone(settings);
        LaunchOnStartupEnabled = launchOnStartupEnabled;

        Text = "TaskbarMediaControls-plus Settings";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 660;
        Height = 470;
        MinimumSize = new Size(600, 420);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        var scrollContainer = new Panel {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8)
        };
        Controls.Add(scrollContainer);

        var root = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 6,
            Width = 620,
            AutoSize = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        scrollContainer.Controls.Add(root);

        root.Controls.Add(CreateStartupRow(), 0, 0);
        root.Controls.Add(CreateVisibilityGroup(), 0, 1);
        root.Controls.Add(CreateActionsGroup(), 0, 2);
        root.Controls.Add(CreateHoverGroup(), 0, 3);
        root.Controls.Add(CreateFallbackGroup(), 0, 4);
        root.Controls.Add(CreateButtonsRow(), 0, 5);

        BindFromSettings(_workingCopy);
    }

    private Control CreateStartupRow() {
        var panel = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(3, 0, 3, 4)
        };
        panel.Controls.Add(_launchOnStartupCheck);
        return panel;
    }

    private Control CreateVisibilityGroup() {
        var box = new GroupBox {
            Text = "Tray Icon Visibility",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var panel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(6)
        };
        panel.Controls.Add(_showPreviousCheck);
        panel.Controls.Add(_showPlayPauseCheck);
        panel.Controls.Add(_showNextCheck);
        box.Controls.Add(panel);
        return box;
    }

    private Control CreateActionsGroup() {
        var box = new GroupBox {
            Text = "Click Actions (Per Icon)",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var grid = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(8),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
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
        var box = new GroupBox {
            Text = "Hover",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var panel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6)
        };
        panel.Controls.Add(_showHoverInfoCheck);
        box.Controls.Add(panel);
        return box;
    }

    private Control CreateFallbackGroup() {
        var box = new GroupBox {
            Text = "Fallback Application",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6)
        };

        var exeRow = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false
        };
        exeRow.Controls.Add(new Label { Text = "Executable path:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        exeRow.Controls.Add(_fallbackExePath);
        var browse = new Button { Text = "Browse...", AutoSize = true };
        browse.Click += (_, _) => BrowseExecutable();
        exeRow.Controls.Add(browse);

        var playerTypeRow = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false
        };
        playerTypeRow.Controls.Add(new Label { Text = "Fallback player type:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        playerTypeRow.Controls.Add(_fallbackPlayerType);

        var behaviorRow = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };
        behaviorRow.Controls.Add(new Label {
            Text = "When Play/Pause double-click is set to fallback action and media is currently active:",
            AutoSize = true
        });
        behaviorRow.Controls.Add(_fallbackWhenMediaActiveAction);

        panel.Controls.Add(exeRow, 0, 0);
        panel.Controls.Add(playerTypeRow, 0, 1);
        panel.Controls.Add(behaviorRow, 0, 2);
        box.Controls.Add(panel);
        return box;
    }

    private Control CreateButtonsRow() {
        var panel = new FlowLayoutPanel {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
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
        foreach (ClickAction action in Enum.GetValues(typeof(ClickAction))) {
            combo.Items.Add(action);
        }

        if (combo.Items.Count > 0) {
            combo.SelectedIndex = 0;
        }

        return combo;
    }

    private static ComboBox CreateFallbackWhenMediaActiveCombo() {
        var combo = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 320
        };

        foreach (FallbackActionWhenMediaActive action in Enum.GetValues(typeof(FallbackActionWhenMediaActive))) {
            combo.Items.Add(action);
        }

        if (combo.Items.Count > 0) {
            combo.SelectedIndex = 0;
        }

        return combo;
    }

    private static ComboBox CreateFallbackPlayerTypeCombo() {
        var combo = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180
        };

        foreach (FallbackPlayerType playerType in Enum.GetValues(typeof(FallbackPlayerType))) {
            combo.Items.Add(playerType);
        }

        if (combo.Items.Count > 0) {
            combo.SelectedIndex = 0;
        }

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

        SetComboValue(_prevSingleAction, settings.PreviousIcon.SingleClick);
        SetComboValue(_prevDoubleAction, settings.PreviousIcon.DoubleClick);
        SetComboValue(_playSingleAction, settings.PlayPauseIcon.SingleClick);
        SetComboValue(_playDoubleAction, settings.PlayPauseIcon.DoubleClick);
        SetComboValue(_nextSingleAction, settings.NextIcon.SingleClick);
        SetComboValue(_nextDoubleAction, settings.NextIcon.DoubleClick);

        _fallbackExePath.Text = settings.FallbackExecutablePath;
        SetFallbackPlayerTypeComboValue(_fallbackPlayerType, settings.FallbackPlayerType);
        SetFallbackWhenMediaActiveComboValue(_fallbackWhenMediaActiveAction, settings.FallbackActionWhenMediaActive);
    }

    private void SaveAndClose() {
        var path = _fallbackExePath.Text.Trim();
        var shouldKeepExistingFallbackPath = !TrayFeatureLogic.IsFallbackPathValid(path);

        _workingCopy.PreviousIcon.Visible = _showPreviousCheck.Checked;
        _workingCopy.PlayPauseIcon.Visible = _showPlayPauseCheck.Checked;
        _workingCopy.NextIcon.Visible = _showNextCheck.Checked;
        _workingCopy.ShowHoverTrackInfo = _showHoverInfoCheck.Checked;
        LaunchOnStartupEnabled = _launchOnStartupCheck.Checked;

        _workingCopy.PreviousIcon.SingleClick = GetComboValue(_prevSingleAction);
        _workingCopy.PreviousIcon.DoubleClick = GetComboValue(_prevDoubleAction);
        _workingCopy.PlayPauseIcon.SingleClick = GetComboValue(_playSingleAction);
        _workingCopy.PlayPauseIcon.DoubleClick = GetComboValue(_playDoubleAction);
        _workingCopy.NextIcon.SingleClick = GetComboValue(_nextSingleAction);
        _workingCopy.NextIcon.DoubleClick = GetComboValue(_nextDoubleAction);

        if (shouldKeepExistingFallbackPath) {
            MessageBox.Show(
                this,
                "Fallback executable path does not exist. Other settings were saved, and the previous fallback executable path was kept.",
                "Invalid fallback path",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
        else {
            _workingCopy.FallbackExecutablePath = path;
        }
        _workingCopy.FallbackPlayerType = GetFallbackPlayerTypeComboValue(_fallbackPlayerType);
        _workingCopy.FallbackActionWhenMediaActive = GetFallbackWhenMediaActiveComboValue(_fallbackWhenMediaActiveAction);

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

        SetComboValue(_prevSingleAction, defaults.PreviousIcon.SingleClick);
        SetComboValue(_prevDoubleAction, defaults.PreviousIcon.DoubleClick);
        SetComboValue(_playSingleAction, defaults.PlayPauseIcon.SingleClick);
        SetComboValue(_playDoubleAction, defaults.PlayPauseIcon.DoubleClick);
        SetComboValue(_nextSingleAction, defaults.NextIcon.SingleClick);
        SetComboValue(_nextDoubleAction, defaults.NextIcon.DoubleClick);

        _fallbackExePath.Text = defaults.FallbackExecutablePath;
        SetFallbackPlayerTypeComboValue(_fallbackPlayerType, defaults.FallbackPlayerType);
        SetFallbackWhenMediaActiveComboValue(_fallbackWhenMediaActiveAction, defaults.FallbackActionWhenMediaActive);
    }

    private static void SetComboValue(ComboBox combo, ClickAction value) {
        if (combo.Items.Count == 0) {
            return;
        }

        for (var i = 0; i < combo.Items.Count; i++) {
            if (combo.Items[i] is ClickAction action && action == value) {
                combo.SelectedIndex = i;
                return;
            }
        }

        if (combo.Items.Count > 0) {
            combo.SelectedIndex = 0;
        }
    }

    private static ClickAction GetComboValue(ComboBox combo) {
        if (combo.SelectedItem is ClickAction selected) {
            return selected;
        }

        return ClickAction.DoNothing;
    }

    private static void SetFallbackWhenMediaActiveComboValue(ComboBox combo, FallbackActionWhenMediaActive value) {
        if (combo.Items.Count == 0) {
            return;
        }

        for (var i = 0; i < combo.Items.Count; i++) {
            if (combo.Items[i] is FallbackActionWhenMediaActive action && action == value) {
                combo.SelectedIndex = i;
                return;
            }
        }

        combo.SelectedIndex = 0;
    }

    private static FallbackActionWhenMediaActive GetFallbackWhenMediaActiveComboValue(ComboBox combo) {
        if (combo.SelectedItem is FallbackActionWhenMediaActive selected) {
            return selected;
        }

        return FallbackActionWhenMediaActive.OpenCurrentMediaAppOrFallback;
    }

    private static void SetFallbackPlayerTypeComboValue(ComboBox combo, FallbackPlayerType value) {
        if (combo.Items.Count == 0) {
            return;
        }

        for (var i = 0; i < combo.Items.Count; i++) {
            if (combo.Items[i] is FallbackPlayerType playerType && playerType == value) {
                combo.SelectedIndex = i;
                return;
            }
        }

        combo.SelectedIndex = 0;
    }

    private static FallbackPlayerType GetFallbackPlayerTypeComboValue(ComboBox combo) {
        if (combo.SelectedItem is FallbackPlayerType selected) {
            return selected;
        }

        return FallbackPlayerType.Other;
    }
}
