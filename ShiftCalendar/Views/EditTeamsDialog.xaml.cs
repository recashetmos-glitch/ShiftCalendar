using Microsoft.EntityFrameworkCore;
using ShiftCalendar.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ShiftCalendar.Views
{
    public partial class EditTeamsDialog : Window
    {
        private readonly ShiftDbContext _context;
        private List<TeamEditModel> _teams;

        public EditTeamsDialog(ShiftDbContext context)
        {
            InitializeComponent();
            _context = context;
            LoadTeams();
        }

        private void LoadTeams()
        {
            _teams = _context.ShiftTeams
                .Include(t => t.Employees)
                .Select(t => new TeamEditModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    CycleOffset = t.CycleOffset,
                    Employees = t.Employees.Select(e => new EmployeeEditModel { Id = e.Id, FullName = e.FullName }).ToList()
                })
                .ToList();

            TeamsList.ItemsSource = _teams;
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TeamEditModel team)
            {
                team.Employees.Add(new EmployeeEditModel { FullName = "Новый сотрудник" });
                TeamsList.Items.Refresh();
            }
        }

        private void RemoveEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is EmployeeEditModel employee)
            {
                foreach (var team in _teams)
                {
                    team.Employees.Remove(employee);
                }
                TeamsList.Items.Refresh();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var teamModel in _teams)
            {
                var team = _context.ShiftTeams.Find(teamModel.Id);
                if (team != null)
                {
                    team.Name = teamModel.Name;

                    // Удаляем старых сотрудников
                    var oldEmployees = _context.Employees.Where(e => e.ShiftTeamId == team.Id).ToList();
                    _context.Employees.RemoveRange(oldEmployees);

                    // Добавляем новых
                    foreach (var empModel in teamModel.Employees)
                    {
                        if (string.IsNullOrWhiteSpace(empModel.FullName)) continue;

                        _context.Employees.Add(new Employee
                        {
                            FullName = empModel.FullName,
                            ShiftTeamId = team.Id
                        });
                    }
                }
            }

            _context.SaveChanges();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class TeamEditModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CycleOffset { get; set; }
        public List<EmployeeEditModel> Employees { get; set; } = new();
    }

    public class EmployeeEditModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
    }
}