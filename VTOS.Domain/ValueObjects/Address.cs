namespace VTOS.Domain.ValueObjects;

public class Address
{
    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = "Vietnam";

    private Address() { } // For EF Core

    public Address(string street, string city, string province, string postalCode, string country = "Vietnam")
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Province = province ?? throw new ArgumentNullException(nameof(province));
        PostalCode = postalCode ?? throw new ArgumentNullException(nameof(postalCode));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }

    public override string ToString()
    {
        return $"{Street}, {City}, {Province}, {PostalCode}, {Country}";
    }
}

