namespace CurlQuickLoader.Models;

public class CurlPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public List<Header> Headers { get; set; } = new();
    public string? Body { get; set; }
    public string? ExtraFlags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public CurlPreset Clone()
    {
        return new CurlPreset
        {
            Id = Guid.NewGuid(),
            Name = Name,
            Url = Url,
            Method = Method,
            Headers = Headers.Select(h => new Header(h.Key, h.Value)).ToList(),
            Body = Body,
            ExtraFlags = ExtraFlags,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}
