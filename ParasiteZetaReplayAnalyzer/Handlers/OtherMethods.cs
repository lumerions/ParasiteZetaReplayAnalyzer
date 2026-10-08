using System.Security.Cryptography;
using Internal.Models;

namespace Internal.OtherMethods;

public class Other
{
    public static string ExportSC2Handle; // why the fuck do we do it this way? its because multiple handles can exist 
    // for 1 application user and this can cause issues like inflated stat values so we select one of the users random handles and just use that for everything in terms of loading
    private static Dictionary<string, Models.Models.FinalResultsDto> PendingPlayerStats = new();
    public static HashSet<string> UserPlayerHandles = new();
    private static Models.Models.FinalResultsDto FinalResults = new();
    private static List<Models.Models.IndividualGameResultsDto> GameResults = new();
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

    public static bool StationSecurityOrAlien (string PlayerName)
    {
        if (PlayerName == "Station Security" || PlayerName == "Alien")
        {
            return true;
        } 

        return false;
    }

    public static void AddToPlayerData (List<Models.Models.PlayersReplayData> PlayerData, string PlrName, string PlrHandle, string ReplayFileName) 
    {
        PlayerData.Add(new Models.Models.PlayersReplayData
        {
            PlayerUsername = PlrName,
            PlayerHandle = PlrHandle,
            ReplayName = ReplayFileName
        }); 
    }

    public static void UpdateLocalDataViaCsvImport (Models.Models.CombinedDataResults Combined,  Dictionary<string, Models.Models.FinalResultsDto> PlayerStats)
    {
        var FileType = 0;
        if (PlayerStats != null && PlayerStats.Count > 0) FileType = 1;
        if (Combined.GameResults.Count > 0 && FileType == 0) FileType = 2;
        if (Combined.PlayerReplayData.Count > 0 && FileType == 0) FileType = 3;
        FileTypeProcessing(FileType, Combined, PlayerStats);
    }

    public static void FileTypeProcessing (int FileType, Models.Models.CombinedDataResults CombinedData, Dictionary<string, Models.Models.FinalResultsDto> Stats)
    {
        if (FileType == 1)
        {
            PendingPlayerStats = Stats;
        }
        if (FileType == 2)
        {
            GameResults = CombinedData.GameResults;
        }
        if (FileType == 3)
        {
            var NewGameResults = CombinedData.GameResults.Select(x => new Models.Models.IndividualGameResultsDto 
            {
                ReplayName = x.ReplayName,
                ChatMessageCount = x.ChatMessageCount,
                ReplayLength = x.ReplayLength,
                WinningAlienUnitType = x.WinningAlienUnitType,
                Players = CombinedData.PlayerReplayData.Where(item => item.ReplayName == x.ReplayName).Select(item => new Models.Models.PlayersReplayData
                {
                    ReplayName = item.ReplayName,
                    PlayerHandle = item.PlayerHandle,
                    PlayerUsername = item.PlayerUsername
                }).ToList()
            }).ToList();

            if (PendingPlayerStats.Count > 0)
            {                        
                PendingPlayerStats.TryGetValue(GetAvailableHandleOrDefault(), out var PlayerObjectFind);
                var CombinedObject = CreateCombinedObject(NewGameResults, PlayerObjectFind.AlienKills, PlayerObjectFind.HumanKills, PlayerObjectFind.MechKills, PlayerObjectFind.Ties, PlayerObjectFind.Victories, PlayerObjectFind.GamesPlayed, PlayerObjectFind.Deaths);
                var ChangeDataDict = new Dictionary<string, Models.Models.FinalResultsDto> { { GetAvailableHandleOrDefault(), PlayerObjectFind } };
                UpdateLocalDataInternalMainUpdate(ChangeDataDict);
                PendingPlayerStats = new();
            }
        }
    }

    public static void UpdateLocalDataInternalMainUpdate (Dictionary<string, Models.Models.FinalResultsDto> ToChange)
    {
        foreach (var (key, value) in ToChange) {
            ExportSC2Handle = key;
        }
        Main.InternalMain.UpdateLocalDataInternalMain(ToChange);
    }

    public static string GetAvailableHandleOrDefault ()
    {
        if (ExportSC2Handle != null)
        {
            return ExportSC2Handle;
        }

        var Skipped = false;

        foreach (var handle in GetApplicationUserHandles())
        {
            if (RandomNumberGenerator.GetInt32(1, 3) == 2)
            {
                Skipped = true;
                continue;
            }

            if (Skipped)
            {
                return handle;
            }

            return handle;
        }

        return "";
    }

    public static HashSet<string> GetApplicationUserHandles ()
    {
        return UserPlayerHandles;
    }

    public static Models.Models.CombinedDataResults CreateCombinedObject (List<Models.Models.IndividualGameResultsDto> GameData, int AlienKills, int HumanKills, int MechKills, int Ties, int Victories, int GamesPlayed, int DeathCount)
    {
        return new Models.Models.CombinedDataResults {
            FinalResults = new Models.Models.FinalResultsDto
            {
                AlienKills = AlienKills,
                HumanKills = HumanKills,
                MechKills = MechKills,
                Ties = Ties,
                Victories = Victories,
                GamesPlayed = GamesPlayed,
                Deaths = DeathCount
            },
            GameResults = GameData,
            PlayerReplayData = new()
        };
    }
}