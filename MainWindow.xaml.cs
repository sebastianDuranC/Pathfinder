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

            var cards = new WrapPanel { ItemWidth = 388, ItemHeight = 418 };
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
        card.Height = 390;

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
        
        var previewView = IconButton("\uE8A7", "Visualizar ruta", new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), Brushes.White, 32);
        var previewDelete = IconButton("\uE74D", "Eliminar ruta", new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), Brushes.White, 32);
        previewDelete.Margin = new Thickness(8, 0, 0, 0);
        previewView.Click += (_, _) => RenderDetail(route.Id);
        previewDelete.Click += (_, _) =>
        {
            if (MessageBox.Show("Eliminar esta ruta?", "Pathfinder", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _routeService.DeleteRoute(route.Id);
                RenderDashboard();
            }
        };
        previewActions.Children.Add(previewView);
        previewActions.Children.Add(previewDelete);
        previewGrid.Children.Add(previewActions);
        preview.Child = previewGrid;
        root.Children.Add(preview);

        var content = new StackPanel { Margin = new Thickness(22, 20, 22, 18) };
        Grid.SetRow(content, 1);
        content.Children.Add(Pill(route.Level.ToUpperInvariant(), PrimaryBrush(), SoftBrush()));
        content.Children.Add(Label(route.Title, 20, FontWeights.Bold, TextBrush(), 0, 12, 0, 6));
        content.Children.Add(Label(Trim(route.Overview, 112), 13, FontWeights.Medium, MutedBrush()));
        content.Children.Add(ProgressBar(route.Progress, 0, 20, 0, 16));

        var footer = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
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
        tile.Height = 390;
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

    private void RenderEditor(LearningRoute draft)
    {
        RenderShell(draft.Id == 0 ? "NEW PATH" : "EDIT PATH", body =>
        {
            var top = PanelCard(16, CardBrush(), BorderBrush());
            top.Margin = new Thickness(0, 0, 0, 30);
            var topGrid = new Grid { Margin = new Thickness(0) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition());
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            top.Child = topGrid;

            var form = new StackPanel { Margin = new Thickness(0, 0, 24, 0) };
            form.Children.Add(FieldLabel("ROUTE TITLE"));
            var titleBox = Input(draft.Title, 40, FontWeights.Bold);
            titleBox.TextChanged += (_, _) => draft.Title = titleBox.Text;
            form.Children.Add(WithPlaceholder(titleBox, "ENTER PATH NAME ..."));
            form.Children.Add(FieldLabel("OVERVIEW"));
            var overviewBox = Input(draft.Overview, 86, FontWeights.Normal);
            overviewBox.AcceptsReturn = true;
            overviewBox.TextWrapping = TextWrapping.Wrap;
            overviewBox.TextChanged += (_, _) => draft.Overview = overviewBox.Text;
            form.Children.Add(WithPlaceholder(overviewBox, "Briefly explain the goal of this route ..."));
            topGrid.Children.Add(form);

            var side = PanelCard(12, SoftBrush(), BorderBrush());
            side.Padding = new Thickness(18);
            var sideStack = new StackPanel();
            
            sideStack.Children.Add(Label("LEVEL", 10, FontWeights.Bold, MutedBrush(), 0, 0, 0, 4));
            var levelBox = CreateLevelDropdown(string.IsNullOrWhiteSpace(draft.Level) ? "Beginners" : draft.Level, selectedLevel =>
            {
                draft.Level = selectedLevel;
                RenderEditor(draft);
            });
            sideStack.Children.Add(levelBox);

            var actions = new DockPanel { Margin = new Thickness(0, 22, 0, 0) };
            var cancel = Button("CANCEL", BorderBrush(), TextBrush());
            cancel.Width = 88;
            cancel.Click += (_, _) => RenderDashboard();
            DockPanel.SetDock(cancel, Dock.Left);
            actions.Children.Add(cancel);

            var save = Button("PUBLISH", PrimaryBrush(), Brushes.White);
            save.Width = 104;
            save.Click += (_, _) => SaveDraft(draft);
            DockPanel.SetDock(save, Dock.Right);
            actions.Children.Add(save);
            sideStack.Children.Add(actions);
            side.Child = sideStack;
            Grid.SetColumn(side, 1);
            topGrid.Children.Add(side);
            body.Children.Add(top);

            var rowHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 20) };
            rowHeader.Children.Add(Label("ROADMAP ARCHITECTURE", 18, FontWeights.Bold, TextBrush()));
            var count = Label($"{draft.Modules.Count} MODULES", 10, FontWeights.Bold, MutedBrush(), horizontal: HorizontalAlignment.Right);
            DockPanel.SetDock(count, Dock.Right);
            rowHeader.Children.Add(count);
            body.Children.Add(rowHeader);

            var moduleRow = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var module in draft.Modules)
            {
                moduleRow.Children.Add(ModuleEditorCard(draft, module));
            }
            moduleRow.Children.Add(AddModuleTile(draft));

            var horizontalScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = moduleRow
            };
            horizontalScroll.PreviewMouseWheel += RedirectMouseWheel;
            body.Children.Add(horizontalScroll);
        });
    }

    private Border ModuleEditorCard(LearningRoute draft, LearningModule module)
    {
        var card = PanelCard(12, CardBrush(), BorderBrush());
        card.Width = 250;
        card.MinHeight = 300;
        card.Margin = new Thickness(0, 0, 22, 0);
        card.AllowDrop = true;
        card.Tag = module;
        card.Drop += (_, args) =>
        {
            if (args.Data.GetData(typeof(LearningModule)) is LearningModule dragged && dragged != module)
            {
                MoveItem(draft.Modules, dragged, draft.Modules.IndexOf(module));
                RenderEditor(draft);
            }
        };

        var stack = new StackPanel();
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10), LastChildFill = false };
        var gripper = CreateGripHorizontalIcon(MutedBrush(), 18);
        gripper.PreviewMouseMove += (_, args) => StartModuleDrag(card, args);
        DockPanel.SetDock(gripper, Dock.Left);
        header.Children.Add(gripper);
        
        var delete = IconButton("\uE74D", "Eliminar modulo", SoftBrush(), MutedBrush(), 28);
        delete.Height = 28;
        delete.Click += (_, _) =>
        {
            draft.Modules.Remove(module);
            RenderEditor(draft);
        };
        DockPanel.SetDock(delete, Dock.Right);
        header.Children.Add(delete);
        stack.Children.Add(header);

        var titleBox = Input(module.Title, 34, FontWeights.Bold);
        titleBox.TextChanged += (_, _) => module.Title = titleBox.Text;
        stack.Children.Add(WithPlaceholder(titleBox, "Module title..."));
        stack.Children.Add(Separator());

        var sourcesHost = new StackPanel();
        foreach (var source in module.Sources)
        {
            sourcesHost.Children.Add(SourceEditorCard(draft, module, source));
        }

        var sourcesScroll = new ScrollViewer
        {
            Content = sourcesHost,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 320,
            Margin = new Thickness(0, 0, 0, 12)
        };
        sourcesScroll.PreviewMouseWheel += RedirectMouseWheel;
        stack.Children.Add(sourcesScroll);

        var add = Button("+ ADD LINK", SoftBrush(), MutedBrush());
        add.Height = 36;
        add.Margin = new Thickness(0, 6, 0, 0);
        add.Click += (_, _) =>
        {
            module.Sources.Add(new LearningSource { Kind = SourceKind.Link });
            RenderEditor(draft);
        };
        stack.Children.Add(add);
        card.Child = stack;

        SetupCardHover(card, CardBrush(), BorderBrush(), CardBrush(), PrimaryBrush(), scaleOnHover: false);
        return card;
    }

    private Border SourceEditorCard(LearningRoute draft, LearningModule module, LearningSource source)
    {
        var card = PanelCard(8, SoftBrush(), BorderBrush());
        card.Margin = new Thickness(0, 0, 0, 10);
        card.Padding = new Thickness(12);
        card.AllowDrop = true;
        card.Tag = source;
        card.Drop += (_, args) =>
        {
            if (args.Data.GetData(typeof(LearningSource)) is LearningSource dragged && dragged != source && module.Sources.Contains(dragged))
            {
                MoveItem(module.Sources, dragged, module.Sources.IndexOf(source));
                RenderEditor(draft);
            }
        };

        var stack = new StackPanel();
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6), LastChildFill = false };
        
        var gripper = CreateGripHorizontalIcon(MutedBrush(), 14);
        gripper.PreviewMouseMove += (_, args) => StartSourceDrag(card, args);
        DockPanel.SetDock(gripper, Dock.Left);
        header.Children.Add(gripper);

        var kind = CreateStyledDropdown(source.Kind, selectedKind =>
        {
            source.Kind = selectedKind;
            RenderEditor(draft);
        });
        DockPanel.SetDock(kind, Dock.Left);
        header.Children.Add(kind);

        var delete = IconButton("\uE74D", "Eliminar fuente", SoftBrush(), MutedBrush(), 26);
        delete.Height = 26;
        delete.Click += (_, _) =>
        {
            module.Sources.Remove(source);
            RenderEditor(draft);
        };
        DockPanel.SetDock(delete, Dock.Right);
        header.Children.Add(delete);
        stack.Children.Add(header);

        var titleBox = Input(source.Title, 30, FontWeights.Bold);
        titleBox.Margin = new Thickness(0, 8, 0, 8);
        titleBox.TextChanged += (_, _) => source.Title = titleBox.Text;
        stack.Children.Add(WithPlaceholder(titleBox, "TITLE..."));

        var locationBox = Input(source.Location, 28, FontWeights.Normal);
        locationBox.FontSize = 11;
        locationBox.TextChanged += (_, _) => source.Location = locationBox.Text;
        stack.Children.Add(WithPlaceholder(locationBox, "resource-url.com"));

        card.Child = stack;

        SetupCardHover(card, SoftBrush(), BorderBrush(), SoftBrush(), PrimaryBrush(), scaleOnHover: false);
        return card;
    }

    private Border AddModuleTile(LearningRoute draft)
    {
        var tile = PanelCard(12, TransparentBrush(), BorderBrush());
        tile.Width = 250;
        tile.Height = 300;
        tile.BorderThickness = new Thickness(0);

        var grid = new Grid();
        var dashArray = new DoubleCollection { 4, 3 };
        var dashRect = new System.Windows.Shapes.Rectangle
        {
            Stroke = new SolidColorBrush(_isDarkMode ? Color.FromRgb(51, 65, 85) : Color.FromRgb(203, 213, 225)),
            StrokeThickness = 1.5,
            StrokeDashArray = dashArray,
            RadiusX = 12,
            RadiusY = 12,
            Margin = new Thickness(-22)
        };
        grid.Children.Add(dashRect);

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(Label("+", 34, FontWeights.Light, MutedBrush(), horizontal: HorizontalAlignment.Center));
        stack.Children.Add(Label("ADD MODULE", 13, FontWeights.Bold, MutedBrush(), horizontal: HorizontalAlignment.Center));
        grid.Children.Add(stack);
        tile.Child = grid;
        tile.Cursor = Cursors.Hand;

        tile.MouseLeftButtonUp += (_, _) =>
        {
            draft.Modules.Add(new LearningModule
            {
                Title = $"Module {draft.Modules.Count + 1}",
                Sources = [new LearningSource { Kind = SourceKind.Link }]
            });
            RenderEditor(draft);
        };

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

    private static (string Glyph, string LabelText, Brush Color) GetSourceKindMetadata(SourceKind kind)
    {
        return kind switch
        {
            SourceKind.Video => ("\uE714", "VIDEO", Brush("#EF4444")),  // Red
            SourceKind.Docs => ("\uE736", "DOCUMENTO", Brush("#3B82F6")), // Blue
            SourceKind.Link => ("\uE71B", "ENLACE WEB", Brush("#10B981")), // Emerald
            SourceKind.File => ("\uE8A5", "ARCHIVO", Brush("#F59E0B")),  // Amber
            _ => ("\uE71B", "RECURSO", Brush("#6366F1"))
        };
    }

    private void RenderDetail(int routeId)
    {
        var route = _routeService.GetRoute(routeId);
        if (route is null)
        {
            RenderDashboard();
            return;
        }

        RenderShell("ROUTE", body =>
        {
            var isBeginner = string.Equals(route.Level, "Beginners", StringComparison.OrdinalIgnoreCase);
            var isAdvanced = string.Equals(route.Level, "Advanced", StringComparison.OrdinalIgnoreCase);

            var bannerGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            if (isBeginner)
            {
                bannerGradient.GradientStops.Add(new GradientStop(Color.FromRgb(14, 165, 233), 0.0));
                bannerGradient.GradientStops.Add(new GradientStop(Color.FromRgb(37, 99, 235), 1.0));
            }
            else if (isAdvanced)
            {
                bannerGradient.GradientStops.Add(new GradientStop(Color.FromRgb(236, 72, 153), 0.0));
                bannerGradient.GradientStops.Add(new GradientStop(Color.FromRgb(124, 58, 237), 1.0));
            }
            else
            {
                bannerGradient.GradientStops.Add(new GradientStop(Color.FromRgb(99, 102, 241), 0.0));
                bannerGradient.GradientStops.Add(new GradientStop(Color.FromRgb(139, 92, 246), 1.0));
            }

            var header = PanelCard(16, bannerGradient, BorderBrush());
            header.Margin = new Thickness(0, 0, 0, 36);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(310) });
            header.Child = grid;

            var left = new StackPanel();
            var back = Button("< DASHBOARD", new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), Brushes.White);
            back.Width = 142;
            back.Click += (_, _) => RenderDashboard();
            left.Children.Add(back);
            
            left.Children.Add(Pill(route.Level.ToUpperInvariant(), Brushes.White, new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)), 0, 20, 0, 8));
            left.Children.Add(Label(route.Title, 36, FontWeights.Bold, Brushes.White));
            left.Children.Add(Label(route.Overview, 15, FontWeights.Medium, new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)), 0, 10, 18, 0));
            grid.Children.Add(left);

            var progress = PanelCard(12, CardBrush(), BorderBrush());
            progress.Width = 290;
            progress.Height = 112;
            progress.HorizontalAlignment = HorizontalAlignment.Right;
            progress.VerticalAlignment = VerticalAlignment.Center;
            
            var progressStack = new StackPanel();
            progressStack.Children.Add(Label("ROUTE COMPLETION", 10, FontWeights.Bold, MutedBrush()));
            progressStack.Children.Add(Label($"{route.Progress}%", 30, FontWeights.ExtraBold, TextBrush(), 0, 4, 0, 6));
            progressStack.Children.Add(ProgressBar(route.Progress));
            progress.Child = progressStack;
            Grid.SetColumn(progress, 1);
            grid.Children.Add(progress);

            var configure = Button("CONFIGURE", new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), Brushes.White);
            configure.Width = 124;
            configure.HorizontalAlignment = HorizontalAlignment.Right;
            configure.VerticalAlignment = VerticalAlignment.Top;
            configure.Click += (_, _) => RenderEditor(CloneRoute(route));
            grid.Children.Add(configure);
            body.Children.Add(header);

            for (var i = 0; i < route.Modules.Count; i++)
            {
                body.Children.Add(ModuleProgressRow(route.Modules[i], i + 1, route.Modules.Count, route.Id));
            }
        });
    }

    private Grid ModuleProgressRow(LearningModule module, int index, int totalModules, int routeId)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 34) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) });
        row.ColumnDefinitions.Add(new ColumnDefinition());

        var leftColGrid = new Grid();
        if (index < totalModules)
        {
            var connector = new Border
            {
                Width = 2,
                Background = _isDarkMode ? Brush("#202E4E") : Brush("#E2E8F0"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 36, 0, -34)
            };
            leftColGrid.Children.Add(connector);
        }

        var isModuleCompleted = module.Sources.Count > 0 && module.Sources.All(s => s.IsCompleted);
        var circle = new Border
        {
            Width = 54,
            Height = 54,
            CornerRadius = new CornerRadius(27),
            Background = isModuleCompleted ? CompletedBrush() : (index == 1 || !isModuleCompleted ? CardBrush() : SoftBrush()),
            BorderBrush = isModuleCompleted ? Brush("#10B981") : (index == 1 ? PrimaryBrush() : BorderBrush()),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 6, 0, 0),
            Effect = new DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 1,
                Color = isModuleCompleted ? Color.FromRgb(16, 185, 129) : Colors.Black,
                Opacity = isModuleCompleted ? 0.2 : 0.05
            }
        };

        var circleContent = isModuleCompleted 
            ? Label("\uE73E", 14, FontWeights.Bold, Brush("#10B981"), horizontal: HorizontalAlignment.Center, vertical: VerticalAlignment.Center)
            : Label(index.ToString("00"), 14, FontWeights.Bold, index == 1 ? PrimaryBrush() : MutedBrush(), horizontal: HorizontalAlignment.Center, vertical: VerticalAlignment.Center);

        if (isModuleCompleted)
        {
            circleContent.FontFamily = new FontFamily("Segoe MDL2 Assets");
        }
        circle.Child = circleContent;
        leftColGrid.Children.Add(circle);
        row.Children.Add(leftColGrid);

        var content = new StackPanel { Margin = new Thickness(18, 8, 0, 0) };
        Grid.SetColumn(content, 1);
        content.Children.Add(Label(module.Title, 22, FontWeights.Bold, TextBrush(), 0, 2, 0, 18));

        var sources = new WrapPanel { ItemWidth = 550, ItemHeight = 90 };
        foreach (var source in module.Sources)
        {
            sources.Children.Add(SourceProgressCard(source, routeId));
        }

        content.Children.Add(sources);
        row.Children.Add(content);
        return row;
    }

    private Border SourceProgressCard(LearningSource source, int routeId)
    {
        var card = PanelCard(12, source.IsCompleted ? CompletedBrush() : CardBrush(), BorderBrush());
        card.Width = 530;
        card.Height = 74;
        card.Margin = new Thickness(0, 0, 20, 14);
        card.Padding = new Thickness(16, 10, 16, 10);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

        var check = new CheckBox
        {
            IsChecked = source.IsCompleted,
            Width = 24,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Cursor = Cursors.Hand
        };
        check.Click += (_, _) =>
        {
            _routeService.ToggleSourceCompletion(source.Id, check.IsChecked == true);
            RenderDetail(routeId);
        };
        grid.Children.Add(check);

        var meta = GetSourceKindMetadata(source.Kind);
        
        var badge = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        var kindIcon = new TextBlock
        {
            Text = meta.Glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 10,
            Foreground = meta.Color,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };
        var kindLabel = Label(meta.LabelText, 9, FontWeights.Bold, meta.Color, vertical: VerticalAlignment.Center);
        badge.Children.Add(kindIcon);
        badge.Children.Add(kindLabel);
        
        var title = Label(source.Title, 15, FontWeights.Bold, source.IsCompleted ? MutedBrush() : TextBrush());
        if (source.IsCompleted)
        {
            title.TextDecorations = TextDecorations.Strikethrough;
            title.Opacity = 0.6;
        }

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(badge);
        text.Children.Add(title);
        
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        var open = IconButton("\uE8A7", "Abrir fuente", SoftBrush(), TextBrush(), 32);
        open.Width = 32;
        open.Height = 32;
        open.Click += (_, _) => OpenSource(source.Location);
        Grid.SetColumn(open, 2);
        grid.Children.Add(open);

        card.Child = grid;

        SetupCardHover(card, source.IsCompleted ? CompletedBrush() : CardBrush(), BorderBrush(), source.IsCompleted ? CompletedBrush() : SoftBrush(), source.IsCompleted ? Brush("#10B981") : PrimaryBrush(), scaleOnHover: false);
        return card;
    }

    private void SaveDraft(LearningRoute draft)
    {
        try
        {
            var routeId = _routeService.SaveRoute(draft);
            RenderDetail(routeId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Pathfinder", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        var theme = IconButton(_isDarkMode ? "\uE706" : "\uE708", _isDarkMode ? "Cambiar a modo claro" : "Cambiar a modo nocturno", CardBrush(), TextBrush(), 42);
        theme.Margin = new Thickness(0, 0, 12, 0);
        theme.Click += (_, _) =>
        {
            _isDarkMode = !_isDarkMode;
            RenderDashboard();
        };
        right.Children.Add(theme);

        var newPath = Button("+ NEW PATH", PrimaryBrush(), Brushes.White);
        newPath.Width = 128;
        newPath.Click += (_, _) => RenderEditor(NewRoute());
        right.Children.Add(newPath);
        DockPanel.SetDock(right, Dock.Right);
        dock.Children.Add(right);

        return header;
    }

    private static LearningRoute NewRoute() =>
        new()
        {
            Title = "",
            Overview = "",
            Audience = "Juniors",
            Level = "Beginners",
            Modules =
            [
                new LearningModule
                {
                    Title = "",
                    Sources =
                    [
                        new LearningSource { Kind = SourceKind.Link }
                    ]
                }
            ]
        };

    private static LearningRoute CloneRoute(LearningRoute route) =>
        new()
        {
            Id = route.Id,
            Title = route.Title,
            Overview = route.Overview,
            Audience = route.Audience,
            Level = route.Level,
            Modules = route.Modules.Select(module => new LearningModule
            {
                Id = module.Id,
                RouteId = module.RouteId,
                Title = module.Title,
                SortOrder = module.SortOrder,
                Sources = module.Sources.Select(source => new LearningSource
                {
                    Id = source.Id,
                    ModuleId = source.ModuleId,
                    Title = source.Title,
                    Location = source.Location,
                    Kind = source.Kind,
                    IsCompleted = source.IsCompleted,
                    SortOrder = source.SortOrder
                }).ToList()
            }).ToList()
        };

    private static void MoveItem<T>(IList<T> items, T item, int targetIndex)
    {
        var oldIndex = items.IndexOf(item);
        if (oldIndex < 0 || targetIndex < 0 || oldIndex == targetIndex)
        {
            return;
        }

        items.RemoveAt(oldIndex);
        if (targetIndex > oldIndex)
        {
            targetIndex--;
        }
        items.Insert(Math.Min(targetIndex, items.Count), item);
    }

    private static void StartModuleDrag(Border card, MouseEventArgs args)
    {
        if (args.LeftButton == MouseButtonState.Pressed && card.Tag is LearningModule module)
        {
            DragDrop.DoDragDrop(card, module, DragDropEffects.Move);
        }
    }

    private static void StartSourceDrag(Border card, MouseEventArgs args)
    {
        if (args.LeftButton == MouseButtonState.Pressed && card.Tag is LearningSource source)
        {
            DragDrop.DoDragDrop(card, source, DragDropEffects.Move);
        }
    }

    private static void OpenSource(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(location) { UseShellExecute = true });
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
