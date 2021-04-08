using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.Services;
using SUNTDLite.Services.KBDAPIServiceModels;
using SUNTDLite.View;
using SUNTDLite.ViewModel;

namespace SUNTDLite
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KBDAPIService _kbdService = SingledToolbox.Get<KBDAPIService>();
        private Configuration<MainAppConfigurationModel> _appConfiguration = SingledToolbox.Get<Configuration<MainAppConfigurationModel>>();
        private MainWindowVM mainWindowVM;
        public MainWindow()
        {
            InitializeComponent();
            mainWindowVM = new MainWindowVM();
            DataContext = mainWindowVM;
            
            FillDocumentArguments();
        }

        private void FillDocumentArguments()
        {
            var cardAttributes = _kbdService?.GetCardAttributes();
            if (cardAttributes != null)
            {
                var defaultAttributes = _appConfiguration.GetPropertyValue<List<DefaultAttribute>>("DefaultAttributes");

                List<DocumentType> documentArguments = new List<DocumentType>();
                foreach (var cardAttribute in cardAttributes)
                {
                    var defaultAttribute = defaultAttributes.FirstOrDefault(da => da.AttributeNumber == cardAttribute.AttributeNumber);
                    if (defaultAttribute != null)
                    {
                        switch (cardAttribute.AttributeType)
                        {
                            case AttributeType.String:
                                {
                                    documentArguments.Add(new DocumentString(
                                        string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                        cardAttribute.AttributeNumber) { Value = string.Empty });
                                }
                                break;
                            case AttributeType.Data:
                                {
                                    documentArguments.Add(new DocumentDate(
                                        string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                        cardAttribute.AttributeNumber) { Value = DateTime.Now });
                                }
                                break;
                            case AttributeType.Classificator:
                                {
                                    var classificatorDesc = _kbdService.GetClassificatorDesc(cardAttribute);
                                    if (classificatorDesc != null)
                                    {
                                        documentArguments.Add(new DocumentClassificator(
                                            string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                            cardAttribute.AttributeNumber, 0, classificatorDesc));
                                    }
                                }
                                break;
                            case AttributeType.ClassificatorExp:
                                {
                                    var classificatorExpDesc = _kbdService.GetClassificatorExpDesc(cardAttribute);
                                    if (classificatorExpDesc != null)
                                    {

                                        documentArguments.Add(new DocumentClassificator(
                                            string.IsNullOrEmpty(defaultAttribute.AttributeName) ? cardAttribute.Title : defaultAttribute.AttributeName,
                                            cardAttribute.AttributeNumber, 0, classificatorExpDesc));
                                    }
                                }
                                break;
                        }
                    }
                }
                mainWindowVM.DocumentArguments = documentArguments;
            }
        }
    }
}
