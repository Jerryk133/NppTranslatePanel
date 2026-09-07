# Changelog

All notable changes to NppTranslatePanel are documented in this file.

## Unreleased

- Added **Open Translation in New Tab**, preserving the source document language and syntax mode
- Added **Save Translation As...** with a target-language filename, UTF-8 output, source extension preservation, and explicit source-overwrite protection
- Added **Open in New Tab** and **Save As...** buttons to the Translate panel
- Prevented opening a translated tab from triggering an automatic translation request
- Enabled width-tracking horizontal scrolling for long unwrapped lines in the Translate panel
- Prevented wrapped translation lines from being clipped beyond the panel edge
- Kept the Translate panel's wrapping mode synchronized when Word Wrap is toggled
- Synchronized zoom in, zoom out, and restored zoom with the Translate panel
- Added privacy and code-signing policies plus a repeatable GitHub Actions build workflow

## 0.3.1 — 2026-09-07

- Fixed Windows CRLF line endings being misinterpreted as blank lines, which inserted an empty line between every translated source line
- Added a smart **Translate** command that translates the selection when present, otherwise the complete document
- Added a **Translate** toolbar button with light, dark, and classic icon variants
- Exposed the smart command in Notepad++ Shortcut Mapper without assigning a potentially conflicting default shortcut

## 0.3.0 — 2026-09-01

- Disabled automatic translation after editing and tab changes by default for new installations
- Opening the Translate panel no longer sends document text unless automatic translation was explicitly enabled
- Renamed **Translate Now** to the unambiguous **Translate Document** command
- Kept **Translate Document** functional when it opens the panel
- Added a permanent third-party data-sharing warning to the Translator settings tab
- Added one-time privacy confirmation before the first request to each translation provider
- Declining privacy confirmation cancels the request and disables automatic translation
- Added **Translate Selection** for explicitly translating only the active editor selection
- Exposed selection translation through Notepad++ Shortcut Mapper without assigning a conflicting default shortcut
- Disabled synchronized scrolling while a selection-only result is displayed
- Added **Clear Translation Cache** to Application Behavior settings with immediate confirmation

## 0.2.0 — 2026-08-28

- Replaced the plain translation text box with a read-only Scintilla editor
- Added optional source syntax highlighting, enabled by default
- Added safe container-based highlighting from the active language's keywords, comments, strings, numbers, and operators
- Added Markdown UDL highlighting for headings, links, code, comments, emphasis, lists, and horizontal rules
- Mirrored the active document's colors, font styles, zoom, tabs, indentation, and wrapping
- Added a plain-text fallback for unknown languages and malformed Notepad++ configuration
- Applied the complete editor font immediately whenever the Translate panel opens

## 0.1.0 — 2026-08-14

Initial public release.

- Live translation of the active Notepad++ document in a dockable panel
- DeepL API Free and Pro support with encrypted API-key storage
- MyMemory fallback provider
- Batched DeepL requests and paragraph translation cache
- Configurable source and target languages, including the Windows system language
- Automatic translation after editing or switching document tabs
- Bidirectional proportional scroll synchronization
- Editor font and color integration
- Translation metrics, privacy-safe diagnostics, and core tests
