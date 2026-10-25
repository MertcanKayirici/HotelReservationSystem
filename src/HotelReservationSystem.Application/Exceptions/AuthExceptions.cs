namespace HotelReservationSystem.Application.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException()
        : base("USER_ALREADY_EXISTS") { }
}

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS") { }
}

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("INVALID_REFRESH_TOKEN") { }
}

public class UserNotFoundException : Exception
{
    public UserNotFoundException()
        : base("USER_NOT_FOUND") { }
}