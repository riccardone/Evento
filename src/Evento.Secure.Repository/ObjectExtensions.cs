using System.ComponentModel;
using System.Dynamic;

namespace Evento.Secure.Repository;
internal static class ObjectExtensions
{
    public static dynamic ToDynamic(this object value)
    {
        IDictionary<string, object> expando = new ExpandoObject();

        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
            expando.Add(property.Name, property.GetValue(value));

        return expando as ExpandoObject;
    }
}