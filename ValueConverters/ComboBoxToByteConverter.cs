using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Service_Finder.ValueConverters
{
    internal class ComboBoxToByteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte b = (byte)value;   
            return b.ToString();    
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ComboBoxItem item = (ComboBoxItem)value;
            byte tag = byte.Parse((string)item.Tag);
            //(byte)item.Tag;        
            return tag;
        }
    }
}
