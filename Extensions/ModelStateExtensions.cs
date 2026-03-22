using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BibleReader.Api.Extensions;

public static class ModelStateExtensions
{
    public static string GetErrors(this ModelStateDictionary modelState)
    {
        return string.Join(" | ", modelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
    }
}
