using Microsoft.EntityFrameworkCore;
using Storage.Core.Context;
using Storage.Core.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Storage.UI
{
    /// <summary>
    /// Interaction logic for LoginForm.xaml
    /// </summary>
    public partial class LoginForm : Window
    {
        private readonly StorageContext dbContext;

        public LoginForm()
        {
            InitializeComponent();
            dbContext = new StorageContext();
        }

        private async Task<User> GetByLoginAsync(string login)
        {
            if (dbContext == null)
            {
                MessageBox.Show("dbContext is null.");
                return null;
            }

            var users = await dbContext.Users.Where(u => u.Login == login).ToListAsync();

            if (users == null || users.Count == 0)
            {
                MessageBox.Show($"User with login '{login}' not found.");
                return null;
            }

            return users[0];
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Login_ClickAsync(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text;
            string password = txtPassword.Password;

            var user = await GetByLoginAsync(login);

            if (user != null && user.Password == password)
            {
                if (user.IsAdmin)
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                }
                else
                {
                    ManagerForm managerForm = new ManagerForm();
                    managerForm.Show();
                }

                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid login or password. Please try again.");
            }
        }
    }
}
