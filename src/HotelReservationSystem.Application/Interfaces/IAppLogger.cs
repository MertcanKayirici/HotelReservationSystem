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
        void Info(string message, params object[] args);

        void Warning(string message);
        void Warning(string message, params object[] args);

        void Error(string message);
        void Error(Exception ex, string message, params object[] args);
    }
}
