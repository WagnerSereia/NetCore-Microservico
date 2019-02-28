using GeekBurger.Orders.Contract.Messages;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeekBurgerProduction.Interfaces
{
    public interface IOrderChangedService
    {
        void AddToMessageList(OrderChangedMessage order);
        void RemoveToMessageList(OrderChangedMessage order);
        void PublisherStartNewOrder(OrderChangedMessage order);
        void PublisherFinishedOrder(OrderChangedMessage order);
        IEnumerable<string> GetOrders();
    }
}
