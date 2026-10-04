namespace Prometej_core.Models.efModels
{
    public class Period
    {
        // Fixed by the list in CurriculumPeriods, not generated. An id is not a place in the
        // order: SortOrder is.
        public int Id { get; set; }
        public required string Name { get; set; }
        public int SortOrder { get; set; }
        public required string TimeFrame { get; set; }
        // The text on the Period's card.
        public required string Description { get; set; }
        // A file in the client's public folder; a Period without one gets a plain card.
        public string? Image { get; set; }
    }
}
