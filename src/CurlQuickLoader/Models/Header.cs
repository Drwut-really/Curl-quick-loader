namespace CurlQuickLoader.Models;

public class Header
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public Header() { }

    public Header(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
