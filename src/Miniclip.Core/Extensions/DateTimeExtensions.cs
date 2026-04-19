namespace Miniclip.Core.Extensions;

public static class DateTimeExtensions
{
    private const string RoundTrip = "o";

    public static string ToRoundTripString(this DateTime value)
        => value.ToString(RoundTrip);

    public static string ToRoundTripString(this DateTimeOffset value)
        => value.ToString(RoundTrip);
}
