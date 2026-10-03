using System.ComponentModel.DataAnnotations;

namespace PikminDetector.Api.Lib.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredWhenAttribute(string propertyName) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var other = context.ObjectType.GetProperty(propertyName)?.GetValue(context.ObjectInstance);
        return other is not null && string.IsNullOrWhiteSpace(value as string)
            ? new ValidationResult(ErrorMessage ?? $"{context.MemberName} is required when {propertyName} is provided.",
                [context.MemberName!])
            : ValidationResult.Success;
    }
}
