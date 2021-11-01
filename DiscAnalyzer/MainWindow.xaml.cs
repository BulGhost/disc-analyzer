using System.Collections.Generic;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;

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
