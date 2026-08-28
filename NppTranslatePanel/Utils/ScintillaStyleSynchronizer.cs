using System;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace NppTranslatePanel.Utils
{
    /// <summary>Copies the active Notepad++ editor's visual styles and layout to another Scintilla control.</summary>
    internal static class ScintillaStyleSynchronizer
    {
        private const int StyleCount = 256;

        public static void Apply(ScintillaGateway source, ScintillaGateway target,
            bool copySyntaxHighlighting)
        {
            if (source == null || target == null)
                return;

            target.SetCodePage(65001);
            target.SetReadOnly(false);
            try
            {
                target.SetLexer((int)Lexer.CONTAINER);
                CopyStyles(source, target, copySyntaxHighlighting);
                target.SetTabWidth(source.GetTabWidth());
                target.SetIndent(source.GetIndent());
                target.SetUseTabs(source.GetUseTabs());
                target.SetIndentationGuides(source.GetIndentationGuides());
                target.SetWrapMode(source.GetWrapMode());
                target.SetZoom(source.GetZoom());
            }
            finally
            {
                target.SetReadOnly(true);
            }
        }

        private static void CopyStyles(ScintillaGateway source, ScintillaGateway target,
            bool copyAllStyles)
        {
            int firstStyle = copyAllStyles ? 0 : (int)SciMsg.STYLE_DEFAULT;
            int lastStyle = copyAllStyles ? StyleCount - 1 : (int)SciMsg.STYLE_DEFAULT;
            for (int style = firstStyle; style <= lastStyle; style++)
            {
                target.StyleSetFore(style, source.StyleGetFore(style));
                target.StyleSetBack(style, source.StyleGetBack(style));
                target.StyleSetBold(style, source.StyleGetBold(style));
                target.StyleSetItalic(style, source.StyleGetItalic(style));
                target.StyleSetUnderline(style, source.StyleGetUnderline(style));
                target.StyleSetSizeFractional(style, source.StyleGetSizeFractional(style));
                target.StyleSetWeight(style, source.StyleGetWeight(style));
                target.StyleSetCharacterSet(style, source.StyleGetCharacterSet(style));
                target.StyleSetEOLFilled(style, source.StyleGetEOLFilled(style));
                target.StyleSetCase(style, source.StyleGetCase(style));
                target.StyleSetVisible(style, source.StyleGetVisible(style));
                target.StyleSetHotSpot(style, source.StyleGetHotSpot(style));

                string font = source.StyleGetFont(style);
                if (!string.IsNullOrWhiteSpace(font))
                    target.StyleSetFont(style, font);
            }

            if (!copyAllStyles)
                target.StyleClearAll();
        }
    }
}
