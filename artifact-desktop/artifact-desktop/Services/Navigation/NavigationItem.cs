using MaterialDesignThemes.Wpf;

namespace artifact.desktop.Services.Navigation
{
    public sealed record NavigationItem(string Label, PackIconKind Icon, Type ViewType);
}

