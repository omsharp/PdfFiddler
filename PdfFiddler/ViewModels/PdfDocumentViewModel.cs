using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFtoImage;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Windows.Media.Imaging;
using static PdfSharp.Quality.Testing.TestModel.TestArrayElements;

namespace PdfFiddler.ViewModels
{
    public enum MovePageDirection
    {
        Up, Down
    }

    public partial class PdfDocumentViewModel : ObservableObject
    {
        private PdfDocument document;

        [ObservableProperty]
        private bool isBusy = false;

        [ObservableProperty]
        private int progressPercentage;

        public ObservableCollection<PdfPageViewModel> Pages { get; } = [];

        public PdfDocumentViewModel()
        {
            document = new PdfDocument();
            Debug.WriteLine("Document View Model Created .....");
        }

        private bool CanAddPages() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanAddPages), IncludeCancelCommand = true)]
        private async Task AddPagesFromPdfFileAsync(CancellationToken cancellationToken)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "PDF Files|*.pdf",
                Title = "Select a PDF file"
            };

            if (fileDialog.ShowDialog() == false) return;

            IsBusy = true;

            try
            {
                ProgressPercentage = 0;

                var pdfBytes = await File.ReadAllBytesAsync(fileDialog.FileName);

                using var pdfStream = new MemoryStream(pdfBytes);
                using var pdfSource = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);

                var pagesCount = pdfSource.PageCount;

                IProgress<int> progress = new Progress<int>(done =>
                {
                    ProgressPercentage = done * 100 / pagesCount;
                });

                var thumbnails = await Task.Run(() =>
                {
                    var images = new List<BitmapSource>();
                    var skBitmaps = Conversion.ToImages(pdfBytes, options: new RenderOptions(Dpi: 72));
                    foreach (var bitmap in skBitmaps)
                    {
                        images.Add(ToBitmapSource(bitmap));
                        progress.Report(images.Count);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    return images;
                }, cancellationToken);

                for (var i = 0; i < pdfSource.PageCount; i++)
                {
                    var pageNumber = Pages.Count + 1;
                    var page = document.Pages.Add(pdfSource.Pages[i]);
                    Pages.Add(new PdfPageViewModel(page, pageNumber, thumbnails[i]));
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: show indication in UI
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddPages), IncludeCancelCommand = true)]
        private async Task AddPagesFromImagesAsync(CancellationToken cancellationToken)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg|*.jpeg|*.png|*.bmp",
                Title = "Select images",
                Multiselect = true
            };

            if (fileDialog.ShowDialog() == false) return;

            IsBusy = true;

            try
            {
                foreach (var imageFile in fileDialog.FileNames)
                {
                    using var image = XImage.FromFile(imageFile);
                    var page = document.AddPage();
                    // handle with and height
                    using var gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);

                    //TODO: Pages.Add(new PdfPageViewModel(page));
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: show indication in UI
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void RemovePageAt(int index)
        {
            document.Pages.RemoveAt(index);
            Pages.RemoveAt(index);
        }

        [RelayCommand]
        private void MovePage(int index)
        {
            throw new NotImplementedException();
        }

        [RelayCommand]
        private void SaveDocumentAs(string fileName)
        {
            document.Save(fileName);
        }

        [RelayCommand]
        private void Reset(string fileName)
        {
            Pages.Clear();
            document.Dispose();
            document = new PdfDocument();
        }

        private static BitmapSource ToBitmapSource(SKBitmap source)
        {
            using (source)
            {
                using var skData = source.Encode(SKEncodedImageFormat.Png, 100);
                using var pngStream = skData.AsStream();
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = pngStream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
            }
        }
    }
}
