using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DiscAnalyzerView.HelperClasses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.LayoutRenderers;
using TextResources = DiscAnalyzerView.Resources.Resources;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DiscAnalyzerView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public App()
        {
            SetupExceptionHandling();

            LayoutRenderer.Register<BuildConfigLayoutRenderer>("buildConfiguration");

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
            _logger = (ILogger)_serviceProvider.GetService(typeof(ILogger<App>));
            ThreadPool.SetMaxThreads(10, 10);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddLogging(logBuilder =>
            {
                logBuilder.ClearProviders();
                logBuilder.SetMinimumLevel(LogLevel.Debug);
                logBuilder.AddNLog("NLog.config");
            });
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                ShowErrorMessage((Exception)e.ExceptionObject);

            DispatcherUnhandledException += (_, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };
            DispatcherUnhandledException += (_, e) =>
                ShowErrorMessage(e.Exception);

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
                ShowErrorMessage(e.Exception);
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = string.Format(TextResources.UnhandledException, source);
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format(TextResources.UnhandledExceptionWithAssumblyData,
                    assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                _logger.LogError(exception, message);
            }
        }

        private void ShowErrorMessage(Exception exception)
        {
            var errorMessage = string.Format(TextResources.ErrorMessage, exception.Message);
            MessageBox.Show(errorMessage, TextResources.MessageBoxHeader,
                MessageBoxButton.OK, MessageBoxImage.Error);

            Current.Shutdown();
        }
    }
}