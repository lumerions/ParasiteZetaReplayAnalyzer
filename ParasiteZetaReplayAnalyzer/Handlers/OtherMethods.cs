using System.Security.Cryptography;
using Internal.Models;

namespace Internal.OtherMethods;

public class Other
{
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

    public static void AddToPlayerData (Dictionary<string, Models.Models.PlayersReplayData> PlayerData, string PlrName, string PlrHandle, string ReplayFileName) 
    {
        bool Result = PlayerData.TryAdd(RandomNumberGenerator.GetHexString(6), new Models.Models.PlayersReplayData
        {
            PlayerUsername = PlrName,
            PlayerHandle = PlrHandle,
            ReplayName = ReplayFileName
        }); 

        if (!Result)
        {
            PlayerData.Add(RandomNumberGenerator.GetHexString(6), new Models.Models.PlayersReplayData
            {
                PlayerUsername = PlrName,
                PlayerHandle = PlrHandle,
                ReplayName = ReplayFileName
            });
        }
    }

    public static void UpdateLocalData (Models.Models.CombinedDataResults CombinedData)
    {
        Main.InternalMain.UpdateLocalDataMain(CombinedData);
    }


    public static void UpdateLocalDataCSV (Dictionary<string, Models.Models.FinalResultsDto> stats)
    {
        Main.InternalMain.UpdateLocalDataMainCSV(stats);
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
                Players = CombinedData.PlayerReplayData.Where(item => item.ReplayName == x.ReplayName).ToDictionary(listitem => RandomNumberGenerator.GetHexString(6), listitemvalue => new Models.Models.PlayersReplayData
                {
                    ReplayName = listitemvalue.ReplayName,
                    PlayerHandle = listitemvalue.PlayerHandle,
                    PlayerUsername = listitemvalue.PlayerUsername
                })
            }).ToList();

            CombinedData.GameResults = NewGameResults;

            if (PendingPlayerStats.Count > 0)
            {
                UpdateLocalDataCSV(PendingPlayerStats);
                PendingPlayerStats = new();
            }
        }
    }

    public static HashSet<string> GetApplicationUserHandles ()
    {
        return UserPlayerHandles;
    }
}