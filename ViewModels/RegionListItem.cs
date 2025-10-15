namespace MicrDbChequeProcessingSystem.ViewModels;

public class RegionListItem
{
    public string Name { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Created { get; set; } = string.Empty;

    public int? Banks { get; set; }

    public int? Branches { get; set; }
}
