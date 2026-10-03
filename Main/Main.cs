using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using s2protocol.NET;
using s2protocol.NET.Models;
using IWshRuntimeLibrary;
using System.Collections.Concurrent;
using Internal.Models;
using Internal.ExportImporter;

namespace Internal.Main;

public class InternalMain
{
    private static List<Models.Models.IndividualGameResultsDto> GameResults = new List<Models.Models.IndividualGameResultsDto>();
    private static string[] PlayerIdHandles = new string[14];
    private static int ReplaysAnalyzed = 0;
    private static ConcurrentDictionary<string, Models.Models.FinalResultsDto> FinalResults = new();
    private static readonly string DefaultDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private static readonly ReplayDecoder decoder = new();
    private static readonly WshShell shell = new();
    private static HashSet<string> UserPlayerHandles = new();

    private static Dictionary<string, string> PlayerData = new();

    public static (int ExistingMechKills, int ExistingAlienKills, int ExistingHumanKills, int ExistingTies, int ExistingVictories, int ExistingGamesPlayed, int ExistingDeaths) GetUserInformation (string PlayerHandle)
    {
        var ExistingMechKills = 0;
        var ExistingAlienKills = 0;
        var ExistingHumanKills = 0;
        var ExistingTies = 0;
        var ExistingVictories = 0;
        var ExistingGamesPlayed = 0;
        var ExistingDeaths = 0;

        if (FinalResults.TryGetValue(PlayerHandle, out var PlayerEntry))
        {
            ExistingMechKills = PlayerEntry.MechKills;
            ExistingAlienKills = PlayerEntry.AlienKills;
            ExistingHumanKills = PlayerEntry.HumanKills;
            ExistingTies = PlayerEntry.Ties;
            ExistingVictories = PlayerEntry.Victories;
            ExistingGamesPlayed = PlayerEntry.GamesPlayed;
            ExistingDeaths = PlayerEntry.Deaths;
        }

        return (ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths);
    }

    public static bool IsAlienUnit (string PlayerUnitTypeWhoDied)
    {            
        if (Models.Models.AlienUnits.Contains(PlayerUnitTypeWhoDied)) {
            return true;
        } else
        {
            return false;
        }
    }

    public static bool IsMechUnit (string PlayerUnitTypeWhoDied)
    {            
        if (Models.Models.MechUnits.Contains(PlayerUnitTypeWhoDied)) {
            return true;
        } else
        {
            return false;
        }
    }

    public static string GetPlayerHandles (DetailsPlayer detailsPlayer)
    {
        var handles = $"{detailsPlayer.Toon.Region}-{detailsPlayer.Toon.ProgramId}-{detailsPlayer.Toon.Realm}-{detailsPlayer.Toon.Id}".Replace("\0", "");

        if (detailsPlayer.Toon.Id == 0) return "Unknown";

        return handles;
    }

    public static async Task<bool> LoadReplay (string LoadReplayPath)
    {
        var options = new ReplayDecoderOptions
        {
            Initdata = true, 
            Details = true, 
            Metadata = true, 
            TrackerEvents = true,
            GameEvents = false, 
            MessageEvents = true, 
            AttributeEvents = false 
        };
        await using FileStream stream = System.IO.File.OpenRead(LoadReplayPath);
        Sc2Replay? replay = await decoder.DecodeAsync(stream, options);

        if (replay == null)
        {
            return false;
        }

        var MapName = replay.Metadata.Title;

        if (MapName != "PARASITE - XIII" && MapName != "PARASITE - DELTA")
        {
            return false;
        }

        Array.Clear(PlayerIdHandles, 0, PlayerIdHandles.Length);
        PlayerData.Clear();

        foreach (var player in replay.Details.Players)
        {
            if (StationSecurityOrAlien(player.Name))
            {
                continue;
            }

            var PlayerHandle = GetPlayerHandles(player);
            if (PlayerHandle == "Unknown") continue;
            PlayerIdHandles[player.WorkingSetSlotId] = PlayerHandle;

            PlayerData.Add(PlayerHandle, player.Name);

            var (ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths) = GetUserInformation(PlayerHandle);

            FinalResults.AddOrUpdate(PlayerHandle,
            new Models.Models.FinalResultsDto
            {
                AlienKills = ExistingAlienKills,
                HumanKills = ExistingHumanKills,
                MechKills = ExistingMechKills,
                Ties = ExistingTies,
                Victories = ExistingVictories,
                Deaths = ExistingDeaths,
                GamesPlayed = 1
            }, 
            (key, currentData) => {

                return new Models.Models.FinalResultsDto 
                {
                    AlienKills = ExistingAlienKills,
                    HumanKills = ExistingHumanKills,
                    MechKills = ExistingMechKills,
                    Ties = ExistingTies,
                    Victories = ExistingVictories,
                    Deaths = ExistingDeaths,
                    GamesPlayed = ExistingGamesPlayed + 1
                };
            });
        }

        var AliveAlienPlayers = new HashSet<string>();
        var AliveHumanPlayers = new HashSet<string>();

        foreach (var unit in replay.TrackerEvents.SUnitBornEvents)
        {
            var UnitType = unit.UnitTypeName;
            var PlayerIdWhoDied = unit.ControlPlayerId;
            var Died = unit.SUnitDiedEvent;

            if (PlayerIdWhoDied <= 0 || PlayerIdWhoDied > 12)
            {
                continue;
            }

            if (IsAlienUnit(UnitType))
            {
                AliveAlienPlayers.Add(GetHandlesByPlayerId(PlayerIdWhoDied) + "?" + UnitType);
            } else
            {
                if (!IsMechUnit(UnitType))
                {
                    AliveHumanPlayers.Add(GetHandlesByPlayerId(PlayerIdWhoDied));
                }
            }

            if (Died == null)
            {
                continue;
            }

            var PlayerIdWhoKilled = Died.KillerPlayerId;

            if (PlayerIdWhoKilled == null)
            {
                continue;
            }

            var KillerUnitBornEvent = Died?.KillerUnitBornEvent;

            if (KillerUnitBornEvent == null)
            {
                continue;
            }

            var KillerId = KillerUnitBornEvent.ControlPlayerId;
            var PlayerUnitTypeWhoDied = KillerUnitBornEvent.UnitTypeName;
            var AlienKill = IsAlienUnit(PlayerUnitTypeWhoDied);
            var MechKill = IsMechUnit(PlayerUnitTypeWhoDied);
            var UserUsingApplicationDied = false;

            if (IsAlienUnit(UnitType))
            {
                AliveAlienPlayers.Remove(GetHandlesByPlayerId(PlayerIdWhoDied) + "?" + UnitType);

                if (UserPlayerHandles.Contains(GetHandlesByPlayerId(PlayerIdWhoDied)))
                {
                    UserUsingApplicationDied = true;
                }
            } else
            {
                if (!IsMechUnit(UnitType)) {
                    AliveHumanPlayers.Remove(GetHandlesByPlayerId(PlayerIdWhoDied));

                    if (UserPlayerHandles.Contains(GetHandlesByPlayerId(PlayerIdWhoDied)))
                    {
                        UserUsingApplicationDied = true;
                    }
                }
            }

            if (KillerId <= 0 || KillerId > 12)
            {
                continue;
            }

            var PlayerHandle = GetHandlesByPlayerId(KillerId);

            if (string.IsNullOrEmpty(PlayerHandle)) continue;

            var (ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths) = GetUserInformation(PlayerHandle);

            FinalResults.AddOrUpdate(PlayerHandle, 
            new Models.Models.FinalResultsDto
            {
                AlienKills = ExistingAlienKills,
                HumanKills = ExistingHumanKills,
                MechKills = ExistingMechKills,
                Ties = ExistingTies,
                Victories = ExistingVictories,
                Deaths = ExistingDeaths,
                GamesPlayed = 1
            }, 
            (key, currentData) => {
                if (AlienKill)
                {
                    ExistingAlienKills += 1;
                } else
                {
                    if (!MechKill)
                    {
                        ExistingHumanKills += 1;
                    } else
                    {
                        ExistingMechKills += 1;
                    }
                }

                if (UserUsingApplicationDied)
                {
                    ExistingDeaths += 1;
                }

                return new Models.Models.FinalResultsDto 
                {
                    AlienKills = ExistingAlienKills,
                    HumanKills = ExistingHumanKills,
                    MechKills = ExistingMechKills,
                    Ties = ExistingTies,
                    Victories = ExistingVictories,
                    Deaths = ExistingDeaths,
                    GamesPlayed = ExistingGamesPlayed
                };
            });
        }

        string DetermineVictoryCondition ()
        {
            if (AliveAlienPlayers.Count > AliveHumanPlayers.Count)
            {
                return "Alien";
            }
            if (AliveAlienPlayers.Count == AliveHumanPlayers.Count)
            {
                return "Tie";
            }

            return "Human";
        }

        void UpdateWinLoseCount (string PlayerHandle, bool Won, bool? Tie)
        {
            var QuestionMarkFound = false;
            StringBuilder sb = new();

            if (PlayerHandle.Contains("?"))
            {
                foreach (var character in PlayerHandle)
                {
                    if (QuestionMarkFound) continue;
                    if (character.ToString() == "?") QuestionMarkFound = true;
                    if (!QuestionMarkFound) sb.Append(character);
                }
            }

            PlayerHandle = sb.ToString();

            var (ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths) = GetUserInformation(PlayerHandle);

            FinalResults.AddOrUpdate(PlayerHandle, 
                new Models.Models.FinalResultsDto
                {
                    AlienKills = ExistingAlienKills,
                    HumanKills = ExistingHumanKills,
                    MechKills = ExistingMechKills,
                    Ties = ExistingTies,
                    Victories = ExistingVictories,
                    Deaths = ExistingDeaths,
                    GamesPlayed = 1
                }, 
                (key, currentData) => {

                    if (Won && Tie == null)
                    {
                        ExistingVictories += 1;
                    }

                    if (Tie != null)
                    {
                        ExistingTies += 1;
                    }

                    return new Models.Models.FinalResultsDto 
                    {
                        AlienKills = ExistingAlienKills,
                        HumanKills = ExistingHumanKills,
                        MechKills = ExistingMechKills,
                        Ties = ExistingTies,
                        Victories = ExistingVictories,
                        Deaths = ExistingDeaths,
                        GamesPlayed = ExistingGamesPlayed
                    };
            });
        }

        string WhoWon = DetermineVictoryCondition();
        var AlienWin = WhoWon == "Alien";
        var MostUsedAlienForm = new List<string>();

        foreach (var item in AliveAlienPlayers)
        {
            if (item == null) continue;

            StringBuilder sb = new();

            var QuestionMarkFound = false;

            if (item.Contains("?"))
            {
                foreach (var character in item)
                {
                    if (character.ToString() == "?") QuestionMarkFound = true;
                    if (QuestionMarkFound) sb.Append(character);
                }

                MostUsedAlienForm.Add(sb.ToString());
            }
            UpdateWinLoseCount(item, AlienWin, WhoWon == "Tie" ? true : null);
        }

        var LeastUsedAlienForm = MostUsedAlienForm.GroupBy(x => x).MinBy(x => x.Count())?.Key;  // we do this because certain host unit types are different from spawn unit types

        foreach (var item in AliveHumanPlayers)
        {
            if (item == null) continue;
            UpdateWinLoseCount(item, !AlienWin, WhoWon == "Tie" ? true : null);
        }

        var ReplayFileName = replay.FileName;
        var ReplayChatMessageCount = replay.ChatMessages.Count;
        var ReplayLength = replay.Header.ElapsedGameLoops / 22.4;
        TimeSpan TimeSpanSeconds = TimeSpan.FromSeconds(ReplayLength);
        double TimeSpanMinutes = TimeSpanSeconds.Minutes;

        GameResults.Add(new Models.Models.IndividualGameResultsDto
        {
            ReplayName = ReplayFileName,
            ChatMessageCount = ReplayChatMessageCount,
            ReplayLength = ReplayLength,
            WinningAlienUnitType = LeastUsedAlienForm,
            Players = PlayerData
        });

        ReplaysAnalyzed += 1;
        return true;
    }

    public static async Task StartLoadingReplays ()
    {
        var ReplayPaths = new List<string>();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        var Count = 0;
        var DefaultSC2DocumentPath = Path.Combine(DefaultDocumentsPath, "StarCraft II");
        foreach (var item in Directory.GetFiles(DefaultSC2DocumentPath))
        {
            if (Count == 2)
            {
                break;
            }

            var FileName = Path.GetFileNameWithoutExtension(item);
            var FileNameExtension = Path.GetFileName(item);

            if (FileName.EndsWith("1") || FileName.EndsWith("2"))
            {
                if (FileName.Contains("@") && FileNameExtension.Contains(".lnk")) {
                    IWshShortcut shortcut = (IWshShortcut) shell.CreateShortcut(Path.Combine(DefaultSC2DocumentPath, FileNameExtension));
                    string UserFolder = shortcut.TargetPath;
                    var UserFolderSplitResult = UserFolder.Split('\\');

                    foreach (var splitResult in UserFolderSplitResult)
                    {
                        if (splitResult.Contains("S") && !splitResult.Contains("StarCraft"))
                        {
                            UserPlayerHandles.Add(splitResult);
                        }
                    }

                    var UserReplayFolder = Path.Combine(UserFolder, "Replays");
                    var UserMultiplayerFolder = Path.Combine(UserReplayFolder, "Multiplayer");

                    foreach (var ReplayPath in Directory.GetFiles(UserMultiplayerFolder))
                    {
                        var ReplayFileName = Path.GetFileName(ReplayPath);

                        if (ReplayFileName.EndsWith(".SC2Replay"))
                        {
                            ReplayPaths.Add(ReplayPath);
                        }
                    }

                    Count += 1;
                }
            }
        }

        await Parallel.ForEachAsync(ReplayPaths, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        }, 
        async (ReplayPath, cancellationToken) => 
        {
            await LoadReplay(ReplayPath);
        });

        Console.WriteLine($"Replays scanned: {ReplaysAnalyzed}");
        var MostPlayedWith = FinalResults.OrderByDescending(i => i.Value.GamesPlayed);

        foreach (var item in MostPlayedWith)
        {
            Console.WriteLine($"[{item.Key},{item.Value.GamesPlayed}-{item.Value.Victories}-{item.Value.AlienKills}-{item.Value.HumanKills}-{item.Value.MechKills}]");
        }

        stopwatch.Stop();
        ExportImport.ExportAsJson(FinalResults, GameResults);
        Console.WriteLine(stopwatch.ElapsedMilliseconds);
    }

    public static string GetHandlesByPlayerId (int PlayerId)
    {
        return PlayerIdHandles[PlayerId];
    }
    
    public static bool StationSecurityOrAlien (string PlayerName)
    {
        if (PlayerName == "Station Security" || PlayerName == "Alien")
        {
            return true;
        } 

        return false;
    }

    public static void GetHighestKD ()
    {
        var HighestKd = FinalResults.OrderByDescending(i => i.Value.Deaths == 0 ? 1 : (decimal) (i.Value.AlienKills + i.Value.HumanKills) / i.Value.Deaths).Select(v => new
        {
            Kd = v.Value.Deaths == 0 ? 1 : (decimal) (v.Value.AlienKills + v.Value.HumanKills) / v.Value.Deaths
        });

        foreach (var item in HighestKd)
        {
            Console.WriteLine($"[{item.Kd}]");
        }
    }

    public static void GetHighestWinningAlienForm ()
    {
        var HighestWinningAlienForms = 
        GameResults.Where(x => x.WinningAlienUnitType != null)
        .CountBy(x => x.WinningAlienUnitType)
        .OrderByDescending(i => i.Value)
        .ToList();

        foreach (var item in HighestWinningAlienForms)
        {
            Console.WriteLine($"[{item.Key}-{item.Value}]");
        }
    }
}