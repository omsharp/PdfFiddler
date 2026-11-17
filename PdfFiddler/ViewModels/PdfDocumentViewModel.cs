using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFtoImage;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PdfFiddler.ViewModels
{
    public partial class PdfDocumentViewModel : ObservableObject
    {
        private PdfDocument document;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddPagesFromPdfFileCommand))]
        [NotifyCanExecuteChangedFor(nameof(AddPagesFromImagesCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearSelectionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedPagesUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedPagesDownCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedPagesCommand))]
        [NotifyCanExecuteChangedFor(nameof(RotateSelectedPagesCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveDocumentAsCommand))]
        private bool isBusy = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearSelectionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedPagesUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(MoveSelectedPagesDownCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveSelectedPagesCommand))]
        [NotifyCanExecuteChangedFor(nameof(RotateSelectedPagesCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveDocumentAsCommand))]
        private bool isEmpty;

        [ObservableProperty]
        private int progressPercentage;

        public ObservableCollection<PdfPageViewModel> Pages { get; } = [];

        public PdfDocumentViewModel()
        {
            document = new PdfDocument();
            IsEmpty = true;
            Pages.CollectionChanged += OnCollectionChanged;
        }


        private bool CanExecuteCommand() => !IsBusy;
        private bool CanClearSelection() => CanExecuteCommand() && Pages.Any(p => p.IsSelected);
        private bool CanRotateSelectedPages() => CanExecuteCommand() && Pages.Any(p => p.IsSelected);
        private bool CanRemoveSelectedPages() => CanExecuteCommand() && Pages.Any(p => p.IsSelected);
        private bool CanReset() => CanExecuteCommand() && Pages.Count != 0;
        private bool CanSaveDocumentAs() => CanExecuteCommand() && Pages.Count != 0;


        private bool CanMoveSelectedPagesUp()
        {
            var firstSelection = Pages.FirstOrDefault(p => p.IsSelected);
            return CanExecuteCommand() && Pages.Count > 0 && firstSelection != null && firstSelection.PageNumber > 1;
        }

        private bool CanMoveSelectedPagesDown()
        {
            var lastSelection = Pages.LastOrDefault(p => p.IsSelected);
            return CanExecuteCommand() && Pages.Count > 0 && lastSelection != null && lastSelection.PageNumber < Pages.Count;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteCommand), IncludeCancelCommand = true)]
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
                    var skBitmaps = Conversion.ToImages(pdfBytes, options: new PDFtoImage.RenderOptions(Dpi: 72));
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

        [RelayCommand(CanExecute = nameof(CanExecuteCommand), IncludeCancelCommand = true)]
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
                    for (var i = 0; i < fileDialog.FileNames.Length; i++)
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

        [RelayCommand(CanExecute = nameof(CanRemoveSelectedPages))]
        private void RemoveSelectedPages()
        {
            IsBusy = true;

            var index = 0;
            while (index < Pages.Count)
            {
                if (Pages[index].IsSelected)
                {
                    document.Pages.RemoveAt(index);
                    Pages.RemoveAt(index);
                }
                else
                {
                    Pages[index].PageNumber = index + 1;
                    index++;
                }
            }

            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(CanRotateSelectedPages))]
        private void RotateSelectedPages()
        {
            IsBusy = true;

            for (var i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                if (page.IsSelected)
                {
                    document.Pages[i].Rotate = 90;

                    // rotate the thumbnail bitmap
                    var newBitmap = new TransformedBitmap();
                    newBitmap.BeginInit();
                    newBitmap.Source = page.Thumbnail;
                    newBitmap.Transform = new RotateTransform(90);
                    newBitmap.EndInit();
                    page.Thumbnail = newBitmap;
                }
            }

            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(CanMoveSelectedPagesUp))]
        private void MoveSelectedPagesUp()
        {
            IsBusy = true;

            var pagesToMove = Pages.Where(p => p.IsSelected).ToList();
            if (pagesToMove.Count > 0)
            {
                var indicesToMove = pagesToMove.Select(p => p.PageNumber - 1).ToList();
                var moveToIndex = indicesToMove[0] - 1;

                if (moveToIndex < 0)
                {
                    IsBusy = false;
                    return;
                }

                for (var i = 0; i < indicesToMove.Count; i++)
                {
                    document.Pages.MovePage(indicesToMove[i], moveToIndex);
                    Pages.Move(indicesToMove[i], moveToIndex);
                    moveToIndex++;
                }

                // renumber pages
                for (var i = 0; i < Pages.Count; i++)
                {
                    Pages[i].PageNumber = i + 1;
                }
            }

            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(CanMoveSelectedPagesDown))]
        private void MoveSelectedPagesDown()
        {
            IsBusy = true;

            var pagesToMove = Pages.Where(p => p.IsSelected).ToList();
            if (pagesToMove.Count > 0)
            {
                var indicesToMove = pagesToMove.Select(p => p.PageNumber - 1).ToList();
                var moveToIndex = indicesToMove.Last() + 1;

                if (moveToIndex >= Pages.Count)
                {
                    IsBusy = false;
                    return;
                }

                for (var i = indicesToMove.Count - 1; i >= 0; i--)
                {
                    document.Pages.MovePage(indicesToMove[i], moveToIndex);
                    Pages.Move(indicesToMove[i], moveToIndex);
                    moveToIndex--;
                }

                // renumber pages
                for (var i = 0; i < Pages.Count; i++)
                {
                    Pages[i].PageNumber = i + 1;
                }
            }

            IsBusy = false;
        }


        [RelayCommand(CanExecute = nameof(CanClearSelection))]
        private void ClearSelection()
        {
            var selected = Pages.Where(p => p.IsSelected);
            foreach (var page in selected)
            {
                page.IsSelected = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanReset))]
        private void Reset(string fileName)
        {
            Pages.Clear();
            document.Dispose();
            document = new PdfDocument();
            IsEmpty = true;
        }

        [RelayCommand(CanExecute = nameof(CanSaveDocumentAs))]
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

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {

            IsEmpty = Pages.Count < 1;

            if (e.NewItems != null)
            {
                foreach (PdfPageViewModel item in e.NewItems)
                {
                    item.PropertyChanged += PdfPageViewModelPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (PdfPageViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= PdfPageViewModelPropertyChanged;
                }
            }
        }

        private void PdfPageViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PdfPageViewModel.IsSelected))
            {
                MoveSelectedPagesUpCommand.NotifyCanExecuteChanged();
                MoveSelectedPagesDownCommand.NotifyCanExecuteChanged();
                ClearSelectionCommand.NotifyCanExecuteChanged();
                RotateSelectedPagesCommand.NotifyCanExecuteChanged();
                RemoveSelectedPagesCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
