using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace NetBank.Controllers.HttpController;


/// <summary>
/// Binds AspNetCore routing feature to all controllers of specific
/// type and its derivatives
/// </summary>
public sealed class DerivedControllerFeatureProvider
    : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly Type _baseType;
    
    public DerivedControllerFeatureProvider(Type baseType)
    {
        _baseType = baseType;
    }

    public void PopulateFeature(
        IEnumerable<ApplicationPart> parts,
        ControllerFeature feature)
    {
        feature.Controllers.Clear();

        var controllerTypes = parts
            .OfType<AssemblyPart>()
            .SelectMany(p => p.Assembly.GetTypes())
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                _baseType.IsAssignableFrom(t));

        foreach (var type in controllerTypes)
        {
            feature.Controllers.Add(type.GetTypeInfo());
        }
    }
}