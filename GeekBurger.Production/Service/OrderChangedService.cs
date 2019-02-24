using AutoMapper;
using GeekBurger.Orders.Contract.Messages;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GeekBurger.Productions.Service
{
    public class OrderChangedService : IOrderChangedService
    {
        #region Initial Data
        private const string Topic = "OrderChanged";
        private const string Queue = "myqueue";//"NewOrder";
        private IConfiguration _configuration;
        private IMapper _mapper;
        private List<Message> _messages;
        private Task _lastTask;
        private IServiceBusNamespace _namespace;
        public OrderChangedService(IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _configuration = configuration;
            _messages = new List<Message>();
            _namespace = _configuration.GetServiceBusNamespace();
            EnsureTopicIsCreated();
            EnsureQueueIsCreated();
            PreencheMessagesMockadas();
        }
        #endregion

        #region Common methods
        public Message GetMessage(OrderChangedMessage entity)
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

        #region Topic
        public void EnsureTopicIsCreated()
        {
            if (!_namespace.Topics.List()
                .Any(topic => topic.Name
                    .Equals(Topic, StringComparison.InvariantCultureIgnoreCase)))
                _namespace.Topics.Define(Topic)
                    .WithSizeInMB(1024).Create();
        }
        private void PreencheMessagesMockadas()
        {
            for (int i = 0; i < 1; i++)
            {
                var order = new OrderChangedMessage
                {
                    OrderId = Guid.NewGuid(),
                    StoreId = Guid.Parse("8d618778-85d7-411e-878b-846a8eef30c0"),
                    State = (GeekBurger.Orders.Contract.Enums.OrderState)new Random().Next(1, 3)
                };

                _messages.Add(GetMessage(order));
            }
        }
        public void AddToMessageList(IEnumerable<EntityEntry<OrderChangedMessage>> changes)
        {
            //_messages.AddRange(changes
            //.Where(entity => entity.State != EntityState.Detached
            //        && entity.State != EntityState.Unchanged)
            //.Select(GetMessage).ToList());
        }

        public async void SendTopicOrderChangedMessagesAsync(OrderChangedMessage order)
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

            _lastTask = SendTopicMessagesAsync(topicClient, GetMessage(order));

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            HandleException(closeTask);
        }
        public async void SendTopicStartOrderMessagesAsync(OrderChangedMessage order)
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

            _lastTask = SendTopicMessagesAsync(topicClient, GetMessage(order));

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            HandleException(closeTask);
        }
        public async Task SendTopicMessagesAsync(TopicClient topicClient, Message message)
        {
            int tries = 0;

            var sendTask = topicClient.SendAsync(message);
            await sendTask;
            var success = HandleException(sendTask);

            if (!success)
                Thread.Sleep(10000 * (tries < 60 ? tries++ : tries));
            else
                _messages.Remove(message);

        }

        #endregion

        #region Queue
        public void EnsureQueueIsCreated()
        {
            if (!_namespace.Queues.List()
                .Any(queue => queue.Name
                    .Equals(Queue, StringComparison.InvariantCultureIgnoreCase)))
                _namespace.Queues.Define(Queue)
                    .WithSizeInMB(1024).Create();
        }
        public async void SendQueueNewOrderMessagesAsync()
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var queueClient = new QueueClient(config.ConnectionString, Queue);

            _lastTask = SendAsync(queueClient);

            await _lastTask;

            var closeTask = queueClient.CloseAsync();
            await closeTask;
            HandleException(closeTask);
        }
        public async Task SendAsync(QueueClient queueClient)
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

                var sendTask = queueClient.SendAsync(message);
                await sendTask;
                var success = HandleException(sendTask);

                if (!success)
                    Thread.Sleep(10000 * (tries < 60 ? tries++ : tries));
                else
                    _messages.Remove(message);
            }
        }
        #endregion
    }
}