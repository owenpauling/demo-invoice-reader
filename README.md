# demo-invoice-reader

A .NET 10 console app that extracts structured data from invoice files using the [Azure AI Document Intelligence](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/overview) prebuilt invoice model. Optionally runs a second OCR pass to fuzzy-search for a handwritten word or phrase.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Azure AI Document Intelligence resource ([create one](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/create-document-intelligence-resource))

## Configuration

Create `appsettings.local.json` in the project root with your resource endpoint and API key:

```json
{
  "DocumentIntelligence": {
    "Endpoint": "https://<your-resource>.cognitiveservices.azure.com/",
    "ApiKey": "<your-api-key>"
  }
}
```

This file is gitignored and overrides the placeholder values in `appsettings.json`.

## Usage

```bash
dotnet run -- path/to/invoice.pdf
```

The extracted data is written to `<filename>_result.json` in the current working directory.

### Phrase search

Pass `--search` with a word or phrase to run a second OCR pass using the `prebuilt-read` model and fuzzy-search the full text for that phrase:

```bash
dotnet run -- path/to/invoice.pdf --search "approved"
dotnet run -- path/to/invoice.pdf --search "approved by finance"
```

The result is printed to the console alongside the normal invoice output:

```
Searching for: "approved"
Found (match score: 92/100)
```

```
Searching for: "approved"
Not found (best score: 43/100, threshold: 80)
```

#### How matching works

1. **OCR pass** — the file is sent to the `prebuilt-read` model, which analyses the document and tags each region of text as either printed or handwritten.
2. **Handwriting filter** — only the spans the model identified as handwritten are extracted from the result. Printed text (invoice fields, product descriptions, etc.) is discarded entirely, so the search cannot match against it.
3. **Normalisation** — both the handwritten text and the search phrase are lowercased and have whitespace collapsed, so spacing inconsistencies from the OCR don't affect the result.
4. **Sliding window** — a window the same character length as the phrase is slid across the handwritten text one character at a time.
5. **Levenshtein distance** — at each window position, the [edit distance](https://en.wikipedia.org/wiki/Levenshtein_distance) between the phrase and that window is computed. Edit distance counts the minimum number of single-character insertions, deletions, or substitutions needed to turn one string into the other.
6. **Scoring** — the distance is converted to a 0–100 score: `score = (1 - distance / windowLength) × 100`. The best score across all window positions is kept.
7. **Threshold** — a score of 80 or above is reported as found. This tolerates roughly one wrong character per five, which covers typical OCR errors on handwriting (e.g. `"approvad"` → 92, `"appr0ved"` → 89).

## Output

All fields from the prebuilt invoice model are mapped, with undetected fields omitted. Example:

```json
{
  "invoiceId": "INV-001",
  "invoiceDate": "2024-01-15T00:00:00+00:00",
  "vendorName": "Acme Corp",
  "vendorAddress": "123 Main St, Seattle, WA, 98101, US",
  "invoiceTotal": 110.00,
  "items": [
    {
      "description": "Widget",
      "quantity": 2,
      "unitPrice": 50.00,
      "amount": 100.00
    }
  ]
}
```

Supported fields include vendor/customer details, billing/shipping/service addresses, line items, payment details, and tax details.
