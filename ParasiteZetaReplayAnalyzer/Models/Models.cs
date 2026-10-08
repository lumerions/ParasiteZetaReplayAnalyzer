using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Internal.Models;

public class Models
{

    public class CombinedResults
    {
        public ConcurrentDictionary<string, FinalResultsDto> Players { get; init; }
        public List<IndividualGameResultsDto> Games { get; init; }
    }

    public class FinalResultsDto
    {
        public int AlienKills { get; set; }
        public int HumanKills { get; set; }
        public int MechKills { get; set; }

        public int Ties { get; set; }
        public int Victories { get; set; }

        public int GamesPlayed { get; set; }
        public int Deaths { get; set; }
    }

    public class IndividualGameResultsDto
    {
        public string ReplayName { get; init; }
        public int ChatMessageCount { get; init; }
        public double ReplayLength { get; init; }
        public string? WinningAlienUnitType { get; init; }
        public List<PlayersReplayData> Players { get; init; }
    }

    public class CombinedDataResults
    {
        public List<IndividualGameResultsDto> GameResults { get; set; }
        public FinalResultsDto FinalResults { get; set; }
        public List<PlayersReplayData> PlayerReplayData { get; set; }
    }

    public class PlayerDataItem
    {
        public string PlayerUsername { get; set; }
        public string PlayerHandle { get; set; }
    }

    public class PlayersReplayData : PlayerDataItem
    {
        public string ReplayName { get; set; }
    }

    public static readonly FrozenSet<string> AlienUnits = new[]
    {
        "XenomorphMatriarch",
        "Yagdra",
        "ZerglingCarbot",
        "MutaliskBroodlord",
        "MutaliskViper",
        "PrimalRoach",
        "PrimalUltralisk",
        "PrimalUltralisk2",
        "Ravager",
        "Mutalisk",
        "LargeSwarmQueen",
        "HybridBehemoth2",
        "HotSTorrasque2",
        "AlienSpawn",
        "AlienSpawn2",
        "Cerebrate",
        "HotSSwarmling",
        "HotSTorrasque",
        "HugeSwarmQueen",
        "HunterKiller",
        "Hydralisk2",
        "InfestedTerran",
        "InfestedTerran3",
        "LarvalQueen",
        "LurkerBurrowed",
        "Archon",
        "Archon2",
        "Archon22",
        "Archon222",
        "Archon3",
        "Archon32",
        "HighTemplarShakuras",
        "HighTemplarTaldarim",
        "HybridDominator",
        "VorazunChampion",
        "PrisonZealot",
        "WhizzardAlien",
        "WhizzardAlien2",
        "Zeratul",
        "AlienSpawnVampireBatTier5",
        "AlphaXenodon",
        "BroodLord",
        "Dehaka",
        "DehakaMirrorImage"
    }.ToFrozenSet();

    public static readonly FrozenSet<string> MechUnits = new[]
    {
        "WarHound",
        "MengskGoliath",
        "Cyclone",
        "TitanMechAssault",
        "HellionTank",
        "SiegeBreaker",
        "Diamondback",
        "MercReaper",
        "FenixChampion",
        "ImmortalTaldarim",
        "ColossusTaldarim",
        "ZealotPurifier"
    }.ToFrozenSet();
}
