using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Prometej_core.Models.Requests
{
    // Model validation checks each item of a list but skips a null one, which would
    // otherwise reach the service.
    public class NoNullItemsAttribute : ValidationAttribute
    {
        public NoNullItemsAttribute() : base("The field {0} must not contain null.")
        {
        }

        public override bool IsValid(object? value)
        {
            return value is not IEnumerable items || items.Cast<object?>().All(item => item != null);
        }
    }
}
