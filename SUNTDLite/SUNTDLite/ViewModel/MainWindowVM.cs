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
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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
                if (value == null)
                {
                    LOG_ERROR("value is null");
                    throw new ArgumentException("document arguments can't be null");
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("DocArgument values:");
                foreach (var item in value)
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
                    if (string.IsNullOrEmpty(_documentFilePath))
                    {
                        _documentFileInfo = null;
                    }
                    else
                    {
                        _documentFileInfo = new FileInfo(_documentFilePath);
                    }
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

        private Visibility _attributesLoadingVisibility = Visibility.Visible;
        public Visibility AttributesLoadingVisibility { 
            get => _attributesLoadingVisibility;
            set 
            {
                if (value != _attributesLoadingVisibility)
                {
                    _attributesLoadingVisibility = value;
                    OnPropertyChanged("AttributesLoadingVisibility");
                }
            } 
        }
        
        private Visibility _attributeGridVisibility = Visibility.Collapsed;
        public Visibility AttributeGridVisibility
        {
            get => _attributeGridVisibility;
            set
            {
                if (value != _attributeGridVisibility)
                {
                    _attributeGridVisibility = value;
                    OnPropertyChanged("AttributeGridVisibility");
                }
            }
        }

        private Visibility _searchLoadingVisibility = Visibility.Collapsed;
        public Visibility SearchLoadingVisibility
        {
            get => _searchLoadingVisibility;
            set
            {
                if (value != _searchLoadingVisibility)
                {
                    _searchLoadingVisibility = value;
                    OnPropertyChanged("SearchLoadingVisibility");
                }
            }
        }

        private string _searchResultString = string.Empty;
        public string SearchResultString
        {
            get => _searchResultString;
            set
            {
                _searchResultString = value;
                OnPropertyChanged("SearchResultString");
            }
        }
        
        private Visibility _searchGridVisibility = Visibility.Visible;
        public Visibility SearchGridVisibility
        {
            get => _searchGridVisibility;
            set
            {
                if (value != _searchGridVisibility)
                {
                    _searchGridVisibility = value;
                    OnPropertyChanged("SearchGridVisibility");
                }
            }
        }

        private bool _buttonSaveDocumentEnable = true;
        public bool ButtonSaveDocumentEnable
        {
            get => _buttonSaveDocumentEnable;
            set
            {
                if (_buttonSaveDocumentEnable != value)
                {
                    _buttonSaveDocumentEnable = value;
                    OnPropertyChanged("ButtonSaveDocumentEnable");
                }
            }
        }
        private void ClearArgumentsValues()
        {
            if (_documentArgumentsList != null && _documentArgumentsList.Any())
            {
                foreach (var daItem in _documentArgumentsList)
                {
                    daItem.Clean();
                }
            }
        }
        public class DocumentListEntry
        {
            public string DocumentName { get; set; }
            public long DocumentUid { get; set; }
        }

        private ObservableCollection<DocumentListEntry> _documentListEntries = new ObservableCollection<DocumentListEntry>();
        public ObservableCollection<DocumentListEntry> DocumentSearchListEntries
        {
            get => _documentListEntries;
            set
            {
                _documentListEntries = value;
                OnPropertyChanged("DocumentSearchListEntries");                
            }
        }
        public MainWindowVM() : base("[MainWindowVM]")
        {
            _configuration = SingledToolbox.Get<Configuration<MainAppConfigurationModel>>();
            _appCommands = SingledToolbox.Get<ApplicationCommands>();
            Task loadAttributesTask = new Task(() =>
            {
                DocumentArguments = _appCommands.GetAttributesCommand();
                AttributesLoadingVisibility = Visibility.Collapsed;
                AttributeGridVisibility = Visibility.Visible;
            });
            loadAttributesTask.Start();
        }

        private Configuration<MainAppConfigurationModel> _configuration;

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
                        long uid = -1;
                        try
                        {
                            uid = (long)args;
                        }
                        catch (InvalidCastException icEx)
                        {
                            LOG_ERROR("Ошибка в команде удаления документа, не могу привести uid документа к типу long.", icEx);
                        }
                        var result = MessageBox.Show($"Вы точно хотите удалить выбранный документ?", "Удаление документа", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            LOG_TRACE($"DeleteDocumentCommand. {uid}");
                            string successed = _appCommands.DeleteDocumentCommand(uid);
                            if (!string.IsNullOrEmpty(successed))
                            {
                                MessageBox.Show($"Запрос на удаление документа {uid} создан.\n {successed}");
                            }
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
                            ButtonSaveDocumentEnable = false;
                            if (_documentArgumentsList != null && _documentArgumentsList.Count > 0)
                            {
                                List<KBDAPIService.CreateCardAttribute> createCardAttributes = new List<KBDAPIService.CreateCardAttribute>();
                                foreach (var daItem in _documentArgumentsList)
                                {
                                    var strings = daItem.GetValueString();
                                    if (strings != null)
                                    {
                                        foreach (var str in strings)
                                        {
                                            createCardAttributes.Add(new KBDAPIService.CreateCardAttribute(daItem.DocumentAttributeNumber, str));
                                        }
                                    }
                                }

                                DocsSoapService.DocumentDesc documentDesc = _appCommands.SaveDocumentCommand(createCardAttributes, _documentFileInfo);
                                if (documentDesc != null)
                                {
                                    MessageBox.Show($"Документ {documentDesc.oid}, {documentDesc.name} успешно сохранен", "Сохранение документа");
                                    ClearArgumentsValues();
                                    DocumentFilePath = string.Empty;
                                }
                                else
                                {
                                    MessageBox.Show($"Сервер не вернул информацию о сохранении документа, файл не был отправлен!", "Сохранение документа",
                                        MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                                }
                            }
                            ButtonSaveDocumentEnable = true;
                        },
                        isEnabledArg =>
                        {
                            if (_daDocumentName == null || string.IsNullOrEmpty(_daDocumentName.Value))
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
                            !(_documentFileInfo.Exists && (_documentFileInfo.Extension.ToLower() == ".pdf" ||
                            _documentFileInfo.Extension.ToLower() == ".doc" || 
                            _documentFileInfo.Extension.ToLower() == ".docx")))
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
                            SearchResultString = "Поиск...";
                            SearchLoadingVisibility = Visibility.Visible;
                            SearchGridVisibility = Visibility.Collapsed;
                            new Task(() =>
                            {
                                DocumentSearchListEntries = new ObservableCollection<DocumentListEntry>();
                                IEnumerable<Tuple<string, long>> documents = _appCommands.SearchDocumentsCommand(_documentSearch);
                                if (documents != null)
                                {
                                    var documentsListEntries = documents.Select(d => new DocumentListEntry() { DocumentName = d.Item1, DocumentUid = d.Item2 }).ToList();
                                    DocumentSearchListEntries.Clear();
                                    foreach (var item in documentsListEntries)
                                    {
                                        DocumentSearchListEntries.Add(item);
                                    }
                                    SearchLoadingVisibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    //MessageBox.Show("Документов не найдено.");
                                    SearchResultString = "Документов не найдено";
                                    SearchLoadingVisibility = Visibility.Visible;
                                }
                                
                                SearchGridVisibility = Visibility.Visible;
                            }).Start();                            
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
