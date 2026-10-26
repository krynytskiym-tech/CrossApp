namespace Core.Reports;

public static class EnumerableExtensions
{
    // Додаткове завдання 2: власний метод-розширення. Топ-N за числовим ключем
    // з другим (унікальним) ключем, щоб порядок не залежав від джерела.
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source, Func<T, int> key, Func<T, string> tieBreaker, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n), n, "Кількість елементів не може бути від'ємною");

        return source.OrderByDescending(key)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}
