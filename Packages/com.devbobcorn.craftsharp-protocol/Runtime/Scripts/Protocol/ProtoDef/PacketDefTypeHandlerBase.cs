#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using CraftSharp.Protocol.ProtoDef.NativeTypes;
using System.Diagnostics.CodeAnalysis;

namespace CraftSharp.Protocol.ProtoDef
{
    public abstract class PacketDefTypeHandlerBase
    {
        // Conditional https://github.com/ProtoDef-io/ProtoDef/blob/master/doc/datatypes/conditional.md
        private static readonly ResourceLocation SWITCH_ID = new("switch");
        private static readonly ResourceLocation OPTION_ID = new("option");

        // Numeric https://github.com/ProtoDef-io/ProtoDef/blob/master/doc/datatypes/numeric.md
        public static readonly ResourceLocation VARINT_ID = new("varint");
        public static readonly ResourceLocation VARLONG_ID = new("varlong");
        public static readonly ResourceLocation I8_ID = new("i8");
        public static readonly ResourceLocation I16_ID = new("i16");
        public static readonly ResourceLocation I32_ID = new("i32");
        public static readonly ResourceLocation I64_ID = new("i64");
        public static readonly ResourceLocation U8_ID = new("u8");
        public static readonly ResourceLocation U16_ID = new("u16");
        public static readonly ResourceLocation U32_ID = new("u32");
        public static readonly ResourceLocation U64_ID = new("u64");
        public static readonly ResourceLocation F32_ID = new("f32");
        public static readonly ResourceLocation F64_ID = new("f64");

        // Primitives https://github.com/ProtoDef-io/ProtoDef/blob/master/doc/datatypes/primitives.md
        public static readonly ResourceLocation BOOL_ID = new("bool");
        public static readonly ResourceLocation VOID_ID = new("void");

        // Structures https://github.com/ProtoDef-io/ProtoDef/blob/master/doc/datatypes/structures.md
        private static readonly ResourceLocation ARRAY_ID = new("array");
        private static readonly ResourceLocation CONTAINER_ID = new("container");

        // Utils https://github.com/ProtoDef-io/ProtoDef/blob/master/doc/datatypes/utils.md
        private static readonly ResourceLocation BUFFER_ID = new("buffer");
        private static readonly ResourceLocation BITFIELD_ID = new("bitfield");
        private static readonly ResourceLocation MAPPER_ID = new("mapper");
        private static readonly ResourceLocation PSTRING_ID = new("pstring");

        // Xtended from PrismarineJS https://github.com/PrismarineJS/minecraft-data/blob/master/data/pc/1.20.5/protocol.json
        public static readonly ResourceLocation UUID_ID = new("UUID");
        public static readonly ResourceLocation ENTITY_METADATA_LOOP_ID = new("entityMetadataLoop");
        public static readonly ResourceLocation TOP_BIT_SET_TERMINATED_ARRAY_ID = new("topBitSetTerminatedArray");
        public static readonly ResourceLocation REST_BUFFER_ID = new("restBuffer");
        public static readonly ResourceLocation NBT_ID = new("nbt");
        public static readonly ResourceLocation OPTIONAL_NBT_ID = new("optionalNbt");
        public static readonly ResourceLocation ANONYMOUS_NBT_ID = new("anonymousNbt");
        public static readonly ResourceLocation ANON_OPTIONAL_NBT_ID = new("anonOptionalNbt");
        public static readonly ResourceLocation ARRAY_WITH_LENGTH_OFFSET_ID = new("arrayWithLengthOffset");

        private static readonly HashSet<ResourceLocation> NATIVE_TYPE_IDS = new()
        {
            SWITCH_ID, OPTION_ID, VARINT_ID, VARLONG_ID, I8_ID, I16_ID, I32_ID, I64_ID,
            U8_ID, U16_ID, U32_ID, U64_ID, F32_ID, F64_ID, BOOL_ID, VOID_ID, ARRAY_ID,
            CONTAINER_ID, BUFFER_ID, BITFIELD_ID, MAPPER_ID, PSTRING_ID, UUID_ID,
            ENTITY_METADATA_LOOP_ID, TOP_BIT_SET_TERMINATED_ARRAY_ID, REST_BUFFER_ID,
            NBT_ID, OPTIONAL_NBT_ID, ANONYMOUS_NBT_ID, ANON_OPTIONAL_NBT_ID,
            ARRAY_WITH_LENGTH_OFFSET_ID
        };

        private static PacketDefTypeHandlerBase BuildDefTypeHandler(string? scope, ResourceLocation customTypeId, ResourceLocation underlyingTypeId, JToken? typeParams, JObject typeDict)
        {
            PacketDefTypeHandlerBase buildItemHandler(JToken itemToken)
            {
                var (itemTypeId, itemTypeParams) = GetUnderlyingTypeIdAndParams(scope, itemToken, typeDict);

                if (itemTypeParams is null && typeDict.ContainsKey(itemTypeId.Path))
                {
                    WriteLine($"Build {itemTypeId} (Proxied)");
                    return new PacketDefTypeHandlerProxy(itemTypeId, itemTypeId);
                }

                WriteLine($"Build {itemTypeId}");

                // Nested type definitions don't have custom type ids
                return BuildDefTypeHandler(scope, itemTypeId, itemTypeId, itemTypeParams, typeDict);
            }

            if (!LOADED_DEF_TYPES.ContainsKey(underlyingTypeId))
            {
                // This type's underlying type is not loaded yet, see if we can load it
                var underlyingTypeIdPath = underlyingTypeId.Path;

                if (typeDict is not null && typeDict.ContainsKey(underlyingTypeIdPath)) // Defined but not loaded yet
                {
                    // Load this type first
                    var (dependentTypeId, dependentTypeParams) = GetUnderlyingTypeIdAndParams(scope, typeDict[underlyingTypeIdPath]!, typeDict);

                    WriteLine($"Load dependent {dependentTypeId} for {customTypeId}");

                    // Nested type definitions don't have custom type ids
                    var handler = BuildDefTypeHandler(scope, dependentTypeId, dependentTypeId, dependentTypeParams, typeDict);

                    // Register type
                    RegisterType(underlyingTypeId, handler);

                    // Call self again with same arguments
                    return BuildDefTypeHandler(scope, customTypeId, underlyingTypeId, typeParams, typeDict);
                }

                throw new KeyNotFoundException($"Underlying type {underlyingTypeId} (used by {customTypeId}) is not loaded, and is not present in declared type list!");
            }

            PacketDefTypeHandlerBase? getUnderlyingTypeHandler()
            {
                if (NATIVE_TYPE_IDS.Contains(underlyingTypeId))
                {
                    return null; // Nothing to inherit
                }

                return GetLoadedHandler(underlyingTypeId);
            }

            void RegisterIfNamed(PacketDefTypeHandlerBase newlyCreatedHandler)
            {
                if (customTypeId != underlyingTypeId)
                {
                    RegisterType(customTypeId, newlyCreatedHandler);
                }
            }

            var underlyingTypeHandler = getUnderlyingTypeHandler();

            // Conditional
            if (underlyingTypeId == SWITCH_ID || underlyingTypeHandler is ConditionalType_switch) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new ConditionalType_switch(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as ConditionalType_switch, buildItemHandler);
                RegisterIfNamed(created);

                return created;
            }

            if (underlyingTypeId == OPTION_ID) // Parameter not overridable, underlying type must be "option" to specify parameters
            {
                var wrappedHandler = buildItemHandler(typeParams!);
                return new ConditionalType_option(customTypeId, wrappedHandler);
            }

            // Structures
            if (underlyingTypeId == ARRAY_ID || underlyingTypeHandler is StructureType_array) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new StructureType_array(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as StructureType_array, buildItemHandler);
                RegisterIfNamed(created);

                return created;
            }

            if (underlyingTypeId == CONTAINER_ID) // Parameter not overridable, underlying type must be "container" to specify parameters
            {
                var containerItems = typeParams!.ToArray().Select(x =>
                {
                    var containerItem = (x as JObject)!;
                    var itemTypeHandler = buildItemHandler(containerItem["type"]!);

                    var named = containerItem.ContainsKey("name");
                    return (!named, named ? (string?)containerItem["name"] : null, itemTypeHandler);
                }).ToArray();

                return new StructureType_container(customTypeId, containerItems);
            }

            // Utils
            if (underlyingTypeId == BUFFER_ID || underlyingTypeHandler is UtilType_buffer) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new UtilType_buffer(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as UtilType_buffer);
                RegisterIfNamed(created);

                return created;
            }

            if (underlyingTypeId == BITFIELD_ID) // Parameter not overridable, underlying type must be "bitfield" to specify parameters
            {
                var bitfieldItems = typeParams!.ToArray().Select(x =>
                {
                    var bitFieldItem = (x as JObject)!;

                    var name = (string)bitFieldItem.GetValue("name")!;
                    var size = (int)bitFieldItem.GetValue("size")!;
                    var signed = (bool)bitFieldItem.GetValue("signed")!;

                    return (name, size, signed);
                }).ToArray();

                return new UtilType_bitfield(customTypeId, bitfieldItems);
            }

            if (underlyingTypeId == MAPPER_ID || underlyingTypeHandler is UtilType_mapper) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new UtilType_mapper(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as UtilType_mapper, buildItemHandler);
                RegisterIfNamed(created);

                return created;
            }

            if (underlyingTypeId == PSTRING_ID || underlyingTypeHandler is UtilType_pstring) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new UtilType_pstring(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as UtilType_pstring);
                RegisterIfNamed(created);

                return created;
            }

            // Xtended
            if (underlyingTypeId == ENTITY_METADATA_LOOP_ID || underlyingTypeHandler is XtendedType_entityMetadataLoop) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new XtendedType_entityMetadataLoop(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as XtendedType_entityMetadataLoop, buildItemHandler);
                RegisterIfNamed(created);

                return created;
            }

            if (underlyingTypeId == TOP_BIT_SET_TERMINATED_ARRAY_ID || underlyingTypeHandler is XtendedType_topBitSetTerminatedArray) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new XtendedType_topBitSetTerminatedArray(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as XtendedType_topBitSetTerminatedArray, buildItemHandler);
                RegisterIfNamed(created);

                return created;
            }

            if (underlyingTypeId == ARRAY_WITH_LENGTH_OFFSET_ID || underlyingTypeHandler is XtendedType_arrayWithLengthOffset) // Parameter overridable
            {
                Dictionary<string, JToken> paramsAsDict = typeParams?.ToObject<Dictionary<string, JToken>>() ?? new();
                var created = new XtendedType_arrayWithLengthOffset(customTypeId, paramsAsDict,
                    getUnderlyingTypeHandler() as XtendedType_arrayWithLengthOffset, buildItemHandler);
                RegisterIfNamed(created);

                return created;
            }

            // Get unparameterized type handler directly from the table
            if (LOADED_DEF_TYPES.TryGetValue(underlyingTypeId, out PacketDefTypeHandlerBase? value))
            {
                if (value is null)
                {
                    throw new InvalidDataException($"Handler for {underlyingTypeId} is not loaded!");
                }

                return value;
            }

            throw new InvalidDataException("Why???");
        }

        public static readonly HashSet<ResourceLocation> CUSTOM_DEF_TYPES = new();

        public static readonly Dictionary<ResourceLocation, PacketDefTypeHandlerBase?> LOADED_DEF_TYPES = GetNativeTypes();

        private static Dictionary<ResourceLocation, PacketDefTypeHandlerBase?> GetNativeTypes() => new()
        {
            // Conditional (Dummy)
            [SWITCH_ID] = null,
            [OPTION_ID] = null,

            // Numeric
            [VARINT_ID] = new NumericType_varint(VARINT_ID),
            [VARLONG_ID] = new NumericType_varlong(VARLONG_ID),
            [I8_ID] = new NumericType_i8(I8_ID),
            [I16_ID] = new NumericType_i16(I16_ID),
            [I32_ID] = new NumericType_i32(I32_ID),
            [I64_ID] = new NumericType_i64(I64_ID),
            [U8_ID] = new NumericType_u8(U8_ID),
            [U16_ID] = new NumericType_u16(U16_ID),
            [U32_ID] = new NumericType_u32(U32_ID),
            [U64_ID] = new NumericType_u64(U64_ID),
            [F32_ID] = new NumericType_f32(F32_ID),
            [F64_ID] = new NumericType_f64(F64_ID),

            // Primitives
            [BOOL_ID] = new PrimitiveType_bool(BOOL_ID),
            [VOID_ID] = new PrimitiveType_void(VOID_ID),

            // Structures (Dummy)
            [ARRAY_ID] = null,
            [CONTAINER_ID] = null,

            // Utils (Dummy)
            [BUFFER_ID] = null,
            [BITFIELD_ID] = null,
            [MAPPER_ID] = null,
            [PSTRING_ID] = null,

            // Xtended (Some are Dummy)
            [UUID_ID] = new XtendedType_UUID(UUID_ID),
            [ENTITY_METADATA_LOOP_ID] = null,
            [TOP_BIT_SET_TERMINATED_ARRAY_ID] = null,
            [REST_BUFFER_ID] = new XtendedType_restBuffer(REST_BUFFER_ID),
            [NBT_ID] = new XtendedType_nbt(NBT_ID),
            [OPTIONAL_NBT_ID] = new XtendedType_nbt(OPTIONAL_NBT_ID),
            [ANONYMOUS_NBT_ID] = new XtendedType_nbt(ANONYMOUS_NBT_ID),
            [ANON_OPTIONAL_NBT_ID] = new XtendedType_nbt(ANON_OPTIONAL_NBT_ID),
            [ARRAY_WITH_LENGTH_OFFSET_ID] = null,
        };

        public static PacketDefTypeHandlerBase GetLoadedHandler(ResourceLocation typeId)
        {
            var handler = LOADED_DEF_TYPES[typeId];

            if (handler is not null)
            {
                return handler;
            }
            else
            {
                throw new InvalidOperationException($"Handler for type {typeId} is dummy and shouldn't be taken from this table!");
            }
        }

        public static bool TryGetLoadedHandler(ResourceLocation typeId, [MaybeNullWhen(false)] out PacketDefTypeHandlerBase? handler)
        {
            if (LOADED_DEF_TYPES.TryGetValue(typeId, out PacketDefTypeHandlerBase? h))
            {
                if (h is not null)
                {
                    handler = h;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        private static ResourceLocation GetTypeId(string? scope, string typeIdPath, JObject typeDict)
        {
            static ResourceLocation GetTypeIdInScope(string? s, string p)
            {
                if (string.IsNullOrEmpty(s)) // If scope is empty, use global namespace
                {
                    return new ResourceLocation(p);
                }
                else
                {
                    return new ResourceLocation(s, p);
                }
            }

            // First check local namespace. Not all types in local namespace might be loaded at
            // this point, so we use the typeDict to look it up.
            if (typeDict.ContainsKey(typeIdPath))
            {
                return GetTypeIdInScope(scope, typeIdPath);
            }

            // Types in global or parent namespaces should have been loaded before loading this namespace,
            // so for these types we just check the loaded table.
            while (!string.IsNullOrEmpty(scope))
            {
                // Fallback to parent namespaces, go up one level and check
                int pos = scope.LastIndexOf('/');
                scope = (pos == -1) ? string.Empty : scope[..pos];

                var typeId = GetTypeIdInScope(scope, typeIdPath);

                if (LOADED_DEF_TYPES.ContainsKey(typeId)) // This type is defined in global namespace
                {
                    return typeId;
                }
            }

            // Finally check if there're any known native type matching this id.
            // These are fallbacks of lowest priority to allow user override.
            var nativeTypeId = new ResourceLocation(typeIdPath);
            if (NATIVE_TYPE_IDS.Contains(nativeTypeId))
            {
                return nativeTypeId;
            }

            throw new InvalidDataException($"Type {typeIdPath} is defined in neither local nor parent namespaces, and is not a native type either!");
        }

        private static (ResourceLocation underlyingTypeId, JToken? underlyingTypeParams)
            GetUnderlyingTypeIdAndParams(string? scope, JToken typeToken, JObject typeDict)
        {
            if (typeToken.Type == JTokenType.String) // underlyingTypeId
            {
                var typeId = GetTypeId(scope, (string)typeToken!, typeDict);
                return (typeId, null);
            }
            else if (typeToken.Type == JTokenType.Array) // [ underlyingTypeId, typeParams ]
            {
                var typeId = GetTypeId(scope, (string)typeToken[0]!, typeDict);
                return (typeId, typeToken[1]);
            }
            else
            {
                throw new InvalidDataException($"Type definition should not be {typeToken.Type}!");
            }
        }

        public static void RegisterType(ResourceLocation typeId, PacketDefTypeHandlerBase typeHandler)
        {
            if (LOADED_DEF_TYPES.TryAdd(typeId, typeHandler))
            {
                //WriteLine($"- Type {typeId} registered.");
            }
            else
            {
                //WriteLine($"- Type {typeId} is already registered!");
            }
        }

        /// <summary>
        /// Register types defined in a scope(namespace). Use null as scope to define global types
        /// </summary>
        /// <param name="scope">Scope, null for global</param>
        /// <param name="typeDict">Current type dictionary</param>
        public static void RegisterTypes(string? scope, JObject typeDict)
        {
            if (typeDict is not null)
            {
                foreach (var pair in typeDict) // { typeIdPath: wrappedType }
                {
                    var typeId = string.IsNullOrEmpty(scope) ? new ResourceLocation(pair.Key)
                            : new ResourceLocation(scope, pair.Key);

                    if (LOADED_DEF_TYPES.ContainsKey(typeId))
                    {
                        // Probably loaded as a dependency of other types
                        continue;
                    }

                    if (pair.Value is not null)
                    {
                        var (underlyingTypeId, underlyingTypeParams) = GetUnderlyingTypeIdAndParams(scope, pair.Value, typeDict);
                        var handler = BuildDefTypeHandler(scope, typeId, underlyingTypeId, underlyingTypeParams, typeDict);

                        if (!LOADED_DEF_TYPES.ContainsKey(typeId)) // Didn't get registered when building handler for other types
                        {
                            // Register type
                            RegisterType(typeId, handler);
                        }
                    }
                }
            }
            else
            {
                WriteLine("Types field not defined!");
            }
        }

        /// <summary>
        /// Register types recursively for a json document.
        /// </summary>
        public static void RegisterTypesRecursive(string? scope, JObject? jsonDoc)
        {
            if (jsonDoc is null) return;

            foreach (var prop in jsonDoc.Properties())
            {
                if (prop.Name == "types") // Child namespace
                {
                    var curScope = string.IsNullOrEmpty(scope) ? null : scope;
                    RegisterTypes(curScope, (prop.Value as JObject)!);
                }
                else
                {
                    var childScope = string.IsNullOrEmpty(scope) ? prop.Name : $"{scope}/{prop.Name}";
                    RegisterTypesRecursive(childScope, prop.Value as JObject);
                }
            }
        }

        public static void ResetLoadedTypes()
        {
            LOADED_DEF_TYPES.Clear();

            var native = GetNativeTypes();

            foreach (var (key, value) in native)
            {
                LOADED_DEF_TYPES.Add(key, value);
            }
        }

        #region Instance members
        public PacketDefTypeHandlerBase(ResourceLocation typeId)
        {
            TypeId = typeId;
        }

        public readonly ResourceLocation TypeId;

        protected static void WriteLine(string line)
        {
            UnityEngine.Debug.Log(line);
        }

        public virtual Type GetValueType()
        {
            return typeof(object);
        }

        public abstract object? ReadValue(PacketRecord rec, string parentPath, Queue<byte> cache);
        #endregion
    }
}
