# Changelog

All notable changes are documented here. The format follows Keep a Changelog, and releases use semantic versioning.

## [Unreleased]

### Changed

- Lower peak animation-atlas memory during playback transitions and asset smoke tests.
- Strengthen offline asset verification with explicit checks for declared files, safe paths and source hashes.
- Generate source path and SHA-256 metadata directly from the selected input video.
- Expand core tests for malformed manifests, malformed hit masks and constrained display work areas.

### Fixed

- Keep package, assembly, file and informational versions synchronized during release packaging.
- Retry activation briefly when a second instance starts while the first instance is still initializing.
- Prevent integer overflow when validating animation atlases and hit-mask payloads.
- Keep throw and roaming physics inside the work area even when the pet is larger than the available display area.
- Avoid shared temporary settings files and clean up incomplete writes safely.
- Treat a stale Windows startup entry as disabled when it points to a different executable path.

## [1.0.0] - 2026-07-20

### Added

- Complete 462-frame transparent “奶蛙捧腹大笑” animation at 30 FPS.
- Original laugh audio, enabled by default and user-switchable.
- Transparent, topmost WPF desktop-pet window with per-frame pixel hit testing.
- Dragging, throwing, gravity, bouncing, mouse-wheel scaling and optional roaming.
- System tray controls, settings window, click-through mode and Windows startup option.
- Directly runnable single-file Windows 11 x64 executable and portable ZIP package.
- Reproducible asset pipeline, integrity verifier, core tests and GitHub Actions workflows.

### Changed

- Use an 80% first-run scale, adjustable from 50% to 180% by mouse wheel or settings slider.
- Normalize existing local settings to the final first-release defaults while preserving compatible values.

### Fixed

- Decode WPF animation atlases only on the UI dispatcher, preventing cross-thread image-ownership crashes at startup.
- Write full exception details to `%LOCALAPPDATA%\NaiWaPet\Logs` when an unexpected error occurs.
