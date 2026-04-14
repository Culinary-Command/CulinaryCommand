// Services/LocationState.cs
using CulinaryCommand.Data.Entities;
using Microsoft.JSInterop;

namespace CulinaryCommand.Services
{
    public class LocationState
    {
        public const string ActiveLocStorageKey = "cc_activeLocationId";
        private readonly IJSRuntime _js;
        public List<Location> ManagedLocations { get; private set; } = new();
        public Location? CurrentLocation { get; private set; }

        public event Action? OnChange;

        public LocationState(IJSRuntime js)
        {
            _js = js;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        public async Task HydrateAsync()
        {
            if (CurrentLocation != null) return; // Already have it

            var savedId = await _js.InvokeAsync<string>("localStorage.getItem", ActiveLocStorageKey);
            
            if (!string.IsNullOrEmpty(savedId) && int.TryParse(savedId, out int id))
            {
                // Assuming you've loaded your ManagedLocations list already
                CurrentLocation = ManagedLocations.FirstOrDefault(l => l.Id == id);
                NotifyStateChanged();
            }
        }
        public async Task SetLocationsAsync(List<Location>? locations)
        {
            ManagedLocations = locations ?? new List<Location>();

            // Try to restore saved current location (but JS may not be ready during prerender)
            string? savedId = null;
            try
            {
                savedId = await _js.InvokeAsync<string?>("localStorage.getItem", ActiveLocStorageKey);
            }
            catch (InvalidOperationException)
            {
                // JS not available yet (prerender). We'll just pick a default for now.
            }

            if (int.TryParse(savedId, out var id))
            {
                CurrentLocation = ManagedLocations.FirstOrDefault(l => l.Id == id);
            }

            // Default to first if invalid or not found
            CurrentLocation ??= ManagedLocations.FirstOrDefault();

            OnChange?.Invoke();
        }

        public async Task SetCurrentLocationAsync(Location? loc)
        {
            CurrentLocation = loc;

            try
            {
                if (loc is null)
                    await _js.InvokeVoidAsync("localStorage.removeItem", ActiveLocStorageKey);
                else
                    await _js.InvokeVoidAsync("localStorage.setItem", ActiveLocStorageKey, loc.Id.ToString());
            }
            catch (InvalidOperationException)
            {
                // JS not available yet (prerender). It's fine; UI state still updates.
            }

            // OnChange?.Invoke();
            NotifyStateChanged();
        }

        public void FlushLocationState()
        {
            ManagedLocations = new List<Location>();
            CurrentLocation = null;
        }
    }
}
