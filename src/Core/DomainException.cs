namespace Core;

// Порушення бізнес-правила: видача більша за залишок, запис не знайдено, дубль id тощо.
// Некоректні аргументи лишаються ArgumentException / ArgumentOutOfRangeException.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
