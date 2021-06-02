using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace SUNTDLite
{
    public class StarWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListView listview = value as ListView;
            double listViewWidth = listview.Width;
            if (Double.IsNaN(listViewWidth))
            {
                listViewWidth = listview.ActualWidth;
            }

            GridView gv = listview.View as GridView;
            for (int i = 0; i < gv.Columns.Count; i++)
            {
                if (!Double.IsNaN(gv.Columns[i].Width))
                {
                    listViewWidth -= gv.Columns[i].Width;
                }
            }
            return listViewWidth - 10;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
