using System.ComponentModel.DataAnnotations;
using BloodBank.API.DTOs;
using Xunit;

namespace BloodBank.Application.Tests;

public class CreateBloodRequestDtoTests
{
    [Theory]
    [InlineData(0, "Months")]
    [InlineData(18, "Months")]
    [InlineData(120, "Years")]
    public void Validate_AcceptsPatientAgesWithinUnitLimits(int age, string ageUnit)
    {
        var dto = ValidRequest();
        dto.PatientAge = age;
        dto.PatientAgeUnit = ageUnit;

        Assert.Empty(ValidationErrors(dto));
    }

    [Theory]
    [InlineData(121, "Years")]
    [InlineData(1441, "Months")]
    public void Validate_RejectsPatientAgeBeyondUnitLimit(int age, string ageUnit)
    {
        var dto = ValidRequest();
        dto.PatientAge = age;
        dto.PatientAgeUnit = ageUnit;

        Assert.Contains(ValidationErrors(dto), error => error.MemberNames.Contains(nameof(dto.PatientAge)));
    }

    private static CreateBloodRequestDto ValidRequest()
    {
        return new CreateBloodRequestDto
        {
            PatientName = "Patient",
            PatientAge = 1,
            PatientAgeUnit = "Years",
            PatientSex = "Unknown",
            BloodGroup = "O+",
            ComponentType = "Whole Blood",
            QuantityUnits = 1,
            Urgency = "Normal"
        };
    }

    private static List<ValidationResult> ValidationErrors(CreateBloodRequestDto dto)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true);
        return errors;
    }
}
