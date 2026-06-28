using System;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Pathfinder.Domain.Entities;

namespace Pathfinder;

public partial class MainWindow : Window
{
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
        _currentRouteId = routeId;

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

            var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(Pill(route.Level.ToUpperInvariant(), Brushes.White, new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)), 0, 0, 0, 8));
            left.Children.Add(Label(route.Title, 36, FontWeights.Bold, Brushes.White));
            left.Children.Add(Label(route.Overview, 15, FontWeights.Medium, new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)), 0, 10, 18, 0));
            grid.Children.Add(left);

            var rightPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 290
            };

            var buttonsWrapper = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var backBtn = new Button
            {
                Height = 28,
                Width = 158,
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Template = CreateButtonTemplate(new CornerRadius(14))
            };
            var backStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            backStack.Children.Add(new TextBlock { Text = "\uE72B", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            backStack.Children.Add(Label("BACK TO DASHBOARD", 10, FontWeights.Bold, Brushes.White, vertical: VerticalAlignment.Center));
            backBtn.Content = backStack;
            backBtn.Click += (_, _) => RenderDashboard();
            backBtn.MouseEnter += (s, e) => backBtn.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            backBtn.MouseLeave += (s, e) => backBtn.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            buttonsWrapper.Children.Add(backBtn);

            var configBtn = new Button
            {
                Height = 28,
                Width = 120,
                Margin = new Thickness(12, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Template = CreateButtonTemplate(new CornerRadius(14))
            };
            var configStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            configStack.Children.Add(new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            configStack.Children.Add(Label("CONFIGURE", 10, FontWeights.Bold, Brushes.White, vertical: VerticalAlignment.Center));
            configBtn.Content = configStack;
            configBtn.Click += (_, _) => RenderEditor(CloneRoute(route));
            configBtn.MouseEnter += (s, e) => configBtn.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            configBtn.MouseLeave += (s, e) => configBtn.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            buttonsWrapper.Children.Add(configBtn);

            rightPanel.Children.Add(buttonsWrapper);

            var progressCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Height = 124,
                Width = 290
            };

            var progressStack = new StackPanel();
            progressStack.Children.Add(Label("GENERAL PROGRESS", 10, FontWeights.Bold, new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))));
            progressStack.Children.Add(Label($"{route.Progress}%", 30, FontWeights.ExtraBold, Brushes.White, 0, 4, 0, 4));

            var track = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Height = 6,
                VerticalAlignment = VerticalAlignment.Center
            };
            var progressBarGrid = new Grid { Height = 12, Margin = new Thickness(0, 4, 0, 4) };
            progressBarGrid.Children.Add(track);

            var indicatorWrapper = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            var indicator = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(3),
                Height = 6,
                Width = 0
            };
            indicatorWrapper.Children.Add(indicator);
            progressBarGrid.Children.Add(indicatorWrapper);

            var totalWidth = 250.0;
            var targetWidth = totalWidth * (route.Progress / 100.0);
            var widthAnim = new DoubleAnimation
            {
                From = 0.0,
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            indicator.BeginAnimation(FrameworkElement.WidthProperty, widthAnim);
            progressStack.Children.Add(progressBarGrid);

            var totalSources = route.Modules.Sum(m => m.Sources.Count);
            var completedSources = route.Modules.Sum(m => m.Sources.Count(s => s.IsCompleted));
            progressStack.Children.Add(Label($"{completedSources} of {totalSources} topics completed", 11, FontWeights.Medium, new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)), 0, 4, 0, 0));

            progressCard.Child = progressStack;
            rightPanel.Children.Add(progressCard);

            Grid.SetColumn(rightPanel, 1);
            grid.Children.Add(rightPanel);

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

    private static void OpenSource(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(location) { UseShellExecute = true });
    }
}
