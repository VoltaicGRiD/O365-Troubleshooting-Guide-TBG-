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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TBG_WPF
{
    /// <summary>
    /// Interaction logic for NewCatIssue.xaml
    /// </summary>
    public partial class NewCatIssue : UserControl
    {
        MainWindow mw;
        private string Tech = string.Empty;

        public NewCatIssue()
        {
            InitializeComponent();
        }

        public void Reload(string Technology)
        {
            mw = MainWindow.thisApp;

            Tech = Technology;

            TechnologyLabel.Text = TechnologyLabel.Text + " " + Tech;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = string.Empty;
                if (IssueRadio.IsChecked == true)
                {
                    query = string.Format("INSERT INTO Suggestions (Submitter, Suggestion, Technology, Category) VALUES ('{0}', '{1}', '{2}', '{3}')", SubmitterBox.Text, "Add this new issue: " + SuggestionBox.Text,
                        Tech, CategoryBox.Text);
                }
                else
                {
                    query = string.Format("INSERT INTO Suggestions (Submitter, Suggestion, Technology) VALUES ('{0}', '{1}', '{2}')", SubmitterBox.Text, "Add this new category: " + SuggestionBox.Text,
                        Tech);
                }

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

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
            }
            catch (Exception exc)
            {
                ResponseLabel.Foreground = new SolidColorBrush(Colors.Red);
                ResponseLabel.Text = "Your suggestion was NOT submitted, an error occurred. Please try again.";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            mw.CloseCatIssue();
        }
    }
}
