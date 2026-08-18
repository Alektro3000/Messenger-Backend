namespace DTO.Auth;

public enum RegisterResult
{
    Succesful,
    RepeatedUser,
    Unknown
}


public enum ChangeProfileResult
{
    Succesful,
    RepeatedUsername,
    UserNotFound,
    Unknown
}