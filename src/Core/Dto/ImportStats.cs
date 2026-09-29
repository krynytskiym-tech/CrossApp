using System.Globalization;

namespace Core.Dto;

public sealed record ImportStats(int Accepted, int Skipped)
{
    public int Total => Accepted + Skipped;

    public double ErrorPercent => Total == 0 ? 0 : 100.0 * Skipped / Total;

    public static ImportStats From<T>(ImportResult<T> result) =>
        new(result.Items.Count, result.Errors.Count);

    public string Summary()
    {
        string percent = ErrorPercent.ToString("F1", CultureInfo.InvariantCulture);
        return $"усього {Total}, прийнято {Accepted}, пропущено {Skipped} ({percent}% помилок)";
    }
}
