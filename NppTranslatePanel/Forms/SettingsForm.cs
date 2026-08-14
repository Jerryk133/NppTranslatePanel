using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using NppTranslatePanel.Utils;

namespace NppTranslatePanel.Forms
{
    /// <summary>User-friendly settings dialog grouped by translator and application behavior.</summary>
    public sealed class SettingsForm : Form
    {
        private readonly Settings settings;
        private readonly RadioButton myMemoryProvider;
        private readonly RadioButton deepLProvider;
        private readonly TextBox sourceLanguage;
        private readonly ComboBox targetLanguage;
        private readonly TextBox contactEmail;
        private readonly TextBox deepLApiKey;
        private readonly CheckBox deepLUseFreeApi;
        private readonly NumericUpDown debounce;
        private readonly CheckBox autoTranslate;
        private readonly CheckBox translateOnTabChange;
        private readonly CheckBox synchronizeScrolling;
        private readonly CheckBox useNppStyling;

        public SettingsForm(Settings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            Text = "NppTranslatePanel Settings";
            Name = "SettingsForm";
            ClientSize = new Size(560, 440);
            MinimumSize = new Size(500, 410);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            Padding = new Padding(12);

            var tabs = new TabControl { Dock = DockStyle.Fill, Name = "SettingsTabs" };
            var translatorTab = new TabPage("Translator") { Name = "TranslatorTab", Padding = new Padding(14) };
            var behaviorTab = new TabPage("Application Behavior") { Name = "BehaviorTab", Padding = new Padding(14) };
            tabs.TabPages.Add(translatorTab);
            tabs.TabPages.Add(behaviorTab);

            var translatorLayout = CreateLayout();
            translatorTab.Controls.Add(translatorLayout);

            var providerBox = new GroupBox
            {
                Text = "Translation service",
                AutoSize = true,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 12)
            };
            var providerChoices = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            myMemoryProvider = new RadioButton
            {
                Text = "MyMemory — free, no API key required",
                AutoSize = true,
                Checked = !string.Equals(settings.translator_provider, "DeepL", StringComparison.OrdinalIgnoreCase)
            };
            deepLProvider = new RadioButton
            {
                Text = "DeepL — API key required",
                AutoSize = true,
                Checked = string.Equals(settings.translator_provider, "DeepL", StringComparison.OrdinalIgnoreCase)
            };
            providerChoices.Controls.Add(myMemoryProvider);
            providerChoices.Controls.Add(deepLProvider);
            providerBox.Controls.Add(providerChoices);
            AddWideRow(translatorLayout, providerBox);

            sourceLanguage = AddTextField(translatorLayout, "Source language", settings.source_language,
                "Language code such as en, cs, or de.");
            targetLanguage = AddTargetLanguageField(translatorLayout);
            SelectTargetLanguage(settings.target_language);
            contactEmail = AddTextField(translatorLayout, "Contact email (optional)", settings.mymemory_contact_email,
                "Used by MyMemory to increase the documented free daily quota.");
            deepLApiKey = AddTextField(translatorLayout, "DeepL API key", SecretProtector.Unprotect(settings.deepl_api_key),
                "Stored locally using Windows encryption for your user account.");
            deepLApiKey.UseSystemPasswordChar = true;
            deepLUseFreeApi = AddCheckBox(translatorLayout, "Use DeepL API Free endpoint", settings.deepl_use_free_api);
            myMemoryProvider.CheckedChanged += (s, e) => UpdateProviderControls();
            deepLProvider.CheckedChanged += (s, e) => UpdateProviderControls();
            UpdateProviderControls();

            var behaviorLayout = CreateLayout();
            behaviorTab.Controls.Add(behaviorLayout);

            autoTranslate = AddCheckBox(behaviorLayout,
                "Translate automatically after typing stops", settings.auto_translate_on_edit);

            debounce = new NumericUpDown
            {
                Minimum = 200,
                Maximum = 10000,
                Increment = 100,
                Value = Math.Max(200, Math.Min(10000, settings.debounce_ms)),
                Width = 100
            };
            var delayPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 5, 0, 8)
            };
            delayPanel.Controls.Add(debounce);
            delayPanel.Controls.Add(new Label
            {
                Text = "milliseconds",
                AutoSize = true,
                Margin = new Padding(7, 5, 0, 0)
            });
            AddFieldRow(behaviorLayout, "Delay after typing", delayPanel);

            translateOnTabChange = AddCheckBox(behaviorLayout,
                "Translate immediately when switching document tabs", settings.translate_on_tab_change);
            synchronizeScrolling = AddCheckBox(behaviorLayout,
                "Synchronize scrolling between editor and translation", settings.synchronize_scrolling);
            AddWideRow(behaviorLayout, new Label
            {
                Text = "Translation is paused while the Translate panel is hidden to avoid unnecessary API usage.",
                AutoSize = true,
                MaximumSize = new Size(465, 0),
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(22, 0, 0, 18)
            });
            useNppStyling = AddCheckBox(behaviorLayout,
                "Use Notepad++ editor colors for plugin windows", settings.use_npp_styling);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            var saveButton = new Button { Text = "Save", AutoSize = true };
            var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            var resetButton = new Button { Text = "Reset to Defaults", AutoSize = true };
            saveButton.Click += SaveButton_Click;
            resetButton.Click += ResetButton_Click;
            buttons.Controls.Add(saveButton);
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(resetButton);

            Controls.Add(tabs);
            Controls.Add(buttons);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += (s, e) =>
            {
                Translator.TranslateForm(this);
                FormStyle.ApplyStyle(this, settings.use_npp_styling);
            };
        }

        private static TableLayoutPanel CreateLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 0,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return layout;
        }

        private static void AddWideRow(TableLayoutPanel layout, Control control)
        {
            int row = layout.RowCount;
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Fill;
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        private static void AddFieldRow(TableLayoutPanel layout, string label, Control field)
        {
            int row = layout.RowCount;
            layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 10, 12, 0)
            }, 0, row);
            layout.Controls.Add(field, 1, row);
        }

        private static TextBox AddTextField(TableLayoutPanel layout, string label, string value, string help)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 5, 0, 8)
            };
            var input = new TextBox { Text = value ?? string.Empty, Dock = DockStyle.Top };
            panel.Controls.Add(input, 0, 0);
            panel.Controls.Add(new Label
            {
                Text = help,
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 3, 0, 0)
            }, 0, 1);
            AddFieldRow(layout, label, panel);
            return input;
        }

        private static CheckBox AddCheckBox(TableLayoutPanel layout, string text, bool value)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Checked = value,
                AutoSize = true,
                Margin = new Padding(3, 8, 0, 8)
            };
            AddWideRow(layout, checkBox);
            return checkBox;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            string source = sourceLanguage.Text.Trim();
            string target = GetSelectedTargetLanguage();
            if (source.Length == 0 || target.Length == 0)
            {
                MessageBox.Show(this, "Source and target language codes are required.",
                    "Invalid language", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (deepLProvider.Checked && string.IsNullOrWhiteSpace(deepLApiKey.Text))
            {
                MessageBox.Show(this, "A DeepL API key is required when DeepL is selected.",
                    "Missing API key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            settings.translator_provider = deepLProvider.Checked ? "DeepL" : "MyMemory";
            settings.source_language = source;
            settings.target_language = target;
            settings.mymemory_contact_email = contactEmail.Text.Trim();
            settings.deepl_api_key = SecretProtector.Protect(deepLApiKey.Text.Trim());
            settings.deepl_use_free_api = deepLUseFreeApi.Checked;
            settings.debounce_ms = Decimal.ToInt32(debounce.Value);
            settings.auto_translate_on_edit = autoTranslate.Checked;
            settings.translate_on_tab_change = translateOnTabChange.Checked;
            settings.synchronize_scrolling = synchronizeScrolling.Checked;
            settings.use_npp_styling = useNppStyling.Checked;
            settings.OnSettingsChanged();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            myMemoryProvider.Checked = true;
            sourceLanguage.Text = "en";
            SelectTargetLanguage(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            contactEmail.Clear();
            deepLApiKey.Clear();
            deepLUseFreeApi.Checked = true;
            debounce.Value = 1000;
            autoTranslate.Checked = true;
            translateOnTabChange.Checked = true;
            synchronizeScrolling.Checked = true;
            useNppStyling.Checked = true;
        }

        private void UpdateProviderControls()
        {
            bool useDeepL = deepLProvider.Checked;
            contactEmail.Enabled = !useDeepL;
            deepLApiKey.Enabled = useDeepL;
            deepLUseFreeApi.Enabled = useDeepL;
        }

        private ComboBox AddTargetLanguageField(TableLayoutPanel layout)
        {
            CultureInfo systemCulture = CultureInfo.CurrentUICulture;
            string systemCode = systemCulture.TwoLetterISOLanguageName;
            var combo = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            combo.Items.Add(new LanguageOption(
                "System language — " + systemCulture.EnglishName + " (" + systemCode + ")", systemCode));
            combo.Items.Add(new LanguageOption("Czech (cs)", "cs"));
            combo.Items.Add(new LanguageOption("English (en)", "en"));
            combo.Items.Add(new LanguageOption("German (de)", "de"));
            combo.Items.Add(new LanguageOption("Slovak (sk)", "sk"));
            combo.Items.Add(new LanguageOption("Polish (pl)", "pl"));
            combo.Items.Add(new LanguageOption("French (fr)", "fr"));
            combo.Items.Add(new LanguageOption("Spanish (es)", "es"));
            combo.Items.Add(new LanguageOption("Italian (it)", "it"));
            combo.Items.Add(new LanguageOption("Dutch (nl)", "nl"));
            combo.Items.Add(new LanguageOption("Portuguese (pt)", "pt"));
            combo.Items.Add(new LanguageOption("Ukrainian (uk)", "uk"));

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 5, 0, 8)
            };
            panel.Controls.Add(combo, 0, 0);
            panel.Controls.Add(new Label
            {
                Text = "Choose the Windows system language or enter another supported language code.",
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 3, 0, 0)
            }, 0, 1);
            AddFieldRow(layout, "Target language", panel);

            return combo;
        }

        private void SelectTargetLanguage(string code)
        {
            if (targetLanguage == null)
                return;
            foreach (object item in targetLanguage.Items)
            {
                if (item is LanguageOption option
                    && string.Equals(option.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    targetLanguage.SelectedItem = item;
                    return;
                }
            }
            targetLanguage.SelectedIndex = -1;
            targetLanguage.Text = code ?? string.Empty;
        }

        private string GetSelectedTargetLanguage()
        {
            if (targetLanguage.SelectedItem is LanguageOption option)
                return option.Code;
            return targetLanguage.Text.Trim();
        }

        private sealed class LanguageOption
        {
            public string Label { get; }
            public string Code { get; }

            public LanguageOption(string label, string code)
            {
                Label = label;
                Code = code;
            }

            public override string ToString() => Label;
        }
    }
}
