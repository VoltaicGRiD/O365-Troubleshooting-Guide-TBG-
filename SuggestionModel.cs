using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBG_WPF
{
    public class SuggestionModel
    {
        public int ID { get; set; }
        public string SubmittedBy { get; set; }
        public string Technology { get; set; }
        public string Category { get; set; }
        public string Issue { get; set; }
        public string Suggestion { get; set; }
    }
}
