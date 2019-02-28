using GeekBurgerProduction.Services;
using GeekBurger.Orders.Contract;
using GeekBurger.Orders.Contract.Messages;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using GeekBurger.Productions.Contract;

namespace GeekBurgerProduction
{
    class Program
    {
        #region Geral
        private static string _storeId;
        private static IConfiguration _configuration;
        private static ServiceBusConfiguration serviceBusConfiguration;
        private const string SubscriptionName = "ProductionMicroserviceSubscription";
        private static List<string> listOrdersId;
        private const string TopicNewOrder = "neworder";
        private const string TopicOrderChanged = "orderchanged";
        private const string TopicProductionAreaChanged = "ProductionAreaChanged";
        public static string StoreId = "8d618778-85d7-411e-878b-846a8eef30c0";
        #endregion
        static void Main(string[] args)
        {
            #region Geral Configuration
            if (args.Length <= 0)
            {
                _storeId = StoreId;// Console.ReadLine();
            }
            else
                _storeId = args[0];

            listOrdersId = new List<string>();
            //https://github.com/Azure-Samples/service-bus-dotnet-manage-publish-subscribe-with-basic-features

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            serviceBusConfiguration = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            
            var serviceBusNamespace = _configuration.GetServiceBusNamespace();

            #endregion

            #region Topic NewOrder
            var topic = serviceBusNamespace.Topics.GetByName(TopicNewOrder);

            topic.Subscriptions.DeleteByName(SubscriptionName);

            if (!topic.Subscriptions.List()
                   .Any(subscription => subscription.Name
                       .Equals(SubscriptionName, StringComparison.InvariantCultureIgnoreCase)))
                topic.Subscriptions
                    .Define(SubscriptionName)
                    .Create();
            #endregion

            #region Topic OrderChanged
            topic = serviceBusNamespace.Topics.GetByName(TopicOrderChanged);

            topic.Subscriptions.DeleteByName(SubscriptionName);

            if (!topic.Subscriptions.List()
                   .Any(subscription => subscription.Name
                       .Equals(SubscriptionName, StringComparison.InvariantCultureIgnoreCase)))
                topic.Subscriptions
                    .Define(SubscriptionName)
                    .Create();
            #endregion

            #region Topic ProductionAreaChanged
            topic = serviceBusNamespace.Topics.GetByName(TopicProductionAreaChanged);

            topic.Subscriptions.DeleteByName(SubscriptionName);

            if (!topic.Subscriptions.List()
                   .Any(subscription => subscription.Name
                       .Equals(SubscriptionName, StringComparison.InvariantCultureIgnoreCase)))
                topic.Subscriptions
                    .Define(SubscriptionName)
                    .Create();
            #endregion

            ReceiveMessagesNewOrderAsync();
            ReceiveMessagesOrderChangedAsync();
            ReceiveMessageProductionAreaChangedAsync();
            //PublisherNewOrderAsync();
            //PublisherOrderChangedAsync();

            Console.ReadKey();
        }
        private static async void ReceiveMessagesNewOrderAsync()
        {
            #region NewOrder
            var subscriptionClient = new SubscriptionClient(serviceBusConfiguration.ConnectionString, TopicNewOrder, SubscriptionName);

            //by default a 1=1 rule is added when subscription is created, so we need to remove it
            await subscriptionClient.RemoveRuleAsync("$Default");

            await subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter { Label = _storeId },
                Name = "filter-store"
            });

            var mo = new MessageHandlerOptions(ExceptionHandle) { AutoComplete = true };

            subscriptionClient.RegisterMessageHandler(MessageTopicHandleNewOrder, mo);
            Console.WriteLine($"Topico {TopicNewOrder} escutada!");
            #endregion

            Console.ReadKey();
        }
        private static async void ReceiveMessagesOrderChangedAsync()
        {
            #region OrderChanged
            var subscriptionClient = new SubscriptionClient(serviceBusConfiguration.ConnectionString, TopicOrderChanged, SubscriptionName);

            //by default a 1=1 rule is added when subscription is created, so we need to remove it
            await subscriptionClient.RemoveRuleAsync("$Default");

            await subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter { Label = _storeId },
                Name = "filter-store"
            });

            var mo = new MessageHandlerOptions(ExceptionHandle) { AutoComplete = true };

            subscriptionClient.RegisterMessageHandler(MessageTopicHandleOrderChanged, mo);
            Console.WriteLine($"Topico {TopicOrderChanged} escutada!");
            #endregion

            Console.ReadKey();
        }
        private static async void ReceiveMessageProductionAreaChangedAsync()
        {
            #region OrderChanged
            var subscriptionClient = new SubscriptionClient(serviceBusConfiguration.ConnectionString, TopicProductionAreaChanged, SubscriptionName);

            //by default a 1=1 rule is added when subscription is created, so we need to remove it
            await subscriptionClient.RemoveRuleAsync("$Default");

            await subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter { Label = _storeId },
                Name = "filter-store"
            });

            var mo = new MessageHandlerOptions(ExceptionHandle) { AutoComplete = true };

            subscriptionClient.RegisterMessageHandler(MessageTopicHandleProductionAreaChanged, mo);
            Console.WriteLine($"Topico {TopicProductionAreaChanged} escutada!");
            #endregion

            Console.ReadKey();
        }
        private static Task MessageTopicHandleNewOrder(Message message, CancellationToken arg2)
        {
            if (message.Label != _storeId)
            {
                return Task.CompletedTask;
            }

            var orderChangesString = Encoding.UTF8.GetString(message.Body);

            var orderChanged = JsonConvert.DeserializeObject<OrderChangedMessage>(orderChangesString);

            var statusOrder =
                orderChanged.State == GeekBurger.Orders.Contract.Enums.OrderState.Canceled ? "CANCELED" :
                orderChanged.State == GeekBurger.Orders.Contract.Enums.OrderState.Finished ? "FINISHED" : "PAID";

            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("TOPIC - NewOrder received");
            Console.WriteLine($"OrderId: {orderChanged.OrderId.ToString()} - status: {statusOrder}");
            Console.WriteLine("---------------------------------------------------------------");

            return Task.CompletedTask;
        }
        private static Task MessageTopicHandleOrderChanged(Message message, CancellationToken arg2)
        {
            if (message.Label != _storeId)
            {
                return Task.CompletedTask;
            }

            var orderChangesString = Encoding.UTF8.GetString(message.Body);

            var orderChanged = JsonConvert.DeserializeObject<OrderChangedMessage>(orderChangesString);

            var statusOrder =
                orderChanged.State == GeekBurger.Orders.Contract.Enums.OrderState.Canceled ? "CANCELED" :
                orderChanged.State == GeekBurger.Orders.Contract.Enums.OrderState.Finished ? "FINISHED" : "PAID";

            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine("TOPIC - OrderChanged received");
            Console.WriteLine($"OrderId: {orderChanged.OrderId.ToString()} - status: {statusOrder}");
            Console.WriteLine("---------------------------------------------------------------");

            return Task.CompletedTask;
        }
        private static Task MessageTopicHandleProductionAreaChanged(Message message, CancellationToken arg2)
        {
            if (message.Label != _storeId)
            {
                return Task.CompletedTask;
            }

            var productionAreaChagedString = Encoding.UTF8.GetString(message.Body);

            var productionAreaChanged = JsonConvert.DeserializeObject<ProductionToGet>(productionAreaChagedString);
                      

            Console.WriteLine("---------------------------------------------------------------");
            Console.WriteLine($"TOPIC - {TopicProductionAreaChanged} received");
            Console.WriteLine($"Production: {productionAreaChanged.Restrictions.FirstOrDefault().ToString()}");
            Console.WriteLine("---------------------------------------------------------------");

            return Task.CompletedTask;
        }
        private static Task ExceptionHandle(ExceptionReceivedEventArgs arg)
        {
            Console.WriteLine($"Message handler encountered an exception {arg.Exception}.");
            var context = arg.ExceptionReceivedContext;
            Console.WriteLine($"- Endpoint: {context.Endpoint}, Path: {context.EntityPath}, Action: {context.Action}");
            return Task.CompletedTask;
        }
        
        #region COMENTADO
        /*
        private static async void PublisherNewOrderAsync()
        {
            var order = new OrderChangedMessage
            {
                OrderId = Guid.NewGuid(),
                StoreId = Guid.Parse(StoreId),
                State = (GeekBurger.Orders.Contract.Enums.OrderState)new Random().Next(1, 3)
            };
            _orderChangedService.PublisherStartNewOrder(order);

            Random randNum = new Random();
            var valorRandomico = randNum.Next(20, 30);
            Thread.Sleep(1000 * valorRandomico);
            Console.WriteLine($"NewOrder published - orderId: {order.OrderId}");
        }
        private static async void PublisherOrderChangedAsync()
        {
            var order = new OrderChangedMessage
            {
                OrderId = Guid.NewGuid(),
                StoreId = Guid.Parse(StoreId),
                State = (GeekBurger.Orders.Contract.Enums.OrderState)new Random().Next(1, 3)
            };
            _orderChangedService.PublisherFinishedOrder(order);

            Random randNum = new Random();
            var valorRandomico = randNum.Next(20, 30);
            Thread.Sleep(1000 * valorRandomico);
            Console.WriteLine($"OrderChanged published - orderId: {order.OrderId}");
        }
        */
        #endregion
    }
}
