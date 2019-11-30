using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TBG_WPF
{
    public class CaseModel : INotifyPropertyChanged
    {
        private int _id { get; set; }
        private DateTime _posted { get; set; }
        private string _caseID { get; set; }
        private string _queue { get; set; }
        private string _severity { get; set; }
        private string _description { get; set; }
        private string _displayDescription { get; set; }
        private string _takenBy { get; set; }
        private DateTime _takenAt { get; set; }
        private bool _transferred { get; set; }
        private string _transferApprovedBy { get; set; }
        private bool _closed { get; set; }
        private bool _canTransfer { get; set; }
        private bool _canTake { get; set; }
        private bool _canDelete { get; set; }
        private string _addedBy { get; set; }
        private int _age { get; set; }
        private string _caseLink { get { return "https://rave.office.net/cases/" + _caseID; } }
        private SolidColorBrush _color
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TakenBy))
                {
                    if (Age < 5)
                        return new SolidColorBrush(Colors.DarkGreen);
                    else if (Age >= 5 && Age < 10)
                        return new SolidColorBrush(Colors.DarkOrange);
                    else
                        return new SolidColorBrush(Colors.Maroon);
                }
                else
                {
                    return new SolidColorBrush(Colors.Black);
                }
            }
        }
        private SolidColorBrush _textColor
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TakenBy))
                {
                    return new SolidColorBrush(Colors.White);
                }
                else
                {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }
        private SolidColorBrush _severityColor
        {
            get
            {
                if (_severity.Contains("A") && string.IsNullOrWhiteSpace(TakenBy))
                    return new SolidColorBrush(Colors.Blue);
                else
                    return new SolidColorBrush(Colors.Black);
            }
        }

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                RaisePropertyChange("ID");
            }
        }

        public DateTime Posted
        {
            get
            {
                return _posted;
            }
            set
            {
                _posted = value;
                RaisePropertyChange("Posted");
            }
        }

        public string PostedString
        {
            get
            {
                return _posted.ToString("hh:mm");
            }
        }

        public string CaseID
        {
            get
            {
                return _caseID;
            }
            set
            {
                _caseID = value;
                RaisePropertyChange("CaseID");
            }
        }

        public string Queue
        {
            get
            {
                return _queue;
            }
            set
            {
                _queue = value;
                RaisePropertyChange("Queue");
            }
        }

        public string CaseLink
        {
            get
            {
                return _caseLink;
            }
        }

        public string Severity
        {
            get
            {
                return _severity;
            }
            set
            {
                _severity = value;
                RaisePropertyChange("Severity");
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                RaisePropertyChange("Description");
            }
        }

        public string DisplayDescription
        {
            get
            {
                if (Description.Length > 100)
                {
                    return Description.Substring(0, 100) + "... (click to see more)";
                }
                else 
                {
                    return Description;
                }
            }
        }

        public string TakenBy
        {
            get
            {
                return _takenBy;
            }
            set
            {
                _takenBy = value;
                RaisePropertyChange("TakenBy");
            }
        }

        public DateTime TakenAt
        {
            get
            {
                return _takenAt;
            }
            set
            {
                _takenAt = value;
                RaisePropertyChange("TakenAt");
            }
        }

        public string TakenAtString
        {
            get
            {
                if (_takenAt.Year > 2018)
                    return _takenAt.ToString("hh:mm");
                else
                    return "";
            }
        }

        public bool Transferred
        {
            get
            {
                return _transferred;
            }
            set
            {
                _transferred = value;
                RaisePropertyChange("Transferred");
            }
        }

        public string TransferApprovedBy
        {
            get
            {
                return _transferApprovedBy;
            }
            set
            {
                _transferApprovedBy = value;
                RaisePropertyChange("TransferApprovedBy");
            }
        }

        public bool Closed
        {
            get
            {
                return _closed;
            }
            set
            {
                _closed = value;
                RaisePropertyChange("Closed");
            }
        }

        public bool CanTransfer
        {
            get
            {
                return _canTransfer;
            }
            set
            {
                _canTransfer = value;
                RaisePropertyChange("CanTransfer");
            }
        }

        public bool CanDelete
        {
            get
            {
                return _canDelete;
            }
            set
            {
                _canDelete = value;
                RaisePropertyChange("CanDelete");
            }
        }

        public bool CanTake
        {
            get
            {
                return _canTake;
            }
            set
            {
                _canTake = value;
                RaisePropertyChange("CanTake");
            }
        }

        public string AddedBy
        {
            get
            {
                return _addedBy;
            }
            set
            {
                _addedBy = value;
                RaisePropertyChange("AddedBy");
            }
        }

        public int Age
        {
            get
            {
                return _age;
            }
            set
            {
                _age = value;
                RaisePropertyChange("Age");
            }
        }

        public SolidColorBrush Color
        {
            get
            {
                return _color;
            }
        }

        public SolidColorBrush TextColor
        {
            get
            {
                return _textColor;
            }
        }

        public SolidColorBrush SeverityColor
        {
            get
            {
                return _severityColor;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChange(string propertyname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
    }
}
