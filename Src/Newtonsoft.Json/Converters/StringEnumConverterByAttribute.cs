#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Globalization;
using Newtonsoft.Json.Utilities;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace Newtonsoft.Json.Converters
{
  /// <summary>
  /// Converts an <see cref="Enum"/> to and from its name string value.
  /// </summary>
  public class StringEnumConverterByAttribute : JsonConverter
  {
    static Dictionary<Type, IDictionary<string, Enum>> cache = new Dictionary<Type, IDictionary<string, Enum>>();
    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
    /// <param name="value">The value.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      if (value == null)
      {
        writer.WriteNull();
        return;
      }

      Enum e = (Enum)value;
      string enumName = e.ToString("G");
      Type enumType = e.GetType();
      JsonValueAttribute attribute = CachedAttributeGetter<JsonValueAttribute>.GetAttribute(enumType.GetField(enumName));

      if (attribute != null)
        enumName = attribute.Value;

      if (char.IsNumber(enumName[0]) || enumName[0] == '-')
        writer.WriteValue(value);
      else
        writer.WriteValue(enumName);
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>The object value.</returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      Type t = (ReflectionUtils.IsNullableType(objectType))
        ? Nullable.GetUnderlyingType(objectType)
        : objectType;

      if (reader.TokenType == JsonToken.Null)
      {
        if (!ReflectionUtils.IsNullableType(objectType))
          throw new Exception("Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));

        return null;
      }

      if (reader.TokenType == JsonToken.String)
      {
        foreach (var kv in GetNamesAndValues(objectType))
        {
          if (string.Compare((string)reader.Value, kv.Key, true, CultureInfo.InvariantCulture) == 0)
            return kv.Value;
        }
        return Enum.Parse(objectType, (string)reader.Value, true);
      }

      if (reader.TokenType == JsonToken.Integer)
        return ConvertUtils.ConvertOrCast(reader.Value, CultureInfo.InvariantCulture, t);

      throw new Exception("Unexpected token when parsing enum. Expected String or Integer, got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
    }

    public static IDictionary<string, Enum> GetNamesAndValues(Type enumType)
    {
      if (!enumType.IsEnum)
        throw new ArgumentException("Type '" + enumType.Name + "' is not an enum.");

      if (cache.ContainsKey(enumType))
        return cache[enumType];

      Dictionary<string, Enum> values = new Dictionary<string, Enum>();

      var fields = enumType.GetFields();

      foreach (FieldInfo field in fields)
      {
        if (!field.IsLiteral)
          continue;

        var attr = CachedAttributeGetter<JsonValueAttribute>.GetAttribute(field);

        if (attr == null)
          values.Add(field.Name, (Enum)field.GetValue(null));
        else
          values.Add(attr.Value, (Enum)field.GetValue(null));
      }

      cache.Add(enumType, values);

      return values;
    }

    /// <summary>
    /// Determines whether this instance can convert the specified object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>
    /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type objectType)
    {
      Type t = (ReflectionUtils.IsNullableType(objectType))
        ? Nullable.GetUnderlyingType(objectType)
        : objectType;

      return t.IsEnum;
    }
  }
}