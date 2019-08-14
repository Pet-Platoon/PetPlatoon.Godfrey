using System;

namespace PetPlatoon.Godfrey.Database.Common
{
    public interface IVersionedEntity
    {
        Guid Version { get; set; }
    }
}
