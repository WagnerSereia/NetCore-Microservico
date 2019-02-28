using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using GeekBurger.Orders.Contract.Messages;
using GeekBurger.Productions.Polly;
using GeekBurgerProduction.Interfaces;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;

namespace GeekBurger.OrderChanged.TopicPublisher.Controllers
{
    [Route("api/orders")]
    public class OrdersController : Controller
    {
        private IMapper _mapper;
        private INewOrderService _newOrder;
        private IOrderChangedService _orderChanged;
        private readonly IReadOnlyPolicyRegistry<string> _policyRegistry;
        private readonly ILogger _logger;
        private string _baseUri;
        private readonly string apiUrl;

        public OrdersController(IMapper mapper, INewOrderService newOrder, IOrderChangedService orderChanged, IReadOnlyPolicyRegistry<string> policyRegistry, ILogger<OrdersController> logger)
        {
            _mapper = mapper;
            _newOrder = newOrder;
            _orderChanged = orderChanged;
            _policyRegistry = policyRegistry;
            _logger = logger;

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            _baseUri = config.GetSection("Api:baseUrl").Get<string>();
            apiUrl = "/api/Production";
        }

        [HttpPost("PublisherTopicNewOrders")]
        public IActionResult PublisherTopicNewOrders()
        {
            _newOrder.PublisherNewOrder();
            return Ok();
        }

        [HttpPost("PublisherTopicStartNewOrders")]
        public IActionResult PublisherTopicStartNewOrders([FromBody] OrderChangedMessage order)
        {
            _orderChanged.PublisherStartNewOrder(order);
            return Ok();
        }

        [HttpPost("PublisherFinishedOrder")]
        public IActionResult PublisherFinishedOrder([FromBody] OrderChangedMessage order)
        {
            _orderChanged.PublisherFinishedOrder(order);
            return Ok();
        }

        [HttpGet("GetOrders")]
        public IActionResult GetOrders()
        {

            var orders = _mapper.Map<List<string>>(_orderChanged.GetOrders().ToList());
            return Ok(orders);
        }

        [HttpGet("ChamadaApiTestePolly")]
        public async Task<HttpResponseMessage> PublisherProductionAreaChanged()
        {
            //Teste para Polly
            var client = new HttpClient();
            
            var retryPolicy = _policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(PolicyNames.BasicRetry)
                              ?? Policy.NoOpAsync<HttpResponseMessage>();

            var context = new Context($"GetSomeData-{Guid.NewGuid()}", new Dictionary<string, object>
                {
                    { PolicyContextItems.Logger, _logger }, { "url", apiUrl }
                });

            var retries = 0;
            
            var response = await retryPolicy.ExecuteAsync((ctx) =>
            {
                client.DefaultRequestHeaders.Remove("retries");
                client.DefaultRequestHeaders.Add("retries", new[] { retries++.ToString() });

                var baseUrl = _baseUri;
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    var uri = Request.GetUri();
                    baseUrl = uri.Scheme + Uri.SchemeDelimiter + uri.Host + ":" + uri.Port;
                }

                var isValid = Uri.IsWellFormedUriString(apiUrl, UriKind.Absolute);

                return client.GetAsync($"{baseUrl}{apiUrl}/PublisherProductionAreaChanged");
            }, context);
                        
            return response;            
        }
    }
}