using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
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

namespace TBG_WPF
{
    /// <summary>
    /// Interaction logic for xaml
    /// </summary>
    public partial class QueuePage : Page
    {
        string token;
        string groupToken;
        QueueManagement Queue;
        string caseSelected;

        public QueuePage(string _token, string _groupToken, QueueManagement _queue)
        {
            token = _token;
            groupToken = _groupToken;
            Queue = _queue;

            InitializeComponent();
        }

        private void OpenCaseButtonClick(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;
            string link = origin.CommandParameter.ToString();

            Process.Start(link);
        }

        private void TakeCaseButton_Click(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;
            MessageBoxResult result = MessageBox.Show("Are you sure you want to take case # " + origin.CommandParameter.ToString() + "?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.None);

            if (result == MessageBoxResult.Yes)
            {
                List<string> entries = token.Split(',').ToList();
                string DisplayName = string.Empty;

                foreach (string s in entries)
                {
                    if (s.Contains("displayName"))
                    {
                        DisplayName = s.Split(':')[1].Trim('"');
                    }
                }

                Debug.WriteLine(DisplayName);

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("UPDATE Cases SET TakenBy='{0}', TakenAt='{1}' WHERE CaseID='{2}'", DisplayName, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), origin.CommandParameter.ToString());

                    //string query2 = string.Format("INSERT INTO OwnedCases (Created, CaseID, Severity, Queue, Owner) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", originSelectedDate, UnlistedCaseNumber.Text.Trim(), UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), UnlistedCaseQueue.Text, DisplayName);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    //using (SqlCommand command = new SqlCommand(query2, connection))
                    //{
                    //    command.ExecuteNonQuery();
                    //}

                    connection.Close();
                }

                CasesDataGrid.ItemsSource = null;
                CasesDataGrid.ItemsSource = Queue.GetCases(token, groupToken);
            }
        }

        private void QueueNewCaseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thisApp.NewCaseFlyout.IsOpen = !MainWindow.thisApp.NewCaseFlyout.IsOpen;

            MainWindow.thisApp.NewCaseNumber.Text = "";
            MainWindow.thisApp.NewCaseSeverity.SelectedIndex = 1;
            MainWindow.thisApp.NewCaseDescription.Text = "";
            MainWindow.thisApp.NewCaseQueue.SelectedIndex = 0;
        }
        
        private void QueueReportsButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thisApp.QueueReportsFlyout.IsOpen = !MainWindow.thisApp.QueueReportsFlyout.IsOpen;
        }

        private void QueueManagementButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thisApp.QueueManagementFlyout.IsOpen = !MainWindow.thisApp.QueueManagementFlyout.IsOpen;
        }

        private void ViewExplanationsButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.thisApp.ViewExplanationsFlyout.IsOpen = !MainWindow.thisApp.ViewExplanationsFlyout.IsOpen;
        }

       
        private void AddExplanationButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExplanationText.Visibility == Visibility.Hidden)
                ExplanationText.Visibility = Visibility.Visible;
            else
                ExplanationText.Visibility = Visibility.Hidden;

            if (SubmitExplanationButton.Visibility == Visibility.Hidden)
                SubmitExplanationButton.Visibility = Visibility.Visible;
            else
                SubmitExplanationButton.Visibility = Visibility.Hidden;

            ExplanationText.Text = "";
            ExplanationText.Focus();
        }

        private void SubmitExplanationButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> entries = token.Split(',').ToList();
            string DisplayName = string.Empty;

            foreach (string s in entries)
            {
                if (s.Contains("displayName"))
                {
                    DisplayName = s.Split(':')[1].Trim('"');
                }
            }

            string Explanation = ExplanationText.Text;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("INSERT INTO Explanations (Engineer, Explanation) VALUES ('{0}','{1}')", DisplayName, Explanation.Replace("'", "''"));

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }

            ExplanationText.Visibility = Visibility.Hidden;
            SubmitExplanationButton.Visibility = Visibility.Hidden;

            ExplanationStatus.Visibility = Visibility.Visible;
        }

        private void ExplanationTextEnter(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                List<string> entries = token.Split(',').ToList();
                string DisplayName = string.Empty;

                foreach (string s in entries)
                {
                    if (s.Contains("displayName"))
                    {
                        DisplayName = s.Split(':')[1].Trim('"');
                    }
                }

                string Explanation = ExplanationText.Text;

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("INSERT INTO Explanations (Engineer, Explanation) VALUES ('{0}','{1}')", DisplayName, Explanation.Replace("'", "''"));

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                ExplanationText.Visibility = Visibility.Hidden;
                SubmitExplanationButton.Visibility = Visibility.Hidden;

                ExplanationStatus.Visibility = Visibility.Visible;
            }
        }

        private void AddUnlistedCaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnlistedCaseNumber.Visibility == Visibility.Hidden)
                UnlistedCaseNumber.Visibility = Visibility.Visible;
            else
                UnlistedCaseNumber.Visibility = Visibility.Hidden;

            if (SubmitUnlistedCaseButton.Visibility == Visibility.Hidden)
                SubmitUnlistedCaseButton.Visibility = Visibility.Visible;
            else
                SubmitUnlistedCaseButton.Visibility = Visibility.Hidden;

            if (UnlistedCaseSeverity.Visibility == Visibility.Hidden)
                UnlistedCaseSeverity.Visibility = Visibility.Visible;
            else
                UnlistedCaseSeverity.Visibility = Visibility.Hidden;

            if (UnlistedCaseQueue.Visibility == Visibility.Hidden)
                UnlistedCaseQueue.Visibility = Visibility.Visible;
            else
                UnlistedCaseQueue.Visibility = Visibility.Hidden;

            if (UnlistedCaseDate.Visibility == Visibility.Hidden)
                UnlistedCaseDate.Visibility = Visibility.Visible;
            else
                UnlistedCaseDate.Visibility = Visibility.Hidden;

            ExplanationStatus.Visibility = Visibility.Hidden;

            AddExplanationButton.Visibility = Visibility.Hidden;

            UnlistedCaseNumber.Text = "";
            UnlistedCaseDate.SelectedDate = DateTime.Today;
            UnlistedCaseNumber.Focus();
        }

        private void UnlistedTextEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (UnlistedCaseNumber.Text.Trim() == "x18531cx3f15cx")
                {
                    MessageBox.Show("Nope!");
                    return;
                }

                List<string> entries = token.Split(',').ToList();
                string DisplayName = string.Empty;

                foreach (string s in entries)
                {
                    if (s.Contains("displayName"))
                    {
                        DisplayName = s.Split(':')[1].Trim('"');
                    }
                }

                List<CaseModel> _cases = Queue.GetCases();
                if (_cases.Any(c => c.CaseID == Regex.Replace(UnlistedCaseNumber.Text.Trim(), "[^0-9]", "")))
                {
                    Debug.WriteLine(DisplayName);

                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = string.Format("UPDATE Cases SET TakenBy='{0}', TakenAt='{1}' WHERE CaseID='{2}'", DisplayName, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), UnlistedCaseNumber.Text);

                        string query2 = string.Format("INSERT INTO OwnedCases (Created, CaseID, Severity, Queue, Owner) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", UnlistedCaseDate.SelectedDate, UnlistedCaseNumber.Text.Trim(), UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), UnlistedCaseQueue.Text, DisplayName);

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (SqlCommand command = new SqlCommand(query2, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        connection.Close();
                    }

                    CasesDataGrid.ItemsSource = null;
                    CasesDataGrid.ItemsSource = Queue.GetCases(token, groupToken);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(UnlistedCaseNumber.Text) && UnlistedCaseDate.SelectedDate != null)
                    {
                        using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                        {
                            connection.Open();

                            string query = string.Format("INSERT INTO Cases (Posted, CaseID, Severity, TakenBy, TakenAt, AddedBy, Queue) VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}', '{6}')",
                                DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), UnlistedCaseNumber.Text, UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), DisplayName,
                                DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), DisplayName, UnlistedCaseQueue.SelectedValue.ToString().Split(':')[1].Trim()[0]);

                            string query2 = string.Format("INSERT INTO OwnedCases (Created, CaseID, Severity, Queue, Owner) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", UnlistedCaseDate.SelectedDate, UnlistedCaseNumber.Text.Trim(), UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), UnlistedCaseQueue.Text, DisplayName);

                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            using (SqlCommand command2 = new SqlCommand(query2, connection))
                            {
                                command2.ExecuteNonQuery();
                            }

                            connection.Close();
                        }
                    }

                    UnlistedCaseNumber.Visibility = Visibility.Hidden;
                    SubmitUnlistedCaseButton.Visibility = Visibility.Hidden;
                    UnlistedCaseSeverity.Visibility = Visibility.Hidden;
                    UnlistedCaseDate.Visibility = Visibility.Hidden;
                    UnlistedCaseQueue.Visibility = Visibility.Hidden;
                    AddExplanationButton.Visibility = Visibility.Visible;
                }
            }
        }

        private void SubmitUnlistedCaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnlistedCaseNumber.Text.Trim() == "x18531cx3f15cx")
            {
                MessageBox.Show("Nope!");
                return;
            }

            List<string> entries = token.Split(',').ToList();
            string DisplayName = string.Empty;

            foreach (string s in entries)
            {
                if (s.Contains("displayName"))
                {
                    DisplayName = s.Split(':')[1].Trim('"');
                }
            }

            List<CaseModel> _cases = Queue.GetCases();
            if (_cases.Any(c => c.CaseID == Regex.Replace(UnlistedCaseNumber.Text.Trim(), "[^0-9]", "")))
            {
                Debug.WriteLine(DisplayName);

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("UPDATE Cases SET TakenBy='{0}', TakenAt='{1}' WHERE CaseID='{2}'", DisplayName, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), UnlistedCaseNumber.Text);

                    string query2 = string.Format("INSERT INTO OwnedCases (Created, CaseID, Severity, Queue, Owner) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", UnlistedCaseDate.SelectedDate, UnlistedCaseNumber.Text.Trim(), UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), UnlistedCaseQueue.Text, DisplayName);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    using (SqlCommand command = new SqlCommand(query2, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                CasesDataGrid.ItemsSource = null;
                CasesDataGrid.ItemsSource = Queue.GetCases(token, groupToken);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(UnlistedCaseNumber.Text) && UnlistedCaseDate.SelectedDate != null)
                {
                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = string.Format("INSERT INTO Cases (Posted, CaseID, Severity, TakenBy, TakenAt, AddedBy, Queue) VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}', '{6}')",
                            DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), UnlistedCaseNumber.Text, UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), DisplayName,
                            DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), DisplayName, UnlistedCaseQueue.SelectedValue.ToString().Split(':')[1].Trim()[0]);

                        string query2 = string.Format("INSERT INTO OwnedCases (Created, CaseID, Severity, Queue, Owner) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", UnlistedCaseDate.SelectedDate, UnlistedCaseNumber.Text.Trim(), UnlistedCaseSeverity.SelectedValue.ToString().Split(':')[1].Split('(')[0].Trim(), UnlistedCaseQueue.Text, DisplayName);

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (SqlCommand command2 = new SqlCommand(query2, connection))
                        {
                            command2.ExecuteNonQuery();
                        }

                        connection.Close();
                    }
                }

                UnlistedCaseNumber.Visibility = Visibility.Hidden;
                SubmitUnlistedCaseButton.Visibility = Visibility.Hidden;
                UnlistedCaseSeverity.Visibility = Visibility.Hidden;
                UnlistedCaseDate.Visibility = Visibility.Hidden;
                UnlistedCaseQueue.Visibility = Visibility.Hidden;
                AddExplanationButton.Visibility = Visibility.Visible;
            }
        }

        private void TransferCaseButton_Click(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;
            caseSelected = origin.CommandParameter.ToString();

            TransferApprovedByText.Visibility = Visibility.Visible;
            TransferApprovedByText.Text = "";
            TransferApprovedByText.Focus();

            SubmitTransferButton.Visibility = Visibility.Visible;
        }

        private void SubmitTransferTextEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrWhiteSpace(TransferApprovedByText.Text))
                {
                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = string.Format("UPDATE Cases SET Transferred=1, TransferApprovedBy='{0}' WHERE CaseID={1}", TransferApprovedByText.Text, caseSelected);

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        connection.Close();
                    }

                    TransferApprovedByText.Visibility = Visibility.Hidden;
                    SubmitTransferButton.Visibility = Visibility.Hidden;
                }
            }
        }

        private void SubmitTransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TransferApprovedByText.Text))
            {
                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("UPDATE Cases SET Transferred=1, TransferApprovedBy='{0}' WHERE CaseID={1}", TransferApprovedByText.Text, caseSelected);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                TransferApprovedByText.Visibility = Visibility.Hidden;
                SubmitTransferButton.Visibility = Visibility.Hidden;
            }
        }

        private void CloseCaseButton_Click(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;
            MessageBoxResult result = MessageBox.Show("Are you sure you want to mark case # " + origin.CommandParameter.ToString() + " as closed?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.None);

            if (result == MessageBoxResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("UPDATE Cases SET Closed=1 WHERE CaseID={0}", origin.CommandParameter.ToString());

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
        }

        private void DeleteCaseButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this case from the queue?\n\nThis cannot be undone", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.None);

            if (result == MessageBoxResult.Yes)
            {
                Button origin = sender as Button;

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("DELETE FROM Cases WHERE ID={0}", origin.CommandParameter.ToString());

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
        }

        private void DisplayFullDescription(object sender, MouseButtonEventArgs e)
        {
            TextBlock origin = sender as TextBlock;

            if (origin.Text.Contains("see more"))
            {
                List<CaseModel> cases = Queue.GetCases();
                string desc = string.Empty;

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("SELECT Description FROM Cases WHERE CaseID={0}", origin.Tag);

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        desc = command.ExecuteScalar().ToString();
                    }

                    connection.Close();
                }

                MessageBox.Show("Full description:\n\n" + desc);
            }
        }
    }
}
