using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Newtonsoft.Json
{
  [global::System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
  public sealed class JsonValueAttribute : Attribute
  {
    readonly string value;

    public JsonValueAttribute(string value)
    {
      this.value = value;
    }

    public string Value
    {
      get { return value; }
    }
  }
}
