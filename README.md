<div align="center">
  <br />
  <a href="https://github.com/Insire/SoftThorn.Monstercat.Browser">
    <img alt="SoftThorn.Monstercat.Browser" src="./assets/icons/Material-Play.svg" >
  </a>
  <h1>SoftThorn.Monstercat.Browser</h1>
  <p>
    Your tool for downloading the monstercat music libary on <code>Windows</code>.
  </p>
</div>

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Insire/SoftThorn.Monstercat.Browser/blob/master/license.md)
[![Build status](https://dev.azure.com/SoftThorn/Monstercat.Browser/_apis/build/status/Monstercat.Browser-CI)](https://dev.azure.com/SoftThorn/Monstercat.Browser/_build/latest?definitionId=6)
[![CodeFactor](https://www.codefactor.io/repository/github/insire/SoftThorn.Monstercat.Browser/badge)](https://www.codefactor.io/repository/github/insire/SoftThorn.Monstercat.Browser)

The SoftThorn.Monstercat.Browser app (or SMB app for short) is my approach for downloading music from [monstercat](https://www.monstercat.com/) in a convenient way. It is also my playground for trying our different XAML frontends in my spare time.

## Features

- Browse the latest tracks and releases from [monstercat](https://www.monstercat.com/)
- Download any/all of their music to your local device (You need an active Gold Subscription for that!)
- Settings
  - Download locations
  - Download file format
  - Dashboard configuration
  - filter/ignore tags
  - edit monstercat credentials

### Planned

- Settings Storage (persist configuration between app restarts)
  - color selection?
  - Enable AssetSize Configuration (Download the art in the format and size you like)
- AutoUpdate via Github releases
- Playlist (manage playback via playlists)
  - Playbackmode (shuffel, one, all, folder)

### ToDo

- add Previous button for going back to the previous track
- remember playback volume level
- figure out how to show playback progress with NAudio
- auto updates
- sign my binaries

## Showcase

![Animation showcasing app usage](/assets/screenshots/workflow.gif)]

## Build Requirements

This app uses SDK-style project files, which means you are required to use [Visual Studio 2022](https://visualstudio.microsoft.com/vs/community/) or newer. Visual Studio will prompt you to install any missing components once you open the [sln](./SoftThorn.Monstercat.Browser.sln) file.

For anyone not wishing to install that, they atleast need:

- Windows 10 (older versions work probably too, but the repository is not configured for those)
- [Net 6.0](https://dotnet.microsoft.com/download/dotnet-core/6.0)
- [Visual Studio Code](https://code.visualstudio.com/) with the [C# Extension](https://github.com/OmniSharp/omnisharp-vscode) provided by Microsoft
- [git](https://git-scm.com/)
