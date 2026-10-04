using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Internal.ExportImporter;
using Internal.Models;
using Internal.Main;
using Internal.Start;
using System.Collections.Concurrent;
using Microsoft.Win32;

namespace ParasiteZetaAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Models.CombinedDataResults CombinedResults;
        private bool ImportedOnce = false;
        public MainWindow()
        {
            Program.Main(Array.Empty<string>());
            InitializeComponent();
        }

        private ConcurrentDictionary<string, Models.FinalResultsDto> SetUpData ()
        {
            CombinedResults = InternalMain.GetLoadedReplayData(ImportedOnce);
            var ResultData = new ConcurrentDictionary<string, Models.FinalResultsDto>();
            var HashSetHandles = InternalMain.GetApplicationUserHandles();
            foreach (var handle in HashSetHandles)
            {
                ResultData.TryAdd(handle, CombinedResults.FinalResults);
            }
            Clipboard.SetText(AppContext.BaseDirectory);
            return ResultData;
        }

        private void ExportJson_Click (object sender, RoutedEventArgs a) 
        {
            var ResultData = SetUpData();
            ExportImport.ExportAsJson(ResultData, CombinedResults.GameResults);
            MessageBox.Show("Successfully exported json, copied path location has been copied to clipboard.");
        }

        private void ExportCsv_Click (object sender, RoutedEventArgs a) 
        {
            var ResultData = SetUpData();
            ExportImport.ExportAsCsv(ResultData, CombinedResults.GameResults);
            MessageBox.Show("Successfully exported csv, copied path location has been copied to clipboard.");
        }

        private void ExportXml_Click (object sender, RoutedEventArgs a) 
        {
            var ResultData = SetUpData();
            ExportImport.ExportAsXml(ResultData, CombinedResults.GameResults);
            MessageBox.Show("Successfully exported xml, copied path location has been copied to clipboard.");
        }

        private void ImportJson_Click (object sender, RoutedEventArgs a) 
        {
            var dialog = new OpenFileDialog { Title = "Select a JSON file to import." };

            if (dialog.ShowDialog() == true)
            {
                string FilePath = dialog.FileName;
                CombinedResults = ExportImport.ImportAsJson(FilePath);
                ImportedOnce = true;
                MessageBox.Show("Successfully imported json, copied path location has been copied to clipboard.");
            }
        }

        private void ImportCsv_Click (object sender, RoutedEventArgs a) 
        {
            var dialog = new OpenFileDialog { Title = "Select a CSV file to import." };


            if (dialog.ShowDialog() == true)
            {
                string FilePath = dialog.FileName;
                CombinedResults = ExportImport.ImportAsCsv(FilePath);
                ImportedOnce = true;
                MessageBox.Show("Successfully imported csv, copied path location has been copied to clipboard.");
            }
        }

        private void ImportXml_Click (object sender, RoutedEventArgs a) 
        {
            var dialog = new OpenFileDialog { Title = "Select a XML file to import." };


            if (dialog.ShowDialog() == true)
            {
                string FilePath = dialog.FileName;
                CombinedResults = ExportImport.ImportAsXml(FilePath);
                ImportedOnce = true;
                MessageBox.Show("Successfully imported xml, copied path location has been copied to clipboard.");
            }
        }

        private void Discord_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText("discord.gg/bwPx8VVQJ7");
            MessageBox.Show("discord.gg/bwPx8VVQJ7, copied to clipboard.");
        }

        private void BestAlienForms_Click (object sender, RoutedEventArgs a)
        {
            OutputBox.Clear();
            var WinningAlienForms = InternalMain.GetHighestWinningAlienForm();
            foreach (var key in WinningAlienForms)
            {
                OutputBox.AppendText("best alien forms " + key + Environment.NewLine);
            }
        }

        private void PlayerStats_Click (object sender, RoutedEventArgs a)
        {
            CombinedResults = InternalMain.GetLoadedReplayData(ImportedOnce);
            OutputBox.Clear();
            OutputBox.Foreground = Brushes.Black;
            OutputBox.AppendText("Alien Kills: " + CombinedResults.FinalResults.AlienKills + Environment.NewLine);
            OutputBox.AppendText("Human Kills: " + CombinedResults.FinalResults.HumanKills + Environment.NewLine);
            OutputBox.AppendText("Mech Kills: " + CombinedResults.FinalResults.MechKills + Environment.NewLine);
            OutputBox.AppendText("Ties: " + CombinedResults.FinalResults.Ties + Environment.NewLine);
            OutputBox.AppendText("Victories: " + CombinedResults.FinalResults.Victories + Environment.NewLine);
            OutputBox.AppendText("Deaths: " + CombinedResults.FinalResults.Deaths + Environment.NewLine);
            OutputBox.AppendText("Games Played: " + CombinedResults.FinalResults.GamesPlayed + Environment.NewLine);
        }
    }
}