using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TBG_WPF
{
    /// <summary>
    /// Interaction logic for SuggestionWindow.xaml
    /// </summary>
    public partial class SuggestionWindow : MetroWindow
    {
        private string Tech, Cat, Iss;
        private MainWindow Main;

        public SuggestionWindow()
        {
            this.InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("INSERT INTO Suggestions (Submitter, Suggestion, Technology, Category, Issue) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", SubmitterBox.Text, ChangeBox.Text, Tech, Cat, Iss);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        int rows = command.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            ResponseLabel.Foreground = new SolidColorBrush(Colors.Green);
                            ResponseLabel.Text = "Your suggestion was submitted successfully, you may close this dialog.";
                        }
                        else
                        {
                            ResponseLabel.Foreground = new SolidColorBrush(Colors.Red);
                            ResponseLabel.Text = "Your suggestion was NOT submitted, an error occurred. Please try again.";
                        }
                    }

                    connection.Close();
                }
            } catch (Exception exc)
            {
                ResponseLabel.Foreground = new SolidColorBrush(Colors.Red);
                ResponseLabel.Text = "Your suggestion was NOT submitted, an error occurred. Please try again.";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public SuggestionWindow(MainWindow mw, string Technology, string Category, string Issue)
        {
            this.InitializeComponent();

            Main = mw;

            Tech = Technology;
            Cat = Category;
            Iss = Issue;

            TechnologyLabel.Text = TechnologyLabel.Text + " " + Tech;
            CategoryLabel.Text = CategoryLabel.Text + " " + Cat;
            IssueLabel.Text = IssueLabel.Text + " " + Iss;
        }
    }
}
