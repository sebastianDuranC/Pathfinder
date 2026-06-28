using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Pathfinder.Domain.Entities;

namespace Pathfinder;

public partial class MainWindow : Window
{
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
                Title = $"",
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
}
