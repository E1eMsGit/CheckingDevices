using System.Collections;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSC.Devices.TA2006.View
{
    public partial class TA2006DataGrid : UserControl
    {
        public TA2006DataGrid()
        {
            InitializeComponent();
        }

        public IEnumerable NewItemsSource
        {
            get => (IEnumerable)GetValue(NewItemsSourceProperty);
            set => SetValue(NewItemsSourceProperty, value);
        }
        private void DataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var cell = LogicalTreeHelper.GetParent(e.OriginalSource as DependencyObject) as DataGridCell;

            if (cell != null)
            {
                switch (cell.Column.Header)
                {
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
       
        public static readonly DependencyProperty NewItemsSourceProperty =
            DependencyProperty.Register(
                "NewItemsSource",
                typeof(IEnumerable),
                typeof(TA2006DataGrid),
                new PropertyMetadata(null, OnItemSourceChanged));

        private static void OnItemSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TA2006DataGrid).dataGrid.ItemsSource = (IEnumerable)e.NewValue;
        }
       
        private void TolerancePreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!short.TryParse(e.Text, out short val))
            {
                e.Handled = true;
            }
        }
        private void TolerancePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}
