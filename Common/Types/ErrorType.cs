namespace Watchmen.Common.Types;

public enum ErrorType
{
    NotFound = 1000,

    AlreadyExists = 1001,

    ValidationFailed = 1002,

    Unauthorized = 1003,
    
    Forbidden = 1004,

    Database = 2000,

    Configuration = 3000,
}