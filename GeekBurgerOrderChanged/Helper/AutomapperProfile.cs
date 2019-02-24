using AutoMapper;
using GeekBurger.Orders.Contract.Messages;

namespace GeekBurger.Productions
{
    public class AutomapperProfile :Profile
    {
        public AutomapperProfile()
        {
            CreateMap<OrderChangedMessage, OrderChangedMessage>();        
        }
    }
}
