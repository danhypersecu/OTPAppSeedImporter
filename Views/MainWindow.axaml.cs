using Avalonia.Controls;
using OTPAppSeedImporter.ViewModels;

namespace OTPAppSeedImporter.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
} 