using System;
using System.Collections.Generic;
using System.Text;

namespace CostManagementDBFiller.Models
{
    class Subscription
    {
        public string subscriptionId { get; set; }
        public string name { get; set; }
        public int clientId { get; set; }
        public int? clientAppId { get; set; }
        public virtual ClientApp clientApp { get; set; }
    }
}
