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
using TaskManagementApp.Data;

namespace TaskManagementApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TestDatabaseConnection();
        }

        private void TestDatabaseConnection()
        {
            try
            {
                using var context = new AppDbContext();
                bool canConnect = context.Database.CanConnect();
                if (canConnect)
                    MessageBox.Show("Database Connected Successfully!",
                                  "Success", MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                else
                    MessageBox.Show("Database connection failed!",
                                  "Error", MessageBoxButton.OK,
                                  MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Connection Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}