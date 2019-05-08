TtxFromTS
=========

TtxFromTS is a command line utility which decodes teletext services from a recorded MPEG transport stream file, and outputs the decoded pages as TTI files, compatible with [vbit2](https://github.com/peterkvt80/vbit2).

TtxFromTS is a cross platform application, available for Windows, macOS, and Linux.

How to use it
-------------

TtxFromTS can be run from the command line (e.g. Command Prompt, Terminal, PowerShell, etc). You can use arguments to set the file to be decoded and the application options.

The output files will be placed in a directory with the same name as input file in the current working directory.

These arguments are available to use:

* `--pid` **(required)** The packet identifier of the teletext elementary stream to be decoded.
* `--cycle` The cycle time to be used between subpages in seconds. Must be greater than 1 second, defaults to 10 seconds.
* `--include-subs` Enables decoding of teletext subtitle packets.
* `--no-config` Disables creation of a vbit2 configuration file.
* `--type` The type of output required. Valid options are `TTI` and `StdOut`. Defaults to TTI.
* `--output` The directory to output the TTI files to. If no directory is given a directory is created in the current working directory with the same name as the input file.
* `-i` **(required)** The input transport stream file.

### Example Command Line

```
TxtFromTS --pid 1234 -i "C:\Path\To\File.ts"
```

Where to download it
--------------------

The latest version of TtxFromTS can be downloaded from the [releases page](https://github.com/orryverducci/TtxFromTS/releases).

Current limitations
-------------------

Currently there are a number of limitations this application is bound by. These may limit what is outputted by the application, or affect how it is used.

The current limitations are:

* The packet identifier (PID) of the specific teletext service must be known. If you do not know it you can use another application such as [DVB Inspector](http://www.digitalekabeltelevisie.nl/dvb_inspector/) or TransEdit to find it.
* Fastext links pointing to specific subpages currently point to only the page number, ignoring the subpage.

I've found a problem
--------------------

If you've found a bug, please file a new issue on the project [issues page](https://github.com/orryverducci/TtxFromTS/issues).

How to contribute
-----------------

If you are a developer and would like to contribute to this application, you are welcome to do so.

All contributions for bug fixes and new features should be submitted as a pull request. Information on how to do this can be found on [GitHub's help pages](https://help.github.com/articles/about-pull-requests/).

This application has been written in the C# programming language, and developed on top of the [.NET Core](https://docs.microsoft.com/en-gb/dotnet/core/) platform. The source code contains projects and configuration for development in [Visual Studio](https://www.visualstudio.com/vs/), [Visual Studio for Mac](https://www.visualstudio.com/vs/), or [Visual Studio Code](https://code.visualstudio.com/).

There are no formal coding convensions for the source code, but you should try to match the coding style of the existing source code files.

What's the license
------------------

This software is distributed under the terms of the Modified BSD License. Full terms can be read in LICENSE.md included in the source code.

This software uses the [Cinegy Transport Stream Decoder Library](https://github.com/Cinegy/TsDecoder), which is distributed under the terms of the Apache License 2.0.