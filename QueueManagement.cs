using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xceed.Words;
using Xceed.Document;
using Xceed.Words.NET;
using System.Drawing;
using System.IO;

namespace TBG_WPF
{
    public class QueueManagement
    {
        BackgroundWorker worker;

        public QueueManagement()
        {
            worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            System.Timers.Timer timer = new System.Timers.Timer(15000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            worker.RunWorkerAsync();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<CaseModel> Cases = GetCases();

            foreach (CaseModel _case in Cases)
            {
                TimeSpan _age = DateTime.Now - _case.Posted;
                int Age = _age.Minutes + (_age.Hours * 60);

                if (string.IsNullOrWhiteSpace(_case.TakenBy))
                {
                    using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
                    {
                        connection.Open();

                        string query = string.Format("UPDATE Cases SET Age={0} WHERE CaseID='{1}'", Age, _case.CaseID);

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        connection.Close();
                    }
                }
            }
        }

        public List<CaseModel> GetCases(string token, string groupToken)
        {
            List<string> entries = token.Split(',').ToList();
            string DisplayName = string.Empty;

            foreach (string s in entries)
            {
                if (s.Contains("displayName"))
                {
                    DisplayName = s.Split(':')[1].Trim('"');
                    break;
                }
            }

            List<CaseModel> cases = new List<CaseModel>();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Cases";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CaseModel newCase = new CaseModel();

                            newCase.Posted = reader.GetDateTime(1);
                            newCase.CaseID = reader.GetString(2);
                            newCase.Queue = reader.GetString(3);
                            newCase.Severity = reader.GetString(5);
                            newCase.Description = !reader.IsDBNull(6) ? reader.GetString(6) : "";
                            newCase.TakenBy = !reader.IsDBNull(7) ? reader.GetString(7) : "";

                            if (newCase.TakenBy.Trim() == DisplayName.Trim())
                                newCase.CanTransfer = true;
                            else
                                newCase.CanTransfer = false;

                            if (string.IsNullOrWhiteSpace(newCase.TakenBy))
                                newCase.CanTake = true;
                            else
                                newCase.CanTake = false;

                            if (groupToken.Contains("521531d2-9a54-4b4a-ac91-598b3b411089"))
                                newCase.CanDelete = true;
                            else
                                newCase.CanDelete = false;

                            newCase.Transferred = reader.GetBoolean(9);
                            newCase.TransferApprovedBy = !reader.IsDBNull(10) ? reader.GetString(10) : "";
                            newCase.Age = reader.GetInt32(11);
                            newCase.Closed = reader.GetBoolean(12);

                            newCase.AddedBy = reader.GetString(13);
                            newCase.ID = reader.GetInt32(14);

                            if (!reader.IsDBNull(8))
                            {
                                newCase.TakenAt = reader.GetDateTime(8);
                            }

                            cases.Add(newCase);
                        }
                    }
                }

                connection.Close();
            }

            return cases;
        }

        public List<CaseModel> GetCases()
        {
            List<CaseModel> cases = new List<CaseModel>();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Cases";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CaseModel newCase = new CaseModel();

                            newCase.Posted = reader.GetDateTime(1);
                            newCase.CaseID = reader.GetString(2);
                            newCase.Queue = reader.GetString(3);
                            newCase.Severity = reader.GetString(5);
                            newCase.Description = !reader.IsDBNull(6) ? reader.GetString(6) : "";
                            newCase.TakenBy = !reader.IsDBNull(7) ? reader.GetString(7) : "";
                            newCase.Transferred = reader.GetBoolean(9);
                            newCase.TransferApprovedBy = !reader.IsDBNull(10) ? reader.GetString(10) : "";
                            newCase.Age = reader.GetInt32(11);
                            newCase.Closed = reader.GetBoolean(12);

                            newCase.AddedBy = reader.GetString(13);
                            newCase.ID = reader.GetInt32(14);

                            if (!reader.IsDBNull(8))
                            {
                                newCase.TakenAt = reader.GetDateTime(8);
                            }

                            cases.Add(newCase);
                        }
                    }
                }

                connection.Close();
            }

            return cases;
        }

        public List<ExplanationModel> GetExplanations()
        {
            List<ExplanationModel> explanations = new List<ExplanationModel>();

            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Explanations";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ExplanationModel newExp = new ExplanationModel();

                            newExp.Engineer = reader.GetString(0);
                            newExp.Explanation = reader.GetString(1);

                            explanations.Add(newExp);
                        }
                    }
                }

                connection.Close();
            }

            return explanations;
        }

        public void GenerateFullReportDocument()
        {
            string path = "C:/Users/" + Environment.UserName + "/Documents/FullQueueReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".docx";

            List<CaseModel> Cases = GetCases();
            List<ExplanationModel> Explanations = GetExplanations();

            using (var doc = DocX.Create(path))
            {
                var table = doc.InsertTable(Cases.Count + 2, 6);

                table.Rows[0].Cells[0].Paragraphs[0].Append("Cases Taken - " + DateTime.Now.ToString("yyyy-MM-dd")).Bold().FontSize(16).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[0].MergeCells(0, 5);

                table.Rows[1].Cells[0].Paragraphs[0].Append("Sev").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[1].Paragraphs[0].Append("Case #").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[2].Paragraphs[0].Append("Posted At").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[3].Paragraphs[0].Append("Taken By").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[4].Paragraphs[0].Append("Was Transferred").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[5].Paragraphs[0].Append("Taken at age").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;

                for (int i = 0; i < Cases.Count; i++)
                {
                    table.Rows[i + 2].Cells[0].Paragraphs[0].Append(Cases[i].Severity).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[1].Paragraphs[0].Append(Cases[i].CaseID).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[2].Paragraphs[0].Append(Cases[i].PostedString).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[3].Paragraphs[0].Append(Cases[i].TakenBy).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[4].Paragraphs[0].Append(Cases[i].Transferred ? "Yes" : "No").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[5].Paragraphs[0].Append(Cases[i].Age.ToString()).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[5].FillColor = GetRowColor(Cases[i].Age);
                }

                table.Design = Xceed.Document.NET.TableDesign.ColorfulGridAccent5;

                doc.InsertSectionPageBreak();

                var table2 = doc.InsertTable(Explanations.Count + 2, 2);

                table2.Rows[0].Cells[0].Paragraphs[0].Append("Engineer Explanations - " + DateTime.Now.ToString("yyyy-MM-dd")).Bold().FontSize(16).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table2.Rows[0].MergeCells(0, 1);

                table2.Rows[1].Cells[0].Paragraphs[0].Append("Engineer").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table2.Rows[1].Cells[1].Paragraphs[0].Append("Explanation").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;

                for (int i = 0; i < Explanations.Count; i++)
                {
                    table2.Rows[i + 2].Cells[0].Paragraphs[0].Append(Explanations[i].Engineer).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table2.Rows[i + 2].Cells[1].Paragraphs[0].Append(Explanations[i].Explanation).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.left;
                }

                table2.Design = Xceed.Document.NET.TableDesign.ColorfulGridAccent5;
                table2.SetColumnWidth(0, 2500);
                table2.SetColumnWidth(1, 6750);

                doc.Save();
            }

            Process.Start(path);
        }

        public void GenerateCasesTakenDocument()
        {
            string path = "C:/Users/" + Environment.UserName + "/Documents/CasesTakenReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".docx";

            List<CaseModel> Cases = GetCases();
            List<ExplanationModel> Explanations = GetExplanations();

            using (var doc = DocX.Create(path))
            {
                var table = doc.InsertTable(Cases.Count + 2, 6);

                table.Rows[0].Cells[0].Paragraphs[0].Append("Cases Taken - " + DateTime.Now.ToString("yyyy-MM-dd")).Bold().FontSize(16).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[0].MergeCells(0, 4);

                table.Rows[1].Cells[0].Paragraphs[0].Append("Sev").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[1].Paragraphs[0].Append("Case #").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[2].Paragraphs[0].Append("Posted At").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[3].Paragraphs[0].Append("Taken By").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[4].Paragraphs[0].Append("Was Transferred").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table.Rows[1].Cells[5].Paragraphs[0].Append("Taken at age").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;

                for (int i = 0; i < Cases.Count; i++)
                {
                    table.Rows[i + 2].Cells[0].Paragraphs[0].Append(Cases[i].Severity).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[1].Paragraphs[0].Append(Cases[i].CaseID).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[2].Paragraphs[0].Append(Cases[i].PostedString).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[3].Paragraphs[0].Append(Cases[i].TakenBy).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[4].Paragraphs[0].Append(Cases[i].Transferred ? "Yes" : "No").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[5].Paragraphs[0].Append(Cases[i].Age.ToString()).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table.Rows[i + 2].Cells[5].FillColor = GetRowColor(Cases[i].Age);
                }

                table.Design = Xceed.Document.NET.TableDesign.ColorfulGridAccent5;

                doc.Save();
            }

            Process.Start(path);
        }

        public void GenerateEasterEggDocument(string Name)
        {
            try
            {
                string path = "C:/Users/" + Environment.UserName + "/Documents/Your-New-Book.docx";

                using (var doc = DocX.Create(path))
                {
                    var title = doc.InsertParagraph().Append(Name + "'s New Book!").FontSize(24).Bold();
                    var next = doc.InsertParagraph().Append("See, you could create a book! It would be fun :P");

                    doc.Save();
                }

                Process.Start(path);
            } catch (Exception exc)
            {
                return;
            }
        }

        public void GenerateCasesTakenCsv()
        {
            string path = "C:/Users/" + Environment.UserName + "/Documents/CasesTakenReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Severity,Case #,Posted At,Taken By,Was transferred,Taken at age");

            List<CaseModel> Cases = GetCases();

            foreach (CaseModel c in Cases)
            {
                string s = string.Format("{0},{1},{2},{3},{4},{5}", c.Severity, c.CaseID, c.PostedString, c.TakenBy, c.Transferred ? "Yes" : "No", c.Age);
                sb.AppendLine(s);
            }

            File.WriteAllText(path, sb.ToString());

            Process.Start(path);
        }

        public void GenerateExplanationsDocument()
        {
            string path = "C:/Users/" + Environment.UserName + "/Documents/ExplanationsReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".docx";

            List<ExplanationModel> Explanations = GetExplanations();

            using (var doc = DocX.Create(path))
            {
                var table2 = doc.InsertTable(Explanations.Count + 2, 2);

                table2.Rows[0].Cells[0].Paragraphs[0].Append("Engineer Explanations - " + DateTime.Now.ToString("yyyy-MM-dd")).Bold().FontSize(16).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table2.Rows[0].MergeCells(0, 1);

                table2.Rows[1].Cells[0].Paragraphs[0].Append("Engineer").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                table2.Rows[1].Cells[1].Paragraphs[0].Append("Explanation").FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;

                for (int i = 0; i < Explanations.Count; i++)
                {
                    table2.Rows[i + 2].Cells[0].Paragraphs[0].Append(Explanations[i].Engineer).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.center;
                    table2.Rows[i + 2].Cells[1].Paragraphs[0].Append(Explanations[i].Explanation).FontSize(12).SpacingBefore(5).SpacingAfter(5).Alignment = Xceed.Document.NET.Alignment.left;
                }

                table2.Design = Xceed.Document.NET.TableDesign.ColorfulGridAccent5;
                table2.SetColumnWidth(0, 2500);
                table2.SetColumnWidth(1, 6750);

                doc.Save();
            }

            Process.Start(path);
        }

        public void GenerateExplanationsCsv()
        {
            string path = "C:/Users/" + Environment.UserName + "/Documents/ExplanationsReport_" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Engineer,Explanation");

            List<ExplanationModel> Explanations = GetExplanations();

            foreach (ExplanationModel e in Explanations)
            {
                string s = string.Format("{0},{1}", e.Engineer, e.Explanation);
                sb.AppendLine(s);
            }

            File.WriteAllText(path, sb.ToString());

            Process.Start(path);
        }

        private Color GetRowColor(int age)
        {
            if (age < 5)
                return Color.LimeGreen;
            else if (age >= 5 && age < 10)
                return Color.GreenYellow;
            else
                return Color.Red;
        }

        public void ClearQueue()
        {
            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "DELETE FROM Cases";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public void ClearExplanations()
        {
            using (SqlConnection connection = new SqlConnection(SQL_Controller.ConnectionString))
            {
                connection.Open();

                string query = "DELETE FROM Explanations";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
    }
}
