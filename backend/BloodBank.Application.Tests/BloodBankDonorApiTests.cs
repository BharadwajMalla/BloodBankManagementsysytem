using System.Reflection;
using BloodBank.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BloodBank.Application.Tests;

public sealed class BloodBankDonorApiTests
{
    [Fact]
    public void DonorAndDonationRoutesRemainUnchanged()
    {
        var controller = typeof(BloodBankController);
        Assert.Equal("api/[controller]", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal("donors", Action(nameof(BloodBankController.GetDonors)).GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Equal("donors", Action(nameof(BloodBankController.CreateDonor)).GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal(
            "donors/{id:guid}/donation",
            Action(nameof(BloodBankController.CompleteDonation)).GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Equal(
            "donors/{id:guid}/donations",
            Action(nameof(BloodBankController.GetDonationHistory)).GetCustomAttribute<HttpGetAttribute>()?.Template);
    }

    private static MethodInfo Action(string name) =>
        typeof(BloodBankController).GetMethod(name)
        ?? throw new InvalidOperationException($"Action {name} was not found.");
}
