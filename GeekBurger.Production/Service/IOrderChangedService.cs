using GeekBurger.Orders.Contract.Messages;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;

namespace GeekBurger.Productions.Service
{
    public interface IOrderChangedService
    {
        void SendQueueNewOrderMessagesAsync();
        void SendTopicOrderChangedMessagesAsync(OrderChangedMessage order);
        void SendTopicStartOrderMessagesAsync(OrderChangedMessage order);
        void AddToMessageList(IEnumerable<EntityEntry<OrderChangedMessage>> changes);
    }
}