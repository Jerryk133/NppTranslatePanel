# NppTranslatePanel

NppTranslatePanel is a Notepad++ plugin that continuously translates the active document and displays the result in a dockable panel. It is written in C# and targets .NET Framework 4.8.

The plugin supports [DeepL API](https://developers.deepl.com/docs) and the free [MyMemory Translation API](https://mymemory.translated.net/doc/spec.php). DeepL requires the user's own API key; MyMemory does not require one.

## Features

- Live translation of the active Notepad++ document
- Dockable, read-only translation panel
- Automatic refresh after typing stops
- Manual translation with **Plugins > NppTranslatePanel > Translate Now**
- Configurable source and target languages
- Paragraph-level cache to avoid translating unchanged text again
- Cancellation of obsolete requests when the document changes
- Automatic splitting of long paragraphs to respect API request limits
- Translation status with provider, character count, API requests, cache hits, and elapsed time
- Privacy-safe local diagnostics that never log document text or API keys
- Optional use of the current Notepad++ editor colors
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

Open **Plugins > NppTranslatePanel > Show Translate Panel**. The plugin translates the active document immediately and refreshes the output whenever you stop typing for the configured delay.

The plugin menu contains:

- **Show Translate Panel** — shows or hides the dockable panel.
- **Translate Now** — opens the panel if necessary and requests an immediate translation.
- **Settings** — opens the plugin settings dialog.

Switching to another Notepad++ tab triggers translation of the newly active document. Translation stops while the panel is hidden so that API quota is not consumed unnecessarily.

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
| `auto_translate_on_edit` | `true` | Starts translation automatically after typing stops. |
| `translate_on_tab_change` | `true` | Translates immediately after switching document tabs. |
| `synchronize_scrolling` | `true` | Synchronizes vertical scrolling proportionally between the editor and translation panel. |
| `use_npp_styling` | `true` | Uses Notepad++ editor colors for the plugin UI. |

Language values are MyMemory language codes such as `en`, `cs`, `de`, or `fr`. Availability depends on the language pairs supported by MyMemory.

Settings are stored in the Notepad++ plugin configuration area under `NppTranslatePanel`.

## MyMemory limits and privacy

MyMemory documents a free anonymous quota of 5,000 characters per day. Supplying an email address through `mymemory_contact_email` raises the documented free quota to 50,000 characters per day.

Document text is sent over HTTPS to the MyMemory service for translation. Do not use the plugin for sensitive content unless sending that content to this third-party service is acceptable to you.

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
- `Forms/TranslatePanel.cs` — dockable translation output
- `Utils/Settings.cs` — user-configurable settings
- `Utils/DiagnosticsLogger.cs` — privacy-safe translation performance and error log
- `PluginInfrastructure/` — Notepad++ and Scintilla interop inherited from NppCSharpPluginPack

## Current limitations

- MyMemory is the only translation backend currently included.
- Translation requires an internet connection.
- Formatting and the exact number of blank lines are not preserved in the translated preview.
- The panel displays translated plain text and does not write it back into the document.
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
