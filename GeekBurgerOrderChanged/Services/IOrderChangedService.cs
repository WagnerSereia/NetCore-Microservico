using GeekBurger.Orders.Contract.Messages;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeekBurger.Orders.Topic.Services
{
    public interface IOrderChangedService
    {
        void SendMessagesAsync();
        void AddToMessageList(IEnumerable<EntityEntry<OrderChangedMessage>> changes);
        Task<bool> PublisherStartNewOrder(OrderChangedMessage order);
        Task<bool> PublisherFinishedOrder(OrderChangedMessage order);
    }
}