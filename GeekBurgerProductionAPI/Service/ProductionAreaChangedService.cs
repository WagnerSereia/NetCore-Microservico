using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using AutoMapper;
using System.Linq;
using System.Threading;
using GeekBurger.Productions.Service;
using GeekBurger.Productions.Contract;
using Newtonsoft.Json;

namespace GeekBurgerProduction.Services
{
    public class ProductionAreaChangedService : IProductionAreaChangedService
    {
        private const string Topic = "ProductionAreaChanged";
        private List<Message> _messages;
        private IConfiguration _configuration;
        private IMapper _mapper;
        private IServiceBusNamespace _namespace;
        private Task _lastTask;
        public static string StoreId = "8d618778-85d7-411e-878b-846a8eef30c0";

       
        public ProductionAreaChangedService(IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _configuration = configuration;
            _messages = new List<Message>();
            _namespace = _configuration.GetServiceBusNamespace();
            EnsureTopicIsCreated();
        }

        public void EnsureTopicIsCreated()
        {
            if (!_namespace.Topics.List()
                .Any(topic => topic.Name
                    .Equals(Topic, StringComparison.InvariantCultureIgnoreCase)))
                _namespace.Topics.Define(Topic)
                    .WithSizeInMB(1024).Create();
        }

        public async void SendMessagesAsync()
        {
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);

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

            message = new Message(Encoding.UTF8.GetBytes($"Teste"));

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

        public async void PublisherProductionAreaChanged()
        {            
            if (_lastTask != null && !_lastTask.IsCompleted)
                return;

            var config = _configuration.GetSection("serviceBus").Get<ServiceBusConfiguration>();
            var topicClient = new TopicClient(config.ConnectionString, Topic);
                        
            _lastTask = SendMessagesAsync(topicClient, GetMessage(this.ReturnProductionToGetAleatory()));

            await _lastTask;

            var closeTask = topicClient.CloseAsync();
            await closeTask;
            HandleException(closeTask);
        }

        private ProductionToGet ReturnProductionToGetAleatory()
        {
            var productionAleatory = new Random().Next(1, 5);

            var productionId = Guid.Parse("EEC25F39-D40E-4E51-ACA2-DE9AAD600F73");
            switch (productionAleatory)
            {
                case 1:
                    #region ProductionToGet 1
                    var production1 = new ProductionToGet()
                    { 
                        ProductionId = productionId,
                        Restrictions = new string[] { "soy", "gluten" },
                        On = true
                    };
                    return production1;
                    #endregion                   
                case 2:
                    #region ProductionToGet 2
                    var production2 = new ProductionToGet()
                    {
                        ProductionId = productionId,
                        Restrictions = new string[] { "dairy", "gluten" },
                        On = true
                    };
                    return production2;
                    #endregion                    
                case 3:
                    #region ProductionToGet 3
                    var production3 = new ProductionToGet()
                    {
                        ProductionId = productionId,
                        Restrictions = new string[] { "soy", "dairy" },
                        On = true
                    };
                    return production3;
                    #endregion                    
                case 4:
                    #region ProductionToGet 4
                    var production4 = new ProductionToGet()
                    {
                        ProductionId = productionId,
                        Restrictions = new string[] { "soy", "dairy", "gluten" },
                        On = true
                    };
                    return production4;
                    #endregion                    
                default:
                    #region ProductionToGet 5
                    var production5 = new ProductionToGet()
                    {
                        ProductionId = productionId,
                        Restrictions = new string[] { "soy" },
                        On = true
                    };
                    return production5;
                    #endregion                    
            }
        }
        public Message GetMessage(ProductionToGet productionToGet)
        {
            var production = Mapper.Map<ProductionToGet>(productionToGet);
            var orderChangedSerialized = JsonConvert.SerializeObject(production);
            var orderChangedByteArray = Encoding.UTF8.GetBytes(orderChangedSerialized);

            return new Message
            {
                Body = orderChangedByteArray,
                MessageId = Guid.NewGuid().ToString(),
                Label = StoreId
            };
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
    }
}
