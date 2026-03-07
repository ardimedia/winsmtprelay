using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WinSmtpRelay.Storage.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QueuedMessageId = table.Column<long>(type: "INTEGER", nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    StatusCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    StatusMessage = table.Column<string>(type: "TEXT", nullable: false),
                    RemoteServer = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueuedMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Sender = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Recipients = table.Column<string>(type: "TEXT", nullable: false),
                    RawMessage = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SizeBytes = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NextRetryUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SourceIp = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    AuthenticatedUser = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueuedMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelayUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowedSenderAddresses = table.Column<string>(type: "TEXT", nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    RateLimitPerDay = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelayUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLogs_QueuedMessageId",
                table: "DeliveryLogs",
                column: "QueuedMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryLogs_TimestampUtc",
                table: "DeliveryLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_QueuedMessages_MessageId",
                table: "QueuedMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_QueuedMessages_NextRetryUtc",
                table: "QueuedMessages",
                column: "NextRetryUtc");

            migrationBuilder.CreateIndex(
                name: "IX_QueuedMessages_Status",
                table: "QueuedMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RelayUsers_Username",
                table: "RelayUsers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryLogs");

            migrationBuilder.DropTable(
                name: "QueuedMessages");

            migrationBuilder.DropTable(
                name: "RelayUsers");
        }
    }
}
