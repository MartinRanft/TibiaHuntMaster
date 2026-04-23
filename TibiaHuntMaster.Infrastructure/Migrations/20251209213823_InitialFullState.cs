using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TibiaHuntMaster.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialFullState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    World = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Vocation = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    GuildName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Residence = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AccountStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AchievementPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LastLogin = table.Column<long>(type: "INTEGER", nullable: true),
                    LastUpdatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Creatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ActualName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Article = table.Column<string>(type: "TEXT", nullable: true),
                    TemplateType = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryType = table.Column<string>(type: "TEXT", nullable: true),
                    CreatureClass = table.Column<string>(type: "TEXT", nullable: true),
                    IsBoss = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsBoosted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Hp = table.Column<int>(type: "INTEGER", nullable: true),
                    Exp = table.Column<int>(type: "INTEGER", nullable: true),
                    Armor = table.Column<int>(type: "INTEGER", nullable: true),
                    Mitigation = table.Column<double>(type: "REAL", nullable: true),
                    MaxDmg = table.Column<int>(type: "INTEGER", nullable: true),
                    SummonMana = table.Column<int>(type: "INTEGER", nullable: true),
                    ConvinceMana = table.Column<int>(type: "INTEGER", nullable: true),
                    SenseInvis = table.Column<bool>(type: "INTEGER", nullable: true),
                    ParaImmune = table.Column<bool>(type: "INTEGER", nullable: true),
                    Illusionable = table.Column<bool>(type: "INTEGER", nullable: true),
                    Pushable = table.Column<bool>(type: "INTEGER", nullable: true),
                    PushObjects = table.Column<bool>(type: "INTEGER", nullable: true),
                    WalksThrough = table.Column<string>(type: "TEXT", nullable: true),
                    WalksAround = table.Column<string>(type: "TEXT", nullable: true),
                    RunsAt = table.Column<int>(type: "INTEGER", nullable: true),
                    Behaviour = table.Column<string>(type: "TEXT", nullable: true),
                    AttackType = table.Column<string>(type: "TEXT", nullable: true),
                    UsedElements = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Strategy = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ImplementedVersion = table.Column<string>(type: "TEXT", nullable: true),
                    SourceJson = table.Column<string>(type: "TEXT", nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Creatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HuntGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CharacterGoalId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HuntingPlaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Vocation = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Image = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ImplementedVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Map = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Map2 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Map3 = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    MapWidth = table.Column<int>(type: "INTEGER", nullable: true),
                    Map2Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Experience = table.Column<int>(type: "INTEGER", nullable: true),
                    ExperienceStar = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    LootValue = table.Column<int>(type: "INTEGER", nullable: true),
                    LootStar = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    BestLoot = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BestLoot2 = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BestLoot3 = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BestLoot4 = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BestLoot5 = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    LevelMages = table.Column<int>(type: "INTEGER", nullable: true),
                    LevelKnights = table.Column<int>(type: "INTEGER", nullable: true),
                    LevelPaladins = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillMages = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillKnights = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillPaladins = table.Column<int>(type: "INTEGER", nullable: true),
                    DefenseMages = table.Column<int>(type: "INTEGER", nullable: true),
                    DefenseKnights = table.Column<int>(type: "INTEGER", nullable: true),
                    DefensePaladins = table.Column<int>(type: "INTEGER", nullable: true),
                    CreaturesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SourceJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntingPlaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImbuementRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Tier = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseFee = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImbuementRecipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ActualName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Plural = table.Column<string>(type: "TEXT", nullable: false),
                    Article = table.Column<string>(type: "TEXT", nullable: false),
                    Implemented = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: false),
                    ItemIdPrimary = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemIdsCsv = table.Column<string>(type: "TEXT", nullable: false),
                    ObjectClass = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PrimaryType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SecondaryType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WeaponType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Hands = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LevelRequired = table.Column<int>(type: "INTEGER", nullable: true),
                    Attack = table.Column<int>(type: "INTEGER", nullable: true),
                    Defense = table.Column<int>(type: "INTEGER", nullable: true),
                    Armor = table.Column<int>(type: "INTEGER", nullable: true),
                    Range = table.Column<int>(type: "INTEGER", nullable: true),
                    ImbueSlots = table.Column<int>(type: "INTEGER", nullable: true),
                    DamageType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ElementAttack = table.Column<int>(type: "INTEGER", nullable: true),
                    ResistSummary = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Stackable = table.Column<bool>(type: "INTEGER", nullable: true),
                    Usable = table.Column<bool>(type: "INTEGER", nullable: true),
                    Pickupable = table.Column<bool>(type: "INTEGER", nullable: true),
                    Marketable = table.Column<bool>(type: "INTEGER", nullable: true),
                    WeightOz = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    SellTo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    VocRequired = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Attrib = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    FlavorText = table.Column<string>(type: "TEXT", nullable: false),
                    ExtrasJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamHuntSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    LootType = table.Column<string>(type: "TEXT", nullable: false),
                    TotalLoot = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalSupplies = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalBalance = table.Column<long>(type: "INTEGER", nullable: false),
                    RawInput = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamHuntSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LoyaltyTitle = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Position = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAccounts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAchievements_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBadges_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterDeaths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    KillersJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterDeaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterDeaths_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetValue = table.Column<long>(type: "INTEGER", nullable: false),
                    StartValue = table.Column<long>(type: "INTEGER", nullable: false),
                    ManualProgressOffset = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterGoals_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    HouseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Town = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Paid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterHouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterHouses_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    FetchedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSnapshots_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImbuementProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    UseBlankScrolls = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImbuementProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImbuementProfiles_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreatureDamageModifiers",
                columns: table => new
                {
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    PhysicalFactor = table.Column<decimal>(type: "NUMERIC", nullable: true),
                    PhysicalRaw = table.Column<string>(type: "TEXT", nullable: true),
                    FireFactor = table.Column<decimal>(type: "NUMERIC", nullable: true),
                    FireRaw = table.Column<string>(type: "TEXT", nullable: true),
                    IceFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    IceRaw = table.Column<string>(type: "TEXT", nullable: true),
                    EnergyFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    EnergyRaw = table.Column<string>(type: "TEXT", nullable: true),
                    EarthFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    EarthRaw = table.Column<string>(type: "TEXT", nullable: true),
                    HolyFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    HolyRaw = table.Column<string>(type: "TEXT", nullable: true),
                    DeathFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    DeathRaw = table.Column<string>(type: "TEXT", nullable: true),
                    HpDrainFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    HpDrainRaw = table.Column<string>(type: "TEXT", nullable: true),
                    DrownFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    DrownRaw = table.Column<string>(type: "TEXT", nullable: true),
                    HealFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    HealRaw = table.Column<string>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "CreatureLoots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MinAmount = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxAmount = table.Column<int>(type: "INTEGER", nullable: true),
                    AmountRaw = table.Column<string>(type: "TEXT", nullable: true),
                    Rarity = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatureLoots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatureLoots_Creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreatureSounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatureSounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatureSounds_Creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HuntSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    SessionStartTime = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    XpGain = table.Column<long>(type: "INTEGER", nullable: false),
                    XpPerHour = table.Column<long>(type: "INTEGER", nullable: false),
                    Loot = table.Column<long>(type: "INTEGER", nullable: false),
                    Supplies = table.Column<long>(type: "INTEGER", nullable: false),
                    Balance = table.Column<long>(type: "INTEGER", nullable: false),
                    Damage = table.Column<long>(type: "INTEGER", nullable: false),
                    Healing = table.Column<long>(type: "INTEGER", nullable: false),
                    IsDoubleXp = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDoubleLoot = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRapidRespawn = table.Column<bool>(type: "INTEGER", nullable: false),
                    HuntGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    RawInput = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntSessions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HuntSessions_HuntGroups_HuntGroupId",
                        column: x => x.HuntGroupId,
                        principalTable: "HuntGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HuntingPlaceCreatures",
                columns: table => new
                {
                    HuntingPlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatureId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntingPlaceCreatures", x => new { x.HuntingPlaceId, x.CreatureId });
                    table.ForeignKey(
                        name: "FK_HuntingPlaceCreatures_Creatures_CreatureId",
                        column: x => x.CreatureId,
                        principalTable: "Creatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HuntingPlaceCreatures_HuntingPlaces_HuntingPlaceId",
                        column: x => x.HuntingPlaceId,
                        principalTable: "HuntingPlaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HuntingPlaceLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HuntingPlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    LevelMages = table.Column<int>(type: "INTEGER", nullable: true),
                    LevelKnights = table.Column<int>(type: "INTEGER", nullable: true),
                    LevelPaladins = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillMages = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillKnights = table.Column<int>(type: "INTEGER", nullable: true),
                    SkillPaladins = table.Column<int>(type: "INTEGER", nullable: true),
                    DefenseMages = table.Column<int>(type: "INTEGER", nullable: true),
                    DefenseKnights = table.Column<int>(type: "INTEGER", nullable: true),
                    DefensePaladins = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntingPlaceLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntingPlaceLevels_HuntingPlaces_HuntingPlaceId",
                        column: x => x.HuntingPlaceId,
                        principalTable: "HuntingPlaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImbuementIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImbuementRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImbuementIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImbuementIngredients_ImbuementRecipes_ImbuementRecipeId",
                        column: x => x.ImbuementRecipeId,
                        principalTable: "ImbuementRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImbuementIngredients_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserItemPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserItemPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserItemPrices_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamHuntMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamHuntSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsLeader = table.Column<bool>(type: "INTEGER", nullable: false),
                    Loot = table.Column<long>(type: "INTEGER", nullable: false),
                    Supplies = table.Column<long>(type: "INTEGER", nullable: false),
                    Balance = table.Column<long>(type: "INTEGER", nullable: false),
                    Damage = table.Column<long>(type: "INTEGER", nullable: false),
                    Healing = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamHuntMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamHuntMembers_TeamHuntSessions_TeamHuntSessionId",
                        column: x => x.TeamHuntSessionId,
                        principalTable: "TeamHuntSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterActiveImbuements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImbuementProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImbuementProfileId1 = table.Column<int>(type: "INTEGER", nullable: false),
                    ImbuementRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterActiveImbuements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterActiveImbuements_ImbuementProfiles_ImbuementProfileId",
                        column: x => x.ImbuementProfileId,
                        principalTable: "ImbuementProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterActiveImbuements_ImbuementProfiles_ImbuementProfileId1",
                        column: x => x.ImbuementProfileId1,
                        principalTable: "ImbuementProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterActiveImbuements_ImbuementRecipes_ImbuementRecipeId",
                        column: x => x.ImbuementRecipeId,
                        principalTable: "ImbuementRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HuntLootEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HuntSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountKept = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntLootEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntLootEntries_HuntSessions_HuntSessionId",
                        column: x => x.HuntSessionId,
                        principalTable: "HuntSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HuntMonsterEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HuntSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MonsterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntMonsterEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntMonsterEntries_HuntSessions_HuntSessionId",
                        column: x => x.HuntSessionId,
                        principalTable: "HuntSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HuntSupplyAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HuntSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuntSupplyAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HuntSupplyAdjustments_HuntSessions_HuntSessionId",
                        column: x => x.HuntSessionId,
                        principalTable: "HuntSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAccounts_CharacterId",
                table: "CharacterAccounts",
                column: "CharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAchievements_CharacterId_Name",
                table: "CharacterAchievements",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActiveImbuements_ImbuementProfileId",
                table: "CharacterActiveImbuements",
                column: "ImbuementProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActiveImbuements_ImbuementProfileId1",
                table: "CharacterActiveImbuements",
                column: "ImbuementProfileId1");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActiveImbuements_ImbuementRecipeId",
                table: "CharacterActiveImbuements",
                column: "ImbuementRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBadges_CharacterId_Name",
                table: "CharacterBadges",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDeaths_CharacterId_TimeUtc",
                table: "CharacterDeaths",
                columns: new[] { "CharacterId", "TimeUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterGoals_CharacterId",
                table: "CharacterGoals",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHouses_CharacterId_HouseId",
                table: "CharacterHouses",
                columns: new[] { "CharacterId", "HouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name_World",
                table: "Characters",
                columns: new[] { "Name", "World" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSnapshots_CharacterId_FetchedAtUtc",
                table: "CharacterSnapshots",
                columns: new[] { "CharacterId", "FetchedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CreatureLoots_CreatureId_ItemName",
                table: "CreatureLoots",
                columns: new[] { "CreatureId", "ItemName" });

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
                name: "IX_CreatureSounds_CreatureId_Text",
                table: "CreatureSounds",
                columns: new[] { "CreatureId", "Text" });

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPlaceCreatures_CreatureId",
                table: "HuntingPlaceCreatures",
                column: "CreatureId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPlaceLevels_HuntingPlaceId",
                table: "HuntingPlaceLevels",
                column: "HuntingPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntingPlaces_Name",
                table: "HuntingPlaces",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_HuntLootEntries_HuntSessionId",
                table: "HuntLootEntries",
                column: "HuntSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntMonsterEntries_HuntSessionId",
                table: "HuntMonsterEntries",
                column: "HuntSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntSessions_CharacterId",
                table: "HuntSessions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntSessions_HuntGroupId",
                table: "HuntSessions",
                column: "HuntGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_HuntSupplyAdjustments_HuntSessionId",
                table: "HuntSupplyAdjustments",
                column: "HuntSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ImbuementIngredients_ImbuementRecipeId",
                table: "ImbuementIngredients",
                column: "ImbuementRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImbuementIngredients_ItemId",
                table: "ImbuementIngredients",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImbuementProfiles_CharacterId",
                table: "ImbuementProfiles",
                column: "CharacterId");

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
                name: "IX_TeamHuntMembers_TeamHuntSessionId",
                table: "TeamHuntMembers",
                column: "TeamHuntSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserItemPrices_ItemId",
                table: "UserItemPrices",
                column: "ItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterAccounts");

            migrationBuilder.DropTable(
                name: "CharacterAchievements");

            migrationBuilder.DropTable(
                name: "CharacterActiveImbuements");

            migrationBuilder.DropTable(
                name: "CharacterBadges");

            migrationBuilder.DropTable(
                name: "CharacterDeaths");

            migrationBuilder.DropTable(
                name: "CharacterGoals");

            migrationBuilder.DropTable(
                name: "CharacterHouses");

            migrationBuilder.DropTable(
                name: "CharacterSnapshots");

            migrationBuilder.DropTable(
                name: "CreatureDamageModifiers");

            migrationBuilder.DropTable(
                name: "CreatureLoots");

            migrationBuilder.DropTable(
                name: "CreatureSounds");

            migrationBuilder.DropTable(
                name: "HuntingPlaceCreatures");

            migrationBuilder.DropTable(
                name: "HuntingPlaceLevels");

            migrationBuilder.DropTable(
                name: "HuntLootEntries");

            migrationBuilder.DropTable(
                name: "HuntMonsterEntries");

            migrationBuilder.DropTable(
                name: "HuntSupplyAdjustments");

            migrationBuilder.DropTable(
                name: "ImbuementIngredients");

            migrationBuilder.DropTable(
                name: "TeamHuntMembers");

            migrationBuilder.DropTable(
                name: "UserItemPrices");

            migrationBuilder.DropTable(
                name: "ImbuementProfiles");

            migrationBuilder.DropTable(
                name: "Creatures");

            migrationBuilder.DropTable(
                name: "HuntingPlaces");

            migrationBuilder.DropTable(
                name: "HuntSessions");

            migrationBuilder.DropTable(
                name: "ImbuementRecipes");

            migrationBuilder.DropTable(
                name: "TeamHuntSessions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "HuntGroups");
        }
    }
}
