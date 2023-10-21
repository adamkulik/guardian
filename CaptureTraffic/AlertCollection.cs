using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureTraffic
{
    public class AlertCollection
    {
        public List<AlertRecord> AlertRecords;

        public AlertCollection()
        {
            AlertRecords = new List<AlertRecord>();
        }
    }
}
