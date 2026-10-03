using System;
using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;
using System.Text;

namespace Internal.ExportImporter;

public class CombinedResults
{
    public ConcurrentDictionary<string, Models.Models.FinalResultsDto> Players { get; init; }
    public List<Models.Models.IndividualGameResultsDto> Games { get; init; }
}

public class ExportImport
{
    private static readonly string CSVPlayerDataString = "PlayerHandle, PlayerName, ReplayName";
    private static readonly string CSVGameDataString = "ReplayName, ChatMessageCount, ReplayLength, WinningAlienUnitType";
    private static readonly string CSVDataString = "AlienKills, HumanKills, MechKills, Ties, Victories, GamesPlayed, Deaths";
    public static void ExportAsXml (ConcurrentDictionary<string, Models.Models.FinalResultsDto> ResultsData, List<Models.Models.IndividualGameResultsDto> GameResults)
    {
        XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "     "
        };

        var ReplayDataXMLPath = Path.Combine(AppContext.BaseDirectory, "UserData", "ReplayData.xml");

        using (XmlWriter writer = XmlWriter.Create(ReplayDataXMLPath, xmlWriterSettings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("data");

            foreach (var (key, value) in ResultsData) {
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

            writer.WriteStartElement("gd");

            foreach (var item in GameResults) {
                writer.WriteStartElement("gamedata");
                writer.WriteElementString("rn", item.ReplayName.ToString());
                writer.WriteElementString("messagescount", item.ChatMessageCount.ToString());
                writer.WriteElementString("rl", item.ReplayLength.ToString());
                writer.WriteElementString("waut", item.WinningAlienUnitType.ToString());
                writer.WriteStartElement("players");

                foreach (var (key, value) in item.Players)
                {
                    writer.WriteStartElement("player");
                    writer.WriteElementString("handle", value.PlayerHandle);
                    writer.WriteElementString("pu", value.PlayerUsername);
                    writer.WriteElementString("rfn", key);
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
            var combined = new CombinedResults { Players = ResultsData, Games = GameResults };
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
                var AlienKills = item.Value.AlienKills;
                var HumanKills = item.Value.HumanKills;
                var MechKills = item.Value.MechKills;
                var Ties = item.Value.Ties;
                var Victories = item.Value.Victories;
                var GamesPlayed = item.Value.GamesPlayed;
                var Deaths = item.Value.Deaths;
                CSVData.Add($"{AlienKills},{HumanKills},{MechKills},{Ties},{Victories},{GamesPlayed},{Deaths}");
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

                foreach (var (key, value) in item.Players)
                {
                    CSVPlayerData.Add($"{value.PlayerHandle},{value.PlayerUsername},{key}");
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
                //var SC2Handle = element.Element("handle")?.Value;
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

                if (int.TryParse(MessagesCount, out var MessagesCountInt) && int.TryParse(ReplayLength, out var ReplayLengthInt))
                {
                    var PlayersList = new Dictionary<string, Models.Models.PlayerDataItem>();

                    foreach (var p in element.Descendants("player"))
                    {
                        var PlayerHandle = p.Element("handle")?.Value;
                        var PlayerUsername = p.Element("pu")?.Value;
                        var ReplayFileName = p.Element("rfn")?.Value;

                        if (!string.IsNullOrEmpty(PlayerHandle))
                        {
                            if (string.IsNullOrEmpty(PlayerUsername)) PlayerUsername = "Unknown";
                            PlayersList[ReplayFileName] = new Models.Models.PlayerDataItem
                            {
                                PlayerUsername = PlayerUsername,
                                PlayerHandle = PlayerHandle
                            };
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

        return CreateCombinedObject(GameDataList, FinalAlienKills, FinalHumanKills, FinalMechKills, FinalTieCount, FinalVictoryCount, FinalGamesPlayed, FinalDeathCount);
    }
    
    public static Models.Models.CombinedDataResults ImportAsCsv (string CSVFilePath)
    { // TODO finish import csv
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
                var GamesPlayed = LineSplit[6];
                var Deaths = LineSplit[6];
                
                if (int.TryParse(AlienKills, out var AlienKillsInt))
                {
                    FinalAlienKills += AlienKillsInt;
                }

                if (int.TryParse(HumanKills, out var HumanKillsInt))
                {
                    FinalHumanKills += HumanKillsInt;
                }

                if (int.TryParse(MechKills, out var MechKillsInt))
                {
                    FinalMechKills += MechKillsInt;
                }

                if (int.TryParse(Ties, out var TiesInt))
                {
                    FinalTieCount += TiesInt;
                }

                if (int.TryParse(Victories, out var VictoriesInt))
                {
                    FinalVictoryCount += VictoriesInt;
                }

                if (int.TryParse(GamesPlayed, out var GamesPlayedInt))
                {
                    FinalGamesPlayed += GamesPlayedInt;
                }

                if (int.TryParse(Deaths, out var DeathsInt))
                {
                    FinalDeathCount += DeathsInt;
                }
            }

            return new Models.Models.CombinedDataResults 
            {
                FinalResults = {
                    AlienKills = FinalAlienKills,
                    HumanKills = FinalHumanKills,
                    MechKills = FinalMechKills,
                    Ties = FinalTieCount,
                    Victories = FinalVictoryCount,
                    Deaths = FinalDeathCount,
                    GamesPlayed = FinalGamesPlayed
                },
                GameResults = new(),
                PlayerReplayData = new()
            };
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

            return new Models.Models.CombinedDataResults 
            {
                FinalResults = new(),
                GameResults = GameResults,
                PlayerReplayData = new()
            };
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

            return new Models.Models.CombinedDataResults 
            {
                FinalResults = new(),
                GameResults = new(),
                PlayerReplayData = PlayerReplayGameData
            };
        }

        return new Models.Models.CombinedDataResults {GameResults = new(), FinalResults = new() , PlayerReplayData = new()};
    }

    public static CombinedResults? ImportAsJson (string JsonFilePath)
    {
        if (Path.GetExtension(JsonFilePath) != ".json") return null;

        var JsonData = File.ReadAllText(JsonFilePath);
        try
        {
            var Data = JsonSerializer.Deserialize<CombinedResults>(JsonData);
            return Data;
        } catch (Exception err)
        {
            Console.WriteLine(err);
            return null;
        }
    }
}