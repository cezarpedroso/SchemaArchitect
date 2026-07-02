-- Sample: E-Commerce Schema (medium complexity)
-- Purpose: demonstrates customers, products, orders, order items, payments, addresses,
--          with constraints, composite keys, unique constraints, indexes, and soft-delete
-- Dialect: Generic SQL (works with SQL Server / PostgreSQL with minor tweaks)

CREATE TABLE Customers (
	CustomerId BIGINT PRIMARY KEY,
	Username VARCHAR(100) NOT NULL UNIQUE,
	Email VARCHAR(320) NOT NULL UNIQUE,
	FullName VARCHAR(200) NOT NULL,
	CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	IsDeleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE CustomerAddresses (
	AddressId BIGINT PRIMARY KEY,
	CustomerId BIGINT NOT NULL REFERENCES Customers(CustomerId) ON DELETE CASCADE,
	Label VARCHAR(60) NOT NULL, -- e.g. 'Home', 'Office'
	Line1 VARCHAR(200) NOT NULL,
	Line2 VARCHAR(200),
	City VARCHAR(100) NOT NULL,
	Region VARCHAR(100),
	PostalCode VARCHAR(30),
	CountryCode CHAR(2) NOT NULL,
	IsPrimary BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IX_CustomerAddresses_Customer_IsPrimary ON CustomerAddresses(CustomerId, IsPrimary);

CREATE TABLE Products (
	ProductId BIGINT PRIMARY KEY,
	SKU VARCHAR(64) NOT NULL UNIQUE,
	Name VARCHAR(300) NOT NULL,
	Description TEXT,
	Price DECIMAL(12,2) NOT NULL CHECK (Price >= 0),
	AvailableFrom DATE,
	IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE ProductTags (
	ProductId BIGINT NOT NULL REFERENCES Products(ProductId) ON DELETE CASCADE,
	Tag VARCHAR(100) NOT NULL,
	PRIMARY KEY (ProductId, Tag)
);

CREATE TABLE Orders (
	OrderId BIGINT PRIMARY KEY,
	CustomerId BIGINT NOT NULL REFERENCES Customers(CustomerId),
	OrderDate TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	Status VARCHAR(32) NOT NULL,
	Currency CHAR(3) NOT NULL DEFAULT 'USD',
	Subtotal DECIMAL(12,2) NOT NULL CHECK (Subtotal >= 0),
	Shipping DECIMAL(12,2) NOT NULL DEFAULT 0,
	Tax DECIMAL(12,2) NOT NULL DEFAULT 0,
	Total DECIMAL(12,2) NOT NULL CHECK (Total >= 0),
	ShippingAddressId BIGINT REFERENCES CustomerAddresses(AddressId)
);

CREATE TABLE OrderItems (
	OrderId BIGINT NOT NULL REFERENCES Orders(OrderId) ON DELETE CASCADE,
	OrderItemId INT NOT NULL,
	ProductId BIGINT NOT NULL REFERENCES Products(ProductId),
	Quantity INT NOT NULL CHECK (Quantity > 0),
	UnitPrice DECIMAL(12,2) NOT NULL CHECK (UnitPrice >= 0),
	Discount DECIMAL(12,2) NOT NULL DEFAULT 0,
	PRIMARY KEY (OrderId, OrderItemId)
);

CREATE INDEX IX_OrderItems_Product ON OrderItems(ProductId);

CREATE TABLE Payments (
	PaymentId BIGINT PRIMARY KEY,
	OrderId BIGINT NOT NULL REFERENCES Orders(OrderId) ON DELETE CASCADE,
	PaidAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	Amount DECIMAL(12,2) NOT NULL,
	Provider VARCHAR(64) NOT NULL,
	TransactionReference VARCHAR(200)
);

-- An example of a computed / persisted column (SQL Server syntax) or view in other DBs
-- ALTER TABLE Orders ADD OrderHash AS HASHBYTES('SHA1', CONCAT(OrderId, '|', CustomerId));

-- End of ecommerce sample
