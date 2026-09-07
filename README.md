# NppTranslatePanel

NppTranslatePanel is a Notepad++ plugin that continuously translates the active document and displays the result in a dockable panel. It is written in C# and targets .NET Framework 4.8.

The plugin supports [DeepL API](https://developers.deepl.com/docs) and the free [MyMemory Translation API](https://mymemory.translated.net/doc/spec.php). DeepL requires the user's own API key; MyMemory does not require one.

## Features

- Live translation of the active Notepad++ document
- Dockable, read-only translation panel
- Optional automatic refresh after typing stops
- Manual translation with **Plugins > NppTranslatePanel > Translate Document**
- Translation of only the selected text with **Translate Selection**
- One-click **Translate** command and toolbar button that use the selection when present, otherwise the complete document
- Configurable source and target languages
- Paragraph-level cache to avoid translating unchanged text again
- Manual clearing of the in-memory translation cache from **Settings > Application Behavior**
- Cancellation of obsolete requests when the document changes
- Automatic splitting of long paragraphs to respect API request limits
- Translation status with provider, character count, API requests, cache hits, and elapsed time
- Privacy-safe local diagnostics that never log document text or API keys
- Optional use of the current Notepad++ editor colors
- Optional matching of the active document's syntax highlighting and editor layout, including Markdown formatting
- Support for both 32-bit and 64-bit Notepad++

## Requirements

- Windows
- Notepad++
- .NET Framework 4.8
- Internet access to `api.mymemory.translated.net`

The plugin build must match the architecture of Notepad++: use `x64` for 64-bit Notepad++ and `x86` for 32-bit Notepad++.

## Installation

1. Create a directory named `NppTranslatePanel` inside the Notepad++ `plugins` directory.
2. Copy `NppTranslatePanel.dll` into that directory.
3. Restart Notepad++.
4. Open the panel using **Plugins > NppTranslatePanel > Show Translate Panel**.

A typical 64-bit installation looks like this:

```text
C:\Program Files\Notepad++\plugins\NppTranslatePanel\NppTranslatePanel.dll
```

## Usage

Open **Plugins > NppTranslatePanel > Show Translate Panel**. With the privacy-safe defaults, opening the panel does not send document text anywhere. Use **Translate Document** to request a translation, or explicitly enable automatic translation in **Settings**.

The plugin menu contains:

- **Show Translate Panel** — shows or hides the dockable panel.
- **Translate Document** — opens the panel if necessary and translates the complete active document.
- **Translate Selection** — translates only the active editor selection and opens the panel if necessary.
- **Translate** — translates the active selection when present; otherwise it translates the complete document. The toolbar button runs this same command.
- **Settings** — opens the plugin settings dialog.

Plugin commands are available in **Settings > Shortcut Mapper > Plugin commands**, where users can assign their preferred keyboard shortcuts without imposing global defaults. Assign a shortcut to **Translate** to use the same selection-or-document behavior from the keyboard.

Automatic translation after editing or switching tabs is disabled by default and can be enabled in **Settings**. Translation always stops while the panel is hidden so that API quota is not consumed unnecessarily.

Use **Settings > Application Behavior > Clear Translation Cache** to discard translations cached during the current Notepad++ session. Clearing is immediate and does not start a translation; the next request will translate the affected text again through the selected service.

## Settings

| Setting | Default | Description |
| --- | --- | --- |
| `source_language` | `en` | Language code of the source document. |
| `target_language` | `cs` | Language code of the translated output. |
| `debounce_ms` | `1000` | Delay in milliseconds after the last edit before translation starts. Values below 200 ms are treated as 200 ms. |
| `mymemory_contact_email` | empty | Optional email sent to MyMemory to increase the free quota. |
| `translator_provider` | `MyMemory` | Selected translation service: `MyMemory` or `DeepL`. |
| `deepl_api_key` | empty | DeepL API key, encrypted locally with Windows DPAPI for the current user. |
| `deepl_use_free_api` | `true` | Uses the DeepL API Free endpoint; disable for a DeepL API Pro key. |
| `auto_translate_on_edit` | `false` | Starts translation automatically after typing stops when explicitly enabled. |
| `translate_on_tab_change` | `false` | Translates immediately after switching document tabs when explicitly enabled. |
| `synchronize_scrolling` | `true` | Synchronizes vertical scrolling proportionally between the editor and translation panel. |
| `match_source_syntax_highlighting` | `true` | Uses the active language definition and mirrors its syntax colors, font styles, zoom, tabs, indentation, and wrapping. |
| `privacy_notice_accepted_provider` | empty | Records the provider for which the one-time privacy confirmation was accepted. |
| `use_npp_styling` | `true` | Uses Notepad++ editor colors for the plugin UI. |

Language values are MyMemory language codes such as `en`, `cs`, `de`, or `fr`. Availability depends on the language pairs supported by MyMemory.

Settings are stored in the Notepad++ plugin configuration area under `NppTranslatePanel`.

## MyMemory limits and privacy

MyMemory documents a free anonymous quota of 5,000 characters per day. Supplying an email address through `mymemory_contact_email` raises the documented free quota to 50,000 characters per day.

Text requested for translation is sent over HTTPS to the selected third-party service. Before the first request to a provider, the plugin asks for confirmation and remembers the accepted provider. Changing providers requires confirmation again. A permanent warning is also displayed in the Translator settings tab.

Do not use the plugin for confidential or sensitive content unless sending that content to the selected provider is acceptable and permitted. Declining the confirmation cancels the request, sends no document text, and disables automatic translation.

Selection-only results do not participate in synchronized scrolling because they represent only part of the source document.

The plugin splits the document into paragraphs and caches completed translations for the current session. Only changed or previously untranslated paragraphs normally require another API request. Paragraphs longer than 450 characters are divided into smaller requests.

## Building from source

Open [NppTranslatePanel.sln](NppTranslatePanel/NppTranslatePanel.sln) in Visual Studio 2022 and build one of these configurations:

- `Release | x64` for 64-bit Notepad++
- `Release | x86` for 32-bit Notepad++

The project restores `UnmanagedExports.Repack.Upgrade` through NuGet and produces a .NET Framework 4.8 plugin DLL.

By default, the post-build target copies the output directly to the matching Notepad++ installation:

```text
%ProgramFiles%\Notepad++\plugins\NppTranslatePanel
%ProgramFiles(x86)%\Notepad++\plugins\NppTranslatePanel
```

Writing there may require an elevated Visual Studio instance. To build without writing into the Notepad++ installation, override the destination with an MSBuild property, for example:

```powershell
msbuild .\NppTranslatePanel\NppTranslatePanel.csproj /p:Configuration=Release /p:Platform=x64 /p:NppPluginsDir64=C:\stage
```

The resulting DLL is located in:

```text
NppTranslatePanel\bin\Release-x64\NppTranslatePanel.dll
```

For a 32-bit release, the output directory is `NppTranslatePanel\bin\Release`.

## Architecture

The main components are:

- `Main.cs` — plugin initialization, menu commands, notifications, and panel lifecycle
- `Translation/ChangeWatcher.cs` — change debounce, request cancellation, segmentation, and orchestration
- `Translation/MyMemoryTranslator.cs` — MyMemory HTTP client and response parsing
- `Translation/TranslationCache.cs` — bounded in-memory paragraph cache
- `Translation/Segmenter.cs` — paragraph splitting
- `Forms/TranslatePanel.cs` — dockable Scintilla translation output
- `Utils/ContainerSyntaxHighlighter.cs` — safe syntax highlighting from Notepad++ language definitions
- `Utils/MarkdownSyntaxHighlighter.cs` — Markdown-aware highlighting that uses Notepad++ UDL styles
- `Utils/ScintillaStyleSynchronizer.cs` — active style and editor-layout mirroring
- `Utils/PrivacyConsent.cs` — provider-specific privacy confirmation state
- `Utils/Settings.cs` — user-configurable settings
- `Utils/DiagnosticsLogger.cs` — privacy-safe translation performance and error log
- `PluginInfrastructure/` — Notepad++ and Scintilla interop inherited from NppCSharpPluginPack

## Current limitations

- Translation requires an internet connection.
- Formatting and the exact number of blank lines are not preserved in the translated preview.
- Syntax highlighting is visual only; machine translation may still change programming-language tokens.
- The panel does not write translated text back into the document.
- The translation cache exists only for the current Notepad++ session and is cleared when translation settings change.

## Diagnostics and tests

Translation diagnostics are written to `NppTranslatePanel.log` in the plugin configuration directory. Each entry contains only the timestamp, provider, character and paragraph counts, API request count, cache hits, duration, outcome, and sanitized error text. Document contents and API keys are never logged.

Build and run the dependency-free core test suite with Visual Studio MSBuild:

```powershell
msbuild .\NppTranslatePanel.Tests\NppTranslatePanel.Tests.csproj /t:Restore,Build /p:Platform=x64 /p:RuntimeIdentifier=win-x64
.\NppTranslatePanel.Tests\bin\x64\Debug\net48\win-x64\NppTranslatePanel.Tests.exe
```

## License

Licensed under the [Apache License 2.0](LICENSE.md).

The Notepad++ C# plugin infrastructure is derived from [NppCSharpPluginPack](https://github.com/molsonkiko/NppCSharpPluginPack) and the archived [NotepadPlusPlusPluginPack.Net](https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net).
