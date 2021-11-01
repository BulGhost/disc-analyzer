using AsyncAwaitBestPractices.MVVM;

namespace DiscAnalyzer.HelperClasses
{
    public class MenuItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public IAsyncCommand Command { get; set; }
    }
}
