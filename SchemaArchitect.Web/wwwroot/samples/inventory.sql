-- Sample: Inventory & Warehouse Schema (medium complexity)
-- Purpose: demonstrates warehouses, stock levels, stock movements, suppliers,
--          purchase orders, composite keys, and transactional history
-- Dialect: Generic SQL

CREATE TABLE Suppliers (
	SupplierId BIGINT PRIMARY KEY,
	Name VARCHAR(200) NOT NULL,
	ContactName VARCHAR(200),
	ContactEmail VARCHAR(320),
	Phone VARCHAR(32)
);

CREATE TABLE Warehouses (
	WarehouseId BIGINT PRIMARY KEY,
	Name VARCHAR(200) NOT NULL,
	Location VARCHAR(300)
);

CREATE TABLE Items (
	ItemId BIGINT PRIMARY KEY,
	SKU VARCHAR(64) NOT NULL UNIQUE,
	Name VARCHAR(300) NOT NULL,
	Description TEXT,
	UnitCost DECIMAL(12,4) NOT NULL CHECK (UnitCost >= 0)
);

-- Stock levels per warehouse and item. Composite PK ensures one row per item/warehouse
CREATE TABLE StockLevels (
	WarehouseId BIGINT NOT NULL REFERENCES Warehouses(WarehouseId) ON DELETE CASCADE,
	ItemId BIGINT NOT NULL REFERENCES Items(ItemId) ON DELETE CASCADE,
	Quantity INT NOT NULL DEFAULT 0,
	ReorderLevel INT NOT NULL DEFAULT 0,
	PRIMARY KEY (WarehouseId, ItemId)
);

CREATE TABLE PurchaseOrders (
	PurchaseOrderId BIGINT PRIMARY KEY,
	SupplierId BIGINT NOT NULL REFERENCES Suppliers(SupplierId),
	CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	ExpectedAt TIMESTAMP,
	Status VARCHAR(32) NOT NULL
);

CREATE TABLE PurchaseOrderLines (
	PurchaseOrderId BIGINT NOT NULL REFERENCES PurchaseOrders(PurchaseOrderId) ON DELETE CASCADE,
	LineNumber INT NOT NULL,
	ItemId BIGINT NOT NULL REFERENCES Items(ItemId),
	Quantity INT NOT NULL CHECK (Quantity > 0),
	UnitPrice DECIMAL(12,4) NOT NULL CHECK (UnitPrice >= 0),
	PRIMARY KEY (PurchaseOrderId, LineNumber)
);

CREATE TABLE StockMovements (
	MovementId BIGINT PRIMARY KEY,
	ItemId BIGINT NOT NULL REFERENCES Items(ItemId),
	FromWarehouseId BIGINT REFERENCES Warehouses(WarehouseId),
	ToWarehouseId BIGINT REFERENCES Warehouses(WarehouseId),
	Quantity INT NOT NULL,
	MovementType VARCHAR(32) NOT NULL, -- 'IN', 'OUT', 'TRANSFER', 'ADJUSTMENT'
	PerformedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	Notes TEXT
);

CREATE INDEX IX_StockMovements_Item ON StockMovements(ItemId);

-- Transactional audit table example
CREATE TABLE StockLevelHistory (
	HistoryId BIGINT PRIMARY KEY,
	WarehouseId BIGINT NOT NULL,
	ItemId BIGINT NOT NULL,
	OldQuantity INT NOT NULL,
	NewQuantity INT NOT NULL,
	ChangedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	ChangedBy VARCHAR(200)
);

-- End of inventory sample
