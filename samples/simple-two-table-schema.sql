CREATE TABLE [dbo].[Customers]
(
    [CustomerId] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] varchar(320) NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
);

CREATE TABLE [dbo].[Orders]
(
    [OrderId] int IDENTITY(1,1) NOT NULL,
    [CustomerId] int NOT NULL,
    [OrderedAt] datetime2(7) NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderId]),
    CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([CustomerId])
);
