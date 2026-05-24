# demo-invoice-reader

A .NET 10 console app that extracts structured data from invoice files using the [Azure AI Document Intelligence](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/overview) prebuilt invoice model.

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
