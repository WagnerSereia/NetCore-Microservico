using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using GeekBurger.Orders.Contract.Messages;
using GeekBurger.Productions.Service;
using Microsoft.AspNetCore.Mvc;

namespace GeekBurger.OrderChanged.TopicPublisher.Controllers
{
    [Route("api/orders")]
    public class OrdersController : Controller
    {
        private IMapper _mapper;
        private IOrderChangedService _order;

        public OrdersController(IMapper mapper, IOrderChangedService order)
        {
            _mapper = mapper;
            _order = order;
        }

        [HttpPost("PublisherQueueNewOrders")]
        public IActionResult PublisherQueueNewOrders()
        {
            _order.SendQueueNewOrderMessagesAsync();
            return Ok();
        }

        [HttpPost("PublisherTopicStartNewOrders")]
        public IActionResult PublisherTopicStartNewOrders([FromBody] OrderChangedMessage order)
        {
            _order.SendTopicStartOrderMessagesAsync(order);
            return Ok();
        }

        [HttpPost("PublisherTopicOrderChanged")]
        public IActionResult PublisherTopicOrderChanged([FromBody] OrderChangedMessage order)
        {
            _order.SendTopicOrderChangedMessagesAsync(order);
            return Ok();
        }

    }
}