namespace BlueSandsLMS.Application.Services;

public sealed class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string message)
        : base(message)
    {
    }
}
