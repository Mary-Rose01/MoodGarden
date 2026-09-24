using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MoodGarden.Services; // adjust to your project's root namespace

public record Mood(string Emoji, string Name, string Bg);

public record Planting(Guid Id, string Emoji, string MoodName, double X, double Y, DateTime At, string? Note);

/// <summary>Single source of truth shared by the Garden, Diary and Stats pages. Persists to localStorage.</summary>
public class GardenState
{
    private const string StorageKey = "cat-mood-garden:plantings";
    private readonly IJSRuntime _js;
    private bool _loaded;

    public GardenState(IJSRuntime js) => _js = js;

    public static readonly IReadOnlyList<Mood> Moods = new List<Mood>
    {
        new("😸", "Happy",     "bg-brand-yellow"),
        new("😿", "Sad",       "bg-brand-blue"),
        new("😾", "Angry",     "bg-brand-coral"),
        new("😴", "Sleepy",    "bg-brand-lilac"),
        new("🤩", "Starry",    "bg-brand-peach"),
        new("😳", "Flustered", "bg-brand-pink"),
        new("😍", "In Love",   "bg-brand-rose"),
        new("🙄", "Sassy",     "bg-brand-green"),
    };

    public static Mood Find(string name) => Moods.FirstOrDefault(m => m.Name == name) ?? Moods[0];

    public static string Css(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    public static string TimeAgo(DateTime utc)
    {
        var d = DateTime.UtcNow - utc;
        if (d.TotalMinutes < 1) return "Just now";
        if (d.TotalHours < 1) return $"{(int)d.TotalMinutes} min ago";
        if (d.TotalDays < 1) return $"{(int)d.TotalHours} hr ago";
        if (d.TotalDays < 2) return "Yesterday, " + utc.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture);
        if (d.TotalDays < 7) return $"{(int)d.TotalDays} days ago";
        return utc.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
    }

    public List<Planting> Plantings { get; private set; } = new();

    /// <summary>Plantings from the last 24 hours (what the garden and stats show).</summary>
    public IReadOnlyList<Planting> Recent =>
        Plantings.Where(p => p.At > DateTime.UtcNow.AddHours(-24)).ToList();

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var json = await _js.InvokeAsync<string?>("garden.load", StorageKey);
            if (!string.IsNullOrEmpty(json))
                Plantings = JsonSerializer.Deserialize<List<Planting>>(json) ?? new();
        }
        catch { Plantings = new(); }
        Changed?.Invoke();
    }

    public async Task AddAsync(Planting planting)
    {
        Plantings.Add(planting);
        await _js.InvokeVoidAsync("garden.save", StorageKey, JsonSerializer.Serialize(Plantings));
        Changed?.Invoke();
    }
}

/// <summary>Inherit from this in pages: loads saved data and re-renders when the garden changes.</summary>
public abstract class GardenPageBase : ComponentBase, IDisposable
{
    [Inject] protected GardenState Garden { get; set; } = default!;

    protected override void OnInitialized() => Garden.Changed += OnGardenChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Garden.EnsureLoadedAsync();
    }

    private void OnGardenChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Garden.Changed -= OnGardenChanged;
}
