namespace backend.Domain.Locations;

public sealed class Location
{
    public int Id { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }

    public Location(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public Location(int id, decimal latitude, decimal longitude)
    {
        Id = id;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void SetId(int id)
    {
        Id = id;
    }
}
