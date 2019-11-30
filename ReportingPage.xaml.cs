using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    /// Interaction logic for ReportingPage.xaml
    /// </summary>
    public partial class ReportingPage : Page
    {
        BackgroundWorker worker;

        CasesTableAdapter queueTA = new CasesTableAdapter();
        ExplanationsTableAdapter expTA = new ExplanationsTableAdapter();
        OwnedCasesTableAdapter casesTA = new OwnedCasesTableAdapter();
        SkillsTableAdapter skillsTA = new SkillsTableAdapter();
        TechnologiesTableAdapter techTA = new TechnologiesTableAdapter();

        List<string> FullSkills = new List<string>();
        List<string> SelectedSkills = new List<string>();

        DataTable queueDT = new DataTable();
        DataTable expDT = new DataTable();
        DataTable casesDT = new DataTable();

        string DisplayName = string.Empty;

        public ReportingPage(string token)
        {
            InitializeComponent();

            List<string> entries = token.Split(',').ToList();

            foreach (string s in entries)
            {
                if (s.Contains("displayName"))
                {
                    DisplayName = s.Split(':')[1].Trim('"');
                }
            }

            queueDT = queueTA.GetData();
            expDT = expTA.GetData();
            casesDT = casesTA.GetDataAll();

            worker = new BackgroundWorker();
            Timer t = new Timer(10000);
            t.Elapsed += T_Elapsed;
            worker.DoWork += Worker_DoWork;
            t.Start();

            CreateSkills();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            DataTable tempTable = new DataTable();
            tempTable = queueTA.GetData();

            bool areSame = true;

            if (queueDT.Rows.Count != tempTable.Rows.Count || queueDT.Columns.Count != tempTable.Columns.Count)
                areSame = false;

            for (int i = 0; i < queueDT.Rows.Count; i++)
            {
                for (int c = 0; c < queueDT.Columns.Count; c++)
                {
                    if (!Equals(queueDT.Rows[i][c], tempTable.Rows[i][c]))
                        areSame = false;
                }
            }

            if (areSame == false)
                queueDT = queueTA.GetData();

            tempTable = expTA.GetData();

            areSame = true;

            if (expDT.Rows.Count != tempTable.Rows.Count || expDT.Columns.Count != tempTable.Columns.Count)
                areSame = false;

            for (int i = 0; i < expDT.Rows.Count; i++)
            {
                for (int c = 0; c < expDT.Columns.Count; c++)
                {
                    if (!Equals(expDT.Rows[i][c], tempTable.Rows[i][c]))
                        areSame = false;
                }
            }

            if (areSame == false)
                expDT = expTA.GetData();

            tempTable = casesTA.GetDataAll();

            areSame = true;

            if (casesDT.Rows.Count != tempTable.Rows.Count || casesDT.Columns.Count != tempTable.Columns.Count)
                areSame = false;

            for (int i = 0; i < casesDT.Rows.Count; i++)
            {
                for (int c = 0; c < casesDT.Columns.Count; c++)
                {
                    if (!Equals(casesDT.Rows[i][c], tempTable.Rows[i][c]))
                        areSame = false;
                }
            }

            if (areSame == false)
                casesDT = casesTA.GetDataAll();

            this.Dispatcher.Invoke(() =>
            {
                CritEngineers.Children.Clear();

                List<string> CritEngineerList = new List<string>();

                foreach (DataRow row in casesDT.Rows)
                {
                    if (row.Field<string>(3) == "A" || row.Field<string>(3) == "A 24x7" || row.Field<string>(3) == "Critical")
                    {
                        CritEngineerList.Add(row.Field<string>("Owner"));
                    }
                }

                List<string> _finalList = CritEngineerList.Distinct().ToList();
                foreach (string s in _finalList)
                {
                    TextBlock newBlock = new TextBlock();
                    newBlock.Text = s.Replace(" (Tek Experts)", "");
                    newBlock.Width = 200;
                    newBlock.Foreground = new SolidColorBrush(Colors.White);
                    newBlock.Margin = new Thickness(10, 10, 0, 0);
                    CritEngineers.Children.Add(newBlock);
                }
            });
        }

        private void T_Elapsed(object sender, ElapsedEventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void AddCase(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CaseNumber.Text) && SeverityComboBox.SelectedIndex > -1 && CreatedDate.SelectedDate != null && SelectedSkills.Count > 0)
            {
                string caseNumber = CaseNumber.Text;
                string severity = SeverityComboBox.Text;
                string description = Description.Text;
                DateTime time = (DateTime)CreatedDate.SelectedDate;
                StringBuilder skills = new StringBuilder();

                foreach (string s in SelectedSkills)
                {
                    skills.Append(s);
                    skills.Append(',');
                }

                queueTA.InsertWithSkills(DateTime.Now, caseNumber, skills.ToString().Trim(','), severity, description, DisplayName, time);

                CaseNumber.Text = "";
                SeverityComboBox.SelectedIndex = -1;
                Description.Text = "";
                CreatedDate.SelectedDate = null;
                SelectedSkills.Clear();
                SkillsComboBox.ItemsSource = null;
                CreateSkills();
            }
        }

        // Create the skill checkboxes in their appropriate categories
        private void CreateSkills()
        {
            DataTable dt = techTA.GetData();

            List<string> tempList = new List<string>();
            List<CheckBox> skillsComboBoxes = new List<CheckBox>();

            EXOSkills.Children.Clear();
            ExchSkills.Children.Clear();
            OutSkills.Children.Clear();
            LyncSkills.Children.Clear();

            foreach (DataRow row in dt.Rows)
            {
                tempList.Add(row.Field<string>(0) + "-" + row.Field<string>(1));
            }

            FullSkills = tempList.Distinct().OrderBy(c => c.Split('-')[1]).ToList();

            foreach (string pair in FullSkills)
            {
                string tech = pair.Split('-')[0];
                string cat = string.Empty;

                CheckBox newSkill = new CheckBox();
                newSkill.Margin = new Thickness(5, 5, 0, 0);
                newSkill.Checked += NewSkill_Checked;
                newSkill.Unchecked += NewSkill_Unchecked;

                if (tech == "Exchange Online")
                {
                    newSkill.Content = "EXO - " + pair.Split('-')[1];
                    EXOSkills.Children.Add(newSkill);
                }
                else if (tech == "Exchange Server")
                {
                    newSkill.Content = "Exch - " + pair.Split('-')[1];
                    ExchSkills.Children.Add(newSkill);
                }
                else if (tech == "Outlook")
                {
                    newSkill.Content = "Out - " + pair.Split('-')[1];
                    OutSkills.Children.Add(newSkill);
                }
                else if (tech == "Teams / Skype for Business")
                {
                    newSkill.Content = "Lync - " + pair.Split('-')[1];
                    LyncSkills.Children.Add(newSkill);
                }
            }
        }

        // Remove the skill from the selected skills list
        private void NewSkill_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox origin = sender as CheckBox;

            SelectedSkills.Remove(origin.Content.ToString());
        }

        // Add the skill to the selected skills list
        private void NewSkill_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox origin = sender as CheckBox;

            SelectedSkills.Add(origin.Content.ToString());
        }

        private void FalseSelectionHandler(object sender, RoutedEventArgs e)
        {
            SkillsComboBox.SelectedItem = null;
        }
        
        private void RunQuery_Click(object sender, RoutedEventArgs e)
        {
            //string queryText = QueryBox.Text;
            //if (queryText.ToLower().Contains("select"))

            //using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            //{
            //    connection.Open();

            //    using (SqlCommand command = new SqlCommand())
            //}
        }

        // Submit button for the query running tool for the administrators / managers
        // NOT IMPLEMENTED YET
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string query = QueryBox.Text;
            string type;
            int affected = 0;
                       
            if (query.ToLower().Contains("SELECT"))
            {
                type = "SELECT";    
            }
            else if (query.ToLower().Contains("UPDATE"))
            {
                type = "UPDATE";
            }
            else if (query.ToLower().Contains("DELETE"))
            {
                type = "DELETE";
            }
            else
            {
                type = "OTHER";
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                {
                    connection.Open();

                    if (type == "SELECT")
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    // perform operations to read data from the query and output it here
                                }
                            }
                        }
                    }

                    if (type == "UPDATE" || type == "DELETE" || type == "OTHER")
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            affected = command.ExecuteNonQuery();
                        }
                    }
                }
            } catch (Exception exc)
            {
                MessageBox.Show("There was an error running the query you posted. Please review it for errors and try again.");
            }

            if (type == "UPDATE")
            {
                MessageBox.Show("Successfully updated " + affected + " rows.");
            }
            else if (type == "DELETE")
            {
                MessageBox.Show("Successfully deleted " + affected + " rows.");
            }
            else if (type == "OTHER")
            {
                MessageBox.Show("Query ran successfully.");
            }
            else if (type == "SELECT")
            {
                // perform operations to display the query-returned data in a new grid
            }
        }
    }
}
