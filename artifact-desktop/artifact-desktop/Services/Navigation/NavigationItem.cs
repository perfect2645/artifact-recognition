using MaterialDesignThemes.Wpf;

namespace artifact.desktop.Services.Navigation
{
    public record NavigationItem(string Label, PackIconKind Icon, Type ViewModelType);
}

