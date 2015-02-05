USE [master]
IF DB_ID('SpendingReport') IS NOT NULL
DROP DATABASE SpendingReport
GO
CREATE DATABASE SpendingReport
GO

USE [SpendingReport]

CREATE TABLE Users
(
	UserId bigint not null
		constraint PK_Users primary key
		identity,

	UserName nvarchar(64) not null
		constraint UQ_Users_UserName unique,

	Salt int not null,

	PasswordHash nvarchar(max) not null,

	FirstName nvarchar(64) not null,

	MiddleInitial nchar(1) null,

	LastName nvarchar(64) not null,

	EmailAddress nvarchar(64) not null
		constraint UQ_Users_EmailAddress unique
)

CREATE TABLE Categories
(
	CategoryId bigint not null
		constraint PK_Categories primary key
		identity,
		
	Name nvarchar(32) not null
		constraint UQ_Categories_Name unique
)

CREATE TABLE UserCategories
(
	UserCategoryId bigint not null
		constraint PK_UserCategories primary key
		identity,

	UserId bigint not null
		constraint FK_UserCategories_Users foreign key references Users(UserId) on delete cascade,

	CategoryId bigint not null
		constraint FK_UserCategories_Categories foreign key references Categories(CategoryId),

	-- Unfortunately, recursive cascades aren't allowed, so I can't have this column auto-null
	-- itself when parent categories are deleted.
	ParentUserCategoryId bigint null
		constraint FK_UserCategories_UserCategories foreign key references UserCategories(UserCategoryId)
		constraint DF_UserCategories_ParentUserCategoryId default(null),

	constraint UQ_UserCategories_UserId_CategoryId unique (UserId, CategoryId),
	index IX_UserCategories_ParentUserCategoryId (ParentUserCategoryId)
)

CREATE TABLE Payees
(
	PayeeId bigint not null
		constraint PK_Payees primary key
		identity,

	Name nvarchar(128) not null
		constraint UQ_Payees_Name unique
)

-- TODO: Consider partitioning this table by TransactionDate
CREATE TABLE Transactions
(
	TransactionId uniqueidentifier not null
		constraint PK_Transactions primary key nonclustered
		constraint DF_Transactions default(NEWID()),

	UserId bigint not null
		constraint FK_Transactions_Users foreign key references Users(UserId) on delete cascade,

	PayeeId bigint not null
		constraint FK_Transactions_Payees foreign key references Payees(PayeeId),

	-- TODO: SQL is unhappy about "on delete set default" for this foreign key; something to do with
	-- the delete cascade on users causing two actions to occur if a user is deleted. Consider how best to
	-- deal with that.
	UserCategoryId bigint null
		constraint FK_Transactions_Categories foreign key references UserCategories(UserCategoryId)
		constraint DF_Transactions_UserCategoryId default(null),

	Amount money not null,

	TransactionDate date not null
		constraint DF_Transactions_TransactionDate default(CURRENT_TIMESTAMP),

	AddedDate datetime2 not null
		constraint DF_Transactions_AddedDate default(CURRENT_TIMESTAMP),

	ModifiedDate datetime2 not null
		constraint DF_Transactions_ModifiedDate default(CURRENT_TIMESTAMP),

	-- I expect this table to be queried more heavily on UserId and transaction date range
	-- than on TransactionId, so I'm making that the clustered index to help make such
	-- queries faster
	index IX_Transactions_UserId_TransactionDate clustered (UserId, TransactionDate),
	index IX_Transactions_UserId_PayeeId (UserId, PayeeId),
	index IX_Transactions_UserCategoryId (UserCategoryId)
)
GO

CREATE FUNCTION FN_UserCategories_HasRecursion
(
	@UserCategoryId bigint,
	@ParentUserCategoryId bigint
)
RETURNS BIT
WITH RETURNS NULL ON NULL INPUT
AS
BEGIN
	DECLARE @HasRecursion bit;

	IF @ParentUserCategoryId IS NOT NULL
	BEGIN
		IF @ParentUserCategoryId = @UserCategoryId
		BEGIN
			SET @HasRecursion = 1;
		END
		ELSE
		BEGIN
			SELECT @ParentUserCategoryId = ParentUserCategoryId
			FROM UserCategories
			WHERE UserCategoryId = @ParentUserCategoryId

			SET @HasRecursion = dbo.FN_UserCategories_HasRecursion(@UserCategoryId, @ParentUserCategoryId);
		END
	END

	RETURN @HasRecursion;
END
GO

-- Since categories can have children, make sure no recursion occurs.
ALTER TABLE UserCategories
ADD CONSTRAINT CK_UserCategories_NoRecursion CHECK (dbo.FN_UserCategories_HasRecursion(UserCategoryId, ParentUserCategoryId) IS NULL)
GO

-- I was originally planning on making these views updateable, but that didn't work with
-- the output clauses in the put stored procedures, so for now these are only used for easy viewing
-- of joined tables and get operations.
CREATE VIEW VW_UserCategories
WITH SCHEMABINDING, VIEW_METADATA
AS
WITH ResolvedUserCategories AS
(
	SELECT UserCategories.UserCategoryId, UserCategories.UserId, Categories.Name, UserCategories.ParentUserCategoryId
	FROM dbo.UserCategories
	INNER JOIN dbo.Categories ON UserCategories.CategoryId = Categories.CategoryId
)
SELECT UserCategories.UserCategoryId, UserCategories.UserId, UserCategories.Name, UserCategories.ParentUserCategoryId, ParentCategories.Name AS ParentName
FROM ResolvedUserCategories AS UserCategories
LEFT JOIN ResolvedUserCategories AS ParentCategories ON UserCategories.ParentUserCategoryId = ParentCategories.UserCategoryId
GO

CREATE VIEW VW_Transactions
WITH SCHEMABINDING, VIEW_METADATA
AS
SELECT TransactionId, Transactions.UserId, Categories.Name AS CategoryName, Payees.Name AS PayeeName, Amount, TransactionDate, AddedDate, ModifiedDate
FROM dbo.Transactions
INNER JOIN dbo.Payees ON Transactions.PayeeId = Payees.PayeeId
LEFT JOIN dbo.VW_UserCategories AS Categories ON Transactions.UserCategoryId = Categories.UserCategoryId
GO

-- TODO: Consider using read uncommitted transaction isolation on get procedures that involve UserIds (currently all
-- of them) to improve performance since it is unlikely that a single user will be introducing concurrency problems
-- by performing operations in two different places at the same time.

-- TODO: Not really liking the rigidity of the execution plans for these get queries, consider if/elsing
-- instead of having a single query even though it will be messy.

CREATE PROCEDURE SP_Users_Get
(
	@UserId bigint = null,
	@UserName nvarchar(64) = null,
	@EmailAddress nvarchar(64) = null
)
AS
BEGIN
	SELECT UserId, UserName, Salt, PasswordHash, FirstName, MiddleInitial, LastName, EmailAddress
	FROM Users
	WHERE (@UserId IS NULL OR UserId = @UserId)
		AND (@UserName IS NULL OR UserName = @UserName)
		AND (@EmailAddress IS NULL OR EmailAddress = @EmailAddress)
	OPTION
	(
		OPTIMIZE FOR
		(
			@UserId = NULL,
			@UserName UNKNOWN,
			@EmailAddress = NULL
		)
	)
END
GO

-- TODO: I just found out that user-defined table types can have constraints
-- (though they can't be nicely named, grr...). Merges work best when joined using source
-- table fields that are pre-sorted; determine whether or not having the indexes that
-- these constraints implicitly cause to be created help or hinder performance and consider
-- reworking the client code to support PK constraints on the Id fields to improve merge
-- performance. Note that a sort still happens if any joins are used since duplicate rows
-- could occur, so this likely won't work if I abstract away the Categories and Payees tables
-- like I am currently doing.

CREATE TYPE UDT_Users_Put AS TABLE
(
	EntityId int not null
		primary key,

	UserId bigint not null,

	UserName nvarchar(64) not null
		unique,

	Salt int not null,

	PasswordHash nvarchar(max) not null,

	FirstName nvarchar(64) not null,

	MiddleInitial char(1) null,

	LastName nvarchar(64) not null,

	EmailAddress nvarchar(64) not null
		unique
)
GO

-- TODO: Regarding upserts using merge, http://weblogs.sqlteam.com/dang/archive/2009/01/31/UPSERT-Race-Condition-With-MERGE.aspx
-- claims that WITH (HOLDLOCK) is necessary to avoid possible race conditions when concurrent connections
-- are running the same merge, and so far in addition to several other articles referring to this article I've found
-- nothing to refute its claims or show that the underlying problem has been fixed somehow. Consider adding this
-- to all merge upserts since I'm not currently using any explicit transactions.

CREATE PROCEDURE SP_Users_Put
(
	@Users UDT_Users_Put readonly
)
AS
BEGIN
	MERGE INTO Users AS target
	USING @Users AS source
	ON target.UserId = source.UserId
	WHEN NOT MATCHED THEN
		INSERT (UserName, Salt, PasswordHash, FirstName, MiddleInitial, LastName, EmailAddress)
		VALUES (UserName, Salt, PasswordHash, FirstName, MiddleInitial, LastName, EmailAddress)
	WHEN MATCHED THEN
		UPDATE
		SET target.PasswordHash = source.PasswordHash,
			target.FirstName = source.FirstName,
			target.MiddleInitial = source.MiddleInitial,
			target.LastName = source.LastName,
			target.EmailAddress = source.EmailAddress
	OUTPUT source.EntityId, inserted.UserId;
END
GO

CREATE TYPE UDT_Users_Del AS TABLE
(
	UserId bigint not null
		primary key
)
GO

CREATE PROCEDURE SP_Users_Del
(
	@Users UDT_Users_Del readonly
)
AS
BEGIN
	MERGE INTO Users AS target
	USING @Users AS source
	ON target.UserId = source.UserId
	WHEN MATCHED THEN
		DELETE;
END
GO

CREATE PROCEDURE SP_UserCategories_Get
(
	@UserId bigint,
	@Name nvarchar(32) = null,
	@ParentName nvarchar(32) = null
)
AS
BEGIN
	SELECT UserCategoryId, UserId, Name, ParentName
	FROM VW_UserCategories
	WHERE UserId = @UserId
		AND (@Name IS NULL OR Name = @Name)
		AND (@ParentName IS NULL OR ParentName = @ParentName)
END
GO

CREATE TYPE UDT_UserCategories_Put AS TABLE
(
	EntityId int not null
		primary key,

	UserCategoryId bigint not null,

	UserId bigint not null,

	Name nvarchar(32) not null
		unique,

	ParentName nvarchar(32) null
)
GO

CREATE PROCEDURE SP_UserCategories_Put
(
	@UserCategories UDT_UserCategories_Put readonly
)
AS
BEGIN
	WITH DistinctCategoryNames AS
	(
		SELECT DISTINCT Name
		FROM @UserCategories
	)
	MERGE INTO Categories AS target
	USING DistinctCategoryNames AS source
	ON target.Name = source.Name
	WHEN NOT MATCHED AND source.Name IS NOT NULL THEN
		INSERT (Name)
		VALUES (Name);

	WITH ResolvedUserCategories AS
	(
		SELECT EntityId, UserCategoryId, UserId, CategoryId
		FROM @UserCategories AS PutUserCategories
		INNER JOIN Categories ON PutUserCategories.Name = Categories.Name
	)
	MERGE INTO UserCategories AS target
	USING ResolvedUserCategories AS source
	ON target.UserCategoryId = source.UserCategoryId
	WHEN NOT MATCHED THEN
		INSERT (UserId, CategoryId)
		VALUES (UserId, CategoryId)
	WHEN MATCHED THEN
		UPDATE
		SET target.CategoryId = source.CategoryId
	OUTPUT source.EntityId, inserted.UserCategoryId;

	WITH ResolvedChildCategories AS
	(
		SELECT Categories.UserCategoryId, ParentCategories.UserCategoryId AS ParentUserCategoryId
		FROM @UserCategories AS source
		INNER JOIN VW_UserCategories AS Categories ON source.UserId = Categories.UserId AND source.Name = Categories.Name
		INNER JOIN VW_UserCategories AS ParentCategories ON source.UserId = ParentCategories.UserId AND source.ParentName = ParentCategories.Name
		WHERE COALESCE(Categories.ParentUserCategoryId, 0) <> COALESCE(ParentCategories.UserCategoryId, 0)
	)
	UPDATE UserCategories
	SET UserCategories.ParentUserCategoryId = ResolvedChildCategories.ParentUserCategoryId
	FROM ResolvedChildCategories
	WHERE UserCategories.UserCategoryId = ResolvedChildCategories.UserCategoryId;
END
GO

CREATE TYPE UDT_UserCategories_Del AS TABLE
(
	UserCategoryId bigint not null
		primary key
)
GO

CREATE PROCEDURE SP_UserCategories_Del
(
	@UserCategories UDT_UserCategories_Del readonly
)
AS
BEGIN
	-- For good query plans, https://technet.microsoft.com/en-us/library/cc879317.aspx recommends
	-- parameterizing all constants in MERGE's ON and WHEN clauses. Also, declaring these as bits
	-- explicitly instead of letting them implicitly be ints will likely improve cardinality estimates
	-- since it greatly increases the probability of the comparisons being true.
	DECLARE @DeleteRequired bit;
	SET @DeleteRequired = 1;

	DEClARE @DeleteNotRequired bit
	SET @DeleteNotRequired = 0;

	-- Since recursive cascades aren't allowed, manually find all children and update their parent
	-- ids to null. Happily, sql will be ok with doing this as long as the foreign key
	-- constraints are all satisfied at the end of the merge's autocommit transaction.
	WITH ResolvedDeleted AS
	(
		SELECT @DeleteRequired AS DeleteRequired, DelUserCategories.UserCategoryId
		FROM @UserCategories AS DelUserCategories
		UNION ALL
		SELECT @DeleteNotRequired AS DeleteRequired, ChildUserCategories.UserCategoryId
		FROM ResolvedDeleted
		INNER JOIN UserCategories AS ChildUserCategories ON ResolvedDeleted.UserCategoryId = ChildUserCategories.ParentUserCategoryId
		WHERE ResolvedDeleted.DeleteRequired = @DeleteRequired
	)
	MERGE INTO UserCategories AS target
	USING ResolvedDeleted AS source
	ON target.UserCategoryId = source.UserCategoryId
	WHEN MATCHED AND source.DeleteRequired = @DeleteNotRequired THEN
		UPDATE
		SET ParentUserCategoryId = NULL
	WHEN MATCHED AND source.DeleteRequired = @DeleteRequired THEN
		DELETE;
END
GO

CREATE PROCEDURE SP_Transactions_Get
(
	@TransactionId uniqueidentifier = null,
	@UserId bigint,
	@CategoryName nvarchar(32) = null,
	@PayeeName nvarchar(128) = null,
	@StartDate date = null,
	@EndDate date = null
)
AS
BEGIN
	SELECT TransactionId, UserId, CategoryName, PayeeName, TransactionDate, AddedDate, ModifiedDate
	FROM VW_Transactions
	WHERE (@TransactionId IS NULL OR TransactionId = @TransactionId)
		AND UserId = @UserId
		AND (@CategoryName IS NULL OR CategoryName = @CategoryName)
		AND (@PayeeName IS NULL OR PayeeName = @PayeeName)
		AND (@StartDate IS NULL OR TransactionDate >= @StartDate)
		AND (@EndDate IS NULL OR TransactionDate <= @EndDate)
	OPTION
	(
		OPTIMIZE FOR
		(
			@TransactionId = NULL,
			@UserId UNKNOWN,
			@CategoryName UNKNOWN,
			@PayeeName = NULL,
			@StartDate UNKNOWN,
			@EndDate UNKNOWN
		)
	)
END
GO

CREATE TYPE UDT_Transactions_Put AS TABLE
(
	EntityId int not null
		primary key,

	TransactionId uniqueidentifier not null,

	UserId bigint not null,

	CategoryName nvarchar(32) not null,

	PayeeName nvarchar(128) not null,

	Amount money not null,

	TransactionDate date not null
)
GO

CREATE PROCEDURE SP_Transactions_Put
(
	@Transactions UDT_Transactions_Put readonly
)
AS
BEGIN
	WITH DistinctPayeeNames AS
	(
		SELECT DISTINCT PayeeName
		FROM @Transactions
	)
	MERGE INTO Payees AS target
	USING DistinctPayeeNames AS source
	ON target.Name = source.PayeeName
	WHEN NOT MATCHED THEN
		INSERT (Name)
		VALUES (PayeeName);

	WITH ResolvedTransactions AS
	(
		SELECT EntityId, TransactionId, PutTransactions.UserId, PayeeId, UserCategoryId, Amount, TransactionDate
		FROM @Transactions AS PutTransactions
		INNER JOIN Payees ON PutTransactions.PayeeName = Payees.Name
		LEFT JOIN VW_UserCategories AS UserCategories ON PutTransactions.CategoryName = UserCategories.Name
	)
	MERGE INTO Transactions AS target
	USING ResolvedTransactions AS source
	ON target.TransactionId = source.TransactionId
	WHEN NOT MATCHED THEN
		INSERT (UserId, PayeeId, UserCategoryId, Amount, TransactionDate)
		VALUES (UserId, PayeeId, UserCategoryId, Amount, TransactionDate)
	WHEN MATCHED THEN UPDATE
		SET target.PayeeId = source.PayeeId,
			target.UserCategoryId = source.UserCategoryId,
			target.Amount = source.Amount,
			target.TransactionDate = source.TransactionDate,
			target.ModifiedDate = CURRENT_TIMESTAMP
	OUTPUT source.EntityId, inserted.TransactionId, inserted.AddedDate, inserted.ModifiedDate;
END
GO

CREATE TYPE UDT_Transactions_Del AS TABLE
(
	TransactionId uniqueidentifier not null
		primary key
)
GO

CREATE PROCEDURE SP_Transactions_Del
(
	@Transactions UDT_Transactions_Del readonly
)
AS
BEGIN
	MERGE INTO VW_Transactions AS target
	USING @Transactions AS source
	ON target.TransactionId = source.TransactionId
	WHEN MATCHED THEN
		DELETE;
END
GO

CREATE PROCEDURE SP_UserPayees_Get
(
	@UserId bigint
)
AS
BEGIN
	WITH DistinctUserPayees AS
	(
		SELECT DISTINCT PayeeId
		FROM Transactions
		WHERE UserId = @UserId AND TransactionDate > DATEADD(YY, -1, GETDATE())
	)
	SELECT Name
	FROM Payees
	WHERE Payees.PayeeId IN (SELECT DistinctUserPayees.PayeeId FROM DistinctUserPayees)
END
GO