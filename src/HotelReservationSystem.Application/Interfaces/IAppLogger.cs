using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Interfaces
{
    public interface IAppLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
