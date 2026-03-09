namespace VTOS.Domain.Enums;

public enum CampaignStatus
{
    Draft = 1,
    Active = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5,
    Locked = 6     // UC 3.9.5b: no more parent orders accepted
}

