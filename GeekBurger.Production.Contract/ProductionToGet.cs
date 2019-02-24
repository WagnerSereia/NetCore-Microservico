using System;
using System.Collections.Generic;
using System.Text;

namespace GeekBurger.Productions.Contract
{
    public class ProductionToGet
    {
        public Guid ProductionId {get;set;}
        public string[] Restrictions {get;set;}
        public bool? On { get; set; }
    } 
}
