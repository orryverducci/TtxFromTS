<h1 align="center"><img alt="TtxFromTS" style="width: 100%; max-width: 500px;" src="https://raw.githubusercontent.com/orryverducci/TtxFromTS/main/logo.png"/></h1>

TtxFromTS is a command line utility which decodes teletext services from a recorded DVB MPEG transport stream file. It can output decoded services as TTI or T42 files, or as a stream of Teletext packets to standard out or a web socket. TTI files are compatible with [vbit2](https://github.com/peterkvt80/vbit2).

TtxFromTS is a cross platform application, available for Windows, macOS, and Linux.

Where to download it
--------------------

The latest version of TtxFromTS can be downloaded from the [project release page on GitHub](https://github.com/orryverducci/TtxFromTS/releases).

How to use it
-------------

TtxFromTS can be run from the command line (e.g. Command Prompt, Terminal, PowerShell, etc). You can use arguments to set the file to be decoded and the application options.

By default TtxFromTS will output TTI files, which will be placed in a subdirectory of the current working directory with the same name as input file.

These arguments are available to use:

* `-i` **(required)** The input transport stream file.
* `--pid` **(required if service ID not specified)** The packet identifier of the teletext elementary stream to be decoded.
* `--sid` **(required if packet ID not specified)** The identifier of the television service containing the teletext service to be decoded.
* `--service-list` Outputs a list of services and their service identifiers. The PID and SID arguments aren't required if this is used.
* `--cycle` The cycle time to be used between subpages in seconds. Must be greater than 1 second, defaults to 10 seconds.
* `--include-subs` Enables decoding of teletext subtitle packets.
* `--no-config` Disables creation of a vbit2 configuration file when outputting TTI files.
* `--type` The type of output required. Valid options are `TTI`, `T42`, `StdOut` and `WebSocket`. Defaults to TTI.
* `--port` The port number to be used by the WebSocket server. Defaults to port 80.
* `--output` The path of the directory to output the TTI files to, or the path of the T42 file to be created.

### Example Command Line

```bash
TxtFromTS --pid 1234 -i "C:\Path\To\File.ts"
```

I've found a problem
--------------------

If you've found a bug, please file a new issue on the project [issues page](https://github.com/orryverducci/TtxFromTS/issues).

How to contribute
-----------------

If you are a developer and would like to contribute to this library, you are welcome to do so.

All contributions for bug fixes and new features should be submitted as a pull request. Information on how to do this can be found on [GitHub's help pages](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/about-pull-requests).

There are no formal coding convensions for the source code, but you should try to match the coding style of the existing source code files.

### Prerequisites

Before the project can be built, you must first install the [.NET 5.0 SDK](https://dotnet.microsoft.com/download) on your system.

The project has been written in the C# programming language. The source code contains projects and configuration for development in [Visual Studio](https://visualstudio.microsoft.com/vs/), [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/), or [Visual Studio Code](https://code.visualstudio.com/).

### Cloning the Repository

You can clone the repository using the following command:

```bash
git clone https://github.com/orryverducci/TtxFromTS.git
```

### Building a development build

You can build a development build of the application using the following command:

```bash
dotnet build
```

You can also run a development build using the following command, replacing the `pid` and `i` arguments with appropriate ones for your use case:

```bash
dotnet run -- --pid 1234 -i "C:\Path\To\File.ts"
```

### Building a release build

You can build an optimised release build of the application, published for a specific operating system, using the following command:

```bash
dotnet publish --runtime win-x64 --configuration Release
```

`win-x64` will need to be replaced with an appropriate runtime indentifier for the desired operating system and system architecture. Valid values are `win-x86`, `win-x64`, `win-arm64`, `osx-x64`, `linux-x64` and `linux-arm`.

What's the license
------------------

This software is distributed under the terms of the Modified BSD License. Full terms can be read in LICENSE.md included in the source code.

This software uses the [Cinegy Transport Stream Decoder Library](https://github.com/Cinegy/TsDecoder), which is distributed under the terms of the Apache License 2.0.
