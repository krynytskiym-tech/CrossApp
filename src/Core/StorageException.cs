namespace Core;

// Технічна помилка інфраструктури (наприклад, пошкоджений JSON). Початковий виняток зберігається в InnerException.
public sealed class StorageException(string message, Exception inner) : Exception(message, inner);
