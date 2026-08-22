using ShiftCalendar.Models;
using System.Windows;
using System.Windows.Controls;

namespace ShiftCalendar.Views
{
    public partial class EditShiftDialog : Window
    {
        public ShiftType SelectedShift { get; private set; }

        public EditShiftDialog(ShiftType currentShift)
        {
            InitializeComponent();
            SelectedShift = currentShift;

            var grid = this.Content as Grid;
            if (grid != null)
            {
                foreach (UIElement child in grid.Children)
                {
                    if (child is StackPanel stackPanel)
                    {
                        foreach (UIElement element in stackPanel.Children)
                        {
                            if (element is RadioButton rb && rb.Tag?.ToString() == currentShift.ToString())
                            {
                                rb.IsChecked = true;
                            }
                        }
                    }
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var grid = this.Content as Grid;
            if (grid != null)
            {
                foreach (UIElement child in grid.Children)
                {
                    if (child is StackPanel stackPanel)
                    {
                        foreach (UIElement element in stackPanel.Children)
                        {
                            if (element is RadioButton rb && rb.IsChecked == true)
                            {
                                SelectedShift = (ShiftType)System.Enum.Parse(typeof(ShiftType), rb.Tag.ToString());
                                DialogResult = true;
                                Close();
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}