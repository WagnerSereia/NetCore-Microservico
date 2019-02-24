using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeekBurger.Orders.Contract;
using GeekBurger.Orders.Contract.Messages;
using GeekBurger.Orders.Topic.Services;
using GeekBurger.Productions.Contract;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SubscriptionClient = Microsoft.Azure.ServiceBus.SubscriptionClient;

namespace GeekBurger.Orders.Topic
{
    class Program
    {
        #region Geral
        private static string _storeId;
        private static IConfiguration _configuration;
        private static ServiceBusConfiguration serviceBusConfiguration;
        private const string SubscriptionName = "Los_Angeles_Beverly_Hills";
        private static List<string> listOrdersId;
        #endregion
        
        #region Topic
        private const string TopicName = "OrderChanged";
        #endregion

        #region Queue
        const string QueuePath = "myqueue";//"NewOrder";
        static IQueueClient _queueClient;        
        private static List<Task> PendingCompleteTasks;
        private static int count;
        #endregion
        private static OrderChangedService _orderChangeService;
        static void Main(string[] args)
        {
            #region Geral Configuration
            if (args.Length <= 0)
            {                
                _storeId = "8d618778-85d7-411e-878b-846a8eef30c0";// Console.ReadLine();
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

            _orderChangeService = new OrderChangedService(_configuration);

            var serviceBusNamespace = _configuration.GetServiceBusNamespace();
            
            #endregion

            #region Topic
            var topic = serviceBusNamespace.Topics.GetByName(TopicName);

            topic.Subscriptions.DeleteByName(SubscriptionName);

            if (!topic.Subscriptions.List()
                   .Any(subscription => subscription.Name
                       .Equals(SubscriptionName, StringComparison.InvariantCultureIgnoreCase)))
                topic.Subscriptions
                    .Define(SubscriptionName)
                    .Create();
            #endregion

            #region Queue
            PendingCompleteTasks = new List<Task>();            
            #endregion

            ReceiveMessagesTopicAsync();
            ReceiveMessagesQueueAsync();

            Console.ReadLine();
        }

        private static async void ReceiveMessagesTopicAsync()
        {
            var subscriptionClient = new SubscriptionClient(serviceBusConfiguration.ConnectionString, TopicName, SubscriptionName);

            //by default a 1=1 rule is added when subscription is created, so we need to remove it
            await subscriptionClient.RemoveRuleAsync("$Default");

            await subscriptionClient.AddRuleAsync(new RuleDescription
            {
                Filter = new CorrelationFilter { Label = _storeId },
                Name = "filter-store"
            });

            var mo = new MessageHandlerOptions(ExceptionHandle) { AutoComplete = true };

            subscriptionClient.RegisterMessageHandler(MessageTopicHandle, mo);

            Console.WriteLine("Topico sendo escutada!");
            Console.ReadKey();
        }
        private static async void ReceiveMessagesQueueAsync()
        {
            _queueClient = new QueueClient(_configuration["serviceBus:ConnectionString"], QueuePath, ReceiveMode.PeekLock);
            var handlerOptions = new MessageHandlerOptions(ExceptionHandle) { AutoComplete = false, MaxConcurrentCalls = 3 };
            _queueClient.RegisterMessageHandler(MessageQueueHandler, handlerOptions);


            //Console.WriteLine($"Request to close async");
            //Console.WriteLine($"pending tasks: {PendingCompleteTasks.Count}");
            //await Task.WhenAll(PendingCompleteTasks);
            //Console.WriteLine($"All pending tasks were completed");
            Console.WriteLine("Fila sendo escutada!");
            Console.ReadKey();
            //await _queueClient.CloseAsync();
        }

        private static Task MessageTopicHandle(Message message, CancellationToken arg2)
        {
            //Console.WriteLine($"message Label: {message.Label}");
            //Console.WriteLine($"message CorrelationId: {message.CorrelationId}");
            var orderChangesString = Encoding.UTF8.GetString(message.Body);
            

            var orderChanged = JsonConvert.DeserializeObject<OrderChangedMessage>(orderChangesString);

            if (orderChanged.State == Contract.Enums.OrderState.Paid)
            {
                if (listOrdersId.Exists(x => x.Equals(orderChanged.OrderId.ToString())))
                {
                    Random randNum = new Random();
                    var valorRandomico = randNum.Next(1, 2);
                    Thread.Sleep(1000 * valorRandomico);

                    listOrdersId.Remove(orderChanged.OrderId.ToString());
                    orderChanged.State = Contract.Enums.OrderState.Finished;
                    _orderChangeService.PublisherFinishedOrder(orderChanged).GetAwaiter().GetResult();
                    Console.WriteLine("TOPIC - Order Finished");
                }                
            }
            else
            {
                if (listOrdersId.Exists(x => x.Equals(orderChanged.OrderId.ToString())))
                    listOrdersId.Remove(orderChanged.OrderId.ToString());
            }

            //Console.WriteLine("TOPIC - NewOrder Received");
            //Console.WriteLine(orderChangesString);

            return Task.CompletedTask;
        }
        private static async Task MessageQueueHandler(Message message, CancellationToken cancellationToken)
        {
            if (_queueClient.IsClosedOrClosing)
                return;

            if (message.Label != _storeId)
            {
                //Console.WriteLine($"Message From Store: {message.Label} with id {message.MessageId} not processed");
                return;
            }

            var orderChangesString = Encoding.UTF8.GetString(message.Body);
            var orderChanged = JsonConvert.DeserializeObject<OrderChangedMessage>(orderChangesString);

            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine($"NewOrder received: {orderChangesString}");

            Random randNum = new Random();
            var valorRandomico = randNum.Next(1,2);
            Thread.Sleep(1000 * valorRandomico);

            //publica inicio da ordem de fabricação
            if (orderChanged.State == Contract.Enums.OrderState.Paid)
            {
                _orderChangeService.PublisherStartNewOrder(orderChanged).GetAwaiter().GetResult();
                
                listOrdersId.Add(orderChanged.OrderId.ToString());
                Console.WriteLine($"Message on Queue Processed: {orderChangesString}");
            }
            else
            {
                if (listOrdersId.Exists(x => x.Equals(orderChanged.OrderId.ToString())))
                {
                    listOrdersId.Remove(orderChanged.OrderId.ToString());
                    Console.WriteLine($"Cancelled|Finished order has not been processed!");
                }else
                    Console.WriteLine($"Order waiting for paid!");
            }

            
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine("");
            //Console.WriteLine($"task {count++}");

            Task PendingCompleteTask;
            lock (PendingCompleteTasks)
            {
                PendingCompleteTasks.Add(_queueClient.CompleteAsync(message.SystemProperties.LockToken));
                PendingCompleteTask = PendingCompleteTasks.LastOrDefault();
            }

            //Console.WriteLine($"calling complete for task {count}");

            await PendingCompleteTask;

            //Console.WriteLine($"remove task {count} from task queue");
            PendingCompleteTasks.Remove(PendingCompleteTask);
        }
        private static Task ExceptionHandle(ExceptionReceivedEventArgs arg)
        {
            Console.WriteLine($"Message handler encountered an exception {arg.Exception}.");
            var context = arg.ExceptionReceivedContext;
            Console.WriteLine($"- Endpoint: {context.Endpoint}, Path: {context.EntityPath}, Action: {context.Action}");
            return Task.CompletedTask;
        }               

        
    }
}
