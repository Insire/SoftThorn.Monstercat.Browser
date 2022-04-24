#

<div align="center">
  <br />

  <a href="https://github.com/Insire/SoftThorn.Monstercat.Browser">
    <img alt="SoftThorn.Monstercat.Browser" src="./assets/icons/Material-Play.svg" >
  </a>
  <h1>SoftThorn.Monstercat.Browser</h1>
  <p>
    Your tool for downloading the monstercat music libary on <code>Windows</code>, <code>MacOS</code> and <code>Linux</code>
  </p>
</div>

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Insire/SoftThorn.Monstercat.Browser/blob/master/license.md)
[![Build status](https://dev.azure.com/SoftThorn/SoftThorn.Monstercat.Browser/_apis/build/status/SoftThorn.Monstercat.Browser-CD)](https://dev.azure.com/SoftThorn/SoftThorn.Monstercat.Browser/_build/latest?definitionId=1)
[![CodeFactor](https://www.codefactor.io/repository/github/insire/SoftThorn.Monstercat.Browser/badge)](https://www.codefactor.io/repository/github/insire/SoftThorn.Monstercat.Browser)
[![codecov](https://codecov.io/gh/Insire/SoftThorn.Monstercat.Browser/branch/master/graph/badge.svg)](https://codecov.io/gh/Insire/SoftThorn.Monstercat.Browser)

The SoftThorn.Monstercat.Browser app (or SMB app for short) is my approach for downloading music from monstercat in a convenient way. It is also my playground for trying our different XAML frontends in my spare time.

## Features

- Browse the latest tracks and releases from monstercat.com
- Download any/all of their music to your local device (You need an active Gold Subscription for that!)

### Planned

- Settings View (configure how SMB works)
- Settings Storage (persist configuration between app restarts)
  - retain download folder selection
  - clear cache
  - credential management
  - color selection?
- Query Latest Artists (finish implementation)
- Query Most Popular Genres (finish implementation)
- AutoUpdate via Github releases
- Enable Asset Download (Download not only the music, but also the art for tracks and releases)
- Enable AssetSize Configuration (Download the art in the format and size you like)
- Playback (playback the music right in the app)
  - Qeueu tracks and releases
- Playlist (manage playback via playlsits)
  - Playbackmode (shuffel, one, all, folder)
  - Playback View (the UI)
  - Add PlaybackButtons to each section in the Shell (UX: quickly add music to the playback queue)
  - playlist storage

## Build Requirements

This app uses SDK-style project files, which means you are required to use [Visual Studio 2022](https://visualstudio.microsoft.com/vs/community/) or newer. Visual Studio will prompt you to install any missing components once you open the [sln](./SoftThorn.Monstercat.Browser.sln) file.

For anyone not wishing to install that, they atleast need:

- Windows 10 (older versions work probably too, but the repository is not configured for those)
- [Net 6.0](https://dotnet.microsoft.com/download/dotnet-core/6.0)
- [Visual Studio Code](https://code.visualstudio.com/) with the [C# Extension](https://github.com/OmniSharp/omnisharp-vscode) provided by Microsoft
- [git](https://git-scm.com/)
