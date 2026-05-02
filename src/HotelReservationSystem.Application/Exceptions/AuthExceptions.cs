using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelReservationSystem.Application.Exceptions
{
    internal class AuthExceptions
    {
        public class UserAlreadyExistsException : Exception
        {
            public UserAlreadyExistsException() : base("USER_ALREADY_EXISTS") { }
        }

        public class InvalidCredentialsException : Exception
        {
            public InvalidCredentialsException() : base("INVALID_CREDENTIALS") { }
        }
    }
}
