namespace Domain.Interfaces;

public interface IAuditable
{
    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }
}
