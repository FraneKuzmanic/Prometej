using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class AuthHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "PasswordHash");

            // Existing rows hold plain-text passwords and nothing kept emails unique.
            // Before the unique index goes on: later duplicates of an email get a suffix,
            // emails are normalised, and every plain-text password becomes a bcrypt hash.
            // An empty database skips all of it and so never needs pgcrypto.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                  IF EXISTS (SELECT 1 FROM "Users") THEN
                    CREATE EXTENSION IF NOT EXISTS pgcrypto;

                    UPDATE "Users" u
                       SET "Email" = u."Email" || '.duplicate-' || u."Id"
                     WHERE EXISTS (SELECT 1 FROM "Users" o
                                    WHERE lower(trim(o."Email")) = lower(trim(u."Email"))
                                      AND o."Id" < u."Id");

                    UPDATE "Users" SET "Email" = lower(trim("Email"));

                    UPDATE "Users"
                       SET "PasswordHash" = crypt("PasswordHash", gen_salt('bf', 11))
                     WHERE "PasswordHash" NOT LIKE '$2%';
                  END IF;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the schema only. Hashing is one-way: the column goes back to its
            // old name still holding bcrypt hashes, and renamed duplicate emails stay renamed.
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Users",
                newName: "Password");
        }
    }
}
