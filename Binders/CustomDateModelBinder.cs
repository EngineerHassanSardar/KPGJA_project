using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace KPGJA.Binders
{
    public class CustomDateModelBinder : IModelBinder
    {
        private readonly string[] formats = new[] { "dd-MM-yyyy", "MM-dd-yyyy", "yyyy-MM-dd" };

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            string value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                bindingContext.Result = ModelBindingResult.Success(date);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Invalid date format: {value}. Expected formats: dd-MM-yyyy, MM-dd-yyyy, yyyy-MM-dd");
            return Task.CompletedTask;
        }
    }
}
