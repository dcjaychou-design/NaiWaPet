# Third-party notices

NaiWaPet is built with the following third-party software. These components are not relicensed by the project's GPL-3.0 license.

## Distributed runtime components

- [.NET runtime](https://github.com/dotnet/runtime) — MIT License and component-specific third-party notices.
- [WPF](https://github.com/dotnet/wpf) — MIT License and component-specific third-party notices.

The self-contained executable bundles the runtime portions needed to run on Windows 11 x64.
The complete official .NET 10.0.10 and WPF 10.0.10 license and third-party-notice texts are embedded in the executable and included in the portable ZIP. They can be viewed in the application through “关于与开源许可”. Exact source copies are stored under `licenses/` in this repository.

## Build-only tools

- Python — Python Software Foundation License.
- imageio — BSD-2-Clause License.
- imageio-ffmpeg — BSD-2-Clause License; its bundled FFmpeg binary carries its own build-time license information.
- NumPy — BSD-3-Clause License.
- Pillow — HPND License.

Build-only tools are not distributed inside NaiWaPet. Their linked projects and installed distributions contain the authoritative license texts and notices.
