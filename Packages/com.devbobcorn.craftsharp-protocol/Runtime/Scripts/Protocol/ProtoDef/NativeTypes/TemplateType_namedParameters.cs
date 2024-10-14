#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CraftSharp.Protocol.ProtoDef.NativeTypes
{
    /// <summary>
    /// Template for type definitions which accepts a json dictionary as type
    /// parameter, including "switch", "array", "buffer", "mapper" and "pstring"
    /// </summary>
    public abstract class TemplateType_namedParameters<T> : PacketDefTypeHandler<T>
    {
        /// <summary>
        /// Parameter name => inheriting parameter name (name used by inheriting types for specifying this value)
        /// <br/>
        /// Note that a single inheriting parameter MIGHT be providing value for multiple parameters.
        /// </summary>
        public readonly Dictionary<string, string> UnresolvedParameters = new();

        /// <summary>
        /// Parameter name => parameter value
        /// </summary>
        public readonly Dictionary<string, JToken> ResolvedParameters = new();

        protected bool ContainsUnresolvedParameters => UnresolvedParameters.Count > 0;

        /// <summary>
        /// Find unspecified parameters whose value is not yet determined, returns true
        /// if such parameters exist. To "determine" the value of a variable parameter
        /// another type is needed to use this type as its underlying type, and then
        /// specify the value there to the name of this variable parameter.
        /// <br/>
        /// Example here:
        /// https://github.com/PrismarineJS/minecraft-data/blob/master/data/pc/1.16.2/protocol.json#L73-L147
        /// </summary>
        protected bool InheritParameters(TemplateType_namedParameters<T>? inheritedDef,
            string[] neededParamNames, Dictionary<string, JToken> paramsAsDict)
        {
            if (inheritedDef is not null)
            {
                var inheritedResolvedParameters = inheritedDef.ResolvedParameters;
                var inheritedUnresolvedParameters = inheritedDef.UnresolvedParameters;

                foreach (var r in inheritedResolvedParameters)
                {
                    ResolvedParameters.Add(r.Key, r.Value);
                }

                foreach (var r in inheritedUnresolvedParameters)
                {
                    UnresolvedParameters.Add(r.Key, r.Value);
                }
            }

            void AddParam(string paramName, JToken paramValue)
            {
                if (paramValue is not null && paramValue.Type == JTokenType.String)
                {
                    var paramValueAsString = paramValue.ToString();

                    if (paramValueAsString.StartsWith('$')) // Got a parameter here to be resolved
                    {
                        // Parameter name => inheriting parameter name
                        UnresolvedParameters.Add(paramName, paramValueAsString[1..]); // Remove the leading '$'
                    }
                    else // Regular literal value, just add
                    {
                        ResolvedParameters.Add(paramName, paramValue);
                    }
                }
                else // Regular literal value, just add
                {
                    ResolvedParameters.Add(paramName, paramValue!);
                }
            }

            foreach (var (paramName, paramValue) in paramsAsDict)
            {
                if (UnresolvedParameters.ContainsValue(paramName)) // If this entry is for specifying value for unresolved parameter(s)
                {
                    var resolvedEntryKeys = UnresolvedParameters.Where(x => x.Value == paramName)
                        .Select(x => x.Key).ToList();

                    foreach (var resolvedEntryKey in resolvedEntryKeys)
                    {
                        // The param value here might be another unresolved param name for
                        // whichever type inherits THIS type to resolve.
                        AddParam(resolvedEntryKey, paramValue);

                        UnresolvedParameters.Remove(resolvedEntryKey);
                    }
                }
                else if (neededParamNames.Contains(paramName))
                {
                    // Regular parameter with literal value
                    AddParam(paramName, paramValue);
                }
                else
                {
                    // Hmm... We didn't ask for it.
                    AddParam(paramName, paramValue);

                    Console.WriteLine($"Parameter named {paramName} with value {paramValue} is given but not needed by type {TypeId}");
                }
            }

            return ContainsUnresolvedParameters;
        }

        protected void CheckIfAllParametersResolvedOrElseThrow()
        {
            if (ContainsUnresolvedParameters)
            {
                throw new InvalidOperationException($"Type has unresolved parameters: {string.Join(", ", UnresolvedParameters.Keys)}");
            }
        }

        protected TemplateType_namedParameters(ResourceLocation typeId) : base(typeId)
        {

        }
    }
}
