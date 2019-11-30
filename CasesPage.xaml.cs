using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TBG_WPF.TBGTableAdapters;

namespace TBG_WPF
{
    /// <summary>
    /// Interaction logic for CasesPage.xaml
    /// </summary>
    public partial class CasesPage : Page
    {
        OwnedCasesTableAdapter casesTA = new OwnedCasesTableAdapter();
        CasesTableAdapter queueTA = new CasesTableAdapter();
        ExplanationsTableAdapter expTA = new ExplanationsTableAdapter();
        TechnologiesTableAdapter techTA = new TechnologiesTableAdapter();
        SkillsTableAdapter skillsTA = new SkillsTableAdapter();
        DataTable casesDT = new DataTable();
        DataTable queueDT = new DataTable();
        DataTable expDT = new DataTable();
        DataTable buddyDT = new DataTable();
        DataTable skillQueueDT = new DataTable();

        string token;
        string groupToken;

        string easterEgg;
        int easterEggIndex = -1;
        List<string> easterEggValues = new List<string>();

        BackgroundWorker worker;

        string DisplayName = null;

        bool isEditing = false;
        string currentAgedCase;

        List<string> Engineers = new List<string>();
        List<string> Skills = new List<string>();
        List<string> FullSkills = new List<string>();

        public CasesPage(string _token, string _groupToken)
        {
            InitializeComponent();

            token = _token;
            groupToken = _groupToken;

            List<string> entries = token.Split(',').ToList();

            foreach (string s in entries)
            {
                if (s.Contains("displayName"))
                {
                    DisplayName = s.Split(':')[1].Trim('"');
                }
            }

            CreateSkills();
            Fill();
            FillQueue();
            PullSkills();
            //CheckIfAged();

            string easterEgg = Encoding.UTF8.GetString(Properties.Resources.easterEgg);
            foreach (string s in easterEgg.Split('\n'))
                easterEggValues.Add(s);

#if DEBUG
            for (int i = 0; i < easterEggValues.Count; i++)
                Debug.WriteLine(i + " - " + easterEggValues[i]);
#endif 

            worker = new BackgroundWorker();
            Timer t = new Timer(10000);
            worker.DoWork += Worker_DoWork;
            t.Elapsed += T_Elapsed;
            t.Start();
        }

        public void SwitchToLight()
        {
            MainGrid.Background = new SolidColorBrush(Colors.White);

            SignedInLabel.Foreground = new SolidColorBrush(Colors.Black);
        }

        public void SwitchToDark()
        {
            MainGrid.Background = new SolidColorBrush(Colors.Black);

            SignedInLabel.Foreground = new SolidColorBrush(Colors.White);
        }

        //// Check if any cases are over 14 days old, if so, prompt the user to input an explanation for the long-running case
        //private void CheckIfAged()
        //{
        //    foreach (DataRow row in casesDT.Rows)
        //    {
        //        if (row.Field<int>("Age") >= 14)
        //        {

        //        }
        //    }
        //}

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {

            this.Dispatcher.Invoke(() =>
            {
                //DataTable compDT = new DataTable();
                //compDT = casesTA.GetDataFor(DisplayName);

                //bool areSame = true;

                //if (casesDT.Rows.Count != compDT.Rows.Count || casesDT.Columns.Count != compDT.Columns.Count)
                //    areSame = false;

                //for (int i = 0; i < casesDT.Rows.Count; i++)
                //{
                //    for (int c = 0; c < casesDT.Columns.Count; c++)
                //    {
                //        if (!Equals(casesDT.Rows[i][c], casesDT.Rows[i][c]))
                //            areSame = false;
                //    }
                //}

                //if (areSame == false)
                Fill();

                FillQueue();
            });

            } catch (Exception esc)
            {

            }
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void Fill()
        {
            if (!isEditing)
            {
                ownedCasesDataGrid.ItemsSource = null;
                casesDT.Clear();

                casesDT = casesTA.GetDataFor(DisplayName);
                casesDT.RowChanged += CasesDT_RowChanged1;
                ownedCasesDataGrid.ItemsSource = casesDT.DefaultView;

                UsernameLabel.Text = DisplayName;

                GetEngineersWithCases();
            }
        }

        private void CasesDT_RowChanged1(object sender, DataRowChangeEventArgs e)
        {
            casesTA.Update(e.Row);
        }

        // Insert any new cases that match the Engineer's skills into the queue
        private void FillQueue()
        {
            DataTable skillsQueueDT = new DataTable();
            skillsQueueDT.Columns.Add("Created");
            skillsQueueDT.Columns.Add("CaseID");
            skillsQueueDT.Columns.Add("Severity");
            skillsQueueDT.Columns.Add("Description");
            skillsQueueDT.Columns.Add("Skills");

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string GetSkills = "SELECT Skills FROM Skills WHERE Engineer=@Engineer";
                string GetCasesBySkill = "SELECT Created, CaseID, Severity, Description, Skills FROM Cases WHERE Skills LIKE '%{0}%' AND TakenAt IS NULL";
                string GetCasesByCritical = "SELECT Created, CaseID, Severity, Description, Skills FROM Cases WHERE (Severity='A' OR Severity='A 24x7' OR Severity='Critical') AND TakenAt IS NULL";
                //string GetCases = "SELECT CaseID, Severity, Skills, Description FROM Cases WHERE TakenBy='' OR TakenBy=NULL";

                string skills = string.Empty;

                using (SqlCommand command = new SqlCommand(GetCasesByCritical, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            List<string> IDs = new List<string>();
                            foreach (DataRow row in skillsQueueDT.Rows)
                            {
                                IDs.Add(row.Field<string>(1));
                            }

                            if (!IDs.Any(i => i == reader.GetString(1)))
                            {
                                DataRow row = skillsQueueDT.NewRow();
                                row.SetField<string>("Created", reader.GetDateTime(0).ToString("yyyy-MM-dd"));
                                row.SetField<string>("CaseID", reader.GetString(1));
                                row.SetField<string>("Severity", reader.GetString(2));
                                row.SetField<string>("Description", reader.GetString(3));
                                string _skills = string.Empty;
                                try { _skills = reader.GetString(4).Replace(",", "\n"); } catch (Exception exc) { _skills = "N/A"; }
                                row.SetField<string>("Skills", _skills);
                                skillsQueueDT.Rows.Add(row);
                            }
                        }
                    }
                }

                try
                {

                    using (SqlCommand command = new SqlCommand(GetSkills, connection))
                    {
                        command.Parameters.AddWithValue("@Engineer", DisplayName);

                        skills = command.ExecuteScalar().ToString();
                    }

                    foreach (string s in skills.Split(','))
                    {
                        string query = string.Format(GetCasesBySkill, s);

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    List<string> IDs = new List<string>();
                                    foreach (DataRow row in skillsQueueDT.Rows)
                                    {
                                        IDs.Add(row.Field<string>(1));
                                    }

                                    if (!IDs.Any(i => i == reader.GetString(1)))
                                    {
                                        DataRow row = skillsQueueDT.NewRow();
                                        row.SetField<string>("Created", reader.GetDateTime(0).ToString("yyyy-MM-dd"));
                                        row.SetField<string>("CaseID", reader.GetString(1));
                                        row.SetField<string>("Severity", reader.GetString(2));
                                        row.SetField<string>("Description", reader.GetString(3));
                                        string _skills = string.Empty;
                                        try { _skills = reader.GetString(4).Replace(",", "\n"); } catch (Exception exc) { _skills = reader.GetString(4); }
                                        row.SetField<string>("Skills", _skills);
                                        skillsQueueDT.Rows.Add(row);
                                    }
                                }
                            }
                        }
                    }

                }
                catch (Exception exc) { }

                connection.Close();
            }

            queueDataGrid.ItemsSource = null;
            queueDataGrid.ItemsSource = skillsQueueDT.DefaultView;
        }

        private void GetEngineersWithCases()
        {
            if (EngineerComboBox.SelectedIndex == -1 || EngineerComboBox.SelectedIndex == 0)
            {
                try
                {
                    EngineerComboBox.ItemsSource = null;
                    Engineers.Clear();

                    Engineers.Add("-- Click to refresh --");

                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = "SELECT DISTINCT Owner FROM OwnedCases WHERE Owner LIKE '%Tek%'";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                    Engineers.Add(reader.GetString(0));
                            }
                        }

                        connection.Close();
                    }

                    EngineerComboBox.ItemsSource = Engineers;
                }
                catch (Exception exc)
                { }
            }
        }

        private void OldCaseAddButton_Click(object sender, RoutedEventArgs e)
        {
            OldCaseGrid.Visibility = Visibility.Visible;

            OldCaseNumber.Focus();
        }

        private void CloseOldCaseGrid_Click(object sender, RoutedEventArgs e)
        {
            OldCaseGrid.Visibility = Visibility.Hidden;

            OldCaseNumber.Text = "";
            OldCaseSeverity.SelectedIndex = -1;
            OldCaseQueue.SelectedIndex = -1;
            OldCaseDate.SelectedDate = null;
        }

        private void SubmitOldCase_Click(object sender, RoutedEventArgs e)
        {
            if (OldCaseNumber.Text == "x18531cx3f15cx")
            {
                MessageBox.Show("Nope!");
                return;
            }

            if (!string.IsNullOrWhiteSpace(OldCaseNumber.Text) && !string.IsNullOrWhiteSpace(OldCaseSeverity.Text) && !string.IsNullOrWhiteSpace(OldCaseQueue.Text) && OldCaseDate.SelectedDate != null)
            {
                casesTA.Insert((DateTime)OldCaseDate.SelectedDate, Regex.Replace(OldCaseNumber.Text, "[^0-9]", ""), OldCaseSeverity.Text, OldCaseQueue.Text, "", false, "", false, false, false, "", DisplayName);

                OldCaseGrid.Visibility = Visibility.Hidden;

                OldCaseNumber.Text = "";
                OldCaseSeverity.SelectedIndex = -1;
                OldCaseQueue.SelectedIndex = -1;
                OldCaseDate.SelectedDate = DateTime.Now;

                Fill();
            }
            else
            {
                MessageBox.Show("Please ensure all the information is filled out");
            }
        }

        private void AddNewCase_Click(object sender, RoutedEventArgs e)
        {
            NewCaseGrid.Visibility = Visibility.Visible;

            NewCaseNumber.Focus();
        }

        private void CloseNewCaseGrid_Click(object sender, RoutedEventArgs e)
        {
            NewCaseGrid.Visibility = Visibility.Hidden;

            NewCaseNumber.Text = "";
            NewCaseSeverity.SelectedIndex = -1;
            NewCaseQueue.SelectedIndex = -1;
            NewCaseDate.SelectedDate = null;
        }

        private void SubmitNewCase_Click(object sender, RoutedEventArgs e)
        {
            if (NewCaseNumber.Text == "x18531cx3f15cx")
            {
                if (NewCaseQueue.SelectedIndex == 0 && NewCaseSeverity.SelectedIndex == 5)
                {
                    MessageBox.Show("Good job! Ping me on teams with the code: 58b15xc so that I know you won!");
                    return;
                }
                else
                {
                    MessageBox.Show("You're missing the correct queue and severity! Find it somewhere!");
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(NewCaseNumber.Text) && !string.IsNullOrWhiteSpace(NewCaseSeverity.Text) && !string.IsNullOrWhiteSpace(NewCaseQueue.Text) && NewCaseDate.SelectedDate != null)
            {
                queueDT = queueTA.GetData();

                DataRow[] foundRows = queueDT.Select("CaseID = '" + NewCaseNumber.Text + "'");
                if (foundRows.Length == 1)
                {
                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = string.Format("UPDATE Cases SET TakenBy='{0}', TakenAt='{1}' WHERE CaseID='{2}'", DisplayName, DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt"), Regex.Replace(NewCaseNumber.Text, "[^0-9]", ""));

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        connection.Close();
                    }
                }
                else
                    queueTA.InsertOwned(DateTime.Now, Regex.Replace(NewCaseNumber.Text, "[^0-9]", ""), NewCaseQueue.Text, NewCaseSeverity.Text, DisplayName, DateTime.Now);

                casesTA.Insert((DateTime)NewCaseDate.SelectedDate, Regex.Replace(NewCaseNumber.Text, "[^0-9]", ""), NewCaseSeverity.Text, NewCaseQueue.Text, "", false, "", false, false, false, "", DisplayName);

                NewCaseGrid.Visibility = Visibility.Hidden;

                NewCaseNumber.Text = "";
                NewCaseSeverity.SelectedIndex = -1;
                NewCaseQueue.SelectedIndex = -1;
                NewCaseDate.SelectedDate = DateTime.Now;

                Fill();
            }
            else
            {
                MessageBox.Show("Please ensure all the information is filled out");
            }
        }

        private void Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            expDT = expTA.GetData();

            string newValue = (e.AddedItems[0] as ComboBoxItem).Content.ToString();

            DataRow[] existing = expDT.Select("Engineer = '" + DisplayName + "'");
            if (existing.Length == 1)
                expTA.UpdateQuery(newValue, DisplayName);
            else
                expTA.Insert(DisplayName, newValue);
        }

        private void EasterEggTrigger(object sender, MouseButtonEventArgs e)
        {
            easterEggIndex++;

            if (easterEggIndex == 87)
            {
                string name = DisplayName.Split(' ')[0];

                QueueManagement qm = new QueueManagement();
                qm.GenerateEasterEggDocument(name);
            }

            else if (easterEggIndex == 259)
            {
                UsernameLabel.Text = DisplayName;
                easterEggIndex = -1;

                return;
            }

            if (string.IsNullOrWhiteSpace(easterEggValues[easterEggIndex]))
                return;
            else
                UsernameLabel.Text = easterEggValues[easterEggIndex];
        }

        private void EngineerBuddyChanged(object sender, SelectionChangedEventArgs e)
        {
            buddyDT.Clear();
            buddyCasesDataGrid.ItemsSource = null;

            string name = e.AddedItems[0].ToString();

            buddyDT = casesTA.GetDataFor(name);
            buddyCasesDataGrid.ItemsSource = buddyDT.DefaultView;
        }

        // Create the skill checkboxes in their appropriate categories
        private void CreateSkills()
        {
            ExoSkills.Children.Clear();
            ExchSkills.Children.Clear();
            OutSkills.Children.Clear();
            LyncSkills.Children.Clear();

            DataTable dt = techTA.GetData();

            List<string> tempList = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                tempList.Add(row.Field<string>(0) + "-" + row.Field<string>(1));
            }

            FullSkills = tempList.Distinct().OrderBy(c => c.Split('-')[1]).ToList();

            foreach (string pair in FullSkills)
            {
                string tech = pair.Split('-')[0];
                string cat = string.Empty;

                if (tech == "Exchange Online")
                    cat = "EXO - " + pair.Split('-')[1];
                else if (tech == "Exchange Server")
                    cat = "Exch - " + pair.Split('-')[1];
                else if (tech == "Outlook")
                    cat = "Out - " + pair.Split('-')[1];
                else if (tech == "Teams / Skype for Business")
                    cat = "Lync - " + pair.Split('-')[1];

                CheckBox newSkill = new CheckBox();
                newSkill.Content = cat;
                newSkill.Margin = new Thickness(5, 5, 0, 0);
                newSkill.Checked += NewSkill_Checked;
                newSkill.Unchecked += NewSkill_Unchecked;

                if (tech == "Exchange Online")
                    ExoSkills.Children.Add(newSkill);
                else if (tech == "Exchange Server")
                    ExchSkills.Children.Add(newSkill);
                else if (tech == "Outlook")
                    OutSkills.Children.Add(newSkill);
                else if (tech == "Teams / Skype for Business")
                    LyncSkills.Children.Add(newSkill);
            }
        }

        /* Need to gather skills from Technologies Table - skills are categories, and queue is Technology
         * Skills can be reported as wrong, allowing the team to alert the QM that it was input wrong */
        private void EditMySkills(object sender, RoutedEventArgs e)
        {
            PullSkills();

            SkillsAndAttributesGrid.Visibility = Visibility.Visible;
            SkillsAndAttributesGrid.IsHitTestVisible = true;
        }

        // Get the skill unchecked, and remove it from the Engineer's profile in the SQL database
        private void NewSkill_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox origin = sender as CheckBox;
            string skill = origin.Content.ToString();

            origin.Foreground = new SolidColorBrush(Colors.White);

            skillsTA.RemoveSkill(skill + ",", DisplayName);
        }

        // Get the skill checked and add it to the Engineer's profile in the SQL database
        private void NewSkill_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox origin = sender as CheckBox;
            string skill = origin.Content.ToString();

            origin.Foreground = new SolidColorBrush(Colors.Gold);

            DataTable tempTable = new DataTable();

            try
            {
                tempTable = skillsTA.GetSkills(DisplayName);
            }
            catch (Exception exc)
            {
                skillsTA.InsertQuery(DisplayName, skill);
                return;
            }

            if (tempTable.Rows.Count > 0)
            {
                List<string> currentSkills = tempTable.Rows[0].Field<string>(0).Split(',').ToList();
                currentSkills.Add(skill);

                StringBuilder newSkills = new StringBuilder();

                foreach (string s in currentSkills)
                {
                    newSkills.Append(s);
                    newSkills.Append(',');
                }

                skillsTA.UpdateQuery(newSkills.ToString().TrimEnd(','), DisplayName);
            }
            else
            {
                skillsTA.InsertQuery(DisplayName, skill);
                return;
            }
        }

        // Close the skills & attributes grid
        private void CloseSkillsAndAttributes_Click(object sender, RoutedEventArgs e)
        {
            SkillsAndAttributesGrid.Visibility = Visibility.Hidden;
            SkillsAndAttributesGrid.IsHitTestVisible = false;
        }

        // Pull the Engineer's skills and check the relevant boxes within the "My Skill & Attributes" panel
        // Remove the 'checked' event from the checkboxes, then re-add it
        private void PullSkills()
        {
            List<CheckBox> skillBoxes = new List<CheckBox>();

            foreach (FrameworkElement element in ExoSkills.Children)
                if (element is CheckBox)
                    skillBoxes.Add(element as CheckBox);
            foreach (FrameworkElement element in ExchSkills.Children)
                if (element is CheckBox)
                    skillBoxes.Add(element as CheckBox);
            foreach (FrameworkElement element in OutSkills.Children)
                if (element is CheckBox)
                    skillBoxes.Add(element as CheckBox);
            foreach (FrameworkElement element in LyncSkills.Children)
                if (element is CheckBox)
                    skillBoxes.Add(element as CheckBox);

            foreach (CheckBox element in skillBoxes)
                element.Checked -= NewSkill_Checked;

            DataTable tempTable = new DataTable();
            tempTable = skillsTA.GetSkills(DisplayName);

            if (tempTable.Rows.Count > 0)
            {
                string allSkills = tempTable.Rows[0].Field<string>(0);

                List<string> skills = allSkills.Split(',').ToList();

                foreach (string s in skills)
                {
                    var skillCB = skillBoxes.First(c => c.Content.ToString() == s);
                    skillCB.IsChecked = true;
                    skillCB.Foreground = new SolidColorBrush(Colors.Gold);
                }
            }

            foreach (CheckBox element in skillBoxes)
                element.Checked += NewSkill_Checked;
        }

        private void OwnedViewInRave_Clicked(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            Process.Start("https://rave.office.net/cases/" + origin.CommandParameter.ToString());
        }

        private void OwnedMarkClosed_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE OwnedCases SET Closed=1, ClosedOn='{1}' WHERE CaseID='{0}'", origin.CommandParameter.ToString(), DateTime.Now.ToString("yyyy-MM-dd"));

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void TransferKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox origin = sender as TextBox;

                string approvedBy = origin.Text;
                MessageBox.Show(approvedBy);
            }
        }

        private void OwnedMarkEscalated_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE OwnedCases SET Escalated=1 WHERE CaseID='{0}'", origin.CommandParameter.ToString());

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void OwnedRemove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OwnedMarkAttention_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE OwnedCases SET BuddyNotes='Needs Attention' WHERE CaseID='{0}'", origin.CommandParameter.ToString());

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void OwnedMarkNoUpdates_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE OwnedCases SET BuddyNotes='No Updates' WHERE CaseID='{0}'", origin.CommandParameter.ToString());

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void OwnedMarkNoEngagement_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE OwnedCases SET BuddyNotes='No cx engagement' WHERE CaseID='{0}'", origin.CommandParameter.ToString());

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void OwnedMarkTransferred_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AgedCaseNavigate(object sender, RoutedEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;

            Process.Start("https://rave.office.net/cases/" + link.Tag);
        }

        private void SubmitAgedCaseExplanation_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE OwnedCases SET AgeExplanation='{1}' WHERE CaseID='{0}'", currentAgedCase, AgedCaseExplanation.Text);

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void QueueOpenInRave_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            Process.Start("https://rave.office.net/cases/" + origin.CommandParameter.ToString());
        }

        private void QueueMarkTaken_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query1 = string.Format("UPDATE Cases SET TakenBy='{0}', TakenAt='{1}' OUTPUT Inserted.Severity, Inserted.Created WHERE CaseID='{2}'", DisplayName, DateTime.Now.ToString("yyyy-MM-dd hh:mm tt"), origin.CommandParameter.ToString());

                string CreatedValue = string.Empty;
                string SeverityValue = string.Empty;

                using (SqlCommand command = new SqlCommand(query1, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SeverityValue = reader.GetString(0);
                            CreatedValue = reader.GetDateTime(1).ToString("yyyy-MM-dd");
                        }
                    }
                }

                string query2 = string.Format("INSERT INTO OwnedCases (CaseID, Created, Severity, Owner) VALUES ('{0}', '{1}', '{2}', '{3}')", origin.CommandParameter.ToString(), CreatedValue, SeverityValue, DisplayName);

                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void QueueMarkWrongSkill_Click(object sender, RoutedEventArgs e)
        {

        }

        private void QueueMarkExternalTaken_Click(object sender, RoutedEventArgs e)
        {
            MenuItem origin = sender as MenuItem;

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = string.Format("UPDATE Cases SET TakenBy='External Agent', TakenAt='{1}' WHERE CaseID='{0}'", origin.CommandParameter.ToString(), DateTime.Now.ToString("yyyy-MM-dd hh:mm tt"));

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private void OwnedCasesBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            isEditing = true;
        }

        private void OwnedCasesEndingEdit(object sender, DataGridRowEditEndingEventArgs e)
        {
            isEditing = false;
        }
    }
}


