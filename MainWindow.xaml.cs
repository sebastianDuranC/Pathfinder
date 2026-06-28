using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using Pathfinder.Application.Services;
using Pathfinder.Domain.Entities;
using Pathfinder.Infrastructure.Persistence;

namespace Pathfinder;

public partial class MainWindow : Window
{
    private readonly LearningRouteService _routeService;
    private bool _isDarkMode;
    private string _searchText = "";
    private int _currentRouteId;

    public MainWindow()
    {
        InitializeComponent();
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        TextOptions.SetTextHintingMode(this, TextHintingMode.Fixed);
        _routeService = new LearningRouteService(new SqliteLearningRouteRepository());
        _routeService.Initialize();
        RenderDashboard();
    }

    private void RenderShell(string activeSection, Action<StackPanel> renderBody)
    {
        Host.Children.Clear();
        Background = BackgroundBrush();

        var root = new DockPanel { Background = BackgroundBrush() };
        root.Children.Add(Header(activeSection));

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var body = new StackPanel { Margin = new Thickness(28, 34, 28, 40) };
        renderBody(body);
        scroll.Content = body;
        root.Children.Add(scroll);
        Host.Children.Add(root);
    }

    private Border Header(string activeSection)
    {
        var header = new Border
        {
            Height = 62,
            Background = CardBrush(),
            BorderBrush = InkBrush(),
            BorderThickness = new Thickness(0, 0, 0, 2)
        };
        DockPanel.SetDock(header, Dock.Top);

        var dock = new DockPanel { Margin = new Thickness(28, 0, 28, 0) };
        header.Child = dock;

        var logo = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        logo.Children.Add(new Border
        {
            Width = 34,
            Height = 34,
            CornerRadius = new CornerRadius(4),
            Background = PrimaryBrush(),
            BorderBrush = InkBrush(),
            BorderThickness = new Thickness(3),
            Child = Label("\u25C6", 18, FontWeights.Black, Brushes.White, horizontal: HorizontalAlignment.Center, vertical: VerticalAlignment.Center)
        });
        logo.Children.Add(Label("PATHFINDER", 20, FontWeights.Black, TextBrush(), 10, 0, 12, 0));
        logo.Children.Add(Pill("LOCAL V1.0", MutedBrush(), SoftBrush(), vertical: VerticalAlignment.Center));

        var nav = Button("DASHBOARD", CardBrush(), activeSection == "DASHBOARD" ? PrimaryBrush() : MutedBrush());
        nav.Width = 110;
        nav.Height = 28;
        nav.Margin = new Thickness(16, 0, 0, 0);
        nav.VerticalAlignment = VerticalAlignment.Center;
        nav.Click += (_, _) => RenderDashboard();
        logo.Children.Add(nav);

        DockPanel.SetDock(logo, Dock.Left);
        dock.Children.Add(logo);

        var right = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var toggle = new Border
        {
            Width = 76,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Background = _isDarkMode ? Brush("#1E293B") : Brush("#F1F5F9"),
            BorderBrush = _isDarkMode ? Brush("#334155") : Brush("#E2E8F0"),
            BorderThickness = new Thickness(1.5),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 0, 12, 0)
        };

        var toggleGrid = new Grid();
        
        var thumb = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            Background = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0),
            Effect = new DropShadowEffect
            {
                BlurRadius = 4,
                ShadowDepth = 1,
                Color = Colors.Black,
                Opacity = 0.12
            }
        };
        var translate = new TranslateTransform();
        thumb.RenderTransform = translate;
        translate.X = _isDarkMode ? 40 : 0;
        
        toggleGrid.Children.Add(thumb);

        var sunIcon = new TextBlock
        {
            Text = "\uE706",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = _isDarkMode ? Brush("#64748B") : Brush("#0F172A"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var moonIcon = new TextBlock
        {
            Text = "\uE708",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = _isDarkMode ? Brush("#0F172A") : Brush("#94A3B8"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var columnsGrid = new Grid();
        columnsGrid.ColumnDefinitions.Add(new ColumnDefinition());
        columnsGrid.ColumnDefinitions.Add(new ColumnDefinition());
        
        Grid.SetColumn(sunIcon, 0);
        Grid.SetColumn(moonIcon, 1);
        columnsGrid.Children.Add(sunIcon);
        columnsGrid.Children.Add(moonIcon);
        columnsGrid.IsHitTestVisible = false;

        toggleGrid.Children.Add(columnsGrid);
        toggle.Child = toggleGrid;
        
        toggle.MouseLeftButtonUp += (s, e) =>
        {
            var targetX = _isDarkMode ? 0 : 40;
            var anim = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            anim.Completed += (sender, args) =>
            {
                _isDarkMode = !_isDarkMode;
                if (activeSection == "ROUTE")
                {
                    RenderDetail(_currentRouteId);
                }
                else
                {
                    RenderDashboard();
                }
            };
            
            translate.BeginAnimation(TranslateTransform.XProperty, anim);
        };
        right.Children.Add(toggle);

        var newPath = Button("+ NEW PATH", PrimaryBrush(), Brushes.White);
        newPath.Width = 128;
        newPath.Click += (_, _) => RenderEditor(NewRoute());
        right.Children.Add(newPath);
        DockPanel.SetDock(right, Dock.Right);
        dock.Children.Add(right);

        return header;
    }

    private static ControlTemplate CreateButtonTemplate(CornerRadius cornerRadius)
    {
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "border";
        borderFactory.SetValue(Border.CornerRadiusProperty, cornerRadius);
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentFactory.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        borderFactory.AppendChild(contentFactory);

        var template = new ControlTemplate(typeof(Button));
        template.VisualTree = borderFactory;
        return template;
    }

    private static Color LightenColor(Color color)
    {
        const float factor = 0.12f;
        return Color.FromRgb(
            (byte)Math.Min(255, color.R + (255 - color.R) * factor),
            (byte)Math.Min(255, color.G + (255 - color.G) * factor),
            (byte)Math.Min(255, color.B + (255 - color.B) * factor)
        );
    }

    private void SetupCardHover(Border card, Brush normalBg, Brush normalBorder, Brush hoverBg, Brush hoverBorder, bool scaleOnHover = true)
    {
        card.Background = normalBg;
        card.BorderBrush = normalBorder;
        
        var scale = new ScaleTransform(1.0, 1.0);
        card.RenderTransform = scale;
        card.RenderTransformOrigin = new Point(0.5, 0.5);
        
        card.MouseEnter += (s, e) =>
        {
            var dur = TimeSpan.FromMilliseconds(200);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            
            if (normalBg is SolidColorBrush normalBgS && hoverBg is SolidColorBrush hoverBgS)
            {
                var bgAnim = new ColorAnimation(normalBgS.Color, hoverBgS.Color, dur) { EasingFunction = ease };
                var newBgBrush = new SolidColorBrush(normalBgS.Color);
                card.Background = newBgBrush;
                newBgBrush.BeginAnimation(SolidColorBrush.ColorProperty, bgAnim);
            }
            
            if (normalBorder is SolidColorBrush normalBorderS && hoverBorder is SolidColorBrush hoverBorderS)
            {
                var borderAnim = new ColorAnimation(normalBorderS.Color, hoverBorderS.Color, dur) { EasingFunction = ease };
                var newBorderBrush = new SolidColorBrush(normalBorderS.Color);
                card.BorderBrush = newBorderBrush;
                newBorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);
            }
            
            if (scaleOnHover)
            {
                var scaleAnim = new DoubleAnimation(1.0, 1.022, dur) { EasingFunction = ease };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }

            if (card.Effect is DropShadowEffect shadow)
            {
                var shadowAnim = new DoubleAnimation(shadow.ShadowDepth, 4, dur) { EasingFunction = ease };
                var blurAnim = new DoubleAnimation(shadow.BlurRadius, 20, dur) { EasingFunction = ease };
                var opacityAnim = new DoubleAnimation(shadow.Opacity, _isDarkMode ? 0.50 : 0.14, dur) { EasingFunction = ease };
                shadow.BeginAnimation(DropShadowEffect.ShadowDepthProperty, shadowAnim);
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
            }
        };
        
        card.MouseLeave += (s, e) =>
        {
            var dur = TimeSpan.FromMilliseconds(200);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            
            if (normalBg is SolidColorBrush normalBgS && hoverBg is SolidColorBrush hoverBgS)
            {
                var bgAnim = new ColorAnimation(hoverBgS.Color, normalBgS.Color, dur) { EasingFunction = ease };
                var newBgBrush = new SolidColorBrush(hoverBgS.Color);
                card.Background = newBgBrush;
                newBgBrush.BeginAnimation(SolidColorBrush.ColorProperty, bgAnim);
            }
            
            if (normalBorder is SolidColorBrush normalBorderS && hoverBorder is SolidColorBrush hoverBorderS)
            {
                var borderAnim = new ColorAnimation(hoverBorderS.Color, normalBorderS.Color, dur) { EasingFunction = ease };
                var newBorderBrush = new SolidColorBrush(hoverBorderS.Color);
                card.BorderBrush = newBorderBrush;
                newBorderBrush.BeginAnimation(SolidColorBrush.ColorProperty, borderAnim);
            }
            
            if (scaleOnHover)
            {
                var scaleAnim = new DoubleAnimation(scale.ScaleX, 1.0, dur) { EasingFunction = ease };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }

            if (card.Effect is DropShadowEffect shadow)
            {
                var origDepth = 2;
                var origBlur = 12;
                var origOpacity = _isDarkMode ? 0.35 : 0.08;
                
                var shadowAnim = new DoubleAnimation(shadow.ShadowDepth, origDepth, dur) { EasingFunction = ease };
                var blurAnim = new DoubleAnimation(shadow.BlurRadius, origBlur, dur) { EasingFunction = ease };
                var opacityAnim = new DoubleAnimation(shadow.Opacity, origOpacity, dur) { EasingFunction = ease };
                shadow.BeginAnimation(DropShadowEffect.ShadowDepthProperty, shadowAnim);
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
            }
        };
    }

    private static void AnimateEntrance(UIElement element)
    {
        element.Opacity = 0;
        var translate = new TranslateTransform(0, 16);
        element.RenderTransform = translate;
        
        var fadeAnim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        var slideAnim = new DoubleAnimation
        {
            From = 16.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        translate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
    }

    private Border PanelCard(double radius, Brush background, Brush border)
    {
        return new Border
        {
            Background = background,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            Padding = new Thickness(22),
            Effect = new DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 2,
                Direction = 270,
                Color = _isDarkMode ? Color.FromRgb(2, 6, 23) : Color.FromRgb(71, 85, 105),
                Opacity = _isDarkMode ? 0.35 : 0.08
            }
        };
    }

    private TextBlock Label(
        string text,
        double size,
        FontWeight weight,
        Brush brush,
        double left = 0,
        double top = 0,
        double right = 0,
        double bottom = 0,
        HorizontalAlignment horizontal = HorizontalAlignment.Left,
        VerticalAlignment vertical = VerticalAlignment.Top)
    {
        var label = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight == FontWeights.Black ? FontWeights.ExtraBold : weight,
            Foreground = brush,
            Margin = new Thickness(left, top, right, bottom),
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = Math.Max(size + 4, size * 1.18)
        };
        TextOptions.SetTextFormattingMode(label, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(label, TextRenderingMode.ClearType);
        TextOptions.SetTextHintingMode(label, TextHintingMode.Fixed);
        return label;
    }

    private TextBlock FieldLabel(string text) =>
        Label(text, 10, FontWeights.Bold, MutedBrush(), 0, 0, 0, 6);

    private TextBox Input(string value, double height, FontWeight weight)
    {
        double verticalPadding = height <= 30 ? 3 : (height <= 36 ? 4 : 8);
        var input = new TextBox
        {
            Text = value,
            Height = height,
            Padding = new Thickness(12, verticalPadding, 12, verticalPadding),
            FontWeight = weight,
            FontSize = height > 40 ? 14 : 13,
            Background = CardBrush(),
            Foreground = TextBrush(),
            BorderBrush = BorderBrush(),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.Name = "border";
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var scrollFactory = new FrameworkElementFactory(typeof(ScrollViewer));
        scrollFactory.Name = "PART_ContentHost";
        borderFactory.AppendChild(scrollFactory);

        var template = new ControlTemplate(typeof(TextBox));
        template.VisualTree = borderFactory;
        input.Template = template;

        input.GotFocus += (s, e) =>
        {
            input.BorderBrush = PrimaryBrush();
            if (input.Template.FindName("border", input) is Border border)
            {
                border.BorderBrush = PrimaryBrush();
                border.BorderThickness = new Thickness(1.5);
            }
        };
        input.LostFocus += (s, e) =>
        {
            input.BorderBrush = BorderBrush();
            if (input.Template.FindName("border", input) is Border border)
            {
                border.BorderBrush = BorderBrush();
                border.BorderThickness = new Thickness(1);
            }
        };

        TextOptions.SetTextFormattingMode(input, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(input, TextRenderingMode.ClearType);
        TextOptions.SetTextHintingMode(input, TextHintingMode.Fixed);
        return input;
    }

    private Button Button(string text, Brush background, Brush foreground)
    {
        var button = new Button
        {
            Content = text,
            Height = 36,
            Padding = new Thickness(16, 0, 16, 0),
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Background = background,
            Foreground = foreground,
            BorderBrush = BorderBrush(),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Template = CreateButtonTemplate(new CornerRadius(8))
        };
        
        button.MouseEnter += (s, e) =>
        {
            button.Opacity = 0.9;
            if (button.Background is SolidColorBrush scb)
            {
                var anim = new ColorAnimation(scb.Color, LightenColor(scb.Color), TimeSpan.FromMilliseconds(150));
                var newBrush = new SolidColorBrush(scb.Color);
                button.Background = newBrush;
                newBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        };
        button.MouseLeave += (s, e) =>
        {
            button.Opacity = 1.0;
            if (button.Background is SolidColorBrush scb && background is SolidColorBrush origScb)
            {
                var anim = new ColorAnimation(scb.Color, origScb.Color, TimeSpan.FromMilliseconds(150));
                var newBrush = new SolidColorBrush(scb.Color);
                button.Background = newBrush;
                newBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        };
        
        return button;
    }

    private Button IconButton(string glyph, string tooltip, Brush background, Brush foreground, double size = 36)
    {
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 13,
            Foreground = foreground,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        TextOptions.SetTextFormattingMode(icon, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(icon, TextRenderingMode.ClearType);

        var button = new Button
        {
            Content = icon,
            Width = size,
            Height = size,
            Padding = new Thickness(0),
            Background = background,
            Foreground = foreground,
            BorderBrush = BorderBrush(),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = tooltip,
            Template = CreateButtonTemplate(new CornerRadius(size / 2.0))
        };

        button.MouseEnter += (s, e) =>
        {
            if (button.Background is SolidColorBrush scb)
            {
                var targetColor = _isDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(241, 245, 249);
                var anim = new ColorAnimation(scb.Color, targetColor, TimeSpan.FromMilliseconds(150));
                var newBrush = new SolidColorBrush(scb.Color);
                button.Background = newBrush;
                newBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        };
        button.MouseLeave += (s, e) =>
        {
            if (button.Background is SolidColorBrush scb && background is SolidColorBrush origScb)
            {
                var anim = new ColorAnimation(scb.Color, origScb.Color, TimeSpan.FromMilliseconds(150));
                var newBrush = new SolidColorBrush(scb.Color);
                button.Background = newBrush;
                newBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        };

        return button;
    }

    private Grid WithPlaceholder(TextBox input, string placeholder)
    {
        var grid = new Grid { Margin = input.Margin };
        input.Margin = new Thickness(0);

        var hint = new TextBlock
        {
            Text = placeholder,
            Foreground = MutedBrush(),
            FontSize = input.FontSize,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(14, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
            Opacity = 0.6
        };

        void UpdateHint()
        {
            hint.Visibility = string.IsNullOrWhiteSpace(input.Text) && !input.IsKeyboardFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        input.TextChanged += (_, _) => UpdateHint();
        input.GotKeyboardFocus += (_, _) => UpdateHint();
        input.LostKeyboardFocus += (_, _) => UpdateHint();
        grid.Loaded += (_, _) => UpdateHint();

        grid.Children.Add(input);
        grid.Children.Add(hint);
        return grid;
    }

    private Border Pill(
        string text,
        Brush foreground,
        Brush background,
        double left = 0,
        double top = 0,
        double right = 0,
        double bottom = 0,
        VerticalAlignment vertical = VerticalAlignment.Top)
    {
        return new Border
        {
            Background = background,
            BorderBrush = foreground,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 3, 10, 3),
            Margin = new Thickness(left, top, right, bottom),
            VerticalAlignment = vertical,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Label(text, 10, FontWeights.Bold, foreground)
        };
    }

    private Border Separator() =>
        new()
        {
            Height = 1,
            Background = BorderBrush(),
            Margin = new Thickness(-22, 12, -22, 12)
        };

    private Grid ProgressBar(int value, double left = 0, double top = 0, double right = 0, double bottom = 0)
    {
        var root = new Grid
        {
            Height = 16,
            Margin = new Thickness(left, top, right, bottom),
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var track = new Border
        {
            Background = SoftBrush(),
            BorderBrush = BorderBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Height = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        root.Children.Add(track);

        var filledColor = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0)
        };
        filledColor.GradientStops.Add(new GradientStop(Color.FromRgb(99, 102, 241), 0.0)); // Indigo
        filledColor.GradientStops.Add(new GradientStop(Color.FromRgb(16, 185, 129), 1.0)); // Emerald

        var indicator = new Border
        {
            Background = filledColor,
            CornerRadius = new CornerRadius(8),
            Height = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        var scale = new ScaleTransform(0.0, 1.0);
        indicator.RenderTransform = scale;
        indicator.RenderTransformOrigin = new Point(0.0, 0.5);
        
        root.Children.Add(indicator);
        
        var anim = new DoubleAnimation
        {
            From = 0.0,
            To = value / 100.0,
            Duration = TimeSpan.FromMilliseconds(800),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        
        return root;
    }

    private Brush BackgroundBrush() => Brush(_isDarkMode ? "#0B1220" : "#F8FAFC");
    private Brush CardBrush() => Brush(_isDarkMode ? "#131E35" : "#FFFFFF");
    private Brush SoftBrush() => Brush(_isDarkMode ? "#1E2B45" : "#F1F5F9");
    private Brush TextBrush() => Brush(_isDarkMode ? "#F8FAFC" : "#0F172A");
    private Brush InkBrush() => Brush(_isDarkMode ? "#020617" : "#0F172A");
    private Brush MutedBrush() => Brush(_isDarkMode ? "#94A3B8" : "#64748B");
    private new Brush BorderBrush() => Brush(_isDarkMode ? "#202E4E" : "#E2E8F0");
    private Brush PrimaryBrush() => Brush("#6366F1");
    private Brush AccentBrush() => Brush("#F59E0B");
    private Brush CompletedBrush() => Brush(_isDarkMode ? "#103025" : "#E6F4EA");
    private Brush DarkFieldBrush() => Brush("#1E2B45");
    private static Brush TransparentBrush() => Brushes.Transparent;
    private static SolidColorBrush Brush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

    private static FrameworkElement CreateGripHorizontalIcon(Brush brush, double size = 18)
    {
        var canvas = new Canvas { Width = 24, Height = 24 };
        var points = new[]
        {
            new Point(5, 9), new Point(12, 9), new Point(19, 9),
            new Point(5, 15), new Point(12, 15), new Point(19, 15)
        };

        foreach (var pt in points)
        {
            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = 2.5,
                Height = 2.5,
                Fill = brush
            };
            Canvas.SetLeft(ellipse, pt.X - 1.25);
            Canvas.SetTop(ellipse, pt.Y - 1.25);
            canvas.Children.Add(ellipse);
        }

        var viewbox = new Viewbox
        {
            Width = size,
            Height = size,
            Child = canvas
        };

        return new Border
        {
            Background = Brushes.Transparent,
            Width = size,
            Height = size,
            Child = viewbox,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.SizeAll
        };
    }

    private Button CreateStyledDropdown(SourceKind currentKind, Action<SourceKind> onSelected)
    {
        var button = new Button
        {
            Height = 26,
            Width = 96,
            Background = SoftBrush(),
            Foreground = PrimaryBrush(),
            BorderBrush = PrimaryBrush(),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Template = CreateButtonTemplate(new CornerRadius(6))
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });

        var text = Label(currentKind.ToString().ToUpperInvariant(), 10, FontWeights.Bold, PrimaryBrush(), horizontal: HorizontalAlignment.Left, vertical: VerticalAlignment.Center);
        text.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(text, 0);
        grid.Children.Add(text);

        var arrow = new TextBlock
        {
            Text = "\uE70D",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 8,
            Foreground = PrimaryBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arrow, 1);
        grid.Children.Add(arrow);

        button.Content = grid;

        button.Click += (s, e) =>
        {
            var menu = new ContextMenu
            {
                Background = CardBrush(),
                BorderBrush = BorderBrush(),
                BorderThickness = new Thickness(1)
            };

            foreach (var kind in Enum.GetValues<SourceKind>())
            {
                var item = new MenuItem
                {
                    Header = kind.ToString().ToUpperInvariant(),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = TextBrush(),
                    Height = 28
                };
                item.Click += (_, _) => onSelected(kind);
                menu.Items.Add(item);
            }

            menu.PlacementTarget = button;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        };

        button.MouseEnter += (s, e) =>
        {
            button.Background = PrimaryBrush();
            text.Foreground = Brushes.White;
            arrow.Foreground = Brushes.White;
        };
        button.MouseLeave += (s, e) =>
        {
            button.Background = SoftBrush();
            text.Foreground = PrimaryBrush();
            arrow.Foreground = PrimaryBrush();
        };

        return button;
    }

    private Button CreateLevelDropdown(string currentLevel, Action<string> onSelected)
    {
        var button = new Button
        {
            Height = 34,
            Background = SoftBrush(),
            Foreground = PrimaryBrush(),
            BorderBrush = PrimaryBrush(),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Template = CreateButtonTemplate(new CornerRadius(6))
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

        var text = Label(currentLevel, 12, FontWeights.Bold, PrimaryBrush(), horizontal: HorizontalAlignment.Left, vertical: VerticalAlignment.Center);
        text.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(text, 0);
        grid.Children.Add(text);

        var arrow = new TextBlock
        {
            Text = "\uE70D",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 10,
            Foreground = PrimaryBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(arrow, 1);
        grid.Children.Add(arrow);

        button.Content = grid;

        button.Click += (s, e) =>
        {
            var menu = new ContextMenu
            {
                Background = CardBrush(),
                BorderBrush = BorderBrush(),
                BorderThickness = new Thickness(1)
            };

            foreach (var level in new[] { "Beginners", "Intermediate", "Advanced" })
             {
                 var item = new MenuItem
                 {
                     Header = level.ToUpperInvariant(),
                     FontWeight = FontWeights.Bold,
                     FontSize = 11,
                     Foreground = TextBrush(),
                     Height = 32
                 };
                 item.Click += (_, _) => onSelected(level);
                 menu.Items.Add(item);
             }

             menu.PlacementTarget = button;
             menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
             menu.IsOpen = true;
         };

         button.MouseEnter += (s, e) =>
         {
             button.Background = PrimaryBrush();
             text.Foreground = Brushes.White;
             arrow.Foreground = Brushes.White;
         };
         button.MouseLeave += (s, e) =>
         {
             button.Background = SoftBrush();
             text.Foreground = PrimaryBrush();
             arrow.Foreground = PrimaryBrush();
         };

         return button;
     }

    private static void RedirectMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            var canScrollVertically = sv.ScrollableHeight > 0;
            if (!canScrollVertically)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = VisualTreeHelper.GetParent(sv) as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : $"{value[..max]}...";
}
