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
    public class NewOrderService : INewOrderService
    {
        #region Atributos da classe
        private const string Topic = "neworder";
        private List<Message> _messages;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IServiceBusNamespace _namespace;
        private IOrderChangedService _orderChanged;
        private Task _lastTask;
        public static string StoreId = "8d618778-85d7-411e-878b-846a8eef30c0";
        #endregion

        #region Construtores
        public NewOrderService(IMapper mapper, IConfiguration configuration,IOrderChangedService orderChanged)
        {
            _mapper = mapper;
            _configuration = configuration;
            _orderChanged = orderChanged;
            _messages = new List<Message>();
            _namespace = _configuration.GetServiceBusNamespace();
            EnsureTopicIsCreated();
        }
        #endregion

        #region Implementação da Interface
        public async void PublisherNewOrder()
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

            var order = new OrderChangedMessage
            {
                OrderId = Guid.NewGuid(),
                StoreId = Guid.Parse(StoreId),
                State = (GeekBurger.Orders.Contract.Enums.OrderState)new Random().Next(1, 3)
            };

            _lastTask = SendMessagesAsync(topicClient, GetMessage(order));

            _orderChanged.PublisherStartNewOrder(order);

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            HandleException(closeTask);
        }

        #endregion

        #region Metodos auxiliares
        public Message GetMessage(OrderChangedMessage order)
        {
            var orderChanged = order;// Mapper.Map<OrderChangedMessage>(order);
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