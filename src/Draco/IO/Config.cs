using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO;

public class Config
{
    private Dictionary<string, object> _configValues { get; set; } = [];
    private Dictionary<int, Dictionary<string, object>> _attributeConfigValues { get; set; } = [];

    /// <summary>
    /// The maximum speed for both encoding/decoding.
    /// </summary>
    public int Speed
    {
        get
        {
            var maxSpeed = Math.Max(GetOption(ConfigOptionName.EncodingSpeed, -1), GetOption(ConfigOptionName.DecodingSpeed, -1));
            return maxSpeed == -1 ? Constants.DefaultSpeed : maxSpeed;
        }
        set
        {
            SetOption(ConfigOptionName.EncodingSpeed, value);
            SetOption(ConfigOptionName.DecodingSpeed, value);
        }
    }

    public T GetOption<T>(string name, T defaultValue)
    {
        return _configValues.ContainsKey(name) ? (T)_configValues[name] : defaultValue;
    }

    public T[] GetOptionValues<T>(string name, int count)
    {
        return _configValues.ContainsKey(name) ? (T[])_configValues[name] : [];
    }

    public void SetOption<T>(string name, T value)
    {
        if (_configValues.ContainsKey(name))
        {
            _configValues[name] = value!;
        }
        else
        {
            _configValues.Add(name, value!);
        }
    }

    public bool IsOptionSet(string key)
    {
        return _configValues.ContainsKey(key);
    }

    public T GetAttributeOption<T>(int attributeKey, string name, T defaultValue)
    {
        if (_attributeConfigValues.ContainsKey(attributeKey))
        {
            return _attributeConfigValues[attributeKey].ContainsKey(name) ? (T)_attributeConfigValues[attributeKey][name] : defaultValue;
        }
        return GetOption(name, defaultValue);
    }

    public T[] GetAttributeOptionValues<T>(int attributeKey, string name, int count)
    {
        if (_attributeConfigValues.ContainsKey(attributeKey))
        {
            return _attributeConfigValues[attributeKey].ContainsKey(name) ? (T[])_attributeConfigValues[attributeKey][name] : [];
        }
        return GetOptionValues<T>(name, count);
    }

    public void SetAttributeOption<T>(int attributeKey, string name, T value)
    {
        if (_attributeConfigValues.ContainsKey(attributeKey))
        {
            if (_attributeConfigValues[attributeKey].ContainsKey(name))
            {
                _attributeConfigValues[attributeKey][name] = value!;
            }
            else
            {
                _attributeConfigValues[attributeKey].Add(name, value!);
            }
        }
        else
        {
            _attributeConfigValues.Add(attributeKey, new Dictionary<string, object> { { name, value! } });
        }
    }

    public bool IsAttributeOptionSet(int attributeKey, string name)
    {
        return (_attributeConfigValues.ContainsKey(attributeKey) && _attributeConfigValues[attributeKey].ContainsKey(name)) || IsOptionSet(name);
    }

    public void SetSymbolEncodingCompressionLevel(int compressionLevel)
    {
        Assertions.ThrowIf(compressionLevel < 0 || compressionLevel > 10, "Invalid compression level");
        SetOption(ConfigOptionName.SymbolEncodingCompressionLevel, compressionLevel);
    }
}
