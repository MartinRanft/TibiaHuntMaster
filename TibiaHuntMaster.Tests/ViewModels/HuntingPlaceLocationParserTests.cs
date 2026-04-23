using FluentAssertions;

using TibiaHuntMaster.App.Services.Map;

namespace TibiaHuntMaster.Tests.ViewModels
{
    public sealed class HuntingPlaceLocationParserTests
    {
        [Fact]
        public void Parse_ShouldExtractMapperCoords_WithStandardTokenOrder()
        {
            const string location = "A bit north-west of Venore, {{Mapper Coords|128.85|124.180|7|3|text=here}}.";

            HuntingPlaceLocationParseResult result = HuntingPlaceLocationParser.Parse(location);

            result.Coordinates.Should().HaveCount(1);
            result.Coordinates[0].X.Should().Be(32853);
            result.Coordinates[0].Y.Should().Be(31924);
            result.Coordinates[0].Z.Should().Be(7);
            result.CleanedLocation.Should().NotContain("Mapper Coords");
            result.CleanedLocation.Should().Contain("north-west of Venore");
        }

        [Fact]
        public void Parse_ShouldExtractMapperCoords_WithTextFirstFormat()
        {
            const string location = "Northeast of Feyrist, {{Mapper Coords|text=here|131.157|125.207|7|2|1|0.50.5}}.";

            HuntingPlaceLocationParseResult result = HuntingPlaceLocationParser.Parse(location);

            result.Coordinates.Should().HaveCount(1);
            result.Coordinates[0].X.Should().Be(33693);
            result.Coordinates[0].Y.Should().Be(32207);
            result.Coordinates[0].Z.Should().Be(7);
        }

        [Fact]
        public void Parse_ShouldExtractLegacyUrlCoordinates_WithCommaSeparator()
        {
            const string location = "South of Roshamuul Depot, [http://tibia.wikia.com/wiki/Mapper?coords=131.52,126.177,7 here].";

            HuntingPlaceLocationParseResult result = HuntingPlaceLocationParser.Parse(location);

            result.Coordinates.Should().HaveCount(1);
            result.Coordinates[0].X.Should().Be(33588);
            result.Coordinates[0].Y.Should().Be(32433);
            result.Coordinates[0].Z.Should().Be(7);
            result.CleanedLocation.Should().Contain("South of Roshamuul Depot");
            result.CleanedLocation.Should().NotContain("http");
        }

        [Fact]
        public void Parse_ShouldExtractLegacyUrlCoordinates_WithHyphenSeparatorAndExtraParameters()
        {
            const string location = "Door [http://tibia.wikia.com/wiki/Mapper?coords=131.31-124.206-14-2-1.5-1 here].";

            HuntingPlaceLocationParseResult result = HuntingPlaceLocationParser.Parse(location);

            result.Coordinates.Should().HaveCount(1);
            result.Coordinates[0].X.Should().Be(33567);
            result.Coordinates[0].Y.Should().Be(31950);
            result.Coordinates[0].Z.Should().Be(14);
        }

        [Fact]
        public void Parse_ShouldExtractMultipleCoordinates_AndKeepOrder()
        {
            const string location = "[[Zao]], {{Mapper Coords|129.169|122.241|7|3|text=here}}, {{Mapper Coords|129.95|122.225|7|3|text=here}} and {{Mapper Coords|129.77|122.157|7|3|text=here}}.";

            HuntingPlaceLocationParseResult result = HuntingPlaceLocationParser.Parse(location);

            result.Coordinates.Should().HaveCount(3);
            result.Coordinates[0].Display.Should().Be("129.169,122.241,7");
            result.Coordinates[1].Display.Should().Be("129.95,122.225,7");
            result.Coordinates[2].Display.Should().Be("129.77,122.157,7");
        }
    }
}
