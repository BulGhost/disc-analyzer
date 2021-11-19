using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;

namespace DiscAnalyzerView.HelperClasses
{
    [LayoutRenderer("buildConfiguration")]
    [ThreadAgnostic]
    public class BuildConfigLayoutRenderer : LayoutRenderer
    {
        private string _buildСonfig;

        private string GetBuildConfig()
        {
            if (_buildСonfig != null)
            {
                return _buildСonfig;
            }

#if DEBUG
            _buildСonfig = "Debug";
#else
            _buildСonfig = "Release";
#endif
            return _buildСonfig;
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(GetBuildConfig());
        }
    }
}
