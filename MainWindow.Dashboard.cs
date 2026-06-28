using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Pathfinder.Domain.Entities;

namespace Pathfinder;

public partial class MainWindow : Window
{
    private void RenderDashboard()
    {
        RenderShell("DASHBOARD", body =>
        {
            var heroGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            heroGradient.GradientStops.Add(new GradientStop(Color.FromRgb(79, 70, 229), 0.0)); // Indigo 600
            heroGradient.GradientStops.Add(new GradientStop(Color.FromRgb(124, 58, 237), 1.0)); // Violet 600

            var hero = PanelCard(16, heroGradient, BorderBrush());
            hero.Margin = new Thickness(0, 0, 0, 36);
            var heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition());
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });

            var heroText = new StackPanel();
            heroText.Children.Add(Label("MY ACTIVE PATHS", 30, FontWeights.ExtraBold, Brushes.White));
            heroText.Children.Add(Label("Continue your learning journey across the technical landscape.", 13, FontWeights.Medium, new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 0, 6, 0, 0));
            heroGrid.Children.Add(heroText);

            var searchBox = new TextBox
            {
                Text = _searchText,
                Height = 40,
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Foreground = Brushes.White
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
            searchBox.Template = template;

            searchBox.GotFocus += (s, e) =>
            {
                searchBox.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                searchBox.BorderBrush = Brushes.White;
            };
            searchBox.LostFocus += (s, e) =>
            {
                searchBox.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                searchBox.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            };

            var searchBoxGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            var searchHint = new TextBlock
            {
                Text = "Search paths...",
                Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(16, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Opacity = 0.8
            };

            void UpdateSearchHint()
            {
                searchHint.Visibility = string.IsNullOrWhiteSpace(searchBox.Text) && !searchBox.IsKeyboardFocused
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            searchBox.TextChanged += (_, _) => UpdateSearchHint();
            searchBox.GotFocus += (_, _) => UpdateSearchHint();
            searchBox.LostFocus += (_, _) => UpdateSearchHint();
            searchBoxGrid.Loaded += (_, _) => UpdateSearchHint();

            searchBoxGrid.Children.Add(searchBox);
            searchBoxGrid.Children.Add(searchHint);

            Grid.SetColumn(searchBoxGrid, 1);
            heroGrid.Children.Add(searchBoxGrid);
            hero.Child = heroGrid;
            body.Children.Add(hero);

            var cards = new WrapPanel { ItemWidth = 388, ItemHeight = 448 };
            body.Children.Add(cards);

            searchBox.TextChanged += (_, _) =>
            {
                _searchText = searchBox.Text;
                RenderDashboardCards();
            };

            void RenderDashboardCards()
            {
                cards.Children.Clear();
                var routes = _routeService.GetRoutes()
                    .Where(route => string.IsNullOrWhiteSpace(_searchText) ||
                                    route.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                                    route.Overview.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var route in routes)
                {
                    cards.Children.Add(RouteCard(route));
                }

                cards.Children.Add(CreateRouteTile());
            }

            RenderDashboardCards();
        });
    }

    private Border RouteCard(LearningRoute route)
    {
        var card = PanelCard(16, CardBrush(), BorderBrush());
        card.Margin = new Thickness(0, 0, 28, 28);
        card.Width = 360;
        card.Height = 420;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(154) });
        root.RowDefinitions.Add(new RowDefinition());

        var isBeginner = string.Equals(route.Level, "Beginners", StringComparison.OrdinalIgnoreCase);
        var isAdvanced = string.Equals(route.Level, "Advanced", StringComparison.OrdinalIgnoreCase);

        var previewGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        
        if (isBeginner)
        {
            previewGradient.GradientStops.Add(new GradientStop(Color.FromRgb(14, 165, 233), 0.0)); // Sky 500
            previewGradient.GradientStops.Add(new GradientStop(Color.FromRgb(37, 99, 235), 1.0)); // Blue 600
        }
        else if (isAdvanced)
        {
            previewGradient.GradientStops.Add(new GradientStop(Color.FromRgb(236, 72, 153), 0.0)); // Pink 500
            previewGradient.GradientStops.Add(new GradientStop(Color.FromRgb(124, 58, 237), 1.0)); // Violet 600
        }
        else // Intermediate
        {
            previewGradient.GradientStops.Add(new GradientStop(Color.FromRgb(99, 102, 241), 0.0)); // Indigo 500
            previewGradient.GradientStops.Add(new GradientStop(Color.FromRgb(139, 92, 246), 1.0)); // Purple 500
        }

        var preview = new Border
        {
            Background = previewGradient,
            CornerRadius = new CornerRadius(15, 15, 0, 0),
            ClipToBounds = true
        };
        
        var previewGrid = new Grid();
        previewGrid.Children.Add(Label(route.Modules.Count == 1 ? "1 MODULE" : $"{route.Modules.Count} MODULES", 10, FontWeights.Bold, Brushes.White, 18, 18, 0, 0));
        previewGrid.Children.Add(Label("PATH", 36, FontWeights.ExtraBold, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 0, 0, 0, 0, HorizontalAlignment.Center, VerticalAlignment.Center));
        
        var previewActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 12, 12, 0)
        };
        
        var previewDelete = IconButton("\uE74D", "Eliminar ruta", new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), Brushes.White, 32);
        previewDelete.Click += (_, _) =>
        {
            if (MessageBox.Show("Eliminar esta ruta?", "Pathfinder", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _routeService.DeleteRoute(route.Id);
                RenderDashboard();
            }
        };
        previewActions.Children.Add(previewDelete);
        previewGrid.Children.Add(previewActions);
        preview.Child = previewGrid;
        root.Children.Add(preview);

        var content = new Grid { Margin = new Thickness(22, 20, 22, 18) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Pill
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Overview
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // ProgressBar
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer
        Grid.SetRow(content, 1);

        var pill = Pill(route.Level.ToUpperInvariant(), PrimaryBrush(), SoftBrush());
        Grid.SetRow(pill, 0);
        content.Children.Add(pill);

        var title = Label(route.Title, 20, FontWeights.Bold, TextBrush(), 0, 12, 0, 6);
        title.MaxHeight = 54;
        title.TextTrimming = TextTrimming.CharacterEllipsis;
        Grid.SetRow(title, 1);
        content.Children.Add(title);

        var overview = Label(route.Overview, 13, FontWeights.Medium, MutedBrush());
        overview.TextTrimming = TextTrimming.CharacterEllipsis;
        overview.Margin = new Thickness(0, 0, 0, 8);
        Grid.SetRow(overview, 2);
        content.Children.Add(overview);

        var progress = ProgressBar(route.Progress, 0, 12, 0, 12);
        Grid.SetRow(progress, 3);
        content.Children.Add(progress);

        var footer = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        Grid.SetRow(footer, 4);

        var view = Button("VIEW ROUTE", PrimaryBrush(), Brushes.White);
        view.Click += (_, _) => RenderDetail(route.Id);
        DockPanel.SetDock(view, Dock.Left);
        footer.Children.Add(view);

        var edit = IconButton("\uE70F", "Editar ruta", SoftBrush(), TextBrush());
        edit.Margin = new Thickness(10, 0, 0, 0);
        edit.Click += (_, _) => RenderEditor(CloneRoute(route));
        DockPanel.SetDock(edit, Dock.Right);
        footer.Children.Add(edit);

        content.Children.Add(footer);
        root.Children.Add(content);
        card.Child = root;

        SetupCardHover(card, CardBrush(), BorderBrush(), CardBrush(), PrimaryBrush());
        return card;
    }

    private Border CreateRouteTile()
    {
        var tile = PanelCard(16, TransparentBrush(), BorderBrush());
        tile.Margin = new Thickness(0, 0, 28, 28);
        tile.Width = 360;
        tile.Height = 420;
        tile.BorderThickness = new Thickness(0); // we use custom rectangle for dashed border

        var grid = new Grid();
        var dashArray = new DoubleCollection { 4, 3 };
        var dashRect = new System.Windows.Shapes.Rectangle
        {
            Stroke = new SolidColorBrush(_isDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225)),
            StrokeThickness = 1.5,
            StrokeDashArray = dashArray,
            RadiusX = 16,
            RadiusY = 16,
            Margin = new Thickness(-22) // offset padding from PanelCard
        };
        grid.Children.Add(dashRect);

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(Label("+", 40, FontWeights.Light, MutedBrush(), horizontal: HorizontalAlignment.Center));
        stack.Children.Add(Label("CREATE NEW ROUTE", 14, FontWeights.Bold, MutedBrush(), horizontal: HorizontalAlignment.Center));
        grid.Children.Add(stack);
        tile.Child = grid;
        tile.Cursor = Cursors.Hand;
        tile.MouseLeftButtonUp += (_, _) => RenderEditor(NewRoute());

        SetupCardHover(tile, TransparentBrush(), BorderBrush(), SoftBrush(), PrimaryBrush());
        tile.MouseEnter += (s, e) =>
        {
            dashRect.Stroke = PrimaryBrush();
            dashRect.StrokeThickness = 2;
        };
        tile.MouseLeave += (s, e) =>
        {
            dashRect.Stroke = new SolidColorBrush(_isDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225));
            dashRect.StrokeThickness = 1.5;
        };

        return tile;
    }
}
