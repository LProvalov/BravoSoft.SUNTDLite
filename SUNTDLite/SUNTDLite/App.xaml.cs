using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.Text;

namespace SUNTDLite
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NLog.Logger _logger = NLog.LogManager.GetLogger("MainLogger");
        private readonly string LogTag = "[App]";
        private Configuration<MainAppConfigurationModel> _AppConfiguration;
        private KBDAPIService _KBDAPIService;
        private FilesWorkingService _filesWorkingService;
        private ApplicationCommands _appCommands;

        private void RecursiveExpandException(StringBuilder sb, Exception ex)
        {
            if (ex != null)
            {
                sb.AppendLine(ex.Message);
                if (ex.InnerException != null)
                {
                    RecursiveExpandException(sb, ex.InnerException);
                }
            }
        }
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Произошла ошибка.");
            RecursiveExpandException(sb, e.Exception);
            _logger.Error($"{LogTag} {sb.ToString()}");
            MessageBox.Show(sb.ToString(), "Описание ошибки", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            _logger.Trace($"{LogTag} OnStartup");
            _AppConfiguration = new ApplicationConfiguration.Configuration<MainAppConfigurationModel>("AppConfiguration.cfg");
            _AppConfiguration.LoadConfiguration();

            SingledToolbox.AddTool(typeof(Configuration<MainAppConfigurationModel>), _AppConfiguration);
            
            var kbdServiceConfig = _AppConfiguration.GetPropertyValue<KBDAPIServiceConfig>("ServiceConfig");
            var searchServiceConfig = _AppConfiguration.GetPropertyValue<SearchServiceConfig>("SearchServiceConfig");
            _KBDAPIService = new KBDAPIService(kbdServiceConfig, searchServiceConfig);
            try
            {
                _KBDAPIService.OpenClients();
                SingledToolbox.AddTool(typeof(KBDAPIService), _KBDAPIService);
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogTag} Exception in KBDAPI: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"{LogTag} InnerEx:{ex.InnerException.Message}");
                }
                MessageBox.Show(ex.Message, "Exception in KBDAPI Service");
                base.Shutdown();
            }

            try
            {
                string tempDirPath = _AppConfiguration.GetPropertyValue<string>("TempDirectoryPath");
                if (string.IsNullOrEmpty(tempDirPath))
                {
                    var currentTempDir = new DirectoryInfo(Directory.GetCurrentDirectory()).CreateSubdirectory("TempAttachments");
                    tempDirPath = currentTempDir.FullName;
                }
                var applicationsForEditingDocuments = _AppConfiguration.GetPropertyValue<ApplicationsForEditingDocuments>("ApplicationsForEditingDocuments");
                _filesWorkingService = new FilesWorkingService(tempDirPath, applicationsForEditingDocuments.UseDefault);
                if (!string.IsNullOrEmpty(applicationsForEditingDocuments.DocumentEditorPath) && File.Exists(applicationsForEditingDocuments.DocumentEditorPath))
                {
                    _filesWorkingService.DocumentEditorPath = applicationsForEditingDocuments.DocumentEditorPath;
                }
                if (!string.IsNullOrEmpty(applicationsForEditingDocuments.PDFDocumentEditorPath) && File.Exists(applicationsForEditingDocuments.PDFDocumentEditorPath))
                {
                    _filesWorkingService.DocumentEditorPath = applicationsForEditingDocuments.PDFDocumentEditorPath;
                }
                SingledToolbox.AddTool(typeof(FilesWorkingService), _filesWorkingService);
            }
            catch (Exception ex)
            {
                _logger.Error($"{LogTag} Exception in FilesWorking Service: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.Error($"{LogTag} InnerEx:{ex.InnerException.Message}");
                }
                MessageBox.Show(ex.Message, "Exception in Files Working Service");
                base.Shutdown();
            }

            _appCommands = new ApplicationCommands();
            SingledToolbox.AddTool(typeof(ApplicationCommands), _appCommands);

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _KBDAPIService.CloseClients();
            base.OnExit(e);
        }
    }
}
