# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build
dotnet run -- path\to\invoice.pdf
```

The output JSON is written to `<input-filename>_result.json` in the current working directory.

## Configuration

`appsettings.json` holds the Azure Document Intelligence endpoint and API key under `DocumentIntelligence:Endpoint` and `DocumentIntelligence:ApiKey`. This file is copied to the output directory on every build. Real credentials go directly in `appsettings.json` (which is gitignored via `appsettings.local.json` pattern — if you add a local override file, add it to `.gitignore`).

## Architecture

Everything lives in `Program.cs` as top-level statements. The flow is linear: validate args → load config → call Azure → map result → write JSON.

**Field extraction** uses four local helper functions (`GetString`, `GetCurrency`, `GetDate`, `GetAddress`) that all follow the same pattern: `TryGetValue` on the `DocumentField` dictionary, then read the appropriate typed property (`ValueString`, `ValueCurrency?.Amount`, `ValueDate`, `ValueAddress`). Nested array fields (`Items`, `PaymentDetails`, `TaxDetails`) each have their own helper that iterates `ValueList` and maps each entry via `ValueDictionary`.

**DTOs** are positional C# records at the bottom of the file (`InvoiceOutput`, `InvoiceLineItem`, `InvoicePaymentDetail`, `InvoiceTaxDetail`). All fields are nullable — the JSON serializer is configured with `WhenWritingNull` so absent fields are omitted from output.

**SDK note:** This uses `Azure.AI.DocumentIntelligence` 1.0.0 (GA, Dec 2024). Key naming differences from older beta versions: `AnalyzeDocumentOptions` (not `AnalyzeDocumentContent`), `BytesSource` (not `Base64Source`), `ValueDictionary` (not `ValueObject`), `UriSource` (not `UrlSource`).
