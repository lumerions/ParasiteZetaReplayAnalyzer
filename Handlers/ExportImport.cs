using System;
using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Linq;

namespace Internal.ExportImporter;

public class ExportImport
{
    public static void ExportAsXml (ConcurrentDictionary<string, Models.Models.FinalResultsDto> ResultsData)
    {
        XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "     "
        };

        using (XmlWriter writer = XmlWriter.Create("ReplayData.xml", xmlWriterSettings))
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
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
    }

    public static void ExportAsJson (ConcurrentDictionary<string, Models.Models.FinalResultsDto> ResultsData)
    {
        var JsonWriteOptions = new JsonSerializerOptions{ WriteIndented = true };

        try
        {
            string Json = JsonSerializer.Serialize(ResultsData, JsonWriteOptions);
            File.WriteAllText("ReplayData.json", Json);
        } catch (Exception err)
        {
            Console.WriteLine(err);
        }
    }

    public static void ImportAsXml (string XmlFilePath)
    {
        XDocument document = XDocument.Load(XmlFilePath);

        foreach (var item in document.Descendants("data"))
        {
            foreach (var element in item.Elements())
            {
                var VictoryCount = element.Element("v");
                Console.WriteLine($"{VictoryCount}");
            }
        }
    }
}