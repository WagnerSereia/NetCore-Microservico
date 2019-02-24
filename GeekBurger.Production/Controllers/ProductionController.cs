using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using GeekBurger.Productions.Contract;
using Microsoft.AspNetCore.Mvc;

namespace GeekBurger.Productions.Controllers
{
    [Route("api/production")]
    public class ProductionController : Controller
    {
        private readonly IMapper _mapper;

        public ProductionController(IMapper mapper)
        {
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetAreas()
        {
            var production = new ProductionToGet()
            {
                ProductionId = Guid.NewGuid(),
                Restrictions = new string[] { "soy", "dairy", "gluten" },
                On = true
            };

            var productionToGet = _mapper.Map<ProductionToGet>(production);

            return Ok(production);
        }
    }
}