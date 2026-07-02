CREATE TABLE [billing].[Invoices]
(
    [InvoiceId] uniqueidentifier NOT NULL,
    [InvoiceNumber] varchar(40) NOT NULL,
    [CustomerName] nvarchar(250) NOT NULL,
    [Subtotal] decimal(18, 2) NOT NULL,
    [TaxAmount] numeric(18, 2) NULL,
    [Notes] nvarchar(max) NULL,
    [PdfBytes] varbinary(max) NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([InvoiceId])
);
