<h1 align="center">Parasite Zeta Replay Analyzer</h1>

<p align="center">
  A C# tool for analyzing StarCraft II replays. <!-- TODO: one line on what it actually detects/outputs -->
</p>

<p align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#">
  <img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/StarCraft%20II-0F1B2D?style=for-the-badge&logo=battle.net&logoColor=white" alt="StarCraft II">
  <img src="https://img.shields.io/badge/Visual%20Studio-5C2D91?style=for-the-badge&logo=visualstudio&logoColor=white" alt="Visual Studio">
  <img src="https://img.shields.io/badge/status-inactive-lightgrey?style=for-the-badge" alt="Status: inactive">
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#tech-stack">Tech Stack</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#project-structure">Structure</a>
</p>

> [!NOTE]
> This project is **no longer actively maintained**. <!-- delete if it still is -->

---

## Features

- **SC2 replay analysis**: reads StarCraft II replay files and extracts match data
- **Parasite analysis**: <!-- TODO: what is analyzed, e.g. a specific ability, unit, or build -->
- **Two-project solution**: the analysis logic and the replay-handling project are split
- <!-- TODO: output format (console, JSON, CSV, UI?) -->

## Tech Stack

| Layer | Tech |
|---|---|
| Language | C# |
| Runtime | .NET <!-- TODO: target framework, check the .csproj --> |
| IDE / build | Visual Studio 2022 solution (`.sln`) |
| Replay parsing | <!-- TODO: library used, e.g. s2protocol port or other --> |

## Installation

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (match the version in the `.csproj`).

```bash
git clone https://github.com/lumerions/ParasiteZetaReplayAnalyzer.git
cd ParasiteZetaReplayAnalyzer
dotnet restore ParasiteZetaAnalyzer.sln
dotnet build ParasiteZetaAnalyzer.sln -c Release
```

Or open `ParasiteZetaAnalyzer.sln` in Visual Studio 2022 and build from there.

## Usage

<!-- TODO: how to run it and what to pass in -->

```bash
dotnet run --project ParasiteZetaAnalyzer -- <path-to-replay.SC2Replay>
```

## Project Structure

```
ParasiteZetaReplayAnalyzer/
├── ParasiteZetaAnalyzer.sln
├── ParasiteZetaAnalyzer/            # main project
├── ParasiteZetaReplayAnalyzer/      # solution folder
│   └── SC2ReplayParasiteAnalyzer.csproj   # replay analyzer project
└── .gitignore
```
