CREATE TABLE [sales].[Customers]
(
    [CustomerId] int IDENTITY(1,1) NOT NULL,
    [CustomerNumber] varchar(30) NOT NULL,
    [DisplayName] nvarchar(200) NOT NULL,
    [EmailAddress] varchar(320) NULL,
    [CreditLimit] decimal(18, 2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedUtc] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
);

CREATE TABLE [sales].[Addresses]
(
    [AddressId] int IDENTITY(1,1) NOT NULL,
    [CustomerId] int NOT NULL,
    [Line1] nvarchar(200) NOT NULL,
    [Line2] nvarchar(200) NULL,
    [City] nvarchar(120) NOT NULL,
    [StateCode] char(2) NOT NULL,
    [PostalCode] varchar(20) NOT NULL,
    [CountryCode] char(2) NOT NULL,
    CONSTRAINT [PK_Addresses] PRIMARY KEY ([AddressId]),
    CONSTRAINT [FK_Addresses_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [sales].[Customers] ([CustomerId])
);

CREATE TABLE [catalog].[Categories]
(
    [CategoryId] int IDENTITY(1,1) NOT NULL,
    [ParentCategoryId] int NULL,
    [Name] nvarchar(120) NOT NULL,
    [SortOrder] smallint NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [FK_Categories_ParentCategory] FOREIGN KEY ([ParentCategoryId]) REFERENCES [catalog].[Categories] ([CategoryId])
);

CREATE TABLE [catalog].[Products]
(
    [ProductId] bigint IDENTITY(1,1) NOT NULL,
    [CategoryId] int NOT NULL,
    [Sku] varchar(40) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [UnitPrice] money NOT NULL,
    [Weight] numeric(10, 3) NULL,
    [Thumbnail] varbinary(max) NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId]),
    CONSTRAINT [FK_Products_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [catalog].[Categories] ([CategoryId])
);

CREATE TABLE [sales].[Orders]
(
    [OrderId] int IDENTITY(1,1) NOT NULL,
    [CustomerId] int NOT NULL CONSTRAINT [FK_Orders_Customers] REFERENCES [sales].[Customers] ([CustomerId]),
    [BillingAddressId] int NULL,
    [ShippingAddressId] int NOT NULL,
    [OrderNumber] varchar(40) NOT NULL,
    [OrderedUtc] datetime2(7) NOT NULL,
    [RequiredByDate] date NULL,
    [Subtotal] decimal(18, 2) NOT NULL,
    [TaxAmount] decimal(18, 2) NOT NULL,
    [ShippingAmount] decimal(18, 2) NOT NULL,
    [IsPaid] bit NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderId]),
    CONSTRAINT [FK_Orders_BillingAddress] FOREIGN KEY ([BillingAddressId]) REFERENCES [sales].[Addresses] ([AddressId]),
    CONSTRAINT [FK_Orders_ShippingAddress] FOREIGN KEY ([ShippingAddressId]) REFERENCES [sales].[Addresses] ([AddressId])
);

CREATE TABLE [sales].[OrderLines]
(
    [OrderLineId] int IDENTITY(1,1) NOT NULL,
    [OrderId] int NOT NULL,
    [ProductId] bigint NOT NULL,
    [Quantity] smallint NOT NULL,
    [UnitPrice] decimal(18, 2) NOT NULL,
    [DiscountAmount] decimal(18, 2) NULL,
    CONSTRAINT [PK_OrderLines] PRIMARY KEY ([OrderLineId]),
    CONSTRAINT [FK_OrderLines_Orders] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([OrderId]),
    CONSTRAINT [FK_OrderLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [catalog].[Products] ([ProductId])
);

CREATE TABLE [audit].[OrderEvents]
(
    [OrderEventId] uniqueidentifier NOT NULL,
    [OrderId] int NOT NULL,
    [EventType] varchar(50) NOT NULL,
    [EventUtc] datetime2(7) NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [Message] nvarchar(500) NULL,
    CONSTRAINT [PK_OrderEvents] PRIMARY KEY ([OrderEventId]),
    CONSTRAINT [FK_OrderEvents_Orders] FOREIGN KEY ([OrderId]) REFERENCES [sales].[Orders] ([OrderId])
);
