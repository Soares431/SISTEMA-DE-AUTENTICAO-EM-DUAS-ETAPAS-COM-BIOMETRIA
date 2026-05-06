CREATE TABLE dbo.person (
    [id] BIGINT NOT NULL IDENTITY,
    [name] VARCHAR(80) NOT NULL,
    [last_name] VARCHAR(80) NOT NULL,
    [address] VARCHAR(100) NOT NULL,
    [gender] VARCHAR(10) NOT NULL,
    PRIMARY KEY ([id])
);