# Task: Slice 1 - Core OCR & Text Transformation Engine
Status: Ready for Review
Current Subagent: Implementer

## Target Files
- `src/Sol/Sol.csproj`
- `src/Sol/Models/OcrEnums.cs`
- `src/Sol/Models/OcrResult.cs`
- `src/Sol/Models/OcrLanguageInfo.cs`
- `src/Sol/Services/IOcrService.cs`
- `src/Sol/Services/OcrService.cs`
- `src/Sol/Services/ITextTransformService.cs`
- `src/Sol/Services/TextTransformService.cs`
- `src/Sol/App.xaml.cs`
- `src/Sol/Helpers/Strings.cs`
- `src/Sol.Tests/TextTransformServiceTests.cs`
- `src/Sol.Tests/OcrServiceTests.cs`

## Checklist
- [x] Phase 1: Spike & Win32/API validation (Exit gate: WinAI & WinRT package restoration and API spike compiling)
- [x] Phase 2: Interface contracts & models (Exit gate: OcrEnums, OcrResult, OcrLanguageInfo, IOcrService, ITextTransformService implemented)
- [x] Phase 3: Core services implementation, DI & localization (Exit gate: OcrService and TextTransformService implemented, registered in App.xaml.cs, Strings.cs updated, build passes)
- [x] Phase 4: Verification & leak defense (Exit gate: comprehensive xUnit test suite passing, `dotnet test` exits 0, no memory leaks)
