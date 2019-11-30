using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Diagnostics;

namespace TBG_WPF
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : MetroWindow
    {
        public string CurrentTechnology = string.Empty;
        public string CurrentCategory = string.Empty;
        public string CurrentIssue = string.Empty;

        private bool TbsChanged = false;
        private bool PeerChanged = false;
        private bool DocuChanged = false;
        private bool TrainingChanged = false;

        private BackgroundWorker worker;

        List<SuggestionModel> suggestions = new List<SuggestionModel>();

        List<string> Technologies = new List<string>();
        List<string> Categories = new List<string>();
        List<string> Issues = new List<string>();

        List<TreeViewItem> TechTV = new List<TreeViewItem>();
        List<TreeViewItem> CatTV = new List<TreeViewItem>();

        public AdminWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            System.Timers.Timer timer = new System.Timers.Timer(30000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            worker.RunWorkerAsync();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "SELECT Technology FROM Technologies";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!Technologies.Any(t => t == reader.GetString(0)))
                            {
                                Technologies.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                connection.Close();
            }

            TechnologyBox.ItemsSource = Technologies;
            TechnologyBox2.ItemsSource = Technologies;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                suggestions.Clear();
                SuggestionDataGrid.ItemsSource = null;
            });

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Suggestions";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suggestions.Add(new SuggestionModel()
                            {
                                ID = reader.GetInt32(0),
                                SubmittedBy = reader.GetString(1),
                                Suggestion = reader.GetString(2),
                                Technology = reader.GetString(3),
                                Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Issue = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            });
                        }
                    }
                }

                connection.Close();
            }

            this.Dispatcher.Invoke(() =>
            {
                SuggestionDataGrid.ItemsSource = suggestions;
            });

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                DateTime ExpiryDate = DateTime.Now.AddDays(-7);

                string PullQuery = "SELECT HasNew,NewUpdated,Technology,Category,Issue FROM Technologies";
                string SetQuery = "UPDATE Technologies SET HasNew=0, HasNewTroubleshooting=0, HasNewPeerSuggestions=0, HasNewDocumentation=0, HasNewTraining=0, NewUpdated=NULL WHERE Technology='{0}' AND Category='{1}' AND Issue='{2}'";

                List<string> Tech = new List<string>();
                List<string> Cat = new List<string>();
                List<string> Iss = new List<string>();

                using (SqlCommand command = new SqlCommand(PullQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetBoolean(0) == true || !reader.IsDBNull(1))
                            {
                                DateTime PulledDate = reader.GetDateTime(1);

                                if (PulledDate.CompareTo(ExpiryDate) < 0)
                                {
                                    Tech.Add(reader.GetString(2));
                                    Cat.Add(reader.GetString(3));
                                    Iss.Add(reader.GetString(4));
                                }
                            }
                        }
                    }
                }

                for (int i = 0; i < Tech.Count; i++)
                {
                    string query = string.Format(SetQuery, Tech[i], Cat[i], Iss[i]);

                    using (SqlCommand updateCommand = new SqlCommand(query, connection))
                    {
                        Debug.WriteLine(updateCommand.ExecuteNonQuery());
                    }
                }

                connection.Close();
            }
        }

        private void TechnologyChanged(object sender, SelectionChangedEventArgs e)
        {
            IssueBox.ItemsSource = null;
            CategoryBox.ItemsSource = null;

            Categories.Clear();
            Issues.Clear();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("SELECT Category FROM Technologies WHERE Technology='{0}'", TechnologyBox.SelectedValue);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!Categories.Any(t => t == reader.GetString(0)))
                            {
                                Categories.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                connection.Close();
            }

            CategoryBox.ItemsSource = Categories;
        }

        private void CategoryChanged(object sender, SelectionChangedEventArgs e)
        {
            IssueBox.ItemsSource = null;

            Issues.Clear();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("SELECT Issue FROM Technologies WHERE Technology='{0}' AND Category='{1}'", TechnologyBox.SelectedValue, CategoryBox.SelectedValue);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!Issues.Any(t => t == reader.GetString(0)))
                            {
                                Issues.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                connection.Close();
            }

            IssueBox.ItemsSource = Issues;
        }

        private void PullButton_Click(object sender, RoutedEventArgs e)
        {
            TbsBox.TextChanged -= TbsTextChanged;
            PeerBox.TextChanged -= PeerTextChanged;
            DocuBox.TextChanged -= DocuTextChanged;
            TrainingBox.TextChanged -= TrainingTextChanged;

            TbsChanged = false;
            PeerChanged = false;
            DocuChanged = false;
            TrainingChanged = false;

            if (TechnologyBox.SelectedIndex > -1 && CategoryBox.SelectedIndex > -1 && IssueBox.SelectedIndex > -1)
            {
                try
                {
                    string Warning = string.Empty;
                    string Troubleshooting = string.Empty;
                    string Peer = string.Empty;
                    string Documentation = string.Empty;
                    string Training = string.Empty;

                    WarningBox.Text = "";
                    TbsBox.Text = "";
                    PeerBox.Text = "";
                    DocuBox.Text = "";
                    TrainingBox.Text = "";

                    CurrentTechnology = TechnologyBox.SelectedValue.ToString();
                    CurrentCategory = CategoryBox.SelectedValue.ToString();
                    CurrentIssue = IssueBox.SelectedValue.ToString();

                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = string.Format("SELECT * FROM Technologies WHERE Technology='{0}' AND Category='{1}' AND Issue='{2}'", CurrentTechnology, CurrentCategory, CurrentIssue);

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();

                                Warning = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                Troubleshooting = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                Peer = reader.IsDBNull(6) ? "" : reader.GetString(6);
                                Documentation = reader.IsDBNull(7) ? "" : reader.GetString(7);
                                Training = reader.IsDBNull(8) ? "" : reader.GetString(8);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(Warning))
                    {
                        WarningBox.Text = Warning.Replace(';', '\n');
                    }

                    if (!string.IsNullOrWhiteSpace(Troubleshooting))
                    {
                        TbsBox.Text = Troubleshooting.Replace(';', '\n');
                    }

                    if (!string.IsNullOrWhiteSpace(Peer))
                    {
                        PeerBox.Text = Peer.Replace(';', '\n');
                    }

                    if (!string.IsNullOrWhiteSpace(Documentation))
                    {
                        DocuBox.Text = Documentation.Replace(';', '\n').Replace("|", " | ");
                    }

                    if (!string.IsNullOrWhiteSpace(Training))
                    {
                        TrainingBox.Text = Training.Replace(';', '\n');
                    }
                }
                catch (Exception exc) { };
            }

            TbsBox.TextChanged += TbsTextChanged;
            PeerBox.TextChanged += PeerTextChanged;
            DocuBox.TextChanged += DocuTextChanged;
            TrainingBox.TextChanged += TrainingTextChanged;

        }

        private void DataSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string Warning = WarningBox.Text.TrimEnd('\n').Replace('\n', ';').Replace("'", "''");
            string Troubleshooting = TbsBox.Text.TrimEnd('\n').Replace('\n', ';').Replace("'", "''");
            string Peer = PeerBox.Text.TrimEnd('\n').Replace('\n', ';').Replace("'", "''");
            string Documentation = DocuBox.Text.TrimEnd('\n').Replace('\n', ';').Replace("'", "''").Replace(" | ", "|");
            string Training = TrainingBox.Text.TrimEnd('\n').Replace('\n', ';').Replace("'", "''");

            bool HasNew = false;
            if (TbsChanged || PeerChanged || DocuChanged || TrainingChanged)
                HasNew = true;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE Technologies SET Warning='{0}', Troubleshooting='{1}', PeerSuggestions='{2}', Documentation='{3}', Training='{4}', HasNew={5}, HasNewTroubleshooting={6}, " +
                    "HasNewPeerSuggestions={7}, HasNewDocumentation={8}, HasNewTraining={9}, NewUpdated='{10}' WHERE Technology='{11}' AND Category='{12}' AND Issue='{13}'", Warning.Replace("'", "''"), Troubleshooting.Replace("'", "''"),
                    Peer.Replace("'", "''"), Documentation.Replace("'", "''"), Training.Replace("'", "''"), HasNew ? 1 : 0, TbsChanged ? 1 : 0, PeerChanged ? 1 : 0, DocuChanged ? 1 : 0, TrainingChanged ? 1 : 0, DateTime.Now.ToString("yyyy-MM-dd"), CurrentTechnology, CurrentCategory, CurrentIssue);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    int ret = command.ExecuteNonQuery();

                    if (ret == 0)
                    {
                        SubmissionLabel.Foreground = new SolidColorBrush(Colors.Red);
                        SubmissionLabel.Text = "There were no changes made, or there was an issue submitting the data to the SQL Server";
                    }
                    else
                    {
                        SubmissionLabel.Foreground = new SolidColorBrush(Colors.Green);
                        SubmissionLabel.Text = "Your data was successfully submitted, changes should appear immediately";
                    }
                }

                connection.Close();
            }
        }

        private void ExpanderExpanded(object sender, RoutedEventArgs e)
        {
            Expander thisExpander = sender as Expander;

            foreach (FrameworkElement element in GuidePanel.Children)
            {
                if (element is Expander && element != thisExpander)
                {
                    (element as Expander).IsExpanded = false;
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            suggestions.Clear();
            SuggestionDataGrid.ItemsSource = null;

            Button thisButton = sender as Button;
            int id = int.Parse(thisButton.CommandParameter.ToString());

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("DELETE FROM Suggestions WHERE ID={0}", id);

                try
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception exc) { };

                worker.RunWorkerAsync();

                connection.Close();
            }

            SuggestionDataGrid.ItemsSource = suggestions;
        }

        private void ImplementedButton_Click(object sender, RoutedEventArgs e)
        {
            suggestions.Clear();
            SuggestionDataGrid.ItemsSource = null;

            Button thisButton = sender as Button;
            int id = int.Parse(thisButton.CommandParameter.ToString());

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("DELETE FROM Suggestions WHERE ID={0}", id);

                try
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception exc) { };

                worker.RunWorkerAsync();

                connection.Close();
            }

            SuggestionDataGrid.ItemsSource = suggestions;
        }

        private void WarningRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (WarningRadio.IsChecked == true)
            {
                AlertSubmitButton.Content = "Click to submit and push warning";
            }
        }

        private void AlertRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (AlertRadio.IsChecked == true)
            {
                AlertSubmitButton.Content = "Click to submit and push alert";
            }
        }

        private void AlertSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("INSERT INTO Notifications (Type, Text, DatePosted) VALUES ('{0}', '{1}', '{2}')", (AlertRadio.IsChecked == true) ? "Alert" : "Warning", AlertTextBox.Text.Replace("'", "''"), DateTime.Now.ToString("yyyy-MM-dd"));

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }

            AlertSubmissionText.Text = "Alert posted, users with the app open will receive it within 1 minute";
        }

        private void CloseAlertBox_Click(object sender, RoutedEventArgs e)
        {
            AlertGrid.IsHitTestVisible = false;
            AlertGrid.Visibility = Visibility.Collapsed;

            AlertSubmissionText.Text = "";
        }

        private void OpenAlertBox_Click(object sender, RoutedEventArgs e)
        {
            AlertGrid.IsHitTestVisible = true;
            AlertGrid.Visibility = Visibility.Visible;

            AlertSubmissionText.Text = "";
        }

        private void UploadScreenshotsButton_Click(object sender, RoutedEventArgs e)
        {
            UploadStatusText.Text = "Upload status: ";

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (.jpg, .png)|*.jpg; *.png;";
            ofd.Title = "Select a screenshot to import";

            Nullable<bool> result = ofd.ShowDialog();

            if (result == true)
            {
                try
                {
                    string filename = ofd.FileName;

                    CloudStorageAccount storageAccount;
                    CloudBlobClient blobClient;
                    CloudBlobContainer blobContainer;
                    CloudBlockBlob blob;

                    string blobReference = CurrentTechnology + "/" + CurrentCategory.Replace('/', ' ') + "/" + CurrentIssue.Replace('/', ' ') + "/" + "Troubleshooting" + "/" + DateTime.Now.ToString("yy-MM-dd_hh-mm-ss") + (filename.EndsWith(".png") ? ".png" : ".jpg");

                    if (CloudStorageAccount.TryParse(Blob_Controller.ConnectionString, out storageAccount))
                    {
                        blobClient = storageAccount.CreateCloudBlobClient();
                        blobContainer = blobClient.GetContainerReference("tbg-media-storage");
                        blob = blobContainer.GetBlockBlobReference(blobReference);

                        FileStream imageStream = new FileStream(filename, FileMode.Open);

                        blob.UploadFromStream(imageStream);
                        UploadStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
                        UploadStatusText.Text = UploadStatusText.Text + "Successful";

                    }
                }
                catch (Exception exc)
                {
                    UploadStatusText.Foreground = new SolidColorBrush(Colors.Red);
                    UploadStatusText.Text = UploadStatusText.Text + "Failed";
                }
            }
        }

        private void TbsTextChanged(object sender, TextChangedEventArgs e)
        {
            TbsChanged = true;
        }

        private void PeerTextChanged(object sender, TextChangedEventArgs e)
        {
            PeerChanged = true;
        }

        private void DocuTextChanged(object sender, TextChangedEventArgs e)
        {
            DocuChanged = true;
        }

        private void TrainingTextChanged(object sender, TextChangedEventArgs e)
        {
            TrainingChanged = true;
        }

        private void CreateIssue_Clicked(object sender, RoutedEventArgs e)
        {
            NewIssueGrid.Visibility = Visibility.Visible;
            NewIssueGrid.IsHitTestVisible = true;
        }

        private void NewIssueSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            NewIssueGrid.Visibility = Visibility.Hidden;
            NewIssueGrid.IsHitTestVisible = false;

            SelectCategoryGrid.Visibility = Visibility.Visible;
            SelectCategoryGrid.IsHitTestVisible = true;

            GetCategories();
        }

        private void DetailsFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(DetailsFilter.Text))
            {
                foreach (TreeViewItem item in DetailsPane.Items)
                {
                    foreach (TreeViewItem item2 in item.Items)
                    {
                        foreach (TreeViewItem item3 in item2.Items)
                        {
                            if (!item3.Header.ToString().ToLower().Contains(DetailsFilter.Text.ToLower()))
                            {
                                item3.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                item3.Background = new SolidColorBrush(Colors.DarkGreen);
                            }
                        }

                        bool hasVisible2 = false;

                        foreach (TreeViewItem item3 in item2.Items)
                        {
                            if (item3.Visibility == Visibility.Visible)
                                hasVisible2 = true;
                        }

                        if (hasVisible2 == false)
                            item2.Visibility = Visibility.Collapsed;
                    }

                    bool hasVisible = false;

                    foreach (TreeViewItem item2 in item.Items)
                    {
                        if (item2.Visibility == Visibility.Visible)
                            hasVisible = true;
                    }

                    if (hasVisible == false)
                        item.Visibility = Visibility.Collapsed;


                    bool checkAny = false;

                    if (item.Visibility == Visibility.Visible)
                        checkAny = true;

                    if (checkAny == false)
                    {
                    }
                }
            }
            else
            {
                foreach (TreeViewItem item in DetailsPane.Items)
                {
                    item.Visibility = Visibility.Visible;

                    foreach (TreeViewItem item2 in item.Items)
                    {
                        item2.Visibility = Visibility.Visible;

                        foreach (TreeViewItem item3 in item2.Items)
                        {
                            item3.Visibility = Visibility.Visible;
                            item3.Background = null;
                        }
                    }
                }
            }
        }

        private void GetCategories()
        {
            DetailsPane.ItemsSource = null;

            DetailsFilter.TextChanged -= DetailsFilterTextChanged;
            DetailsFilter.Text = string.Empty;
            DetailsFilter.TextChanged += DetailsFilterTextChanged;

            string query = "SELECT Technology,Category FROM Technologies";

            TechTV.Clear();
            CatTV.Clear();
            
            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!TechTV.Any(t => t.Header.ToString() == reader.GetString(0)))
                            {
                                TreeViewItem newItem = new TreeViewItem();
                                Debug.WriteLine(reader.GetString(0));
                                newItem.Header = reader.GetString(0);
                                TechTV.Add(newItem);
                            }

                            if (!CatTV.Any(c => c.Header.ToString() == reader.GetString(1)))
                            {
                                TreeViewItem newItem = new TreeViewItem();
                                newItem.Header = reader.GetString(1);
                                CatTV.Add(newItem);
                            }

                            TreeViewItem TechItem = TechTV.First(t => t.Header.ToString() == reader.GetString(0));
                            TreeViewItem CatItem = CatTV.First(c => c.Header.ToString() == reader.GetString(1));

                            if (CatItem.Parent == null)
                                TechItem.Items.Add(CatItem);
                        }
                    }
                }

                connection.Close();
            }

            DetailsPane.ItemsSource = TechTV;
        }

        private void SelectedCategory_Click(object sender, RoutedEventArgs e)
        {
            string Tech = string.Empty;
            string Cat = string.Empty;
            string Iss = NewIssueTextBox.Text;

            NewIssueTextBox.Text = "";

            foreach (TreeViewItem item in DetailsPane.Items)
            {
                foreach (TreeViewItem cat in item.Items)
                {
                    if (cat.IsSelected)
                    {
                        Tech = (cat.Parent as TreeViewItem).Header.ToString();
                        Cat = cat.Header.ToString();
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("INSERT INTO Technologies (Technology, Category, Issue) VALUES ('{0}', '{1}', '{2}')", Tech, Cat, Iss);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    Debug.WriteLine("NEW ISSUE: " + command.ExecuteNonQuery());
                }

                connection.Close();
            }

            SelectCategoryGrid.Visibility = Visibility.Hidden;
            SelectCategoryGrid.IsHitTestVisible = false;
        }

        private void CreateCategory_Click(object sender, RoutedEventArgs e)
        {
            NewCategoryGrid.Visibility = Visibility.Visible;
            NewCategoryGrid.IsHitTestVisible = true;
        }

        private void NewCategorySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string Tech = TechnologyBox2.SelectedValue.ToString();
            string Cat = NewCategoryTextBox.Text;
            string Iss = NewIssueFromCategoryTextBox.Text;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("INSERT INTO Technologies (Technology, Category, Issue) VALUES ('{0}', '{1}', '{2}')", Tech, Cat, Iss);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    Debug.Write("NEW CATEGORY: " + command.ExecuteNonQuery());
                }

                connection.Close();
            }

            NewCategoryGrid.Visibility = Visibility.Hidden;
            NewCategoryGrid.IsHitTestVisible = false;

            NewIssueFromCategoryTextBox.Text = "";
            NewIssueFromCategoryLabel.Text = "Enter issue title below (Suggestion: ):";
            TechnologyBox2.SelectedIndex = -1;
            NewCategoryTextBox.Text = "";
        }

        private void NewCategoryBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            NewIssueFromCategoryLabel.Text = "Enter issue title below (Suggestion: \"General " + NewCategoryTextBox.Text + "\"):";
        }

        private void CloseNewCategoryGrid_Click(object sender, RoutedEventArgs e)
        {
            NewCategoryGrid.Visibility = Visibility.Hidden;
            NewCategoryGrid.IsHitTestVisible = false;
        }

        private void CloseNewIssueGrid_Click(object sender, RoutedEventArgs e)
        {
            NewIssueGrid.Visibility = Visibility.Hidden;
            NewIssueGrid.IsHitTestVisible = false;
        }

        //private void NewCatIssueSubmit(object sender, RoutedEventArgs e)
        //{
        //    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
        //    {
        //        connection.Open();

        //        string query = string.Empty;

        //        if (CategoryRadio.IsChecked == true)
        //        {
        //            query = string.Format("INSERT INTO Technologies (Technology, Category, Issue) VALUES ('{0}', '{1}', 'Advisory')", NewTechnologyBox.SelectedValue.ToString(), NewTextBox.Text);
        //        }
        //        else
        //        {
        //            query = string.Format("INSERT INTO Technologies (Technology, Category, Issue) VALUES ('{0}', '{1}', '{2}')", NewTechnologyBox.SelectedValue.ToString(), NewCategoryBox.SelectedValue.ToString(), NewTextBox.Text);
        //        }

        //        using (SqlCommand command = new SqlCommand(query, connection))
        //        {
        //            command.ExecuteNonQuery();
        //        }

        //        connection.Close();
        //    }
        //}
    }
}
