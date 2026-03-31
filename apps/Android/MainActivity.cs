using Android.App;
using Android.Graphics;
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
    private readonly global::Android.Graphics.Color _textPrimaryColor = new(Color.ParseColor("#f5f7fb"));
    private readonly global::Android.Graphics.Color _textSecondaryColor = new(Color.ParseColor("#aab4c5"));
    private readonly global::Android.Graphics.Color _accentColor = new(Color.ParseColor("#f6a57a"));

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
            Orientation = Orientation.Vertical,
        };
        root.SetGravity(GravityFlags.CenterHorizontal);
        root.SetPadding(Dp(24), Dp(40), Dp(24), Dp(40));
        root.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        var card = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        card.SetPadding(Dp(20), Dp(20), Dp(20), Dp(20));
        card.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        card.SetBackgroundColor(_surfaceColor);

        var title = CreateTextView("Relationship Planner", 30, _textPrimaryColor, TypefaceStyle.Bold);
        var subtitle = CreateTextView(
            "Native Android baseline is running.\nWe will add one feature at a time and confirm each step.",
            15,
            _textSecondaryColor);
        subtitle.SetPadding(0, Dp(8), 0, 0);

        var status = CreateTextView(
            "Step 1: title screen only.",
            14,
            _accentColor,
            TypefaceStyle.Bold);
        status.SetPadding(0, Dp(14), 0, 0);

        card.AddView(title);
        card.AddView(subtitle);
        card.AddView(status);
        root.AddView(card);
        scrollView.AddView(root);

        return scrollView;
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

    private int Dp(int value)
    {
        return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Resources?.DisplayMetrics ?? new DisplayMetrics());
    }
}
