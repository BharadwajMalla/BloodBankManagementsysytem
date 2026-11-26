using BloodBank.API.Models;
using BloodBank.API.Services;
using Xunit;

namespace BloodBank.Application.Tests;

public class PatientGroupingCompatibilityTests
{
    [Fact]
    public void IsCompatible_AcceptsMatchingBloodGroupAndCompatibleCrossmatches()
    {
        var record = CompatibleGrouping();

        Assert.True(PatientGroupingCompatibility.IsCompatible(record, "A+"));
    }

    [Theory]
    [InlineData("Incompatible", "Compatible", "Compatible")]
    [InlineData("Compatible", "Not tested", "Compatible")]
    [InlineData("Compatible", "Compatible", "Incompatible")]
    public void IsCompatible_RejectsAnyFailedOrUnfinishedCrossmatch(
        string saline, string albumin, string ahg)
    {
        var record = CompatibleGrouping();
        record.CrossmatchSaline = saline;
        record.CrossmatchAlbumin = albumin;
        record.CrossmatchAhg = ahg;

        Assert.False(PatientGroupingCompatibility.IsCompatible(record, "A+"));
    }

    [Fact]
    public void IsCompatible_RejectsBloodGroupOrRhMismatch()
    {
        var record = CompatibleGrouping();
        record.BloodGroup = "A-";

        Assert.False(PatientGroupingCompatibility.IsCompatible(record, "A+"));

        record.BloodGroup = "A+";
        record.RhType = "Negative";
        Assert.False(PatientGroupingCompatibility.IsCompatible(record, "A+"));
    }

    [Fact]
    public void IsCompatible_RejectsGroupingThatHasNotBeenFullyTested()
    {
        var record = CompatibleGrouping();
        record.ACells = "Not tested";

        Assert.False(PatientGroupingCompatibility.IsCompatible(record, "A+"));
    }

    private static PatientGroupingRecord CompatibleGrouping()
    {
        return new PatientGroupingRecord
        {
            BloodGroup = "A+",
            RhType = "Positive",
            CrossmatchSaline = "Compatible",
            CrossmatchAlbumin = "Compatible",
            CrossmatchAhg = "Compatible"
        };
    }
}
