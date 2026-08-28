# Changelog

All notable changes to NppTranslatePanel are documented in this file.

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
