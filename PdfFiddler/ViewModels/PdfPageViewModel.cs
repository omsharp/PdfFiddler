using CommunityToolkit.Mvvm.ComponentModel;
using PdfSharp.Pdf;
using System.Windows.Media.Imaging;

namespace PdfFiddler.ViewModels
{
    public partial class PdfPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private PdfPage pdfPage;

        [ObservableProperty]
        private BitmapSource thumbnail;

        [ObservableProperty]
        private int pageNumber;

        [ObservableProperty]
        private bool isSelected;

        public PdfPageViewModel(PdfPage pdfPage, int pageNumber, BitmapSource thumbnail)
        {
            PdfPage = pdfPage;
            PageNumber = pageNumber;
            Thumbnail = thumbnail;
            isSelected= false;
        }
    }
}
