using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace VRCFaceTracking.Core;

public static class SomniumSpace
{
    public static void EnsureVRCOSCDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            VRCOSCDirectory = Path.Combine(
                $"{Environment.GetEnvironmentVariable("localappdata")}Low", "Somnium Space Ltd", "Somnium Space VR", "OSC"
            );
        }
    }

    public static string VRCOSCDirectory { get; private set; }

    /// <summary>
    /// Parse a VDF file into a dictionary structure
    /// </summary>
    /// <param name="vdfContent">Content of the VDF file</param>
    /// <returns>Dictionary representing the VDF structure</returns>
    private static Dictionary<string, object> ParseVdfFile(string vdfContent)
    {
        var parser = new VdfParser(vdfContent);
        return parser.Parse();
    }

    /// <summary>
    /// A simple parser for Valve's VDF format
    /// </summary>
    private class VdfParser
    {
        private readonly string _content;
        private int _position;

        public VdfParser(string content)
        {
            _content = content;
            _position = 0;
        }

        public Dictionary<string, object> Parse()
        {
            SkipWhitespace();
            return ParseObject();
        }

        private Dictionary<string, object> ParseObject()
        {
            var result = new Dictionary<string, object>();

            while (_position < _content.Length)
            {
                SkipWhitespace();

                // Check for end of object
                if (_position < _content.Length && _content[_position] == '}')
                {
                    _position++; // Skip the closing brace
                    break;
                }

                // Parse key
                string key = ParseString();
                if (string.IsNullOrEmpty(key))
                    break;

                SkipWhitespace();

                // Parse value
                object value;

                // Check if the value is an object
                if (_position < _content.Length && _content[_position] == '{')
                {
                    _position++; // Skip the opening brace
                    value = ParseObject();
                }
                else
                {
                    value = ParseString();
                }

                result[key] = value;
            }

            return result;
        }

        private string ParseString()
        {
            SkipWhitespace();

            // Check for quoted string
            if (_position < _content.Length && _content[_position] == '"')
            {
                _position++; // Skip opening quote

                var startPos = _position;

                // Find the closing quote
                while (_position < _content.Length && _content[_position] != '"')
                {
                    // Handle escaped characters
                    if (_content[_position] == '\\' && _position + 1 < _content.Length)
                    {
                        _position += 2; // Skip the escape sequence
                    }
                    else
                    {
                        _position++;
                    }
                }

                if (_position < _content.Length)
                {
                    var result = _content.Substring(startPos, _position - startPos);
                    _position++; // Skip closing quote
                    return result;
                }
            }

            return string.Empty;
        }

        private void SkipWhitespace()
        {
            while (_position < _content.Length)
            {
                char c = _content[_position];

                if (char.IsWhiteSpace(c) || c == '\r' || c == '\n' || c == '\t')
                {
                    _position++;
                }
                else if (c == '/' && _position + 1 < _content.Length && _content[_position + 1] == '/')
                {
                    // Skip single line comments
                    _position += 2;
                    while (_position < _content.Length && _content[_position] != '\n')
                    {
                        _position++;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    [SupportedOSPlatform("windows")]
    public static bool ForceEnableOsc()
    {
        // Set all registry keys containing osc in the name to 1 in Computer\HKEY_CURRENT_USER\Software\VRChat\VRChat
        var regKey = Registry.CurrentUser.OpenSubKey("Software\\VRChat\\VRChat", true);
        if (regKey == null)
            return true;    // Assume we already have osc enabled

        var keys = regKey.GetValueNames().Where(x => x.StartsWith("VRC_INPUT_OSC") || x.StartsWith("UI.Settings.Osc"));

        var wasOscForced = false;
        foreach (var key in keys)
        {
            if ((int) regKey.GetValue(key) == 0)
            {
                // Osc is likely not enabled
                regKey.SetValue(key, 1);
                wasOscForced = true;
            }
        }

        return wasOscForced;
    }

    public static bool IsSomniumSpaceRunning() => Process.GetProcesses().Any(x => x.ProcessName == "Somnium Space VR");
}
