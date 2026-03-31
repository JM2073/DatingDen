using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using RelationshipPlanner.Android.Data;

namespace RelationshipPlanner.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private const string PreferencesName = "relationship_planner_android";
    private const string ApiBaseUrlKey = "api_base_url";
    private const string SelectedUserIdKey = "selected_user_id";
    private const string DefaultApiBaseUrl = "http://10.0.2.2:5283/";

    private EditText? _apiBaseUrlEditText;
    private TextView? _statusText;
    private TextView? _currentUserText;
    private TextView? _plannerSummaryText;
    private TextView? _financeSummaryText;
    private TextView? _errorText;
    private LinearLayout? _usersContainer;
    private PlannerApiClient? _apiClient;
    private HttpClient? _httpClient;
    private string _apiBaseUrl = DefaultApiBaseUrl;
    private IReadOnlyList<PlannerUser> _users = [];
    private int? _selectedUserId;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SetContentView(Resource.Layout.activity_main);
        BindViews();
        LoadPreferences();
        ConfigureApiClient(_apiBaseUrl);
        _ = LoadDashboardAsync();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _httpClient?.Dispose();
    }

    private void BindViews()
    {
        _apiBaseUrlEditText = FindViewById<EditText>(Resource.Id.apiBaseUrlEditText);
        _statusText = FindViewById<TextView>(Resource.Id.statusText);
        _currentUserText = FindViewById<TextView>(Resource.Id.currentUserText);
        _plannerSummaryText = FindViewById<TextView>(Resource.Id.plannerSummaryText);
        _financeSummaryText = FindViewById<TextView>(Resource.Id.financeSummaryText);
        _errorText = FindViewById<TextView>(Resource.Id.errorText);
        _usersContainer = FindViewById<LinearLayout>(Resource.Id.usersContainer);

        var saveButton = FindViewById<Button>(Resource.Id.saveUrlButton);
        var refreshButton = FindViewById<Button>(Resource.Id.refreshButton);

        if (saveButton is not null)
        {
            saveButton.Click += async (_, _) => await SaveUrlAndRefreshAsync();
        }

        if (refreshButton is not null)
        {
            refreshButton.Click += async (_, _) => await LoadDashboardAsync();
        }
    }

    private void LoadPreferences()
    {
        var prefs = GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        _apiBaseUrl = NormalizeApiBaseUrl(prefs.GetString(ApiBaseUrlKey, DefaultApiBaseUrl));
        _selectedUserId = prefs.Contains(SelectedUserIdKey) ? prefs.GetInt(SelectedUserIdKey, 0) : null;

        if (_apiBaseUrlEditText is not null)
        {
            _apiBaseUrlEditText.Text = _apiBaseUrl;
        }
    }

    private void ConfigureApiClient(string baseUrl)
    {
        _httpClient?.Dispose();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };

        _apiClient = new PlannerApiClient(_httpClient);
    }

    private async Task SaveUrlAndRefreshAsync()
    {
        var baseUrl = NormalizeApiBaseUrl(_apiBaseUrlEditText?.Text);
        SavePreference(ApiBaseUrlKey, baseUrl);
        _apiBaseUrl = baseUrl;
        ConfigureApiClient(baseUrl);
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        if (_apiClient is null)
        {
            return;
        }

        SetBusyState("Loading dashboard from backend...");
        ClearError();

        try
        {
            var health = await _apiClient.GetHealthAsync();
            _users = await _apiClient.GetUsersAsync();
            if (_users.Count == 0)
            {
                SetStatus("Connected, but no users exist yet.");
                RenderUsers();
                SetPlannerSummaryText("No users to display.");
                SetFinanceSummaryText("No users to display.");
                return;
            }

            var selectedUserId = ResolveSelectedUserId();
            var selectedUser = _users.FirstOrDefault(user => user.Id == selectedUserId) ?? _users.FirstOrDefault(user => user.IsDefault) ?? _users[0];
            _selectedUserId = selectedUser.Id;
            SavePreference(SelectedUserIdKey, selectedUser.Id);

            var entries = await _apiClient.GetEntriesAsync(selectedUser.Id);
            var finance = await _apiClient.GetFinanceSummaryAsync(selectedUser.Id, DateOnly.FromDateTime(DateTime.Today));

            RenderUsers();
            UpdateCurrentUser(selectedUser);
            UpdatePlannerSummary(entries);
            UpdateFinanceSummary(finance);

            var serviceName = string.IsNullOrWhiteSpace(health?.Service) ? "backend" : health.Service;
        SetStatus($"Connected to {serviceName} at {_apiBaseUrl}");
        }
        catch (Exception ex)
        {
            SetStatus("Unable to reach the backend.");
            SetError(ex.Message);
        }
    }

    private int ResolveSelectedUserId()
    {
        if (_selectedUserId.HasValue && _users.Any(user => user.Id == _selectedUserId.Value))
        {
            return _selectedUserId.Value;
        }

        var defaultUser = _users.FirstOrDefault(user => user.IsDefault) ?? _users.First();
        return defaultUser.Id;
    }

    private void RenderUsers()
    {
        if (_usersContainer is null)
        {
            return;
        }

        _usersContainer.RemoveAllViews();

        foreach (var user in _users)
        {
            var button = new Button(this)
            {
            Text = user.Id == _selectedUserId ? $"{user.Name} ✓" : user.Name,
            };

            button.SetPadding(24, 18, 24, 18);
            button.SetAllCaps(false);
            button.SetTextColor(Resources!.GetColor(Resource.Color.rp_text_primary, Theme));
            button.SetBackgroundColor(global::Android.Graphics.Color.ParseColor(user.AccentColor));
            button.LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
            {
                BottomMargin = 12
            };
            button.Click += async (_, _) => await SelectUserAsync(user.Id);
            _usersContainer.AddView(button);
        }
    }

    private async Task SelectUserAsync(int userId)
    {
        _selectedUserId = userId;
        SavePreference(SelectedUserIdKey, userId);
        await LoadDashboardAsync();
    }

    private void UpdateCurrentUser(PlannerUser user)
    {
        if (_currentUserText is null)
        {
            return;
        }

        var steam = string.IsNullOrWhiteSpace(user.SteamId64) ? "Steam not linked." : "Steam linked.";
        _currentUserText.Text = $"{user.Name} • {steam}";
    }

    private void UpdatePlannerSummary(IReadOnlyList<PlannerEntry> entries)
    {
        if (_plannerSummaryText is null)
        {
            return;
        }

        var dateIdeas = entries.Count(entry => entry.Kind == "date");
        var reminders = entries.Count(entry => entry.Kind == "reminder");
        var memories = entries.Count(entry => entry.Kind == "memory");
        var countdowns = entries.Count(entry => entry.Kind == "countdown");
        var gotTogether = entries.Count(entry => entry.Kind == "got_together");
        var gameEntries = entries.Count(entry => entry.Kind is "game_plan" or "game_status");

        _plannerSummaryText.Text =
            $"Total entries: {entries.Count}\n" +
            $"Date ideas: {dateIdeas}\n" +
            $"Reminders: {reminders}\n" +
            $"Memories: {memories}\n" +
            $"Countdowns: {countdowns}\n" +
            $"Got together: {gotTogether}\n" +
            $"Game entries: {gameEntries}";
    }

    private void SetPlannerSummaryText(string text)
    {
        if (_plannerSummaryText is null)
        {
            return;
        }

        _plannerSummaryText.Text = text;
    }

    private void UpdateFinanceSummary(FinanceSummary summary)
    {
        if (_financeSummaryText is null)
        {
            return;
        }

        _financeSummaryText.Text =
            $"Items: {summary.ItemCount}\n" +
            $"Budgeted income: {summary.BudgetedIncome:C}\n" +
            $"Budgeted outflow: {summary.BudgetedOutflow:C}\n" +
            $"Net budget: {summary.NetBudget:C}\n" +
            $"Actual income: {summary.ActualIncome:C}\n" +
            $"Actual outflow: {summary.ActualOutflow:C}\n" +
            $"Net actual: {summary.NetActual:C}";
    }

    private void SetFinanceSummaryText(string text)
    {
        if (_financeSummaryText is null)
        {
            return;
        }

        _financeSummaryText.Text = text;
    }

    private void SetBusyState(string message)
    {
        RunOnUiThread(() =>
        {
            if (_statusText is not null)
            {
                _statusText.Text = message;
            }

            if (_errorText is not null)
            {
                _errorText.Text = string.Empty;
            }
        });
    }

    private void SetStatus(string message)
    {
        RunOnUiThread(() =>
        {
            if (_statusText is not null)
            {
                _statusText.Text = message;
            }
        });
    }

    private void SetError(string message)
    {
        RunOnUiThread(() =>
        {
            if (_errorText is not null)
            {
                _errorText.Text = message;
            }
        });
    }

    private void ClearError()
    {
        RunOnUiThread(() =>
        {
            if (_errorText is not null)
            {
                _errorText.Text = string.Empty;
            }
        });
    }

    private void SavePreference(string key, string value)
    {
        var prefs = GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        prefs.Edit().PutString(key, value).Apply();
    }

    private void SavePreference(string key, int value)
    {
        var prefs = GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        prefs.Edit().PutInt(key, value).Apply();
    }

    private string NormalizeApiBaseUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultApiBaseUrl;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return DefaultApiBaseUrl;
        }

        var builder = new UriBuilder(uri);
        if (!builder.Path.EndsWith('/'))
        {
            builder.Path = $"{builder.Path.TrimEnd('/')}/";
        }

        return builder.Uri.ToString();
    }
}
