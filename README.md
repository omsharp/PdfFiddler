# PDF Fiddler

PDF Fiddler is a lightweight Windows desktop application for assembling and adjusting PDF documents. It is built with WPF and .NET, and lets you import pages from existing PDF files or image files, reorder and rotate pages, remove selected pages, and save the result as a new PDF.

## Features

- Add pages from a PDF file
- Add pages from image files
- Select multiple pages for bulk actions
- Remove selected pages
- Rotate selected pages
- Move selected pages up or down in the document order
- Clear selection and reset the current document
- Export the edited result to a new PDF file
- Thumbnail preview for each page

## Technologies

- .NET 10 (Windows targeting)
- WPF
- CommunityToolkit.Mvvm
- PDFsharp
- PDFtoImage

## Requirements

- Windows 10 or later
- .NET 10 SDK or Visual Studio 2022 with .NET desktop development workload

## Getting started

### Clone the repository

```bash
git clone https://github.com/omsharp/PdfFiddler.git
cd PdfFiddler
```

### Build

```bash
dotnet build PdfFiddler.sln
```

### Run

```bash
dotnet run --project PdfFiddler/PdfFiddler.csproj
```

Or open `PdfFiddler.sln` in Visual Studio and run the project with F5.

## Project structure

```text
PdfFiddler/
├── PdfFiddler.sln
├── PdfFiddler/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── PdfFiddler.csproj
│   ├── ViewModels/
│   ├── Views/
│   ├── Converters/
│   └── app.ico
└── README.md
```

## Typical workflow

1. Open the app.
2. Add a PDF file or a set of image files.
3. Select one or more thumbnails in the document view.
4. Use the toolbar actions to rotate, remove, or reorder pages.
5. Save the result as a new PDF file.

## Notes

This project is a desktop utility intended for simple PDF page manipulation and is optimized for quick editing workflows rather than full-featured PDF editing suites.
