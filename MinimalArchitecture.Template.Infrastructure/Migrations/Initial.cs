using FluentMigrator;

namespace MinimalArchitecture.Template.Infrastructure.Migrations
{
    [Migration(0, "Initial")]
    public sealed class Initial : Migration
    {
        public override void Down()
        {
            Delete.Index("ix_payments_requested_at_utc")
                .OnTable("payments");
            Delete.Table("payments");
        }

        public override void Up()
        {
            Create.Table("payments")
                .WithColumn("id")
                    .AsGuid().PrimaryKey()
                .WithColumn("correlation_id")
                    .AsGuid().NotNullable().Unique()
                .WithColumn("amount")
                    .AsDecimal(18, 2).NotNullable()
                .WithColumn("processed_by")
                    .AsString().NotNullable()
                .WithColumn("requested_at_utc")
                    .AsDateTimeOffset().NotNullable()
                    .WithDefault(SystemMethods.CurrentDateTimeOffset)
                .WithColumn("integration_attempts")
                    .AsInt32().NotNullable()
                .WithColumn("processing_attempts")
                    .AsInt32().NotNullable();

            Create.Index("ix_payments_requested_at_utc")
                .OnTable("payments")
                .OnColumn("requested_at_utc").Descending();
        }
    }
}
