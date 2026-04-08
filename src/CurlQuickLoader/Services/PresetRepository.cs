using System.Text.Json;
using CurlQuickLoader.Models;

namespace CurlQuickLoader.Services;

public class PresetRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _presetsDir;
    private readonly string _presetsFile;

    public PresetRepository()
    {
        // Use Environment.ProcessPath so single-file exe deployments store
        // presets next to the actual .exe, not in the temp extraction folder.
        string exeDir = Logger.GetBaseDir();
        _presetsDir = Path.Combine(exeDir, "presets");
        _presetsFile = Path.Combine(_presetsDir, "presets.json");
        Logger.Info($"Presets directory: {_presetsDir}");
    }

    public List<CurlPreset> Load()
    {
        Logger.Info($"Loading presets from: {_presetsFile}");
        if (!File.Exists(_presetsFile))
        {
            Logger.Info("Presets file not found, creating empty store");
            Directory.CreateDirectory(_presetsDir);
            File.WriteAllText(_presetsFile, "[]");
            return new List<CurlPreset>();
        }

        try
        {
            string json = File.ReadAllText(_presetsFile);
            var list = JsonSerializer.Deserialize<List<CurlPreset>>(json, JsonOptions) ?? new List<CurlPreset>();
            Logger.Info($"Loaded {list.Count} preset(s)");
            return list;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to deserialize presets", ex);
            return new List<CurlPreset>();
        }
    }

    public void Save(List<CurlPreset> presets)
    {
        Logger.Info($"Saving {presets.Count} preset(s)");
        Directory.CreateDirectory(_presetsDir);
        string tmpFile = _presetsFile + ".tmp";
        string json = JsonSerializer.Serialize(presets, JsonOptions);
        File.WriteAllText(tmpFile, json);

        // Atomic replace
        if (File.Exists(_presetsFile))
            File.Replace(tmpFile, _presetsFile, null);
        else
            File.Move(tmpFile, _presetsFile);
    }

    public void Add(CurlPreset preset)
    {
        var presets = Load();
        presets.Add(preset);
        Save(presets);
    }

    public void Update(CurlPreset preset)
    {
        var presets = Load();
        int idx = presets.FindIndex(p => p.Id == preset.Id);
        if (idx >= 0)
        {
            preset.UpdatedAt = DateTime.Now;
            presets[idx] = preset;
            Save(presets);
        }
    }

    public void Delete(Guid id)
    {
        var presets = Load();
        presets.RemoveAll(p => p.Id == id);
        Save(presets);
    }

    public CurlPreset? FindByName(string name)
    {
        return Load().FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool NameExists(string name, Guid? excludeId = null)
    {
        return Load().Any(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
            p.Id != excludeId);
    }

    public string GetPresetsFilePath() => _presetsFile;
}
