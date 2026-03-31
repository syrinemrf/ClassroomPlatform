namespace ITBS_Classroom.Models.ViewModels;

public class CalendarEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string Color { get; set; } = "#1a73e8";
    public string? Url { get; set; }
    public string CourseName { get; set; } = string.Empty;
}
