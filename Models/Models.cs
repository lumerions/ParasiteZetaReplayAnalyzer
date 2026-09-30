namespace Internal.Models;

public class Models
{
    public class FinalResultsDto
    {
        public int AlienKills { get; set; }
        public int HumanKills { get; set; }
        public int MechKills { get; set; }

        public int Ties { get; set; }
        public int Victories { get; set; }

        public int GamesPlayed { get; set; }
    }

    public class GameResultsDto
    {
        public string ReplayName {get; init;}
        public string ReplayLength {get; init;}
        public Dictionary<string, string> Players {get; init;}
    }

    public static readonly HashSet<string> AlienUnits = new()
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
    };

    public static readonly HashSet<string> MechUnits = new()
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
    };
}
