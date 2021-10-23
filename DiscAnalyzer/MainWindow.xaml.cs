using System.Windows.Controls.Ribbon;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new ApplicationViewModel(Tree);
        }
    }
}
