using AutoMapper;
using GeekBurger.Orders.Contract.Messages;
using GeekBurger.Productions.Contract;
using GeekBurger.Productions.Model;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GeekBurger.Productions
{
    public class AutomapperProfile :Profile
    {
        public AutomapperProfile()
        {
            CreateMap<OrderChangedMessage, OrderChangedMessage>();
            CreateMap<Production, ProductionToGet>();         
            CreateMap<EntityEntry<Production>, ProductionAreaChanged>()
            .ForMember(dest => dest.Production, opt => opt.MapFrom(src => src.Entity));
        }
    }
}
