using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RTSC.Devices.TA2007.View
{
    /// <summary>
    /// Логика взаимодействия для TA2007DataGrid.xaml
    /// </summary>
    public partial class TA2007DataGrid : UserControl
    {
        public TA2007DataGrid()
        {
            InitializeComponent();
        }

        public IEnumerable NewItemsSource
        {
            get => (IEnumerable)GetValue(NewItemsSourceProperty);
            set => SetValue(NewItemsSourceProperty, value);
        }

        public static readonly DependencyProperty NewItemsSourceProperty =
       DependencyProperty.Register("NewItemsSource"
           , typeof(IEnumerable)
           , typeof(TA2007DataGrid)
           , new PropertyMetadata(null, OnItemSourceChanged));

        private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TA2007DataGrid).dataGrid.ItemsSource = (IEnumerable)e.NewValue;
        }
        private void DataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var cell = LogicalTreeHelper.GetParent(e.OriginalSource as DependencyObject) as DataGridCell;

            if (cell != null)
            {
                switch (cell.Column.Header)
                {
                    case "Норма":
                        Mouse.OverrideCursor = Cursors.Pen;
                        break;
                    case "Допуск":
                        Mouse.OverrideCursor = Cursors.Pen;
                        break;
                    default:
                        Mouse.OverrideCursor = Cursors.Arrow;
                        break;
                }
            }
            else
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        private void ToleranceInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void NormInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9\-]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
