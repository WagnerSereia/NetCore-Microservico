using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeekBurger.Productions.Service
{
    public interface ILogService
    {
        void SendMessagesAsync(string message);
    }
}
