using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBG_WPF
{
    public class NotificationModel
    {
        public int ID { get; set; }
        public enum NotificationType
        {
            Warning,
            Alert
        }
        public NotificationType Type { get; set; }
        public string Text { get; set; }
        public DateTime Posted { get; set; }
    }
}
