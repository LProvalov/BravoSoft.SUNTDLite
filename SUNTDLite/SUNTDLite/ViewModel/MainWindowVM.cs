using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Microsoft.Win32;
using System.Windows;
using System.IO;
using System.Windows.Input;
using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.View;
using SUNTDLite.Services;

namespace SUNTDLite.ViewModel
{
    public class MainWindowVM : BaseVM
    {
        private List<DocumentType> _documentArgumentsList = new List<DocumentType>();
        public IList<DocumentType> DocumentArguments
        {
            get => _documentArgumentsList;
            set
            {
                LOG_TRACE($"DocumentArguments changed");
                if(value == null)
                {
                    LOG_ERROR("value is null");
                    throw new ArgumentException("document arguments can't be null");
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("DocArgument values:");
                foreach(var item in value)
                {
                    sb.Append(item.Name).Append("/").Append(item.DocumentAttributeNumber).Append("\t");
                }
                LOG_TRACE(sb.ToString());

                _documentArgumentsList.Clear();
                _documentArgumentsList.AddRange(value);

                var daDocumentName = _documentArgumentsList.FirstOrDefault(da => da.Name.Equals("Наименование"));
                if (daDocumentName != null)
                {
                    _daDocumentName = (DocumentString)daDocumentName;
                    daDocumentName.OnNameChanged += () =>
                    {
                        CommandManager.InvalidateRequerySuggested();
                    };
                }

                OnPropertyChanged("DocumentArguments");
            }
        }
        private DocumentString _daDocumentName = null;
        private ApplicationCommands _appCommands = null;

        private FileInfo _documentFileInfo = null;
        private string _documentFilePath = string.Empty;
        public string DocumentFilePath
        {
            get => _documentFilePath;
            set
            {
                LOG_TRACE($"DocumentFilePath changed. {value}");
                if (_documentFilePath != value)
                {
                    _documentFilePath = value;
                    _documentFileInfo = new FileInfo(_documentFilePath);
                    OnPropertyChanged("DocumentFilePath");
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _documentSearch = string.Empty;
        public string DocumentSearch
        {
            get => _documentSearch;
            set
            {
                LOG_TRACE($"DocumentSearch changed.");
                _documentSearch = value;
                if (!string.IsNullOrEmpty(_documentSearch))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
                OnPropertyChanged("DocumentSearch");
            }
        }

        public class DocumentListEntry
        {
            public string DocumentName { get; set; }
            public long DocumentUid { get; set; }
        }

        private List<DocumentListEntry> _documentListEntries = new List<DocumentListEntry>();
        public List<DocumentListEntry> DocumentSearchListEntries
        {
            get => _documentListEntries;
            set
            {
                _documentListEntries = value;
                OnPropertyChanged("DocumentSearchListEntries");
            }
        }
        public MainWindowVM() : base ("[MainWindowVM]")
        {
            _configuration = SingledToolbox.Get<Configuration<MainAppConfigurationModel>>();
            _appCommands = SingledToolbox.Get<ApplicationCommands>();
        }

        private Configuration<MainAppConfigurationModel> _configuration;
        //private KBDAPIService kbdService = SingledToolbox.Get<KBDAPIService>();
        //private FilesWorkingService filesWorkingService = SingledToolbox.Get<FilesWorkingService>();

        private Command _selectDocumentFileCommand;
        public Command SelectDocumentFileCommand
        {
            get
            {
                return _selectDocumentFileCommand ??
                    (_selectDocumentFileCommand = new Command(obj =>
                   {
                       LOG_TRACE($"SelectDocumentFileCommand.");
                       OpenFileDialog openFileDialog = new OpenFileDialog();
                       openFileDialog.Filter = "MS Documents (*.doc;*.docx)|*.doc;*.docx|PDF Documents (*.pdf)|*pdf| All files (*.*)|*.*";
                       openFileDialog.FilterIndex = 3;
                       string initialDirectory = _configuration.GetPropertyValue<string>("DocumentSearchFolder");
                       if (!string.IsNullOrEmpty(initialDirectory))
                       {
                           openFileDialog.InitialDirectory = initialDirectory;
                       }

                       if (openFileDialog.ShowDialog() == true)
                       {
                           FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                           _configuration.SetPropertyValue("DocumentSearchFolder", fileInfo.DirectoryName);
                           DocumentFilePath = openFileDialog.FileName;
                       }
                   }));
            }
        }

        private Command _deleteDocumentCommand;
        public Command DeleteDocumentCommand
        {
            get
            {
                return _deleteDocumentCommand ??
                    (_deleteDocumentCommand = new Command(args =>
                    {
                        var uid = args as string;
                        LOG_TRACE($"DeleteDocumentCommand. {uid}");
                        bool successed = _appCommands.DeleteDocumentCommand(uid);
                        if (successed)
                        {
                            MessageBox.Show($"Документ {uid} успешно удалён");
                        }
                    }));
            }
        }

        private Command _saveDocumentCommand;
        public Command SaveDocumentCommand
        {
            get
            {
                return _saveDocumentCommand ??
                    (_saveDocumentCommand = new Command(
                        obj =>
                        {
                            LOG_TRACE($"SaveDocumentCommand.");
                            var createCardAttributes = _documentArgumentsList.Where(da => !string.IsNullOrEmpty(da.GetValueString()))
                                .Select(da => new KBDAPIService.CreateCardAttribute(da.DocumentAttributeNumber, da.GetValueString())).ToList();
                            DocsSoapService.DocumentDesc documentDesc = _appCommands.SaveDocumentCommand(createCardAttributes, _documentFileInfo);
                            if (documentDesc != null)
                            {
                                MessageBox.Show($"Документ {documentDesc.oid}, {documentDesc.name} успешно сохранен", "Сохранение документа");
                            }
                            else
                            {
                                MessageBox.Show($"Сервер не вернул информацию о сохранении документа, файл не был отправлен!", "Сохранение документа", 
                                    MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                            }
                        },
                        isEnabledArg =>
                        {
                            if (string.IsNullOrEmpty(DocumentFilePath) ||
                            !File.Exists(DocumentFilePath) ||
                            _daDocumentName == null || string.IsNullOrEmpty(_daDocumentName.Value))
                            {
                                return false;
                            }
                            return true;
                        }));
            }
        }

        private Command _makeRefsOnDocumentCommand;
        public Command MakeRefsOnDocumentCommand
        {
            get
            {
                return _makeRefsOnDocumentCommand ??
                    (_makeRefsOnDocumentCommand = new Command(
                        obj =>
                        {
                            LOG_TRACE($"MakeRefsOnDocumentCommand.");
                            if (_documentFileInfo != null)
                            {
                                DocumentFilePath = _appCommands.MakeRefsOnDocumentCommand(_documentFileInfo);
                            }
                        },
                        isEnableArgs =>
                        {
                            // TODO: implement checking file type and ability to open editor
                            if (string.IsNullOrEmpty(DocumentFilePath) ||
                            !(_documentFileInfo.Exists && (_documentFileInfo.Extension == ".pdf" || _documentFileInfo.Extension == ".doc" || _documentFileInfo.Extension == ".docx")))
                            {
                                return false;
                            }
                            return true;
                        }));
            }
        }

        private Command _searchDocumentsCommand;
        public Command SearchDocummentsCommand
        {
            get
            {
                return _searchDocumentsCommand ??
                    (_searchDocumentsCommand = new Command(
                        obj => {
                            DocumentSearchListEntries.Clear();
                            IEnumerable<Tuple<string, long>> documents = _appCommands.SearchDocumentsCommand(_documentSearch);
                            if (documents != null)
                            {
                                var documentsListEntries = documents.Select(d => new DocumentListEntry() { DocumentName = d.Item1, DocumentUid = d.Item2 }).ToList();
                                DocumentSearchListEntries = documentsListEntries;
                            }
                        },
                        isEnableArgs =>
                        {
                            if (!string.IsNullOrEmpty(DocumentSearch))
                            {
                                return true;
                            }
                            return false;
                        }));
            }
        }
    }
}
