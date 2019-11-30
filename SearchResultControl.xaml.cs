using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for SearchResultControl.xaml
    /// </summary>
    public partial class SearchResultControl : UserControl,INotifyPropertyChanged
    {
        private string _Phrase { get; set; }

        public enum _Type
        {
            Category,
            Issue,
            Content,
            NoResults
        }

        public string Phrase
        {
            get { return _Phrase; }
            set
            {
                _Phrase = value;
                RaisePropertyChanged("Phrase");
            }
        }

        public SearchResultControl()
        {
            DataContext = this;

            InitializeComponent();
        }

        public SearchResultControl(_Type type, MainWindow caller)
        {
            DataContext = this;

            InitializeComponent();

            if (type == _Type.Category || type == _Type.Issue)
            {
                ContentHolder.Visibility = Visibility.Collapsed;
                ContentDirectoryHolder.CornerRadius = new CornerRadius(5);
                ContentDirectoryHolder.BorderThickness = new Thickness(3);

                if (type == _Type.Category)
                    ContentDirectoryHolder.BorderBrush = new SolidColorBrush(Colors.Green);
                else if (type == _Type.Issue)
                    ContentDirectoryHolder.BorderBrush = new SolidColorBrush(Colors.LimeGreen);
            }

            else if (type == _Type.NoResults)
            {
                ContentHolder.Visibility = Visibility.Collapsed;
                ContentDirectoryHolder.BorderThickness = new Thickness(3);
                ContentDirectoryHolder.CornerRadius = new CornerRadius(5);
                ContentDirectoryHolder.BorderBrush = new SolidColorBrush(Colors.Red);
                ContentDirectory.Text = "NO RESULTS FOUND";
            }
        }

        public void SetValues(_Type type, string Tech, string Cat, string Iss = null, string Exp = null)
        {
            if (Iss == null)
                ContentDirectory.Text = "CATEGORY: " + Tech + " > " + Cat;
            else if (Exp == null && Iss != null)
                ContentDirectory.Text = "ISSUE: " + Tech + " > " + Cat + " > " + Iss;
            else
                ContentDirectory.Text = Tech + " > " + Cat + " > " + Iss + " > " + Exp;

            if (Exp != null && Exp == "Documentation")
            {
                StackPanel spanel = new StackPanel();

                foreach (string s in Phrase.Split(';'))
                {
                    string Topic = s.Split('|')[0];
                    string Link = s.Split('|')[1];

                    TextBlock newText = new TextBlock();
                    Hyperlink hyperlink = new Hyperlink();
                    Hyperlink copylink = new Hyperlink();
                    hyperlink.NavigateUri = new Uri(Link.Trim().Trim(':'));
                    hyperlink.Click += Hyperlink_Click;
                    hyperlink.Inlines.Add(Topic.Trim().Trim(':'));
                    hyperlink.FontSize = 14;
                    hyperlink.Foreground = new SolidColorBrush(Colors.LightBlue);
                    newText.Inlines.Add(hyperlink);
                    newText.Margin = new Thickness(5,5,0,5);

                    spanel.Children.Add(newText);
                }

                ContentHolder.Child = spanel;
            }
            else if (Exp != null && Exp != "Documentation")
            {
                Content.Text = Phrase.Replace(';', '\n');
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink source = sender as Hyperlink;
            System.Diagnostics.Process.Start(source.NavigateUri.ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string propertyName)
        {
            var handlers = PropertyChanged;

            handlers(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
