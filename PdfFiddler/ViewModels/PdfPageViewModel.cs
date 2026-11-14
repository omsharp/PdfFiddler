using CommunityToolkit.Mvvm.ComponentModel;
using PdfSharp.Pdf;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
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
