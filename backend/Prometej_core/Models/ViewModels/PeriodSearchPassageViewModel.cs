namespace Prometej_core.Models.ViewModels
{
    // One matching piece of a Period Content, as plain text.
    public class PeriodSearchPassageViewModel
    {
        // The nearest heading above the passage; none when the passage is itself a heading.
        public string? Heading { get; set; }
        public required string Text { get; set; }
    }
}
