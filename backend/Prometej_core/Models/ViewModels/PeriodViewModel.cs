namespace Prometej_core.Models.ViewModels
{
    public class PeriodViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string TimeFrame { get; set; }
        public required string Description { get; set; }
        public string? Image { get; set; }
    }
}
