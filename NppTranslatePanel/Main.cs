using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;
using NppTranslatePanel.Forms;
using NppTranslatePanel.Translation;
using NppTranslatePanel.Utils;
using static Kbg.NppPluginNET.PluginInfrastructure.Win32;

namespace Kbg.NppPluginNET
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "NppTranslatePanel";
        public static readonly string PluginConfigDirectory = Path.Combine(Npp.notepad.GetConfigDirectory(), PluginName);
        public static Settings settings = new Settings();
        public static bool isShuttingDown = false;

        public static TranslatePanel translatePanel = null;
        public static readonly ChangeWatcher watcher = new ChangeWatcher
        {
            Translator = new MyMemoryTranslator()
        };

        static internal int IdTranslatePanel = -1;
        static internal int IdTranslateDocument = -1;
        static internal int IdTranslateSelection = -1;
        static internal int IdTranslate = -1;
        private static bool privacyConfirmationVisible;
        private static bool creatingTranslationDocument;
        private static ToolbarIconSet toolbarIconSet;
        #endregion

        #region " Startup/CleanUp "

        static internal void CommandMenuInit()
        {
            // first make it so that all references to any third-party dependencies point to the correct location
            AppDomain.CurrentDomain.AssemblyResolve += LoadDependency;

            // load translations at startup
            Translator.ResetTranslations(true);

            ApplySettingsToWatcher();

            PluginBase.SetCommand(0, Translator.GetTranslatedMenuItem("&Show Translate Panel"), ToggleTranslatePanel);
            IdTranslatePanel = 0;
            PluginBase.SetCommand(1, Translator.GetTranslatedMenuItem("Translate &Document"), TranslateDocument);
            IdTranslateDocument = 1;
            PluginBase.SetCommand(2, Translator.GetTranslatedMenuItem("Translate &Selection"), TranslateSelection);
            IdTranslateSelection = 2;
            PluginBase.SetCommand(3, Translator.GetTranslatedMenuItem("&Translate"), Translate);
            IdTranslate = 3;
            PluginBase.SetCommand(4, Translator.GetTranslatedMenuItem("Open Translation in New &Tab"), OpenTranslationInNewTab);
            PluginBase.SetCommand(5, Translator.GetTranslatedMenuItem("&Save Translation As..."), SaveTranslationAs);
            PluginBase.SetCommand(6, Translator.GetTranslatedMenuItem("---"), null);
            PluginBase.SetCommand(7, Translator.GetTranslatedMenuItem("&Settings"), OpenSettings);
            PluginBase.SetCommand(8, Translator.GetTranslatedMenuItem("---"), null);
            PluginBase.SetCommand(9, Translator.GetTranslatedMenuItem("&About"), ShowAbout);
        }

        private static Assembly LoadDependency(object sender, ResolveEventArgs args)
        {
            string assemblyFile = Path.Combine(Npp.pluginDllDirectory, new AssemblyName(args.Name).Name) + ".dll";
            if (File.Exists(assemblyFile))
                return Assembly.LoadFrom(assemblyFile);
            return null;
        }

        /// <summary>push the current Settings values into the running ChangeWatcher/translator; called at startup and whenever settings change</summary>
        public static void ApplySettingsToWatcher()
        {
            watcher.SourceLang = settings.source_language;
            watcher.TargetLang = settings.target_language;
            watcher.DebounceMs = settings.debounce_ms;
            watcher.ResetCache();
            if (string.Equals(settings.translator_provider, "DeepL", StringComparison.OrdinalIgnoreCase))
            {
                watcher.Translator = new DeepLTranslator
                {
                    ApiKey = SecretProtector.Unprotect(settings.deepl_api_key),
                    UseFreeApi = settings.deepl_use_free_api
                };
            }
            else
            {
                watcher.Translator = new MyMemoryTranslator
                {
                    ContactEmail = settings.mymemory_contact_email
                };
            }
        }

        public static void OnNotification(ScNotification notification)
        {
            uint code = notification.Header.Code;
            switch (code)
            {
            case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                // a new buffer became active; reconnect to its Scintilla instance and refresh the translation for it
                Npp.editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                if (translatePanel != null && !translatePanel.IsDisposed)
                    translatePanel.ApplyEditorFont();
                if (!creatingTranslationDocument && settings.translate_on_tab_change
                    && watcher.Enabled && EnsurePrivacyConsent())
                    TranslateCurrentDocument();
                return;
            case (uint)SciMsg.SCN_MODIFIED:
                // Ignore styling, folding and marker notifications. Only actual text edits
                // should restart the debounce timer and potentially consume API quota.
                int textChangeMask = (int)SciMsg.SC_MOD_INSERTTEXT | (int)SciMsg.SC_MOD_DELETETEXT;
                if (!creatingTranslationDocument && settings.auto_translate_on_edit && watcher.Enabled
                    && (notification.ModificationType & textChangeMask) != 0
                    && EnsurePrivacyConsent())
                {
                    SetSelectionOnly(false);
                    watcher.NotifyTextChanged();
                }
                break;
            case (uint)SciMsg.SCN_ZOOM:
                if (translatePanel != null && !translatePanel.IsDisposed
                    && translatePanel.Visible)
                {
                    translatePanel.SyncZoomFromEditor();
                }
                break;
            case (uint)SciMsg.SCN_UPDATEUI:
                if (settings.synchronize_scrolling
                    && (notification.Updated & (int)SciMsg.SC_UPDATE_V_SCROLL) != 0
                    && translatePanel != null && !translatePanel.IsDisposed && translatePanel.Visible)
                {
                    translatePanel.SyncFromEditor();
                }
                break;
            case (uint)NppMsg.NPPN_WORDSTYLESUPDATED:
                RestyleEverything();
                return;
            case (uint)NppMsg.NPPN_NATIVELANGCHANGED:
                Translator.ResetTranslations(false);
                break;
            }
        }

        static internal void PluginCleanUp()
        {
            if (translatePanel != null && !translatePanel.IsDisposed)
            {
                translatePanel.Close();
                translatePanel.Dispose();
            }
            toolbarIconSet?.Dispose();
            toolbarIconSet = null;
            isShuttingDown = true;
        }

        static internal void RegisterToolbarIcon()
        {
            if (IdTranslate < 0)
                return;

            try
            {
                if (toolbarIconSet == null)
                    toolbarIconSet = new ToolbarIconSet();
                Npp.notepad.AddToolbarIcon(IdTranslate, toolbarIconSet.Handles);
            }
            catch (Exception ex)
            {
                // A toolbar button is an optional convenience. Never let a graphics or
                // compatibility problem prevent Notepad++ from loading the plugin.
                Debug.WriteLine("NppTranslatePanel toolbar icon registration failed: " + ex.Message);
            }
        }
        #endregion

        #region " Menu functions "

        static void OpenSettings()
        {
            using (var form = new SettingsForm(settings, watcher.ResetCache))
                form.ShowDialog();
        }

        static void ShowAbout()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string message =
                "NppTranslatePanel " + version + "\r\n\r\n" +
                "Live translation of the active Notepad++ document in a synchronized dockable panel.\r\n\r\n" +
                "Translation providers: DeepL and MyMemory\r\n" +
                "Framework: .NET Framework 4.8\r\n" +
                "License: Apache License 2.0";
            MessageBox.Show(message, "About NppTranslatePanel",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void RestyleEverything()
        {
            if (translatePanel != null && !translatePanel.IsDisposed)
            {
                FormStyle.ApplyStyle(translatePanel, settings.use_npp_styling);
                translatePanel.ApplyEditorFont();
            }
        }

        static void TranslateDocument()
        {
            if (translatePanel == null || translatePanel.IsDisposed || !translatePanel.Visible)
                ShowTranslatePanel();
            if (EnsurePrivacyConsent())
                TranslateCurrentDocument();
        }

        static void Translate()
        {
            if (ShouldTranslateSelection(Npp.editor?.GetSelectionLength() ?? 0))
                TranslateSelection();
            else
                TranslateDocument();
        }

        internal static bool ShouldTranslateSelection(int selectionLength)
        {
            return selectionLength > 0;
        }

        static void TranslateSelection()
        {
            if (Npp.editor == null || Npp.editor.GetSelectionLength() <= 0)
            {
                MessageBox.Show("Select the text to translate, then run Translate Selection again.",
                    "No text selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedText = Npp.editor.GetSelText();
            if (string.IsNullOrEmpty(selectedText))
                return;

            if (translatePanel == null || translatePanel.IsDisposed || !translatePanel.Visible)
                ShowTranslatePanel();
            if (!EnsurePrivacyConsent())
                return;

            SetSelectionOnly(true);
            _ = watcher.TranslateTextNow(selectedText, true);
        }

        internal static void OpenTranslationInNewTab()
        {
            if (!TryGetTranslationOutput(out TranslationOutput output))
                return;

            creatingTranslationDocument = true;
            try
            {
                Npp.notepad.FileNew();
                Npp.editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                Npp.editor.SetCodePage(65001);
                Npp.editor.SetText(output.Text);
                Npp.notepad.SetCurrentLanguage(output.SourceLanguage);
                Npp.editor.SetCurrentPos(0);
                Npp.editor.SetAnchor(0);
                translatePanel.ApplyEditorFont();
                translatePanel.SetStatus("Translation opened in a new tab. Use Ctrl+S to save it.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("The translation could not be opened in a new tab.\r\n\r\n" + ex.Message,
                    "Open Translation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                creatingTranslationDocument = false;
            }
        }

        internal static void SaveTranslationAs()
        {
            if (!TryGetTranslationOutput(out TranslationOutput output))
                return;

            using (var dialog = new SaveFileDialog
            {
                AddExtension = true,
                CheckPathExists = true,
                OverwritePrompt = true,
                FileName = TranslationExport.BuildSuggestedFileName(
                    output.SourceFilePath, output.TargetLanguage, output.SelectionOnly),
                Filter = TranslationExport.BuildFileFilter(output.SourceFilePath),
                DefaultExt = TranslationExport.GetDefaultExtension(output.SourceFilePath),
                Title = "Save Translation As"
            })
            {
                string sourceDirectory = TranslationExport.GetExistingSourceDirectory(output.SourceFilePath);
                if (!string.IsNullOrEmpty(sourceDirectory))
                    dialog.InitialDirectory = sourceDirectory;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                if (TranslationExport.PathsEqual(dialog.FileName, output.SourceFilePath))
                {
                    DialogResult overwrite = MessageBox.Show(
                        "This is the original source document. Saving will overwrite it with the translation.\r\n\r\nContinue?",
                        "Overwrite Source Document", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (overwrite != DialogResult.Yes)
                        return;
                }

                try
                {
                    File.WriteAllText(dialog.FileName, output.Text, new UTF8Encoding(false));
                    translatePanel.SetStatus("Translation saved to " + dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The translation could not be saved.\r\n\r\n" + ex.Message,
                        "Save Translation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static bool TryGetTranslationOutput(out TranslationOutput output)
        {
            if (translatePanel != null && !translatePanel.IsDisposed
                && translatePanel.TryGetTranslationOutput(out output))
                return true;

            output = null;
            MessageBox.Show("Translate a document or selection before using this command.",
                "No Translation Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        static void ToggleTranslatePanel()
        {
            bool wasVisible = translatePanel != null && !translatePanel.IsDisposed && translatePanel.Visible;
            if (wasVisible)
            {
                Npp.notepad.HideDockingForm(translatePanel);
                watcher.Enabled = false;
                return;
            }

            ShowTranslatePanel();
            if (settings.auto_translate_on_edit || settings.translate_on_tab_change)
            {
                if (EnsurePrivacyConsent())
                    TranslateCurrentDocument();
            }
            else
                translatePanel.SetStatus("Ready. Use Translate Document to translate this document.");
        }

        private static bool EnsurePrivacyConsent()
        {
            string provider = PrivacyConsent.NormalizeProvider(settings.translator_provider);
            if (PrivacyConsent.IsAccepted(provider, settings.privacy_notice_accepted_provider))
                return true;

            if (privacyConfirmationVisible)
                return false;

            privacyConfirmationVisible = true;
            try
            {
                string displayProvider = string.IsNullOrWhiteSpace(provider)
                    ? "the selected translation provider"
                    : provider;
                string message =
                    "NppTranslatePanel will send the text requested for translation to " +
                    displayProvider + " over the internet. " +
                    "The service processes the submitted text according to its own privacy policy.\r\n\r\n" +
                    "Do not continue with confidential or sensitive content unless sending it to this " +
                    "third-party service is acceptable and permitted.\r\n\r\n" +
                    "Continue and remember this choice for " + displayProvider + "?";
                DialogResult result = MessageBox.Show(message, "Privacy confirmation",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    settings.privacy_notice_accepted_provider = provider;
                    settings.SaveToIniFile();
                    return true;
                }

                settings.auto_translate_on_edit = false;
                settings.translate_on_tab_change = false;
                settings.SaveToIniFile();
                if (translatePanel != null && !translatePanel.IsDisposed)
                    translatePanel.SetStatus("Translation cancelled. No document text was sent.");
                return false;
            }
            finally
            {
                privacyConfirmationVisible = false;
            }
        }

        private static void TranslateCurrentDocument()
        {
            SetSelectionOnly(false);
            watcher.TranslateDocumentNow();
        }

        private static void SetSelectionOnly(bool value)
        {
            if (translatePanel != null && !translatePanel.IsDisposed)
                translatePanel.SetSelectionOnly(value);
        }

        private static void ShowTranslatePanel()
        {
            if (translatePanel == null || translatePanel.IsDisposed)
            {
                translatePanel = new TranslatePanel();
                watcher.TranslationReady += translatePanel.SetTranslatedText;
                watcher.TranslationFailed += translatePanel.ShowError;
                watcher.TranslationStarted += HandleTranslationStarted;
                watcher.TranslationCompleted += translatePanel.ShowCompleted;
                translatePanel.ApplyEditorFont();
                DisplayTranslatePanel(translatePanel);
            }
            else
            {
                translatePanel.ApplyEditorFont();
                Npp.notepad.ShowDockingForm(translatePanel);
            }
            watcher.Enabled = true;
        }

        private static void HandleTranslationStarted(TranslationRunInfo info)
        {
            if (translatePanel == null || translatePanel.IsDisposed)
                return;

            translatePanel.PrepareTranslationOutput(
                Npp.notepad.GetCurrentFilePath(),
                Npp.notepad.GetCurrentLanguage(),
                settings.target_language,
                info.SelectionOnly);
            translatePanel.ShowTranslating(info);
        }

        private static void DisplayTranslatePanel(TranslatePanel form)
        {
            NppTbData _nppTbData = new NppTbData();
            _nppTbData.hClient = form.Handle;
            _nppTbData.pszName = "Translate";
            _nppTbData.dlgID = IdTranslatePanel;
            _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT;
            _nppTbData.pszModuleName = PluginName;
            IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
            Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

            // NOTE: deliberately not freeing _ptrNppTbData - Notepad++'s docking manager
            // keeps referring to this block for the lifetime of the session (e.g. when
            // saving/restoring dock position), matching the upstream plugin pack's own pattern.
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            Npp.notepad.ShowDockingForm(form);
        }
        #endregion
    }
}
