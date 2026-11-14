using PdfSharp;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PdfFiddler;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        PdfSharpLicense.SetAgplLicense();
    }
}