# Changelog

All notable changes are documented here. The format follows Keep a Changelog, and releases use semantic versioning.

## [Unreleased]

## [1.0.0] - 2026-07-20

### Added

- Complete 462-frame transparent “奶蛙捧腹大笑” animation at 30 FPS.
- Original laugh audio, enabled by default and user-switchable.
- Transparent, topmost WPF desktop-pet window with per-frame pixel hit testing.
- Dragging, throwing, gravity, bouncing, mouse-wheel scaling and optional roaming.
- System tray controls, settings window, click-through mode and Windows startup option.
- Single-file Windows 11 x64 portable build and per-user installer.
- Reproducible asset pipeline, integrity verifier, core tests and GitHub Actions workflows.

### Changed

- Use an 80% first-run scale, adjustable from 50% to 180% by mouse wheel or settings slider.
- Normalize existing local settings to the final first-release defaults while preserving compatible values.

### Fixed

- Decode WPF animation atlases only on the UI dispatcher, preventing cross-thread image-ownership crashes at startup.
- Write full exception details to `%LOCALAPPDATA%\NaiWaPet\Logs` when an unexpected error occurs.
