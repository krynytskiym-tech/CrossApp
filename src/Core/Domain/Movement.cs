namespace Core.Domain;

// Запис в історії руху товару: незмінний, створюється лише всередині Product.
public sealed record Movement(MovementKind Kind, int Amount);
