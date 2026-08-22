using ShiftCalendar.ViewModels;
using ShiftCalendar.Models;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShiftCalendar
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollToCurrentDay();
        }

        private void ScrollToCurrentDay()
        {
            if (DataContext is MainViewModel vm && vm.CurrentMonthView != null)
            {
                int currentDay = vm.CurrentMonthView.CurrentDay;
                if (currentDay > 0)
                {
                    double offset = 90 + (currentDay - 1) * 26 - (CalendarScrollViewer.ActualWidth / 2);
                    if (offset > 0)
                    {
                        CalendarScrollViewer.ScrollToHorizontalOffset(offset);
                    }
                }
            }
        }

        private void Day_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is DayCellViewModel cell)
            {
                if (cell.Day.HasValue)
                {
                    var viewModel = DataContext as MainViewModel;
                    
                    // Если это строка смены (не замещающего), открываем диалог управления отсутствием
                    if (cell.ParentRow != null && !cell.ParentRow.IsSubstituteRow)
                    {
                        viewModel?.ManageAbsenceCommand.Execute(null);
                    }
                    else
                    {
                        // Для замещающих или других случаев - редактирование смены
                        viewModel?.EditShiftCommand.Execute(cell);
                    }
                }
            }
        }

        private void TeamHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is ShiftTeamViewModel team)
            {
                team.ToggleExpandCommand.Execute(null);
            }
        }

        private void EditShift_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is DayCellViewModel cell)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.EditShiftCommand.Execute(cell);
            }
        }

        private void ManageAbsence_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.ManageAbsenceCommand.Execute(null);
        }

        private void AssignSubstitute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is DayCellViewModel cell)
            {
                var viewModel = DataContext as MainViewModel;
                viewModel?.AssignSubstituteCommand.Execute(cell);
            }
        }

        // Обработчик наведения мыши на ячейку дня
        private void Day_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is DayCellViewModel cell && cell.Day.HasValue)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel?.CurrentMonthView == null) return;

                var date = new DateTime(viewModel.CurrentMonthView.Year, viewModel.CurrentMonthView.Month, cell.Day.Value);

                // Получаем всех сотрудников этой смены
                var team = viewModel.CurrentMonthView.ShiftRows
                    .FirstOrDefault(r => r.ShiftTeamId == cell.ParentRow?.ShiftTeamId);

                if (team == null) return;

                var sb = new StringBuilder();
                sb.AppendLine($"{cell.Day} {viewModel.CurrentMonthView.MonthName}");
                sb.AppendLine(new string('─', 25));

                foreach (var dayCell in team.Cells.Where(c => c.Day == cell.Day))
                {
                    var employee = viewModel.GetEmployeeById(dayCell.EmployeeId);
                    if (employee == null) continue;

                    string statusIcon = dayCell.EmployeeStatus switch
                    {
                        AbsenceType.НаСмене => "✅",
                        AbsenceType.Отпуск => "🏖️",
                        AbsenceType.Больничный => "",
                        AbsenceType.Отгул => "😴",
                        _ => ""
                    };

                    string substituteInfo = dayCell.HasSubstitute
                        ? $" (зам: {dayCell.SubstituteName})"
                        : "";

                    sb.AppendLine($"{statusIcon} {employee.FullName}{substituteInfo}");
                }

                border.ToolTip = sb.ToString();
            }
        }
    }
}