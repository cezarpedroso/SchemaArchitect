CREATE TABLE [catalog].[Products]
(
    [ProductId] bigint IDENTITY(1,1) NOT NULL,
    [Sku] varchar(40) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId])
);

CREATE TABLE [sales].[OrderLines]
(
    [OrderLineId] int IDENTITY(1,1) NOT NULL,
    [OrderId] int NOT NULL,
    [ProductId] bigint NOT NULL,
    [Quantity] smallint NOT NULL,
    CONSTRAINT [PK_OrderLines] PRIMARY KEY ([OrderLineId]),
    CONSTRAINT [FK_OrderLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([ProductId])
);
