using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using PacketDotNet;

namespace AsterixReader.Backend.Services;

public class DisplayFilterEvaluator
{
    private readonly string _filterExpression;

    public DisplayFilterEvaluator(string filterExpression)
    {
        _filterExpression = filterExpression?.Trim() ?? string.Empty;
    }

    public bool Matches(Packet packet)
    {
        if (string.IsNullOrEmpty(_filterExpression))
            return true;

        try
        {
            return EvaluateExpression(_filterExpression, packet);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating display filter '{_filterExpression}': {ex.Message}");
            return false;
        }
    }

    private bool EvaluateExpression(string expression, Packet packet)
    {
        expression = expression.Trim();
        
        // Handle parentheses
        if (expression.StartsWith("(") && expression.EndsWith(")"))
        {
            var inner = expression.Substring(1, expression.Length - 2).Trim();
            return EvaluateExpression(inner, packet);
        }

        // Handle logical NOT
        if (expression.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
        {
            var inner = expression.Substring(4).Trim();
            return !EvaluateExpression(inner, packet);
        }

        // Handle OR operator (lower precedence)
        var orMatch = Regex.Match(expression, @"\s+or\s+", RegexOptions.IgnoreCase);
        if (orMatch.Success)
        {
            var left = expression.Substring(0, orMatch.Index).Trim();
            var right = expression.Substring(orMatch.Index + orMatch.Length).Trim();
            return EvaluateExpression(left, packet) || EvaluateExpression(right, packet);
        }

        // Handle AND operator
        var andMatch = Regex.Match(expression, @"\s+and\s+", RegexOptions.IgnoreCase);
        if (andMatch.Success)
        {
            var left = expression.Substring(0, andMatch.Index).Trim();
            var right = expression.Substring(andMatch.Index + andMatch.Length).Trim();
            return EvaluateExpression(left, packet) && EvaluateExpression(right, packet);
        }

        // Evaluate simple comparison
        return EvaluateComparison(expression, packet);
    }

    private bool EvaluateComparison(string expression, Packet packet)
    {
        // Match comparison operators: ==, !=, >, <, >=, <=
        var match = Regex.Match(expression, @"(.+?)\s*(==|!=|>=|<=|>|<)\s*(.+)");
        if (!match.Success)
        {
            // Try to evaluate as a boolean field (e.g., "tcp.flags.syn")
            return EvaluateField(expression, packet) != null;
        }

        var fieldName = match.Groups[1].Value.Trim();
        var op = match.Groups[2].Value.Trim();
        var valueStr = match.Groups[3].Value.Trim().Trim('"', '\'');

        var fieldValue = EvaluateField(fieldName, packet);
        if (fieldValue == null)
            return false;

        var fieldStr = fieldValue.ToString() ?? "";

        // Special handling for ip.addr (matches either src or dst)
        if (fieldStr.StartsWith("SRC|DST|"))
        {
            // Extract src and dst from the special format
            var parts = fieldStr.Split('|');
            if (parts.Length >= 4)
            {
                var src = parts[2];
                var dst = parts[3];
                // For ip.addr, match if either src or dst matches
                if (op == "==")
                    return src.Equals(valueStr, StringComparison.OrdinalIgnoreCase) ||
                           dst.Equals(valueStr, StringComparison.OrdinalIgnoreCase);
                if (op == "!=")
                    return !(src.Equals(valueStr, StringComparison.OrdinalIgnoreCase) ||
                            dst.Equals(valueStr, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        // Try to parse value as number
        if (double.TryParse(valueStr, out double numValue))
        {
            if (double.TryParse(fieldStr, out double fieldNum))
            {
                return CompareNumbers(fieldNum, op, numValue);
            }
        }

        // Compare as strings
        return CompareStrings(fieldStr, op, valueStr);
    }

    private object? EvaluateField(string fieldName, Packet packet)
    {
        fieldName = fieldName.Trim().ToLower();

        // UDP fields
        if (fieldName.StartsWith("udp."))
        {
            var udpPacket = packet.Extract<UdpPacket>();
            if (udpPacket == null) return null;

            var field = fieldName.Substring(4);
            return field switch
            {
                "port" => udpPacket.DestinationPort,
                "dstport" => udpPacket.DestinationPort,
                "srcport" => udpPacket.SourcePort,
                "length" => udpPacket.Length,
                "len" => udpPacket.Length,
                _ => null
            };
        }

        // TCP fields
        if (fieldName.StartsWith("tcp."))
        {
            var tcpPacket = packet.Extract<TcpPacket>();
            if (tcpPacket == null) return null;

            var field = fieldName.Substring(4);
            if (field.StartsWith("flags."))
            {
                var flagName = field.Substring(6);
                var flags = (byte)tcpPacket.Flags;
                // TCP flags are bits: URG=32, ACK=16, PSH=8, RST=4, SYN=2, FIN=1
                return flagName switch
                {
                    "syn" => (flags & 0x02) != 0,
                    "ack" => (flags & 0x10) != 0,
                    "fin" => (flags & 0x01) != 0,
                    "rst" => (flags & 0x04) != 0,
                    "psh" => (flags & 0x08) != 0,
                    "urg" => (flags & 0x20) != 0,
                    _ => null
                };
            }

            return field switch
            {
                "port" => tcpPacket.DestinationPort,
                "dstport" => tcpPacket.DestinationPort,
                "srcport" => tcpPacket.SourcePort,
                "len" => tcpPacket.DataOffset * 4,
                _ => null
            };
        }

        // IP fields
        if (fieldName.StartsWith("ip."))
        {
            var ipPacket = packet.Extract<IPPacket>();
            if (ipPacket == null) return null;

            var field = fieldName.Substring(3);
            if (field == "src")
            {
                return ipPacket.SourceAddress.ToString();
            }
            if (field == "dst")
            {
                return ipPacket.DestinationAddress.ToString();
            }
            if (field == "addr")
            {
                // For "ip.addr", return a special marker that will be handled in comparison
                // Format: "SRC|DST"
                return $"SRC|DST|{ipPacket.SourceAddress}|{ipPacket.DestinationAddress}";
            }
            if (field == "len" || field == "length")
            {
                return ipPacket.TotalLength;
            }
            if (field == "ttl")
            {
                return ipPacket.TimeToLive;
            }
            if (field == "proto" || field == "protocol")
            {
                return ipPacket.Protocol.ToString().ToLower();
            }
        }

        // Protocol checks
        if (fieldName == "udp")
        {
            return packet.Extract<UdpPacket>() != null;
        }
        if (fieldName == "tcp")
        {
            return packet.Extract<TcpPacket>() != null;
        }
        if (fieldName == "ip")
        {
            return packet.Extract<IPPacket>() != null;
        }

        return null;
    }

    private bool CompareNumbers(double fieldValue, string op, double compareValue)
    {
        return op switch
        {
            "==" => Math.Abs(fieldValue - compareValue) < 0.0001,
            "!=" => Math.Abs(fieldValue - compareValue) >= 0.0001,
            ">" => fieldValue > compareValue,
            "<" => fieldValue < compareValue,
            ">=" => fieldValue >= compareValue,
            "<=" => fieldValue <= compareValue,
            _ => false
        };
    }

    private bool CompareStrings(string fieldValue, string op, string compareValue)
    {
        return op switch
        {
            "==" => fieldValue.Equals(compareValue, StringComparison.OrdinalIgnoreCase),
            "!=" => !fieldValue.Equals(compareValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public static bool IsDisplayFilterSyntax(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return false;

        // Display filters typically contain:
        // - Field names with dots (udp.port, ip.src)
        // - Comparison operators (==, !=, >, <)
        // - Logical operators (and, or, not)
        
        var hasDotField = Regex.IsMatch(filter, @"\w+\.\w+");
        var hasComparison = Regex.IsMatch(filter, @"(==|!=|>=|<=|>|<)");
        var hasLogical = Regex.IsMatch(filter, @"\s+(and|or|not)\s+", RegexOptions.IgnoreCase);

        // If it has dot fields or comparison operators, it's likely display filter syntax
        return hasDotField || (hasComparison && !hasDotField && !filter.Contains(" port "));
    }
}

