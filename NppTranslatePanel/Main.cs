using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        static internal int IdTranslateNow = -1;
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
            PluginBase.SetCommand(1, Translator.GetTranslatedMenuItem("&Translate Now"), TranslateNow);
            IdTranslateNow = 1;
            PluginBase.SetCommand(2, Translator.GetTranslatedMenuItem("---"), null);
            PluginBase.SetCommand(3, Translator.GetTranslatedMenuItem("&Settings"), OpenSettings);
            PluginBase.SetCommand(4, Translator.GetTranslatedMenuItem("---"), null);
            PluginBase.SetCommand(5, Translator.GetTranslatedMenuItem("&About"), ShowAbout);
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
                if (settings.translate_on_tab_change)
                    watcher.TranslateNow();
                return;
            case (uint)SciMsg.SCN_MODIFIED:
                // Ignore styling, folding and marker notifications. Only actual text edits
                // should restart the debounce timer and potentially consume API quota.
                int textChangeMask = (int)SciMsg.SC_MOD_INSERTTEXT | (int)SciMsg.SC_MOD_DELETETEXT;
                if (settings.auto_translate_on_edit && (notification.ModificationType & textChangeMask) != 0)
                    watcher.NotifyTextChanged();
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
            isShuttingDown = true;
        }
        #endregion

        #region " Menu functions "

        static void OpenSettings()
        {
            using (var form = new SettingsForm(settings))
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

        static void TranslateNow()
        {
            if (translatePanel == null || translatePanel.IsDisposed || !translatePanel.Visible)
                ToggleTranslatePanel();
            else
                watcher.TranslateNow();
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
            if (translatePanel == null || translatePanel.IsDisposed)
            {
                translatePanel = new TranslatePanel();
                watcher.TranslationReady += translatePanel.SetTranslatedText;
                watcher.TranslationFailed += translatePanel.ShowError;
                watcher.TranslationStarted += translatePanel.ShowTranslating;
                watcher.TranslationCompleted += translatePanel.ShowCompleted;
                DisplayTranslatePanel(translatePanel);
            }
            else
            {
                Npp.notepad.ShowDockingForm(translatePanel);
            }
            watcher.Enabled = true;
            watcher.TranslateNow();
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
