using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public abstract class EntityPalette
    {
        /// <summary>
        /// Get mapping dictionary. Must be overriden with proper implementation.
        /// </summary>
        /// <returns>Palette dictionary</returns>
        protected abstract Dictionary<int, EntityType> GetDict();

        /// <summary>
        /// Get entity type from type ID
        /// </summary>
        /// <param name="id">Entity type ID</param>
        /// <returns>EntityType corresponding to the specified ID</returns>
        public EntityType FromId(int id, bool living)
        {
            Dictionary<int, EntityType> entityTypes = GetDict();

            //1.14+ entities have the same set of IDs regardless of living status
            if (entityTypes.ContainsKey(id))
                return entityTypes[id];

            throw new System.IO.InvalidDataException($"Unknown Entity ID {id} in palette {GetType()}. Is it up to date for this Minecraft version?");
        }
    }
}
