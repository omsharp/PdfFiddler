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
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static PdfSharp.Pdf.PdfDictionary;
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
            ProgressPercentage = 0;

            try
            {
                var pdfBytes = await File.ReadAllBytesAsync(fileDialog.FileName);
                using var pdfStream = new MemoryStream(pdfBytes);
                using var pdfSource = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);
                var pagesCount = pdfSource.PageCount;

                IProgress<int> progress = new Progress<int>(done =>
                {
                    ProgressPercentage = done * 100 / pagesCount;
                });

                // The heavy work is done in a separate thread.
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

                // Cheap work done on the same UI thread
                for (var i = 0; i < pdfSource.PageCount; i++)
                {
                    var page = document.Pages.Add(pdfSource.Pages[i]);
                    Pages.Add(new PdfPageViewModel(page, Pages.Count + 1, thumbnails[i]));
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: show indication in UI
            }
            finally
            {
                IsBusy = false;
                ProgressPercentage = 0;
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddPages), IncludeCancelCommand = true)]
        private async Task AddPagesFromImagesAsync(CancellationToken cancellationToken)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Select images",
                Multiselect = true
            };

            if (fileDialog.ShowDialog() == false) return;

            IsBusy = true;
            ProgressPercentage = 0;

            try
            {

                IProgress<int> progress = new Progress<int>(done =>
                {
                    ProgressPercentage = done * 100 / fileDialog.FileNames.Length;
                });

                // The heavy work is done in a separate thread.
                var thumbnails = await Task.Run(() =>
                {
                    var images = new List<BitmapSource>();
                    for(var i =0; i<fileDialog.FileNames.Length; i++)
                    {
                        var imageFile = fileDialog.FileNames[i];
                        using var imageStream = new FileStream(imageFile, FileMode.Open);
                        using var image = XImage.FromStream(imageStream);
                        imageStream.Position = 0;
                        var thumbnail = BitmapFrame.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        images.Add(thumbnail);
                        progress.Report(i);
                    }

                    return images;
                }, cancellationToken);


                // Cheap work done on the same UI thread
                for (var i = 0; i < fileDialog.FileNames.Count(); i++)
                {
                    using var image = XImage.FromFile(fileDialog.FileNames[i]);
                    
                    var page = document.AddPage();
                    page.Width = XUnit.FromPoint(image.PointWidth);
                    page.Height = XUnit.FromPoint(image.PointHeight);

                    using var gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);

                    Pages.Add(new PdfPageViewModel(page, Pages.Count + 1, thumbnails[i]));
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: show indication in UI
            }
            finally
            {
                IsBusy = false;
                ProgressPercentage = 0;
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
        private void SaveDocumentAs()
        {


            var saveFileDialog = new SaveFileDialog()
            {
                Title = "Save PDF File",
                Filter = "PDF Files|*.pdf",
                DefaultExt = "pdf",
                FileName = "untitled"
            };

            if (saveFileDialog.ShowDialog() == false) return;

            IsBusy = true;

            // we can not keep using the same document object after it was save
            // we have to reload the saved file and create a new document object
            // and we have to populate the Pages view model list with the new PdfPage objects
            document.Save(saveFileDialog.FileName);
            using var pdfSource = PdfReader.Open(saveFileDialog.FileName, PdfDocumentOpenMode.Import);
            document.Dispose();
            document = new PdfDocument();

            for (var i = 0; i < pdfSource.Pages.Count; i++)
            {
                var pdfPage = document.AddPage(pdfSource.Pages[i]);
                Pages[i].PdfPage = pdfPage;
            }

            IsBusy = false;
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
