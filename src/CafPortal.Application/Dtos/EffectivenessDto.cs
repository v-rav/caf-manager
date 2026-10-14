namespace CafPortal.Application.Dtos;

public class EffectivenessDto
{
    public string Role { get; set; } = "PM";
    public double GreenFrom { get; set; }
    public double AmberFrom { get; set; }
    public int Owners { get; set; }
    public int Assigned { get; set; }
    public int Delivered { get; set; }
    public int Active { get; set; }
    public int DeliveredPct { get; set; }
    public int? AvgDeliveryDays { get; set; }
    public int? AvgActiveAge { get; set; }
    public List<EffectivenessRowDto> Rows { get; set; } = new();
}

public class EffectivenessRowDto
{
    public string Name { get; set; } = string.Empty;
    public int Assigned { get; set; }
    public int Active { get; set; }
    public int Delivered { get; set; }
    public int Withdrawn { get; set; }
    public int Deferred { get; set; }
    public int Blocked { get; set; }
    public int? SuccessRate { get; set; }
    public int DeliveredPct { get; set; }
    public int? AvgDeliveryDays { get; set; }
    public int? AvgActiveAge { get; set; }
    public int AvgMsi { get; set; }
    public int? OnTrackPct { get; set; }
    public int SlaBreaches { get; set; }
    public double? AvgGhcp { get; set; }
    public double AcrDelivered { get; set; }
    public int Effectiveness { get; set; }
    /// <summary>Band tone from EffGreenFrom / EffAmberFrom: success · warning · danger.</summary>
    public string Tone { get; set; } = "danger";
    /// <summary>Human-readable component breakdown of the composite.</summary>
    public string Explain { get; set; } = string.Empty;
}
