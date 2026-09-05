# Specification: Slice 1 - Core OCR & Text Transformation Engine

## 1. Overview
Port the core headless OCR and text transformation engines from Text-Grab 4.15.0 into Sol:
- WinRT OCR (`Windows.Media.Ocr.OcrEngine`)
- Windows AI Recognition (`Microsoft.Windows.AI.Imaging.TextRecognizer`) with graceful fallback
- 40+ string transformation methods and error corrections from `StringMethods.cs`
- Headless architecture with zero WPF dependencies, using `SoftwareBitmap` and standard .NET types
- Fully integrated with Sol's DI container (`App.xaml.cs`) and localization (`Strings.S.*`)

## 2. Component Architecture
- **Contracts / Enums / Models**:
  - `Sol.Models.OcrEnums`: `OcrEngineKind`, `OcrOutputKind`, `CurrentCase`, `SpotInLine`
  - `Sol.Models.OcrResult`: `OcrWord`, `OcrLine`, `OcrResult`, `OcrOutput`
  - `Sol.Models.OcrLanguageInfo`: Language descriptor with display name, tag, engine, and space-joining flags
- **Services**:
  - `IOcrService` / `OcrService`: WinRT OCR + WinAI OCR engine abstraction, ideal scaling, CJK furigana filtering, table reconstruction from bounding boxes
  - `ITextTransformService` / `TextTransformService`: Case toggles, line operations (single line, deduplication, shuffling, prefix/suffix insertion), OCR character error corrections (number/letter confusion, Greek/Cyrillic to Latin, GUID formatting), regex pattern matching and extraction, column unstacking
- **DI & Localization**:
  - Registered in `App.xaml.cs` as singletons
  - Zero hardcoded strings: all diagnostic and user-facing messages via `Strings.S.*`

## 3. Exit Gates
- Phase 1: WinAI & WinRT package/API spike verifies compilation and basic OCR engine creation.
- Phase 2: Complete models, interfaces, and services implemented.
- Phase 3: DI registration and Strings localization wired up.
- Phase 4: Full xUnit test suite passing with exit code 0 (`dotnet test`), zero compiler warnings, no memory leaks.
