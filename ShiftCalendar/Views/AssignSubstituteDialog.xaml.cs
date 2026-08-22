using ShiftCalendar.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShiftCalendar.Views
{
    public partial class AssignSubstituteDialog : Window
    {
        private readonly ShiftDbContext _context;
        private readonly DateTime _date;
        private readonly int _originalEmployeeId;
        private readonly int _shiftTeamId;
        public int? SelectedSubstituteId { get; private set; }

        public AssignSubstituteDialog(ShiftDbContext context, DateTime date, int originalEmployeeId, int shiftTeamId)
        {
            InitializeComponent();
            _context = context;
            _date = date;
            _originalEmployeeId = originalEmployeeId;
            _shiftTeamId = shiftTeamId;

            DateText.Text = date.ToString("dd.MM.yyyy");

            // Загружаем все смены в ComboBox
            var teams = _context.ShiftTeams.ToList();
            TeamSelector.ItemsSource = teams.Select(t => t.Name).ToList();

            // Выбираем текущую смену
            var currentTeamIndex = teams.FindIndex(t => t.Id == shiftTeamId);
            if (currentTeamIndex >= 0)
            {
                TeamSelector.SelectedIndex = currentTeamIndex;
            }
        }

        private void TeamSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamSelector.SelectedIndex < 0) return;

            var teams = _context.ShiftTeams.ToList();
            var selectedTeam = teams[TeamSelector.SelectedIndex];

            // Загружаем сотрудников выбранной смены, кроме отсутствующего
            var employees = _context.Employees
                .Where(emp => emp.ShiftTeamId == selectedTeam.Id && emp.Id != _originalEmployeeId)
                .Select(emp => emp.FullName)
                .ToList();

            EmployeesList.ItemsSource = employees;

            if (employees.Any())
            {
                EmployeesList.SelectedIndex = 0;
            }
        }

        private void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesList.SelectedItem is string selectedName)
            {
                var employee = _context.Employees.FirstOrDefault(emp => emp.FullName == selectedName);
                if (employee != null)
                {
                    SelectedSubstituteId = employee.Id;
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Выберите сотрудника из списка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}