using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using s2protocol.NET;
using s2protocol.NET.Models;
using IWshRuntimeLibrary;
using System.Collections.Concurrent;
using Internal.Models;
using Internal.ExportImporter;
using Internal.OtherMethods;

namespace Internal.Main;

public class InternalMain : Other
{
    private static List<Models.Models.IndividualGameResultsDto> GameResults = new List<Models.Models.IndividualGameResultsDto>();
    private static int ReplaysAnalyzed = 0;
    private static ConcurrentDictionary<string, Models.Models.FinalResultsDto> FinalResults = new();
    private static readonly string DefaultDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private static readonly ReplayDecoder decoder = new();
    private static readonly WshShell shell = new();

    public static void UpdateStatData (string PlayerHandle, bool IncrementGamesPlayed, int ExistingMechKills, int ExistingAlienKills, int ExistingHumanKills, int ExistingTies, int ExistingVictories, int ExistingGamesPlayed, int ExistingDeaths)
    {
        if (IncrementGamesPlayed)
        {
            ExistingGamesPlayed += 1;
        }
        
        FinalResults.AddOrUpdate(PlayerHandle,
        new Models.Models.FinalResultsDto
        {
            AlienKills = ExistingAlienKills,
            HumanKills = ExistingHumanKills,
            MechKills = ExistingMechKills,
            Ties = ExistingTies,
            Victories = ExistingVictories,
            Deaths = ExistingDeaths,
            GamesPlayed = ExistingGamesPlayed
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
                GamesPlayed = ExistingGamesPlayed
            };
        });
    }

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

    public static string GetPlayerHandles (DetailsPlayer detailsPlayer)
    {
        var handles = $"{detailsPlayer.Toon.Region}-{detailsPlayer.Toon.ProgramId}-{detailsPlayer.Toon.Realm}-{detailsPlayer.Toon.Id}".Replace("\0", "");

        if (detailsPlayer.Toon.Id == 0) return "Unknown";

        return handles;
    }

    public static async Task<bool> LoadReplay (string LoadReplayPath)
    {
        var ReplayFileName = Path.GetFileName(LoadReplayPath);
        var options = new ReplayDecoderOptions
        {
            Initdata = false, 
            Details = true, 
            Metadata = true, 
            TrackerEvents = true,
            GameEvents = false, 
            MessageEvents = true, 
            AttributeEvents = false 
        };
        
        Sc2Replay? replay = await decoder.DecodeAsync(LoadReplayPath, options);

        if (replay == null)
        {
            return false;
        }

        var MapName = replay.Metadata.Title;

        if (MapName != "PARASITE - XIII" && MapName != "PARASITE - DELTA")
        {
            return false;
        }

        List<Models.Models.PlayersReplayData> PlayerData = new();
        string[] PlayerIdHandles = new string[14];

        string GetHandlesByPlayerId (int PlayerId)
        {
            return PlayerIdHandles[PlayerId];
        }

        foreach (var player in replay.Details.Players)
        {
            if (StationSecurityOrAlien(player.Name))
            {
                continue;
            }

            var PlayerHandle = GetPlayerHandles(player);
            if (PlayerHandle == "Unknown") continue;
            PlayerIdHandles[player.WorkingSetSlotId] = PlayerHandle;
            
            AddToPlayerData(PlayerData, player.Name, PlayerHandle, ReplayFileName);            

            var (ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths) = GetUserInformation(PlayerHandle);

            UpdateStatData(PlayerHandle, true, ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths);
        }

        var AliveAlienPlayers = new HashSet<string>();
        var AliveHumanPlayers = new HashSet<string>();
        var DeathCounts = new int[14];

        foreach (var unit in replay.TrackerEvents.SUnitBornEvents)
        {
            var UnitType = unit.UnitTypeName;
            var PlayerIdWhoDied = unit.ControlPlayerId;
            var Died = unit.SUnitDiedEvent;

            if (PlayerIdWhoDied <= 0 || PlayerIdWhoDied > 12)
            {
                continue;
            }

            if (!AliveAlienPlayers.Contains(GetHandlesByPlayerId(PlayerIdWhoDied)) && !AliveHumanPlayers.Contains(GetHandlesByPlayerId(PlayerIdWhoDied))) 
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
                DeathCounts[PlayerIdWhoDied] = DeathCounts[PlayerIdWhoDied] += 1;
            }

            FinalResults.AddOrUpdate(PlayerHandle, 
            new Models.Models.FinalResultsDto
            {
                AlienKills = ExistingAlienKills,
                HumanKills = ExistingHumanKills,
                MechKills = ExistingMechKills,
                Ties = ExistingTies,
                Victories = ExistingVictories,
                Deaths = ExistingDeaths,
                GamesPlayed = ExistingGamesPlayed
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

        void UpdateWinLoseCount (string PlayerHandle, bool Won, bool? Tie, bool? DeathIncrement, int DeathIncrementCount)
        {
            
            if (DeathIncrement == null)
            {
                if (PlayerHandle.Contains("?"))
                {
                    var QuestionMarkFound = false;

                    StringBuilder sb = new();

                    foreach (var character in PlayerHandle)
                    {
                        if (QuestionMarkFound) continue;
                        if (character.ToString() == "?") QuestionMarkFound = true;
                        if (!QuestionMarkFound) sb.Append(character);
                    }
                    PlayerHandle = sb.ToString();
                }
            }

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
                    GamesPlayed = ExistingGamesPlayed
                }, 
                (key, currentData) => {

                    if (Won && Tie == null && DeathIncrement == null)
                    {
                        ExistingVictories += 1;
                    }

                    if (Tie != null && DeathIncrement == null)
                    {
                        ExistingTies += 1;
                    }

                    if (DeathIncrement == true && DeathIncrementCount > 0)
                    {
                        ExistingDeaths += DeathIncrementCount;
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

            if (item.Contains("?"))
            {
                StringBuilder sb = new();

                var QuestionMarkFound = false;

                foreach (var character in item)
                {
                    if (character.ToString() == "?") QuestionMarkFound = true;
                    if (QuestionMarkFound) sb.Append(character);
                }

                MostUsedAlienForm.Add(sb.ToString());
            }
            UpdateWinLoseCount(item, AlienWin, WhoWon == "Tie" ? true : null, null, 0);
        }

        var LeastUsedAlienForm = MostUsedAlienForm.GroupBy(x => x).MinBy(x => x.Count())?.Key;  // we do this because certain host unit types are different from spawn unit types

        if (!AlienWin)
        {
            LeastUsedAlienForm = "None";
        }

        foreach (var item in AliveHumanPlayers)
        {
            if (item == null) continue;
            UpdateWinLoseCount(item, !AlienWin, WhoWon == "Tie" ? true : null, null, 0);
        }

        for (int i = 0; i < DeathCounts.Length; i++)
        {
            var handle = GetHandlesByPlayerId(i);

            if (string.IsNullOrEmpty(handle))
                continue;

            var deathCount = DeathCounts[i];

            if (deathCount == 0)
                continue;

            UpdateWinLoseCount(handle, !AlienWin, WhoWon == "Tie" ? true : null, true, deathCount);
        }

        var ReplayChatMessageCount = replay.ChatMessages.Count;
        var ReplayLength = replay.Header.ElapsedGameLoops / 22.4;
        TimeSpan TimeSpanSeconds = TimeSpan.FromSeconds(ReplayLength);
        double TimeSpanMinutes = TimeSpanSeconds.Minutes;

        GameResults.Add(new Models.Models.IndividualGameResultsDto
        {
            ReplayName = ReplayFileName,
            ChatMessageCount = ReplayChatMessageCount,
            ReplayLength = TimeSpanMinutes,
            WinningAlienUnitType = LeastUsedAlienForm,
            Players = PlayerData
        });

        Interlocked.Increment(ref ReplaysAnalyzed);

        return true;
    }

    public static async Task StartLoadingReplays ()
    {
        var UseCache = false;
        var UserCacheLocation = Path.Combine(AppContext.BaseDirectory, "UserCache");
        var CacheInfo = Path.Combine(UserCacheLocation, "CacheInfo");
        var CachedJsonData = Path.Combine(UserCacheLocation, "CacheData.json");
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

                    foreach (var ReplayPath in Directory.EnumerateFiles(UserMultiplayerFolder, "*.SC2Replay"))
                    {
                        if (System.IO.File.Exists(CacheInfo) && System.IO.File.Exists(CachedJsonData))
                        {
                            DateTime creationUTC = System.IO.File.GetLastWriteTimeUtc(ReplayPath);
                            DateTimeOffset fileCreationDate = DateTimeOffset.Parse(creationUTC.ToString("O"));
                            string cacheInformation = System.IO.File.ReadAllText(CacheInfo);
                            DateTimeOffset cacheInformationDate = DateTimeOffset.Parse(cacheInformation);

                            UseCache = true;
                            if (fileCreationDate > cacheInformationDate)
                            {
                                ReplayPaths.Add(ReplayPath);
                            }
                        } else
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
        Console.WriteLine($"Cache Used:{UseCache}");
      //  var MostPlayedWith = FinalResults.OrderByDescending(i => i.Value.GamesPlayed);

      //  foreach (var item in MostPlayedWith)
      //  {
          //  if (UserPlayerHandles.Contains(item.Key))
          //  {
          //      Console.WriteLine($"[{item.Key},{item.Value.GamesPlayed}-{item.Value.Victories}-{item.Value.AlienKills}-{item.Value.HumanKills}-{item.Value.MechKills}]");
           // }
        //}

        if (UseCache) {
            var NewCachedFinalResults = new ConcurrentDictionary<string, Models.Models.FinalResultsDto>(
                FinalResults.Select(kvp => new KeyValuePair<string, Models.Models.FinalResultsDto>(kvp.Key, new Models.Models.FinalResultsDto
                {
                    AlienKills = kvp.Value.AlienKills,
                    HumanKills = kvp.Value.HumanKills,
                    MechKills = kvp.Value.MechKills,
                    Deaths = kvp.Value.Deaths,
                    Ties = kvp.Value.Ties,
                    Victories = kvp.Value.Victories,
                    GamesPlayed = kvp.Value.GamesPlayed
                })
            ));

            var NewCachedGameResults =
                GameResults.Select(item => new Models.Models.IndividualGameResultsDto {
                    ReplayName = item.ReplayName,
                    ChatMessageCount = item.ChatMessageCount,
                    ReplayLength = item.ReplayLength,
                    WinningAlienUnitType = item.WinningAlienUnitType,
                    Players = item.Players.Select(playerItem => new Models.Models.PlayersReplayData
                    {
                        PlayerUsername = playerItem.PlayerUsername,
                        PlayerHandle = playerItem.PlayerHandle,
                        ReplayName = playerItem.ReplayName
                    }).ToList()
                }).ToList();

            var CachedData = ExportImport.ImportAsJson(Path.Combine(UserCacheLocation, "CacheData.json"), true);
            var GameResultsCombinedConcat = NewCachedGameResults.Concat(CachedData.GameResults);
            var FinalResultsCombinedConcat = new ConcurrentDictionary<string, Models.Models.FinalResultsDto>(CachedData.FinalResultsList);

            foreach (var (key, value) in NewCachedFinalResults)
            {
                FinalResultsCombinedConcat.AddOrUpdate(
                    key,
                    _ => new Models.Models.FinalResultsDto
                    {
                        AlienKills = value.AlienKills,
                        HumanKills = value.HumanKills,
                        MechKills = value.MechKills,
                        Deaths = value.Deaths,
                        Ties = value.Ties,
                        Victories = value.Victories,
                        GamesPlayed = value.GamesPlayed
                    },
                    (_, cached) => new Models.Models.FinalResultsDto
                    {
                        AlienKills = cached.AlienKills + value.AlienKills,
                        HumanKills = cached.HumanKills + value.HumanKills,
                        MechKills = cached.MechKills + value.MechKills,
                        Deaths = cached.Deaths + value.Deaths,
                        Ties = cached.Ties + value.Ties,
                        Victories = cached.Victories + value.Victories,
                        GamesPlayed = cached.GamesPlayed + value.GamesPlayed
                    }
                );
            }

            var FinalResultsCombined = new ConcurrentDictionary<string, Models.Models.FinalResultsDto>(
                FinalResultsCombinedConcat.Select(kvp => new KeyValuePair<string, Models.Models.FinalResultsDto>(kvp.Key, new Models.Models.FinalResultsDto
                {
                    AlienKills = kvp.Value.AlienKills,
                    HumanKills = kvp.Value.HumanKills,
                    MechKills = kvp.Value.MechKills,
                    Deaths = kvp.Value.Deaths,
                    Ties = kvp.Value.Ties,
                    Victories = kvp.Value.Victories,
                    GamesPlayed = kvp.Value.GamesPlayed
                })
            ));

            var GameResultsCombined = 
                GameResultsCombinedConcat.Select(item => new Models.Models.IndividualGameResultsDto
                {
                    ReplayName = item.ReplayName,
                    ChatMessageCount = item.ChatMessageCount,
                    ReplayLength = item.ReplayLength,
                    WinningAlienUnitType = item.WinningAlienUnitType,
                    Players = item.Players
                }).ToList();

            ExportImport.ExportAsJson(FinalResultsCombined, GameResultsCombined, true);
            GameResults = GameResultsCombined;
            FinalResults = FinalResultsCombined;
        } else
        {
            ExportImport.ExportAsJson(FinalResults, GameResults, true);
        }

        System.IO.File.WriteAllText(CacheInfo, DateTime.UtcNow.ToString("O"));
        stopwatch.Stop();
        Console.WriteLine(stopwatch.ElapsedMilliseconds);
    }

    public static Models.Models.CombinedDataResults GetLoadedReplayData () 
    {
        var FinalMechKills = 0;
        var FinalAlienKills = 0; 
        var FinalHumanKills = 0; 
        var FinalTieCount = 0; 
        var FinalVictoryCount = 0; 
        var FinalGamesPlayed = 0; 
        var FinalDeathCount = 0;
        string playerHandle = "";

        foreach (var playerhand in UserPlayerHandles)
        {
            playerHandle = playerhand;
        }

        var hand = ExportSC2Handle == null ? playerHandle : ExportSC2Handle;
        var (ResultMechKills, ResultAlienKills, ResultHumanKills, ResultTieCount, ResultVictoryCount, ResultGamesPlayed, ResultDeathCount) = GetUserInformation(hand);
        FinalMechKills += ResultMechKills;
        FinalAlienKills += ResultAlienKills;
        FinalHumanKills += ResultHumanKills;
        FinalTieCount += ResultTieCount;
        FinalVictoryCount += ResultVictoryCount;
        FinalGamesPlayed += ResultGamesPlayed;
        FinalDeathCount += ResultDeathCount;
    
        return new Models.Models.CombinedDataResults 
        {
            FinalResults = new Models.Models.FinalResultsDto {
                AlienKills = FinalAlienKills,
                HumanKills = FinalHumanKills,
                MechKills = FinalMechKills,
                Ties = FinalTieCount,
                Victories = FinalVictoryCount,
                Deaths = FinalDeathCount,
                GamesPlayed = FinalGamesPlayed
            },
            GameResults = GameResults,
            PlayerReplayData = new()
        };
    }

    public static void UpdateLocalDataInternalMain (Dictionary<string, Models.Models.FinalResultsDto> ToChange)
    {
        foreach (var (key, dto) in ToChange)
        {
            FinalResults[key] = dto;
        }
    }

    public static ConcurrentDictionary<string, Models.Models.FinalResultsDto> GetFinalResults ()
    {
        return FinalResults;
    }

    public static List<Models.Models.IndividualGameResultsDto> GetGameResults ()
    {
        return GameResults;
    }
}