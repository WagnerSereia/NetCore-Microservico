using System;
using System.Collections.Generic;
using System.Text;

namespace GeekBurger.Orders.Topic.Services
{
    internal class ServiceBusConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        public string NamespaceName { get; set; }
        public string ConnectionString { get; set; }
        public string UrlBaseAPI { get; set; }
    }
}
