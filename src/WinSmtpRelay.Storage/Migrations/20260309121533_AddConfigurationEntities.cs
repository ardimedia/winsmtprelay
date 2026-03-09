using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WinSmtpRelay.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcceptedDomains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptedDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DkimDomains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Selector = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PrivateKeyPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DkimDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HeaderRewriteEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HeaderName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    MatchValue = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeaderRewriteEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpAccessRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Network = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAccessRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaxConnectionsPerIpPerMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMessagesPerSenderPerMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMessagesPerSenderPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedAuthBanThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedAuthBanMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiveConnectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    RequireTls = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImplicitTls = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireAuth = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxMessageSizeBytes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxConnections = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiveConnectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SendConnectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SmartHost = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SmartHostPort = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "TEXT", nullable: true),
                    OpportunisticTls = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireTls = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxConcurrentDeliveries = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetryHours = table.Column<int>(type: "INTEGER", nullable: false),
                    RetryIntervalsMinutes = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConnectTimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SendConnectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SenderRewriteEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromPattern = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    ToAddress = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SenderRewriteEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DomainPattern = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SendConnectorId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DomainRoutes_SendConnectors_SendConnectorId",
                        column: x => x.SendConnectorId,
                        principalTable: "SendConnectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RateLimitSettings",
                columns: new[] { "Id", "FailedAuthBanMinutes", "FailedAuthBanThreshold", "MaxConnectionsPerIpPerMinute", "MaxMessagesPerSenderPerDay", "MaxMessagesPerSenderPerMinute", "UpdatedUtc" },
                values: new object[] { 1, 30, 5, 30, 1000, 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptedDomains_Domain",
                table: "AcceptedDomains",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DkimDomains_Domain",
                table: "DkimDomains",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DomainRoutes_SendConnectorId",
                table: "DomainRoutes",
                column: "SendConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_DomainRoutes_SortOrder",
                table: "DomainRoutes",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_HeaderRewriteEntries_SortOrder",
                table: "HeaderRewriteEntries",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_IpAccessRules_SortOrder",
                table: "IpAccessRules",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SenderRewriteEntries_SortOrder",
                table: "SenderRewriteEntries",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptedDomains");

            migrationBuilder.DropTable(
                name: "DkimDomains");

            migrationBuilder.DropTable(
                name: "DomainRoutes");

            migrationBuilder.DropTable(
                name: "HeaderRewriteEntries");

            migrationBuilder.DropTable(
                name: "IpAccessRules");

            migrationBuilder.DropTable(
                name: "RateLimitSettings");

            migrationBuilder.DropTable(
                name: "ReceiveConnectors");

            migrationBuilder.DropTable(
                name: "SenderRewriteEntries");

            migrationBuilder.DropTable(
                name: "SendConnectors");
        }
    }
}
