using System;
using AutoMapper;
using GeekBurger.Productions.Contract;
using GeekBurger.Productions.Service;
using Microsoft.AspNetCore.Mvc;

namespace GeekBurger.Productions.Controllers
{
    [Route("api/production")]
    public class ProductionController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IProductionAreaChangedService _productionAreaChangedService;
        public ProductionController(IMapper mapper, IProductionAreaChangedService productionAreaChangedService)
        {
            _mapper = mapper;
            _productionAreaChangedService = productionAreaChangedService;
        }

        [HttpGet("PublisherProductionAreaChanged")]
        public IActionResult PublisherProductionAreaChanged()
        {
            //Simulação Polly
            var numeroAleatorio = new Random().Next(1, 3);
            if (numeroAleatorio <= 2)
                return BadRequest();
            else
            {
                _productionAreaChangedService.PublisherProductionAreaChanged();
                return Ok();
            }
        }
    }
}