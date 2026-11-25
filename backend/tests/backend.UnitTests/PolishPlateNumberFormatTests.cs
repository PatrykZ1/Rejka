using backend.Infrastructure.Analysis;

namespace backend.UnitTests;

public sealed class PolishPlateNumberFormatTests
{
    [Theory]
    [InlineData("BI903KM")]
    [InlineData("KRA404AM")]
    [InlineData("NNI32590")]
    [InlineData("WY330XG")]
    [InlineData("WE7JV01")]
    public void IsValid_AcceptsStandardPolishPlateFormats(string registration)
    {
        Assert.True(PolishPlateNumberFormat.IsValid(registration));
    }

    [Theory]
    [InlineData("P0KAZIK")]
    [InlineData("W1XYZ")]
    [InlineData("W1ABC12")]
    [InlineData("W0AS2")]
    public void IsValid_AcceptsIndividualPolishPlateFormats(string registration)
    {
        Assert.True(PolishPlateNumberFormat.IsValid(registration));
    }

    [Theory]
    [InlineData("5526GF")]
    [InlineData("686E")]
    [InlineData("7526CC7")]
    [InlineData("I923PU")]
    [InlineData("WE7JVO1")]
    [InlineData("W1AB")]
    [InlineData("W1ABCDEF")]
    [InlineData("W1AB123")]
    [InlineData("W1A2BC")]
    [InlineData("H1ABC")]
    public void IsValid_RejectsInvalidPolishPlateFormats(string registration)
    {
        Assert.False(PolishPlateNumberFormat.IsValid(registration));
    }
}
