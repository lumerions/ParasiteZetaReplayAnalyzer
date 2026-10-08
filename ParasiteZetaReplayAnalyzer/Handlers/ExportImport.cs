using System;
using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using Internal.OtherMethods;

namespace Internal.ExportImporter;

public class ExportImport
{
    private static readonly string CSVPlayerDataString = "PlayerHandle, PlayerName, ReplayName";
    private static readonly string CSVGameDataString = "ReplayName, ChatMessageCount, ReplayLength, WinningAlienUnitType";
    private static readonly string CSVDataString = "AlienKills, HumanKills, MechKills, Ties, Victories, GamesPlayed, Deaths, PlayerHandle";
    public static void ExportAsXml (ConcurrentDictionary<string, Models.Models.FinalResultsDto> ResultsData, List<Models.Models.IndividualGameResultsDto> GameResults, bool Cache)
    {
        XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "     "
        };

        var FileNameToUse = Cache == true ? "UserCache" : "UserData";
        var DataFileNameToUse = Cache == true ? "CacheData.xml" : "ReplayData.xml";
        var ReplayDataXMLPath = Path.Combine(AppContext.BaseDirectory, FileNameToUse, DataFileNameToUse);
        var ApplicationUserExportedAlready = false;

        using (XmlWriter writer = XmlWriter.Create(ReplayDataXMLPath, xmlWriterSettings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("data");

            foreach (var (key, value) in ResultsData) {
                if (ApplicationUserExportedAlready == false)
                {
                    writer.WriteStartElement("pd");
                    writer.WriteElementString("handle", key);
                    writer.WriteElementString("gp", value.GamesPlayed.ToString());
                    writer.WriteElementString("v", value.Victories.ToString());
                    writer.WriteElementString("ak", value.AlienKills.ToString());
                    writer.WriteElementString("hk", value.HumanKills.ToString());
                    writer.WriteElementString("mk", value.MechKills.ToString());
                    writer.WriteElementString("t", value.Ties.ToString());
                    writer.WriteElementString("d", value.Deaths.ToString());
                    writer.WriteEndElement();
                }
                if (OtherMethods.Other.GetApplicationUserHandles().Contains(key) && ApplicationUserExportedAlready == false)
                {
                    ApplicationUserExportedAlready = true;
                }
            }

            writer.WriteStartElement("gd");

            foreach (var item in GameResults) {
                writer.WriteStartElement("gamedata");
                writer.WriteElementString("rn", item.ReplayName.ToString());
                writer.WriteElementString("messagescount", item.ChatMessageCount.ToString());
                writer.WriteElementString("rl", item.ReplayLength.ToString());
                writer.WriteElementString("waut", item.WinningAlienUnitType == null ? "" : item.WinningAlienUnitType.ToString());
                writer.WriteStartElement("players");

                foreach (var playerItem in item.Players)
                {
                    writer.WriteStartElement("player");
                    writer.WriteElementString("handle", playerItem.PlayerHandle);
                    writer.WriteElementString("pu", playerItem.PlayerUsername);
                    writer.WriteElementString("rfn", playerItem.ReplayName);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
    }

    public static void ExportAsJson (ConcurrentDictionary<string, Models.Models.FinalResultsDto> ResultsData, List<Models.Models.IndividualGameResultsDto> GameResults)
    {
        var JsonWriteOptions = new JsonSerializerOptions { WriteIndented = true };

        try
        {
            var combined = new Models.Models.CombinedResults { Players = ResultsData, Games = GameResults };
            string Json = JsonSerializer.Serialize(combined, JsonWriteOptions);
            var JsonFilePath = Path.Combine(AppContext.BaseDirectory, "UserData", "ReplayData.json");
            File.WriteAllText(JsonFilePath, Json);
        } catch (Exception err)
        {
            Console.WriteLine(err);
        }
    }

    public static void ExportAsCsv (ConcurrentDictionary<string, Models.Models.FinalResultsDto> ResultsData, List<Models.Models.IndividualGameResultsDto> GameResults)
    {
        try
        {
            var CSVPlayerData = new List<string> { CSVPlayerDataString };
            var CSVGameData = new List<string> { CSVGameDataString };
            var CSVData = new List<string> { CSVDataString };

            foreach (var item in ResultsData)
            {
                var SC2Handle = item.Key;
                var AlienKills = item.Value.AlienKills;
                var HumanKills = item.Value.HumanKills;
                var MechKills = item.Value.MechKills;
                var Ties = item.Value.Ties;
                var Victories = item.Value.Victories;
                var GamesPlayed = item.Value.GamesPlayed;
                var Deaths = item.Value.Deaths;
                CSVData.Add($"{AlienKills},{HumanKills},{MechKills},{Ties},{Victories},{GamesPlayed},{Deaths},{SC2Handle}");
            }

            foreach (var item in GameResults)
            {
                var ReplayLength = item.ReplayLength;
                var ChatMessageCount = item.ChatMessageCount;
                var ReplayName = item.ReplayName;
                var WinningAlienUnitType = item.WinningAlienUnitType;

                if (ReplayName.Contains(","))
                {
                    ReplayName.Replace(",", "");
                }

                CSVGameData.Add($"{ReplayName},{ChatMessageCount},{ReplayLength},{WinningAlienUnitType}");

                foreach (var playerItem in item.Players)
                {
                    CSVPlayerData.Add($"{playerItem.PlayerHandle},{playerItem.PlayerUsername},{playerItem.ReplayName}");
                }
            }

            var CSVPlayerDataFilePath = Path.Combine(AppContext.BaseDirectory, "UserData", "PlayerReplayData.csv");
            var CSVFilePath = Path.Combine(AppContext.BaseDirectory, "UserData", "ReplayData.csv");
            var CSVFileGameDataPath = Path.Combine(AppContext.BaseDirectory, "UserData", "GameData.csv");

            File.WriteAllLines(CSVPlayerDataFilePath, CSVPlayerData);
            File.WriteAllLines(CSVFilePath, CSVData);
            File.WriteAllLines(CSVFileGameDataPath, CSVGameData);
        } catch (Exception err)
        {
            Console.WriteLine(err);
        }
    }


    public static Models.Models.CombinedDataResults ImportAsXml (string XmlFilePath)
    {
        if (Path.GetExtension(XmlFilePath) != ".xml") return new Models.Models.CombinedDataResults {GameResults = new(), FinalResults = new()};

        var GameDataList = new List<Models.Models.IndividualGameResultsDto>();
        var FinalMechKills = 0;
        var FinalHumanKills = 0;
        var FinalAlienKills = 0;
        var FinalGamesPlayed = 0;
        var FinalVictoryCount = 0;
        var FinalTieCount = 0;
        var FinalDeathCount = 0;

        try {
            XDocument document = XDocument.Load(XmlFilePath);

            foreach (var element in document.Descendants("pd"))
            {
                var MechKills = element.Element("mk")?.Value;
                var HumanKills = element.Element("hk")?.Value;
                var AlienKills = element.Element("ak")?.Value;
                var GamesPlayed = element.Element("gp")?.Value;
                var VictoryCount = element.Element("v")?.Value;
                var TieCount = element.Element("t")?.Value;
                var DeathCount = element.Element("d")?.Value;

                if (int.TryParse(MechKills, out var MechKillsInt) && int.TryParse(HumanKills, out var HumanKillsInt) && int.TryParse(AlienKills, out var AlienKillsInt) && int.TryParse(GamesPlayed, out var GamesPlayedInt) && int.TryParse(VictoryCount, out var VictoryCountInt) && int.TryParse(TieCount, out var TieCountInt) && int.TryParse(DeathCount, out var DeathCountInt))
                {
                    FinalTieCount += TieCountInt;
                    FinalVictoryCount += VictoryCountInt;
                    FinalGamesPlayed += GamesPlayedInt;
                    FinalAlienKills += AlienKillsInt;
                    FinalHumanKills += HumanKillsInt;
                    FinalMechKills += MechKillsInt;
                    FinalDeathCount += DeathCountInt;
                } 
            }

            foreach (var element in document.Descendants("gamedata"))
            {
                var ReplayName = element.Element("rn")?.Value;
                var MessagesCount = element.Element("messagescount")?.Value;
                var ReplayLength = element.Element("rl")?.Value;

                if (int.TryParse(MessagesCount, out var MessagesCountInt) && double.TryParse(ReplayLength, out var ReplayLengthInt))
                {
                    var PlayersList = new List<Models.Models.PlayersReplayData>();

                    foreach (var p in element.Descendants("player"))
                    {
                        var PlayerHandle = p.Element("handle")?.Value;
                        var PlayerUsername = p.Element("pu")?.Value;
                        var ReplayFileName = p.Element("rfn")?.Value;

                        if (!string.IsNullOrEmpty(PlayerHandle))
                        {
                            if (string.IsNullOrEmpty(PlayerUsername)) PlayerUsername = "Unknown";
                            Other.AddToPlayerData(PlayersList, PlayerUsername, PlayerHandle, ReplayFileName);            
                        }
                    }

                    GameDataList.Add(new Models.Models.IndividualGameResultsDto
                    {
                        ReplayName = ReplayName,
                        ChatMessageCount = MessagesCountInt,
                        ReplayLength = ReplayLengthInt,
                        Players = PlayersList
                    });
                }
            }
        } 
        catch (XmlException xmlerr) {
            Console.WriteLine("Invalid XML Format");
        } 
        catch (IOException ioerr)
        {
            Console.WriteLine("IO Error");
        }

        var CombinedObject = OtherMethods.Other.CreateCombinedObject(GameDataList, FinalAlienKills, FinalHumanKills, FinalMechKills, FinalTieCount, FinalVictoryCount, FinalGamesPlayed, FinalDeathCount);
        var ChangeDataDict = new Dictionary<string, Models.Models.FinalResultsDto> { { OtherMethods.Other.GetAvailableHandleOrDefault(), CombinedObject.FinalResults } };
    
        OtherMethods.Other.UpdateLocalDataInternalMainUpdate(ChangeDataDict);
        return CombinedObject;
    }
    
    public static Models.Models.CombinedDataResults ImportAsCsv (string CSVFilePath)
    {
        if (Path.GetExtension(CSVFilePath) != ".csv") return new Models.Models.CombinedDataResults {GameResults = new(), FinalResults = new(), PlayerReplayData = new()};

        var CSVLines = File.ReadAllLines(CSVFilePath);
        if (CSVLines.Length == 0) return new Models.Models.CombinedDataResults {GameResults = new(), FinalResults = new() , PlayerReplayData = new()};

        var FinalMechKills = 0;
        var FinalHumanKills = 0;
        var FinalAlienKills = 0;
        var FinalGamesPlayed = 0;
        var FinalVictoryCount = 0;
        var FinalTieCount = 0;
        var FinalDeathCount = 0;
        var PlayerStats = new Dictionary<string, Models.Models.FinalResultsDto>();

        if (CSVLines[0].StartsWith("AlienKills"))
        {
            foreach (var line in CSVLines.Skip(1))
            {
                var LineSplit = line.Split(",");
                var AlienKills = LineSplit[0];
                var HumanKills = LineSplit[1];
                var MechKills = LineSplit[2];
                var Ties = LineSplit[3];
                var Victories = LineSplit[4];
                var GamesPlayed = LineSplit[5];
                var Deaths = LineSplit[6];
                var handle = LineSplit[7];

                if (int.TryParse(AlienKills, out var AlienKillsInt) && int.TryParse(HumanKills, out var HumanKillsInt) && int.TryParse(MechKills, out var MechKillsInt) && int.TryParse(Ties, out var TiesInt) && int.TryParse(Victories, out var VictoriesInt) && int.TryParse(GamesPlayed, out var GamesPlayedInt) && int.TryParse(Deaths, out var DeathsInt)) 
                {
                    PlayerStats[handle] = new Models.Models.FinalResultsDto
                    {
                        MechKills = MechKillsInt, 
                        HumanKills = HumanKillsInt, 
                        AlienKills = AlienKillsInt,
                        GamesPlayed = GamesPlayedInt, 
                        Victories = VictoriesInt, 
                        Ties = TiesInt, 
                        Deaths = DeathsInt
                    };
                }   
            }

            var CombinedObject = new Models.Models.CombinedDataResults 
            {
                FinalResults = new(),
                GameResults = new(),
                PlayerReplayData = new()
            };

            Other.UpdateLocalDataViaCsvImport(CombinedObject, PlayerStats);
            return CombinedObject;
        } else if (CSVLines[0].StartsWith("ReplayName"))
        {
            var GameResults = new List<Models.Models.IndividualGameResultsDto>();

            foreach (var line in CSVLines.Skip(1))
            {
                var LineSplit = line.Split(",");
                var ReplayName = LineSplit[0];
                var ChatMessageCount = LineSplit[1];
                var ReplayLength = LineSplit[2];
                var WinningAlienUnitType = LineSplit[3];

                GameResults.Add(new Models.Models.IndividualGameResultsDto
                {
                    ReplayName = ReplayName,
                    ChatMessageCount = int.TryParse(ChatMessageCount, out var count) ? count : 0,
                    ReplayLength = double.TryParse(ReplayLength, out var doublecount) ? doublecount : 0,
                    WinningAlienUnitType = WinningAlienUnitType
                });
            }

            var CombinedObject = new Models.Models.CombinedDataResults 
            {
                FinalResults = new(),
                GameResults = GameResults,
                PlayerReplayData = new()
            };

            Other.UpdateLocalDataViaCsvImport(CombinedObject, new Dictionary<string, Models.Models.FinalResultsDto>());

            return CombinedObject;
        } else if (CSVLines[0].StartsWith("PlayerHandle"))
        {
            var PlayerReplayGameData = new List<Models.Models.PlayersReplayData>();

            foreach (var line in CSVLines.Skip(1))
            {
                var LineSplit = line.Split(",");
                var PlayerHandle = LineSplit[0];
                var PlayerName = LineSplit[1];
                var ReplayName = LineSplit[2];

                PlayerReplayGameData.Add(new Models.Models.PlayersReplayData
                {
                    PlayerHandle = PlayerHandle,
                    PlayerUsername = PlayerName,
                    ReplayName = ReplayName
                });
            }

            var CombinedObject = new Models.Models.CombinedDataResults 
            {
                FinalResults = new(),
                GameResults = new(),
                PlayerReplayData = PlayerReplayGameData
            };

            Other.UpdateLocalDataViaCsvImport(CombinedObject, new Dictionary<string, Models.Models.FinalResultsDto>());
            
            return CombinedObject;
        }

        return new Models.Models.CombinedDataResults {GameResults = new(), FinalResults = new(), PlayerReplayData = new()};
    }

    public static Models.Models.CombinedDataResults? ImportAsJson (string JsonFilePath)
    {
        if (Path.GetExtension(JsonFilePath) != ".json") return null;

        var JsonData = File.ReadAllText(JsonFilePath);
        try
        {
            var Data = JsonSerializer.Deserialize<Models.Models.CombinedResults>(JsonData);
            var GameResults = new List<Models.Models.IndividualGameResultsDto>();
            List<Models.Models.PlayersReplayData> PlayerData = new();
            
            foreach (var item in Data.Games) 
            {
            
                foreach (var playerItem in item.Players)
                {
                    Other.AddToPlayerData(PlayerData, playerItem.PlayerUsername, playerItem.PlayerHandle, playerItem.ReplayName);            
                }

                GameResults.Add(new Models.Models.IndividualGameResultsDto
                {
                    ReplayName = item.ReplayName,
                    ChatMessageCount = item.ChatMessageCount,
                    ReplayLength = item.ReplayLength,
                    WinningAlienUnitType = item.WinningAlienUnitType,
                    Players = PlayerData
                });
            }

            var FinalMechKills = 0;
            var FinalHumanKills = 0;
            var FinalAlienKills = 0;
            var FinalGamesPlayed = 0;
            var FinalVictoryCount = 0;
            var FinalTieCount = 0;
            var FinalDeathCount = 0;

            foreach (var stat in Data.Players)
            {
                FinalMechKills = stat.Value.MechKills;
                FinalHumanKills = stat.Value.HumanKills;
                FinalAlienKills = stat.Value.AlienKills;
                FinalGamesPlayed = stat.Value.GamesPlayed;
                FinalVictoryCount = stat.Value.Victories;
                FinalTieCount = stat.Value.Ties;
                FinalDeathCount = stat.Value.Deaths;
            }

            var CombinedObject = new Models.Models.CombinedDataResults {
                GameResults = GameResults, 
                FinalResults = new Models.Models.FinalResultsDto {
                    AlienKills = FinalAlienKills,
                    HumanKills = FinalHumanKills,
                    MechKills = FinalMechKills,
                    Ties = FinalTieCount,
                    Victories = FinalVictoryCount,
                    Deaths = FinalDeathCount,
                    GamesPlayed = FinalGamesPlayed
                },
                PlayerReplayData = new()
            };

            var ChangeDataDict = new Dictionary<string, Models.Models.FinalResultsDto> { { OtherMethods.Other.GetAvailableHandleOrDefault(), CombinedObject.FinalResults } };
            OtherMethods.Other.UpdateLocalDataInternalMainUpdate(ChangeDataDict);
            return CombinedObject;
        } catch (Exception err)
        {
            Console.WriteLine(err);
            return null;
        }
    }
}