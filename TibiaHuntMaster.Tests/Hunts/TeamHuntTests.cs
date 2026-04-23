using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;
using TibiaHuntMaster.Infrastructure.Services.Parsing;

// Wichtig für Tests

namespace TibiaHuntMaster.Tests.Hunts
{
    public sealed class TeamHuntTests
    {
        // Beispiel 1: 4er Gruppe
        private const string input4Pl = """
                                        Session data: From 2020-10-06, 20:29:08 to 2020-10-06, 23:49:29
                                        Session: 03:20h
                                        Loot Type: Leader
                                        Loot: 5,193,564
                                        Supplies: 2,133,891
                                        Balance: 3,059,673
                                        Mister Pilsner
                                            Loot: 334,624
                                            Supplies: 442,043
                                            Balance: -107,419
                                            Damage: 8,313,942
                                            Healing: 364,530
                                        Phacee
                                            Loot: 37,455
                                            Supplies: 907,007
                                            Balance: -869,552
                                            Damage: 8,079,133
                                            Healing: 3,899,558
                                        Scorpsgirl (Leader)
                                            Loot: 2,378,141
                                            Supplies: 314,706
                                            Balance: 2,063,435
                                            Damage: 4,244,731
                                            Healing: 91,079
                                        Talim
                                            Loot: 2,443,344
                                            Supplies: 470,135
                                            Balance: 1,973,209
                                            Damage: 6,974,666
                                            Healing: 1,130,015
                                        """;

        // Beispiel 2: Duo mit Waste
        private const string inputDuo = """
                                        Session data: From 2020-09-09, 19:12:33 to 2020-09-09, 19:29:16
                                        Session: 00:16h
                                        Loot Type: Market
                                        Loot: 295,770
                                        Supplies: 104,813
                                        Balance: 190,957
                                        Mister Pilsner (Leader)
                                            Loot: 58,761
                                            Supplies: 47,619
                                            Balance: 11,142
                                            Damage: 662,628
                                            Healing: 64,271
                                        Talim
                                            Loot: 237,009
                                            Supplies: 57,194
                                            Balance: 179,815
                                            Damage: 855,593
                                            Healing: 176,005
                                        """;

        [Fact(DisplayName = "👥 Parser: Reads 4-Player Team Hunt correctly")]
        public void Parser_Reads_4Player_Team()
        {
            TeamHuntParser parser = new(NullLogger<TeamHuntParser>.Instance);

            // UPDATE: Signatur (Dummy ID 0, out error)
            bool success = parser.TryParse(input4Pl, 0, out TeamHuntSessionEntity? session, out string _);

            success.Should().BeTrue();
            session!.Members.Should().HaveCount(4);

            // Header Checks
            session.Duration.TotalMinutes.Should().Be(200); // 3h 20m
            session.LootType.Should().Be("Leader");
            session.TotalBalance.Should().Be(3_059_673);

            // Member Checks
            TeamHuntMemberEntity? leader = session.Members.FirstOrDefault(m => m.IsLeader);
            leader.Should().NotBeNull();
            leader!.Name.Should().Be("Scorpsgirl");
            leader.Damage.Should().Be(4_244_731);

            TeamHuntMemberEntity talim = session.Members.First(m => m.Name == "Talim");
            talim.Balance.Should().Be(1_973_209);

            // Negative Balance Check
            TeamHuntMemberEntity phacee = session.Members.First(m => m.Name == "Phacee");
            phacee.Balance.Should().Be(-869_552);
        }

        [Fact(DisplayName = "👥 Parser: Reads Duo Hunt correctly")]
        public void Parser_Reads_Duo_Team()
        {
            TeamHuntParser parser = new(NullLogger<TeamHuntParser>.Instance);

            // UPDATE: Signatur
            bool success = parser.TryParse(inputDuo, 0, out TeamHuntSessionEntity? session, out string _);

            success.Should().BeTrue();
            session!.Members.Should().HaveCount(2);

            session.LootType.Should().Be("Market");
            session.TotalLoot.Should().Be(295_770);

            TeamHuntMemberEntity leader = session.Members.First(m => m.IsLeader);
            leader.Name.Should().Be("Mister Pilsner");
            leader.Damage.Should().Be(662_628);
        }

        [Fact(DisplayName = "👥 Parser: Truncates overlong member names safely")]
        public void Parser_Truncates_Overlong_MemberNames()
        {
            string longName = new('X', UserInputLimits.TeamMemberNameMaxLength + 25);
            string input = $"""
                            Session data: From 2020-09-09, 19:12:33 to 2020-09-09, 19:29:16
                            Session: 00:16h
                            Loot Type: Leader
                            Loot: 295,770
                            Supplies: 104,813
                            Balance: 190,957
                            {longName} (Leader)
                                Loot: 58,761
                                Supplies: 47,619
                                Balance: 11,142
                                Damage: 662,628
                                Healing: 64,271
                            """;

            TeamHuntParser parser = new(NullLogger<TeamHuntParser>.Instance);
            bool success = parser.TryParse(input, 0, out TeamHuntSessionEntity? session, out string _);

            success.Should().BeTrue();
            session.Should().NotBeNull();
            session!.Members.Should().HaveCount(1);
            session.Members[0].Name.Length.Should().Be(UserInputLimits.TeamMemberNameMaxLength);
        }

        [Fact(DisplayName = "🌍 Parser: Reads localized (DE) team hunt labels")]
        public void Parser_Reads_Localized_German_Labels()
        {
            string localizedInput = """
                                    Sitzungsdaten: Von 2020-10-06, 20:29:08 bis 2020-10-06, 23:49:29
                                    Sitzung: 03:20h
                                    Beutetyp: Leader
                                    Beute: 5,193,564
                                    Vorräte: 2,133,891
                                    Bilanz: 3,059,673
                                    Scorpsgirl (Anführer)
                                        Beute: 2,378,141
                                        Vorräte: 314,706
                                        Bilanz: 2,063,435
                                        Schaden: 4,244,731
                                        Heilung: 91,079
                                    """;

            TeamHuntParser parser = new(NullLogger<TeamHuntParser>.Instance);
            bool success = parser.TryParse(localizedInput, 0, out TeamHuntSessionEntity? session, out string error);

            success.Should().BeTrue(error);
            session.Should().NotBeNull();
            session!.Duration.TotalMinutes.Should().Be(200);
            session.LootType.Should().Be("Leader");
            session.TotalLoot.Should().Be(5_193_564);
            session.TotalSupplies.Should().Be(2_133_891);
            session.TotalBalance.Should().Be(3_059_673);
            session.Members.Should().HaveCount(1);
            session.Members[0].IsLeader.Should().BeTrue();
            session.Members[0].Name.Should().Be("Scorpsgirl");
            session.Members[0].Damage.Should().Be(4_244_731);
            session.Members[0].Healing.Should().Be(91_079);
        }
    }
}
