namespace Watchmen.Common.Types;

public readonly struct Attempt
{
    private readonly Error? givenError;
    private readonly bool hasSuccess;

    private Attempt(bool success)
    {
        givenError = default;
        hasSuccess = success;
    }

    private Attempt(Error error)
    {
        givenError = error;
        hasSuccess = false;
    }

    public static Attempt Success() => new(true);
    public static Attempt Failure(Error error) => new(error);

    public static implicit operator Attempt(Error error) => Failure(error);

    public bool IsSuccess => hasSuccess;
    public bool IsFailure => !hasSuccess;

    public Error HasError => !hasSuccess ? givenError! : throw new InvalidOperationException("Cannot access error of a successful result");

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
        => hasSuccess ? onSuccess() : onFailure(givenError!);

    public void Match(Action onSuccess, Action<Error> onFailure)
    {
        if (hasSuccess)
            onSuccess();
        else
            onFailure(givenError!);
    }

    public void Deconstruct(out bool isSuccess, out Error? error)
    {
        isSuccess = hasSuccess;
        error = hasSuccess ? default : givenError;
    }

    public override string ToString()
        => hasSuccess ? "Success" : $"Failure({givenError})";

    public override bool Equals(object? obj)
        => obj is Attempt other && Equals(other);

    public bool Equals(Attempt other)
    {
        if (hasSuccess != other.hasSuccess) return false;
        return hasSuccess || EqualityComparer<Error>.Default.Equals(givenError, other.givenError);
    }

    public override int GetHashCode()
        => hasSuccess
            ? HashCode.Combine(hasSuccess)
            : HashCode.Combine(hasSuccess, givenError);

    public static bool operator ==(Attempt left, Attempt right)
        => left.Equals(right);

    public static bool operator !=(Attempt left, Attempt right)
        => !(left == right);
}