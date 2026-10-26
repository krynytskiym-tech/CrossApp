using Core.Events;

namespace Core.Reports;

// Додаткове завдання 1 (Join / GroupJoin): друга колекція — журнал змін, зібраний із події Changed.
public sealed record JournalEntry(string ProductId, CatalogChangeKind Kind, int Delta);

public sealed record MovementLine(string Sku, string Name, CatalogChangeKind Kind, int Delta);

public sealed record ActivityLine(string Sku, string Name, int Movements, int NetDelta);
