using Microsoft.JSInterop;

namespace RelationshipPlanner.Blazor.Data;

public sealed class UserSessionService
{
    private const string CurrentUserKey = "planner.currentUserId";
    private readonly IJSRuntime _jsRuntime;

    public UserSessionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<int?> GetCurrentUserIdAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<int?>("plannerSessionStorage.getInt", CurrentUserKey);
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask SetCurrentUserIdAsync(int? userId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("plannerSessionStorage.setInt", CurrentUserKey, userId);
        }
        catch
        {
            // Ignore browser storage errors and keep the app usable.
        }
    }

    public async ValueTask ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("plannerSessionStorage.clear", CurrentUserKey);
        }
        catch
        {
        }
    }
}

