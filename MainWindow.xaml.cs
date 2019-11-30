using MahApps.Metro;
using MahApps.Metro.Controls;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TBG_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //Set the API Endpoint to Graph 'me' endpoint
        string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        //Set the API Endpoint to Graph 'groups' endpoint
        string graphGroupsAPIEndpoint = "https://graph.microsoft.com/v1.0/me/MemberOf";

        //Set the scope for API call to user.read
        string[] scopes = new string[] { "user.read" };

        public string token = string.Empty;
        public string GroupToken = string.Empty;

        public string CurrentTechnology = string.Empty;
        public string CurrentCategory = string.Empty;
        public string CurrentIssue = string.Empty;

        public string Suggestion = string.Empty;

        public bool RequiresAdminPassword = true;
        public bool IsQueueAdmin = false;

        QueuePage queuePage;
        CasesPage casesPage;
        ReportingPage reportingPage;

        //List<ComboBoxItem> Technologies = new List<ComboBoxItem>();
        //List<ComboBoxItem> Categories = new List<ComboBoxItem>();
        //List<TreeViewItem> CategoriesTV = new List<TreeViewItem>();
        //List<ComboBoxItem> Issues = new List<ComboBoxItem>();
        //List<TreeViewItem> IssuesTV = new List<TreeViewItem>();

        List<TreeViewItem> Technologies = new List<TreeViewItem>();
        List<TreeViewItem> Categories = new List<TreeViewItem>();
        List<TreeViewItem> Issues = new List<TreeViewItem>();

        Brush OriginalBrush;
        Brush WindowBrush;

        string caseSelected = string.Empty;

        List<NotificationModel> Notifications = new List<NotificationModel>();

        public static MainWindow thisApp;

        private static string FileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/";

        NewCatIssue newCat = new NewCatIssue();

        QueueManagement Queue = new QueueManagement();

        TBG TBGDataSet = new TBG();
        TBGTableAdapters.CasesTableAdapter casesTableAdapter = new TBGTableAdapters.CasesTableAdapter();
        DataTable fullEditTable = new DataTable();

        BackgroundWorker worker;
        BackgroundWorker queueWorker;

        public MainWindow()
        {
            InitializeComponent();

            OriginalBrush = TbsExpanderP.Background;
            WindowBrush = TopicTile.Background;

            //NewCaseFlyout.Background = WindowBrush;
            //QueueReportsFlyout.Background = WindowBrush;
            //QueueManagementFlyout.Background = WindowBrush;

            thisApp = this;

            fullEditTable = casesTableAdapter.GetData();
            FullEditGrid.ItemsSource = fullEditTable.DefaultView;
            fullEditTable.RowChanged += FullEditTable_RowChanged;

            DisableAll();

            AquireToken();

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/");
            }

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/Screenshots/"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/Screenshots/");
            }

            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/Seen_Notifications.txt"))
            {
                var newFile = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/Seen_Notifications.txt");
                newFile.Close();
            }

            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            System.Timers.Timer timer = new System.Timers.Timer(15000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            worker.RunWorkerAsync();

            queueWorker = new BackgroundWorker();
            queueWorker.DoWork += queueWorker_DoWork;
            System.Timers.Timer timer2 = new System.Timers.Timer(5000);
            timer2.Elapsed += Timer2_Elapsed;
            timer2.Start();

            queueWorker.RunWorkerAsync();
        }

        private void PassBack()
        {
            queuePage = new QueuePage(token, GroupToken, Queue);

            List<CaseModel> cases = Queue.GetCases();
            queuePage.CasesDataGrid.ItemsSource = cases;
            DetailsPane.ItemsSource = Technologies;

            if (IsQueueAdmin)
            {
                queuePage.QueueManagementButton.Visibility = Visibility.Visible;
                queuePage.QueueReportsButton.Visibility = Visibility.Visible;
                queuePage.QueueNewCaseButton.Visibility = Visibility.Visible;
                queuePage.ViewExplanationsButton.Visibility = Visibility.Visible;
            }

            casesPage = new CasesPage(token, GroupToken);
            reportingPage = new ReportingPage(token);
            
            PageFrame.Navigate(casesPage);
        }

        private void FullEditTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            casesTableAdapter.Update(e.Row);
        }

        private void Timer2_Elapsed(object sender, EventArgs e)
        {
            queueWorker.RunWorkerAsync();
        }

        private void queueWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (queuePage != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine("Get Queue");

                    List<CaseModel> Cases = Queue.GetCases(token, GroupToken);
                    int takenCount = Cases.Where(c => !string.IsNullOrWhiteSpace(c.TakenBy) && c.Transferred == false).Count();
                    int closedCount = Cases.Where(c => c.Closed == true).Count();
                    queuePage.TakenCount.Text = takenCount.ToString();
                    queuePage.ClosedCount.Text = closedCount.ToString();

                    if (takenCount > 20 && takenCount <= 30)
                        queuePage.TakenCount.Foreground = new SolidColorBrush(Colors.Yellow);
                    else if (takenCount > 30 && takenCount <= 39)
                        queuePage.TakenCount.Foreground = new SolidColorBrush(Colors.YellowGreen);
                    else if (takenCount > 39)
                        queuePage.TakenCount.Foreground = new SolidColorBrush(Colors.Chartreuse);
                    else
                        queuePage.TakenCount.Foreground = new SolidColorBrush(Colors.White);

                    int selected = queuePage.CasesDataGrid.SelectedIndex;

                    queuePage.CasesDataGrid.ItemsSource = null;
                    queuePage.CasesDataGrid.ItemsSource = Cases.OrderBy(c => c.TakenAt).ThenBy(c => c.Severity).ThenBy(c => c.Age).ToList();

                    queuePage.CasesDataGrid.SelectedIndex = selected;


                    List<ExplanationModel> Explanations = Queue.GetExplanations();

                    ExplanationsDataGrid.ItemsSource = null;
                    ExplanationsDataGrid.ItemsSource = Explanations;
                });
            }
        }

        internal void CloseCatIssue()
        {
            CatIssueGrid.Visibility = Visibility.Collapsed;
            CatIssueGrid.IsHitTestVisible = false;
        }

        [DllImport("user32.dll")]
        public static extern int FlashWindow(IntPtr Hwnd, bool Revert);

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                AlertsPanel.Children.Clear();
                WarningsPanel.Children.Clear();
                Notifications.Clear();
            });

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("SELECT * FROM Notifications WHERE DatePosted > '{0}'", DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd"));

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                        }
                    }
                }

                connection.Close();
            }

            if (Notifications.Count > 0)
            {
                string Seen = File.ReadAllText(FileDirectory + "Seen_Notifications.txt");
                foreach (string s in Seen.Split(','))
                {
                    try
                    {
                        int index = Notifications.FindIndex(n => n.ID.ToString() == s);
                        Notifications.RemoveAt(index);
                    }
                    catch (Exception exc)
                    {

                    }
                }
            }

            if (Notifications.Count > 0)
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (!this.Title.Contains(" - New warnings or alerts!"))
                        this.Title = this.Title + " - New warnings or alerts!";
                    FlashWindow(this.CriticalHandle, false);
                });

                if (Notifications.Any(n => n.Type == NotificationModel.NotificationType.Alert))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        AlertGrid.Visibility = Visibility.Visible;
                        AlertGrid.IsHitTestVisible = true;
                    });
                }

                if (Notifications.Any(n => n.Type == NotificationModel.NotificationType.Warning))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        WarningGrid.Visibility = Visibility.Visible;
                        WarningGrid.IsHitTestVisible = true;
                    });
                }
            }

            this.Dispatcher.Invoke(() =>
            {
                CreateNotifications();
            });
        }

        private void Timer_Elapsed(object sender, EventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void CreateNotifications()
        {
            foreach (NotificationModel notification in Notifications)
            {
                Border _border = new Border();
                _border.Margin = new Thickness(10, 10, 10, 0);
                _border.Width = 320;
                _border.BorderBrush = new SolidColorBrush(Colors.White);
                _border.BorderThickness = new Thickness(1);
                _border.CornerRadius = new CornerRadius(0, 8, 0, 8);
                _border.Name = "Notification_" + notification.ID;

                StackPanel _stackPanel = new StackPanel();

                TextBlock _textBlock = new TextBlock();
                _textBlock.Width = 300;
                _textBlock.Text = notification.Text;
                _textBlock.Foreground = new SolidColorBrush(Colors.White);
                _textBlock.TextWrapping = TextWrapping.Wrap;
                _textBlock.Margin = new Thickness(10, 10, 10, 10);

                Button _button = new Button();
                _button.Width = 298;
                _button.Content = "Dismiss";
                _button.Margin = new Thickness(10, 0, 10, 10);
                _button.CommandParameter = notification.ID;
                _button.Click += DismissNotification_Click;

                _stackPanel.Children.Add(_textBlock);
                _stackPanel.Children.Add(_button);
                _border.Child = _stackPanel;

                if (notification.Type == NotificationModel.NotificationType.Alert)
                    AlertsPanel.Children.Add(_border);
                else if (notification.Type == NotificationModel.NotificationType.Warning)
                    WarningsPanel.Children.Add(_border);
            }
        }

        private void DismissNotification_Click(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;

            string previous = File.ReadAllText(FileDirectory + "Seen_Notifications.txt");
            string current = string.Empty;

            if (string.IsNullOrWhiteSpace(previous))
            {
                current = origin.CommandParameter.ToString();
            }
            else if (previous.EndsWith(","))
            {
                current = previous + origin.CommandParameter.ToString();
            }
            else
            {
                current = previous + "," + origin.CommandParameter.ToString();
            }

            File.WriteAllText(FileDirectory + "Seen_Notifications.txt", current);

            try
            {
                try
                {
                    foreach (FrameworkElement element in WarningsPanel.Children)
                    {
                        if (element.Name == "Notification_" + origin.CommandParameter.ToString())
                        {
                            WarningsPanel.Children.Remove(element);
                        }
                    }
                }
                catch (Exception exc) { }

                try
                {
                    foreach (FrameworkElement element2 in AlertsPanel.Children)
                    {
                        if (element2.Name == "Notification_" + origin.CommandParameter.ToString())
                        {
                            AlertsPanel.Children.Remove(element2);
                        }
                    }
                }
                catch (Exception exc) { }
            }
            catch (Exception exc) { }

            if (WarningsPanel.Children.Count == 0)
            {
                WarningGrid.Visibility = Visibility.Collapsed;
                WarningGrid.IsHitTestVisible = false;
            }

            if (AlertsPanel.Children.Count == 0)
            {
                AlertGrid.Visibility = Visibility.Collapsed;
                AlertGrid.IsHitTestVisible = false;
            }

            if (AlertsPanel.Children.Count == 0 && WarningsPanel.Children.Count == 0)
            {
                this.Title = this.Title.Replace(" - New warnings or alerts!", "");
            }
        }

        private void EnableAll()
        {
            foreach (FrameworkElement element in MainGrid.Children)
            {
                element.IsEnabled = true;
            }

            if (IsQueueAdmin)
                ReportingButton.IsEnabled = true;
            else
                ReportingButton.IsEnabled = false;
        }

        private void DisableAll()
        {
            foreach (FrameworkElement element in MainGrid.Children)
            {
                element.IsEnabled = false; ;
            }
        }


        #region New Data Pulling Method
        private void DetailsFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(DetailsFilter.Text))
            {
                if (ExpandAll.IsChecked == false)
                {
                    ExpandAll.IsChecked = true;
                }

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

        private void OpenSelector(object sender, RoutedEventArgs e)
        {
            PageFrame.Visibility = Visibility.Hidden;

            MenuExpander.IsExpanded = false;

            DetailsPaneFlyout.IsOpen = true;
            DetailsPane.ItemsSource = null;

            ExpandAll.Unchecked -= ExpandAll_Unchecked;
            ExpandAll.IsChecked = false;
            ExpandAll.Unchecked += ExpandAll_Unchecked;

            DetailsFilter.TextChanged -= DetailsFilterTextChanged;
            DetailsFilter.Text = string.Empty;
            DetailsFilter.TextChanged += DetailsFilterTextChanged;

            string query = "SELECT Technology,Category,Issue,HasNew FROM Technologies";

            Technologies.Clear();
            Categories.Clear();
            Issues.Clear();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string Tech = reader.GetString(0);
                            string Cat = reader.GetString(1);

                            TreeViewItem technologyItem = new TreeViewItem();
                            TreeViewItem categoryItem = new TreeViewItem();
                            TreeViewItem issueItem = new TreeViewItem();

                            if (!Technologies.Any(t => t.Header.ToString() == Tech))
                            {
                                technologyItem.Header = reader.GetString(0);
                                Technologies.Add(technologyItem);
                            }

                            bool categoryExists = false;

                            foreach (TreeViewItem tech in Technologies)
                            {
                                if (tech.Header.ToString() == Tech)
                                {
                                    foreach (TreeViewItem cat in tech.Items)
                                    {
                                        if (cat.Header.ToString() == Cat)
                                        {
                                            categoryExists = true;

                                            issueItem.Header = reader.GetString(2);
                                            issueItem.Margin = new Thickness(0, 3, 0, 3);

                                            if (!reader.IsDBNull(3) && reader.GetBoolean(3) == true)
                                            {
                                                issueItem.Foreground = new SolidColorBrush(Colors.Gold);
                                                issueItem.FontWeight = FontWeights.Bold;
                                            }

                                            issueItem.PreviewMouseDown += ItemSelected;
                                            cat.Items.Add(issueItem);
                                        }
                                    }

                                    if (categoryExists == false)
                                    {
                                        categoryItem.Header = reader.GetString(1);

                                        issueItem.Header = reader.GetString(2);
                                        issueItem.Margin = new Thickness(0, 3, 0, 3);

                                        if (!reader.IsDBNull(3) && reader.GetBoolean(3) == true)
                                        {
                                            issueItem.Foreground = new SolidColorBrush(Colors.Gold);
                                            issueItem.FontWeight = FontWeights.Bold;
                                        }

                                        issueItem.PreviewMouseDown += ItemSelected;

                                        tech.Items.Add(categoryItem);
                                        categoryItem.Items.Add(issueItem);
                                    }
                                }
                            }
                        }
                    }
                }

                connection.Close();
            }

            DetailsPane.ItemsSource = Technologies;
        }

        private void ItemSelected(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem origin = sender as TreeViewItem;

            DetailsPaneFlyout.IsOpen = false;

            try
            {
                TbsExpander.Children.Clear();
                PeerExpander.Children.Clear();
                DocuExpander.Children.Clear();
                TrainingExpander.Children.Clear();

                string Warning = string.Empty;
                string Troubleshooting = string.Empty;
                string Peer = string.Empty;
                string Documentation = string.Empty;
                string Training = string.Empty;

                bool HasNew = false;
                bool HasNewTroubleshooting = false;
                bool HasNewPeerSuggestions = false;
                bool HasNewDocumentation = false;
                bool HasNewTraining = false;

                TbsExpanderP.Header = "Troubleshooting";
                PeerExpanderP.Header = "Peer Suggested Items";
                DocuExpanderP.Header = "Relevant Links / Documentation";
                TrainingExpanderP.Header = "Training Documents";

                TbsExpanderP.Background = OriginalBrush;
                TbsExpanderP.BorderBrush = OriginalBrush;
                TbsExpanderP.FontWeight = FontWeights.Normal;

                PeerExpanderP.Background = OriginalBrush;
                PeerExpanderP.BorderBrush = OriginalBrush;
                PeerExpanderP.FontWeight = FontWeights.Normal;

                DocuExpanderP.Background = OriginalBrush;
                DocuExpanderP.BorderBrush = OriginalBrush;
                DocuExpanderP.FontWeight = FontWeights.Normal;

                TrainingExpanderP.Background = OriginalBrush;
                TrainingExpanderP.BorderBrush = OriginalBrush;
                TrainingExpanderP.FontWeight = FontWeights.Normal;

                try
                {
                    foreach (string s in Directory.GetFiles(FileDirectory + "/Screenshots/"))
                    {
                        File.Delete(s);
                    }
                }
                catch (Exception exc)
                {
                    ErrorDialog error = new ErrorDialog();
                    error.ErrorText.Text = "Please ensure all screenshot files are closed. Unable to load screenshots if they exist.";
                    error.ShowDialog();
                }

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    TreeViewItem CatItem = origin.Parent as TreeViewItem;
                    TreeViewItem TechItem = CatItem.Parent as TreeViewItem;

                    string Technology = TechItem.Header.ToString(); ;
                    string Category = CatItem.Header.ToString();
                    string Issue = origin.Header.ToString().Replace(" * NEW", "");

                    string query = string.Format("SELECT * FROM Technologies WHERE Technology='{0}' AND Category='{1}' AND Issue='{2}'", Technology, Category, Issue);

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
                            HasNew = reader.IsDBNull(9) ? false : reader.GetBoolean(9);
                            HasNewTroubleshooting = reader.IsDBNull(10) ? false : reader.GetBoolean(10);
                            HasNewPeerSuggestions = reader.IsDBNull(11) ? false : reader.GetBoolean(11);
                            HasNewDocumentation = reader.IsDBNull(12) ? false : reader.GetBoolean(12);
                            HasNewTraining = reader.IsDBNull(13) ? false : reader.GetBoolean(13);
                        }
                    }

                    connection.Close();
                }

                if (HasNewTroubleshooting == true)
                {
                    TbsExpanderP.Header += " - NEW CONTENT AVAILABLE";
                    TbsExpanderP.Background = new SolidColorBrush(Colors.DarkGoldenrod);
                    TbsExpanderP.BorderBrush = new SolidColorBrush(Colors.DarkGoldenrod);
                    TbsExpanderP.FontWeight = FontWeights.Bold;
                }

                if (HasNewPeerSuggestions == true)
                {
                    PeerExpanderP.Header += " - NEW CONTENT AVAILABLE";
                    PeerExpanderP.Background = new SolidColorBrush(Colors.DarkGoldenrod);
                    PeerExpanderP.BorderBrush = new SolidColorBrush(Colors.DarkGoldenrod);
                    PeerExpanderP.FontWeight = FontWeights.Bold;
                }

                if (HasNewDocumentation == true)
                {
                    DocuExpanderP.Header += " - NEW CONTENT AVAILABLE";
                    DocuExpanderP.Background = new SolidColorBrush(Colors.DarkGoldenrod);
                    DocuExpanderP.BorderBrush = new SolidColorBrush(Colors.DarkGoldenrod);
                    DocuExpanderP.FontWeight = FontWeights.Bold;
                }

                if (HasNewTraining == true)
                {
                    TrainingExpanderP.Header += " - NEW CONTENT AVAILABLE";
                    TrainingExpanderP.Background = new SolidColorBrush(Colors.DarkGoldenrod);
                    TrainingExpanderP.BorderBrush = new SolidColorBrush(Colors.DarkGoldenrod);
                    TrainingExpanderP.FontWeight = FontWeights.Bold;
                }

                Troubleshooting.TrimEnd(';');
                Peer.TrimEnd(';');
                Documentation.TrimEnd(';');
                Training.TrimEnd(';');

                if (!string.IsNullOrWhiteSpace(Warning))
                {
                    WarningLabel.Visibility = Visibility.Visible;
                    WarningLabel.Text = Warning;
                }
                else
                {
                    WarningLabel.Visibility = Visibility.Collapsed;
                    WarningLabel.Text = "";
                }

                if (!string.IsNullOrWhiteSpace(Troubleshooting))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in Troubleshooting.Split(';'))
                    {
                        s.Replace("''", "'");

                        if (s.Contains("\""))
                        {
                            if (s.Trim().EndsWith("\""))
                            {
                                int start = s.IndexOf('"');
                                int end = s.LastIndexOf('"');

                                Run std = new Run();
                                std.Foreground = new SolidColorBrush(Colors.White);
                                std.Text = s.Substring(0, start).TrimEnd('"');

                                Run ln = new Run();
                                ln.FontStyle = FontStyles.Italic;
                                ln.Foreground = new SolidColorBrush(Colors.LightBlue);
                                ln.Text = s.TrimStart('"').Substring(start, end - start) + "\"";

                                TextBlock newText = new TextBlock();
                                newText.Inlines.Add(std);
                                newText.Inlines.Add(ln);
                                newText.TextWrapping = TextWrapping.Wrap;
                                newText.Margin = new Thickness(10, 10, 0, 0);
                                newText.Text.Trim();
                                newText.FontSize = 14;
                                TbsExpander.Children.Add(newText);
                            }
                            else
                            {
                                int start = s.IndexOf('"');
                                int end = s.LastIndexOf('"');

                                Run std = new Run();
                                std.Foreground = new SolidColorBrush(Colors.White);
                                std.Text = s.Substring(0, start).TrimEnd('"');

                                Run ln = new Run();
                                ln.FontStyle = FontStyles.Italic;
                                ln.Foreground = new SolidColorBrush(Colors.LightBlue);
                                ln.Text = s.TrimStart('"').Substring(start, end - start) + "\"";

                                Run std2 = new Run();
                                std2.Foreground = new SolidColorBrush(Colors.White);
                                std2.Text = " " + s.Substring(end, s.Length - end).TrimStart('"').Trim();

                                TextBlock newText = new TextBlock();
                                newText.Inlines.Add(std);
                                newText.Inlines.Add(ln);
                                newText.Inlines.Add(std2);
                                newText.TextWrapping = TextWrapping.Wrap;
                                newText.Margin = new Thickness(10, 10, 0, 0);
                                newText.Text.Trim();
                                newText.FontSize = 14;
                                TbsExpander.Children.Add(newText);
                            }
                        }

                        else
                        {
                            TextBlock newText = new TextBlock();
                            newText.Text = s.TrimEnd(); ;
                            newText.Foreground = new SolidColorBrush(Colors.White);
                            newText.TextWrapping = TextWrapping.Wrap;
                            newText.Margin = new Thickness(10, 10, 0, 0);
                            newText.FontSize = 14;
                            TbsExpander.Children.Add(newText);
                        }
                    }

                    List<string> images = GetImages();

                    if (images.Count > 0)
                    {
                        Expander imgExpander = new Expander();
                        imgExpander.Header = "Screenshots & Media";
                        imgExpander.Margin = new Thickness(10, 25, 10, 5);

                        WrapPanel content = new WrapPanel();
                        imgExpander.Content = content;

                        foreach (string str in images)
                        {
                            BitmapImage bimg = new BitmapImage();
                            bimg.BeginInit();
                            bimg.UriSource = new Uri(str, UriKind.Absolute);
                            bimg.CacheOption = BitmapCacheOption.OnLoad;
                            bimg.EndInit();

                            Image img = new Image();
                            img.Source = bimg;
                            img.Stretch = Stretch.UniformToFill;
                            img.Width = 300;
                            img.Height = 200;
                            img.Margin = new Thickness(0, 0, 5, 0);
                            img.HorizontalAlignment = HorizontalAlignment.Left;
                            img.VerticalAlignment = VerticalAlignment.Top;
                            img.PreviewMouseDown += Img_PreviewMouseDown;

                            content.Children.Add(img);
                        }

                        Button openExt = new Button();
                        openExt.Content = "Open folder in explorer";
                        openExt.VerticalAlignment = VerticalAlignment.Bottom;
                        openExt.HorizontalAlignment = HorizontalAlignment.Left;
                        openExt.Click += OpenExt_Click;

                        content.Children.Add(openExt);

                        TbsExpander.Children.Add(imgExpander);
                    }
                }

                if (!string.IsNullOrWhiteSpace(Peer))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in Peer.Split(';'))
                    {
                        s.Replace("''", "'");

                        if (s.Contains("\""))
                        {
                            int start = s.IndexOf('"');
                            int end = s.LastIndexOf('"');

                            Run std = new Run();
                            std.Foreground = new SolidColorBrush(Colors.White);
                            std.Text = s.Substring(0, start).TrimEnd('"');

                            Run ln = new Run();
                            ln.FontStyle = FontStyles.Italic;
                            ln.Foreground = new SolidColorBrush(Colors.LightBlue);
                            ln.Text = s.TrimStart('"').Substring(start, end - start) + "\"";

                            Run std2 = new Run();
                            std2.Foreground = new SolidColorBrush(Colors.White);
                            std2.Text = " " + s.Substring(end, s.Length - end).TrimStart('"').Trim();

                            TextBlock newText = new TextBlock();
                            newText.Inlines.Add(std);
                            newText.Inlines.Add(ln);
                            newText.Inlines.Add(std2);
                            newText.TextWrapping = TextWrapping.Wrap;
                            newText.Margin = new Thickness(10, 10, 0, 0);
                            newText.Text.Trim();
                            newText.FontSize = 14;
                            PeerExpander.Children.Add(newText);
                        }
                        else
                        {
                            TextBlock newText = new TextBlock();
                            newText.Text = s.TrimEnd();
                            newText.Foreground = new SolidColorBrush(Colors.White);
                            newText.TextWrapping = TextWrapping.Wrap;
                            newText.Margin = new Thickness(10, 10, 0, 0);
                            newText.FontSize = 14;
                            PeerExpander.Children.Add(newText);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(Documentation))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in Documentation.Split(';'))
                    {
                        s.Replace("''", "'");

                        string Topic = s.Split('|')[0];
                        string Link = s.Split('|')[1];

                        //Button button = new Button();
                        //button.Foreground = new SolidColorBrush(Colors.LightBlue);
                        //button.Content = Topic.Trim().Trim(':');
                        //button.CommandParameter = Link.Trim().Trim(':');
                        //button.Click += LinkButton_Click;
                        //button.Margin = new Thickness(10, 5, 0, 0);
                        //DocuExpander.Children.Add(button);

                        TextBlock newText = new TextBlock();
                        Hyperlink hyperlink = new Hyperlink();
                        Hyperlink copylink = new Hyperlink();
                        hyperlink.NavigateUri = new Uri(Link.Trim().Trim(':'));
                        hyperlink.Click += Hyperlink_Click;
                        hyperlink.Inlines.Add(Topic.Trim().Trim(':'));
                        hyperlink.FontSize = 14;
                        hyperlink.Foreground = new SolidColorBrush(Colors.LightBlue);
                        copylink.NavigateUri = new Uri(Link.Trim().Trim(':'));
                        copylink.Click += Copylink_Click;
                        copylink.Inlines.Add("copy link");
                        copylink.FontSize = 12;
                        copylink.Foreground = new SolidColorBrush(Colors.LightBlue);
                        newText.Inlines.Add(hyperlink);
                        newText.Inlines.Add("     ");
                        newText.Inlines.Add(copylink);
                        newText.Margin = new Thickness(10, 10, 0, 0);
                        DocuExpander.Children.Add(newText);
                    }
                }

                if (!string.IsNullOrWhiteSpace(Training))
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in Training.Split(';'))
                    {
                        s.Replace("''", "'");

                        //Button button = new Button();
                        //button.Foreground = new SolidColorBrush(Colors.LightBlue);
                        //button.Content = Topic.Trim().Trim(':');
                        //button.CommandParameter = Link.Trim().Trim(':');
                        //button.Click += LinkButton_Click;
                        //button.Margin = new Thickness(10, 5, 0, 0);
                        //DocuExpander.Children.Add(button);

                        TextBlock newText = new TextBlock();
                        Hyperlink hyperlink = new Hyperlink();
                        Hyperlink copylink = new Hyperlink();
                        hyperlink.NavigateUri = new Uri(s.Trim().Trim(':'));
                        hyperlink.Click += Hyperlink_Click;
                        hyperlink.Inlines.Add(s.Trim().Trim(':'));
                        hyperlink.FontSize = 14;
                        hyperlink.Foreground = new SolidColorBrush(Colors.LightBlue);
                        copylink.NavigateUri = new Uri(s.Trim().Trim(':'));
                        copylink.Click += Copylink_Click;
                        copylink.Inlines.Add("copy link");
                        copylink.FontSize = 12;
                        copylink.Foreground = new SolidColorBrush(Colors.LightBlue);
                        newText.Inlines.Add(hyperlink);
                        newText.Inlines.Add("     ");
                        newText.Inlines.Add(copylink);
                        newText.Margin = new Thickness(10, 10, 0, 0);
                        TrainingExpander.Children.Add(newText);
                    }
                }

                if (!string.IsNullOrWhiteSpace(Training) || !string.IsNullOrWhiteSpace(Documentation) || !string.IsNullOrWhiteSpace(Peer) || !string.IsNullOrWhiteSpace(Troubleshooting))
                {
                    //LoadingLabel.Foreground = new SolidColorBrush(Colors.LimeGreen);
                    //LoadingLabel.Text = "Data Found! Done loading.";

                    NoDataGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    //LoadingLabel.Foreground = new SolidColorBrush(Colors.Red);
                    //LoadingLabel.Text = "No data. Done loading.";

                    NoDataGrid.Visibility = Visibility.Visible;
                }

                Button FeedbackButton0 = new Button()
                {
                    Margin = new Thickness(0, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Content = "Suggest Changes or Additions",
                    ClickMode = ClickMode.Release,
                };

                Button FeedbackButton1 = new Button()
                {
                    Margin = new Thickness(0, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Content = "Suggest Changes or Additions",
                    ClickMode = ClickMode.Release,
                };

                Button FeedbackButton2 = new Button()
                {
                    Margin = new Thickness(0, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Content = "Suggest Changes or Additions",
                    ClickMode = ClickMode.Release,
                };

                Button FeedbackButton3 = new Button()
                {
                    Margin = new Thickness(0, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Content = "Suggest Changes or Additions",
                    ClickMode = ClickMode.Release,
                };

                FeedbackButton0.Click += FeedbackButton_Click;
                FeedbackButton1.Click += FeedbackButton_Click;
                FeedbackButton2.Click += FeedbackButton_Click;
                FeedbackButton3.Click += FeedbackButton_Click;

                TbsExpander.Children.Add(FeedbackButton0);
                PeerExpander.Children.Add(FeedbackButton1);
                DocuExpander.Children.Add(FeedbackButton2);
                TrainingExpander.Children.Add(FeedbackButton3);

                TbsExpanderP.IsExpanded = true;
            }
            catch (Exception exc)
            {
                ErrorDialog err = new ErrorDialog();
                err.ErrorText.Text = exc.ToString() + "\n\n" + exc.Message + "\n\n" + exc.StackTrace;
                err.ShowDialog();
            }
        }

        private void ExpandAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (TreeViewItem item in DetailsPane.Items)
            {
                item.IsExpanded = true;

                foreach (TreeViewItem item2 in item.Items)
                {
                    item2.IsExpanded = true;
                }
            }
        }

        private void ExpandAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (TreeViewItem item in DetailsPane.Items)
            {
                foreach (TreeViewItem item2 in item.Items)
                {
                    item2.IsExpanded = false;
                }

                item.IsExpanded = false;
            }
        }
        #endregion

        #region Old Data Pulling Method
        //private void TechnologyChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //IssueBox.ItemsSource = null;
        //    //CategoryBox.ItemsSource = null;
        //    DetailsPane.ItemsSource = null;

        //    //IssueBox.SelectedIndex = -1;
        //    //CategoryBox.SelectedIndex = -1;

        //    Categories.Clear();
        //    Issues.Clear();

        //    CategoriesTV.Clear();
        //    IssuesTV.Clear();

        //    DetailsPaneFlyout.IsOpen = true;

        //    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
        //    {
        //        connection.Open();

        //        string query = string.Format("SELECT Category,HasNew FROM Technologies WHERE Technology='{0}'", TechnologyBox.SelectedValue.ToString().Split(':')[1].Trim());
        //        string query2 = "SELECT Issue,HasNew FROM Technologies WHERE Technology='{0}' AND Category='{1}'";

        //        using (SqlCommand command = new SqlCommand(query, connection))
        //        {
        //            using (SqlDataReader reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    Debug.WriteLine(reader.GetString(0));
        //                    if (!reader.IsDBNull(1))
        //                        Debug.Write(" - " + reader.GetBoolean(1));
        //                    else
        //                        Debug.Write(" - Null");

        //                    ComboBoxItem newItem = new ComboBoxItem();
        //                    TreeViewItem newTVItem = new TreeViewItem();

        //                    if (!Categories.Any(t => t.Content.ToString() == reader.GetString(0)))
        //                    {
        //                        newItem.Content = reader.GetString(0);
        //                        newTVItem.Header = reader.GetString(0);

        //                        //if (!reader.IsDBNull(1) && reader.GetBoolean(1) == true)
        //                        //{
        //                        //    newItem.Content += " * NEW";
        //                        //    newTVItem.FontWeight = FontWeights.Bold;
        //                        //    newItem.Foreground = new SolidColorBrush(Colors.Gold);

        //                        //    newTVItem.Header += " * NEW";
        //                        //    newTVItem.FontWeight = FontWeights.Bold;
        //                        //    newTVItem.Foreground = new SolidColorBrush(Colors.Gold);
        //                        //}

        //                        Categories.Add(newItem);
        //                        CategoriesTV.Add(newTVItem);
        //                    }

        //                    //if (Categories.Any(t => t.Content.ToString() == reader.GetString(0)))
        //                    //{
        //                    //    if (!reader.IsDBNull(1) && reader.GetBoolean(1))
        //                    //    {
        //                    //        ComboBoxItem item = Categories.Select(i => i.Content.ToString() == reader.GetString(0)) as ComboBoxItem;
        //                    //    }
        //                    //}
        //                }
        //            }
        //        }

        //        foreach (TreeViewItem item in CategoriesTV)
        //        {
        //            string newQuery = string.Format(query2, TechnologyBox.SelectedValue.ToString().Split(':')[1].Trim(), item.Header.ToString().Replace(" * NEW", ""));

        //            using (SqlCommand command = new SqlCommand(newQuery, connection))
        //            {
        //                List<TreeViewItem> sub = new List<TreeViewItem>();

        //                using (SqlDataReader reader = command.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        TreeViewItem newItem = new TreeViewItem();

        //                        if (!Issues.Any(t => t.Content.ToString() == reader.GetString(0)))
        //                        {
        //                            newItem.Header = reader.GetString(0);

        //                            if (!reader.IsDBNull(1) && reader.GetBoolean(1) == true)
        //                            {
        //                                newItem.Header += " * NEW";
        //                                newItem.FontWeight = FontWeights.Bold;
        //                                newItem.Foreground = new SolidColorBrush(Colors.Gold);
        //                            }
        //                        }

        //                        sub.Add(newItem);
        //                    }
        //                }

        //                item.ItemsSource = sub;

        //            }
        //        }

        //        connection.Close();
        //    }

        //    var newList = Categories.OrderBy(t => t.Content).ToList();
        //    var newList2 = CategoriesTV.OrderBy(t => t.Header).ToList();

        //    CategoryBox.ItemsSource = newList;
        //    DetailsPane.ItemsSource = newList2;

        //    CatIssueSuggestionButton.IsEnabled = true;
        //    newCat.Reload(CurrentTechnology);
        //    CatIssueUserControl.Content = newCat;
        //}

        //private void CategoryChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (CategoryBox.SelectedIndex > -1)
        //    {
        //        try
        //        {
        //            IssueBox.ItemsSource = null;

        //            Issues.Clear();

        //            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
        //            {
        //                connection.Open();

        //                string query = string.Format("SELECT Issue,HasNew FROM Technologies WHERE Technology='{0}' AND Category='{1}'",
        //                    TechnologyBox.SelectedValue.ToString().Split(':')[1].Trim(), CategoryBox.SelectedValue.ToString().Replace(" - NEW DATA", "").Split(':')[1].Trim());

        //                using (SqlCommand command = new SqlCommand(query, connection))
        //                {
        //                    using (SqlDataReader reader = command.ExecuteReader())
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            ComboBoxItem newItem = new ComboBoxItem();

        //                            if (!Issues.Any(t => t.Content.ToString() == reader.GetString(0)))
        //                            {
        //                                newItem.Content = reader.GetString(0);

        //                                if (!reader.IsDBNull(1) && reader.GetBoolean(1) == true)
        //                                {
        //                                    newItem.Content += " * NEW";
        //                                    newItem.Foreground = new SolidColorBrush(Colors.Gold);
        //                                }

        //                                Issues.Add(newItem);
        //                            }
        //                        }
        //                    }
        //                }

        //                connection.Close();
        //            }

        //            var newList = Issues.OrderBy(t => t.Content.ToString());

        //            //IssueBox.ItemsSource = newList;
        //        }
        //        catch (Exception exc) { }
        //    }
        //}

        //private void Submit_Clicked(object sender, RoutedEventArgs e)
        //{
        //    //LoadingLabel.Foreground = new SolidColorBrush(Colors.White);
        //    //LoadingLabel.Text = "Loading...";

        //    //if (TechnologyBox.SelectedIndex > -1 && CategoryBox.SelectedIndex > -1 && IssueBox.SelectedIndex > -1)
        //    //{
        //        try
        //        {
        //            TbsExpander.Children.Clear();
        //            PeerExpander.Children.Clear();
        //            DocuExpander.Children.Clear();
        //            TrainingExpander.Children.Clear();

        //            string Warning = string.Empty;
        //            string Troubleshooting = string.Empty;
        //            string Peer = string.Empty;
        //            string Documentation = string.Empty;
        //            string Training = string.Empty;

        //            bool HasNew = false;
        //            bool HasNewTroubleshooting = false;
        //            bool HasNewPeerSuggestions = false;
        //            bool HasNewDocumentation = false;
        //            bool HasNewTraining = false;

        //            TbsExpanderP.Header = "Troubleshooting";
        //            PeerExpanderP.Header = "Peer Suggested Items";
        //            DocuExpanderP.Header = "Relevant Links / Documentation";
        //            TrainingExpanderP.Header = "Training Documents";

        //            TbsExpanderP.Background = OriginalBrush;
        //            TbsExpanderP.BorderBrush = OriginalBrush;
        //            TbsExpanderP.FontWeight = FontWeights.Normal;

        //            PeerExpanderP.Background = OriginalBrush;
        //            PeerExpanderP.BorderBrush = OriginalBrush;
        //            PeerExpanderP.FontWeight = FontWeights.Normal;

        //            DocuExpanderP.Background = OriginalBrush;
        //            DocuExpanderP.BorderBrush = OriginalBrush;
        //            DocuExpanderP.FontWeight = FontWeights.Normal;

        //            TrainingExpanderP.Background = OriginalBrush;
        //            TrainingExpanderP.BorderBrush = OriginalBrush;
        //            TrainingExpanderP.FontWeight = FontWeights.Normal;

        //            try
        //            {
        //                foreach (string s in Directory.GetFiles(FileDirectory + "/Screenshots/"))
        //                {
        //                    File.Delete(s);
        //                }
        //            }
        //            catch (Exception exc)
        //            {
        //                ErrorDialog error = new ErrorDialog();
        //                error.ErrorText.Text = "Please ensure all screenshot files are closed. Unable to load screenshots if they exist.";
        //                error.ShowDialog();
        //            }

        //            //CurrentTechnology = TechnologyBox.SelectedValue.ToString();
        //            //CurrentCategory = CategoryBox.SelectedValue.ToString();
        //            //CurrentIssue = IssueBox.SelectedValue.ToString();

        //            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
        //            {
        //                connection.Open();

        //                string query = string.Format("SELECT * FROM Technologies WHERE Technology='{0}' AND Category='{1}' AND Issue='{2}'", TechnologyBox.SelectedValue.ToString().Split(':')[1].Trim(),
        //                    CategoryBox.SelectedValue.ToString().Split(':')[1].Trim().Replace(" - NEW DATA", ""), IssueBox.SelectedValue.ToString().Replace(" - NEW DATA", "").Split(':')[1].Trim());

        //                using (SqlCommand command = new SqlCommand(query, connection))
        //                {
        //                    using (SqlDataReader reader = command.ExecuteReader())
        //                    {
        //                        reader.Read();

        //                        Warning = reader.IsDBNull(4) ? "" : reader.GetString(4);
        //                        Troubleshooting = reader.IsDBNull(5) ? "" : reader.GetString(5);
        //                        Peer = reader.IsDBNull(6) ? "" : reader.GetString(6);
        //                        Documentation = reader.IsDBNull(7) ? "" : reader.GetString(7);
        //                        Training = reader.IsDBNull(8) ? "" : reader.GetString(8);
        //                        HasNew = reader.IsDBNull(9) ? false : reader.GetBoolean(9);
        //                        HasNewTroubleshooting = reader.IsDBNull(10) ? false : reader.GetBoolean(10);
        //                        HasNewPeerSuggestions = reader.IsDBNull(11) ? false : reader.GetBoolean(11);
        //                        HasNewDocumentation = reader.IsDBNull(12) ? false : reader.GetBoolean(12);
        //                        HasNewTraining = reader.IsDBNull(13) ? false : reader.GetBoolean(13);
        //                    }
        //                }

        //                connection.Close();
        //            }

        //            if (HasNewTroubleshooting == true)
        //            {
        //                TbsExpanderP.Header += " - NEW CONTENT AVAILABLE";
        //                TbsExpanderP.Background = new SolidColorBrush(Colors.Gold);
        //                TbsExpanderP.BorderBrush = new SolidColorBrush(Colors.Gold);
        //                TbsExpanderP.FontWeight = FontWeights.Bold;
        //            }

        //            if (HasNewPeerSuggestions == true)
        //            {
        //                PeerExpanderP.Header += " - NEW CONTENT AVAILABLE";
        //                PeerExpanderP.Background = new SolidColorBrush(Colors.Gold);
        //                PeerExpanderP.BorderBrush = new SolidColorBrush(Colors.Gold);
        //                PeerExpanderP.FontWeight = FontWeights.Bold;
        //            }

        //            if (HasNewDocumentation == true)
        //            {
        //                DocuExpanderP.Header += " - NEW CONTENT AVAILABLE";
        //                DocuExpanderP.Background = new SolidColorBrush(Colors.Gold);
        //                DocuExpanderP.BorderBrush = new SolidColorBrush(Colors.Gold);
        //                DocuExpanderP.FontWeight = FontWeights.Bold;
        //            }

        //            if (HasNewTraining == true)
        //            {
        //                TrainingExpanderP.Header += " - NEW CONTENT AVAILABLE";
        //                TrainingExpanderP.Background = new SolidColorBrush(Colors.Gold);
        //                TrainingExpanderP.BorderBrush = new SolidColorBrush(Colors.Gold);
        //                TrainingExpanderP.FontWeight = FontWeights.Bold;
        //            }

        //            Troubleshooting.TrimEnd(';');
        //            Peer.TrimEnd(';');
        //            Documentation.TrimEnd(';');
        //            Training.TrimEnd(';');

        //            if (!string.IsNullOrWhiteSpace(Warning))
        //            {
        //                WarningLabel.Visibility = Visibility.Visible;
        //                WarningLabel.Text = Warning;
        //            }
        //            else
        //            {
        //                WarningLabel.Visibility = Visibility.Collapsed;
        //                WarningLabel.Text = "";
        //            }

        //            if (!string.IsNullOrWhiteSpace(Troubleshooting))
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                foreach (string s in Troubleshooting.Split(';'))
        //                {
        //                    if (s.Contains("\""))
        //                    {
        //                        if (s.Trim().EndsWith("\""))
        //                        {
        //                            int start = s.IndexOf('"');
        //                            int end = s.LastIndexOf('"');

        //                            Run std = new Run();
        //                            std.Foreground = new SolidColorBrush(Colors.White);
        //                            std.Text = s.Substring(0, start).TrimEnd('"');

        //                            Run ln = new Run();
        //                            ln.FontStyle = FontStyles.Italic;
        //                            ln.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                            ln.Text = s.TrimStart('"').Substring(start, end - start) + "\"";

        //                            TextBlock newText = new TextBlock();
        //                            newText.Inlines.Add(std);
        //                            newText.Inlines.Add(ln);
        //                            newText.TextWrapping = TextWrapping.Wrap;
        //                            newText.Margin = new Thickness(10, 10, 0, 0);
        //                            newText.Text.Trim();
        //                            newText.FontSize = 14;
        //                            TbsExpander.Children.Add(newText);
        //                        }
        //                        else
        //                        {
        //                            int start = s.IndexOf('"');
        //                            int end = s.LastIndexOf('"');

        //                            Run std = new Run();
        //                            std.Foreground = new SolidColorBrush(Colors.White);
        //                            std.Text = s.Substring(0, start).TrimEnd('"');

        //                            Run ln = new Run();
        //                            ln.FontStyle = FontStyles.Italic;
        //                            ln.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                            ln.Text = s.TrimStart('"').Substring(start, end - start) + "\"";

        //                            Run std2 = new Run();
        //                            std2.Foreground = new SolidColorBrush(Colors.White);
        //                            std2.Text = " " + s.Substring(end, s.Length - end).TrimStart('"').Trim() ;

        //                            TextBlock newText = new TextBlock();
        //                            newText.Inlines.Add(std);
        //                            newText.Inlines.Add(ln);
        //                            newText.Inlines.Add(std2);
        //                            newText.TextWrapping = TextWrapping.Wrap;
        //                            newText.Margin = new Thickness(10, 10, 0, 0);
        //                            newText.Text.Trim();
        //                            newText.FontSize = 14;
        //                            TbsExpander.Children.Add(newText);
        //                        }
        //                    }

        //                    else
        //                    {
        //                        TextBlock newText = new TextBlock();
        //                        newText.Text = s.TrimEnd(); ;
        //                        newText.Foreground = new SolidColorBrush(Colors.White);
        //                        newText.TextWrapping = TextWrapping.Wrap;
        //                        newText.Margin = new Thickness(10, 10, 0, 0);
        //                        newText.FontSize = 14;
        //                        TbsExpander.Children.Add(newText);
        //                    }
        //                }

        //                List<string> images = GetImages();

        //                if (images.Count > 0)
        //                {
        //                    Expander imgExpander = new Expander();
        //                    imgExpander.Header = "Screenshots & Media";
        //                    imgExpander.Margin = new Thickness(10, 25, 10, 5);

        //                    WrapPanel content = new WrapPanel();
        //                    imgExpander.Content = content;

        //                    foreach (string str in images)
        //                    {
        //                        BitmapImage bimg = new BitmapImage();
        //                        bimg.BeginInit();
        //                        bimg.UriSource = new Uri(str, UriKind.Absolute);
        //                        bimg.CacheOption = BitmapCacheOption.OnLoad;
        //                        bimg.EndInit();

        //                        Image img = new Image();
        //                        img.Source = bimg;
        //                        img.Stretch = Stretch.UniformToFill;
        //                        img.Width = 300;
        //                        img.Height = 200;
        //                        img.Margin = new Thickness(0, 0, 5, 0);
        //                        img.HorizontalAlignment = HorizontalAlignment.Left;
        //                        img.VerticalAlignment = VerticalAlignment.Top;
        //                        img.PreviewMouseDown += Img_PreviewMouseDown;

        //                        content.Children.Add(img);
        //                    }

        //                    Button openExt = new Button();
        //                    openExt.Content = "Open folder in explorer";
        //                    openExt.VerticalAlignment = VerticalAlignment.Bottom;
        //                    openExt.HorizontalAlignment = HorizontalAlignment.Left;
        //                    openExt.Click += OpenExt_Click;

        //                    content.Children.Add(openExt);

        //                    TbsExpander.Children.Add(imgExpander);
        //                }
        //            }

        //            if (!string.IsNullOrWhiteSpace(Peer))
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                foreach (string s in Peer.Split(';'))
        //                {
        //                    if (s.Contains("\""))
        //                    {
        //                        int start = s.IndexOf('"');
        //                        int end = s.LastIndexOf('"');

        //                        Run std = new Run();
        //                        std.Foreground = new SolidColorBrush(Colors.White);
        //                        std.Text = s.Substring(0, start).TrimEnd('"');

        //                        Run ln = new Run();
        //                        ln.FontStyle = FontStyles.Italic;
        //                        ln.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                        ln.Text = s.TrimStart('"').Substring(start, end - start) + "\"";

        //                        Run std2 = new Run();
        //                        std2.Foreground = new SolidColorBrush(Colors.White);
        //                        std2.Text = " " + s.Substring(end, s.Length - end).TrimStart('"').Trim();

        //                        TextBlock newText = new TextBlock();
        //                        newText.Inlines.Add(std);
        //                        newText.Inlines.Add(ln);
        //                        newText.Inlines.Add(std2);
        //                        newText.TextWrapping = TextWrapping.Wrap;
        //                        newText.Margin = new Thickness(10, 10, 0, 0);
        //                        newText.Text.Trim();
        //                        newText.FontSize = 14;
        //                        PeerExpander.Children.Add(newText);
        //                    }
        //                    else
        //                    {
        //                        TextBlock newText = new TextBlock();
        //                        newText.Text = s.TrimEnd();
        //                        newText.Foreground = new SolidColorBrush(Colors.White);
        //                        newText.TextWrapping = TextWrapping.Wrap;
        //                        newText.Margin = new Thickness(10, 10, 0, 0);
        //                        newText.FontSize = 14;
        //                        PeerExpander.Children.Add(newText);
        //                    }
        //                }
        //            }

        //            if (!string.IsNullOrWhiteSpace(Documentation))
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                foreach (string s in Documentation.Split(';'))
        //                {
        //                    string Topic = s.Split('|')[0];
        //                    string Link = s.Split('|')[1];

        //                    //Button button = new Button();
        //                    //button.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                    //button.Content = Topic.Trim().Trim(':');
        //                    //button.CommandParameter = Link.Trim().Trim(':');
        //                    //button.Click += LinkButton_Click;
        //                    //button.Margin = new Thickness(10, 5, 0, 0);
        //                    //DocuExpander.Children.Add(button);

        //                    TextBlock newText = new TextBlock();
        //                    Hyperlink hyperlink = new Hyperlink();
        //                    Hyperlink copylink = new Hyperlink();
        //                    hyperlink.NavigateUri = new Uri(Link.Trim().Trim(':'));
        //                    hyperlink.Click += Hyperlink_Click;
        //                    hyperlink.Inlines.Add(Topic.Trim().Trim(':'));
        //                    hyperlink.FontSize = 14;
        //                    hyperlink.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                    copylink.NavigateUri = new Uri(Link.Trim().Trim(':'));
        //                    copylink.Click += Copylink_Click;
        //                    copylink.Inlines.Add("copy link");
        //                    copylink.FontSize = 12;
        //                    copylink.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                    newText.Inlines.Add(hyperlink);
        //                    newText.Inlines.Add("     ");
        //                    newText.Inlines.Add(copylink);
        //                    newText.Margin = new Thickness(10, 10, 0, 0);
        //                    DocuExpander.Children.Add(newText);
        //                }
        //            }

        //            if (!string.IsNullOrWhiteSpace(Training))
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                foreach (string s in Training.Split(';'))
        //                {
        //                    //Button button = new Button();
        //                    //button.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                    //button.Content = Topic.Trim().Trim(':');
        //                    //button.CommandParameter = Link.Trim().Trim(':');
        //                    //button.Click += LinkButton_Click;
        //                    //button.Margin = new Thickness(10, 5, 0, 0);
        //                    //DocuExpander.Children.Add(button);

        //                    TextBlock newText = new TextBlock();
        //                    Hyperlink hyperlink = new Hyperlink();
        //                    Hyperlink copylink = new Hyperlink();
        //                    hyperlink.NavigateUri = new Uri(s.Trim().Trim(':'));
        //                    hyperlink.Click += Hyperlink_Click;
        //                    hyperlink.Inlines.Add(s.Trim().Trim(':'));
        //                    hyperlink.FontSize = 14;
        //                    hyperlink.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                    copylink.NavigateUri = new Uri(s.Trim().Trim(':'));
        //                    copylink.Click += Copylink_Click;
        //                    copylink.Inlines.Add("copy link");
        //                    copylink.FontSize = 12;
        //                    copylink.Foreground = new SolidColorBrush(Colors.LightBlue);
        //                    newText.Inlines.Add(hyperlink);
        //                    newText.Inlines.Add("     ");
        //                    newText.Inlines.Add(copylink);
        //                    newText.Margin = new Thickness(10, 10, 0, 0);
        //                    TrainingExpander.Children.Add(newText);
        //                }
        //            }

        //            if (!string.IsNullOrWhiteSpace(Training) || !string.IsNullOrWhiteSpace(Documentation) || !string.IsNullOrWhiteSpace(Peer) || !string.IsNullOrWhiteSpace(Troubleshooting))
        //            {
        //                //LoadingLabel.Foreground = new SolidColorBrush(Colors.LimeGreen);
        //                //LoadingLabel.Text = "Data Found! Done loading.";

        //                NoDataGrid.Visibility = Visibility.Collapsed;
        //            }
        //            else
        //            {
        //                //LoadingLabel.Foreground = new SolidColorBrush(Colors.Red);
        //                //LoadingLabel.Text = "No data. Done loading.";

        //                NoDataGrid.Visibility = Visibility.Visible;
        //            }

        //            Button FeedbackButton0 = new Button()
        //            {
        //                Margin = new Thickness(0, 10, 10, 10),
        //                HorizontalAlignment = HorizontalAlignment.Right,
        //                VerticalAlignment = VerticalAlignment.Bottom,
        //                Content = "Suggest Changes or Additions",
        //                ClickMode = ClickMode.Release,
        //            };

        //            Button FeedbackButton1 = new Button()
        //            {
        //                Margin = new Thickness(0, 10, 10, 10),
        //                HorizontalAlignment = HorizontalAlignment.Right,
        //                VerticalAlignment = VerticalAlignment.Bottom,
        //                Content = "Suggest Changes or Additions",
        //                ClickMode = ClickMode.Release,
        //            };

        //            Button FeedbackButton2 = new Button()
        //            {
        //                Margin = new Thickness(0, 10, 10, 10),
        //                HorizontalAlignment = HorizontalAlignment.Right,
        //                VerticalAlignment = VerticalAlignment.Bottom,
        //                Content = "Suggest Changes or Additions",
        //                ClickMode = ClickMode.Release,
        //            };

        //            Button FeedbackButton3 = new Button()
        //            {
        //                Margin = new Thickness(0, 10, 10, 10),
        //                HorizontalAlignment = HorizontalAlignment.Right,
        //                VerticalAlignment = VerticalAlignment.Bottom,
        //                Content = "Suggest Changes or Additions",
        //                ClickMode = ClickMode.Release,
        //            };

        //            FeedbackButton0.Click += FeedbackButton_Click;
        //            FeedbackButton1.Click += FeedbackButton_Click;
        //            FeedbackButton2.Click += FeedbackButton_Click;
        //            FeedbackButton3.Click += FeedbackButton_Click;

        //            TbsExpander.Children.Add(FeedbackButton0);
        //            PeerExpander.Children.Add(FeedbackButton1);
        //            DocuExpander.Children.Add(FeedbackButton2);
        //            TrainingExpander.Children.Add(FeedbackButton3);
        //        }
        //        catch (Exception exc)
        //        {
        //            ErrorDialog err = new ErrorDialog();
        //            err.ErrorText.Text = exc.ToString() + "\n\n" + exc.Message + "\n\n" + exc.StackTrace;
        //            err.ShowDialog();
        //        }
        //    //}
        //}
        #endregion

        private void CheckVersion(TextBox sender, TextChangedEventArgs args)
        {

        }



        private void OpenExt_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(FileDirectory + "/Screenshots/");
        }

        private void Img_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Image origin = new Image();
            origin.Source = (sender as Image).Source;
            origin.HorizontalAlignment = HorizontalAlignment.Stretch;
            origin.VerticalAlignment = VerticalAlignment.Stretch;
            origin.Stretch = Stretch.Uniform;

            ImageOverview.Children.Add(origin);
            ImageOverview.Visibility = Visibility.Visible;
            ImageOverview.IsHitTestVisible = true;
            ImageOverview.PreviewMouseDown += ImageOverview_PreviewMouseDown;
        }

        private void ImageOverview_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ImageOverview.Children.RemoveAt(1);
            ImageOverview.Visibility = Visibility.Collapsed;
            ImageOverview.IsHitTestVisible = false;
            ImageOverview.PreviewMouseDown -= ImageOverview_PreviewMouseDown;
        }

        private List<string> GetImages()
        {
            List<string> images = new List<string>();

            CloudStorageAccount storageAccount;
            CloudBlobClient blobClient;
            CloudBlobContainer blobContainer;
            CloudBlobDirectory directory;

            List<IListBlobItem> blobs = new List<IListBlobItem>();

            string blobReference = CurrentTechnology + "/" + CurrentCategory.Replace('/', ' ') + "/" + CurrentIssue.Replace('/', ' ') + "/" + "Troubleshooting" + "/" + (string.Empty.EndsWith(".png") ? ".png" : ".jpg");

            if (CloudStorageAccount.TryParse(Blob_Controller.ConnectionString, out storageAccount))
            {
                blobClient = storageAccount.CreateCloudBlobClient();
                blobContainer = blobClient.GetContainerReference("tbg-media-storage");
                directory = blobContainer.GetDirectoryReference(CurrentTechnology + "/" + CurrentCategory.Replace('/', ' ') + "/" + CurrentIssue.Replace('/', ' ') + "/" + "Troubleshooting" + "/");
                blobs = directory.ListBlobs().ToList();

                foreach (IListBlobItem blob in blobs)
                {
                    if (blob is CloudBlockBlob)
                    {
                        FileStream ioStream = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/Screenshots/" + (blob as CloudBlockBlob).Name.Split('/')[4], FileMode.Create);
                        (blob as CloudBlockBlob).DownloadToStream(ioStream, null, null, null);
                        ioStream.Close();

                        images.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/Screenshots/" + (blob as CloudBlockBlob).Name.Split('/')[4]);
                    }
                }
            }

            return images;
        }

        private void Copylink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink source = sender as Hyperlink;
            Clipboard.SetText(source.NavigateUri.ToString());

            source.Inlines.Clear();
            source.Inlines.Add("copied");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink source = sender as Hyperlink;
            System.Diagnostics.Process.Start(source.NavigateUri.ToString());
        }

        private async void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            SuggestionWindow form = new SuggestionWindow(this, CurrentTechnology, CurrentCategory, CurrentIssue);
            form.Left = this.Left + 665;
            form.Top = this.Top + 200;
            form.ShowDialog();
        }

        private void VersionChecker_RunCheck(object sender, KeyEventArgs e)
        {
            TextChangedEventArgs eve = null;

            VersionBox_TextChanged(null, eve);
        }

        private void VersionBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SolidColorBrush _green = new SolidColorBrush(Colors.Green);
            SolidColorBrush _red = new SolidColorBrush(Colors.Red);

            const string SAGood = "Semi-Annual: Up-to-date";
            const string SABad = "Semi-Annual: OUT DATED";

            const string MGood = "Monthly: Up-to-date";
            const string MBad = "Monthly: OUT DATED";

            List<string> AcceptedVersions = new List<string>();

            bool isVolumeLicensing = false;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("SELECT * FROM Versions WHERE Product='{0}'", VersionCombo.SelectedValue.ToString().Split(':')[1].Trim());

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();

                        if (reader.IsDBNull(1))
                        {
                            isVolumeLicensing = true;
                        }

                        if (isVolumeLicensing)
                        {
                            AcceptedVersions.Add(reader.IsDBNull(2) ? "" : reader.GetString(2));
                            AcceptedVersions.Add(reader.IsDBNull(3) ? "" : reader.GetString(3));
                            AcceptedVersions.Add(reader.IsDBNull(4) ? "" : reader.GetString(4));
                            AcceptedVersions.Add(reader.IsDBNull(5) ? "" : reader.GetString(5));
                            AcceptedVersions.Add(reader.IsDBNull(6) ? "" : reader.GetString(6));
                        }
                        else
                        {
                            AcceptedVersions.Add(reader.GetString(1));
                            AcceptedVersions.Add(reader.IsDBNull(2) ? "" : reader.GetString(2));
                            AcceptedVersions.Add(reader.IsDBNull(3) ? "" : reader.GetString(3));
                            AcceptedVersions.Add(reader.IsDBNull(4) ? "" : reader.GetString(4));
                            AcceptedVersions.Add(reader.IsDBNull(5) ? "" : reader.GetString(5));
                            AcceptedVersions.Add(reader.IsDBNull(6) ? "" : reader.GetString(6));
                        }
                    }
                }

                connection.Close();
            }

            string _ver = VersionBox.Text.Trim();

            int index = VersionCombo.SelectedIndex;

            if (index == 0)
            {
                if (AcceptedVersions.Any(av => av == _ver))
                {
                    SALabel.Foreground = _green;
                    SALabel.Text = SAGood;

                    if (AcceptedVersions[1] == _ver)
                    {
                        MLabel.Foreground = _green;
                        MLabel.Text = MGood;
                    }
                }
                else
                {
                    SALabel.Foreground = _red;
                    MLabel.Foreground = _red;

                    SALabel.Text = SABad;
                    MLabel.Text = MBad;
                }
            }
            else if (index > 0 && index <= 8)
            {
                if (AcceptedVersions.Any(av => av == _ver.ToString()))
                {
                    MLabel.Foreground = _green;
                    MLabel.Text = "This version is up-to-date";
                    SALabel.Text = "";
                }
                else
                {
                    MLabel.Foreground = _red;
                    MLabel.Text = "This version is OUT DATED";
                    SALabel.Text = "";
                }
            }
            else
            {
                MLabel.Foreground = _red;
                MLabel.Text = "SELECT A VERSION FIRST";
            }
        }

        private void AdminConsoleOpen(object sender, RoutedEventArgs e)
        {

            if (RequiresAdminPassword == false)
            {

                AdminWindow aw = new AdminWindow();
                aw.Show();

            }

        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void NewCatIssueButton_Click(object sender, RoutedEventArgs e)
        {
            CatIssueGrid.Visibility = Visibility.Visible;
            CatIssueGrid.IsHitTestVisible = true;
        }


        /// <summary>
        /// Call AcquireToken - to acquire a token requiring user to sign-in
        /// </summary>
        private async void AquireToken()
        {
            AuthenticationResult authResult = null;
            var app = App.PublicClientApp;

            var accounts = await app.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent.
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await app.AcquireTokenInteractive(scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    ErrorDialog err = new ErrorDialog();
                    err.ErrorText.Text = "There was an error signing you in. Ensure you're signing in with your Microsoft Credentials. The application will now exit.";
                    err.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ErrorDialog err = new ErrorDialog();
                err.ErrorText.Text = "There was an error signing you in. Ensure you're signing in with your Microsoft Credentials. The application will now exit.";
                err.ShowDialog();
            }

            if (authResult != null)
            {
                token = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                GroupToken = await GetHttpContentWithToken(graphGroupsAPIEndpoint, authResult.AccessToken);
            }

            if (GroupToken.Contains("37e4d2af-8a52-4ed3-97c1-14df03cd9b14"))
            {
                RequiresAdminPassword = false;
            }

            if (GroupToken.Contains("521531d2-9a54-4b4a-ac91-598b3b411089"))
            {
                IsQueueAdmin = true;
            }

            EnableAll();
            PassBack();
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
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

        private void GetVersion(object sender, RoutedEventArgs e)
        {
            try
            {
                VersionLabel.Text = "Version: " + ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (Exception)
            {
                VersionLabel.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        private void OpenToolsButtonClick(object sender, RoutedEventArgs e)
        {
            ToolsFlyout.IsOpen = !ToolsFlyout.IsOpen;
        }

        private void ToolTileClick(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;

            try
            {
                switch (origin.CommandParameter)
                {
                    case "Timber":
                        Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Timber/Timber.exe");
                        break;
                    case "Fiddler":
                        Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Programs/Fiddler/Fiddler.exe");
                        break;
                    case "RAVE":
                        Process.Start("https://rave.office.net/");
                        break;
                    case "ViewPoint":
                        Process.Start("https://support.office.net");
                        break;
                    case "ExoPS":
                        Process.Start("powershell.exe -NoExit C:\\ExoPS.ps1");
                        break;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error: " + exc.Message + " -- " + exc.StackTrace + " -- " + exc.InnerException, "Confirmation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OptionsButtonClick(object sender, RoutedEventArgs e)
        {
            OptionsFlyout.IsOpen = !OptionsFlyout.IsOpen;
        }

        private void SavePrefButtonClick(object sender, RoutedEventArgs e)
        {
            //string eos = string.Empty ;

            //if (EoSDTPicker.SelectedDateTime.HasValue)
            //    eos = EoSDTPicker.SelectedDateTime.ToString();

            //StringBuilder sb = new StringBuilder();
            //sb.AppendLine("eos " + eos);

            //File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/O365_TBG/config.txt", sb.ToString());

            //MessageBox.Show("Your preferences have been saved, thank you");
        }

        private void OpenGuidesButtonClick(object sender, RoutedEventArgs e)
        {
            GuidesFlyout.IsOpen = !GuidesFlyout.IsOpen;
        }

        private void GuideButtonClick(object sender, RoutedEventArgs e)
        {
            Button origin = sender as Button;

            switch (origin.CommandParameter)
            {
                case "Downgrade":
                    Process.Start(Guides.Downgrade.AbsoluteUri);
                    break;
                case "RaveFiles":
                    Process.Start(Guides.RaveFiles.AbsoluteUri);
                    break;
            }
        }

        private void AlertGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            NotificationOverlay.Visibility = Visibility.Visible;
            NotificationOverlay.IsHitTestVisible = true;
        }

        private void WarningGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            NotificationOverlay.Visibility = Visibility.Visible;
            NotificationOverlay.IsHitTestVisible = true;
        }

        private void CloseNotificationOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (AlertsPanel.Children.Count > 0 || WarningsPanel.Children.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("You have undismissed notifications, are you sure you want to close this window without dismissing the notifications?\n\n" +
                    "The alert on the left of the page will remain visibile as long as there are undismissed notifications.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.None);

                if (result == MessageBoxResult.Yes)
                {
                    NotificationOverlay.Visibility = Visibility.Hidden;
                    NotificationOverlay.IsHitTestVisible = false;
                }
            }
            else
            {
                NotificationOverlay.Visibility = Visibility.Hidden;
                NotificationOverlay.IsHitTestVisible = false;
            }
        }

        private void SearchPanelOpen(object sender, RoutedEventArgs e)
        {
            SearchFlyout.IsOpen = !SearchFlyout.IsOpen;
        }

        private void SearchButton(object sender, RoutedEventArgs e)
        {
            QueryResponses.Children.Clear();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string CategoryQuery = string.Format("SELECT Technology,Category FROM Technologies WHERE Issue LIKE '%{0}%'", SearchQueryBox.Text);
                string IssueQuery = string.Format("SELECT Technology,Category,Issue FROM Technologies WHERE Issue LIKE '%{0}%'", SearchQueryBox.Text);
                string TroubleshootingQuery = string.Format("SELECT Technology,Category,Issue,Troubleshooting FROM Technologies WHERE Troubleshooting LIKE '%{0}%'", SearchQueryBox.Text);
                string PeerQuery = string.Format("SELECT Technology,Category,Issue,PeerSuggestions FROM Technologies WHERE PeerSuggestions LIKE '%{0}%'", SearchQueryBox.Text);
                string DocumentationQuery = string.Format("SELECT Technology,Category,Issue,Documentation FROM Technologies WHERE Documentation LIKE '%{0}%'", SearchQueryBox.Text);

                using (SqlCommand command = new SqlCommand(TroubleshootingQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SearchResultControl newResult = new SearchResultControl(SearchResultControl._Type.Category, this);

                            newResult.Margin = new Thickness(0, 10, 0, 0);

                            newResult.SetValues(SearchResultControl._Type.Category, reader.GetString(0), reader.GetString(1));

                            QueryResponses.Children.Add(newResult);
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(IssueQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SearchResultControl newResult = new SearchResultControl(SearchResultControl._Type.Issue, this);

                            newResult.Margin = new Thickness(0, 10, 0, 0);

                            newResult.SetValues(SearchResultControl._Type.Issue, reader.GetString(0), reader.GetString(1), reader.GetString(2));

                            QueryResponses.Children.Add(newResult);
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(TroubleshootingQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SearchResultControl newResult = new SearchResultControl(SearchResultControl._Type.Content, this)
                            {
                                Phrase = reader.GetString(3)
                            };

                            newResult.Margin = new Thickness(0, 10, 0, 0);

                            newResult.SetValues(SearchResultControl._Type.Content, reader.GetString(0), reader.GetString(1), reader.GetString(2), "Troubleshooting");

                            QueryResponses.Children.Add(newResult);
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(PeerQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SearchResultControl newResult = new SearchResultControl(SearchResultControl._Type.Content, this)
                            {
                                Phrase = reader.GetString(3)
                            };

                            newResult.Margin = new Thickness(0, 10, 0, 0);

                            newResult.SetValues(SearchResultControl._Type.Content, reader.GetString(0), reader.GetString(1), reader.GetString(2), "Peer Suggestions");

                            QueryResponses.Children.Add(newResult);
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand(DocumentationQuery, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SearchResultControl newResult = new SearchResultControl(SearchResultControl._Type.Content, this)
                            {
                                Phrase = reader.GetString(3)
                            };

                            newResult.Margin = new Thickness(0, 10, 0, 0);

                            newResult.SetValues(SearchResultControl._Type.Content, reader.GetString(0), reader.GetString(1), reader.GetString(2), "Documentation");

                            QueryResponses.Children.Add(newResult);
                        }
                    }
                }

                connection.Close();
            }

            if (QueryResponses.Children.Count == 0)
            {
                SearchResultControl newResult = new SearchResultControl(SearchResultControl._Type.NoResults, this);

                newResult.Margin = new Thickness(0, 10, 0, 0);

                QueryResponses.Children.Add(newResult);
            }
        }

        private void RefreshFullEdit_Click(object sender, RoutedEventArgs e)
        {
            FullEditGrid.ItemsSource = null;
            fullEditTable = casesTableAdapter.GetData();
            FullEditGrid.ItemsSource = fullEditTable.DefaultView;
            fullEditTable.RowChanged += FullEditTable_RowChanged;
        }

        private void FullEditFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[CaseID] LIKE '%{0}%' ", FullEditFilter.Text);
            sb.Append("OR ");
            sb.AppendFormat("Description LIKE '%{0}%' ", FullEditFilter.Text);
            sb.Append("OR ");
            sb.AppendFormat("[Taken By] LIKE '%{0}%' ", FullEditFilter.Text);
            sb.Append("OR ");
            sb.AppendFormat("Severity LIKE '%{0}%' ", FullEditFilter.Text);
            sb.Append("OR ");
            sb.AppendFormat("Queue LIKE '%{0}%' ", FullEditFilter.Text);
            sb.Append("OR ");
            sb.AppendFormat("[Added By] LIKE '%{0}%' ", FullEditFilter.Text);
            sb.Append("OR ");
            sb.AppendFormat("[Transfer Approved By] LIKE '%{0}%'", FullEditFilter.Text);

            string finalFilter = sb.ToString();

            fullEditTable.DefaultView.RowFilter = finalFilter;
        }

        private void NewCaseSubmitButton(object sender, RoutedEventArgs e)
        {
            List<CaseModel> _cases = Queue.GetCases();
            if (_cases.Any(c => c.CaseID == Regex.Replace(NewCaseNumber.Text.Trim(), "[^0-9]", "")))
            {
                MessageBox.Show("This case already exists in TBG.");
                return;
            }
            else
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

                char queue = NewCaseQueue.SelectedValue.ToString().Split(':')[1].ToUpper().Trim()[0];

                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    string query = string.Format("INSERT INTO Cases (Queue, CaseID, Posted, Severity, Description, AddedBy, Created) VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}', '{6}')", queue, Regex.Replace(NewCaseNumber.Text, "[^0-9]", ""), DateTime.Now.ToString("yyyy -MM-dd hh:mm:00 tt"), NewCaseSeverity.SelectedValue.ToString().Split(':')[1].Trim(), NewCaseDescription.Text.Replace("'", "''"), DisplayName, "2000-01-01");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                NewCaseFlyout.IsOpen = !NewCaseFlyout.IsOpen;

                NewCaseNumber.Text = "";
                NewCaseSeverity.SelectedIndex = 1;
                NewCaseDescription.Text = "";
                NewCaseQueue.SelectedIndex = 0;
            }
        }

        private void GenerateFullSummaryDocument(object sender, RoutedEventArgs e)
        {
            Queue.GenerateFullReportDocument();
        }

        private void GenerateCasesTakenCsv(object sender, RoutedEventArgs e)
        {
            Queue.GenerateCasesTakenCsv();
        }

        private void GenerateCasesTakenDocument(object sender, RoutedEventArgs e)
        {
            Queue.GenerateCasesTakenDocument();
        }

        private void GenerateExplanationsDocument(object sender, RoutedEventArgs e)
        {
            Queue.GenerateExplanationsDocument();
        }

        private void GenerateExplanationsCsv(object sender, RoutedEventArgs e)
        {
            Queue.GenerateExplanationsCsv();
        }

        private void ExplanationClearButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you absolutely sure you want to clear engineer explanations? If you have not run reports, you will be unable to retreive the data.\n\nDoing this is not reversable!", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
                Queue.ClearExplanations();
        }

        private void FullEditGrid_Open(object sender, RoutedEventArgs e)
        {
            MainWindow.thisApp.FullEditFlyout.IsOpen = !MainWindow.thisApp.FullEditFlyout.IsOpen;
        }

        private void QueueClearButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you absolutely sure you want to clear the queue? If you have not run reports, you will be unable to retreive the data.\n\nDoing this is not reversable!", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
                Queue.ClearQueue();
        }

        private void OpenQueue(object sender, RoutedEventArgs e)
        {
            MenuExpander.IsExpanded = false;
            PageFrame.Visibility = Visibility.Visible;
            PageFrame.Navigate(queuePage);
            List<CaseModel> cases = Queue.GetCases(token, GroupToken);
            queuePage.CasesDataGrid.ItemsSource = cases;
        }

        private void OpenMyCases(object sender, RoutedEventArgs e)
        {
            MenuExpander.IsExpanded = false;
            PageFrame.Visibility = Visibility.Visible;
            PageFrame.Navigate(casesPage);
        }

        private void OpenReporting(object sender, RoutedEventArgs e)
        {
            MenuExpander.IsExpanded = false;
            PageFrame.Visibility = Visibility.Visible;
            PageFrame.Navigate(reportingPage);
        }

        private void ChangeTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeFlyout.IsOpen = true;

            List<string> themes = new List<string>();
            foreach (var theme in ThemeManager.Themes)
            {
                themes.Add(theme.Name);
            }

            ThemeComboBox.ItemsSource = null;
            ThemeComboBox.ItemsSource = themes;
        }

        private void SubmitThemeChange(object sender, RoutedEventArgs e)
        {
            ThemeManager.ChangeTheme(Application.Current, ThemeComboBox.Text);

            if (ThemeComboBox.Text.Contains("Light"))
            {
                casesPage.SwitchToLight();
                //reportingPage.SwitchToLight();
            }

            else
            {
                casesPage.SwitchToDark();
                //reportingPage.SwitchToDark();
            }
        }
    }
}
