namespace Watchmen.Common.Types;

public readonly struct Result<T>
{
    private readonly T? givenValue;
    private readonly Error? givenError;
    private readonly bool hasSuccess;

    private Result(T value)
    {
        givenValue = value;
        givenError = default;
        hasSuccess = true;
    }

    private Result(Error error)
    {
        givenValue = default;
        givenError = error;
        hasSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public bool IsSuccess => hasSuccess;
    public bool IsFailure => !hasSuccess;

    public T Value => hasSuccess ? givenValue! : throw new InvalidOperationException("Cannot access value of a failed result");
    public Error HasError => !hasSuccess ? givenError! : throw new InvalidOperationException("Cannot access error of a successful result");

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => hasSuccess ? onSuccess(givenValue!) : onFailure(givenError!);

    public void Deconstruct(out bool isSuccess, out T? value, out Error? error)
    {
        isSuccess = hasSuccess;
        value = hasSuccess ? givenValue : default;
        error = hasSuccess ? default : givenError;
    }

    public override string ToString()
        => hasSuccess ? $"Success({givenValue})" : $"Failure({givenError})";

    public override bool Equals(object? obj)
        => obj is Result<T> other && Equals(other);

    public bool Equals(Result<T> other)
    {
        if (hasSuccess != other.hasSuccess) return false;
        return hasSuccess
            ? EqualityComparer<T>.Default.Equals(givenValue, other.givenValue)
            : EqualityComparer<Error>.Default.Equals(givenError, other.givenError);
    }

    public override int GetHashCode()
        => hasSuccess
            ? HashCode.Combine(hasSuccess, givenValue)
            : HashCode.Combine(hasSuccess, givenError);

    public static bool operator ==(Result<T> left, Result<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Result<T> left, Result<T> right)
    {
        return !(left == right);
    }
}