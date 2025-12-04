using Arch.Core;

namespace TTRPG.Core
{
    public interface IEntityFactory
    {
        /// <summary>
        /// Creates an entity in the given World based on a Blueprint ID.
        /// </summary>
        Entity Create(string blueprintId, World world);

        void ApplyTemplate(Entity entity, string templateId, World world);
    }
}