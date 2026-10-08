using System.Collections.Concurrent;
using System;
using System.Linq;
using Internal.Models;

namespace Internal.Calc;

public class DataCalculator
{
    public static List<string> GetHighestWinningAlienForm (List<Models.Models.IndividualGameResultsDto> GameResults)
    {
        var HighestWinningAlienForms = 
        GameResults.Where(x => x.WinningAlienUnitType != null)
        .CountBy(x => x.WinningAlienUnitType)
        .OrderByDescending(i => i.Value)
        .Select(x => x.Key)
        .ToList();

        return HighestWinningAlienForms;
    }

    public class KDResult
    {
        public decimal Kd { get; set; }
        public string Handle { get; set; }
    }

    public class HumanAlienWinRate
    {
        public int AlienWins { get; set; }
        public int HumanWins { get; set; }
        public decimal AlienToHumanRatio { get; set; }
    } 

    public class BestItem
    {
        
    }

    public static List<KDResult> GetHighestKD (ConcurrentDictionary<string, Models.Models.FinalResultsDto> FinalResults)
    {
        var HighestKd = FinalResults.OrderByDescending(i => i.Value.Deaths == 0 ? 1 : (decimal) (i.Value.AlienKills + i.Value.HumanKills) / i.Value.Deaths).Select(v => new KDResult
        {
            Kd = v.Value.Deaths == 0 ? 1 : (decimal) (v.Value.AlienKills + v.Value.HumanKills) / v.Value.Deaths,
            Handle = v.Key
        }).ToList();
        return HighestKd;
    }

    public static HumanAlienWinRate GetAlienHumanWinRate (List<Models.Models.IndividualGameResultsDto> GameResults)
    {
        var HumanWins = GameResults.Count(item => item.WinningAlienUnitType == "None");
        var AlienWins = GameResults.Count - HumanWins;
        var HighestNumb = Math.Max(HumanWins, AlienWins);
        var LowestNumb = Math.Min(HumanWins, AlienWins);
        decimal AlienToHumanRatio = LowestNumb == 0 ? 0 : (decimal) HighestNumb / LowestNumb;

        return new HumanAlienWinRate
        {
            AlienWins = GameResults.Count - HumanWins,
            HumanWins = HumanWins,
            AlienToHumanRatio = Math.Round(AlienToHumanRatio, 2)
        };
    }

    public static Dictionary<string, Models.Models.FinalResultsDto> GetHighestKills (ConcurrentDictionary<string, Models.Models.FinalResultsDto> FinalResults)
    {
        return FinalResults.OrderByDescending(i => i.Value.MechKills + i.Value.AlienKills + i.Value.HumanKills).ToDictionary(
            group => group.Key,
            group => group.Value
        );
    }

    public static Dictionary<string, Models.Models.FinalResultsDto> GetHighestDeaths (ConcurrentDictionary<string, Models.Models.FinalResultsDto> FinalResults)
    {
        return FinalResults.OrderByDescending(i => i.Value.Deaths).ToDictionary(
            group => group.Key,
            group => group.Value
        );
    }
}