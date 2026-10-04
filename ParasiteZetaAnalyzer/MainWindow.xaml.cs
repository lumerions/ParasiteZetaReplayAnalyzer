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

namespace ParasiteZetaAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Models.CombinedDataResults CombinedResults;

        public MainWindow()
        {
            InitializeComponent();
            Program.Main(Array.Empty<string>());
            CombinedResults = InternalMain.GetLoadedReplayData();
        }

        private void ExportJson_Click (object sender, RoutedEventArgs a) 
        {
            var ResultData = new ConcurrentDictionary<string, Models.FinalResultsDto>();
            ResultData.TryAdd("", CombinedResults.FinalResults);
            ExportImport.ExportAsJson(ResultData, CombinedResults.GameResults);
            Clipboard.SetText(AppContext.BaseDirectory);
            MessageBox.Show("Successfully exported json, copied path location has been copied to clipboard.");
        }

        private void ExportCsv_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText(AppContext.BaseDirectory);
            MessageBox.Show("Successfully exported csv, copied path location has been copied to clipboard.");
        }

        private void ExportXml_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText(AppContext.BaseDirectory);
            MessageBox.Show("Successfully exported xml, copied path location has been copied to clipboard.");
        }

        private void ImportJson_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText(AppContext.BaseDirectory);
            MessageBox.Show("Successfully imported json, copied path location has been copied to clipboard.");
        }

        private void ImportCsv_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText(AppContext.BaseDirectory);
            MessageBox.Show("Successfully imported csv, copied path location has been copied to clipboard.");
        }

        private void ImportXml_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText(AppContext.BaseDirectory);
            MessageBox.Show("Successfully imported xml, copied path location has been copied to clipboard.");
        }

        private void Discord_Click (object sender, RoutedEventArgs a) 
        {
            Clipboard.SetText("discord.gg/bwPx8VVQJ7");
            MessageBox.Show("discord.gg/bwPx8VVQJ7, copied to clipboard.");
        }
    }
}