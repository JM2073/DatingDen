using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace RelationshipPlanner.Android;

[Activity(Label = "Relationship Planner", MainLauncher = true)]
public class MainActivity : Activity
{
    private readonly global::Android.Graphics.Color _backgroundColor = new(Color.ParseColor("#0e1422"));
    private readonly global::Android.Graphics.Color _surfaceColor = new(Color.ParseColor("#182033"));
    private readonly global::Android.Graphics.Color _surfaceAltColor = new(Color.ParseColor("#20293a"));
    private readonly global::Android.Graphics.Color _borderColor = new(Color.ParseColor("#2d3852"));
    private readonly global::Android.Graphics.Color _textPrimaryColor = new(Color.ParseColor("#f5f7fb"));
    private readonly global::Android.Graphics.Color _textSecondaryColor = new(Color.ParseColor("#aab4c5"));
    private readonly global::Android.Graphics.Color _accentColor = new(Color.ParseColor("#f6a57a"));
    private EditText? _apiBaseUrlEditText;
    private TextView? _connectionStatusText;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SetContentView(BuildContentView());
    }

    private View BuildContentView()
    {
        var scrollView = new ScrollView(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        scrollView.SetBackgroundColor(_backgroundColor);

        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        root.SetPadding(Dp(20), Dp(22), Dp(20), Dp(28));
        root.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        root.AddView(CreateTitleBlock());
        root.AddView(CreateConnectionCard());
        root.AddView(CreateCurrentUserCard());
        root.AddView(CreatePlannerSnapshotCard());
        root.AddView(CreateFinanceSnapshotCard());
        root.AddView(CreateRecentEntriesCard());

        scrollView.AddView(root);
        return scrollView;
    }

    private View CreateTitleBlock()
    {
        var container = CreateVerticalSectionContainer(topMarginDp: 0);

        container.AddView(CreateTextView("Relationship Planner", 30, _textPrimaryColor, TypefaceStyle.Bold));

        var subtitle = CreateTextView(
            "Native Android shell first. We will add one feature at a time and confirm each step before wiring it to the API.",
            14,
            _textSecondaryColor);
        subtitle.SetPadding(0, Dp(6), 0, 0);
        container.AddView(subtitle);

        var status = CreateTextView("Step 1: the UI shell is visible.", 13, _accentColor, TypefaceStyle.Bold);
        status.SetPadding(0, Dp(12), 0, 0);
        container.AddView(status);

        return container;
    }

    private LinearLayout CreateConnectionCard()
    {
        var card = CreateCard("Backend connection", "UI only for now. We will wire this to the API later.");

        var apiLabel = CreateSmallLabel("API base URL");
        apiLabel.SetPadding(0, Dp(12), 0, 0);
        card.AddView(apiLabel);

        var apiBaseUrl = CreateField("http://192.168.1.50:5283/");
        _apiBaseUrlEditText = apiBaseUrl;
        card.AddView(apiBaseUrl);

        var buttonRow = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };
        buttonRow.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(12)
        };

        var saveButton = CreatePillButton("Save URL", _accentColor, Color.Black);
        var refreshButton = CreatePillButton("Refresh", _surfaceAltColor, _textPrimaryColor);
        saveButton.Click += HandleSaveUrlClicked;
        refreshButton.Click += HandleRefreshClicked;

        buttonRow.AddView(saveButton, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));
        buttonRow.AddView(Spacer(12));
        buttonRow.AddView(refreshButton, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));

        _connectionStatusText = CreateTextView("Waiting for API hookup.", 13, _textSecondaryColor);
        var status = _connectionStatusText;
        status.SetPadding(0, Dp(12), 0, 0);

        card.AddView(buttonRow);
        card.AddView(status);
        return card;
    }

    private void HandleSaveUrlClicked(object? sender, EventArgs e)
    {
        var value = _apiBaseUrlEditText?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            SetConnectionStatus("Type a backend URL before saving.");
            return;
        }

        SetConnectionStatus($"Saved URL shell value: {value}");
    }

    private void HandleRefreshClicked(object? sender, EventArgs e)
    {
        var currentValue = _apiBaseUrlEditText?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(currentValue))
        {
            SetConnectionStatus("Refresh tapped, but no backend URL has been entered yet.");
            return;
        }

        SetConnectionStatus($"Refresh tapped for {currentValue}");
    }

    private LinearLayout CreateCurrentUserCard()
    {
        var card = CreateCard("Current user", "Device-local selection will come later.");
        card.AddView(CreatePillLabel("No user selected yet.", _surfaceAltColor, _textSecondaryColor));
        card.AddView(CreatePillLabel("Steam not linked.", _surfaceAltColor, _textSecondaryColor, topMarginDp: 10));
        return card;
    }

    private LinearLayout CreatePlannerSnapshotCard()
    {
        var card = CreateCard("Planner snapshot", "Static layout only. Data comes later.");
        card.AddView(CreateSnapshotLine("Total entries", "—"));
        card.AddView(CreateSnapshotLine("Date ideas", "—"));
        card.AddView(CreateSnapshotLine("Reminders", "—"));
        card.AddView(CreateSnapshotLine("Memories", "—"));
        card.AddView(CreateSnapshotLine("Countdowns", "—"));
        card.AddView(CreateSnapshotLine("Got together", "—"));
        return card;
    }

    private LinearLayout CreateFinanceSnapshotCard()
    {
        var card = CreateCard("Finance snapshot", "This is the visual shell for the finance tab.");
        card.AddView(CreateSnapshotLine("Items", "—"));
        card.AddView(CreateSnapshotLine("Budgeted income", "—"));
        card.AddView(CreateSnapshotLine("Budgeted outflow", "—"));
        card.AddView(CreateSnapshotLine("Net budget", "—"));
        card.AddView(CreateSnapshotLine("Actual income", "—"));
        card.AddView(CreateSnapshotLine("Actual outflow", "—"));
        card.AddView(CreateSnapshotLine("Net actual", "—"));
        return card;
    }

    private LinearLayout CreateRecentEntriesCard()
    {
        var card = CreateCard("Recent entries", "A placeholder list for the native app.");

        card.AddView(CreatePlaceholderEntry("Nothing loaded yet.", "Once the API is wired, recent items will appear here."));
        card.AddView(CreatePlaceholderEntry("Planner items", "Date ideas, reminders, memories, and more."));
        card.AddView(CreatePlaceholderEntry("Game plans", "Quick add and status cards will land here later."));

        return card;
    }

    private LinearLayout CreateCard(string title, string subtitle)
    {
        var card = CreateVerticalSectionContainer(topMarginDp: 18);
        card.Background = CreateCardBackground();

        var titleView = CreateTextView(title, 18, _textPrimaryColor, TypefaceStyle.Bold);
        card.AddView(titleView);

        var subtitleView = CreateTextView(subtitle, 13, _textSecondaryColor);
        subtitleView.SetPadding(0, Dp(6), 0, 0);
        card.AddView(subtitleView);

        return card;
    }

    private LinearLayout CreateVerticalSectionContainer(int topMarginDp)
    {
        var container = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        container.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(topMarginDp)
        };
        return container;
    }

    private TextView CreateTextView(string text, float sizeSp, global::Android.Graphics.Color color, TypefaceStyle style = TypefaceStyle.Normal)
    {
        var textView = new TextView(this)
        {
            Text = text
        };
        textView.SetTextColor(color);
        textView.SetTextSize(ComplexUnitType.Sp, sizeSp);
        textView.SetTypeface(null, style);
        textView.SetLineSpacing(0f, 1.1f);
        return textView;
    }

    private TextView CreateSmallLabel(string text)
    {
        return CreateTextView(text, 12, _textSecondaryColor, TypefaceStyle.Bold);
    }

    private EditText CreateField(string hint)
    {
        var editText = new EditText(this)
        {
            Hint = hint
        };
        editText.SetTextColor(_textPrimaryColor);
        editText.SetHintTextColor(_textSecondaryColor);
        editText.SetBackgroundColor(_surfaceAltColor);
        editText.SetPadding(Dp(12), Dp(12), Dp(12), Dp(12));
        editText.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
        };
        return editText;
    }

    private Button CreatePillButton(string text, global::Android.Graphics.Color backgroundColor, global::Android.Graphics.Color textColor)
    {
        var button = new Button(this)
        {
            Text = text
        };
        button.SetAllCaps(false);
        button.SetTextColor(textColor);
        button.SetBackgroundColor(backgroundColor);
        button.SetPadding(Dp(12), Dp(12), Dp(12), Dp(12));
        button.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        return button;
    }

    private TextView CreatePillLabel(string text, global::Android.Graphics.Color backgroundColor, global::Android.Graphics.Color textColor, int topMarginDp = 0)
    {
        var label = CreateTextView(text, 13, textColor);
        label.SetPadding(Dp(12), Dp(10), Dp(12), Dp(10));
        label.Background = CreateRoundedBackground(backgroundColor);
        label.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(topMarginDp)
        };
        return label;
    }

    private LinearLayout CreateSnapshotLine(string label, string value)
    {
        var row = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };
        row.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(10)
        };

        var left = CreateTextView(label, 13, _textSecondaryColor);
        left.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);

        var right = CreateTextView(value, 13, _textPrimaryColor, TypefaceStyle.Bold);
        right.Gravity = GravityFlags.End;

        row.AddView(left);
        row.AddView(right);
        return row;
    }

    private LinearLayout CreatePlaceholderEntry(string title, string subtitle)
    {
        var card = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        card.SetPadding(Dp(14), Dp(12), Dp(14), Dp(12));
        card.Background = CreateRoundedBackground(_surfaceAltColor);
        card.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(10)
        };

        card.AddView(CreateTextView(title, 15, _textPrimaryColor, TypefaceStyle.Bold));

        var subtitleView = CreateTextView(subtitle, 12, _textSecondaryColor);
        subtitleView.SetPadding(0, Dp(4), 0, 0);
        card.AddView(subtitleView);
        return card;
    }

    private Drawable CreateCardBackground()
    {
        var drawable = new GradientDrawable();
        drawable.SetColor(_surfaceColor);
        drawable.SetCornerRadius(Dp(18));
        drawable.SetStroke(Dp(1), _borderColor);
        return drawable;
    }

    private Drawable CreateRoundedBackground(global::Android.Graphics.Color color)
    {
        var drawable = new GradientDrawable();
        drawable.SetColor(color);
        drawable.SetCornerRadius(Dp(14));
        drawable.SetStroke(Dp(1), _borderColor);
        return drawable;
    }

    private View Spacer(int widthDp)
    {
        return new Space(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(Dp(widthDp), 1)
        };
    }

    private int Dp(int value)
    {
        return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Resources?.DisplayMetrics ?? new DisplayMetrics());
    }

    private void SetConnectionStatus(string message)
    {
        if (_connectionStatusText is not null)
        {
            _connectionStatusText.Text = message;
        }
    }
}
