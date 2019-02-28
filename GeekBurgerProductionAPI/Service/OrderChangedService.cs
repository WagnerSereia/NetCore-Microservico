using AutoMapper;
using GeekBurger.Orders.Contract.Messages;
using GeekBurger.Productions.Service;
using GeekBurgerProduction.Interfaces;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeekBurgerProduction.Services
{
    public class OrderChangedService : IOrderChangedService
    {
        #region Atributos da classe
        private const string Topic = "orderchanged";
        private List<string> _orders = new List<string>();
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IServiceBusNamespace _namespace;
        private Task _lastTask;
        #endregion

        #region Construtores
        public OrderChangedService(IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _configuration = configuration;            
            _namespace = _configuration.GetServiceBusNamespace();
            EnsureTopicIsCreated();
        }
        
        #endregion

        #region Implementação da Interface
        public void AddToMessageList(OrderChangedMessage order)
        {
            if (!_orders.Exists(x => x.Equals(order.OrderId)))
                _orders.Add(order.OrderId.ToString());
        }

        public void RemoveToMessageList(OrderChangedMessage order)
        {
            if (!_orders.Exists(x => x.Equals(order.OrderId.ToString())))
                _orders.Remove(order.StoreId.ToString());
        }

        public async void PublisherFinishedOrder(OrderChangedMessage order)
        {         
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

            order.State = GeekBurger.Orders.Contract.Enums.OrderState.Finished;

            _lastTask = SendMessagesAsync(topicClient, GetMessage(order));

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            if(HandleException(closeTask))
                RemoveToMessageList(order);
        }

        public async void PublisherStartNewOrder(OrderChangedMessage order)
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

            _lastTask = SendMessagesAsync(topicClient, GetMessage(order));

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            if (HandleException(closeTask))
                AddToMessageList(order);
        }
        public IEnumerable<string> GetOrders()
        {
            return _orders;
        }
        #endregion

        #region Metodos auxiliares
        public Message GetMessage(OrderChangedMessage order)
        {
            var orderChanged = Mapper.Map<OrderChangedMessage>(order);
            var orderChangedSerialized = JsonConvert.SerializeObject(orderChanged);
            var orderChangedByteArray = Encoding.UTF8.GetBytes(orderChangedSerialized);

            return new Message
            {
                Body = orderChangedByteArray,
                MessageId = Guid.NewGuid().ToString(),
                Label = orderChanged.StoreId.ToString()
            };
        }
        public void EnsureTopicIsCreated()
        {
            if (!_namespace.Topics.List()
                .Any(topic => topic.Name
                    .Equals(Topic, StringComparison.InvariantCultureIgnoreCase)))
                _namespace.Topics.Define(Topic)
                    .WithSizeInMB(1024).Create();
        }
        public async Task SendMessagesAsync(TopicClient topicClient, Message message)
        {
            int tries = 0;

            var sendTask = topicClient.SendAsync(message);
            await sendTask;
            var success = HandleException(sendTask);

            if (!success)
                Thread.Sleep(10000 * (tries < 60 ? tries++ : tries));
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

        
        #endregion
    }
}
