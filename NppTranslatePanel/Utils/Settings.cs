using System.ComponentModel;
using CsvQuery.PluginInfrastructure;
using Kbg.NppPluginNET;

namespace NppTranslatePanel.Utils
{
    /// <summary>
    /// Manages application settings
    /// </summary>
    public class Settings : SettingsBase
    {
        /// <inheritdoc />
        public override void OnSettingsChanged()
        {
            base.OnSettingsChanged();
            Main.RestyleEverything();
            Main.ApplySettingsToWatcher();
        }

        #region TRANSLATION
        [Description("Language code of the text you are writing, e.g. \"en\", \"cs\", \"de\". " +
                    "See https://mymemory.translated.net for the list of supported codes."),
            Category("Translation"), DefaultValue("en")]
        public string source_language { get; set; }

        [Description("Language code to translate into, e.g. \"cs\", \"en\", \"de\"."),
            Category("Translation"), DefaultValue("cs")]
        public string target_language { get; set; }

        [Description("How long to wait, in milliseconds, after you stop typing before requesting a translation."),
            Category("Translation"), DefaultValue(1000)]
        public int debounce_ms { get; set; }

        [Description("Optional email address passed to the MyMemory translation API. " +
                    "Raises the free daily quota from 5,000 to 50,000 characters. Leave empty to stay anonymous."),
            Category("Translation"), DefaultValue("")]
        public string mymemory_contact_email { get; set; }

        [Description("Translation provider used by the plugin."),
            Category("Translation"), DefaultValue("MyMemory")]
        public string translator_provider { get; set; }

        [Description("DeepL API key encrypted for the current Windows user."),
            Category("Translation"), DefaultValue("")]
        public string deepl_api_key { get; set; }

        [Description("Use the DeepL API Free endpoint instead of the DeepL API Pro endpoint."),
            Category("Translation"), DefaultValue(true)]
        public bool deepl_use_free_api { get; set; }
        #endregion

        #region BEHAVIOR
        [Description("Automatically translate after the document text changes."),
            Category("Behavior"), DefaultValue(true)]
        public bool auto_translate_on_edit { get; set; }

        [Description("Translate immediately after switching to another document tab."),
            Category("Behavior"), DefaultValue(true)]
        public bool translate_on_tab_change { get; set; }

        [Description("Synchronize vertical scrolling between the editor and the translation panel."),
            Category("Behavior"), DefaultValue(true)]
        public bool synchronize_scrolling { get; set; }
        #endregion

        #region STYLING
        [Description("Use the same colors as the editor window for this plugin's forms?"),
            Category("Styling"), DefaultValue(true)]
        public bool use_npp_styling { get; set; }
        #endregion
    }
}
