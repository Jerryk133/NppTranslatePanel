using System;
using System.Drawing;
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
        private const uint EmGetFirstVisibleLine = 0x00CE;
        private const uint EmGetLineCount = 0x00BA;
        private const uint EmLineScroll = 0x00B6;

        private Font editorFont;
        private int panelLineHeight = 16;
        private readonly Timer scrollPollTimer;
        private int lastPanelFirstLine;
        private bool synchronizingScroll;

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

        /// <summary>Matches the translation output font to Scintilla's default editor style.</summary>
        public void ApplyEditorFont()
        {
            if (Npp.editor == null || txtOutput == null || txtOutput.IsDisposed)
                return;

            try
            {
                int style = (int)SciMsg.STYLE_DEFAULT;
                string family = Npp.editor.StyleGetFont(style);
                int fractionalSize = Npp.editor.StyleGetSizeFractional(style);
                float size = fractionalSize > 0
                    ? fractionalSize / 100f
                    : Npp.editor.StyleGetSize(style);
                if (string.IsNullOrWhiteSpace(family) || size <= 0)
                    return;

                FontStyle fontStyle = FontStyle.Regular;
                if (Npp.editor.StyleGetBold(style))
                    fontStyle |= FontStyle.Bold;
                if (Npp.editor.StyleGetItalic(style))
                    fontStyle |= FontStyle.Italic;

                var newFont = new Font(family, size, fontStyle, GraphicsUnit.Point);
                editorFont = newFont;
                panelLineHeight = Math.Max(1, (int)Math.Ceiling(newFont.GetHeight()));
                txtOutput.Font = newFont;
                // Do not dispose the previous Font here. WinForms may still retain it internally
                // while processing layout and paint messages, which makes Font.Height throw.
            }
            catch (ArgumentException)
            {
                // Keep the current UI font if the configured editor font is unavailable.
            }
        }

        public void SetTranslatedText(string text)
        {
            int caretPos = Math.Min(txtOutput.SelectionStart, text.Length);
            txtOutput.Text = text;
            txtOutput.SelectionStart = caretPos;
            txtOutput.ScrollToCaret();
            lblStatus.Text = "Updated " + DateTime.Now.ToString("HH:mm:ss");
            SyncFromEditor();
        }

        public void ShowError(string message)
        {
            lblStatus.Text = "Error: " + message;
        }

        public void ShowTranslating(TranslationRunInfo info)
        {
            lblStatus.Text = string.Format("Translating {0:N0} characters with {1}...",
                info.CharacterCount, info.Provider);
        }

        public void ShowCompleted(TranslationRunInfo info)
        {
            lblStatus.Text = string.Format(
                "{0} | {1:N0} chars | {2} API request{3} | {4} cached | {5:0.0}s",
                info.Provider, info.CharacterCount, info.ApiRequests,
                info.ApiRequests == 1 ? string.Empty : "s", info.CacheHits,
                info.Duration.TotalSeconds);
        }

        public void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        /// <summary>Moves the translation panel to the same proportional position as the editor.</summary>
        public void SyncFromEditor()
        {
            if (!Main.settings.synchronize_scrolling || synchronizingScroll
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
                if (IsDisposed || !Visible || !Main.settings.synchronize_scrolling || synchronizingScroll
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
                Win32.SendMessage(txtOutput.Handle, EmLineScroll, IntPtr.Zero,
                    new IntPtr(targetLine - currentLine));
                lastPanelFirstLine = GetPanelFirstVisibleLine();
            }
            finally
            {
                synchronizingScroll = false;
            }
        }

        private int GetPanelFirstVisibleLine()
        {
            return (int)Win32.SendMessage(txtOutput.Handle, EmGetFirstVisibleLine,
                IntPtr.Zero, IntPtr.Zero);
        }

        private int GetPanelLineCount()
        {
            return Math.Max(1, (int)Win32.SendMessage(txtOutput.Handle, EmGetLineCount,
                IntPtr.Zero, IntPtr.Zero));
        }

        private int GetPanelVisibleLineCount()
        {
            return Math.Max(1, txtOutput.ClientSize.Height / Math.Max(1, panelLineHeight));
        }
    }
}
