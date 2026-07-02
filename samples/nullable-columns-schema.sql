CREATE TABLE [hr].[Employees]
(
    [EmployeeId] int IDENTITY(1,1) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [MiddleName] nvarchar(100) NULL,
    [LastName] nvarchar(100) NOT NULL,
    [ManagerId] int NULL,
    [DateOfBirth] date NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([EmployeeId]),
    CONSTRAINT [FK_Employees_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [hr].[Employees] ([EmployeeId])
);
