using System;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace NppTranslatePanel.Forms
{
    /// <summary>Read-only Scintilla editor hosted inside the translation panel.</summary>
    internal sealed class ScintillaOutput : Control
    {
        private ScintillaGateway gateway;

        public ScintillaOutput()
        {
            SetStyle(ControlStyles.UserPaint, false);
            SetStyle(ControlStyles.Opaque, true);
            TabStop = false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ClassName = "Scintilla";
                parameters.Style |= 0x40000000; // WS_CHILD
                parameters.Style |= 0x10000000; // WS_VISIBLE
                parameters.Style |= 0x00200000; // WS_VSCROLL
                return parameters;
            }
        }

        public ScintillaGateway Gateway
        {
            get
            {
                if (!IsHandleCreated)
                    CreateControl();
                return gateway;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            gateway = new ScintillaGateway(Handle);
            gateway.SetCodePage(65001); // UTF-8
            gateway.SetReadOnly(true);
            gateway.SetHScrollBar(true);
            gateway.SetScrollWidthTracking(true);
            gateway.SetVScrollBar(true);
            gateway.SetMarginWidthN(0, 0);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            gateway = null;
            base.OnHandleDestroyed(e);
        }

        public void SetContent(string text)
        {
            ScintillaGateway editor = Gateway;
            if (editor == null)
                return;

            int firstVisibleLine = editor.GetFirstVisibleLine();
            editor.SetReadOnly(false);
            try
            {
                editor.SetText(text ?? string.Empty);
            }
            finally
            {
                editor.SetReadOnly(true);
            }
            editor.SetFirstVisibleLine(firstVisibleLine);
        }
    }
}
