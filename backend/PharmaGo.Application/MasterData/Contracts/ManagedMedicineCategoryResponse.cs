namespace PharmaGo.Application.MasterData.Contracts;

public class ManagedMedicineCategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MedicinesCount { get; init; }
}
