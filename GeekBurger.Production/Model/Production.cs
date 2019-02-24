using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekBurger.Productions.Model
{
    public class Production
    {
        public Guid ProductionId { get; set; }
        public string[] Restrictions { get; set; }
        public bool? On { get; set; }
    }
}
