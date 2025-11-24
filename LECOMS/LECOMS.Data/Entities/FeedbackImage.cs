using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class FeedbackImage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FeedbackId { get; set; }
        public string Url { get; set; }

        // Nav
        public Feedback Feedback { get; set; }
    }
}
