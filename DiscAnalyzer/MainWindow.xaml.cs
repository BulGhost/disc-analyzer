using System.Windows.Controls.Ribbon;
using Aga.Controls.Tree;
using DiscAnalyzer.ViewModels;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public ApplicationViewModel ViewModel { get; set; } = new();

        public TreeList TreeList => Tree;
    }
}
