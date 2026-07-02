-- Sample: Accounting / Financial Ledger Schema (medium complexity)
-- Purpose: demonstrates chart of accounts, journal entries, posting rules,
--          multi-currency amounts, and balance constraints
-- Dialect: Generic SQL

CREATE TABLE Accounts (
	AccountId BIGINT PRIMARY KEY,
	Code VARCHAR(32) NOT NULL UNIQUE, -- GL code
	Name VARCHAR(200) NOT NULL,
	AccountType VARCHAR(32) NOT NULL, -- 'Asset','Liability','Equity','Revenue','Expense'
	Currency CHAR(3) NOT NULL DEFAULT 'USD'
);

CREATE TABLE Journals (
	JournalId BIGINT PRIMARY KEY,
	Name VARCHAR(200) NOT NULL,
	Description TEXT
);

CREATE TABLE JournalEntries (
	EntryId BIGINT PRIMARY KEY,
	JournalId BIGINT REFERENCES Journals(JournalId),
	EntryDate DATE NOT NULL,
	Reference VARCHAR(200),
	CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE JournalLines (
	EntryId BIGINT NOT NULL REFERENCES JournalEntries(EntryId) ON DELETE CASCADE,
	LineNumber INT NOT NULL,
	AccountId BIGINT NOT NULL REFERENCES Accounts(AccountId),
	Amount DECIMAL(18,4) NOT NULL, -- positive for debit, negative for credit OR explicit side
	Currency CHAR(3) NOT NULL DEFAULT 'USD',
	Memo VARCHAR(400),
	PRIMARY KEY (EntryId, LineNumber)
);

-- Ensure that each journal entry balances (sum of amounts = 0) is usually enforced at application or DB trigger level
-- Example trigger (Postgres-style) would check SUM(Amount) = 0 before insert/update of JournalLines

CREATE TABLE Currencies (
	CurrencyCode CHAR(3) PRIMARY KEY,
	DisplayName VARCHAR(100),
	MinorUnit INT NOT NULL DEFAULT 2
);

CREATE TABLE ExchangeRates (
	RateId BIGINT PRIMARY KEY,
	FromCurrency CHAR(3) NOT NULL REFERENCES Currencies(CurrencyCode),
	ToCurrency CHAR(3) NOT NULL REFERENCES Currencies(CurrencyCode),
	Rate DECIMAL(18,8) NOT NULL CHECK (Rate > 0),
	EffectiveAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	UNIQUE (FromCurrency, ToCurrency, EffectiveAt)
);

CREATE INDEX IX_JournalEntries_Date ON JournalEntries(EntryDate);

-- Specialized view that produces ledger balances per account (conceptual)
-- CREATE VIEW AccountBalances AS
-- SELECT jl.AccountId, a.Code, a.Name, SUM(jl.Amount) AS Balance
-- FROM JournalLines jl
-- JOIN Accounts a ON a.AccountId = jl.AccountId
-- GROUP BY jl.AccountId, a.Code, a.Name;

-- End of accounting sample
