using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ByteWizardsContentRewrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HuntingPlaceCreatures_Creatures_CreatureId",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropForeignKey(
                name: "FK_MonsterSpawnCreatureLinks_Creatures_CreatureId",
                table: "MonsterSpawnCreatureLinks");

            migrationBuilder.DropTable(
                name: "CreatureDamageModifiers");

            migrationBuilder.DropIndex(
                name: "IX_MonsterSpawnCoordinates_X_Y_Z",
                table: "MonsterSpawnCoordinates");

            migrationBuilder.DropIndex(
                name: "IX_MonsterSpawnCoordinates_Z_X_Y",
                table: "MonsterSpawnCoordinates");

            migrationBuilder.DropIndex(
                name: "IX_MonsterImageAssets_AssetUri",
                table: "MonsterImageAssets");

            migrationBuilder.DropIndex(
                name: "IX_MonsterImageAssets_CanonicalSlug",
                table: "MonsterImageAssets");

            migrationBuilder.DropIndex(
                name: "IX_MonsterImageAssets_ContentHash",
                table: "MonsterImageAssets");

            migrationBuilder.DropIndex(
                name: "IX_MonsterImageAliases_Slug",
                table: "MonsterImageAliases");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemIdPrimary",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_LevelRequired_Attack_Defense_Armor",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Name",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_NormalizedName",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ObjectClass_PrimaryType_SecondaryType",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ObjectClass_PrimaryType_WeaponType_Hands_LevelRequired",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_WeaponType_Hands",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_HuntingPlaces_Name",
                table: "HuntingPlaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HuntingPlaceCreatures",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropIndex(
                name: "IX_CreatureSounds_CreatureId_Text",
                table: "CreatureSounds");

            migrationBuilder.DropIndex(
                name: "IX_Creatures_ActualName",
                table: "Creatures");

            migrationBuilder.DropIndex(
                name: "IX_Creatures_ContentHash",
                table: "Creatures");

            migrationBuilder.DropIndex(
                name: "IX_CreatureLoots_CreatureId_ItemName",
                table: "CreatureLoots");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "MonsterImageAssets",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MonsterImageAssets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "MonsterImageAssets",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RemoteAssetId",
                table: "MonsterImageAssets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "MonsterImageAssets",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MonsterImageAssets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Value",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldDefaultValue: 0L);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "Items",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "Items",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Items",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategorySlug",
                table: "Items",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Items",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ContentId",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DamageRange",
                table: "Items",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DeathAttack",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefenseMod",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DroppedByCsv",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EarthAttack",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnergyAttack",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FireAttack",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HolyAttack",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IceAttack",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagesJson",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAt",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NpcPrice",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NpcValue",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryAssetId",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageFileName",
                table: "Items",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageMimeType",
                table: "Items",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageStorageKey",
                table: "Items",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoundsJson",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceJson",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceLastUpdatedAt",
                table: "Items",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "TemplateType",
                table: "Items",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UpgradeClass",
                table: "Items",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Walkable",
                table: "Items",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikiUrl",
                table: "Items",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CategoriesJson",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ContentId",
                table: "HuntingPlaces",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAt",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Map4",
                table: "HuntingPlaces",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlainTextContent",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawWikiText",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceLastUpdatedAt",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "StructuredDataJson",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "HuntingPlaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "HuntingPlaces",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WikiUrl",
                table: "HuntingPlaces",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatureId",
                table: "HuntingPlaceCreatures",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "HuntingPlaceCreatures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<string>(
                name: "CreatureName",
                table: "HuntingPlaceCreatures",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Abilities",
                table: "Creatures",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BestiaryClass",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BestiaryDifficulty",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BestiaryOccurrence",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BosstiaryCategory",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentId",
                table: "Creatures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_DeathFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_DeathRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_DrownFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_DrownRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_EarthFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_EarthRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_EnergyFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_EnergyRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_FireFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_FireRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_HealFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_HealRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_HolyFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_HolyRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_HpDrainFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_HpDrainRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_IceFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_IceRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Damage_PhysicalFactor",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Damage_PhysicalRaw",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "History",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagesJson",
                table: "Creatures",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAt",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plural",
                table: "Creatures",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PrimaryAssetId",
                table: "Creatures",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageFileName",
                table: "Creatures",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageMimeType",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageStorageKey",
                table: "Creatures",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RaceId",
                table: "Creatures",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryType",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceLastUpdatedAt",
                table: "Creatures",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "SpawnType",
                table: "Creatures",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Speed",
                table: "Creatures",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuredDataJson",
                table: "Creatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UsesSpells",
                table: "Creatures",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Chance",
                table: "CreatureLoots",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Raw",
                table: "CreatureLoots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HuntingPlaceCreatures",
                table: "HuntingPlaceCreatures",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ContentId",
                table: "Items",
                column: "ContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPlaces_ContentId",
                table: "HuntingPlaces",
                column: "ContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPlaceCreatures_HuntingPlaceId",
                table: "HuntingPlaceCreatures",
                column: "HuntingPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatureSounds_CreatureId",
                table: "CreatureSounds",
                column: "CreatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Creatures_ContentId",
                table: "Creatures",
                column: "ContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatureLoots_CreatureId",
                table: "CreatureLoots",
                column: "CreatureId");

            migrationBuilder.AddForeignKey(
                name: "FK_HuntingPlaceCreatures_Creatures_CreatureId",
                table: "HuntingPlaceCreatures",
                column: "CreatureId",
                principalTable: "Creatures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MonsterSpawnCreatureLinks_Creatures_CreatureId",
                table: "MonsterSpawnCreatureLinks",
                column: "CreatureId",
                principalTable: "Creatures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HuntingPlaceCreatures_Creatures_CreatureId",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropForeignKey(
                name: "FK_MonsterSpawnCreatureLinks_Creatures_CreatureId",
                table: "MonsterSpawnCreatureLinks");

            migrationBuilder.DropIndex(
                name: "IX_Items_ContentId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_HuntingPlaces_ContentId",
                table: "HuntingPlaces");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HuntingPlaceCreatures",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropIndex(
                name: "IX_HuntingPlaceCreatures_HuntingPlaceId",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropIndex(
                name: "IX_CreatureSounds_CreatureId",
                table: "CreatureSounds");

            migrationBuilder.DropIndex(
                name: "IX_Creatures_ContentId",
                table: "Creatures");

            migrationBuilder.DropIndex(
                name: "IX_CreatureLoots_CreatureId",
                table: "CreatureLoots");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "MonsterImageAssets");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MonsterImageAssets");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "MonsterImageAssets");

            migrationBuilder.DropColumn(
                name: "RemoteAssetId",
                table: "MonsterImageAssets");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "MonsterImageAssets");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MonsterImageAssets");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CategorySlug",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ContentId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DamageRange",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DeathAttack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DefenseMod",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DroppedByCsv",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "EarthAttack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "EnergyAttack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "FireAttack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "HolyAttack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IceAttack",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ImagesJson",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "NpcPrice",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "NpcValue",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PrimaryAssetId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PrimaryImageFileName",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PrimaryImageMimeType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PrimaryImageStorageKey",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SoundsJson",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SourceJson",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SourceLastUpdatedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "TemplateType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UpgradeClass",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Walkable",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "WikiUrl",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CategoriesJson",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "ContentId",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "Map4",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "PlainTextContent",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "RawWikiText",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "SourceLastUpdatedAt",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "StructuredDataJson",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "WikiUrl",
                table: "HuntingPlaces");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropColumn(
                name: "CreatureName",
                table: "HuntingPlaceCreatures");

            migrationBuilder.DropColumn(
                name: "Abilities",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "BestiaryClass",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "BestiaryDifficulty",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "BestiaryOccurrence",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "BosstiaryCategory",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "ContentId",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_DeathFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_DeathRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_DrownFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_DrownRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_EarthFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_EarthRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_EnergyFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_EnergyRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_FireFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_FireRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_HealFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_HealRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_HolyFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_HolyRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_HpDrainFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_HpDrainRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_IceFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_IceRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_PhysicalFactor",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Damage_PhysicalRaw",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "History",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "ImagesJson",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Plural",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "PrimaryAssetId",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "PrimaryImageFileName",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "PrimaryImageMimeType",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "PrimaryImageStorageKey",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "RaceId",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "SecondaryType",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "SourceLastUpdatedAt",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "SpawnType",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "StructuredDataJson",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "UsesSpells",
                table: "Creatures");

            migrationBuilder.DropColumn(
                name: "Chance",
                table: "CreatureLoots");

            migrationBuilder.DropColumn(
                name: "Raw",
                table: "CreatureLoots");

            migrationBuilder.AlterColumn<long>(
                name: "Value",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "UpdatedAtUtc",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAtUtc",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "CreatureId",
                table: "HuntingPlaceCreatures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HuntingPlaceCreatures",
                table: "HuntingPlaceCreatures",
                columns: new[] { "HuntingPlaceId", "CreatureId" });

            migrationBuilder.CreateTable(
                name: "CreatureDamageModifiers",
                columns: table => new
                {
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeathFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    DeathRaw = table.Column<string>(type: "TEXT", nullable: true),
                    DrownFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    DrownRaw = table.Column<string>(type: "TEXT", nullable: true),
                    EarthFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    EarthRaw = table.Column<string>(type: "TEXT", nullable: true),
                    EnergyFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    EnergyRaw = table.Column<string>(type: "TEXT", nullable: true),
                    FireFactor = table.Column<decimal>(type: "NUMERIC", nullable: true),
                    FireRaw = table.Column<string>(type: "TEXT", nullable: true),
                    HealFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    HealRaw = table.Column<string>(type: "TEXT", nullable: true),
                    HolyFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    HolyRaw = table.Column<string>(type: "TEXT", nullable: true),
                    HpDrainFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    HpDrainRaw = table.Column<string>(type: "TEXT", nullable: true),
                    IceFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    IceRaw = table.Column<string>(type: "TEXT", nullable: true),
                    PhysicalFactor = table.Column<decimal>(type: "NUMERIC", nullable: true),
                    PhysicalRaw = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatureDamageModifiers", x => x.CreatureId);
                    table.ForeignKey(
                        name: "FK_CreatureDamageModifiers_Creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonsterSpawnCoordinates_X_Y_Z",
                table: "MonsterSpawnCoordinates",
                columns: new[] { "X", "Y", "Z" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonsterSpawnCoordinates_Z_X_Y",
                table: "MonsterSpawnCoordinates",
                columns: new[] { "Z", "X", "Y" });

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAssets_AssetUri",
                table: "MonsterImageAssets",
                column: "AssetUri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAssets_CanonicalSlug",
                table: "MonsterImageAssets",
                column: "CanonicalSlug");

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAssets_ContentHash",
                table: "MonsterImageAssets",
                column: "ContentHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonsterImageAliases_Slug",
                table: "MonsterImageAliases",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemIdPrimary",
                table: "Items",
                column: "ItemIdPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_Items_LevelRequired_Attack_Defense_Armor",
                table: "Items",
                columns: new[] { "LevelRequired", "Attack", "Defense", "Armor" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Items_NormalizedName",
                table: "Items",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ObjectClass_PrimaryType_SecondaryType",
                table: "Items",
                columns: new[] { "ObjectClass", "PrimaryType", "SecondaryType" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ObjectClass_PrimaryType_WeaponType_Hands_LevelRequired",
                table: "Items",
                columns: new[] { "ObjectClass", "PrimaryType", "WeaponType", "Hands", "LevelRequired" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_WeaponType_Hands",
                table: "Items",
                columns: new[] { "WeaponType", "Hands" });

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPlaces_Name",
                table: "HuntingPlaces",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CreatureSounds_CreatureId_Text",
                table: "CreatureSounds",
                columns: new[] { "CreatureId", "Text" });

            migrationBuilder.CreateIndex(
                name: "IX_Creatures_ActualName",
                table: "Creatures",
                column: "ActualName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Creatures_ContentHash",
                table: "Creatures",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_CreatureLoots_CreatureId_ItemName",
                table: "CreatureLoots",
                columns: new[] { "CreatureId", "ItemName" });

            migrationBuilder.AddForeignKey(
                name: "FK_HuntingPlaceCreatures_Creatures_CreatureId",
                table: "HuntingPlaceCreatures",
                column: "CreatureId",
                principalTable: "Creatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MonsterSpawnCreatureLinks_Creatures_CreatureId",
                table: "MonsterSpawnCreatureLinks",
                column: "CreatureId",
                principalTable: "Creatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
