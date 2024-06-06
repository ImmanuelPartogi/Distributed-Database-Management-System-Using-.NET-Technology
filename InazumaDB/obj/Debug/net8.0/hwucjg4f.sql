BEGIN TRANSACTION;
GO

ALTER TABLE [orderHeaders] ADD [StripePaymentIntendId] nvarchar(max) NULL;
GO

ALTER TABLE [orderHeaders] ADD [StripeSessionId] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240512105559_addStripeInfoToOrderHeader', N'8.0.4');
GO

COMMIT;
GO

