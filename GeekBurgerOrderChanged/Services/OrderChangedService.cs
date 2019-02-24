using AutoMapper;
using GeekBurger.Orders.Contract.Messages;
//using GeekBurger.Products.Contract;
//using GeekBurger.Products.Model;
//using GeekBurger.Products.Repository;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeekBurger.Orders.Topic.Services
{
    public class OrderChangedService : IOrderChangedService
    {
        private const string Topic = "OrderChanged";
        private IConfiguration _configuration;
        private IMapper _mapper;
        private List<Message> _messages;
        private Task _lastTask;
        private IServiceBusNamespace _namespace;

        HttpClient client;
        private static string _urlBase;

        public OrderChangedService(IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _configuration = configuration;            
            _messages = new List<Message>();
            _namespace = _configuration.GetServiceBusNamespace();
            EnsureTopicIsCreated();
            PreparaDadosApi();
        }
        public OrderChangedService(IConfiguration configuration)
        {
            _configuration = configuration;
            PreparaDadosApi();
        }
        public void EnsureTopicIsCreated()
        {
            if (!_namespace.Topics.List()
                .Any(topic => topic.Name
                    .Equals(Topic, StringComparison.InvariantCultureIgnoreCase)))
                _namespace.Topics.Define(Topic)
                    .WithSizeInMB(1024).Create();

        }

        public void AddToMessageList(IEnumerable<EntityEntry<OrderChangedMessage>> changes)
        {
            _messages.AddRange(changes
            .Where(entity => entity.State != EntityState.Detached
                    && entity.State != EntityState.Unchanged)
            .Select(GetMessage).ToList());
        }

        public Message GetMessage(EntityEntry<OrderChangedMessage> entity)
        {
            var orderChanged = Mapper.Map<OrderChangedMessage>(entity);
            var orderChangedSerialized = JsonConvert.SerializeObject(orderChanged);
            var orderChangedByteArray = Encoding.UTF8.GetBytes(orderChangedSerialized);
                        
            return new Message
            {
                Body = orderChangedByteArray,
                MessageId = Guid.NewGuid().ToString(),
                Label = orderChanged.StoreId.ToString()
            };
        }

        public async void SendMessagesAsync()
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

            //_logService.SendMessagesAsync("Product was changed");

            _lastTask = SendAsync(topicClient);

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            HandleException(closeTask);
        }

        public async Task SendAsync(TopicClient topicClient)
        {
            int tries = 0;
            Message message;
            while (true)
            {
                if (_messages.Count <= 0)
                    break;

                lock (_messages)
                {
                    message = _messages.FirstOrDefault();
                }

                var sendTask = topicClient.SendAsync(message);
                await sendTask;
                var success = HandleException(sendTask);

                if (!success)
                    Thread.Sleep(10000 * (tries < 60 ? tries++ : tries));
                else
                    _messages.Remove(message);
            }
        }

        public bool HandleException(Task task)
        {
            if (task.Exception == null || task.Exception.InnerExceptions.Count == 0) return true;

            task.Exception.InnerExceptions.ToList().ForEach(innerException =>
            {
                Console.WriteLine($"Error in SendAsync task: {innerException.Message}. Details:{innerException.StackTrace} ");

                if (innerException is ServiceBusCommunicationException)
                    Console.WriteLine("Connection Problem with Host. Internet Connection can be down");
            });

            return false;
        }

        private void PreparaDadosApi()
        {
            _urlBase = "http://localhost:26346/api/";// _configuration.GetSection("API_Access:UrlBase").Value;

            if (client == null)
            {
                client = new HttpClient();

                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));                
            }
        }
        public async Task<bool> PublisherStartNewOrder(OrderChangedMessage order)
        {
            var orderChanged = JsonConvert.SerializeObject(order);
           
            var stringContent = new StringContent(orderChanged, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_urlBase+"orders/PublisherTopicStartNewOrders ", stringContent);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("NewOrder has not been published on Topic NewOrder"); 
                return false;
            }
            Console.WriteLine("NewOrder published on Topic NewOrder");
            return true;
        }

        public async Task<bool> PublisherFinishedOrder(OrderChangedMessage order)
        {
            var orderChanged = JsonConvert.SerializeObject(order);

            var stringContent = new StringContent(orderChanged, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_urlBase + "orders/PublisherTopicOrderChanged", stringContent);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Order Finished has not been published on Topic NewOrder");
                return false;
            }
            Console.WriteLine("Order Finished published on Topic NewOrder");
            return true;
        }
    }
}