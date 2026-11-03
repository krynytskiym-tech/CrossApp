using Core.Domain;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory] // 3) звіт на кількох наборах
    [InlineData(10, 2)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.LowStock(TestData.Items(), threshold).Count);

    [Fact] // 4) детермінований топ-N з тижня 7
    public void TopByQuantity_IsDeterministic() =>
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" },
            ReportService.TopByQuantity(TestData.Items(), 2).Select(l => l.Sku));

    [Fact]
    public void Totals_OnEmptySource_ReturnsZeros()
    {
        var totals = ReportService.Totals([]);

        Assert.Equal((0, 0, 0d, 0), totals);
    }

    [Fact]
    public void ByUnit_IgnoresCase_AndSumsGroups()
    {
        Product[] source =
        [
            Product.Create("G-1", "SKU-G1", "А", "шт", 10),
            Product.Create("G-2", "SKU-G2", "Б", "ШТ", 5)
        ];

        var groups = ReportService.ByUnit(source);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.Count);
        Assert.Equal(15, group.TotalQuantity);
    }
}
