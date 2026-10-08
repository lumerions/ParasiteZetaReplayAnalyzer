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

    private struct PlayerDataChanges {
        public string AlienFormType;
        public int AlienKills;
        public int HumanKills;
        public int MechKills;
        public int DeathCount;
    };

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

        var PlayerData = new List<Models.Models.PlayersReplayData>(12);

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
        }

        int AliveHumanPlayerCount = 0;
        int AliveAlienPlayerCount = 0;
        var DiedAlready = new bool[14];
        var PDData = new PlayerDataChanges[14];

        foreach (var unit in replay.TrackerEvents.SUnitBornEvents)
        {
            var UnitType = unit.UnitTypeName;
            var PlayerIdWhoDied = unit.ControlPlayerId;
            var Died = unit.SUnitDiedEvent;

            if (PlayerIdWhoDied <= 0 || PlayerIdWhoDied > 12)
            {
                continue;
            }

            if (DiedAlready[PlayerIdWhoDied])
            {
                continue;
            }

            if (IsAlienUnit(UnitType))
            {
                PDData[PlayerIdWhoDied].AlienFormType = UnitType;
                AliveAlienPlayerCount += 1;
            } else
            {
                if (!IsMechUnit(UnitType))
                {
                    PDData[PlayerIdWhoDied].AlienFormType = "Human";
                    AliveHumanPlayerCount += 1;
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
                PDData[PlayerIdWhoDied].AlienFormType = "DeadAlien";

                AliveAlienPlayerCount -= 1;

                if (UserPlayerHandles.Contains(GetHandlesByPlayerId(PlayerIdWhoDied)))
                {
                    UserUsingApplicationDied = true;
                }
            } else
            {
                if (!IsMechUnit(UnitType)) {

                    PDData[PlayerIdWhoDied].AlienFormType = "DeadHuman";

                    AliveHumanPlayerCount -= 1;

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

            if (AlienKill)
            {
                PDData[KillerId].AlienKills = PDData[KillerId].AlienKills += 1;
            } else
            {
                if (!MechKill)
                {
                    PDData[KillerId].HumanKills = PDData[KillerId].HumanKills += 1;
                } else
                {
                    PDData[KillerId].MechKills = PDData[KillerId].MechKills += 1;
                }
            }

            if (UserUsingApplicationDied)
            {
                PDData[PlayerIdWhoDied].DeathCount = PDData[PlayerIdWhoDied].DeathCount += 1;
            }

            DiedAlready[PlayerIdWhoDied] = true;
        }

        string DetermineVictoryCondition ()
        {
            if (AliveAlienPlayerCount > AliveHumanPlayerCount)
            {
                return "Alien";
            }
            if (AliveAlienPlayerCount == AliveHumanPlayerCount)
            {
                return "Tie";
            }

            return "Human";
        }

        void UpdateWinLoseCount (string PlayerHandle, bool Won, bool? Tie, int DeathIncrementCount, int MechKills, int AlienKills, int HumanKills)
        {
            var (ExistingMechKills, ExistingAlienKills, ExistingHumanKills, ExistingTies, ExistingVictories, ExistingGamesPlayed, ExistingDeaths) = GetUserInformation(PlayerHandle);

            ExistingMechKills += MechKills;
            ExistingAlienKills += AlienKills;
            ExistingHumanKills += HumanKills;

            if (Won && Tie == null)
            {
                ExistingVictories += 1;
            }

            if (Tie != null)
            {
                ExistingTies += 1;
            }

            if (DeathIncrementCount > 0)
            {
                ExistingDeaths += DeathIncrementCount;
            }

            ExistingGamesPlayed += 1;

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

        string WhoWon = DetermineVictoryCondition();
        var AlienWin = WhoWon == "Alien";
        var MostUsedAlienForm = new List<string>();

        for (int i = 0; i < PDData.Length; i++)
        {
            var handle = GetHandlesByPlayerId(i);

            if (string.IsNullOrEmpty(handle))
                continue;

            ref var Data = ref PDData[i];
            var MechKills = Data.MechKills;
            var AlienKills = Data.AlienKills;
            var HumanKills = Data.HumanKills;
            var AlienFormType = Data.AlienFormType;
            var IsHumanPlayer = AlienFormType == "Human" || AlienFormType == "DeadHuman";

            if (!IsHumanPlayer)
            {
                MostUsedAlienForm.Add(AlienFormType);
                UpdateWinLoseCount(handle, AlienWin, WhoWon == "Tie" ? true : null, Data.DeathCount, MechKills, AlienKills, HumanKills);
            } else
            {
                UpdateWinLoseCount(handle, !AlienWin, WhoWon == "Tie" ? true : null, Data.DeathCount, MechKills, AlienKills, HumanKills);
            }
        }

        var LeastUsedAlienForm = MostUsedAlienForm.GroupBy(x => x).MinBy(x => x.Count())?.Key;  // we do this because certain host unit types are different from spawn unit types

        if (!AlienWin)
        {
            LeastUsedAlienForm = "None";
        }

        var ReplayChatMessageCount = replay.ChatMessages.Count;
        var ReplayLength = replay.Header.ElapsedGameLoops / 22.4;
        TimeSpan TimeSpanSeconds = TimeSpan.FromSeconds(ReplayLength);
        double TimeSpanMinutes = TimeSpanSeconds.TotalMinutes;

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
        var Benchmark = false;
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
                        if (!Benchmark)
                        {
                            ReplayPaths.Add(ReplayPath);
                        } else
                        {
                            await LoadReplay(ReplayPath);
                        }
                    }

                    Count += 1;
                }
            }
        }

        if (!Benchmark)
        {
            await Parallel.ForEachAsync(ReplayPaths, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, 
            async (ReplayPath, cancellationToken) => 
            {
                await LoadReplay(ReplayPath);
            });
        }

        Console.WriteLine($"Replays scanned: {ReplaysAnalyzed}");
      //  var MostPlayedWith = FinalResults.OrderByDescending(i => i.Value.GamesPlayed);

      //  foreach (var item in MostPlayedWith)
      //  {
          //  if (UserPlayerHandles.Contains(item.Key))
          //  {
          //      Console.WriteLine($"[{item.Key},{item.Value.GamesPlayed}-{item.Value.Victories}-{item.Value.AlienKills}-{item.Value.HumanKills}-{item.Value.MechKills}]");
           // }
        //}

        stopwatch.Stop();
        //ExportImport.ImportAsCsv(@"C:\Users\asdfg\Desktop\ParasiteZetaReplayAnalyzer\bin\Debug\net10.0\UserData\ReplayData.csv");
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
        string ReplayHandle = "";

        foreach (var playerhand in UserPlayerHandles)
        {
            ReplayHandle = playerhand;
        }

        var hand = ExportSC2Handle == null ? ReplayHandle : ExportSC2Handle;
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