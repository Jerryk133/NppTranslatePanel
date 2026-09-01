using System;
using System.Windows.Forms;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;
using NppTranslatePanel.Utils;
using NppTranslatePanel.Translation;

namespace NppTranslatePanel.Forms
{
    /// <summary>
    /// Dockable panel that shows a machine translation of the active document,
    /// refreshed by <see cref="Translation.ChangeWatcher"/>.
    /// </summary>
    public partial class TranslatePanel : FormBase
    {
        private readonly Timer scrollPollTimer;
        private int lastPanelFirstLine;
        private bool synchronizingScroll;
        private bool selectionOnly;
        private string translatedText = string.Empty;

        public TranslatePanel() : base(isModal: false, isDocking: true)
        {
            InitializeComponent();
            scrollPollTimer = new Timer { Interval = 100 };
            scrollPollTimer.Tick += (s, e) => PollPanelScroll();
            scrollPollTimer.Start();
            FormClosed += (s, e) => scrollPollTimer.Dispose();
            // stop translating once the panel is hidden/undocked/closed, so we don't keep
            // burning the translation API's daily quota into an invisible form
            VisibleChanged += (s, e) =>
            {
                if (!Visible)
                    Main.watcher.Enabled = false;
                else
                    ApplyEditorFont();
            };
        }

        /// <summary>
        /// Matches the translation output to the active editor's font and, when enabled,
        /// its lexer, syntax colors, keyword sets and layout settings.
        /// </summary>
        public void ApplyEditorFont()
        {
            if (Npp.editor == null || txtOutput == null || txtOutput.IsDisposed)
                return;

            try
            {
                ScintillaStyleSynchronizer.Apply(Npp.editor, txtOutput.Gateway,
                    Main.settings.match_source_syntax_highlighting);
                ContainerSyntaxHighlighter.Apply(txtOutput.Gateway, translatedText,
                    Main.settings.match_source_syntax_highlighting);
            }
            catch (InvalidOperationException)
            {
                // Keep the panel usable if the native Scintilla control is temporarily unavailable.
            }
        }

        public void SetTranslatedText(string text)
        {
            translatedText = text ?? string.Empty;
            txtOutput.SetContent(translatedText);
            ContainerSyntaxHighlighter.Apply(txtOutput.Gateway, translatedText,
                Main.settings.match_source_syntax_highlighting);
            lblStatus.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss");
            if (selectionOnly)
                ScrollPanelToLine(0);
            else
                SyncFromEditor();
        }

        public void ShowError(string message)
        {
            lblStatus.Text = "Error: " + message;
        }

        public void ShowTranslating(TranslationRunInfo info)
        {
            lblStatus.Text = info.SelectionOnly
                ? string.Format("Translating selection ({0:N0} characters) with {1}...",
                    info.CharacterCount, info.Provider)
                : string.Format("Translating {0:N0} characters with {1}...",
                    info.CharacterCount, info.Provider);
        }

        public void ShowCompleted(TranslationRunInfo info)
        {
            lblStatus.Text = string.Format(
                "{0} | {1}{2:N0} chars | {3} API request{4} | {5} cached | {6:0.0}s",
                info.Provider, info.SelectionOnly ? "Selection | " : string.Empty,
                info.CharacterCount, info.ApiRequests,
                info.ApiRequests == 1 ? string.Empty : "s", info.CacheHits,
                info.Duration.TotalSeconds);
        }

        public void SetSelectionOnly(bool value)
        {
            selectionOnly = value;
        }

        public void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        /// <summary>Moves the translation panel to the same proportional position as the editor.</summary>
        public void SyncFromEditor()
        {
            if (selectionOnly || !Main.settings.synchronize_scrolling || synchronizingScroll
                || Npp.editor == null || txtOutput == null || !txtOutput.IsHandleCreated)
                return;

            int documentLines = Math.Max(1, Npp.editor.GetLineCount());
            int lastDocumentLine = documentLines - 1;
            int totalDisplayLines = Npp.editor.VisibleFromDocLine(lastDocumentLine)
                + Math.Max(1, Npp.editor.WrapCount(lastDocumentLine));
            int editorRange = Math.Max(1, totalDisplayLines - Npp.editor.LinesOnScreen());
            double position = Math.Max(0, Math.Min(1,
                (double)Npp.editor.GetFirstVisibleLine() / editorRange));

            int panelRange = Math.Max(0, GetPanelLineCount() - GetPanelVisibleLineCount());
            int targetLine = (int)Math.Round(position * panelRange);
            ScrollPanelToLine(targetLine);
        }

        private void PollPanelScroll()
        {
            try
            {
                if (IsDisposed || !Visible || selectionOnly
                    || !Main.settings.synchronize_scrolling || synchronizingScroll
                    || txtOutput.IsDisposed || !txtOutput.IsHandleCreated)
                    return;

                int firstLine = GetPanelFirstVisibleLine();
                if (firstLine == lastPanelFirstLine)
                    return;

                lastPanelFirstLine = firstLine;
                SyncEditorFromPanel(firstLine);
            }
            catch (ObjectDisposedException)
            {
                scrollPollTimer.Stop();
            }
        }

        private void SyncEditorFromPanel(int firstPanelLine)
        {
            if (Npp.editor == null)
                return;

            int panelRange = Math.Max(1, GetPanelLineCount() - GetPanelVisibleLineCount());
            double position = Math.Max(0, Math.Min(1, (double)firstPanelLine / panelRange));

            int documentLines = Math.Max(1, Npp.editor.GetLineCount());
            int lastDocumentLine = documentLines - 1;
            int totalDisplayLines = Npp.editor.VisibleFromDocLine(lastDocumentLine)
                + Math.Max(1, Npp.editor.WrapCount(lastDocumentLine));
            int editorRange = Math.Max(0, totalDisplayLines - Npp.editor.LinesOnScreen());

            synchronizingScroll = true;
            try
            {
                Npp.editor.SetFirstVisibleLine((int)Math.Round(position * editorRange));
            }
            finally
            {
                synchronizingScroll = false;
            }
        }

        private void ScrollPanelToLine(int targetLine)
        {
            int currentLine = GetPanelFirstVisibleLine();
            if (currentLine == targetLine)
            {
                lastPanelFirstLine = currentLine;
                return;
            }

            synchronizingScroll = true;
            try
            {
                txtOutput.Gateway.SetFirstVisibleLine(targetLine);
                lastPanelFirstLine = GetPanelFirstVisibleLine();
            }
            finally
            {
                synchronizingScroll = false;
            }
        }

        private int GetPanelFirstVisibleLine()
        {
            return txtOutput.Gateway.GetFirstVisibleLine();
        }

        private int GetPanelLineCount()
        {
            return Math.Max(1, txtOutput.Gateway.GetLineCount());
        }

        private int GetPanelVisibleLineCount()
        {
            return Math.Max(1, txtOutput.Gateway.LinesOnScreen());
        }
    }
}
