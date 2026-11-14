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
        private PdfPage page;

        [ObservableProperty]
        private BitmapSource thumbnail;

        [ObservableProperty]
        private int pageNumber;

        public PdfPageViewModel(PdfPage page, int pageNumber, BitmapSource thumbnail)
        {
            this.page = page;
            this.pageNumber = pageNumber;
            this.thumbnail = thumbnail;
        }

    }
}
