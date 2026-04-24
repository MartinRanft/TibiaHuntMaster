using Avalonia.Platform;

using TibiaHuntMaster.Core.Abstractions.Map;

namespace TibiaHuntMaster.App.Services.Map
{
    public sealed class AvaresMinimapMarkerService : IMinimapMarkerService
    {
        private readonly Lazy<IReadOnlyList<Core.Map.Map.MinimapMarker>> _markers;
        private readonly Lazy<IReadOnlyDictionary<byte, IReadOnlyList<Core.Map.Map.MinimapMarker>>> _markersByZ;

        public AvaresMinimapMarkerService()
        {
            _markers = new Lazy<IReadOnlyList<Core.Map.Map.MinimapMarker>>(LoadMarkers, isThreadSafe: true);
            _markersByZ = new Lazy<IReadOnlyDictionary<byte, IReadOnlyList<Core.Map.Map.MinimapMarker>>>(BuildMarkersByZ, isThreadSafe: true);
        }

        public IReadOnlyList<Core.Map.Map.MinimapMarker> GetAllMarkers()
        {
            return _markers.Value;
        }

        public IReadOnlyList<Core.Map.Map.MinimapMarker> GetMarkersInBounds(
            int minX,
            int minY,
            int maxX,
            int maxY,
            byte z)
        {
            if (!_markersByZ.Value.TryGetValue(z, out IReadOnlyList<Core.Map.Map.MinimapMarker>? levelMarkers))
            {
                return Array.Empty<Core.Map.Map.MinimapMarker>();
            }

            List<Core.Map.Map.MinimapMarker> visible = new();
            int i = 0;
            while (i < levelMarkers.Count)
            {
                Core.Map.Map.MinimapMarker marker = levelMarkers[i];
                i += 1;

                if (marker.X < minX || marker.X > maxX || marker.Y < minY || marker.Y > maxY)
                {
                    continue;
                }

                visible.Add(marker);
            }

            return visible;
        }

        private static IReadOnlyList<Core.Map.Map.MinimapMarker> LoadMarkers()
        {
            Uri uri = new Uri("avares://TibiaHuntMaster.App/Assets/Map/minimapmarkers.bin");

            using System.IO.Stream stream = AssetLoader.Open(uri);
            using System.IO.MemoryStream ms = new System.IO.MemoryStream();
            stream.CopyTo(ms);

            byte[] data = ms.ToArray();
            return ParseAll(data);
        }

        private IReadOnlyDictionary<byte, IReadOnlyList<Core.Map.Map.MinimapMarker>> BuildMarkersByZ()
        {
            Dictionary<byte, List<Core.Map.Map.MinimapMarker>> grouped = new();

            IReadOnlyList<Core.Map.Map.MinimapMarker> all = _markers.Value;
            int i = 0;
            while (i < all.Count)
            {
                Core.Map.Map.MinimapMarker marker = all[i];
                i += 1;

                if (!grouped.TryGetValue(marker.Z, out List<Core.Map.Map.MinimapMarker>? list))
                {
                    list = new List<Core.Map.Map.MinimapMarker>();
                    grouped[marker.Z] = list;
                }

                list.Add(marker);
            }

            Dictionary<byte, IReadOnlyList<Core.Map.Map.MinimapMarker>> readonlyGrouped = new();
            foreach ((byte z, List<Core.Map.Map.MinimapMarker> list) in grouped)
            {
                readonlyGrouped[z] = list;
            }

            return readonlyGrouped;
        }

        private static IReadOnlyList<Core.Map.Map.MinimapMarker> ParseAll(byte[] data)
        {
            List<Core.Map.Map.MinimapMarker> result = new List<Core.Map.Map.MinimapMarker>(capacity: 8000);

            int index = 0;
            while (index < data.Length)
            {
                // Each marker starts with 0x0A, then a varint length, then the message bytes. :contentReference[oaicite:2]{index=2}
                if (data[index] != 0x0A)
                {
                    break;
                }

                index += 1;

                uint length = ReadVarint(data, ref index);
                int msgLen = (int)length;

                if (msgLen <= 0 || (index + msgLen) > data.Length)
                {
                    break;
                }

                ReadOnlySpan<byte> msg = new ReadOnlySpan<byte>(data, index, msgLen);
                index += msgLen;

                if (TryParseMarker(msg, out Core.Map.Map.MinimapMarker marker))
                {
                    result.Add(marker);
                }
            }

            return result;
        }

        private static bool TryParseMarker(ReadOnlySpan<byte> msg, out Core.Map.Map.MinimapMarker marker)
        {
            // We parse protobuf-like fields:
            // field 1 (len-delimited): position submessage (x,y,z)
            // field 2 (varint): icon id (0..19)
            // field 3 (len-delimited): text (utf-8)
            int x = 0;
            int y = 0;
            byte z = 0;
            byte iconId = 0;
            string text = string.Empty;

            int i = 0;
            while (i < msg.Length)
            {
                uint tag = ReadVarint(msg, ref i);
                int fieldNumber = (int)(tag >> 3);
                int wireType = (int)(tag & 0x07);

                if (wireType == 0)
                {
                    uint value = ReadVarint(msg, ref i);

                    if (fieldNumber == 2)
                    {
                        iconId = (byte)value;
                    }

                    continue;
                }

                if (wireType == 2)
                {
                    uint len = ReadVarint(msg, ref i);
                    int l = (int)len;

                    if (l < 0 || (i + l) > msg.Length)
                    {
                        break;
                    }

                    ReadOnlySpan<byte> payload = msg.Slice(i, l);
                    i += l;

                    if (fieldNumber == 1)
                    {
                        TryParsePosition(payload, out x, out y, out z);
                    }
                    else if (fieldNumber == 3)
                    {
                        text = System.Text.Encoding.UTF8.GetString(payload);
                    }

                    continue;
                }

                // Unsupported wire type for this file
                break;
            }

            marker = new Core.Map.Map.MinimapMarker(x, y, z, iconId, text);
            return true;
        }

        private static bool TryParsePosition(ReadOnlySpan<byte> msg, out int x, out int y, out byte z)
        {
            x = 0;
            y = 0;
            z = 0;

            int i = 0;
            while (i < msg.Length)
            {
                uint tag = ReadVarint(msg, ref i);
                int fieldNumber = (int)(tag >> 3);
                int wireType = (int)(tag & 0x07);

                if (wireType != 0)
                {
                    return false;
                }

                uint value = ReadVarint(msg, ref i);

                if (fieldNumber == 1) x = (int)value;
                else if (fieldNumber == 2) y = (int)value;
                else if (fieldNumber == 3) z = (byte)value;
            }

            return true;
        }

        private static uint ReadVarint(ReadOnlySpan<byte> data, ref int index)
        {
            uint result = 0;
            int shift = 0;

            while (index < data.Length)
            {
                byte b = data[index];
                index += 1;

                result |= (uint)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                {
                    return result;
                }

                shift += 7;

                if (shift > 28)
                {
                    break;
                }
            }

            return result;
        }
    }
}
