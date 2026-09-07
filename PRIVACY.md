# Privacy policy

Effective date: September 7, 2026

NppTranslatePanel is a Notepad++ plugin that sends text to a translation provider only when a translation is requested. The project maintainer does not operate an intermediary translation server and does not receive users' documents, translations, API keys, email addresses, diagnostics, or usage telemetry.

## Network data transfers

The plugin supports two third-party translation providers. The selected provider receives the text submitted for translation, the source and target language codes, and normal connection metadata such as the user's IP address.

### DeepL

When DeepL is selected, the plugin sends translation requests over HTTPS to either `api-free.deepl.com` or `api.deepl.com`. The request contains:

- the selected text or active document text requested for translation;
- the source and target language codes; and
- the user's DeepL API key in the HTTPS authorization header.

DeepL processes this information under its own [privacy policy](https://www.deepl.com/en/privacy) and the terms applicable to the user's DeepL API plan.

### MyMemory

When MyMemory is selected, the plugin sends an HTTPS request to `api.mymemory.translated.net`. The request contains:

- the selected text or active document text requested for translation;
- the source and target language codes; and
- the optional contact email configured by the user, when present.

MyMemory's API uses URL query parameters for these values. HTTPS protects the request in transit, but MyMemory can process and retain the submitted values. MyMemory's terms state that submitted segments may be collected and stored on a long-term basis and that connection data such as IP address and access time may be logged. Review the [MyMemory terms and privacy information](https://mymemory.translated.net/terms-and-conditions) before using this provider.

## User choice and automatic translation

Opening the Translate panel does not send document text with the default settings. Before the first translation request to each provider, the plugin displays a confirmation naming that provider. Declining the confirmation cancels the request and disables automatic translation.

The user can explicitly enable translation after editing or after switching document tabs. When enabled, these options can repeatedly send changed document text to the selected provider after the configured delay. Hiding the Translate panel stops automatic translation.

Do not submit confidential, sensitive, regulated, or third-party content unless transmission to and processing by the selected provider is acceptable and authorized.

## Data stored locally

The plugin stores its settings in the Notepad++ plugin configuration directory. These settings can include the selected provider, language codes, behavior options, the optional MyMemory contact email, and the provider for which privacy confirmation was accepted.

The DeepL API key is encrypted with Windows Data Protection API for the current Windows user before it is written to the settings file. This protects the stored value for normal local use but does not replace appropriate Windows account and device security.

Completed paragraph translations are cached only in memory for the current Notepad++ session. The cache can be cleared from the plugin settings and is reset when relevant translation settings change.

The local `NppTranslatePanel.log` diagnostics file contains timestamps, provider name, character and paragraph counts, API request count, cache hits, duration, outcome, and sanitized error information. It does not intentionally contain document text, translated text, or API keys. The plugin does not transmit this log automatically.

Translations opened in a new tab or saved with **Save Translation As...** are stored only where the user chooses. Notepad++ and the operating system then control those files.

## Telemetry and advertising

NppTranslatePanel contains no analytics, advertising, tracking SDK, automatic update service, or maintainer-operated telemetry endpoint.

## Changes and questions

Material changes to this policy will be documented in the repository. Questions that do not contain private document text, API keys, or other secrets may be submitted through the project's [GitHub issues](https://github.com/Jerryk133/NppTranslatePanel/issues).
