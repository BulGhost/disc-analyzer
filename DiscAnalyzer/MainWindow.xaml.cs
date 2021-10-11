using System.Windows.Controls.Ribbon;
using DiscAnalyzer.ViewModels;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new DirectoryStructureViewModel();
        }
    }
}
