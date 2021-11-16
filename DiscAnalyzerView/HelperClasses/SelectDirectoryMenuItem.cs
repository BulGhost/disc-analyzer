using System.Windows.Input;

namespace DiscAnalyzerView.HelperClasses
{
    public class SelectDirectoryMenuItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public ICommand Command { get; set; }
        public object CommandParameter { get; set; }
    }
}
