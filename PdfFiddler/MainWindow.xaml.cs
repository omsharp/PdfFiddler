using Microsoft.Win32;
using PdfFiddler.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PdfFiddler;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new PdfDocumentViewModel();
    }

    private void zoomInButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var image in FindVisualChildren<Image>(pagesPreviewListBox))
        {
            if (image.Name == "previewImage" && image.Width < 800)
            {
                image.Width += 50;
                image.Height += 50;
            }
        }
    }

    private void zoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var image in FindVisualChildren<Image>(pagesPreviewListBox))
        {
            if (image.Name == "previewImage" && image.Width > 100)
            {
                image.Width -= 50;
                image.Height -= 50;
            }
        }
    }

    private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                if (child != null && child is T)
                    yield return (T)child;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}